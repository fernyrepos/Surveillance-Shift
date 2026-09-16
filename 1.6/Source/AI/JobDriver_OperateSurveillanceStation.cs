using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_OperateSurveillanceStation : JobDriver
    {
        private Building_SurveillanceStation Station => (Building_SurveillanceStation)job.GetTarget(TargetIndex.A).Thing;
        private const int ThreatCheckIntervalTicks = 60;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Station.CanUseNow is false || Station.HasCameras is false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var monitor = ToilMaker.MakeToil("MakeNewToils");
            monitor.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Station);
                pawn.skills?.Learn(SkillDefOf.Intellectual, 0.025f);
                pawn.GainComfortFromCellIfPossible(1, chairsOnly: true);
                if (pawn.IsHashIntervalTick(ThreatCheckIntervalTicks))
                {
                    Building_AlarmTrigger.CheckThreatsAndSoundAlarm(Station.Map, pawn);
                }
            };
            monitor.handlingFacing = true;
            monitor.defaultCompleteMode = ToilCompleteMode.Never;
            yield return monitor;
        }
    }
}
