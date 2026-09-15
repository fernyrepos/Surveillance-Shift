using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_ViewCameraFeeds : JobDriver
    {
        private Building_SurveillanceStation Station => (Building_SurveillanceStation)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Station.CanUseNow is false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var open = ToilMaker.MakeToil("MakeNewToils");
            open.initAction = () =>
            {
                pawn.rotationTracker.FaceCell(Station.Position);
                Find.WindowStack.Add(new Dialog_SurveillanceCameras(Station.Map));
            };
            yield return open;
        }
    }
}
