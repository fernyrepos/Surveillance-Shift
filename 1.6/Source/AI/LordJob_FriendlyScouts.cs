using System.Collections.Generic;
using RimWorld;
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

        public Pawn Negotiator
        {
            get
            {
                Pawn best = null;
                var bestLevel = -1;
                foreach (var p in lord.ownedPawns)
                {
                    if (p.ageTracker.Adult is false) continue;
                    var level = p.skills.GetSkill(SkillDefOf.Social).Level;
                    if (level > bestLevel)
                    {
                        bestLevel = level;
                        best = p;
                    }
                }
                return best ?? lord.ownedPawns[0];
            }
        }

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
                    State.ApplyDebt(faction, -SurveillanceShiftMod.settings.relationsIgnorePenalty);
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
                    State.ApplyDebt(faction, -SurveillanceShiftMod.settings.relationsIgnorePenalty);
                }
            }));
            graph.AddTransition(harmTransition);

            return graph;
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
