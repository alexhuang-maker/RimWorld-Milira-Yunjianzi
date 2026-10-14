using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziShenjiGearPolicy
    {
        internal static int RequiredModel(string gear)
        {
            switch (gear)
            {
                case "YJZ_Apparel_ShenjiArmor_Base": return 1;
                case "YJZ_Apparel_ShenjiArmor_Intermediate":
                case "YJZ_Apparel_ShenjiVisor": return 2;
                case "YJZ_Apparel_ShenjiArmor_Final":
                case "YJZ_Apparel_JiuxiaoImmortalCrown": return 3;
                default: return 0;
            }
        }

        internal static bool IsShenjiGun(string gear)
        {
            return gear == "YJZ_Weapon_Shenji_Assault" || gear == "YJZ_Weapon_Shenji_Gatling" ||
                gear == "YJZ_Weapon_Shenji_Gauss" || gear == "YJZ_Weapon_Shenji_Legendary";
        }

        internal static int Model(string race)
        {
            switch (race)
            {
                case "YJZ_Shenji_Race": return 1;
                case "YJZ_Shenji_Race_Lingwei": return 2;
                case "YJZ_Shenji_Race_Tianjiang": return 3;
                default: return 0;
            }
        }

        internal static bool Allows(string gear, string race)
        {
            int required = RequiredModel(gear);
            if (required != 0) return Model(race) == required;
            return !IsShenjiGun(gear) || Model(race) != 0;
        }

        internal static bool Allows(ThingDef gear, Pawn pawn)
        {
            return Allows(gear?.defName, pawn?.def?.defName);
        }

        internal static string RejectionReason(ThingDef gear)
        {
            switch (RequiredModel(gear?.defName))
            {
                case 1: return "仅青羽机侍可以穿戴。";
                case 2: return "仅玄翎灵卫可以穿戴。";
                case 3: return "仅九霄天将可以穿戴。";
                default: return "神机专用枪械，仅青羽机侍、玄翎灵卫和九霄天将可以装备。";
            }
        }

        internal static void PreserveRejectedItem(ThingWithComps item, Pawn pawn)
        {
            // Some external equip jobs detach/despawn the item before calling
            // the tracker. Reject the equip without losing the physical item.
            if (item == null || item.Destroyed || item.Spawned || item.ParentHolder != null) return;
            if (pawn?.MapHeld != null && GenPlace.TryPlaceThing(item, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near)) return;
            pawn?.inventory?.innerContainer.TryAdd(item);
        }
    }

    [HarmonyPatch]
    internal static class Patch_ShenjiGear_CanEquip
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
                new[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) });
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (YunjianziShenjiGearPolicy.Allows(thing?.def, pawn)) return;
            __result = false;
            cantReason = YunjianziShenjiGearPolicy.RejectionReason(thing?.def);
        }
    }

    [HarmonyPatch(typeof(Apparel), nameof(Apparel.PawnCanWear))]
    internal static class Patch_ShenjiGear_PawnCanWear
    {
        private static void Postfix(Apparel __instance, Pawn pawn, ref bool __result)
        {
            if (!YunjianziShenjiGearPolicy.Allows(__instance.def, pawn)) __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
    internal static class Patch_ShenjiGear_Wear
    {
        private static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            if (YunjianziShenjiGearPolicy.Allows(newApparel?.def, __instance.pawn)) return true;
            YunjianziShenjiGearPolicy.PreserveRejectedItem(newApparel, __instance.pawn);
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    internal static class Patch_ShenjiGear_AddEquipment
    {
        private static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            if (YunjianziShenjiGearPolicy.Allows(newEq?.def, __instance.pawn)) return true;
            YunjianziShenjiGearPolicy.PreserveRejectedItem(newEq, __instance.pawn);
            return false;
        }
    }

    // The native Milira selector bypasses EquipmentUtility.CanEquip. Apply the
    // same policy there so ordinary Milians cannot select Shenji-only guns.
    [HarmonyPatch]
    internal static class Patch_ShenjiGear_MiliraWeaponTarget
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("Milira.CompTargetableWeapon:Available");
        }

        private static bool Prepare() { return TargetMethod() != null; }

        private static void Postfix(Thing thing, Pawn pawn, ref bool __result)
        {
            if (!YunjianziShenjiGearPolicy.Allows(thing?.def, pawn)) __result = false;
        }
    }
}
