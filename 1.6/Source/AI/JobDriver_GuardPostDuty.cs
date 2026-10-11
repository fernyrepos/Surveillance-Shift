using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_GuardPostDuty : JobDriver_GuardBase
    {
        private Building_GuardPost Post => job.targetA.Thing as Building_GuardPost;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, Post?.GetComp<CompAssignableToPawn_GuardPost>()?.MaxAssignedPawnsCount ?? 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            rotateToFace = TargetIndex.B;
            this.FailOnDespawnedOrNull(TargetIndex.A);
            BindDutyFailure();
            yield return Toils_Goto.GotoCell(job.targetC.IsValid ? TargetIndex.C : TargetIndex.A, PathEndMode.OnCell);

            var watch = ToilMaker.MakeToil("Watch");
            watch.defaultCompleteMode = ToilCompleteMode.Never;
            watch.initAction = () =>
            {
                pawn.pather.StopDead();
                FacePostDirection();
            };
            watch.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(30)) FacePostDirection();
                WatchTick();
            };
            yield return watch;
        }

        private void FacePostDirection()
        {
            var post = Post;
            if (post is null) return;
            pawn.rotationTracker.FaceCell(post.Position + post.Rotation.FacingCell);
        }
    }
}
