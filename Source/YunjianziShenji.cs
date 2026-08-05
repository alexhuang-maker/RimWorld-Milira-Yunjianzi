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
        private int rocketDroneReadyTick;
        private int recallRepairReadyTick;
        private int lastArmorTier;

        public Pawn Pawn { get { return parent as Pawn; } }
        public Pawn Owner
        {
            get
            {
                if (ownerThingId < 0) return null;
                foreach (Map map in Find.Maps)
                {
                    Pawn found = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == ownerThingId);
                    if (found != null) return found;
                }
                return null;
            }
        }
        public CompYunjianziCultivation Cultivation { get { Pawn owner = Owner; return owner == null ? null : owner.TryGetComp<CompYunjianziCultivation>(); } }
        public float ShieldMax
        {
            get
            {
                CompYunjianziCultivation c = Cultivation;
                if (c == null || !c.HasSkill(YunjianziSkills.ShenjiShield)) return 0f;
                return c.HasSkill(YunjianziSkills.ShenjiDarkShield) ? 300f : 100f;
            }
        }

        public void BindTo(Pawn owner)
        {
            ownerThingId = owner == null ? -1 : owner.thingIDNumber;
            shield = ShieldMax;
            SynchronizeTier(true);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SynchronizeTier(true);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead) return;
            if (pawn.IsHashIntervalTick(60))
            {
                SynchronizeTier(false);
                if (pawn.needs != null && pawn.needs.energy != null) pawn.needs.energy.CurLevelPercentage = 1f;
                if (Find.TickManager.TicksGame >= shieldBrokenUntil && ShieldMax > 0f)
                    shield = Mathf.Min(ShieldMax, shield + ShieldMax / 120f);
                TryAutomaticRocketJump();
            }
            if (pawn.IsHashIntervalTick(90)) TryRocketDroneAttack();
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
            int tier = ArmorTier;
            if (force || tier != lastArmorTier)
            {
                List<Hediff> oldArmor = pawn.health.hediffSet.hediffs.Where(h => h.def == YJZDefOf.YJZ_ShenjiArmor_Base || h.def == YJZDefOf.YJZ_ShenjiArmor_Intermediate || h.def == YJZDefOf.YJZ_ShenjiArmor_Final).ToList();
                for (int i = 0; i < oldArmor.Count; i++) pawn.health.RemoveHediff(oldArmor[i]);
                HediffDef armor = tier == 3 ? YJZDefOf.YJZ_ShenjiArmor_Final : tier == 2 ? YJZDefOf.YJZ_ShenjiArmor_Intermediate : YJZDefOf.YJZ_ShenjiArmor_Base;
                pawn.health.AddHediff(armor);
                lastArmorTier = tier;
            }
            if (pawn.equipment != null && (pawn.equipment.Primary == null || pawn.equipment.Primary.def != DesiredWeapon))
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
                    .Where(a => !accessoriesUnlocked || !YunjianziShenjiApparelPolicy.IsAllowedAccessory(a.def))
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

        private void TryRocketDroneAttack()
        {
            CompYunjianziCultivation c = Cultivation;
            Pawn pawn = Pawn;
            if (c == null || !c.HasSkill(YunjianziSkills.ShenjiRocketDrone) || pawn == null || !pawn.Spawned || pawn.Downed || Find.TickManager.TicksGame < rocketDroneReadyTick) return;
            Thing target = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget), PathEndMode.Touch,
                TraverseParms.For(pawn), 35.9f, t => t != pawn && t.HostileTo(pawn) && (!(t is Pawn) || !((Pawn)t).Dead));
            if (target == null) return;
            GenExplosion.DoExplosion(target.Position, pawn.Map, 1.9f, DamageDefOf.Bomb, pawn, 18, .25f, SoundDefOf.BulletImpact_Ground);
            rocketDroneReadyTick = Find.TickManager.TicksGame + 600;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ownerThingId, "yjz_shenjiOwner", -1);
            Scribe_Values.Look(ref shield, "yjz_shenjiShield", 0f);
            Scribe_Values.Look(ref shieldBrokenUntil, "yjz_shenjiShieldBrokenUntil", 0);
            Scribe_Values.Look(ref rocketJumpReadyTick, "yjz_shenjiRocketJumpReady", 0);
            Scribe_Values.Look(ref rocketDroneReadyTick, "yjz_shenjiRocketDroneReady", 0);
            Scribe_Values.Look(ref recallRepairReadyTick, "yjz_shenjiRecallRepairReady", 0);
            Scribe_Values.Look(ref lastArmorTier, "yjz_shenjiArmorTier", 0);
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
        // 固有 Hediff 已提供装甲。配件节点只开放工具层，禁止实体装甲再次叠加。
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
            int count = BoundShenji().Count();
            if (count >= ShenjiLimit) summon.Disable(string.Format("神机数量已达上限：{0}/{1}", count, ShenjiLimit));
            else if (Energy < 10f) summon.Disable("灵力不足：需要10点。");
            yield return summon;
        }

        private IEnumerable<Pawn> BoundShenji()
        {
            if (Pawn == null) yield break;
            foreach (Map map in Find.Maps)
                foreach (Pawn candidate in map.mapPawns.AllPawns)
                {
                    CompYunjianziShenji comp = candidate.TryGetComp<CompYunjianziShenji>();
                    if (comp != null && comp.Owner == Pawn && !candidate.Dead) yield return candidate;
                }
        }

        private void SummonShenji()
        {
            if (Pawn == null || !Pawn.Spawned || BoundShenji().Count() >= ShenjiLimit || !TrySpend(10f)) return;
            Pawn shenji = PawnGenerator.GeneratePawn(YJZDefOf.YJZ_Shenji, Faction.OfPlayer);
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 3);
            if (!cell.IsValid) cell = Pawn.Position;
            GenSpawn.Spawn(shenji, cell, Pawn.Map);
            CompYunjianziShenji comp = shenji.TryGetComp<CompYunjianziShenji>();
            if (comp != null) comp.BindTo(Pawn);
            FleckMaker.Static(cell, Pawn.Map, FleckDefOf.PsycastSkipOuterRingExit, 1.5f);
            Messages.Message("神机应召而至。", shenji, MessageTypeDefOf.PositiveEvent, false);
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
