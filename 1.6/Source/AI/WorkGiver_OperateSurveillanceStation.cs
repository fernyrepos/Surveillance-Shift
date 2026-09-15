using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_OperateSurveillanceStation : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefsOf.SS_SurveillanceStation);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool Prioritized => true;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Some;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == ModCompatibility.GetPerspectiveShiftAvatar() || t.Faction != pawn.Faction || t is not Building_SurveillanceStation station) return false;
            if (station.CanUseNow is false || pawn.CanReserve(station, 1, -1, null, forced) is false) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefsOf.SS_OperateSurveillanceStation, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            if (t.Thing is Building_SurveillanceStation station)
            {
                return (float)station.Priority;
            }
            return 0f;
        }
    }
}
