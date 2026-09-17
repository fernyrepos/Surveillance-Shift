using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class LordJob_FriendlyScouts : LordJob
    {
        private Faction faction;
        private IntVec3 chillSpot;
        public bool negotiated;
        private Mote markerMote;

        public LordJob_FriendlyScouts() { }

        public LordJob_FriendlyScouts(Faction faction, IntVec3 chillSpot)
        {
            this.faction = faction;
            this.chillSpot = chillSpot;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            var travel = new LordToil_Travel(chillSpot);
            graph.StartingToil = travel;

            var wait = new LordToil_DefendPoint(chillSpot);
            graph.AddToil(wait);

            var exit = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
            graph.AddToil(exit);

            var travelToWait = new Transition(travel, wait);
            travelToWait.AddTrigger(new Trigger_Memo("TravelArrived"));
            graph.AddTransition(travelToWait);

            var negotiatedToExit = new Transition(wait, exit);
            negotiatedToExit.AddSource(travel);
            negotiatedToExit.AddTrigger(new Trigger_Memo("ScoutsNegotiated"));
            graph.AddTransition(negotiatedToExit);

            var waitToExit = new Transition(wait, exit);
            waitToExit.AddTrigger(new Trigger_TicksPassed(15000));
            waitToExit.AddPreAction(new TransitionAction_Custom(() =>
            {
                if (negotiated is false)
                {
                    State.ApplyDebt(faction, -25);
                    Messages.Message("SS_ScoutsIgnored".Translate(faction.Name), MessageTypeDefOf.NegativeEvent);
                }
            }));
            graph.AddTransition(waitToExit);

            var harmTransition = new Transition(wait, exit);
            harmTransition.AddSource(travel);
            harmTransition.AddTrigger(new Trigger_PawnHarmed());
            harmTransition.AddPreAction(new TransitionAction_Custom(() =>
            {
                if (negotiated is false)
                {
                    State.ApplyDebt(faction, -25);
                }
            }));
            graph.AddTransition(harmTransition);

            return graph;
        }

        public void CleanupMarker()
        {
            if (markerMote != null && markerMote.Destroyed is false)
            {
                markerMote.Destroy();
                markerMote = null;
            }
        }

        public override void LordJobTick()
        {
            base.LordJobTick();
            if (negotiated is false && lord.ownedPawns.Count > 0)
            {
                var leader = lord.ownedPawns[0];
                if (leader.Spawned)
                {
                    if (markerMote == null || markerMote.Destroyed || markerMote.link1.Target.Thing != leader)
                    {
                        markerMote = MoteMaker.MakeAttachedOverlay(leader, DefsOf.SS_Mote_Spyglass, new Vector3(0f, 0f, 0.4f));
                    }
                    markerMote.Maintain();
                }
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            CleanupMarker();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref chillSpot, "chillSpot");
            Scribe_Values.Look(ref negotiated, "negotiated");
        }
    }
}
