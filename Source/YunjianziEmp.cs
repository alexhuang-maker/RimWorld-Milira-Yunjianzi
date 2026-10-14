using RimWorld;
using Verse;

namespace Yunjianzi
{
    internal static class YunjianziEmp
    {
        // EMP potency is independent of lethal damage (especially the 1200+ spear).
        // Vanilla handles shields, EMPResistance, adaptation and DisabledByEMP.
        internal const float LightningDamage = 12f;
        internal const float ChainDamage = 6f;
        internal const float SpearDamage = 30f;

        internal static void Apply(Thing target, Thing caster, float amount)
        {
            if (target == null || target.Destroyed || !target.Spawned || amount <= 0f ||
                (target is Pawn pawn && pawn.Dead)) return;
            target.TakeDamage(new DamageInfo(DamageDefOf.EMP, amount, 0f, -1f, caster));
        }
    }
}
