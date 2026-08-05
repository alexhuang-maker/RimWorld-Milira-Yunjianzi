using System.Linq;
using RimWorld;
using Verse;

namespace Yunjianzi
{
    public sealed class YunjianziTalismanRecipeExtension : DefModExtension
    {
        public string skillId;
    }

    public sealed class RecipeWorker_YunjianziTalisman : RecipeWorker
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part)) return false;
            YunjianziTalismanRecipeExtension extension = recipe.GetModExtension<YunjianziTalismanRecipeExtension>();
            if (extension == null || extension.skillId.NullOrEmpty() || thing == null || thing.Map == null) return false;

            return thing.Map.mapPawns.AllPawnsSpawned.Any(pawn =>
                pawn.Faction == Faction.OfPlayer &&
                pawn.TryGetComp<CompYunjianziCultivation>() != null &&
                pawn.TryGetComp<CompYunjianziCultivation>().HasSkill(extension.skillId));
        }

        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null)
        {
            if (AvailableOnNow(thing, part)) return true;
            return "需要云间子先在修行面板中参悟对应符咒。";
        }
    }
}
