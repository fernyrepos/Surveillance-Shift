using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_ReportThreatInPerson : JobDriver
    {
        private const float ViewDistance = 20f;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);

            var approach = ToilMaker.MakeToil("ApproachAvatar");
            approach.initAction = () =>
            pawn.pather.StartPath(job.GetTarget(TargetIndex.A).Thing, PathEndMode.Touch);
            approach.tickAction = () =>
            {
                var avatar = (Pawn)job.GetTarget(TargetIndex.A).Thing;
                if (pawn.Position.InHorDistOf(avatar.Position, ViewDistance) && GenSight.LineOfSight(pawn.Position, avatar.Position, pawn.Map, skipFirstCell: true))
                {
                    pawn.pather.StopDead();
                    ReadyForNextToil();
                }
            };
            approach.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return approach;

            var report = ToilMaker.MakeToil("DeliverReport");
            report.defaultCompleteMode = ToilCompleteMode.Instant;
            report.initAction = () =>
            State.DeliverThreatSpotted(pawn, job.GetTarget(TargetIndex.B).Thing, LetterDefOf.ThreatSmall, inPerson: true);
            yield return report;
        }
    }
}
