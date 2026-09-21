using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_ResetAlarmTrigger : WorkGiver_Scanner
    {
        private const int ResetDelayAfterTriggerTicks = 600;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefsOf.SS_AlarmTrigger);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_AlarmTrigger trigger || trigger.AlarmActive is false || trigger.Faction != pawn.Faction) return false;
            if (forced is false && (Find.TickManager.TicksGame < trigger.LastTriggeredTick + ResetDelayAfterTriggerTicks || trigger.Map.dangerWatcher.DangerRating != StoryDanger.None))
                return false;
            return pawn.CanReach(trigger, PathEndMode.InteractionCell, Danger.Deadly) && pawn.CanReserve(trigger, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefsOf.SS_ResetAlarmTrigger, t);
        }
    }
}
