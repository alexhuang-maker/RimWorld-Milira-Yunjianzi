using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public sealed class Building_YunjianziStartingShenjiArk : Building, IOpenable
    {
        internal const string LockedHint = "暂时无法开启，先往前探索吧。";
        internal const string LockedMenuLabel = "暂时无法开启";
        private Pawn inheritor;
        private bool opened;
        // Missing in legacy saves means the original single ark keeps its gift.
        private bool containsCompanion=true;
        internal bool HasCompanionReward => containsCompanion;
        public void BindTo(Pawn pawn,bool hasCompanion=true) { inheritor=pawn; containsCompanion=hasCompanion; }
        public bool Opened => opened;
        public bool Unlocked => (inheritor ?? (Spawned ? Map.mapPawns.FreeColonistsSpawned
            .FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi) : null))
            ?.TryGetComp<CompYunjianziCultivation>()?.HasOpenedBoundScroll == true;
        public bool CanOpen => !opened && Spawned && Unlocked;
        public int OpenTicks => 180;
        public override Graphic Graphic => opened ? GraphicDatabase.Get<Graphic_Single>(
            "Milira/Building/Buildings_Misc/MilianCapsule_Empty",ShaderDatabase.Cutout,new Vector2(3,3),Color.white) : base.Graphic;
        public void Open()
        {
            if(!CanOpen)return;
            if(!containsCompanion)
            {
                opened=true;
                DirtyMapMesh(Map);
                Messages.Message("碎片方舟缓缓开启，内部空无一物。",this,MessageTypeDefOf.NeutralEvent);
                return;
            }
            Pawn owner=inheritor;
            if(owner==null || !owner.Spawned || owner.Map!=Map)
                owner=Map.mapPawns.FreeColonistsSpawned.FirstOrDefault(YunjianziSoleColonistUtility.IsYunjianzi);
            var part=Find.Scenario.AllParts.OfType<ScenPart_YunjianziStartingShenji>().FirstOrDefault();
            if(part?.GrantAt(owner,Position)!=true)return;
            opened=true;
            DirtyMapMesh(Map);
            Messages.Message("碎片方舟感应到传承之印，缓缓开启。沉眠的青羽机侍苏醒，加入云间子的行列。",this,MessageTypeDefOf.PositiveEvent);
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) => GetMultiSelectFloatMenuOptions(new[]{selPawn});
        public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach(var option in base.GetMultiSelectFloatMenuOptions(selPawns))yield return option;
            if(opened || !Spawned)yield break;
            if(!Unlocked){yield return new FloatMenuOption(LockedMenuLabel,()=>
                Messages.Message(LockedHint,this,MessageTypeDefOf.RejectInput,false));yield break;}
            Pawn opener=selPawns.FirstOrDefault(p=>p.Faction==Faction.OfPlayer && p.RaceProps.Humanlike &&
                p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && p.CanReserveAndReach(this,PathEndMode.Touch,Danger.Deadly));
            yield return new FloatMenuOption(opener==null?"无法开启碎片方舟":"开启碎片方舟",opener==null?null:
                ()=>{
                    var job=JobMaker.MakeJob(JobDefOf.Open,this);
                    job.ignoreDesignations=true;
                    opener.jobs.TryTakeOrderedJob(job,JobTag.Misc);
                });
        }
        public override void Notify_ThingSelected()
        {
            base.Notify_ThingSelected();
            if(Spawned && !opened && !Unlocked)
                Messages.Message(LockedHint,this,MessageTypeDefOf.RejectInput,false);
        }
        public override string GetInspectString() => opened?
            (containsCompanion?"方舟已开启，青羽机侍已苏醒。":"方舟已开启，内部空无一物。"):
            !Unlocked?LockedHint:
            "传承之印已解开封锁。选中云间子，右键开启碎片方舟，查看其中留存之物。";
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref inheritor,"yjz_arkInheritor");
            Scribe_Values.Look(ref opened,"yjz_arkOpened");
            Scribe_Values.Look(ref containsCompanion,"yjz_arkContainsCompanion",true);
        }
    }
}
