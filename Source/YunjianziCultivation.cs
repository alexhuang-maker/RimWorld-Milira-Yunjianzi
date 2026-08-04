using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class YunjianziAbilityTextures
    {
        public static readonly Texture2D SummonZhuxie = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SummonZhuxie");
        public static readonly Texture2D BlinkStrike = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/BlinkStrike");
        public static readonly Texture2D ImmortalArray = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/ImmortalArray");
        public static readonly Texture2D SwordRain =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SwordRain", false) ?? BlinkStrike;
    }

    public sealed class CompProperties_YunjianziCultivation : CompProperties
    {
        public CompProperties_YunjianziCultivation() { compClass = typeof(CompYunjianziCultivation); }
    }

    public sealed partial class CompYunjianziCultivation : ThingComp
    {
        private static readonly float[] EnergyCaps = { 0f, 100f, 200f, 500f, 1000f, 2000f };
        private static readonly float[] ShieldCaps = { 0f, 0f, 70f, 140f, 280f, 560f };
        private float energy = -1f;
        private float shield = -1f;
        private int lastDamageTick = -99999;
        private int blinkReadyTick;
        private int swordRainReadyTick;
        private int shadowCursor;
        private int deathLockUntil;
        private int deathLockReadyTick;
        private List<int> shadowLightningReady = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };

        public Pawn Pawn { get { return parent as Pawn; } }
        public bool IsYunjianzi
        {
            get
            {
                Pawn pawn = Pawn;
                return pawn != null && ((pawn.kindDef != null && pawn.kindDef.defName == "YJZ_Yunjianzi") ||
                    (pawn.health != null && pawn.health.hediffSet.HasHediff(YJZDefOf.YJZ_Identity_Yunjianzi)));
            }
        }
        public Hediff RealmHediff { get { return Pawn == null || Pawn.health == null ? null : Pawn.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_CultivationRealm); } }
        public int Realm { get { Hediff h = RealmHediff; return Mathf.Clamp(Mathf.FloorToInt(h == null ? 0f : h.Severity), 0, 5); } }
        public string RealmLabel { get { return new[] { "未入道", "练气", "筑基", "紫府", "金丹", "化神" }[Realm]; } }
        public float EnergyMax { get { return EnergyCaps[Realm]; } }
        public float ShieldMax { get { return ShieldCaps[Realm]; } }
        public float Energy { get { return Mathf.Clamp(energy, 0f, EnergyMax); } set { energy = Mathf.Clamp(value, 0f, EnergyMax); } }
        public float Shield { get { return Mathf.Clamp(shield, 0f, ShieldMax); } set { shield = Mathf.Clamp(value, 0f, ShieldMax); } }
        public int ShadowCount { get { return Realm < 3 ? 0 : Realm == 3 ? 2 : Realm == 4 ? 4 : 8; } }
        public float SwordDamage { get { return new[] { 0f, 25f, 29f, 38f, 48f, 62f }[Realm]; } }
        public float SwordArmorPenetration { get { return Realm >= 5 ? 10f : new[] { 0f, .9f, .95f, 1.1f, 1.35f, 10f }[Realm]; } }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!IsYunjianzi) return;
            EnsureRealmAndAbilities();
            if (energy < 0f) energy = EnergyMax;
            if (shield < 0f) shield = ShieldMax;
        }

        private void EnsureRealmAndAbilities()
        {
            EnsureIdentityStory();
            if (RealmHediff == null)
            {
                Hediff realm = HediffMaker.MakeHediff(YJZDefOf.YJZ_CultivationRealm, Pawn);
                realm.Severity = 1f;
                Pawn.health.AddHediff(realm);
            }
        }

        private void EnsureIdentityStory()
        {
            if (Pawn == null || Pawn.story == null) return;
            if (Pawn.story.Childhood != YJZDefOf.YJZ_Backstory_Childhood)
                Pawn.story.Childhood = YJZDefOf.YJZ_Backstory_Childhood;
            if (Pawn.story.Adulthood != YJZDefOf.YJZ_Backstory_Adulthood)
                Pawn.story.Adulthood = YJZDefOf.YJZ_Backstory_Adulthood;
            if (Pawn.story.traits != null && !Pawn.story.traits.HasTrait(YJZDefOf.YJZ_Trait_CloudseaSwordHeart))
                Pawn.story.traits.GainTrait(new Trait(YJZDefOf.YJZ_Trait_CloudseaSwordHeart, 0), true);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!IsYunjianzi || Pawn == null || Pawn.Dead) return;
            TickArraySystem();
            if (Pawn.IsHashIntervalTick(60))
            {
                EnsureRealmAndAbilities();
                UpdateSwordHeartClarity();
                Energy += EnergyMax * 0.005f;
                if (Realm >= 5 && Pawn.needs != null)
                {
                    if (Pawn.needs.food != null) Pawn.needs.food.CurLevelPercentage = 1f;
                    if (Pawn.needs.rest != null) Pawn.needs.rest.CurLevelPercentage = 1f;
                }
                if (Realm >= 2 && Find.TickManager.TicksGame > lastDamageTick + 1200)
                    Shield += Mathf.Max(1f, ShieldMax / 120f);
            }
            if (Realm >= 3 && Pawn.Spawned && !Pawn.Downed && Pawn.IsHashIntervalTick(30))
                TryShadowStrike();
        }

        public bool TrySpend(float amount, bool message = true)
        {
            if (Energy + .001f < amount)
            {
                if (message) Messages.Message(string.Format("灵力不足：需要 {0:0}，当前 {1:0.0}。", amount, Energy), Pawn, MessageTypeDefOf.RejectInput);
                return false;
            }
            Energy -= amount;
            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (!IsYunjianzi || Realm <= 0 || Pawn == null || Pawn.Dead) yield break;
            if (Find.Selector.SingleSelectedThing == Pawn)
                yield return new GizmoYunjianziCultivation { comp = this };
            if (Pawn.Faction == Faction.OfPlayer || DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "御剑诀·唤剑",
                    defaultDesc = "消耗10点灵力。召回或生成世界中唯一的诛邪剑并装备。",
                    icon = YunjianziAbilityTextures.SummonZhuxie,
                    action = SummonZhuxie
                };
                if (Realm >= 3)
                {
                    Command_Action blink = new Command_Action
                    {
                        defaultLabel = "御剑诀·瞬斩",
                        defaultDesc = "消耗20点灵力，闪现至射程内敌人身后并强力斩击。冷却5秒。",
                        icon = YunjianziAbilityTextures.BlinkStrike,
                        action = BeginBlinkTargeting
                    };
                    int remaining = blinkReadyTick - Find.TickManager.TicksGame;
                    if (remaining > 0) blink.Disable(string.Format("冷却中：{0:0.0}秒", remaining / 60f));
                    yield return blink;

                    Command_Action swordRain = new Command_Action
                    {
                        defaultLabel = "御剑诀·天剑雨",
                        defaultDesc = "紫府境解锁。消耗80点灵力，在目标区域降下两轮诛邪剑雨。每轮覆盖约迫击炮弹爆炸范围，只造成中等纯粹伤害，不附带眩晕、落雷或击退。冷却30秒。",
                        icon = YunjianziAbilityTextures.SwordRain,
                        action = BeginSwordRainTargeting
                    };
                    int swordRainRemaining = swordRainReadyTick - Find.TickManager.TicksGame;
                    if (swordRainRemaining > 0)
                        swordRain.Disable(string.Format("冷却中：{0:0.0}秒", swordRainRemaining / 60f));
                    else if (Energy < 80f)
                        swordRain.Disable("灵力不足：需要80点。");
                    yield return swordRain;
                }
                foreach (Gizmo arrayGizmo in GetArrayGizmos()) yield return arrayGizmo;
            }
        }

        private void SummonZhuxie()
        {
            if (Pawn.equipment != null && Pawn.equipment.Primary != null && Pawn.equipment.Primary.def == YJZDefOf.YJZ_Weapon_Zhuxie)
            {
                Messages.Message("诛邪已在云间子手中。", Pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }
            if (!TrySpend(10f)) return;
            ThingWithComps sword = FindExistingZhuxie();
            if (sword == null) sword = (ThingWithComps)ThingMaker.MakeThing(YJZDefOf.YJZ_Weapon_Zhuxie);
            if (sword.Spawned) sword.DeSpawn();
            Pawn previousOwner = Find.Maps.SelectMany(m => m.mapPawns.AllPawns).FirstOrDefault(p => p.equipment != null && p.equipment.Primary == sword);
            if (previousOwner != null && previousOwner.equipment != null) previousOwner.equipment.Remove(sword);
            if (Pawn.equipment.Primary != null)
            {
                ThingWithComps dropped;
                Pawn.equipment.TryDropEquipment(Pawn.equipment.Primary, out dropped, Pawn.Position, false);
            }
            Pawn.equipment.AddEquipment(sword);
            RemoveDuplicateZhuxie(sword);
            Messages.Message("诛邪，应念而来。", Pawn, MessageTypeDefOf.PositiveEvent);
        }

        private void BeginSwordRainTargeting()
        {
            if (Realm < 3 || Find.TickManager.TicksGame < swordRainReadyTick || Pawn == null || !Pawn.Spawned) return;
            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetItems = false,
                validator = target => target.IsValid && target.Cell.InBounds(Pawn.Map) &&
                    target.Cell.DistanceTo(Pawn.Position) <= 35.9f
            };
            Find.Targeter.BeginTargeting(parms, target => TryCastSwordRain(target.Cell));
        }

        private void TryCastSwordRain(IntVec3 center)
        {
            if (Realm < 3 || Pawn == null || !Pawn.Spawned || !center.InBounds(Pawn.Map) ||
                center.DistanceTo(Pawn.Position) > 35.9f)
            {
                Messages.Message("天剑雨目标必须位于35.9格内。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            if (!TrySpend(80f)) return;
            ThingYunjianziSwordRain rain = (ThingYunjianziSwordRain)ThingMaker.MakeThing(YJZDefOf.YJZ_Thing_SwordRain);
            rain.Initialize(Pawn, Realm);
            GenSpawn.Spawn(rain, center, Pawn.Map);
            swordRainReadyTick = Find.TickManager.TicksGame + 1800;
        }

        private ThingWithComps FindExistingZhuxie()
        {
            foreach (Map map in Find.Maps)
            {
                ThingWithComps loose = map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Weapon_Zhuxie).OfType<ThingWithComps>().FirstOrDefault();
                if (loose != null) return loose;
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    ThingWithComps held = ZhuxieOwnedBy(pawn);
                    if (held != null) return held;
                }
            }
            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                ThingWithComps held = ZhuxieOwnedBy(pawn);
                if (held != null) return held;
            }
            return null;
        }

        private static ThingWithComps ZhuxieOwnedBy(Pawn pawn)
        {
            if (pawn == null) return null;
            ThingWithComps primary = pawn.equipment == null ? null : pawn.equipment.Primary;
            if (primary != null && primary.def == YJZDefOf.YJZ_Weapon_Zhuxie) return primary;
            if (pawn.inventory != null)
                return pawn.inventory.innerContainer.OfType<ThingWithComps>().FirstOrDefault(t => t.def == YJZDefOf.YJZ_Weapon_Zhuxie);
            return null;
        }

        private void RemoveDuplicateZhuxie(ThingWithComps keep)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Weapon_Zhuxie).ToList())
                    if (thing != keep) thing.Destroy(DestroyMode.Vanish);
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    ThingWithComps weapon = pawn.equipment == null ? null : pawn.equipment.Primary;
                    if (weapon != null && weapon != keep && weapon.def == YJZDefOf.YJZ_Weapon_Zhuxie)
                    {
                        pawn.equipment.Remove(weapon);
                        weapon.Destroy(DestroyMode.Vanish);
                    }
                    if (pawn.inventory != null)
                    {
                        foreach (ThingWithComps carried in pawn.inventory.innerContainer.OfType<ThingWithComps>().Where(t => t != keep && t.def == YJZDefOf.YJZ_Weapon_Zhuxie).ToList())
                        {
                            pawn.inventory.innerContainer.Remove(carried);
                            carried.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                ThingWithComps weapon = pawn.equipment == null ? null : pawn.equipment.Primary;
                if (weapon != null && weapon != keep && weapon.def == YJZDefOf.YJZ_Weapon_Zhuxie)
                {
                    pawn.equipment.Remove(weapon);
                    weapon.Destroy(DestroyMode.Vanish);
                }
                if (pawn.inventory != null)
                {
                    foreach (ThingWithComps carried in pawn.inventory.innerContainer.OfType<ThingWithComps>().Where(t => t != keep && t.def == YJZDefOf.YJZ_Weapon_Zhuxie).ToList())
                    {
                        pawn.inventory.innerContainer.Remove(carried);
                        carried.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private void BeginBlinkTargeting()
        {
            if (Find.TickManager.TicksGame < blinkReadyTick) return;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetLocations = false;
            parms.canTargetPawns = true;
            parms.canTargetHumans = true;
            parms.canTargetMechs = true;
            parms.canTargetAnimals = true;
            parms.canTargetBuildings = true;
            Find.Targeter.BeginTargeting(parms, target => TryBlink(target.Thing, true));
        }

        private bool IsValidCombatTarget(Thing target, float range)
        {
            if (target == null || !target.Spawned || target.Destroyed || !target.HostileTo(Pawn) || target.Map != Pawn.Map) return false;
            Pawn targetPawn = target as Pawn;
            if (targetPawn != null && targetPawn.Dead) return false;
            if (!(target is Pawn) && !(target is Building_Turret)) return false;
            return Pawn.Position.DistanceTo(target.Position) <= range;
        }

        private IEnumerable<Thing> CombatTargetsInRange(float range)
        {
            if (Pawn == null || Pawn.Map == null) yield break;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(Pawn.Position, Pawn.Map, range, true))
                if (thing != Pawn && IsValidCombatTarget(thing, range)) yield return thing;
        }

        private bool TryBlink(Thing target, bool initial)
        {
            if (!IsValidCombatTarget(target, 25.9f))
            {
                if (initial) Messages.Message("目标必须是25.9格内的敌对作战单位或炮塔。", Pawn, MessageTypeDefOf.RejectInput);
                return false;
            }
            if (!TrySpend(20f, initial)) return false;
            Map map = Pawn.Map;
            Vector3 originDrawPos = Pawn.DrawPos;
            Vector3 targetDrawPos = target.DrawPos;
            IntVec3 away = target.Position - Pawn.Position;
            IntVec3 desired = target.Position + new IntVec3(Math.Sign(away.x), 0, Math.Sign(away.z));
            IntVec3 destination;
            if (!CellFinder.TryFindRandomCellNear(target.Position, Pawn.Map, 2, c => c.Standable(Pawn.Map) && !c.Fogged(Pawn.Map), out destination))
                destination = desired.InBounds(Pawn.Map) && desired.Standable(Pawn.Map) ? desired : Pawn.Position;
            Pawn.pather.StopDead();
            Pawn.Position = destination;
            Pawn.rotationTracker.FaceTarget(new LocalTargetInfo(target));
            DamageInfo info = new DamageInfo(DamageDefOf.Cut, SwordDamage * 1.6f, SwordArmorPenetration, -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(info);
            if (Realm >= 4 && !target.Destroyed) ApplyLightning(target, SwordDamage * .45f);
            blinkReadyTick = Find.TickManager.TicksGame + 300;
            YunjianziCombatEffects.ShowBlinkSlash(originDrawPos, targetDrawPos, Pawn.DrawPos, map);
            if (Realm >= 4 && (target.Destroyed || (target is Pawn && ((Pawn)target).Dead)) && Energy >= 20f)
            {
                Thing next = CombatTargetsInRange(25.9f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
                if (next != null) TryBlink(next, false);
            }
            return true;
        }

        private void TryShadowStrike()
        {
            if (ShadowCount <= 0) return;
            int shadowIndex = shadowCursor % ShadowCount;
            shadowCursor++;
            ThingYunjianziSwordShadow existing = Pawn.Map.listerThings.ThingsOfDef(YJZDefOf.YJZ_Projectile_SwordShadow)
                .OfType<ThingYunjianziSwordShadow>()
                .FirstOrDefault(s => s.Owner == Pawn && s.ShadowIndex == shadowIndex);
            if (existing != null) return;
            Thing target = CombatTargetsInRange(25.9f)
                .Where(t => GenSight.LineOfSight(Pawn.Position, t.Position, Pawn.Map))
                .OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) return;
            ThingYunjianziSwordShadow shadow = (ThingYunjianziSwordShadow)ThingMaker.MakeThing(YJZDefOf.YJZ_Projectile_SwordShadow);
            shadow.Initialize(Pawn, shadowIndex, target);
            GenSpawn.Spawn(shadow, Pawn.Position, Pawn.Map);
        }

        public void OnShadowProjectileHit(Thing target, int shadowIndex)
        {
            if (target == null || target.Destroyed || (target is Pawn && ((Pawn)target).Dead)) return;
            DamageInfo info = new DamageInfo(DamageDefOf.Cut, SwordDamage * .7f, Realm >= 5 ? 10f : SwordArmorPenetration * .7f, -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(info);
            if (Realm >= 5 && !target.Destroyed && (!(target is Pawn) || !((Pawn)target).Dead))
            {
                Pawn pawnTarget = target as Pawn;
                if (pawnTarget != null) ApplyControl(pawnTarget);
                while (shadowLightningReady.Count < 8) shadowLightningReady.Add(0);
                if (Find.TickManager.TicksGame >= shadowLightningReady[shadowIndex])
                {
                    ApplyLightning(target, SwordDamage * .45f);
                    shadowLightningReady[shadowIndex] = Find.TickManager.TicksGame + 600;
                }
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!IsYunjianzi || Pawn == null || Pawn.Dead) return;
            int now = Find.TickManager.TicksGame;
            if (Realm >= 4 && HoldingZhuxie && (now < deathLockUntil || (Pawn.health.summaryHealth.SummaryHealthPercent < .25f && now >= deathLockReadyTick)))
            {
                if (now >= deathLockUntil)
                {
                    deathLockUntil = now + 1200;
                    deathLockReadyTick = now + 60000;
                    Messages.Message("诛邪护主：云间子进入20秒锁血状态。", Pawn, MessageTypeDefOf.PositiveEvent);
                }
                absorbed = true;
                return;
            }
            if (Realm >= 2 && Shield > 0f && dinfo.Def.harmsHealth)
            {
                lastDamageTick = now;
                float amount = dinfo.Amount;
                if (Shield >= amount)
                {
                    Shield -= amount;
                    absorbed = true;
                    FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.PsycastAreaEffect, .35f);
                }
                else
                {
                    dinfo.SetAmount(amount - Shield);
                    Shield = 0f;
                }
            }
        }

        public bool HoldingZhuxie { get { return Pawn != null && Pawn.equipment != null && Pawn.equipment.Primary != null && Pawn.equipment.Primary.def == YJZDefOf.YJZ_Weapon_Zhuxie; } }

        public void OnZhuxieHit(Pawn target)
        {
            if (target == null || target.Dead) return;
            if (Realm >= 2) ApplyControl(target);
            if (Realm >= 4)
            {
                ApplyLightning(target, SwordDamage * .45f);
                HealFromStrike();
            }
        }

        private void ApplyControl(Pawn target)
        {
            if (target == null || target.Dead || target.health.hediffSet.HasHediff(YJZDefOf.YJZ_SwordControlImmunity)) return;
            if (target.stances != null && target.stances.stunner != null)
                target.stances.stunner.StunFor(target.RaceProps.IsMechanoid ? 75 : 30, Pawn);
            Hediff immune = HediffMaker.MakeHediff(YJZDefOf.YJZ_SwordControlImmunity, target);
            target.health.AddHediff(immune);
        }

        private void ApplyLightning(Thing target, float amount)
        {
            DamageInfo lightning = new DamageInfo(DamageDefOf.Burn, amount, Realm >= 5 ? 10f : .65f, -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(lightning);
            YunjianziCombatEffects.ShowLightning(target);
        }

        private void UpdateSwordHeartClarity()
        {
            if (Pawn.health == null) return;
            Hediff clarity = Pawn.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_SwordHeartClarity);
            bool shouldHave = HoldingZhuxie && Pawn.story != null && Pawn.story.traits != null &&
                Pawn.story.traits.HasTrait(YJZDefOf.YJZ_Trait_CloudseaSwordHeart);
            if (shouldHave && clarity == null)
                Pawn.health.AddHediff(HediffMaker.MakeHediff(YJZDefOf.YJZ_SwordHeartClarity, Pawn));
            else if (!shouldHave && clarity != null)
                Pawn.health.RemoveHediff(clarity);
        }

        private void HealFromStrike()
        {
            Hediff_Injury injury = Pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(h => h.Severity > 0f).OrderByDescending(h => h.BleedRate).ThenByDescending(h => h.Severity).FirstOrDefault();
            if (injury != null) injury.Heal(Mathf.Min(3f, injury.Severity));
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref energy, "yjz_energy", -1f);
            Scribe_Values.Look(ref shield, "yjz_shield", -1f);
            Scribe_Values.Look(ref lastDamageTick, "yjz_lastDamageTick", -99999);
            Scribe_Values.Look(ref blinkReadyTick, "yjz_blinkReadyTick", 0);
            Scribe_Values.Look(ref swordRainReadyTick, "yjz_swordRainReadyTick", 0);
            Scribe_Values.Look(ref deathLockUntil, "yjz_deathLockUntil", 0);
            Scribe_Values.Look(ref deathLockReadyTick, "yjz_deathLockReadyTick", 0);
            Scribe_Collections.Look(ref shadowLightningReady, "yjz_shadowLightningReady", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && shadowLightningReady == null)
                shadowLightningReady = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };
            ExposeArrayData();
        }
    }

    [StaticConstructorOnStartup]
    public sealed class GizmoYunjianziCultivation : Gizmo
    {
        public CompYunjianziCultivation comp;
        private static readonly Texture2D Panel = ContentFinder<Texture2D>.Get("Yunjianzi/UI/CultivationPanel", true);
        private static readonly Texture2D[] RealmEmblems =
        {
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Realms/Realm_1", true),
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Realms/Realm_2", true),
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Realms/Realm_3", true),
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Realms/Realm_4", true),
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Realms/Realm_5", true)
        };
        private static readonly Texture2D EnergyFill = SolidColorMaterials.NewSolidColorTexture(new Color(.24f, .88f, .91f));
        private static readonly Texture2D EnergyLowFill = SolidColorMaterials.NewSolidColorTexture(new Color(.86f, .65f, .26f));
        private static readonly Texture2D ShieldFill = SolidColorMaterials.NewSolidColorTexture(new Color(.44f, .66f, 1f));
        private static readonly Texture2D Empty = SolidColorMaterials.NewSolidColorTexture(new Color(.025f, .09f, .105f));
        private static readonly Color BarBorder = new Color(.72f, .68f, .48f, .9f);
        private static readonly Color RealmLit = new Color(.50f, 1f, 1f);
        private static readonly Color RealmPassed = new Color(.72f, .94f, .92f, .82f);
        private static readonly Color RealmLocked = new Color(.30f, .42f, .44f, .30f);
        private static readonly Color RealmBackplate = new Color(.012f, .075f, .085f, .94f);

        public GizmoYunjianziCultivation() { Order = -100f; }
        public override float GetWidth(float maxWidth) { return 250f; }
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 80f);
            GUI.color = Color.white;
            GUI.DrawTexture(outer, Panel, ScaleMode.StretchToFill, true);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(outer.x + 67f, outer.y + 6f, 169f, 18f), string.Format("云间子 · {0}", comp.RealmLabel));

            float energyFill = comp.Energy / Mathf.Max(1f, comp.EnergyMax);
            Texture2D energyTexture = energyFill <= .2f ? EnergyLowFill : EnergyFill;
            Rect energyRect = new Rect(outer.x + 68f, outer.y + 26f, 168f, 17f);
            DrawBar(energyRect, energyFill, energyTexture, string.Format("灵力  {0:0.0} / {1:0}", comp.Energy, comp.EnergyMax));
            TooltipHandler.TipRegion(energyRect, "灵力用于施展御剑诀与仙阵诀。每秒自然恢复最大灵力的0.5%。");

            if (comp.Realm >= 2)
            {
                Rect shieldRect = new Rect(outer.x + 68f, outer.y + 48f, 168f, 12f);
                DrawBar(shieldRect, comp.Shield / Mathf.Max(1f, comp.ShieldMax), ShieldFill,
                    string.Format("剑罡  {0:0.0} / {1:0}", comp.Shield, comp.ShieldMax));
                TooltipHandler.TipRegion(shieldRect, "剑罡可抵挡伤害；20秒未受击后开始恢复。筑基境的剑罡约等于普通护盾腰带。");
            }

            DrawRealmEmblems(outer);
            TooltipHandler.TipRegion(new Rect(outer.x + 126f, outer.y + 60f, 98f, 20f),
                "修行境界：练气、筑基、紫府、金丹、化神。亮起的玉印表示当前境界。\n当前：" + comp.RealmLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawRealmEmblems(Rect outer)
        {
            float[] sizes = { 12f, 13.5f, 15f, 16.5f, 18f };
            for (int i = 1; i <= 5; i++)
            {
                float centerX = outer.x + 135.5f + (i - 1) * 17.3f;
                float size = sizes[i - 1];
                if (i == comp.Realm)
                    size *= 1f + .035f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
                Rect marker = new Rect(centerX - size * .5f, outer.y + 68f - size * .5f, size, size);
                Widgets.DrawBoxSolid(marker.ExpandedBy(1f), RealmBackplate);
                GUI.color = i == comp.Realm ? Color.white : (i < comp.Realm ? RealmPassed : RealmLocked);
                GUI.DrawTexture(marker, RealmEmblems[i - 1], ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        private void DrawRealmProgress(Rect outer)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 1; i <= 5; i++)
            {
                float centerX = outer.x + 135.5f + (i - 1) * 17.3f;
                Rect marker = new Rect(centerX - 6f, outer.y + 62f, 12f, 16f);
                GUI.color = i == comp.Realm ? RealmLit : (i < comp.Realm ? new Color(.48f, .78f, .76f, .75f) : new Color(1f, 1f, 1f, .24f));
                Widgets.Label(marker, "◆");
            }
            GUI.color = Color.white;
        }

        private static void DrawBar(Rect rect, float fill, Texture2D texture, string label)
        {
            Widgets.DrawBoxSolid(rect.ExpandedBy(1f), BarBorder);
            Widgets.FillableBar(rect, Mathf.Clamp01(fill), texture, Empty, false);
            float sheenWidth = Mathf.Max(0f, (rect.width - 2f) * Mathf.Clamp01(fill));
            if (sheenWidth > 1f)
                Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.y + 1f, sheenWidth, 2f), new Color(1f, 1f, 1f, .34f));
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(rect, label);
        }
    }

    [DefOf]
    public static class YJZDefOf
    {
        public static HediffDef YJZ_Identity_Yunjianzi;
        public static HediffDef YJZ_CultivationRealm;
        public static HediffDef YJZ_SwordControlImmunity;
        public static HediffDef YJZ_SwordHeartClarity;
        public static TraitDef YJZ_Trait_CloudseaSwordHeart;
        public static BackstoryDef YJZ_Backstory_Childhood;
        public static BackstoryDef YJZ_Backstory_Adulthood;
        public static ThingDef YJZ_Weapon_Zhuxie;
        public static ThingDef YJZ_Projectile_SwordShadow;
        public static ThingDef YJZ_Thing_SwordRain;
        public static ThingDef YJZ_Thing_ArrayCore;
        public static ThingDef YJZ_Thing_ArrayCore_Foundation;
        public static ThingDef YJZ_Thing_ArrayCore_PurplePalace;
        public static ThingDef YJZ_Thing_ArrayCore_GoldenCore;
        public static ThingDef YJZ_Thing_ArrayCore_SpiritTransformation;
        public static HediffDef YJZ_ArrayShield;
        public static FleckDef YJZ_Fleck_SwordShadowAfterimage;
        public static FleckDef YJZ_Fleck_BlinkSlash;
        public static FleckDef YJZ_Fleck_SwordRainBlade;
        public static FleckDef YJZ_Fleck_SwordRainImpact;
        public static FleckDef YJZ_Fleck_ArrayWarpA;
        public static FleckDef YJZ_Fleck_ArrayWarpB;
        static YJZDefOf() { DefOfHelper.EnsureInitializedInCtor(typeof(YJZDefOf)); }
    }

    internal static class YunjianziCombatEffects
    {
        public static void ShowLightning(Thing target)
        {
            if (target == null || !target.Spawned || target.Map == null) return;
            Map map = target.Map;
            map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, target.Position));
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, 1.5f);
        }

        public static void ShowBlinkSlash(Vector3 origin, Vector3 target, Vector3 destination, Map map)
        {
            if (map == null) return;
            Vector3 direction = target - origin;
            direction.y = 0f;
            float angle = direction.sqrMagnitude < .001f ? 0f : Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            FleckMaker.Static(origin.ToIntVec3(), map, FleckDefOf.PsycastSkipFlashEntry, 1.15f);
            FleckMaker.Static(destination.ToIntVec3(), map, FleckDefOf.PsycastSkipOuterRingExit, 1.25f);

            FleckCreationData slash = FleckMaker.GetDataStatic(target, map, YJZDefOf.YJZ_Fleck_BlinkSlash, 1.18f);
            slash.rotation = angle + 90f;
            map.flecks.CreateFleck(slash);

            for (int i = 1; i <= 7; i++)
            {
                Vector3 point = Vector3.Lerp(origin, target, i / 8f);
                FleckCreationData wake = FleckMaker.GetDataStatic(point, map, YJZDefOf.YJZ_Fleck_SwordShadowAfterimage, .65f + i * .055f);
                wake.rotation = angle;
                map.flecks.CreateFleck(wake);
            }
            FleckMaker.ThrowMicroSparks(target, map);
            FleckMaker.ThrowLightningGlow(target, map, 1.1f);
        }

        public static void ShowArrayTeleport(Vector3 origin, Vector3 destination, Map map)
        {
            if (map == null) return;
            DrawWarpGate(origin, map, false);
            DrawWarpGate(destination, map, true);
            for (int i = 1; i <= 7; i++)
            {
                Vector3 point = Vector3.Lerp(origin, destination, i / 8f);
                FleckCreationData arc = FleckMaker.GetDataStatic(point, map, YJZDefOf.YJZ_Fleck_ArrayWarpB,
                    .30f + i * .035f);
                arc.rotation = i * 47f;
                map.flecks.CreateFleck(arc);
            }
        }

        private static void DrawWarpGate(Vector3 position, Map map, bool arrival)
        {
            for (int i = 0; i < 3; i++)
            {
                FleckCreationData ring = FleckMaker.GetDataStatic(position, map, YJZDefOf.YJZ_Fleck_ArrayWarpA,
                    (arrival ? 1.25f : 1.05f) + i * .22f);
                ring.rotation = i * 120f + (arrival ? 25f : -25f);
                map.flecks.CreateFleck(ring);
            }
            FleckCreationData pillar = FleckMaker.GetDataStatic(position, map, YJZDefOf.YJZ_Fleck_ArrayWarpB,
                arrival ? 1.55f : 1.25f);
            pillar.rotation = arrival ? 35f : -35f;
            map.flecks.CreateFleck(pillar);
            FleckMaker.ThrowLightningGlow(position, map, arrival ? 2.2f : 1.7f);
            FleckMaker.ThrowMicroSparks(position, map);
        }

        public static void ShowKnockback(Vector3 origin, Vector3 destination, Map map)
        {
            if (map == null) return;
            Vector3 delta = destination - origin;
            delta.y = 0f;
            float angle = delta.sqrMagnitude < .001f ? 0f : Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            for (int i = 0; i <= 4; i++)
            {
                Vector3 point = Vector3.Lerp(origin, destination, i / 4f);
                FleckCreationData arc = FleckMaker.GetDataStatic(point, map, YJZDefOf.YJZ_Fleck_ArrayWarpB, .42f + i * .08f);
                arc.rotation = angle + 90f;
                map.flecks.CreateFleck(arc);
            }
            FleckMaker.ThrowLightningGlow(destination, map, 1.1f);
        }
    }

    internal static class YunjianziRealmUtility
    {
        public static int RealmOf(Pawn pawn)
        {
            CompYunjianziCultivation comp = pawn == null ? null : pawn.TryGetComp<CompYunjianziCultivation>();
            return comp != null && comp.IsYunjianzi ? comp.Realm : 0;
        }
    }

    public sealed class ThoughtWorker_YunjianziSpiritualCirculation : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            int realm = YunjianziRealmUtility.RealmOf(pawn);
            return realm <= 0 ? ThoughtState.Inactive : ThoughtState.ActiveAtStage(realm - 1);
        }
    }

    [HarmonyPatch(typeof(Need_Food), "get_FoodFallPerTick")]
    internal static class Patch_NeedFood_YunjianziCultivation
    {
        private static readonly float[] Factors = { 1f, .90f, .75f, .55f, .30f, 0f };

        private static void Postfix(Need_Food __instance, ref float __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            int realm = YunjianziRealmUtility.RealmOf(pawn);
            if (realm > 0) __result *= Factors[Mathf.Clamp(realm, 0, 5)];
        }
    }

    [HarmonyPatch(typeof(Need_Rest), "get_RestFallPerTick")]
    internal static class Patch_NeedRest_YunjianziCultivation
    {
        private static readonly float[] Factors = { 1f, .95f, .85f, .65f, .35f, 0f };

        private static void Postfix(Need_Rest __instance, ref float __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            int realm = YunjianziRealmUtility.RealmOf(pawn);
            if (realm > 0) __result *= Factors[Mathf.Clamp(realm, 0, 5)];
        }
    }

    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    internal static class Patch_Pawn_PostApplyDamage_Yunjianzi
    {
        private static bool applyingPassive;
        private static void Postfix(Pawn __instance, DamageInfo dinfo)
        {
            if (applyingPassive || __instance == null || dinfo.Weapon != YJZDefOf.YJZ_Weapon_Zhuxie) return;
            Pawn attacker = dinfo.Instigator as Pawn;
            CompYunjianziCultivation comp = attacker == null ? null : attacker.TryGetComp<CompYunjianziCultivation>();
            if (comp == null || !comp.IsYunjianzi) return;
            try { applyingPassive = true; comp.OnZhuxieHit(__instance); }
            finally { applyingPassive = false; }
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    internal static class Patch_Thing_TakeDamage_ZhuxieScaling
    {
        private static bool Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn target = __instance as Pawn;
            Hediff arrayShield = target == null || target.health == null ? null : target.health.hediffSet.GetFirstHediffOfDef(YJZDefOf.YJZ_ArrayShield);
            if (arrayShield != null && arrayShield.Severity > 0f && dinfo.Def.harmsHealth)
            {
                float shield = arrayShield.Severity;
                if (shield >= dinfo.Amount)
                {
                    arrayShield.Severity = shield - dinfo.Amount;
                    if (target.Spawned) FleckMaker.Static(target.Position, target.Map, FleckDefOf.PsycastAreaEffect, .25f);
                    return false;
                }
                dinfo.SetAmount(dinfo.Amount - shield);
                arrayShield.Severity = 0f;
            }
            if (dinfo.Weapon != YJZDefOf.YJZ_Weapon_Zhuxie) return true;
            Pawn attacker = dinfo.Instigator as Pawn;
            CompYunjianziCultivation comp = attacker == null ? null : attacker.TryGetComp<CompYunjianziCultivation>();
            if (comp == null || !comp.IsYunjianzi || !comp.HoldingZhuxie) return true;
            dinfo.SetAmount(dinfo.Amount * (comp.SwordDamage / 25f));
            if (comp.Realm >= 5) dinfo.SetIgnoreArmor(true);
            return true;
        }
    }
}
