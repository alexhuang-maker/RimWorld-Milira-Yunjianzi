using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    public sealed class FixedSkillSpec
    {
        public SkillDef skill;
        public int level;
        public Passion passion = Passion.None;
    }

    public sealed class FixedPawnTemplateExtension : DefModExtension
    {
        public List<FixedSkillSpec> skills = new List<FixedSkillSpec>();
    }

    [StaticConstructorOnStartup]
    internal static class YunjianziBootstrap
    {
        static YunjianziBootstrap()
        {
            new Harmony("alex.miliraprivateexpansion.yunjianzi.fixedtemplate").PatchAll();
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyAfter("Ariandel.AriandelLibrary.Patch")]
    [HarmonyPriority(Priority.Last)]
    internal static class Patch_PawnGenerator_GeneratePawn
    {
        private static void Postfix(Pawn __result)
        {
            if (__result == null || __result.kindDef == null || __result.skills == null)
            {
                return;
            }

            FixedPawnTemplateExtension extension = __result.kindDef.GetModExtension<FixedPawnTemplateExtension>();
            if (extension == null || extension.skills == null)
            {
                return;
            }

            foreach (FixedSkillSpec spec in extension.skills)
            {
                if (spec == null || spec.skill == null)
                {
                    continue;
                }

                SkillRecord record = __result.skills.GetSkill(spec.skill);
                record.levelInt = Math.Max(0, Math.Min(20, spec.level));
                record.passion = spec.passion;
                record.xpSinceLastLevel = 0f;
                record.xpSinceMidnight = 0f;
            }
        }
    }
}
