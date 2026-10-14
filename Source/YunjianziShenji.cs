using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    // All Yunjianzi content belongs to the player's unique progression.
    // True jade steel is the sole item allowed in natural generation pools.
    [StaticConstructorOnStartup]
    internal static class YunjianziNaturalGenerationPolicy
    {
        static YunjianziNaturalGenerationPolicy()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null || def.defName == "YJZ_TrueJadeSteel") continue;
                string packageId = def.modContentPack?.PackageIdPlayerFacing;
                if (packageId == null || !packageId.StartsWith("Alex.MiliraPrivateExpansion.Yunjianzi", StringComparison.OrdinalIgnoreCase)) continue;

                def.generateCommonality = 0f;
                def.generateAllowChance = 0f;
                def.tradeability = Tradeability.None;

                // QuestNode_GetPawnKind builds its generic animal pool from
                // RaceProps and ignores generateCommonality/ecoSystemWeight.
                // Exclude every race owned by either Yunjianzi package while
                // leaving direct skill/PawnGenerator creation untouched.
                if (def.race != null)
                    def.race.neverIncludeInQuests = true;

                // PawnWeaponGenerator only admits tagged primary weapons.
                // Shenji and Zhuxie are created directly with ThingMaker.
                if (def.equipmentType == EquipmentType.Primary)
                    def.weaponTags = null;
            }
            PawnWeaponGenerator.Reset();
        }
    }

    /// <summary>
    /// The Foundation-stage ion round keeps vanilla explosive resolution but
    /// adds a compact cyan discharge so it reads as ionized energy rather than
    /// a conventional high-explosive minigun shell.
    /// </summary>
    public sealed class Projectile_YunjianziIonRound : Projectile_Explosive
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!blockedByShield && Map != null)
            {
                FleckMaker.ThrowLightningGlow(ExactPosition, Map, 0.55f);
                for (int i = 0; i < 3; i++) FleckMaker.ThrowMicroSparks(ExactPosition, Map);
            }
            base.Impact(hitThing, blockedByShield);
        }
    }

    public sealed class CompProperties_YunjianziShenji : CompProperties
    {
        public CompProperties_YunjianziShenji() { compClass = typeof(CompYunjianziShenji); }
    }

    [StaticConstructorOnStartup]
    public sealed partial class CompYunjianziShenji : ThingComp
    {
        // The shield textures carry real transparency.  Transparent is used here
        // instead of MoteGlow so the subtle honeycomb remains visible on every map
        // lighting profile without relying on black-background additive blending.
        private static readonly Material SolarShieldMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ShenjiShieldSolar", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.58f));
        private static readonly Material SolarShieldHitMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ShenjiShieldSolar", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.95f));
        private static readonly Material DarkMatterShieldMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ShenjiShieldDarkMatter", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.68f));
        private static readonly Material DarkMatterShieldHitMaterial = MaterialPool.MatFrom(
            "Yunjianzi/Effects/ShenjiShieldDarkMatter", ShaderDatabase.Transparent, Color.white);
        private int ownerThingId = -1;
        private float shield;
        private int shieldBrokenUntil;
        private int lastShieldHitTick = -9999;
        private int rocketJumpReadyTick;
        private int recallRepairReadyTick;
        private int cloudCommandBurstReadyTick;
        private int shieldModelVersion;
        private int lastArmorTier;
        private int boundTick;
        private bool nativeControlBound;
        private int nextNativeControlAttemptTick;
        private Pawn ownerCache;

        public Pawn Pawn { get { return parent as Pawn; } }
        public Pawn Owner
        {
            get
            {
                if (ownerThingId < 0) return null;
                if (ownerCache != null && ownerCache.thingIDNumber == ownerThingId && !ownerCache.Destroyed)
                    return ownerCache;
                foreach (Map map in Find.Maps)
                {
                    Pawn found = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == ownerThingId);
                    if (found != null) return ownerCache = found;
                }
                return null;
            }
        }
        public CompYunjianziCultivation Cultivation { get { Pawn owner = Owner; return owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>(); } }
        public int BoundTick { get { return boundTick; } }
        internal float ShieldEnergy { get { return shield; } }
        internal int ShieldBrokenTicksRemaining
        {
            get { return Mathf.Max(0, shieldBrokenUntil - (Find.TickManager == null ? 0 : Find.TickManager.TicksGame)); }
        }
        public bool IsBoundTo(Pawn owner) { return owner != null && ownerThingId == owner.thingIDNumber; }
        public float ShieldMax
        {
            get
            {
                CompYunjianziCultivation c = Cultivation;
                if (c == null || !c.HasSkill(YunjianziSkills.ShenjiShield)) return 0f;
                if (c.HasSkill(YunjianziSkills.ShenjiDarkShield)) return 2000f;
                // Match Milira Imperium's 6.0-energy shield belts in the
                // integer energy scale used by this component (6.0 = 600).
                return 600f;
            }
        }

        private int RequiredShieldModelVersion
        {
            get { return Cultivation != null && Cultivation.HasSkill(YunjianziSkills.ShenjiDarkShield) ? 2 : 1; }
        }

        public bool BindTo(Pawn owner)
        {
            ownerThingId = owner == null ? -1 : owner.thingIDNumber;
            ownerCache = owner;
            boundTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            shield = ShieldMax;
            shieldModelVersion = RequiredShieldModelVersion;
            SynchronizeTier(true);
            nextNativeControlAttemptTick = 0;
            return EnsureNativeControlRelation();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 型号由培育配方决定；只在新机体生成时补齐无属性的内衬与目镜。
            SynchronizeTier(!respawningAfterLoad);
            // 旧版把“监督关系存在”误当成“已经进入控制组”。读档时重新核对一次，
            // 成功后 nativeControlBound 会再次锁定，不会成为持续轮询负担。
            if (respawningAfterLoad)
            {
                nativeControlBound = false;
                nextNativeControlAttemptTick = 0;
            }
            EnsureNativeControlRelation();
            if (!respawningAfterLoad && ownerThingId < 0)
                TryBindToLocalYunjianzi();

            // Migrate saves made before Shenji shield initialization was tied
            // to skill acquisition. Do not refill a shield that is still in
            // its genuine post-break cooldown.
            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            int requiredShieldModelVersion = RequiredShieldModelVersion;
            if (respawningAfterLoad && ShieldMax > 0f && shieldModelVersion < requiredShieldModelVersion)
            {
                shield = ShieldMax;
                shieldBrokenUntil = 0;
                shieldModelVersion = requiredShieldModelVersion;
            }
            else if (respawningAfterLoad && ShieldMax > 0f && shield <= 0f && shieldBrokenUntil <= now)
                shield = ShieldMax;
            else if (ShieldMax > 0f)
                shield = Mathf.Clamp(shield, 0f, ShieldMax);
        }

        internal void NotifyShieldSkillChanged()
        {
            shield = ShieldMax;
            shieldBrokenUntil = 0;
            shieldModelVersion = RequiredShieldModelVersion;
        }

        public override string CompInspectStringExtra()
        {
            float maximum = ShieldMax;
            if (maximum <= 0f)
                return Cultivation == null ? "神机护盾：未连接云间灵脉" : "神机护盾：尚未解锁";
            int remaining = Mathf.Max(0, shieldBrokenUntil - Find.TickManager.TicksGame);
            if (remaining > 0)
                return string.Format("神机护盾：已破碎（{0:0.0}秒后开始重聚）\n护盾值：0 / {1:0}", remaining / 60f, maximum);
            return string.Format("神机护盾：{0:0.#} / {1:0}", Mathf.Clamp(shield, 0f, maximum), maximum);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (Pawn != null && Pawn.Faction == Faction.OfPlayer && ShieldMax > 0f)
                yield return new Gizmo_YunjianziShenjiShieldStatus(this);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn pawn = Pawn;
            float maximum = ShieldMax;
            if (pawn == null || !pawn.Spawned || shield <= 0f || maximum <= 0f) return;

            // Keep the map quiet during ordinary work.  The shield surface is
            // revealed while drafted, or briefly after it reacts to a harmful hit.
            int ticksSinceHit = Find.TickManager.TicksGame - lastShieldHitTick;
            if (!pawn.Drafted && ticksSinceHit > 90) return;

            bool darkMatter = Cultivation != null && Cultivation.HasSkill(YunjianziSkills.ShenjiDarkShield);
            bool hitFlash = ticksSinceHit <= 8;
            Material material = darkMatter
                ? (hitFlash ? DarkMatterShieldHitMaterial : DarkMatterShieldMaterial)
                : (hitFlash ? SolarShieldHitMaterial : SolarShieldMaterial);
            if (material == null) return;

            float ratio = Mathf.Clamp01(shield / maximum);
            float pulse = 1f + Mathf.Sin((Find.TickManager.TicksGame + pawn.thingIDNumber) * 0.07f) * 0.025f;
            float diameter = Mathf.Lerp(1.7f, 2.05f, ratio) * pulse;
            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Quaternion.identity,
                new Vector3(diameter, 1f, diameter));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead) return;
            if (pawn.IsHashIntervalTick(60))
            {
                if (!nativeControlBound && Find.TickManager.TicksGame >= nextNativeControlAttemptTick)
                    EnsureNativeControlRelation();
                if (ownerThingId < 0)
                    TryBindToLocalYunjianzi();
                if (pawn.needs != null && pawn.needs.energy != null) pawn.needs.energy.CurLevelPercentage = 1f;
                if (Find.TickManager.TicksGame >= shieldBrokenUntil && ShieldMax > 0f)
                    // MI belts recharge at 0.15 against a 6.0 capacity: 2.5%
                    // per second, or about 40 seconds from empty to full.
                    shield = Mathf.Min(ShieldMax, shield + ShieldMax / 40f);
                TryAutomaticRocketJump();
            }
        }

        private bool EnsureNativeControlRelation()
        {
            if (nativeControlBound) return true;
            if (!ModsConfig.BiotechActive || Find.TickManager.TicksGame < nextNativeControlAttemptTick) return false;
            nextNativeControlAttemptTick = Find.TickManager.TicksGame + 300;
            Pawn pawn = Pawn;
            Pawn owner = Owner;
            if (pawn == null || owner == null || pawn.relations == null) return false;

            bool addedRelation = false;
            try
            {
                if (owner.mechanitor == null)
                    owner.mechanitor = new Pawn_MechanitorTracker(owner);

                // RimWorld 1.6 exposes this public lifecycle notification.  With
                // respawningAfterLoad=true it refreshes bandwidth and control
                // groups, avoiding reflection into the private tracker method.
                owner.mechanitor.Notify_PawnSpawned(true);
                if (owner.mechanitor.TotalAvailableControlGroups <= 0)
                    throw new InvalidOperationException("云间子没有可用的机械控制组。请确认云间灵脉的 MechControlGroups 属性已加载。");

                if (!pawn.relations.DirectRelationExists(PawnRelationDefOf.Overseer, owner))
                {
                    pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, owner);
                    addedRelation = true;
                }

                owner.mechanitor.AssignPawnControlGroup(pawn, MechWorkModeDefOf.Work);
                MechanitorControlGroup group = MechanitorUtility.GetMechControlGroup(pawn);
                nativeControlBound = group != null;
                if (!nativeControlBound)
                    throw new InvalidOperationException("原版控制组未接受云间神机。绑定已回滚。");
                return true;
            }
            catch (Exception ex)
            {
                if (addedRelation && pawn.relations.DirectRelationExists(PawnRelationDefOf.Overseer, owner))
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, owner);
                Log.ErrorOnce("[Yunjianzi] 神机控制关系初始化失败：" + ex, pawn.thingIDNumber ^ 0x59A31);
                return false;
            }
        }

        private int ArmorTier
        {
            get
            {
                Pawn pawn = Pawn;
                if (pawn != null && pawn.kindDef == YJZDefOf.YJZ_Shenji_Tianjiang) return 3;
                if (pawn != null && pawn.kindDef == YJZDefOf.YJZ_Shenji_Lingwei) return 2;
                return 1;
            }
        }

        private void SynchronizeTier(bool force)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.health == null) return;
            int currentTier = ArmorTier;
            int tier = currentTier;
            // 旧存档可能仍带有早期的固有装甲 Hediff；属性已迁移到可见的专属神机甲，必须清掉以免双算。
            List<Hediff> oldArmor = pawn.health.hediffSet.hediffs.Where(h => h.def == YJZDefOf.YJZ_ShenjiArmor_Base || h.def == YJZDefOf.YJZ_ShenjiArmor_Intermediate || h.def == YJZDefOf.YJZ_ShenjiArmor_Final).ToList();
            for (int i = 0; i < oldArmor.Count; i++) pawn.health.RemoveHediff(oldArmor[i]);

            if (pawn.apparel != null)
            {
                // Normalize existing Shenji linings as well as newly generated
                // ones. Mutating the material retains quality, identity and
                // wear; this follows vanilla CompAbilityEffect_Transmute.
                foreach (Apparel existingLining in pawn.apparel.WornApparel.Where(a =>
                    a.def == YJZDefOf.Milian_Lining && a.Stuff != YJZDefOf.Milira_FeatherThread))
                {
                    float durability = (float)existingLining.HitPoints / existingLining.MaxHitPoints;
                    existingLining.SetStuffDirect(YJZDefOf.Milira_FeatherThread);
                    StatDefOf.MaxHitPoints.Worker.ClearCacheForThing(existingLining);
                    existingLining.Notify_ColorChanged();
                    existingLining.HitPoints = Mathf.Clamp(Mathf.CeilToInt(existingLining.MaxHitPoints * durability),
                        1, existingLining.MaxHitPoints);
                    pawn.apparel.Notify_ApparelChanged();
                }
                // Issue a missing lining only at initial generation, so taking
                // it off or losing it does not generate a replacement on load.
                bool firstInitialization = force && lastArmorTier <= 0;
                bool hasLining = pawn.apparel.WornApparel
                    .Any(a => a.def == YJZDefOf.Milian_Lining);
                if (firstInitialization && !hasLining)
                {
                    Apparel lining = ThingMaker.MakeThing(YJZDefOf.Milian_Lining, YJZDefOf.Milira_FeatherThread) as Apparel;
                    if (lining != null)
                    {
                        CompQuality quality = lining.TryGetComp<CompQuality>();
                        if (quality != null)
                            quality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                        pawn.apparel.Wear(lining, false);
                    }
                }

                // Preserve old-save gear by dropping armor/headgear that no
                // longer matches this fixed chassis instead of destroying it.
                List<Apparel> invalidGear = pawn.apparel.WornApparel.Where(a =>
                    !YunjianziShenjiGearPolicy.Allows(a.def, pawn)).ToList();
                for (int i = 0; i < invalidGear.Count; i++)
                {
                    Apparel dropped;
                    pawn.apparel.TryDrop(invalidGear[i], out dropped, pawn.Position, false);
                }
                if (force || lastArmorTier <= 0) lastArmorTier = tier;
            }
            CompYunjianziCultivation cultivation = Cultivation;
            if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
            {
                bool accessoriesUnlocked = cultivation != null && cultivation.HasSkill(YunjianziSkills.ShenjiAccessories);
                List<Apparel> lockedAccessories = pawn.apparel.WornApparel
                    .Where(a => !YunjianziShenjiApparelPolicy.IsIntrinsicApparel(a.def) &&
                        (!accessoriesUnlocked || !YunjianziShenjiApparelPolicy.IsAllowedAccessory(a.def)))
                    .ToList();
                for (int i = 0; i < lockedAccessories.Count; i++)
                {
                    Apparel dropped;
                    pawn.apparel.TryDrop(lockedAccessories[i], out dropped, pawn.Position, false);
                }
            }
        }

        private void TryBindToLocalYunjianzi()
        {
            Pawn pawn = Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Faction != Faction.OfPlayer) return;
            Pawn gestatorOwner = pawn.GetOverseer();
            CompYunjianziCultivation gestatorCultivation = gestatorOwner == null ? null : gestatorOwner.TryGetComp<CompYunjianziCultivation>();
            if (gestatorCultivation != null && gestatorCultivation.IsYunjianzi &&
                gestatorCultivation.HasSkill(YunjianziSkills.ShenjiSummon) && gestatorCultivation.CanAcceptShenji)
            {
                BindTo(gestatorOwner);
                return;
            }
            CompYunjianziCultivation owner = pawn.Map.mapPawns.FreeColonistsSpawned
                .Select(p => p.TryGetComp<CompYunjianziCultivation>())
                .Where(c => c != null && c.IsYunjianzi && c.HasSkill(YunjianziSkills.ShenjiSummon) && c.CanAcceptShenji)
                .OrderBy(c => c.BoundShenjiCount).FirstOrDefault();
            if (owner != null) BindTo(owner.Pawn);
            else if (gestatorOwner != null && gestatorOwner.relations.DirectRelationExists(PawnRelationDefOf.Overseer, pawn))
            {
                gestatorOwner.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, pawn);
                Messages.Message("神机控制数已达上限；新培育机体暂未绑定，请先拆解或解除一台现役神机。",
                    pawn, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void TryTriggerCloudCommandBurst()
        {
            Pawn pawn = Pawn;
            int now = Find.TickManager.TicksGame;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || now < cloudCommandBurstReadyTick ||
                YJZDefOf.YJZ_Hediff_CloudCommandChip == null ||
                pawn.health?.hediffSet?.HasHediff(YJZDefOf.YJZ_Hediff_CloudCommandChip) != true)
                return;

            cloudCommandBurstReadyTick = now + 60000;
            shield = ShieldMax;
            shieldBrokenUntil = 0;
            Map map = pawn.Map;
            List<Pawn> enemies = GenRadial.RadialDistinctThingsAround(pawn.Position, map, 6f, true)
                .OfType<Pawn>()
                .Where(enemy => enemy != pawn && enemy.Spawned && !enemy.Dead && enemy.HostileTo(pawn))
                .ToList();

            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastAreaEffect, 2.35f);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, map, 3.1f);
            for (int index = 0; index < enemies.Count; index++)
            {
                Pawn enemy = enemies[index];
                int dx = Math.Sign(enemy.Position.x - pawn.Position.x);
                int dz = Math.Sign(enemy.Position.z - pawn.Position.z);
                if (dx == 0 && dz == 0)
                {
                    Rot4 fallback = new Rot4(index % 4);
                    dx = fallback.FacingCell.x;
                    dz = fallback.FacingCell.z;
                }
                IntVec3 direction = new IntVec3(dx, 0, dz);
                IntVec3 destination = enemy.Position;
                for (int step = 1; step <= 4; step++)
                {
                    IntVec3 candidate = enemy.Position + direction * step;
                    if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map) ||
                        candidate.GetFirstPawn(map) != null) break;
                    destination = candidate;
                }
                if (destination == enemy.Position) continue;
                Vector3 from = enemy.DrawPos;
                enemy.pather?.StopDead();
                enemy.Position = destination;
                enemy.Notify_Teleported(false, true);
                YunjianziCombatEffects.ShowKnockback(from, enemy.DrawPos, map);
            }
        }

        public bool TryPreventDeath()
        {
            CompYunjianziCultivation c = Cultivation;
            Pawn pawn = Pawn;
            Pawn owner = Owner;
            if (pawn == null || owner == null || c == null || !c.HasSkill(YunjianziSkills.ShenjiRecallRepair) || Find.TickManager.TicksGame < recallRepairReadyTick) return false;
            recallRepairReadyTick = Find.TickManager.TicksGame + 120000;
            List<Hediff> removable = pawn.health.hediffSet.hediffs.Where(h => h.def.isBad).ToList();
            for (int i = 0; i < removable.Count; i++) pawn.health.RemoveHediff(removable[i]);
            if (pawn.Spawned && owner.Spawned && pawn.Map == owner.Map)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(owner.Position, owner.Map, 3);
                if (cell.IsValid) pawn.Position = cell;
                FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 1.5f);
            }
            shield = ShieldMax;
            Messages.Message("神机被击溃，已归返云间子身边并完成重构。", pawn, MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private void TryAutomaticRocketJump()
        {
            CompYunjianziCultivation c = Cultivation;
            Pawn pawn = Pawn;
            if (c == null || !c.HasSkill(YunjianziSkills.ShenjiRocketJump) || pawn == null || !pawn.Spawned || !pawn.Drafted || Find.TickManager.TicksGame < rocketJumpReadyTick) return;
            Pawn enemy = pawn.Map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.HostileTo(pawn) && !p.Dead && p.Position.DistanceTo(pawn.Position) <= 7f);
            if (enemy == null) return;
            IntVec3 best = IntVec3.Invalid;
            float score = pawn.Position.DistanceTo(enemy.Position);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 12f, true))
            {
                if (!cell.InBounds(pawn.Map) || !cell.Standable(pawn.Map)) continue;
                float candidate = cell.DistanceTo(enemy.Position);
                if (candidate > score) { score = candidate; best = cell; }
            }
            if (!best.IsValid) return;
            FleckMaker.Static(pawn.Position, pawn.Map, FleckDefOf.PsycastSkipFlashEntry, 1.1f);
            pawn.Position = best;
            pawn.Notify_Teleported(false, true);
            FleckMaker.Static(best, pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1.2f);
            rocketJumpReadyTick = Find.TickManager.TicksGame + 60000;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ownerThingId, "yjz_shenjiOwner", -1);
            Scribe_Values.Look(ref shield, "yjz_shenjiShield", 0f);
            Scribe_Values.Look(ref shieldBrokenUntil, "yjz_shenjiShieldBrokenUntil", 0);
            Scribe_Values.Look(ref rocketJumpReadyTick, "yjz_shenjiRocketJumpReady", 0);
            Scribe_Values.Look(ref recallRepairReadyTick, "yjz_shenjiRecallRepairReady", 0);
            Scribe_Values.Look(ref cloudCommandBurstReadyTick, "yjz_shenjiCloudCommandBurstReady", 0);
            Scribe_Values.Look(ref shieldModelVersion, "yjz_shenjiShieldModelVersion", 0);
            Scribe_Values.Look(ref lastArmorTier, "yjz_shenjiArmorTier", 0);
            Scribe_Values.Look(ref boundTick, "yjz_shenjiBoundTick", 0);
            Scribe_Values.Look(ref nativeControlBound, "yjz_shenjiNativeControlBound", false);
            bool legacyNativeControlAttempted = false;
            Scribe_Values.Look(ref legacyNativeControlAttempted, "yjz_shenjiNativeControlAttempted", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ownerCache = null;
                nextNativeControlAttemptTick = 0;
            }
        }
    }

    [StaticConstructorOnStartup]
    internal sealed class Gizmo_YunjianziShenjiShieldStatus : Gizmo
    {
        private static readonly Texture2D SolarBar = SolidColorMaterials.NewSolidColorTexture(new Color(0.28f, 0.82f, 0.86f));
        private static readonly Texture2D DarkBar = SolidColorMaterials.NewSolidColorTexture(new Color(0.52f, 0.28f, 0.78f));
        private static readonly Texture2D EmptyBar = SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.10f, 0.13f));
        private readonly CompYunjianziShenji shield;

        internal Gizmo_YunjianziShenjiShieldStatus(CompYunjianziShenji shield)
        {
            this.shield = shield;
            Order = -110f;
        }

        public override float GetWidth(float maxWidth) { return 160f; }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = outer.ContractedBy(6f);
            Widgets.DrawWindowBackground(outer);
            Text.Font = GameFont.Tiny;
            Rect title = inner;
            title.height = 28f;
            int brokenTicks = shield.ShieldBrokenTicksRemaining;
            Widgets.Label(title, brokenTicks > 0 ? "神机护盾（重聚中）" : "神机护盾");

            Rect bar = inner;
            bar.yMin = inner.y + 34f;
            float maximum = Mathf.Max(1f, shield.ShieldMax);
            float current = Mathf.Clamp(shield.ShieldEnergy, 0f, maximum);
            bool darkMatter = shield.Cultivation != null && shield.Cultivation.HasSkill(YunjianziSkills.ShenjiDarkShield);
            Widgets.FillableBar(bar, current / maximum, darkMatter ? DarkBar : SolarBar, EmptyBar, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            string value = brokenTicks > 0
                ? string.Format("0 / {0:0}  {1:0.0}s", maximum, brokenTicks / 60f)
                : string.Format("{0:0.#} / {1:0}", current, maximum);
            Widgets.Label(bar, value);
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(inner, "神机偏射护盾：玉光结盾容量600，普通/远程/爆炸每次扣50、明确近战扣300；太阴天幕容量2000，分别扣10/50。只要尚有护盾能量，即使本次扣费耗尽护盾，也完整挡下这一击，不按原始伤害漏伤。玉光结盾会被EMP击破，太阴天幕不消耗能量抵挡EMP。破盾后需20秒重聚，再逐步充能。" );
            return new GizmoResult(GizmoState.Clear);
        }
    }

    /// <summary>
    /// 紫府神机的主激光。基础命中仍交给 Ancot 的 Beam，金丹节点只负责追加
    /// 可见的小型折射光束，避免用无特效的直接扣血伪装“弹跳”。
    /// </summary>
    public sealed class Projectile_YunjianziShenjiLaser : AncotLibrary.Beam
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            Thing firingThing = launcher;
            Pawn firingPawn = firingThing as Pawn;
            Vector3 firstOrigin = hitThing != null ? hitThing.DrawPos : ExactPosition;
            CompYunjianziShenji shenji = firingPawn == null ? null : firingPawn.TryGetComp<CompYunjianziShenji>();
            bool canRicochet = !blockedByShield && hitThing != null && map != null && shenji != null &&
                shenji.Cultivation != null && shenji.Cultivation.HasSkill(YunjianziSkills.ShenjiRicochetLaser);

            base.Impact(hitThing, blockedByShield);
            if (!canRicochet) return;

            List<Thing> visited = new List<Thing> { hitThing };
            Vector3 source = firstOrigin;
            IntVec3 center = firstOrigin.ToIntVec3();
            for (int hop = 0; hop < 3; hop++)
            {
                Thing next = map.listerThings.ThingsInGroup(ThingRequestGroup.AttackTarget)
                    .Where(t => t != null && t.Spawned && !visited.Contains(t) && t.HostileTo(firingPawn) &&
                        (!(t is Pawn) || !((Pawn)t).Dead) && t.Position.DistanceTo(center) <= 7f)
                    .OrderBy(t => t.Position.DistanceToSquared(center))
                    .FirstOrDefault();
                if (next == null) break;

                Projectile beam = ThingMaker.MakeThing(YJZDefOf.YJZ_Projectile_ShenjiChainLaser) as Projectile;
                if (beam == null) break;
                GenSpawn.Spawn(beam, center, map);
                beam.Launch(firingThing, source, next, next, ProjectileHitFlags.IntendedTarget, false, equipment, equipmentDef);
                visited.Add(next);
                source = next.DrawPos;
                center = next.Position;
            }
        }
    }

    internal static class YunjianziShenjiApparelPolicy
    {
        public static bool IsIntrinsicApparel(ThingDef def)
        {
            return IsIntrinsicArmor(def) || def == YJZDefOf.YJZ_Apparel_ShenjiVisor ||
                def == YJZDefOf.YJZ_Apparel_JiuxiaoImmortalCrown ||
                def == YJZDefOf.Milian_Lining;
        }

        public static bool IsIntrinsicArmor(ThingDef def)
        {
            return def == YJZDefOf.YJZ_Apparel_ShenjiArmor_Base ||
                def == YJZDefOf.YJZ_Apparel_ShenjiArmor_Intermediate ||
                def == YJZDefOf.YJZ_Apparel_ShenjiArmor_Final;
        }

        // 专属神机甲已提供完整防护。配件节点只开放工具层，禁止其他实体装甲再次叠加。
        public static bool IsAllowedAccessory(ThingDef def)
        {
            if (def == null || def.apparel == null || def.apparel.layers == null) return false;
            for (int i = 0; i < def.apparel.layers.Count; i++)
            {
                ApparelLayerDef layer = def.apparel.layers[i];
                if (layer == ApparelLayerDefOf.OnSkin || layer == ApparelLayerDefOf.Middle ||
                    layer == ApparelLayerDefOf.Shell || layer == ApparelLayerDefOf.Overhead)
                    return false;
            }
            return true;
        }
    }

    public sealed partial class CompYunjianziCultivation
    {
        private int issuedShenjiCores = -1;

        private int ShenjiLimit
        {
            get
            {
                if (HasSkill(YunjianziSkills.ShenjiQuadControl)) return 9;
                if (HasSkill(YunjianziSkills.ShenjiDualControl)) return 4;
                return 1;
            }
        }

        private IEnumerable<Gizmo> GetShenjiGizmos()
        {
            int count = BoundShenji().Count;
            int worldTotal = CountWorldShenjiAndCores();
            if (worldTotal >= ShenjiLimit) yield break;
            Command_Action production = new Command_Action
            {
                defaultLabel = "凝成青羽神机核心",
                defaultDesc = "主动凝成一枚青羽神机核心。全世界现役神机与青羽、玄翎、九霄三类核心共享神机额度；只有总量低于上限时才能继续生成。核心可在合玉炉升炼，再投入米莉安培育器。\n\n全世界神机与核心：" + worldTotal + "/" + ShenjiLimit + "。\n当前受控神机：" + count + "/" + ShenjiLimit + "。",
                icon = YunjianziAbilityTextures.SummonShenji,
                action = TryGenerateShenjiCore
            };
            yield return production;
        }

        internal int BoundShenjiCount { get { return BoundShenji().Count; } }
        internal bool CanAcceptShenji { get { return BoundShenjiCount < ShenjiLimit; } }

        private void TryGenerateShenjiCore()
        {
            if (Pawn == null || !Pawn.Spawned || !HasSkill(YunjianziSkills.ShenjiSummon)) return;
            int existing = CountWorldShenjiAndCores();
            issuedShenjiCores = existing;
            if (existing >= ShenjiLimit)
            {
                Messages.Message("全世界的神机与三类核心总量已达到当前额度上限。", Pawn,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            ThingDef coreDef = DefDatabase<ThingDef>.GetNamedSilentFail("YJZ_ShenjiCore_Qingyu");
            if (coreDef == null) return;
            Thing core = ThingMaker.MakeThing(coreDef);
            if (!GenPlace.TryPlaceThing(core, Pawn.Position, Pawn.Map, ThingPlaceMode.Near))
            {
                core.Destroy();
                return;
            }
            issuedShenjiCores = existing + 1;
            Messages.Message("《神机篇》的灵机凝成一枚青羽神机核心。请在合玉炉升炼，或将其用于米莉安培育器。",
                core, MessageTypeDefOf.PositiveEvent, false);
        }

        private static int CountWorldShenjiAndCores()
        {
            HashSet<Thing> things = new HashSet<Thing>();
            List<Thing> heldThings = new List<Thing>();

            // ListerThings covers everything spawned on every loaded map;
            // recursive traversal additionally reaches inventories, gestators,
            // transporters and other nested holders.
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.AllThings)
                    things.Add(thing);
                ThingOwnerUtility.GetAllThingsRecursively(map, heldThings, true);
                foreach (Thing thing in heldThings)
                    things.Add(thing);
            }

            // World is an IThingHolder whose descendants include caravans,
            // travelling transporters, world pawns and world-object contents.
            if (Find.World != null)
            {
                ThingOwnerUtility.GetAllThingsRecursively(Find.World, heldThings, true);
                foreach (Thing thing in heldThings)
                    things.Add(thing);
            }

            int total = 0;
            foreach (Thing thing in things)
            {
                if (thing == null || thing.Destroyed) continue;
                string defName = thing.def?.defName;
                if (defName == "YJZ_ShenjiCore_Qingyu" ||
                    defName == "YJZ_ShenjiCore_Lingwei" ||
                    defName == "YJZ_ShenjiCore_Tianjiang")
                {
                    total += thing.stackCount;
                }
                else if (thing is Pawn mech && !mech.Dead &&
                    (defName == "YJZ_Shenji_Race" ||
                     defName == "YJZ_Shenji_Race_Lingwei" ||
                     defName == "YJZ_Shenji_Race_Tianjiang"))
                {
                    total++;
                }
            }
            return total;
        }

        private List<Pawn> BoundShenji()
        {
            List<Pawn> result = new List<Pawn>();
            if (Pawn == null) return result;
            List<Map> maps = Find.Maps.ToList();
            for (int m = 0; m < maps.Count; m++)
            {
                Pawn[] snapshot = maps[m].mapPawns.AllPawns.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    Pawn candidate = snapshot[i];
                    CompYunjianziShenji comp = candidate.TryGetComp<CompYunjianziShenji>();
                    if (comp != null && comp.IsBoundTo(Pawn) && !candidate.Dead) result.Add(candidate);
                }
            }
            return result;
        }

        internal void RefreshBoundShenjiShields()
        {
            List<Pawn> shenji = BoundShenji();
            for (int i = 0; i < shenji.Count; i++)
                shenji[i].TryGetComp<CompYunjianziShenji>()?.NotifyShieldSkillChanged();
        }

    }

    public sealed class RecipeWorker_YunjianziShenji : RecipeWorker
    {
        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            int realm;
            string requiredSkill;
            Requirements(out realm, out requiredSkill);
            Map map = thing == null ? null : thing.Map;
            bool available = map != null && map.mapPawns.FreeColonistsSpawned.Any(p =>
            {
                CompYunjianziCultivation c = p.TryGetComp<CompYunjianziCultivation>();
                return c != null && c.IsYunjianzi && c.Realm >= realm &&
                    (requiredSkill == null || c.HasSkill(requiredSkill));
            });
            if (available) return true;
            string detail = realm > 0 ? CompYunjianziCultivation.RealmName(realm) + "境" : "神机诀";
            return "需要地图上的云间子达到" + detail + (requiredSkill == null ? "。" : "并掌握对应神机诀节点。");
        }

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return AvailableReport(thing, part).Accepted;
        }

        private void Requirements(out int realm, out string skill)
        {
            string id = recipe == null ? string.Empty : recipe.defName;
            realm = 1;
            skill = YunjianziSkills.ShenjiSummon;
            if (id == "YJZ_UpgradeCore_Lingwei" || id == "YJZ_Make_ShenjiArmor_Intermediate" ||
                id == "YJZ_Make_Shenji_Gauss" || id == "YJZ_Make_ShenjiVisor")
            {
                realm = 3; skill = YunjianziSkills.ShenjiGauss;
            }
            else if (id == "YJZ_UpgradeCore_Tianjiang" || id == "YJZ_Make_ShenjiArmor_Final" ||
                id == "YJZ_Make_Shenji_Legendary" || id == "YJZ_Make_JiuxiaoImmortalCrown")
            {
                realm = 4; skill = YunjianziSkills.ShenjiLegendaryFirepower;
            }
            else if (id == "YJZ_Make_Shenji_Gatling")
            {
                realm = 2; skill = YunjianziSkills.ShenjiGatling;
            }
        }
    }

    /// <summary>
    /// 云间子以神机诀直接控制专属米莉安。让她拥有原版 Mechanitor tracker，
    /// 监督关系才能驱动征召和工作 AI；神机仍为零带宽单位。
    /// </summary>
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.ShouldBeMechanitor))]
    internal static class Patch_MechanitorUtility_YunjianziController
    {
        private static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result || pawn == null) return;
            CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation != null && cultivation.IsYunjianzi) __result = true;
        }
    }

    // 专属神机与飞龙获得零带宽特许；普通机械体完全保留原版判定，
    // 因而仍会消耗云间灵脉实际提供的剩余带宽与控制组。
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.CanControlMech))]
    internal static class Patch_MechanitorUtility_YunjianziSpecialMechAuthorization
    {
        private static void Postfix(Pawn pawn, Pawn mech, ref AcceptanceReport __result)
        {
            CompYunjianziCultivation cultivation = pawn == null ? null : pawn.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.IsYunjianzi) return;
            CompYunjianziShenji shenji = mech == null ? null : mech.TryGetComp<CompYunjianziShenji>();
            CompYunjianziSkyWyvern wyvern = mech == null ? null : mech.TryGetComp<CompYunjianziSkyWyvern>();
            // Only override a rejection for a correctly bound special unit.
            // Never replace the vanilla result for ordinary Milian/mechanoids.
            if ((shenji != null && shenji.IsBoundTo(pawn)) ||
                (wyvern != null && wyvern.IsBoundTo(pawn)))
                __result = true;
        }
    }

    // Vanilla reconstructs disassembly yields by scanning for the first recipe
    // that produces the mech.  The three Shenji models have tiered gestation
    // recipes, so explicitly preserve the core that belongs to the chassis.
    [HarmonyPatch(typeof(MechanitorUtility), nameof(MechanitorUtility.IngredientsFromDisassembly))]
    internal static class Patch_MechanitorUtility_YunjianziShenjiDisassemblyCore
    {
        private static void Postfix(ThingDef mech, ref List<ThingDefCountClass> __result)
        {
            if (mech == null || __result == null) return;

            string coreDefName;
            switch (mech.defName)
            {
                case "YJZ_Shenji_Race":
                    coreDefName = "YJZ_ShenjiCore_Qingyu";
                    break;
                case "YJZ_Shenji_Race_Lingwei":
                    coreDefName = "YJZ_ShenjiCore_Lingwei";
                    break;
                case "YJZ_Shenji_Race_Tianjiang":
                    coreDefName = "YJZ_ShenjiCore_Tianjiang";
                    break;
                default:
                    return;
            }

            __result.RemoveAll(item => item?.thingDef != null &&
                (item.thingDef.defName == "YJZ_ShenjiCore_Qingyu" ||
                 item.thingDef.defName == "YJZ_ShenjiCore_Lingwei" ||
                 item.thingDef.defName == "YJZ_ShenjiCore_Tianjiang"));

            ThingDef coreDef = DefDatabase<ThingDef>.GetNamedSilentFail(coreDefName);
            if (coreDef != null)
                __result.Add(new ThingDefCountClass(coreDef, 1));
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) })]
    internal static class Patch_Pawn_Kill_YunjianziShenjiRecall
    {
        private static bool Prefix(Pawn __instance)
        {
            CompYunjianziShenji comp = __instance == null ? null : __instance.TryGetComp<CompYunjianziShenji>();
            return comp == null || !comp.TryPreventDeath();
        }
    }

    // 米莉拉原版工作/配置页会排除没有 OverseerSubject 的机械体。
    // 神机使用真实的 Overseer 关系与原版控制组；这里仅补足外部配置页的识别兼容。
    // 米莉拉配件白名单兼容。
    [StaticConstructorOnStartup]
    internal static class YunjianziShenjiAccessoryCompatibility
    {
        static YunjianziShenjiAccessoryCompatibility()
        {
            LongEventHandler.ExecuteWhenFinished(InjectShenjiIntoMiliraAccessories);
        }

        private static void InjectShenjiIntoMiliraAccessories()
        {
            List<PawnKindDef> shenjiKinds = new[] { "YJZ_Shenji", "YJZ_Shenji_Lingwei", "YJZ_Shenji_Tianjiang" }
                .Select(DefDatabase<PawnKindDef>.GetNamedSilentFail).Where(kind => kind != null).ToList();
            if (shenjiKinds.Count == 0) return;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                string package = def.modContentPack == null ? string.Empty : def.modContentPack.PackageIdPlayerFacing.ToLowerInvariant();
                bool officialMilira = package.Contains("ancot.milirarace") || package.Contains("ariandel.miliraimperium");
                if (!officialMilira || package.Contains("wingsofdemocracy")) continue;
                if (YunjianziShenjiApparelPolicy.IsAllowedAccessory(def) && def.apparel.tags != null && def.apparel.tags.Any(t => t != null && t.StartsWith("Milian_")) && !def.apparel.tags.Contains("YJZ_Shenji"))
                    def.apparel.tags.Add("YJZ_Shenji");
                if (def.comps == null) continue;
                for (int i = 0; i < def.comps.Count; i++)
                {
                    CompProperties props = def.comps[i];
                    System.Reflection.FieldInfo field = AccessTools.Field(props.GetType(), "targetableMilianPawnkinds");
                    if (field == null) continue;
                    System.Collections.IList list = field.GetValue(props) as System.Collections.IList;
                    if (list != null)
                        for (int kindIndex = 0; kindIndex < shenjiKinds.Count; kindIndex++)
                            if (!list.Contains(shenjiKinds[kindIndex])) list.Add(shenjiKinds[kindIndex]);
                }
            }
        }
    }
}
