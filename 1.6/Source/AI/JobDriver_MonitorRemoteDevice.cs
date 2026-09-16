using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_MonitorRemoteDevice : JobDriver
    {
        private const int FaceIntervalTicks = 420;
        private Mote scanningMote;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() =>
            {
                var router = CompRemoteSurveillanceDevice.FindLinkedRouter(pawn);
                return router is null || router.IsActive is false || router.HasCameras is false;
            });

            var monitor = ToilMaker.MakeToil("MakeNewToils");
            monitor.handlingFacing = true;
            monitor.defaultCompleteMode = ToilCompleteMode.Never;
            monitor.initAction = () =>
            {
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.Random.FacingCell);
                AttachObservationMote();
            };
            monitor.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(FaceIntervalTicks))
                {
                    pawn.rotationTracker.FaceCell(pawn.Position + Rot4.Random.FacingCell);
                }
                pawn.skills.Learn(SkillDefOf.Intellectual, 0.025f);
            };
            yield return monitor;

            AddFinishAction(_ => CleanupObservationMote());
        }

        private void AttachObservationMote()
        {
            scanningMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_RemoteSurveillanceDevice, Vector3.zero);
        }

        private void CleanupObservationMote()
        {
            if (scanningMote != null && scanningMote.Destroyed is false)
            {
                scanningMote.Destroy();
                scanningMote = null;
            }
        }
    }
}
