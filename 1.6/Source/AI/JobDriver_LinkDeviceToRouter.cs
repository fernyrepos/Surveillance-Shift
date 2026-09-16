using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_LinkDeviceToRouter : JobDriver
    {
        private const int LinkDurationTicks = 60 * 5;
        private Building_SurveillanceRouter Router => (Building_SurveillanceRouter)job.GetTarget(TargetIndex.A).Thing;
        private ThingWithComps Device => (ThingWithComps)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Router.linkedDevice != null && Router.linkedDevice != Device);
            this.FailOn(() => Device.Destroyed || pawn.inventory.innerContainer.Contains(Device) is false);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var wait = ToilMaker.MakeToil("MakeNewToils");
            wait.handlingFacing = true;
            wait.defaultCompleteMode = ToilCompleteMode.Delay;
            wait.initAction = () =>
            {
                pawn.rotationTracker.FaceCell(Router.Position);
                wait.actor.jobs.curDriver.ticksLeftThisToil = LinkDurationTicks;
            };
            yield return wait;

            var link = ToilMaker.MakeToil("MakeNewToils");
            link.initAction = () =>
            Device.GetComp<CompRemoteSurveillanceDevice>().LinkTo(Router);
            yield return link;
        }
    }
}
