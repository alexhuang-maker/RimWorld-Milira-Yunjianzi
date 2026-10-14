using RimWorld;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed class CompProperties_JiuxiaoXianxu : CompProperties
    {
        public float drawScale = 1.50f;
        public float drawOffsetZ = -0.08f;

        public CompProperties_JiuxiaoXianxu()
        {
            compClass = typeof(CompJiuxiaoXianxu);
        }
    }

    /// <summary>Draws a floating celestial silk without replacing the robe or wings.</summary>
    [StaticConstructorOnStartup]
    public sealed class CompJiuxiaoXianxu : ThingComp
    {
        private CompProperties_JiuxiaoXianxu Props => (CompProperties_JiuxiaoXianxu)props;

        public override void CompDrawWornExtras()
        {
            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            Rot4 rot = pawn.Rotation;
            JiuxiaoXianxuDrawer.Draw(pawn, pawn.DrawPos, rot, true,
                Props.drawScale, Props.drawOffsetZ, true);
        }
    }

    [StaticConstructorOnStartup]
    internal static class JiuxiaoXianxuDrawer
    {
        private const string Root = "Yunjianzi/Apparel/JiuxiaoXianxu/JiuxiaoXianxu_";
        private static readonly Material[] Back = Load("Back");
        private static readonly Material[] Front = Load("Front");
        private static readonly MaterialPropertyBlock Dimmed = CreatePropertyBlock();

        private static MaterialPropertyBlock CreatePropertyBlock()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(ShaderPropertyIDs.Color, new Color(.94f, .96f, .95f, .92f));
            return block;
        }

        private static Material[] Load(string layer)
        {
            // Rot4.AsInt is North, East, South, West. The final side source is
            // authored in world-facing orientation, so use the matching material.
            return new[] { "north", "east", "south", "west" }
                .Select(dir => MaterialPool.MatFrom(Root + layer + "_" + dir, ShaderDatabase.Transparent)).ToArray();
        }

        internal static bool TryGet(Pawn pawn, out CompJiuxiaoXianxu comp)
        {
            comp = pawn?.apparel?.WornApparel?.Select(a => a.TryGetComp<CompJiuxiaoXianxu>()).FirstOrDefault(c => c != null);
            return comp != null;
        }

        internal static void Draw(Pawn pawn, Vector3 drawLoc, Rot4 rot, bool front,
            float scale, float offsetZ, bool overPawn)
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            float phase = ticks * .035f + pawn.thingIDNumber * .17f;
            float animatedScale = scale + Mathf.Sin(phase) * .035f;
            float horizontalOffset = 0f;
            // Worn extras need to sit above high-profile furniture such as the
            // white-jade cultivation platform. The separate back pass remains
            // below the pawn so the central jade bi-disc is still occluded south.
            float altitudeOffset = overPawn ? (front ? .285f : .245f) : -.035f;
            Vector3 loc = drawLoc + new Vector3(horizontalOffset, altitudeOffset,
                offsetZ + Mathf.Cos(phase * .52f) * .018f);
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity,
                new Vector3(animatedScale, 1f, animatedScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, (front ? Front : Back)[rot.AsInt],
                0, null, 0, Dimmed);
            XianxuParticles.Draw(pawn, loc, rot, front, animatedScale, ticks);
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt),
        new[] { typeof(Vector3), typeof(Rot4?), typeof(bool) })]
    internal static class Patch_PawnRenderer_RenderPawnAt_JiuxiaoXianxuBack
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        private static void Prefix(PawnRenderer __instance, Vector3 drawLoc, Rot4? rotOverride)
        {
            Pawn pawn = PawnRef(__instance);
            if (pawn == null || pawn.Dead || !JiuxiaoXianxuDrawer.TryGet(pawn, out CompJiuxiaoXianxu comp)) return;
            Rot4 rot = rotOverride ?? pawn.Rotation;
            JiuxiaoXianxuDrawer.Draw(pawn, drawLoc, rotOverride ?? pawn.Rotation, false,
                ((CompProperties_JiuxiaoXianxu)comp.props).drawScale,
                ((CompProperties_JiuxiaoXianxu)comp.props).drawOffsetZ, false);
        }
    }
}
