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

    public sealed class CompYunjianziShenji : ThingComp
    {
        private int ownerThingId = -1;
        private float shield;
        private int shieldBrokenUntil;
        private int rocketJumpReadyTick;
        private int recallRepairReadyTick;
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
        public bool IsBoundTo(Pawn owner) { return owner != null && ownerThingId == owner.thingIDNumber; }
        public float ShieldMax
        {
            get
            {
                CompYunjianziCultivation c = Cultivation;
                if (c == null || !c.HasSkill(YunjianziSkills.ShenjiShield)) return 0f;
                return c.HasSkill(YunjianziSkills.ShenjiDarkShield) ? 300f : 100f;
            }
        }

        public bool BindTo(Pawn owner)
        {
            ownerThingId = owner == null ? -1 : owner.thingIDNumber;
            ownerCache = owner;
            boundTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            shield = ShieldMax;
            SynchronizeTier(true);
            nextNativeControlAttemptTick = 0;
            return EnsureNativeControlRelation();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 新生成体会在 BindTo 中按当前境界定型；读档只补齐缺失装备，绝不自动升级旧型号。
            SynchronizeTier(!respawningAfterLoad);
            // 旧版把“监督关系存在”误当成“已经进入控制组”。读档时重新核对一次，
            // 成功后 nativeControlBound 会再次锁定，不会成为持续轮询负担。
            if (respawningAfterLoad)
            {
                nativeControlBound = false;
                nextNativeControlAttemptTick = 0;
            }
            EnsureNativeControlRelation();
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
                if (pawn.needs != null && pawn.needs.energy != null) pawn.needs.energy.CurLevelPercentage = 1f;
                if (Find.TickManager.TicksGame >= shieldBrokenUntil && ShieldMax > 0f)
                    shield = Mathf.Min(ShieldMax, shield + ShieldMax / 120f);
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
                    throw new InvalidOperationException("原版控制组未接受神机米莉安。绑定已回滚。");
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
                CompYunjianziCultivation c = Cultivation;
                if (c == null) return 1;
                if (c.Realm >= 5) return 3;
                if (c.Realm >= 3) return 2;
                return 1;
            }
        }

        private ThingDef DesiredWeapon
        {
            get
            {
                CompYunjianziCultivation c = Cultivation;
                if (c != null && c.HasSkill(YunjianziSkills.ShenjiLegendaryFirepower)) return YJZDefOf.YJZ_Weapon_Shenji_Legendary;
                if (c != null && c.HasSkill(YunjianziSkills.ShenjiGauss)) return YJZDefOf.YJZ_Weapon_Shenji_Gauss;
                if (c != null && c.HasSkill(YunjianziSkills.ShenjiGatling)) return YJZDefOf.YJZ_Weapon_Shenji_Gatling;
                return YJZDefOf.YJZ_Weapon_Shenji_Assault;
            }
        }

        private void SynchronizeTier(bool force)
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.health == null) return;
            int currentTier = ArmorTier;
            int tier = force || lastArmorTier <= 0 ? currentTier : Mathf.Clamp(lastArmorTier, 1, 3);
            // 旧存档可能仍带有早期的固有装甲 Hediff；属性已迁移到可见的专属神机甲，必须清掉以免双算。
            List<Hediff> oldArmor = pawn.health.hediffSet.hediffs.Where(h => h.def == YJZDefOf.YJZ_ShenjiArmor_Base || h.def == YJZDefOf.YJZ_ShenjiArmor_Intermediate || h.def == YJZDefOf.YJZ_ShenjiArmor_Final).ToList();
            for (int i = 0; i < oldArmor.Count; i++) pawn.health.RemoveHediff(oldArmor[i]);

            if (pawn.apparel != null)
            {
                // Native Milian kinds are generated with Milian_Lining under
                // their middle armour. Shenji uses the same real layer, always
                // made from hyperweave. It is issued once at initialization;
                // later tier changes preserve this exact lining.
                bool firstInitialization = force && lastArmorTier <= 0;
                bool hasLining = pawn.apparel.WornApparel
                    .Any(a => a.def == YJZDefOf.Milian_Lining);
                if (firstInitialization && !hasLining)
                {
                    Apparel lining = ThingMaker.MakeThing(YJZDefOf.Milian_Lining, YJZDefOf.Hyperweave) as Apparel;
                    if (lining != null)
                    {
                        CompQuality quality = lining.TryGetComp<CompQuality>();
                        if (quality != null)
                            quality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                        pawn.apparel.Wear(lining, false);
                    }
                }

                ThingDef desiredArmor = tier >= 3 ? YJZDefOf.YJZ_Apparel_ShenjiArmor_Final :
                    tier >= 2 ? YJZDefOf.YJZ_Apparel_ShenjiArmor_Intermediate : YJZDefOf.YJZ_Apparel_ShenjiArmor_Base;
                List<Apparel> obsoleteIntrinsicArmor = pawn.apparel.WornApparel
                    .Where(a => force && YunjianziShenjiApparelPolicy.IsIntrinsicArmor(a.def) && a.def != desiredArmor).ToList();
                for (int i = 0; i < obsoleteIntrinsicArmor.Count; i++)
                {
                    Apparel obsolete = obsoleteIntrinsicArmor[i];
                    pawn.apparel.Remove(obsolete);
                    obsolete.Destroy(DestroyMode.Vanish);
                }
                bool hasIntrinsicArmor = pawn.apparel.WornApparel.Any(a => YunjianziShenjiApparelPolicy.IsIntrinsicArmor(a.def));
                if (!hasIntrinsicArmor)
                {
                    Apparel armor = ThingMaker.MakeThing(desiredArmor) as Apparel;
                    if (armor != null)
                    {
                        CompQuality quality = armor.TryGetComp<CompQuality>();
                        if (quality != null) quality.SetQuality(tier >= 2 ? QualityCategory.Legendary : QualityCategory.Normal, ArtGenerationContext.Outsider);
                        pawn.apparel.Wear(armor, false);
                    }
                }
                // Match Milira Guard: hair contains no goggle; the visor is native
                // Milian Overhead apparel with south/east assets, engine-mirrored
                // west, and a transparent north view.
                bool hasVisor = pawn.apparel.WornApparel
                    .Any(a => a.def == YJZDefOf.YJZ_Apparel_ShenjiVisor);
                if (!hasVisor)
                {
                    Apparel visor = ThingMaker.MakeThing(YJZDefOf.YJZ_Apparel_ShenjiVisor) as Apparel;
                    if (visor != null)
                    {
                        CompQuality quality = visor.TryGetComp<CompQuality>();
                        if (quality != null)
                            quality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                        pawn.apparel.Wear(visor, false);
                    }
                }
                if (force || lastArmorTier <= 0) lastArmorTier = tier;
            }
            if (pawn.equipment != null && (pawn.equipment.Primary == null || (force && pawn.equipment.Primary.def != DesiredWeapon)))
            {
                if (pawn.equipment.Primary != null) pawn.equipment.Remove(pawn.equipment.Primary);
                ThingWithComps weapon = ThingMaker.MakeThing(DesiredWeapon) as ThingWithComps;
                if (weapon != null)
                {
                    CompQuality quality = weapon.TryGetComp<CompQuality>();
                    if (quality != null) quality.SetQuality(DesiredWeapon == YJZDefOf.YJZ_Weapon_Shenji_Legendary ? QualityCategory.Legendary : QualityCategory.Normal, ArtGenerationContext.Outsider);
                    pawn.equipment.AddEquipment(weapon);
                }
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

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!dinfo.Def.harmsHealth || shield <= 0f || ShieldMax <= 0f) return;
            float cost = Cultivation != null && Cultivation.HasSkill(YunjianziSkills.ShenjiDarkShield)
                ? (dinfo.Def.isRanged ? 10f : 50f)
                : dinfo.Amount;
            if (shield >= cost)
            {
                shield -= cost;
                absorbed = true;
            }
            else
            {
                float ratio = Mathf.Clamp01(shield / Mathf.Max(1f, cost));
                dinfo.SetAmount(dinfo.Amount * (1f - ratio));
                shield = 0f;
            }
            if (shield <= 0f) shieldBrokenUntil = Find.TickManager.TicksGame + 1200;
            if (Pawn != null && Pawn.Spawned) FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.PsycastAreaEffect, .45f);
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
        private int ShenjiLimit
        {
            get
            {
                if (HasSkill(YunjianziSkills.ShenjiQuadControl)) return 4;
                if (HasSkill(YunjianziSkills.ShenjiDualControl)) return 2;
                return 1;
            }
        }

        private IEnumerable<Gizmo> GetShenjiGizmos()
        {
            Command_Action summon = new Command_Action
            {
                defaultLabel = "神机诀·唤神机",
                defaultDesc = "消耗10点灵力，召唤一台专属远程火力支援型米莉安。神机不占机械带宽，数量上限由神机诀节点决定。",
                icon = YunjianziAbilityTextures.SummonShenji,
                action = SummonShenji
            };
            int count = BoundShenji().Count;
            if (count >= ShenjiLimit)
                summon.defaultDesc += string.Format("\n\n当前神机：{0}/{1}。再次召唤将覆盖最早召唤的一台。", count, ShenjiLimit);
            if (Energy < 10f) summon.Disable("灵力不足：需要10点。");
            yield return summon;
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

        private void SummonShenji()
        {
            if (Pawn == null || !Pawn.Spawned || !TrySpend(10f)) return;
            List<Pawn> bound = BoundShenji();
            Pawn shenji = PawnGenerator.GeneratePawn(YJZDefOf.YJZ_Shenji, Faction.OfPlayer);
            int removeCount = Math.Max(0, bound.Count - ShenjiLimit + 1);
            foreach (Pawn old in bound.OrderBy(p =>
            {
                CompYunjianziShenji c = p.TryGetComp<CompYunjianziShenji>();
                return c == null ? 0 : c.BoundTick;
            }).ThenBy(p => p.thingIDNumber).Take(removeCount).ToList())
            {
                if (old.Spawned) FleckMaker.Static(old.Position, old.Map, FleckDefOf.PsycastSkipFlashEntry, 1.2f);
                old.Destroy(DestroyMode.Vanish);
            }
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 3);
            if (!cell.IsValid) cell = Pawn.Position;
            GenSpawn.Spawn(shenji, cell, Pawn.Map);
            CompYunjianziShenji comp = shenji.TryGetComp<CompYunjianziShenji>();
            if (comp == null || !comp.BindTo(Pawn))
            {
                if (shenji.Spawned) shenji.Destroy(DestroyMode.Vanish);
                Energy += 10f;
                Messages.Message("神机控制组初始化失败，本次召唤已取消并返还灵力。请查看日志中的首条 [Yunjianzi] 错误。",
                    Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            // 使用原版机械师/米莉安共用的监督关系，建立征召、控制组与工作模式。
            // 神机 BandwidthCost 为 0，不会挤占普通机械体的带宽。
            FleckMaker.Static(cell, Pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1.5f);
            Messages.Message("神机应召而至。", shenji, MessageTypeDefOf.PositiveEvent, false);
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
            PawnKindDef shenji = DefDatabase<PawnKindDef>.GetNamedSilentFail("YJZ_Shenji");
            if (shenji == null) return;
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
                    if (list != null && !list.Contains(shenji)) list.Add(shenji);
                }
            }
        }
    }
}
