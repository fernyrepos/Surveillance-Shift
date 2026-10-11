using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_BodyguardDuty : JobDriver_GuardBase
    {
        private const float CloseRadius = 4f;
        private const float LeaveRadius = 7f;

        private Pawn Protectee => job.targetA.Thing as Pawn;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            BindDutyFailure();

            var escort = ToilMaker.MakeToil("Escort");
            escort.defaultCompleteMode = ToilCompleteMode.Never;
            escort.tickAction = EscortTick;
            yield return escort;
        }

        private void EscortTick()
        {
            WatchTick();
            if (pawn.jobs.curDriver != this || pawn.IsHashIntervalTick(15) is false) return;

            var protectee = Protectee;
            if (protectee is null) return;
            var close = IsNear(protectee, pawn.pather.Moving ? CloseRadius + 1f : LeaveRadius);
            if (close)
            {
                if (pawn.pather.Moving && IsNear(protectee, CloseRadius)) pawn.pather.StopDead();
                return;
            }

            var dest = protectee.pather is { Moving: true } ? protectee.pather.LastPassableCellInPath : protectee.Position;
            if (pawn.pather.Moving && pawn.pather.Destination.Cell.InHorDistOf(dest, CloseRadius)) return;

            var cell = CellFinder.RandomClosewalkCellNear(dest, pawn.Map, 2);
            if (cell.IsValid && cell != pawn.Position && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                pawn.jobs.curJob.locomotionUrgency = pawn.Position.DistanceTo(protectee.Position) > 14f ? LocomotionUrgency.Sprint : LocomotionUrgency.Jog;
                pawn.pather.StartPath(cell, PathEndMode.OnCell);
            }
        }

        private bool IsNear(Pawn protectee, float radius)
        {
            if (pawn.Position.AdjacentTo8WayOrInside(protectee.Position)) return true;
            return pawn.Position.InHorDistOf(protectee.Position, radius) && GenSight.LineOfSight(pawn.Position, protectee.Position, pawn.Map);
        }
    }
}
