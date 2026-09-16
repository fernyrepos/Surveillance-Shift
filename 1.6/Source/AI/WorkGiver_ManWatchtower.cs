using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_ManWatchtower : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool Prioritized => true;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Some;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == ModCompatibility.GetPerspectiveShiftAvatar() || t.Faction != pawn.Faction || t is not Building_Watchtower tower) return false;
            if (pawn.CanReach(tower, PathEndMode.InteractionCell, Danger.Some) is false)
            {
                JobFailReason.Is("NoPath".Translate());
                return false;
            }
            return pawn.CanReserve(tower, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefsOf.SS_ManWatchtower, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            if (t.Thing is Building_Watchtower tower)
            {
                return (float)tower.Priority;
            }
            return 0f;
        }
    }
}
