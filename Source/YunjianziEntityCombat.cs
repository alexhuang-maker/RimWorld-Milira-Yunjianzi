using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Yunjianzi
{
    public class CompProperties_YunjianziEntity : CompProperties
    {
        public int role;
        public CompProperties_YunjianziEntity() : base(typeof(CompYunjianziEntity)) { }
    }
    public class CompYunjianziEntity : ThingComp
    {
        public int Role => ((CompProperties_YunjianziEntity)props).role;
        public Pawn Pawn => (Pawn)parent;
        public int nextSkill, windupUntil, motionStart=-1000, motionEnd, linkUntil;
        public IntVec3 aim=IntVec3.Invalid;
        public Vector3 motionDirection;
        public List<IntVec3> strikes=new List<IntVec3>();
        public Pawn linked;
        public bool rewardClaimed;
        int Now => Find.TickManager.TicksGame;
        public const int HunterCloakTicks=240, HunterCloakCooldown=1080;
        Hediff HunterCloak => Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("YJZ_EntityHunterCloak"));
        public void BreakHunterCloak()
        {
            if(Role!=2)return;
            var cloak=HunterCloak;
            if(cloak==null)return;
            cloak.TryGetComp<HediffComp_Invisibility>()?.BecomeVisible(true);
            Pawn.health.RemoveHediff(cloak);
        }
        bool TryHunterCloak()
        {
            if(Role!=2 || !Active || Now<nextSkill || HunterCloak!=null || Pawn.IsBurning()
                || Pawn.stances?.stunner.Stunned==true || Pawn.health.hediffSet.HasHediff(HediffDefOf.DisruptorFlash)
                || Pawn.health.hediffSet.HasHediff(HediffDefOf.CoveredInFirefoam))return false;
            if(!Pawn.Map.mapPawns.AllPawnsSpawned.Any(p=>!p.Dead && !p.Downed && p.HostileTo(Pawn)
                && p.Position.DistanceTo(Pawn.Position)>2 && p.Position.DistanceTo(Pawn.Position)<=18
                && GenSight.LineOfSight(Pawn.Position,p.Position,Pawn.Map)))return false;
            nextSkill=Now+HunterCloakCooldown;
            EntityVisuals.Pulse(Pawn.Map,Pawn.DrawPos,.65f);
            Pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("YJZ_EntityHunterCloak"));
            return true;
        }
        public override void PostPostApplyDamage(DamageInfo dinfo,float totalDamageDealt)
        {
            if(totalDamageDealt>0 && Role==2)BreakHunterCloak();
        }
        public bool Active => Pawn.Spawned && !Pawn.Dead && !Pawn.Downed;
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref nextSkill,"nextSkill"); Scribe_Values.Look(ref windupUntil,"windupUntil");
            Scribe_Values.Look(ref aim,"aim",IntVec3.Invalid); Scribe_Collections.Look(ref strikes,"strikes",LookMode.Value);
            Scribe_Values.Look(ref rewardClaimed,"rewardClaimed");
            if(strikes==null)strikes=new List<IntVec3>();
        }
        public void Animate(Vector3 toward,int duration=25)
        {
            motionStart=Now;motionEnd=Now+duration;motionDirection=toward-Pawn.DrawPos;motionDirection.y=0;
            if(motionDirection.sqrMagnitude>.001f)motionDirection.Normalize();
        }
        public override void CompTick()
        {
            if(!Active) { if(Pawn.Spawned)BreakHunterCloak();windupUntil=0;strikes.Clear();return; }
            if(Pawn.stances?.stunner.Stunned==true) {BreakHunterCloak();windupUntil=0;strikes.Clear();return;}
            if(Role==6){YunjianziMobileMortar.Tick(this);return;}
            if(windupUntil>0)
            {
                Pawn.pather?.StopDead();
                if(Now>=windupUntil) {windupUntil=0;ReleaseSkill();}
                return;
            }
            if(Now<nextSkill || !parent.IsHashIntervalTick(30))return;
            if(Role==2) {TryHunterCloak();return;}
            if(Role==4) {Mend();return;}
            if(Role!=0 && Role!=5 && Role!=7)return;
            float range=Role==0?5:Role==5?5.5f:25;
            var target=Pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>!p.Dead && !p.Downed && p.HostileTo(Pawn)
                && p.Position.DistanceTo(Pawn.Position)<=range && GenSight.LineOfSight(Pawn.Position,p.Position,Pawn.Map))
                .OrderBy(p=>p.Position.DistanceToSquared(Pawn.Position)).FirstOrDefault();
            if(target==null)return;
            aim=target.Position;strikes.Clear();
            if(Role==7)
            {
                strikes.Add(aim);
                foreach(var p in Pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>p!=target && p.HostileTo(Pawn) && !p.Dead
                    && p.Position.DistanceTo(Pawn.Position)<=range && GenSight.LineOfSight(Pawn.Position,p.Position,Pawn.Map)).Take(2)) strikes.Add(p.Position);
            }
            windupUntil=Now+(Role==0?48:Role==5?105:150);
            nextSkill=windupUntil+(Role==0?600:Role==5?1000:1200);
            Pawn.rotationTracker.FaceCell(aim); Pawn.pather.StopDead();
            Pawn.stances.SetStance(new Stance_Cooldown(windupUntil-Now,new LocalTargetInfo(aim),null));
        }
        void ReleaseSkill()
        {
            if(!aim.InBounds(Pawn.Map) || !GenSight.LineOfSight(Pawn.Position,aim,Pawn.Map))return;
            Animate(aim.ToVector3Shifted(),Role==7?50:32);
            if(Role==7)
            {
                foreach(var cell in strikes.Where(c=>c.InBounds(Pawn.Map)))
                {
                    // Snapshot cells are fixed during the warning; players can leave them.
                    ImpactAt(cell,2.2f,60,DamageDefOf.Blunt);
                    Building_YunjianziTombstone.Fall(Pawn.Map,cell,Pawn);
                }
                strikes.Clear();return;
            }
            if(Role==0)
            {
                var cells=GenSight.PointsOnLineOfSight(Pawn.Position,aim).ToList();
                IntVec3 landing=Pawn.Position;
                foreach(var c in cells.Skip(1))
                {
                    if(!c.InBounds(Pawn.Map)||!c.Standable(Pawn.Map)||c.GetEdifice(Pawn.Map)!=null||c.GetFirstPawn(Pawn.Map)!=null)break;
                    landing=c;
                }
                if(landing!=Pawn.Position) {Pawn.Position=landing;Pawn.Notify_Teleported();}
                foreach(var p in Pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>p.HostileTo(Pawn)&&p.Position.DistanceTo(Pawn.Position)<=1.5f).ToList())
                    p.TakeDamage(new DamageInfo(DamageDefOf.Cut,26,.75f,-1,Pawn));
            }
            else ImpactAt(aim,2.4f,48,DamageDefOf.Blunt);
            FleckMaker.ThrowDustPuffThick(Pawn.DrawPos,Pawn.Map,1.4f,new Color(.4f,.23f,.24f));
        }
        void ImpactAt(IntVec3 cell,float radius,float damage,DamageDef def)
        {
            foreach(var p in Pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>p.HostileTo(Pawn) && !p.Dead
                && p.Position.DistanceTo(cell)<=radius && GenSight.LineOfSight(cell,p.Position,Pawn.Map)).ToList())
                p.TakeDamage(new DamageInfo(def,damage,.65f,-1,Pawn));
            FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(),Pawn.Map,radius,new Color(.45f,.24f,.27f));
            EntityVisuals.Pulse(Pawn.Map,cell.ToVector3Shifted(),radius);
        }
        void Mend()
        {
            var target=Pawn.Map.mapPawns.AllPawnsSpawned.Where(p=>p!=Pawn && !p.Dead && p.Faction==Pawn.Faction
                && p.TryGetComp<CompYunjianziEntity>() is CompYunjianziEntity c && c.Role!=4 && c.Role!=7
                && p.Position.DistanceTo(Pawn.Position)<10 && GenSight.LineOfSight(Pawn.Position,p.Position,Pawn.Map)
                && p.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any()).OrderBy(p=>p.health.summaryHealth.SummaryHealthPercent).FirstOrDefault();
            if(target==null)return;
            // Shared target cooldown prevents stacking several support units into instant full heals.
            var other=target.TryGetComp<CompYunjianziEntity>();
            if(other.linkUntil>Now)return;
            other.linkUntil=Now+240;linked=target;linkUntil=Now+100;nextSkill=Now+720;
            float budget=18;
            foreach(var injury in target.health.hediffSet.hediffs.OfType<Hediff_Injury>().Where(h=>!h.IsPermanent()).ToList())
            {float amount=Math.Min(budget,injury.Severity);injury.Heal(amount);budget-=amount;if(budget<=0)break;}
            Animate(target.DrawPos,45);
        }
        public override void PostPreApplyDamage(ref DamageInfo dinfo,out bool absorbed)
        {
            absorbed=false;
            if(Role!=1 || !Active || dinfo.Instigator==null || !dinfo.Def.isRanged)return;
            Vector3 dir=dinfo.Instigator.DrawPos-Pawn.DrawPos;dir.y=0;
            if(dir.sqrMagnitude>.001f && Vector3.Dot(dir.normalized,Pawn.Rotation.FacingCell.ToVector3())>.45f)
            {dinfo.SetAmount(dinfo.Amount*.4f);EntityVisuals.Pulse(Pawn.Map,Pawn.DrawPos,.8f);}
        }
        public override void PostDraw()
        {
            if(!Active)return;
            if(windupUntil>Now)
            {
                if(Role==7)foreach(var c in strikes)
                {
                    EntityVisuals.Ring(c.ToVector3Shifted(),2.2f);
                    if(windupUntil-Now<=45)EntityVisuals.FallingStone(c.ToVector3Shifted(),windupUntil-Now);
                }
                else if(Role==5)EntityVisuals.Ring(aim.ToVector3Shifted(),2.4f);
                else if(Role==6)EntityVisuals.Ring(aim.ToVector3Shifted(),2.8f);
                else GenDraw.DrawLineBetween(Pawn.DrawPos,aim.ToVector3Shifted(),EntityVisuals.Red,.10f);
            }
            if(Role==4 && linked!=null && linked.Spawned && linked.Map==Pawn.Map && linkUntil>Now)
                GenDraw.DrawLineBetween(Pawn.DrawPos,linked.DrawPos,EntityVisuals.Red,.08f);
            var stance=Pawn.stances?.curStance as Stance_Warmup;
            if((Role==3 || Role==6) && stance!=null && stance.focusTarg.IsValid)
            {
                if(Role==6)EntityVisuals.Ring(stance.focusTarg.CenterVector3,2.9f);
                else GenDraw.DrawLineBetween(Pawn.DrawPos,stance.focusTarg.CenterVector3,EntityVisuals.Red,.045f);
            }
        }
    }
    public class Verb_YunjianziEntityShot : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            bool fired=base.TryCastShot();
            if(fired && CasterIsPawn)
            {
                CasterPawn.TryGetComp<CompYunjianziEntity>()?.Animate(currentTarget.CenterVector3,30);
                FleckMaker.ThrowDustPuffThick(CasterPawn.DrawPos,CasterPawn.Map,.7f,new Color(.45f,.15f,.19f));
            }
            return fired;
        }
    }
    [HarmonyPatch(typeof(Verb_MeleeAttack),"TryCastShot")]
    public static class Patch_YunjianziEntityMelee
    {
        static void Prefix(Verb_MeleeAttack __instance)
            => __instance.CasterPawn?.TryGetComp<CompYunjianziEntity>()?.BreakHunterCloak();
        static void Postfix(Verb_MeleeAttack __instance,bool __result)
        {
            var comp=__instance.CasterPawn?.TryGetComp<CompYunjianziEntity>();
            if(comp==null || !comp.Active)return;
            var target=__instance.CurrentTarget;
            comp.Animate(target.CenterVector3);
            if(__result)
            {
                EntityVisuals.Pulse(comp.Pawn.Map,target.CenterVector3,.55f);
                if(comp.Role==2 && target.Pawn!=null)
                {
                    var slow=DefDatabase<HediffDef>.GetNamed("YJZ_EntityTendonSnare");
                    if(!target.Pawn.health.hediffSet.HasHediff(slow))target.Pawn.health.AddHediff(slow);
                }
            }
        }
    }
    [HarmonyPatch(typeof(AnomalyIncidentUtility),nameof(AnomalyIncidentUtility.PawnShardOnDeath))]
    public static class Patch_YunjianziEntityNoShard
    {static bool Prefix(Pawn pawn)=>pawn.TryGetComp<CompYunjianziEntity>()==null;}

    // Vanilla TryGetAttackVerb considers equipment and melee, not race ranged verbs.
    // Expose the sniper to native ranged AI; the mobile mortar owns its separate firing timer.
    [HarmonyPatch(typeof(Pawn),nameof(Pawn.TryGetAttackVerb))]
    public static class Patch_YunjianziEntityAttackVerb
    {
        static void Postfix(Pawn __instance,Thing target,ref Verb __result)
        {
            var c=__instance.TryGetComp<CompYunjianziEntity>();
            if(c==null || c.Role!=3 || !c.Active)return;
            var ranged=__instance.verbTracker.AllVerbs.FirstOrDefault(v=>v is Verb_YunjianziEntityShot && v.Available());
            if(ranged!=null && (target==null || target.Position.DistanceTo(__instance.Position)>=ranged.verbProps.minRange))__result=ranged;
        }
    }

    public class Pawn_YunjianziEntity : Pawn
    {
        public override void Kill(DamageInfo? dinfo,Hediff exactCulprit=null)
        {
            if(Dead)return;
            // No corpse Def, no equipment, no inventory and no incident-added shard.
            if(Spawned)EntityVisuals.Pulse(Map,DrawPos,this.TryGetComp<CompYunjianziEntity>()?.Role>=5?2:1);
            foreach(var item in inventory?.innerContainer.ToList() ?? new List<Thing>())item.Destroy(DestroyMode.Vanish);
            foreach(var item in equipment?.AllEquipmentListForReading.ToList() ?? new List<ThingWithComps>())item.Destroy(DestroyMode.Vanish);
            foreach(var item in apparel?.WornApparel.ToList() ?? new List<Apparel>())item.Destroy(DestroyMode.Vanish);
            var shard=health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ShardHolder);
            if(shard!=null)health.RemoveHediff(shard);
            base.Kill(dinfo,exactCulprit);
            if(Dead && GameComponent_YunjianziPursuit.Instance is GameComponent_YunjianziPursuit story)
                foreach(var node in story.nodes.Where(n=>n.entityBoss==this))node.entityBossDefeated=true;
        }
    }
}
