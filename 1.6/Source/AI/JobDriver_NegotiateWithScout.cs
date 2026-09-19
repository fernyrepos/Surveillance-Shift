using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class JobDriver_NegotiateWithScout : JobDriver
    {
        private Pawn Scout => (Pawn)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Scout, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Scout.GetLord()?.LordJob is not LordJob_FriendlyScouts { negotiated: false });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var talk = ToilMaker.MakeToil("TalkToScout");
            talk.initAction = () =>
            {
                pawn.rotationTracker.FaceCell(Scout.Position);
                Dialog_FriendlyScoutNegotiation.Open(pawn, Scout);
            };
            talk.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return talk;
        }
    }
}
