using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    // Ariandel's entry grid uses an old static image while its detail panel uses
    // the real pawn portrait. Replace only Yunjianzi's recruited-image draw,
    // keeping Ariandel's locked icon, background, cooldown and interaction UI.
    [HarmonyPatch]
    internal static class Patch_YunjianziSpecialPawnIcon
    {
        private static MethodInfo drawEntry;
        private static MethodInfo getPortrait;
        private static MethodInfo getGameComponent;
        private static MethodInfo getSpecialPawns;
        private static MethodInfo getRegisteredPawn;
        private static FieldInfo entryId;
        private static FieldInfo normalTexture;
        private static FieldInfo stateCache;
        private static FieldInfo statePawn;
        private static FieldInfo stateRegistered;

        private static bool Prepare()
        {
            Type windowType = AccessTools.TypeByName("AriandelLibrary.AL_SpecialPawnManager");
            Type entryType = AccessTools.TypeByName("AriandelLibrary.SpecialPawnEntry");
            Type componentType = AccessTools.TypeByName("AriandelLibrary.AriandelLibrary_GameComponent");
            Type managerType = AccessTools.TypeByName("AriandelLibrary.SpecialPawnManager");
            if (windowType == null || entryType == null || componentType == null || managerType == null) return false;
            drawEntry = AccessTools.DeclaredMethod(windowType, "DrawEntryIcon", new[] { typeof(Rect), entryType, typeof(float) });
            getPortrait = AccessTools.DeclaredMethod(windowType, "GetPawnPortrait", new[] { typeof(Pawn) });
            getGameComponent = AccessTools.PropertyGetter(componentType, "Instance");
            getSpecialPawns = AccessTools.PropertyGetter(componentType, "SpecialPawns");
            getRegisteredPawn = AccessTools.DeclaredMethod(managerType, "GetPawnByStaticID", new[] { typeof(string) });
            entryId = AccessTools.Field(entryType, "uniqueID");
            normalTexture = AccessTools.Field(entryType, "texNormal");
            stateCache = AccessTools.Field(windowType, "frameStateCache");
            Type stateType = stateCache?.FieldType.IsGenericType == true
                ? stateCache.FieldType.GetGenericArguments()[1] : null;
            statePawn = stateType == null ? null : AccessTools.Field(stateType, "pawn");
            stateRegistered = stateType == null ? null : AccessTools.Field(stateType, "isRegistered");
            bool supported = drawEntry != null && getPortrait != null && entryId != null &&
                normalTexture != null && statePawn != null && stateRegistered != null &&
                getGameComponent != null && getSpecialPawns != null && getRegisteredPawn != null;
            if (!supported) Log.Warning("[Yunjianzi] Special pawn icon API changed; retaining the static icon.");
            return supported;
        }

        private static MethodBase TargetMethod() => drawEntry;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawTexture = AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture),
                new[] { typeof(Rect), typeof(Texture), typeof(ScaleMode), typeof(bool) });
            MethodInfo replacement = AccessTools.DeclaredMethod(typeof(Patch_YunjianziSpecialPawnIcon), nameof(DrawTexture));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(drawTexture))
                {
                    // Append the window and entry to the original draw arguments.
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }

        private static void DrawTexture(Rect rect, Texture texture, ScaleMode mode, bool alphaBlend, object window, object entry)
        {
            GUI.DrawTexture(rect, ResolveTexture(texture, window, entry), mode, alphaBlend);
        }

        internal static Texture ResolveTexture(Texture original, object window, object entry)
        {
            if (Event.current?.type != EventType.Repaint || entry == null || window == null ||
                (string)entryId.GetValue(entry) != "Yunjianzi" ||
                !ReferenceEquals(original, normalTexture.GetValue(entry))) return original;

            IDictionary states = stateCache.GetValue(window) as IDictionary;
            object state = states?["Yunjianzi"];
            if (state == null || !(bool)stateRegistered.GetValue(state)) return original;
            Pawn pawn = statePawn.GetValue(state) as Pawn;
            if (pawn == null)
            {
                // RefreshFrameState only resolves the selected entry's pawn.
                // Unselected registered entries need the same registry lookup
                // used by the detail panel, independent of current selection.
                object component = getGameComponent.Invoke(null, null);
                object manager = component == null ? null : getSpecialPawns.Invoke(component, null);
                if (manager != null)
                    pawn = getRegisteredPawn.Invoke(manager, new object[] { "Yunjianzi" }) as Pawn;
            }
            if (pawn == null || pawn.Destroyed) return original;

            // The manager already handles portrait caching/render failures and
            // renders clothes, hair and wings identically to the detail panel.
            return getPortrait.Invoke(window, new object[] { pawn }) as Texture ?? original;
        }
    }
}
