using RimWorld;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    public sealed partial class CompYunjianziShenji
    {
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            int now = Find.TickManager.TicksGame;
            if (dinfo.Def == null || dinfo.Amount <= 0f || shield <= 0f ||
                ShieldMax <= 0f || now < shieldBrokenUntil) return;

            // EMP does not harm health, so it must be handled before that filter.
            bool emp = dinfo.Def == DamageDefOf.EMP;
            if (!emp && !dinfo.Def.harmsHealth) return;
            bool darkMatter = Cultivation != null && Cultivation.HasSkill(YunjianziSkills.ShenjiDarkShield);
            if (emp && darkMatter)
            {
                absorbed = true;
                dinfo.SetAmount(0f);
                lastShieldHitTick = now;
                return;
            }

            // Absence of isRanged is not evidence of melee: vanilla Bomb and
            // several mod energy attacks omit it. Follow the actual weapon /
            // melee tool metadata, as Milira Imperium's deflector does.
            bool melee = dinfo.Tool != null || dinfo.WeaponBodyPartGroup != null ||
                dinfo.WeaponLinkedHediff != null || dinfo.Weapon?.IsMeleeWeapon == true;
            float cost = emp ? shield : darkMatter ? (melee ? 50f : 10f) : (melee ? 300f : 50f);
            shield = Mathf.Max(0f, shield - cost);

            // A deflection is a whole hit, including the hit that depletes the
            // last energy. Never leak a percentage of an arbitrarily large hit.
            // Solar EMP deliberately passes through after disrupting the shield.
            absorbed = !emp;
            if (absorbed) dinfo.SetAmount(0f);
            lastShieldHitTick = now;
            if (shield <= 0f)
            {
                shieldBrokenUntil = now + 1200;
                TryTriggerCloudCommandBurst();
            }
            if (Pawn != null && Pawn.Spawned)
                FleckMaker.Static(Pawn.Position, Pawn.Map, FleckDefOf.PsycastAreaEffect, .45f);
        }
    }
}
