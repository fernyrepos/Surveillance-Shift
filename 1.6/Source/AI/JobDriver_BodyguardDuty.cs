using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_BodyguardDuty : JobDriver_GuardBase
    {
        private const float CloseRadius = 3f;
        private const float IdleLeaveRadius = 6f;
        private const float MovingLeaveRadius = 3.5f;
        private const int RepathInterval = 8;

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
            if (pawn.jobs.curDriver != this || pawn.IsHashIntervalTick(RepathInterval) is false) return;

            var protectee = Protectee;
            if (protectee is null) return;
            var protecteeMoving = protectee.pather is { Moving: true };
            var leaveRadius = protecteeMoving ? MovingLeaveRadius : IdleLeaveRadius;
            if (IsNear(protectee, pawn.pather.Moving && protecteeMoving is false ? CloseRadius : leaveRadius))
            {
                if (pawn.pather.Moving && protecteeMoving is false) pawn.pather.StopDead();
                return;
            }

            if (pawn.pather.Moving && pawn.pather.Destination.Cell.InHorDistOf(protectee.Position, CloseRadius)) return;

            var cell = CellFinder.RandomClosewalkCellNear(protectee.Position, pawn.Map, 2);
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
