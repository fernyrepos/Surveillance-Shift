using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_OperateSurveillanceStation : JobDriver
    {
        private Building_SurveillanceStation Station => (Building_SurveillanceStation)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Station.CanUseNow is false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var monitor = ToilMaker.MakeToil("MakeNewToils");
            monitor.tickAction = () =>
            {
                pawn.rotationTracker.FaceCell(Station.Position);
                pawn.skills?.Learn(SkillDefOf.Intellectual, 0.025f);
                pawn.GainComfortFromCellIfPossible(1, chairsOnly: true);
            };
            monitor.defaultCompleteMode = ToilCompleteMode.Never;
            yield return monitor;
        }
    }
}
