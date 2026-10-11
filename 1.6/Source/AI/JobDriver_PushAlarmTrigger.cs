using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_PushAlarmTrigger : JobDriver
    {
        private Building_AlarmTrigger Trigger => (Building_AlarmTrigger)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var push = ToilMaker.MakeToil("PushAlarm");
            push.defaultCompleteMode = ToilCompleteMode.Instant;
            push.initAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Trigger);
                Trigger.ToggleAlarm();
                if (job.targetB.Thing is Pawn { Spawned: true } hostile) State.SpawnPing(hostile.Position, pawn.Map, true);
            };
            push.handlingFacing = true;
            yield return push;
        }
    }
}
