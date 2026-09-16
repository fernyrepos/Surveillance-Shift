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
            if (router is null)
            {
                JobFailReason.Is("SS_DeviceNotLinked".Translate());
                return false;
            }
            if (router.IsActive is false)
            {
                JobFailReason.Is("SS_LinkedRouterOffline".Translate());
                return false;
            }
            if (router.HasCameras is false)
            {
                JobFailReason.Is("SS_NoCamerasLinked".Translate());
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefsOf.SS_MonitorRemoteDevice, pawn);
        }
    }
}
