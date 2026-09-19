using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_MonitorRemoteDevice : WorkGiver
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn == ModCompatibility.GetPerspectiveShiftAvatar() || pawn.Faction != Faction.OfPlayer) return true;
            var router = CompRemoteSurveillanceDevice.FindLinkedRouter(pawn);
            return router is null || router.IsActive is false || router.HasCameras is false;
        }

        public override Job NonScanJob(Pawn pawn)
        {
            return JobMaker.MakeJob(DefsOf.SS_MonitorRemoteDevice, pawn);
        }
    }
}
