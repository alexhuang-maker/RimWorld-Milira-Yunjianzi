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
    [StaticConstructorOnStartup]
    internal static class YunjianziAbilityTextures
    {
        public static readonly Texture2D SummonZhuxie = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SummonZhuxie");
        public static readonly Texture2D BlinkStrike = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/BlinkStrike");
        public static readonly Texture2D ImmortalArray = ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/ImmortalArray");
        public static readonly Texture2D Meditate = ContentFinder<Texture2D>.Get("Yunjianzi/Buildings/Meditation/MeditationCushion");
        public static readonly Texture2D SwordRain =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SwordRain", false) ?? BlinkStrike;
        public static readonly Texture2D ArrayTeleport =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/ArrayTeleport", false) ?? ImmortalArray;
        public static readonly Texture2D CultivationTree =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/CultivationTree", false) ?? SummonZhuxie;
        public static readonly Texture2D SummonShenji =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SummonShenji", false) ??
            ContentFinder<Texture2D>.Get("Milian/Mechanoid_uiIcon/PawnI", false) ?? SummonZhuxie;
        public static readonly Texture2D SummonSkyWyvern =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/SummonSkyWyvern", false) ?? SummonShenji;
        public static readonly Texture2D TalismanSlot =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/TalismanSlot", false) ?? CultivationTree;
        public static readonly Texture2D FireTornado =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/FireTornado", false) ?? ImmortalArray;
        public static readonly Texture2D ThunderWind =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/ThunderWind", false) ?? ImmortalArray;
        public static readonly Texture2D BeanSoldiers =
            ContentFinder<Texture2D>.Get("Yunjianzi/UI/Abilities/BeanSoldiers", false) ?? SummonShenji;
        public static readonly Texture2D DevX = MakeDevX();

        private static Texture2D MakeDevX()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(.02f, .10f, .24f, 1f);
            Color blue = new Color(.12f, .62f, 1f, 1f);
            Color highlight = new Color(.55f, .88f, 1f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int diagonal = Mathf.Min(Mathf.Abs(x - y), Mathf.Abs((size - 1 - x) - y));
                    Color color = transparent;
                    if (diagonal <= 8 && x > 7 && x < size - 8 && y > 7 && y < size - 8) color = outline;
                    if (diagonal <= 5 && x > 9 && x < size - 10 && y > 9 && y < size - 10) color = blue;
                    if (diagonal <= 2 && x > 11 && x < size - 12 && y > 11 && y < size - 12) color = highlight;
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            texture.name = "Yunjianzi_DEV_X";
            return texture;
        }
    }

    public sealed class CompProperties_YunjianziCultivation : CompProperties
    {
        public CompProperties_YunjianziCultivation() { compClass = typeof(CompYunjianziCultivation); }
    }

    [StaticConstructorOnStartup]
    public sealed partial class CompYunjianziCultivation : ThingComp, IThingHolder
    {
        private static Material swordWardMaterial;
        private static MaterialPropertyBlock swordWardPropertyBlock;
        // Starting with Foundation Establishment, each realm multiplies its
        // former cultivation requirement by the next power of two:
        // Foundation x2, Purple Palace x4, Golden Core x8, Deity x16.
        private static readonly float[] EnergyCaps = { 0f, 100f, 400f, 2000f, 8000f, 32000f };
        // Sword ward is an innate realm benefit.  Each breakthrough greatly
        // expands its base reserve; the optional SwordWard skill improves it
        // further instead of being required to unlock the mechanic.
        private static readonly float[] ShieldCaps = { 0f, 100f, 300f, 800f, 1500f, 5000f };
        private float energy = -1f;
        private float shield = -1f;
        private int lastDamageTick = -99999;
        private int blinkReadyTick;
        private bool autoBlink;
        private int swordRainReadyTick;
        private int swordRainCharges = -1;
        private int swordRainNextChargeTick;
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
        public float RealmEnergyCap { get { return EnergyCaps[Realm]; } }
        public float EnergyMax { get { return Mathf.Clamp(cultivatedEnergyMax, 0f, RealmEnergyCap); } }
        public float ShieldMax
        {
            get
            {
                float realmBase = ShieldCaps[Realm];
                float skillFactor = HasSkill(YunjianziSkills.SwordWard) ? 1.5f : 1f;
                float swordArrayFactor = WearingCloudSwordArray ? 1.5f : 1f;
                return realmBase * skillFactor * swordArrayFactor;
            }
        }
        public float Energy { get { return Mathf.Clamp(energy, 0f, EnergyMax); } set { energy = Mathf.Clamp(value, 0f, EnergyMax); } }
        public float Shield { get { return Mathf.Clamp(shield, 0f, ShieldMax); } set { shield = Mathf.Clamp(value, 0f, ShieldMax); } }

        private static Material SwordWardMaterial
        {
            get
            {
                if (swordWardMaterial == null)
                    swordWardMaterial = MaterialPool.MatFrom("Yunjianzi/Effects/SwordWardShield", ShaderDatabase.TransparentPostLight);
                return swordWardMaterial;
            }
        }

        internal void DrawSwordWardEffect(Vector3 drawLoc)
        {
            Pawn pawn = Pawn;
            if (!IsYunjianzi || pawn == null || pawn.Dead || !pawn.Spawned || !pawn.Drafted ||
                Shield <= 0.001f || ShieldMax <= 0f)
                return;

            if (swordWardPropertyBlock == null) swordWardPropertyBlock = new MaterialPropertyBlock();
            float ratio = Mathf.Clamp01(Shield / ShieldMax);
            float pulse = Mathf.Sin((Find.TickManager.TicksGame + pawn.thingIDNumber * 17) * .045f) * .035f;
            float hitFlash = Find.TickManager.TicksGame - lastDamageTick < 10 ? .18f : 0f;
            float size = 2.02f + pulse + hitFlash;
            float alpha = Mathf.Lerp(.16f, .36f, Mathf.Sqrt(ratio)) + hitFlash * .55f;
            swordWardPropertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(.82f, 1f, 1f, Mathf.Clamp01(alpha)));

            drawLoc.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            float angle = (Find.TickManager.TicksGame * .12f + pawn.thingIDNumber * 29f) % 360f;
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, SwordWardMaterial, 0, null, 0, swordWardPropertyBlock);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn pawn = Pawn;
            if (pawn != null)
            {
                DrawSwordWardEffect(pawn.DrawPos);
                DrawThunderSpearCharge(pawn.DrawPos);
            }
        }
        public int ShadowCount
        {
            get
            {
                int baseCount = HasSkill(YunjianziSkills.SwordShadowsIII) ? 8 :
                    HasSkill(YunjianziSkills.SwordShadowsII) ? 4 :
                    HasSkill(YunjianziSkills.SwordShadowsI) ? 2 : 0;
                return baseCount + (WearingCloudSwordArray ? 6 : 0);
            }
        }
        public bool WearingCloudSwordArray
        {
            get
            {
                return Pawn?.apparel?.WornApparel.Any(apparel =>
                    apparel.def == YJZDefOf.YJZ_Apparel_CloudSwordArray) == true;
            }
        }
        public bool WearingJiuxiaoXianxu
        {
            get
            {
                return Pawn?.apparel?.WornApparel.Any(apparel =>
                    apparel.def == YJZDefOf.YJZ_Apparel_JiuxiaoXianxu) == true;
            }
        }
        public float SwordDamage { get { return new[] { 0f, 25f, 29f, 50f, 85f, 140f }[Realm]; } }
        public float SwordCooldown { get { return new[] { 2f, 2f, 1.75f, 1.5f, 1.25f, 1f }[Realm]; } }
        public float SwordArmorPenetration { get { return HasSkill(YunjianziSkills.ArmorIgnore) ? 10f : new[] { 0f, .9f, .95f, 1.1f, 1.35f, 1.6f }[Realm]; } }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!IsYunjianzi) return;
            EnsureRealmAndAbilities();
            EnsureMechanitorTracker();
            InitializeCultivationProgress();
            if (energy < 0f) energy = EnergyMax;
            if (shield < 0f) shield = ShieldMax;
            TryCreateBoundScroll();
        }

        private void EnsureMechanitorTracker()
        {
            if (!ModsConfig.BiotechActive || Pawn == null) return;
            if (Pawn.mechanitor == null)
                Pawn.mechanitor = new Pawn_MechanitorTracker(Pawn);
            // The public load notification refreshes both bandwidth and the
            // number of control groups after the realm Hediff is present.
            Pawn.mechanitor.Notify_PawnSpawned(true);
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
                MaintainTalismanEffects();
                EnsureRealmAndAbilities();
                UpdateSwordRainCharges();
                UpdateSwordHeartClarity();
                Energy += EnergyMax * 0.004f * TalismanEnergyRecoveryFactor *
                    RobeEnergyRecoveryFactor * CloudSwordArrayRecoveryFactor;
                MaintainFastingNeeds();
                if (ShieldMax > 0f && Find.TickManager.TicksGame > lastDamageTick + 1200)
                    Shield += Mathf.Max(1f, ShieldMax /
                        (HasSkill(YunjianziSkills.SwordWard) ? 60f : 180f));
            }
            if (ShadowCount > 0 && Pawn.Spawned && !Pawn.Downed && Pawn.IsHashIntervalTick(30))
                TryShadowStrike();
            if (autoBlink && HasSkill(YunjianziSkills.BlinkStrike) && Pawn.Spawned && Pawn.Drafted && !Pawn.Downed &&
                Pawn.Awake() && Pawn.IsHashIntervalTick(15))
                TryAutoBlink();
        }

        // 紫府起灵力自行周天，饥饿与睡眠不再消耗。除了冻结需求，
        // 也清除突破前遗留的营养不良，避免刚破境仍被旧状态拖死。
        internal void MaintainFastingNeeds()
        {
            if (Realm < 3 || Pawn == null || Pawn.needs == null) return;
            if (Pawn.needs.food != null) Pawn.needs.food.CurLevelPercentage = 1f;
            if (Pawn.needs.rest != null) Pawn.needs.rest.CurLevelPercentage = 1f;
            Hediff malnutrition = Pawn.health?.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
            if (malnutrition != null) Pawn.health.RemoveHediff(malnutrition);
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

        // Summoning is preparation, not an emergency combat action.  Drafted
        // pawns and pawns already tracking an enemy must first leave combat.
        // Every summon entry point checks this again so hotkeys and external
        // gizmo activators cannot bypass the disabled Command.
        private bool SummoningBlockedByCombat
        {
            get
            {
                return Pawn != null &&
                    (Pawn.Drafted || Pawn.mindState?.enemyTarget != null);
            }
        }

        private bool RejectSummoningDuringCombat()
        {
            if (!SummoningBlockedByCombat) return false;
            Messages.Message("战斗中无法唤起神机或云麒麟。请先解除征召并脱离交战。",
                Pawn, MessageTypeDefOf.RejectInput, false);
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
                foreach (Gizmo scrollGizmo in GetBoundScrollGizmos()) yield return scrollGizmo;
                Command_Action summon = new Command_Action
                {
                    defaultLabel = "御剑诀·唤剑",
                    defaultDesc = "循道书所记剑诀呼唤诛邪。此剑与云间子性命相系，无论流落何方，闻召皆会归于掌中。\n\n消耗10点灵力。",
                    icon = YunjianziAbilityTextures.SummonZhuxie,
                    action = SummonZhuxie
                };
                if (!HasOpenedBoundScroll) summon.Disable("宝珠尚寂。须先研读遗仙宝卷，方能听见诛邪剑鸣。");
                yield return summon;
                if (HasSkill(YunjianziSkills.BlinkStrike))
                {
                    Command_Action blink = new CommandYunjianziBlink
                    {
                        comp = this,
                        defaultLabel = "御剑诀·缩地御剑",
                        defaultDesc = "人随剑意越过咫尺，现于敌后，以诛邪斩其未备。\n\n消耗20点灵力，冷却5秒。",
                        icon = YunjianziAbilityTextures.BlinkStrike,
                        action = BeginBlinkTargeting
                    };
                    blink.defaultLabel = autoBlink ? "御剑诀·缩地御剑（自动）" : "御剑诀·缩地御剑";
                    blink.defaultDesc = "人随剑意越过咫尺，现于敌后，以诛邪斩其未备。\n\n消耗20点灵力，冷却5秒。右键可切换自行寻隙出剑；此法只在征召迎敌时生效。";
                    int remaining = blinkReadyTick - Find.TickManager.TicksGame;
                    if (remaining > 0) blink.Disable(string.Format("冷却中：{0:0.0}秒", remaining / 60f));
                    yield return blink;

                }
                if (HasSkill(YunjianziSkills.SwordRain))
                {
                    UpdateSwordRainCharges();
                    Command_Action swordRain = new Command_Action
                    {
                        defaultLabel = string.Format("御剑诀·天剑雨 {0}/{1}", swordRainCharges, SwordRainMaxCharges),
                        defaultDesc = SwordRainDescription,
                        icon = YunjianziAbilityTextures.SwordRain,
                        action = BeginSwordRainTargeting
                    };
                    int swordRainRemaining = swordRainNextChargeTick - Find.TickManager.TicksGame;
                    if (swordRainCharges <= 0)
                        swordRain.Disable(string.Format("充能中：{0:0.0}秒", Mathf.Max(0, swordRainRemaining) / 60f));
                    else if (Energy < 80f)
                        swordRain.Disable("灵力不足：需要80点。");
                    else if (!CanBeginSwordRainJump)
                        swordRain.Disable("当前状态无法起飞。");
                    yield return swordRain;
                }
                if (HasSkill(YunjianziSkills.ArrayCore))
                    foreach (Gizmo arrayGizmo in GetArrayGizmos()) yield return arrayGizmo;
                if (HasSkill(YunjianziSkills.ShenjiSummon))
                    foreach (Gizmo shenjiGizmo in GetShenjiGizmos()) yield return shenjiGizmo;
                if (HasSkill(YunjianziSkills.ShenjiWyvern))
                    foreach (Gizmo wyvernGizmo in GetSkyWyvernGizmos()) yield return wyvernGizmo;
                if (HasSkill(YunjianziSkills.YangfuPrimer))
                    foreach (Gizmo combatTalismanGizmo in GetYangfuCombatGizmos()) yield return combatTalismanGizmo;
                if (HasSkill(YunjianziSkills.YangfuPrimer))
                    foreach (Gizmo talismanGizmo in GetTalismanGizmos()) yield return talismanGizmo;

                if (Prefs.DevMode && DebugSettings.godMode)
                {
                    if (!HasOpenedBoundScroll)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "DEV：直接开卷",
                            defaultDesc = "God Mode调试：立即视为已研读云间宝卷，开启修为面板并获得初始10点灵力。",
                            icon = YunjianziAbilityTextures.DevX,
                            action = DebugOpenBoundScroll
                        };
                    }

                    Command_Action advanceRealm = new Command_Action
                    {
                        defaultLabel = "DEV：提升境界",
                        defaultDesc = "God Mode调试：直接提升一个境界，同时把修为上限、当前灵力与剑罡补满。不会自动解锁技能。",
                        icon = YunjianziAbilityTextures.DevX,
                        action = DebugAdvanceRealm
                    };
                    if (Realm >= 5) advanceRealm.Disable("已达到化神境。");
                    yield return advanceRealm;

                    yield return new Command_Action
                    {
                        defaultLabel = "DEV：补满灵力/修为",
                        defaultDesc = "God Mode调试：把当前境界的灵力上限提升至境界极限，并补满当前灵力与剑罡。",
                        icon = YunjianziAbilityTextures.DevX,
                        action = DebugFillCultivation
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "DEV：解锁境界技能",
                        defaultDesc = "God Mode调试：免费解锁当前境界及之前的全部技能节点。适合快速检查完整境界效果。",
                        icon = YunjianziAbilityTextures.DevX,
                        action = DebugUnlockThroughCurrentRealm
                    };

                    yield return new Command_Action
                    {
                        defaultLabel = "DEV：重置技能树",
                        defaultDesc = "God Mode调试：清除除诛邪剑契外的全部已学节点，取消自动缩地御剑并收回现有仙阵。境界和修为不变。",
                        icon = YunjianziAbilityTextures.DevX,
                        action = DebugResetSkills
                    };
                }
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
            UpdateSwordRainCharges();
            if (!HasSkill(YunjianziSkills.SwordRain) || swordRainCharges <= 0 || !CanBeginSwordRainJump) return;
            TargetingParameters parms = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                validator = target => target.IsValid && IsValidSwordRainLanding(target.Cell)
            };
            Find.Targeter.BeginTargeting(
                parms,
                target => TryCastSwordRain(target.Cell),
                DrawSwordRainTargetHighlight,
                null,
                Pawn,
                null,
                YunjianziAbilityTextures.SwordRain,
                true,
                delegate { Widgets.MouseAttachedLabel(string.Format("霸体跃击落点 · 连斩半径 {0:0.0}", SwordRainRadius)); });
        }

        private void DrawSwordRainTargetHighlight(LocalTargetInfo target)
        {
            if (!target.IsValid || Pawn == null || Pawn.Map == null || !target.Cell.InBounds(Pawn.Map)) return;
            GenDraw.DrawRadiusRing(target.Cell, SwordRainRadius, new Color(.48f, .90f, 1f));
        }

        private void TryCastSwordRain(IntVec3 center)
        {
            UpdateSwordRainCharges();
            if (!HasSkill(YunjianziSkills.SwordRain) || !CanBeginSwordRainJump || !IsValidSwordRainLanding(center))
            {
                Messages.Message("天剑雨需要可以起飞，且落点必须是35.9格内可见、无屋顶的可站立位置。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            if (swordRainCharges <= 0)
            {
                Messages.Message("天剑雨尚未完成充能。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }
            if (!TrySpend(80f)) return;
            LaunchSwordRainJump(center, SwordRainDamage, SwordRainRadius);
            swordRainCharges--;
            if (swordRainCharges < SwordRainMaxCharges && swordRainNextChargeTick <= 0)
                swordRainNextChargeTick = Find.TickManager.TicksGame + SwordRainCooldownTicks;
            swordRainReadyTick = swordRainNextChargeTick;
        }

        private int SwordRainMaxCharges { get { return HasSkill(YunjianziSkills.SwordRainGolden) ? 2 : 1; } }
        private int SwordRainCooldownTicks
        {
            get
            {
                int baseTicks = HasSkill(YunjianziSkills.SwordRainSpirit) ? 1200 :
                    HasSkill(YunjianziSkills.SwordRainGolden) ? 1500 : 1800;
                return Mathf.Max(60, Mathf.RoundToInt(baseTicks * TalismanCooldownFactor));
            }
        }
        private int SwordRainDamage { get { return HasSkill(YunjianziSkills.SwordRainSpirit) ? 168 : 84; } }
        private float SwordRainRadius { get { return SwordRainEchoMotion.RadiusForRealm(Realm, HasSkill(YunjianziSkills.SwordRainGolden)); } }
        private string SwordRainDescription
        {
            get
            {
                return string.Format("消耗80点灵力，面向落点进入霸体并腾空蓄势，随后重重落在目标位置，分出当前外观的淡蓝色透明虚影。虚影在半径{1:0.0}格内连续穿梭斩击8次，持续约2.4秒；每次掠过敌人造成切割伤害，单个目标全部命中时合计{0}点，护甲穿透62%，不伤及友方。筑基起每提升一境增加1格半径，九霄剑阵额外增加1.5格，斩击宽度与剑痕同步扩大。起飞至落地免疫眩晕打断。需露天起飞，落点须在35.9格内、可见且无屋顶。每次充能冷却{2:0}秒，最多储存{3}次。",
                    SwordRainDamage, SwordRainRadius, SwordRainCooldownTicks / 60f, SwordRainMaxCharges);
            }
        }

        private void UpdateSwordRainCharges()
        {
            if (!HasSkill(YunjianziSkills.SwordRain)) return;
            int now = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            int max = SwordRainMaxCharges;
            if (swordRainCharges < 0)
            {
                if (swordRainReadyTick > now)
                {
                    swordRainCharges = 0;
                    swordRainNextChargeTick = swordRainReadyTick;
                }
                else
                {
                    swordRainCharges = max;
                    swordRainNextChargeTick = 0;
                }
            }
            swordRainCharges = Mathf.Clamp(swordRainCharges, 0, max);
            if (swordRainCharges < max && swordRainNextChargeTick <= 0)
                swordRainNextChargeTick = now + SwordRainCooldownTicks;
            while (swordRainCharges < max && swordRainNextChargeTick > 0 && now >= swordRainNextChargeTick)
            {
                swordRainCharges++;
                swordRainNextChargeTick = swordRainCharges < max
                    ? swordRainNextChargeTick + SwordRainCooldownTicks
                    : 0;
            }
            swordRainReadyTick = swordRainNextChargeTick;
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
            DamageInfo info = new DamageInfo(DamageDefOf.Cut, SwordDamage * 3f, SwordArmorPenetration, -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(info);
            if (HasSkill(YunjianziSkills.LightningEnchantment) && !target.Destroyed) ApplyLightning(target, SwordDamage * .75f);
            if (HasSkill(YunjianziSkills.ChainLightningEnchantment) && !target.Destroyed)
                ApplyChainLightning(target);
            blinkReadyTick = Find.TickManager.TicksGame + Mathf.RoundToInt(300f * TalismanCooldownFactor);
            YunjianziCombatEffects.ShowBlinkSlash(originDrawPos, targetDrawPos, Pawn.DrawPos, map);
            if (HasSkill(YunjianziSkills.BlinkChain) && (target.Destroyed || (target is Pawn && ((Pawn)target).Dead)) && Energy >= 20f)
            {
                Thing next = CombatTargetsInRange(25.9f).OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
                if (next != null) TryBlink(next, false);
            }
            return true;
        }

        private void TryAutoBlink()
        {
            if (!autoBlink || Find.TickManager.TicksGame < blinkReadyTick || Energy < 20f ||
                Pawn == null || !Pawn.Spawned || Pawn.Map == null || Pawn.Downed || !Pawn.Drafted)
                return;

            Thing target = CombatTargetsInRange(25.9f)
                .Where(t => !t.Position.Fogged(Pawn.Map))
                .OrderBy(t => t is Pawn && ((Pawn)t).Downed ? 2 : t is Building_Turret ? 1 : 0)
                .ThenBy(t => t.Position.DistanceToSquared(Pawn.Position))
                .FirstOrDefault();
            if (target != null)
                TryBlink(target, false);
        }

        public void ToggleAutoBlink()
        {
            autoBlink = !autoBlink;
            Messages.Message(autoBlink
                ? "缩地御剑已转为自行应敌：征召时会主动寻找目标。"
                : "缩地御剑已改回手动施展。",
                Pawn, autoBlink ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent, false);
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
                .OrderBy(t => t.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if (target == null) return;
            ThingYunjianziSwordShadow shadow = (ThingYunjianziSwordShadow)ThingMaker.MakeThing(YJZDefOf.YJZ_Projectile_SwordShadow);
            shadow.Initialize(Pawn, shadowIndex, target);
            GenSpawn.Spawn(shadow, Pawn.Position, Pawn.Map);
        }

        public void OnShadowProjectileHit(Thing target, int shadowIndex)
        {
            if (target == null || target.Destroyed || (target is Pawn && ((Pawn)target).Dead)) return;
            // A sword shadow carries 70% of Zhuxie's force, but it is still the
            // same cultivated edge. Reducing penetration as well made armor
            // discount the attack twice and collapsed real hits to 2-3 damage.
            DamageInfo info = new DamageInfo(DamageDefOf.Cut, SwordDamage * .7f, SwordArmorPenetration, -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(info);
            if (HasSkill(YunjianziSkills.ShadowAffixes) && !target.Destroyed && (!(target is Pawn) || !((Pawn)target).Dead))
            {
                Pawn pawnTarget = target as Pawn;
                if (pawnTarget != null && HasSkill(YunjianziSkills.SwordEnchantment)) ApplyControl(pawnTarget);
                while (shadowLightningReady.Count < ShadowCount) shadowLightningReady.Add(0);
                if (HasSkill(YunjianziSkills.LightningEnchantment) && Find.TickManager.TicksGame >= shadowLightningReady[shadowIndex])
                {
                    ApplyLightning(target, SwordDamage * .75f);
                    shadowLightningReady[shadowIndex] = Find.TickManager.TicksGame +
                        Mathf.RoundToInt(600f * TalismanCooldownFactor);
                }
                if (HasSkill(YunjianziSkills.ChainLightningEnchantment)) ApplyChainLightning(target);
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!IsYunjianzi || Pawn == null || Pawn.Dead) return;
            if (WearingCloudSwordArray && dinfo.Def.harmsHealth && dinfo.Amount > 100f)
                dinfo.SetAmount(100f);
            int now = Find.TickManager.TicksGame;
            if (HasSkill(YunjianziSkills.DeathLock) && HoldingZhuxie && (now < deathLockUntil || (Pawn.health.summaryHealth.SummaryHealthPercent < .25f && now >= deathLockReadyTick)))
            {
                if (now >= deathLockUntil)
                {
                    deathLockUntil = now + 1200;
                    deathLockReadyTick = now + 60000;
                    Messages.Message("抱一守命：诛邪镇住生死玄关，暂护云间子不坠死境。", Pawn, MessageTypeDefOf.PositiveEvent);
                }
                absorbed = true;
                return;
            }
            if (ShieldMax > 0f && Shield > 0f && dinfo.Def.harmsHealth)
            {
                lastDamageTick = now;
                float amount = dinfo.Amount;
                float shieldBefore = Shield;
                if (Shield >= amount)
                {
                    Shield -= amount;
                    if (WearingCloudSwordArray) TriggerSwordWardBursts(shieldBefore, Shield);
                    absorbed = true;
                    if (Pawn.Spawned) FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.PsycastAreaEffect, .35f);
                }
                else
                {
                    dinfo.SetAmount(amount - Shield);
                    Shield = 0f;
                    if (WearingCloudSwordArray) TriggerSwordWardBursts(shieldBefore, 0f);
                }
            }
        }

        private void TriggerSwordWardBursts(float before, float after)
        {
            float max = ShieldMax;
            if (max <= 0f || after >= before || Pawn == null || !Pawn.Spawned || Pawn.Map == null) return;
            float third = max / 3f;
            int crossed = Mathf.CeilToInt(Mathf.Clamp(before, 0f, max) / third) -
                Mathf.CeilToInt(Mathf.Clamp(after, 0f, max) / third);
            for (int i = 0; i < crossed; i++) BurstSwordWard();
        }

        private void BurstSwordWard()
        {
            Map map = Pawn.Map;
            bool swordArray = WearingCloudSwordArray;
            float radius = swordArray ? 5f : 3.9f;
            int pushDistance = swordArray ? 4 : 2;
            float damage = (20f + Realm * 15f) * (swordArray ? 1.5f : 1f);
            List<Thing> enemies = GenRadial.RadialDistinctThingsAround(Pawn.Position, map, radius, true)
                .Where(t => t != Pawn && t.Spawned && t.HostileTo(Pawn) &&
                    ((t is Pawn targetPawn && !targetPawn.Dead) || t is Building_Turret))
                .ToList();

            FleckMaker.Static(Pawn.Position, map, FleckDefOf.PsycastAreaEffect, swordArray ? 2.2f : 1.65f);
            FleckMaker.ThrowLightningGlow(Pawn.DrawPos, map, swordArray ? 2.8f : 2.1f);
            for (int index = 0; index < enemies.Count; index++)
            {
                Thing enemy = enemies[index];
                enemy.TakeDamage(new DamageInfo(DamageDefOf.Cut, damage, SwordArmorPenetration,
                    -1f, Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
                Pawn enemyPawn = enemy as Pawn;
                if (enemyPawn == null || enemyPawn.Dead || !enemyPawn.Spawned) continue;

                int dx = Math.Sign(enemyPawn.Position.x - Pawn.Position.x);
                int dz = Math.Sign(enemyPawn.Position.z - Pawn.Position.z);
                if (dx == 0 && dz == 0)
                {
                    Rot4 fallback = new Rot4(index % 4);
                    dx = fallback.FacingCell.x;
                    dz = fallback.FacingCell.z;
                }
                IntVec3 direction = new IntVec3(dx, 0, dz);
                IntVec3 destination = enemyPawn.Position;
                for (int step = 1; step <= pushDistance; step++)
                {
                    IntVec3 candidate = enemyPawn.Position + direction * step;
                    if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map) ||
                        candidate.GetFirstPawn(map) != null) break;
                    destination = candidate;
                }
                if (destination == enemyPawn.Position) continue;
                Vector3 from = enemyPawn.DrawPos;
                enemyPawn.pather?.StopDead();
                enemyPawn.Position = destination;
                enemyPawn.Notify_Teleported(false, true);
                YunjianziCombatEffects.ShowKnockback(from, enemyPawn.DrawPos, map);
            }
        }

        public bool HoldingZhuxie { get { return Pawn != null && Pawn.equipment != null && Pawn.equipment.Primary != null && Pawn.equipment.Primary.def == YJZDefOf.YJZ_Weapon_Zhuxie; } }

        public void OnZhuxieHit(Pawn target)
        {
            if (target == null || target.Dead) return;
            if (HasSkill(YunjianziSkills.SwordEnchantment)) ApplyControl(target);
            if (HasSkill(YunjianziSkills.LightningEnchantment))
                ApplyLightning(target, SwordDamage * .75f);
            if (HasSkill(YunjianziSkills.ChainLightningEnchantment)) ApplyChainLightning(target);
            if (HasSkill(YunjianziSkills.SwordHealing))
                HealFromStrike();
        }

        private void ApplyControl(Pawn target)
        {
            if (target == null || target.Dead || target.health.hediffSet.HasHediff(YJZDefOf.YJZ_SwordControlImmunity)) return;
            YunjianziEmp.Apply(target, Pawn, 2.5f);
            if (target.Destroyed || target.Dead || !target.Spawned) return;
            if (target.stances != null && target.stances.stunner != null)
                target.stances.stunner.StunFor(target.RaceProps.IsMechanoid ? 75 : 30, Pawn);
            Hediff immune = HediffMaker.MakeHediff(YJZDefOf.YJZ_SwordControlImmunity, target);
            target.health.AddHediff(immune);
        }

        private void ApplyLightning(Thing target, float amount)
        {
            if (target == null || !target.Spawned) return;
            YunjianziCombatEffects.ShowLightningAt(target.Position, target.Map, Pawn,
                visualStrength: .7f, damage: amount,
                armorPenetration: HasSkill(YunjianziSkills.ArmorIgnore) ? 10f : .75f);
        }

        private void ApplyChainLightning(Thing firstTarget)
        {
            if (Pawn == null || Pawn.Map == null || firstTarget == null || firstTarget.Destroyed) return;
            Map map = Pawn.Map;
            List<Thing> struck = new List<Thing>();
            List<Vector3> arcPoints = new List<Vector3>();
            Thing current = firstTarget;

            // VPE's ChainBolt uses four bounces and a finite bounce range.
            // Keep that readable shape, but use our own low-damage effect so
            // this mod does not acquire a hard dependency on VPE/VEF.
            for (int jump = 0; jump < 5 && current != null; jump++)
            {
                struck.Add(current);
                arcPoints.Add(current.DrawPos);
                IntVec3 hitCell = current.Position;
                YunjianziEmp.Apply(current, Pawn, YunjianziEmp.ChainDamage);
                if (!current.Destroyed && current.Spawned && (!(current is Pawn hitPawn) || !hitPawn.Dead))
                    current.TakeDamage(new DamageInfo(YJZDefOf.YJZ_ShenxiaoLightning,
                    SwordDamage * .15f, .35f, -1f, Pawn,
                    null, null, DamageInfo.SourceCategory.ThingOrUnknown));

                current = GenRadial.RadialDistinctThingsAround(hitCell, map, 6.9f, true)
                    .Where(t => t != null && t.Spawned && !struck.Contains(t) && t.HostileTo(Pawn) &&
                        ((t is Pawn && !((Pawn)t).Dead) || t is Building_Turret))
                    .OrderBy(t => t.Position.DistanceToSquared(hitCell))
                    .FirstOrDefault();
            }

            YunjianziPurpleLightning.Chain(arcPoints, map);
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
            Scribe_Values.Look(ref lastZhuxieSlashVariant, "yjz_lastZhuxieSlashVariant", -1);
            Scribe_Values.Look(ref autoBlink, "yjz_autoBlink", false);
            Scribe_Values.Look(ref swordRainReadyTick, "yjz_swordRainReadyTick", 0);
            Scribe_Values.Look(ref swordRainCharges, "yjz_swordRainCharges", -1);
            Scribe_Values.Look(ref swordRainNextChargeTick, "yjz_swordRainNextChargeTick", 0);
            Scribe_Values.Look(ref issuedShenjiCores, "yjz_issuedShenjiCores", -1);
            ExposeQilinSummonData();
            Scribe_Values.Look(ref deathLockUntil, "yjz_deathLockUntil", 0);
            Scribe_Values.Look(ref deathLockReadyTick, "yjz_deathLockReadyTick", 0);
            Scribe_Collections.Look(ref shadowLightningReady, "yjz_shadowLightningReady", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && shadowLightningReady == null)
                shadowLightningReady = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };
            ExposeArrayData();
            ExposeBreakthroughData();
            ExposeSkillTreeData();
            ExposeTalismanData();
            ExposeYangfuCombatData();
        }
    }

    public sealed class CommandYunjianziBlink : Command_Action
    {
        public CompYunjianziCultivation comp;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Event current = Event.current;
            bool rightClicked = current != null && current.type == EventType.MouseDown && current.button == 1 &&
                new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f).Contains(current.mousePosition);

            GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
            if (!rightClicked || comp == null) return result;

            comp.ToggleAutoBlink();
            current.Use();
            return new GizmoResult(GizmoState.Interacted, current);
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
        internal static Texture2D RealmTexture(int realm)
        {
            return RealmEmblems[Mathf.Clamp(realm, 1, 5) - 1];
        }
        private static readonly Texture2D EnergyFill = SolidColorMaterials.NewSolidColorTexture(new Color(.24f, .88f, .91f));
        private static readonly Texture2D EnergyLowFill = SolidColorMaterials.NewSolidColorTexture(new Color(.86f, .65f, .26f));
        private static readonly Texture2D ShieldFill = SolidColorMaterials.NewSolidColorTexture(new Color(.44f, .66f, 1f));
        private static readonly Texture2D Empty = SolidColorMaterials.NewSolidColorTexture(new Color(.025f, .09f, .105f));
        private static readonly Color BarBorder = new Color(.72f, .68f, .48f, .9f);
        private static readonly Color RealmLit = new Color(.50f, 1f, 1f);
        private static readonly Color RealmPassed = new Color(.72f, .94f, .92f, .82f);
        private static readonly Color RealmLocked = new Color(.30f, .42f, .44f, .30f);

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
            // Keep the three information bands disjoint.  The old shield bar ended at
            // y=60 while the larger realm emblems started around y=55, so the bar
            // physically painted over the Zifu/Jindan/Huashen emblems.
            Rect energyRect = new Rect(outer.x + 68f, outer.y + 24f, 168f, 16f);
            DrawBar(energyRect, energyFill, energyTexture,
                string.Format("灵 {0}/{1}  修 {1}/{2}",
                    CompactNumber(comp.Energy), CompactNumber(comp.EnergyMax),
                    CompactNumber(comp.RealmEnergyCap)), 8);
            TooltipHandler.TipRegion(energyRect,
                "前一组数值是当前灵力与已解锁上限；后一组是当前修为上限与本境界极限。\n" +
                string.Format("精确数值：灵力 {0:0.##}/{1:0.##}，修为 {1:0.##}/{2:0.##}。\n",
                    comp.Energy, comp.EnergyMax, comp.RealmEnergyCap) +
                "灵力充满后继续打坐，或由云间子消灭敌人，都会提高灵力上限。达到境界极限后可闭关突破。");

            if (comp.ShieldMax > 0f)
            {
                Rect shieldRect = new Rect(outer.x + 68f, outer.y + 43f, 168f, 13f);
                DrawBar(shieldRect, comp.Shield / Mathf.Max(1f, comp.ShieldMax), ShieldFill,
                    string.Format("剑罡 {0}/{1}", CompactNumber(comp.Shield),
                        CompactNumber(comp.ShieldMax)), 8);
                TooltipHandler.TipRegion(shieldRect,
                    string.Format("精确数值：剑罡 {0:0.##}/{1:0.##}。\n", comp.Shield, comp.ShieldMax) +
                    "剑罡随境界自然壮大，可替云间子承受来袭之力。装备云海剑轮时，上限提高50%，单次受伤不超过100点；剑罡每损失最大值的三分之一，还会爆发剑气伤敌并将其推开。参悟「剑罡卫形」可再使上限提高50%，并将重聚速度提升至三倍。");
            }

            DrawRealmEmblems(outer);
            TooltipHandler.TipRegion(new Rect(outer.x + 126f, outer.y + 57f, 98f, 22f),
                "修行境界：练气、筑基、紫府、金丹、化神。亮起的玉印表示当前境界。\n当前：" + comp.RealmLabel);
            Rect orbRect = new Rect(outer.x + 7f, outer.y + 8f, 56f, 57f);
            if (comp.HasOpenedBoundScroll)
            {
                TooltipHandler.TipRegion(orbRect, "点击宝珠，在识海中重展遗仙宝卷。每一次参悟都需以自身灵力印证；右侧玉印所示，便是当前境界。");
                if (Mouse.IsOver(orbRect))
                {
                    GUI.color = new Color(.40f, .94f, 1f, .82f);
                    Widgets.DrawBox(orbRect, 2);
                    GUI.color = Color.white;
                }
                if (Widgets.ButtonInvisible(orbRect))
                {
                    Find.WindowStack.Add(new Window_YunjianziCultivationTree(comp));
                    Event current = Event.current;
                    return new GizmoResult(GizmoState.Interacted, current);
                }
            }
            else
            {
                TooltipHandler.TipRegion(orbRect, "宝珠沉寂无光。待遗仙宝卷纳入识海，其中篇章才会在此徐徐展开。");
            }
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawRealmEmblems(Rect outer)
        {
            float[] sizes = { 10f, 11.5f, 13f, 14.5f, 16f };
            for (int i = 1; i <= 5; i++)
            {
                float centerX = outer.x + 135.5f + (i - 1) * 17.3f;
                float size = sizes[i - 1];
                if (i == comp.Realm)
                    size *= 1f + .035f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
                Rect marker = new Rect(centerX - size * .5f, outer.y + 68f - size * .5f, size, size);
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

        private static void DrawBar(Rect rect, float fill, Texture2D texture, string label, int fontSize = 0)
        {
            Widgets.DrawBoxSolid(rect.ExpandedBy(1f), BarBorder);
            Widgets.FillableBar(rect, Mathf.Clamp01(fill), texture, Empty, false);
            float sheenWidth = Mathf.Max(0f, (rect.width - 2f) * Mathf.Clamp01(fill));
            if (sheenWidth > 1f)
                Widgets.DrawBoxSolid(new Rect(rect.x + 1f, rect.y + 1f, sheenWidth, 2f), new Color(1f, 1f, 1f, .34f));
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.black;
            if (fontSize > 0)
            {
                GUIStyle style = new GUIStyle(Text.CurFontStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = fontSize
                };
                GUI.Label(rect, label, style);
            }
            else
            {
                Widgets.Label(rect, label);
            }
            GUI.color = Color.white;
        }

        private static string CompactNumber(float value)
        {
            float absolute = Mathf.Abs(value);
            if (absolute >= 1000000f) return (value / 1000000f).ToString("0.##") + "m";
            if (absolute >= 1000f) return (value / 1000f).ToString("0.##") + "k";
            return value.ToString(absolute >= 100f ? "0" : "0.#");
        }
    }

    [DefOf]
    public static class YJZDefOf
    {
        public static ThingDef YJZ_PawnFlyer_SwordRain;
        public static ThingDef YJZ_Thing_SwordRainEcho;
        public static FleckDef YJZ_Fleck_SwordRainAura;
        public static FleckDef YJZ_Fleck_SwordRainBurstA;
        public static FleckDef YJZ_Fleck_SwordRainBurstB;
        public static HediffDef YJZ_Identity_Yunjianzi;
        public static HediffDef YJZ_CultivationRealm;
        public static HediffDef YJZ_SwordControlImmunity;
        public static HediffDef YJZ_SwordHeartClarity;
        public static DamageDef YJZ_ShenxiaoLightning;
        public static DamageDef YJZ_ThunderSpearBlast;
        public static DamageDef YJZ_ZhuxieBuildingCut;
        public static TraitDef YJZ_Trait_CloudseaSwordHeart;
        public static TimeAssignmentDef YJZ_Cultivate;
        public static JobDef YJZ_Meditate;
        public static JobDef YJZ_ReadBoundScroll;
        public static JobDef YJZ_Breakthrough;
        public static ThingDef YJZ_Item_BoundImmortalScroll;
        public static BackstoryDef YJZ_Backstory_Childhood;
        public static BackstoryDef YJZ_Backstory_Adulthood;
        public static ThingDef YJZ_Weapon_Zhuxie;
        public static ThingDef YJZ_Apparel_YunjianRobe;
        public static ThingDef YJZ_Apparel_CloudSwordArray;
        public static ThingDef YJZ_Apparel_JiuxiaoXianxu;
        public static ThingDef YJZ_Projectile_SwordShadow;
        public static ThingDef YJZ_Thing_SwordRain;
        public static ThingDef YJZ_Effect_LightningChain;
        public static ThingDef YJZ_Projectile_ThunderSpear;
        public static ThingDef YJZ_Effect_ThunderSpearAnchor;
        public static ThingDef YJZ_Thing_ArrayCore;
        public static ThingDef YJZ_Thing_ArrayCore_Foundation;
        public static ThingDef YJZ_Thing_ArrayCore_PurplePalace;
        public static ThingDef YJZ_Thing_ArrayCore_GoldenCore;
        public static ThingDef YJZ_Thing_ArrayCore_SpiritTransformation;
        public static HediffDef YJZ_ArrayShield;
        public static HediffDef YJZ_ArrayEdgeSlow;
        public static HediffDef YJZ_ArrayEdgeControlImmunity;
        public static FleckDef YJZ_Fleck_SwordShadowAfterimage;
        public static FleckDef YJZ_Fleck_QilinThunderClawA;
        public static FleckDef YJZ_Fleck_QilinThunderClawB;
        public static FleckDef YJZ_Fleck_SwordShadowCloudWake;
        public static FleckDef YJZ_Fleck_BlinkSlash;
        public static FleckDef YJZ_Fleck_ZhuxieSlashA;
        public static FleckDef YJZ_Fleck_ZhuxieSlashB;
        public static FleckDef YJZ_Fleck_SwordRainBlade;
        public static FleckDef YJZ_Fleck_SwordRainImpact;
        public static FleckDef YJZ_Fleck_ArrayWarpA;
        public static FleckDef YJZ_Fleck_ArrayWarpB;
        public static PawnKindDef YJZ_Shenji;
        public static PawnKindDef YJZ_Shenji_Lingwei;
        public static PawnKindDef YJZ_Shenji_Tianjiang;
        public static PawnKindDef YJZ_SkyWyvern_Purple;
        public static PawnKindDef YJZ_SkyWyvern_Golden;
        public static PawnKindDef YJZ_SkyWyvern_Spirit;
        public static HediffDef YJZ_SkyWyvernAcceleration;
        public static HediffDef YJZ_QilinBattleRoar;
        public static HediffDef YJZ_SkyWyvernRegeneration;
        public static ThingDef YJZ_Weapon_Shenji_Assault;
        public static ThingDef YJZ_Weapon_Shenji_Gatling;
        public static ThingDef YJZ_Weapon_Shenji_Gauss;
        public static ThingDef YJZ_Weapon_Shenji_Legendary;
        public static ThingDef YJZ_Apparel_ShenjiArmor_Base;
        public static ThingDef YJZ_Apparel_ShenjiArmor_Intermediate;
        public static ThingDef YJZ_Apparel_ShenjiArmor_Final;
        public static ThingDef YJZ_Apparel_ShenjiVisor;
        public static ThingDef YJZ_Apparel_JiuxiaoImmortalCrown;
        public static ThingDef Milian_Lining;
        public static ThingDef Milira_FeatherThread;
        public static ThingDef YJZ_Projectile_ShenjiLaser;
        public static ThingDef YJZ_Projectile_ShenjiChainLaser;
        public static HediffDef YJZ_ShenjiArmor_Base;
        public static HediffDef YJZ_ShenjiArmor_Intermediate;
        public static HediffDef YJZ_ShenjiArmor_Final;
        public static HediffDef YJZ_Hediff_CloudCommandChip;
        public static ThingDef YJZ_MilianFitting_CloudCommandChip;
        public static ThingDef YJZ_Effect_TalismanRed;
        public static ThingDef YJZ_Effect_TalismanPurple;
        public static ThingDef YJZ_Effect_TalismanGreen;
        public static ThingDef YJZ_Effect_FireTornado;
        public static ThingDef YJZ_Effect_ThunderWind;
        public static PawnKindDef YJZ_BeanSoldier_1;
        public static PawnKindDef YJZ_BeanSoldier_2;
        public static PawnKindDef YJZ_BeanSoldier_3;
        public static PawnKindDef YJZ_BeanSoldier_4;
        public static PawnKindDef YJZ_BeanSoldier_5;
        public static PawnKindDef YJZ_BeanSoldier_CloudCommand;
        public static PawnKindDef YJZ_ThunderBeast;
        static YJZDefOf() { DefOfHelper.EnsureInitializedInCtor(typeof(YJZDefOf)); }
    }

    internal static class YunjianziCombatEffects
    {
        public static void ShowLightning(Thing target, Pawn caster = null, float paidLightningCost = 0f)
        {
            if (target == null || !target.Spawned || target.Map == null) return;
            ShowLightningAt(target.Position, target.Map, caster, paidLightningCost, .7f);
        }

        public static void ShowLightningAt(IntVec3 cell, Map map, Pawn caster = null, float paidLightningCost = 0f,
            float visualStrength = 1f, float damage = -1f, float armorPenetration = .75f,
            float radius = YunjianziLightningDamage.MinimumRadius)
        {
            if (map == null || !cell.InBounds(map)) return;
            YunjianziPurpleLightning.Strike(cell, map, visualStrength, caster, damage, armorPenetration, radius);
            ChargeSwordWardFromLightning(caster);
            ThunderBeastUtility.TrySummon(caster, cell, map, false, paidLightningCost);
        }

        public static void ChargeSwordWardFromLightning(Pawn caster)
        {
            CompYunjianziCultivation cultivation = caster == null
                ? null
                : caster.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.WearingJiuxiaoXianxu || cultivation.ShieldMax <= 0f) return;
            cultivation.Shield += Mathf.Max(10f, cultivation.ShieldMax * .005f);
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
            YunjianziPurpleLightning.Glow(target, map, 1.1f);
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
            YunjianziPurpleLightning.Glow(position, map, arrival ? 2.2f : 1.7f);
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
            YunjianziPurpleLightning.Glow(destination, map, 1.1f);
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

    internal static class YunjianziNeedUtility
    {
        // Verse.Need keeps its owner in a protected field. Cache a strongly typed
        // accessor once instead of constructing a Traverse on every need tick.
        private static readonly AccessTools.FieldRef<Need, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Need, Pawn>("pawn");

        internal static Pawn PawnOf(Need need) => need == null ? null : PawnRef(need);
    }

    [HarmonyPatch(typeof(Need_Food), "get_FoodFallPerTick")]
    internal static class Patch_NeedFood_YunjianziCultivation
    {
        private static readonly float[] Factors = { 1f, .90f, .75f, 0f, 0f, 0f };

        private static void Postfix(Need_Food __instance, ref float __result)
        {
            Pawn pawn = YunjianziNeedUtility.PawnOf(__instance);
            int realm = YunjianziRealmUtility.RealmOf(pawn);
            if (realm > 0) __result *= Factors[Mathf.Clamp(realm, 0, 5)];
        }
    }

    [HarmonyPatch(typeof(Need_Rest), "get_RestFallPerTick")]
    internal static class Patch_NeedRest_YunjianziCultivation
    {
        private static readonly float[] Factors = { 1f, .95f, .85f, 0f, 0f, 0f };

        private static void Postfix(Need_Rest __instance, ref float __result)
        {
            Pawn pawn = YunjianziNeedUtility.PawnOf(__instance);
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

    /// <summary>
    /// Play Zhuxie's cultivated sword-light at the start of every real melee
    /// cast, so misses and dodges still visibly read as committed slashes.
    /// </summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    internal static class Patch_Verb_TryCastNextBurstShot_ZhuxieSlash
    {
        private static void Prefix(Verb __instance)
        {
            if (__instance == null || __instance.EquipmentSource == null ||
                __instance.EquipmentSource.def != YJZDefOf.YJZ_Weapon_Zhuxie ||
                __instance.verbProps == null || !__instance.verbProps.IsMeleeAttack) return;
            Pawn attacker = __instance.CasterPawn;
            Thing target = __instance.CurrentTarget.Thing;
            CompYunjianziCultivation cultivation = attacker == null
                ? null
                : attacker.TryGetComp<CompYunjianziCultivation>();
            if (cultivation == null || !cultivation.IsYunjianzi || cultivation.Realm < 2 ||
                target == null || !attacker.Spawned || target.Map != attacker.Map) return;

            cultivation.PlayZhuxieSlash(target.DrawPos);
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    internal static class Patch_Thing_TakeDamage_ZhuxieScaling
    {
        private static bool Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn target = __instance as Pawn;
            Pawn attacker = dinfo.Instigator as Pawn;
            CompYunjianziCultivation attackerComp = attacker == null ? null : attacker.TryGetComp<CompYunjianziCultivation>();
            if ((dinfo.Def == YJZDefOf.YJZ_ShenxiaoLightning || dinfo.Def == YJZDefOf.YJZ_ThunderSpearBlast) &&
                attackerComp?.WearingJiuxiaoXianxu == true)
                dinfo.SetAmount(dinfo.Amount * 2f);

            CompYunjianziCultivation targetComp = target == null ? null : target.TryGetComp<CompYunjianziCultivation>();
            if (dinfo.Def.harmsHealth && targetComp?.WearingJiuxiaoXianxu == true)
                dinfo.SetAmount(dinfo.Amount * .5f);

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
            CompYunjianziCultivation comp = attacker == null ? null : attacker.TryGetComp<CompYunjianziCultivation>();
            if (comp == null || !comp.IsYunjianzi || !comp.HoldingZhuxie) return true;
            dinfo.SetAmount(dinfo.Amount * (comp.SwordDamage / 25f));
            // Buildings receive only the cultivated sword edge: no lightning
            // or other OnZhuxieHit pawn effects. Passable buildings take x4;
            // impassable buildings such as walls take x6 through DamageDef.
            if (__instance.def.category == ThingCategory.Building)
                dinfo.Def = YJZDefOf.YJZ_ZhuxieBuildingCut;
            if (comp.HasSkill(YunjianziSkills.ArmorIgnore)) dinfo.SetIgnoreArmor(true);
            return true;
        }
    }

    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldownTicks),
        new Type[] { typeof(Verb), typeof(Pawn) })]
    internal static class Patch_VerbProperties_AdjustedCooldownTicks_Zhuxie
    {
        private static void Postfix(Verb ownerVerb, Pawn attacker, ref int __result)
        {
            if (ownerVerb == null || ownerVerb.EquipmentSource == null ||
                ownerVerb.EquipmentSource.def != YJZDefOf.YJZ_Weapon_Zhuxie) return;
            CompYunjianziCultivation comp = attacker == null
                ? null
                : attacker.TryGetComp<CompYunjianziCultivation>();
            if (comp == null || !comp.IsYunjianzi || !comp.HoldingZhuxie) return;

            // Preserve every external cooldown multiplier already applied by
            // vanilla while replacing Zhuxie's two-second base interval with
            // its current realm interval.
            __result = Mathf.Max(1, Mathf.RoundToInt(__result * (comp.SwordCooldown / 2f)));
        }
    }
}
