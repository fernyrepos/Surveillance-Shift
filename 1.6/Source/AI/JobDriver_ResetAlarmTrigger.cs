using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_ResetAlarmTrigger : JobDriver
    {
        private Building_AlarmTrigger Trigger => (Building_AlarmTrigger)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Trigger.AlarmActive is false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var reset = ToilMaker.MakeToil("ResetAlarm");
            reset.defaultCompleteMode = ToilCompleteMode.Instant;
            reset.initAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Trigger);
                Trigger.ResetAlarm();
            };
            reset.handlingFacing = true;
            yield return reset;
        }
    }
}
