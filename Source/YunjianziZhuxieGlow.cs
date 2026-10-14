using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    [StaticConstructorOnStartup]
    internal static class ZhuxieGlow
    {
        private static readonly Texture2D Source;
        private static readonly Material Lit;
        private static readonly Material Glow;
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();

        static ZhuxieGlow()
        {
            Source = ContentFinder<Texture2D>.Get("Yunjianzi/Weapons/Zhuxie", false);
            if (Source == null) return;
            try
            {
                Texture2D mask = YunjianziTrimGlow.CreateMask(Source, p =>
                {
                    p.a = ZhuxieGlowTuning.MaskAlpha(p.r, p.g, p.b, p.a);
                    return p;
                });
                if (mask == null) return;
                Lit = MaterialPool.MatFrom(mask, ShaderDatabase.TransparentPostLight, Color.white);
                Glow = MaterialPool.MatFrom(mask, ShaderDatabase.MoteGlow, Color.white);
            }
            catch (Exception e)
            {
                Log.Warning("[Yunjianzi] Could not prepare Zhuxie detail glow: " + e.Message);
            }
        }

        internal static void DrawEquipment(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Thing eq)
        {
            // Keep the original draw, including all vanilla/earlier mod changes
            // to flip, aim, recoil and size. Other equipment gets no extra pass.
            Graphics.DrawMesh(mesh, matrix, material, layer);
            if (eq?.def != YJZDefOf.YJZ_Weapon_Zhuxie || Glow == null ||
                material == null || material.mainTexture != Source || material.shader == ShaderDatabase.Invisible) return;
            Pawn pawn = (eq.ParentHolder as Pawn_EquipmentTracker)?.pawn;
            if (pawn == null || pawn.Dead) return;
            CompYunjianziCultivation cultivation = pawn.TryGetComp<CompYunjianziCultivation>();
            int realm = cultivation?.IsYunjianzi == true ? cultivation.Realm : 0;
            float lit = ZhuxieGlowTuning.LitAlpha(realm);
            if (lit > 0f && Lit != null)
            {
                Properties.SetColor(ShaderPropertyIDs.Color, new Color(1f, 1f, 1f, lit));
                Graphics.DrawMesh(mesh, matrix, Lit, layer, null, 0, Properties);
            }
            Properties.SetColor(ShaderPropertyIDs.Color,
                new Color(1f, 1f, 1f, ZhuxieGlowTuning.GlowAlpha(realm)));
            Graphics.DrawMesh(mesh, matrix, Glow, layer, null, 0, Properties);
        }
    }

    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    internal static class Patch_Zhuxie_DetailGlow
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = instructions.ToList();
            var draw = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
                new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });
            // Fail softly if another renderer replaces the native draw entirely.
            // Never partially rewrite an unfamiliar weapon draw method.
            if (original.Count(instruction => instruction.Calls(draw)) != 1)
            {
                Log.Warning("[Yunjianzi] Zhuxie glow skipped: equipment renderer has no unique native DrawMesh call.");
                return original;
            }
            var result = new List<CodeInstruction>();
            foreach (CodeInstruction instruction in original)
            {
                if (instruction.Calls(draw))
                {
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction));
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(ZhuxieGlow), nameof(ZhuxieGlow.DrawEquipment));
                }
                result.Add(instruction);
            }
            return result;
        }
    }
}
