using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_MonitorRemoteDevice : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.OnCell;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Some;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t != pawn || pawn == ModCompatibility.GetPerspectiveShiftAvatar() || pawn.Faction != Faction.OfPlayer) return false;
            var router = CompRemoteSurveillanceDevice.FindLinkedRouter(pawn);
            return router != null && router.IsActive;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefsOf.SS_MonitorRemoteDevice, pawn);
        }
    }
}
