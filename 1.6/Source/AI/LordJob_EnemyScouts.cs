using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class LordJob_EnemyScouts : LordJob
    {
        private Faction faction;
        private IntVec3 scoutSpot;
        private bool retaliationTriggered;
        private bool scanCompleted;
        private const int ScanDurationTicks = 2500 * 3;

        public LordJob_EnemyScouts() { }

        public LordJob_EnemyScouts(Faction faction, IntVec3 scoutSpot)
        {
            this.faction = faction;
            this.scoutSpot = scoutSpot;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            var travel = new LordToil_Travel(scoutSpot);
            graph.StartingToil = travel;

            var scanToil = new LordToil_EnemyScoutObserve(scoutSpot, ScanDurationTicks);
            graph.AddToil(scanToil);

            var exit = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
            graph.AddToil(exit);

            var travelToScan = new Transition(travel, scanToil);
            travelToScan.AddTrigger(new Trigger_Memo("TravelArrived"));
            graph.AddTransition(travelToScan);

            var scanToExit = new Transition(scanToil, exit);
            scanToExit.AddTrigger(new Trigger_TicksPassed(ScanDurationTicks));
            scanToExit.AddPreAction(new TransitionAction_Custom(() =>
            {
                scanCompleted = true;
                State.ApplyDebt(faction);
                State.SetKnownWealth(faction, Map.wealthWatcher.WealthTotal);
                State.ResetRetaliation(faction);
                Messages.Message("SS_EnemyScoutsFinished".Translate(faction.Name), MessageTypeDefOf.ThreatSmall);
            }));
            graph.AddTransition(scanToExit);

            var harmTransition = new Transition(scanToil, exit);
            harmTransition.AddSource(travel);
            harmTransition.AddTrigger(new Trigger_PawnHarmed());
            graph.AddTransition(harmTransition);

            return graph;
        }

        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            base.Notify_PawnLost(p, condition);
            if (condition == PawnLostCondition.Killed && retaliationTriggered is false)
            {
                retaliationTriggered = true;
                State.IncrementRetaliation(faction);
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (scanCompleted is false && retaliationTriggered is false)
            {
                retaliationTriggered = true;
                State.IncrementRetaliation(faction);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref scoutSpot, "scoutSpot");
            Scribe_Values.Look(ref retaliationTriggered, "retaliationTriggered");
            Scribe_Values.Look(ref scanCompleted, "scanCompleted");
        }
    }
}
