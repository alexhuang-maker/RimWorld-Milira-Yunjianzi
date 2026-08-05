using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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

    public sealed partial class CompYunjianziCultivation
    {
        private static readonly Dictionary<string, string> TalismanSkills = new Dictionary<string, string>
        {
            { "YJZ_Talisman_Zhuoyu", YunjianziSkills.TalismanZhuoyu },
            { "YJZ_Talisman_Shenjie", YunjianziSkills.TalismanShenjie },
            { "YJZ_Talisman_Shenxing", YunjianziSkills.TalismanShenxing },
            { "YJZ_Talisman_Jiqing", YunjianziSkills.TalismanJiqing },
            { "YJZ_Talisman_Baicao", YunjianziSkills.TalismanBaicao },
            { "YJZ_Talisman_Xinjian", YunjianziSkills.TalismanXinjian },
            { "YJZ_Talisman_Beidou", YunjianziSkills.TalismanBeidou },
            { "YJZ_Talisman_Guihai", YunjianziSkills.TalismanGuihai },
            { "YJZ_Talisman_Shenlian", YunjianziSkills.TalismanShenlian },
            { "YJZ_Talisman_Huijian", YunjianziSkills.TalismanHuijian },
            { "YJZ_Talisman_Bumei", YunjianziSkills.TalismanBumei },
            { "YJZ_Talisman_Jiexin", YunjianziSkills.TalismanJiexin },
            { "YJZ_Talisman_Liuyang", YunjianziSkills.TalismanLiuyang },
            { "YJZ_Talisman_Tiandao", YunjianziSkills.TalismanTiandao },
            { "YJZ_Talisman_Huanri", YunjianziSkills.TalismanHuanri }
        };

        private ThingOwner<Thing> equippedTalismans;
        private List<string> activeTalismanDefs = new List<string>();

        public int TalismanSlotCount { get { return Mathf.Clamp(Realm - 1, 0, 4); } }

        public ThingOwner GetDirectlyHeldThings()
        {
            EnsureTalismanState();
            return equippedTalismans;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        private void EnsureTalismanState()
        {
            if (equippedTalismans == null) equippedTalismans = new ThingOwner<Thing>(this, false, LookMode.Deep);
            if (activeTalismanDefs == null) activeTalismanDefs = new List<string>();
            while (activeTalismanDefs.Count < 4) activeTalismanDefs.Add(string.Empty);
            while (activeTalismanDefs.Count > 4) activeTalismanDefs.RemoveAt(activeTalismanDefs.Count - 1);

            for (int slot = TalismanSlotCount; slot < activeTalismanDefs.Count; slot++)
                if (!activeTalismanDefs[slot].NullOrEmpty()) UnequipTalisman(slot, false);
        }

        internal void ExposeTalismanData()
        {
            Scribe_Deep.Look(ref equippedTalismans, "yjz_equippedTalismans", this);
            Scribe_Collections.Look(ref activeTalismanDefs, "yjz_activeTalismanDefs", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) EnsureTalismanState();
        }

        public bool HasActiveTalisman(string defName)
        {
            EnsureTalismanState();
            return !defName.NullOrEmpty() && activeTalismanDefs.Take(TalismanSlotCount).Contains(defName);
        }

        internal IEnumerable<Gizmo> GetTalismanGizmos()
        {
            EnsureTalismanState();
            for (int slot = 0; slot < TalismanSlotCount; slot++)
            {
                int capturedSlot = slot;
                Thing active = ActiveTalismanThing(slot);
                Texture2D slotIcon = active == null ? YunjianziAbilityTextures.TalismanSlot :
                    ContentFinder<Texture2D>.Get(active.def.graphicData.texPath, false) ?? YunjianziAbilityTextures.TalismanSlot;
                yield return new CommandYunjianziTalismanSlot
                {
                    comp = this,
                    slot = capturedSlot,
                    defaultLabel = active == null ? "阳符位·" + (slot + 1) : "阳符位·" + (slot + 1) + "「" + active.LabelCap + "」",
                    defaultDesc = active == null
                        ? "空置阳符位。右键选择已经参悟并持有的符咒。"
                        : "当前生效：" + active.LabelCap + "。\n\n右键替换或卸下；被替换的实体符咒会返回云间子的物品栏。",
                    icon = slotIcon,
                    action = delegate { OpenTalismanMenu(capturedSlot); }
                };
            }
        }

        private Thing ActiveTalismanThing(int slot)
        {
            if (slot < 0 || slot >= activeTalismanDefs.Count || activeTalismanDefs[slot].NullOrEmpty()) return null;
            return equippedTalismans.FirstOrDefault(thing => thing.def.defName == activeTalismanDefs[slot]);
        }

        internal void OpenTalismanMenu(int slot)
        {
            EnsureTalismanState();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Thing current = ActiveTalismanThing(slot);
            if (current != null)
                options.Add(new FloatMenuOption("卸下「" + current.LabelCap + "」", delegate { UnequipTalisman(slot, true); }));

            HashSet<string> activeDefs = new HashSet<string>(activeTalismanDefs.Where(defName => !defName.NullOrEmpty()));
            foreach (Thing talisman in AvailableTalismans().Where(thing => !activeDefs.Contains(thing.def.defName))
                .GroupBy(thing => thing.def).Select(group => group.First()).OrderBy(thing => thing.LabelCap))
            {
                Thing captured = talisman;
                string location = captured.holdingOwner == (Pawn.inventory == null ? null : Pawn.inventory.innerContainer) ? "随身" : "地图";
                options.Add(new FloatMenuOption("装备「" + captured.LabelCap + "」  ·  " + location,
                    delegate { EquipTalisman(slot, captured); }, captured.def.uiIcon, Color.white));
            }

            if (options.Count == 0)
                options.Add(new FloatMenuOption("没有已参悟且可用的符咒", null));
            Find.WindowStack.Add(new FloatMenu(options, "阳符位·" + (slot + 1), false));
        }

        private IEnumerable<Thing> AvailableTalismans()
        {
            IEnumerable<Thing> carried = Pawn.inventory == null ? Enumerable.Empty<Thing>() : Pawn.inventory.innerContainer;
            IEnumerable<Thing> mapThings = Pawn.Spawned && Pawn.Map != null ? Pawn.Map.listerThings.AllThings : Enumerable.Empty<Thing>();
            return carried.Concat(mapThings).Where(thing => thing != null && IsTalisman(thing.def) && CanEquipTalisman(thing.def));
        }

        private static bool IsTalisman(ThingDef def)
        {
            return def != null && TalismanSkills.ContainsKey(def.defName);
        }

        private bool CanEquipTalisman(ThingDef def)
        {
            string skill;
            return def != null && TalismanSkills.TryGetValue(def.defName, out skill) && HasSkill(skill);
        }

        private void EquipTalisman(int slot, Thing source)
        {
            if (slot < 0 || slot >= TalismanSlotCount || source == null || !CanEquipTalisman(source.def)) return;
            UnequipTalisman(slot, false);

            Thing one = source.stackCount > 1 ? source.SplitOff(1) : source;
            if (one.Spawned) one.DeSpawn();
            if (!equippedTalismans.TryAddOrTransfer(one, false))
            {
                if (Pawn.Spawned) GenPlace.TryPlaceThing(one, Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
                Messages.Message("符咒未能收入符位。", Pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }
            activeTalismanDefs[slot] = one.def.defName;
            Messages.Message("阳符位·" + (slot + 1) + "已装备「" + one.LabelCap + "」。", Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private void UnequipTalisman(int slot, bool message)
        {
            if (slot < 0 || slot >= activeTalismanDefs.Count || activeTalismanDefs[slot].NullOrEmpty()) return;
            Thing active = ActiveTalismanThing(slot);
            activeTalismanDefs[slot] = string.Empty;
            if (active != null)
            {
                bool returned = Pawn != null && Pawn.inventory != null && Pawn.inventory.innerContainer.TryAddOrTransfer(active, false);
                if (!returned && Pawn != null && Pawn.Spawned)
                {
                    equippedTalismans.Remove(active);
                    GenPlace.TryPlaceThing(active, Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
                }
            }
            if (message) Messages.Message("阳符位·" + (slot + 1) + "已空置。", Pawn, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    public sealed class CommandYunjianziTalismanSlot : Command_Action
    {
        public CompYunjianziCultivation comp;
        public int slot;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Event current = Event.current;
            bool rightClicked = current != null && current.type == EventType.MouseDown && current.button == 1 &&
                new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f).Contains(current.mousePosition);
            GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
            if (!rightClicked || comp == null) return result;
            comp.OpenTalismanMenu(slot);
            current.Use();
            return new GizmoResult(GizmoState.Interacted, current);
        }
    }
}
