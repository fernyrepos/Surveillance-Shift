using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_PatrolDuty : JobDriver_GuardBase
    {
        private Building_PatrolWaypoint Waypoint => job.targetA.Thing as Building_PatrolWaypoint;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            BindDutyFailure();

            var travel = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            travel.AddPreTickAction(WatchTick);
            yield return travel;

            var arrive = ToilMaker.MakeToil("Arrive");
            arrive.defaultCompleteMode = ToilCompleteMode.Instant;
            arrive.initAction = () => pawn.GetGuardAssignment().lastWaypoint = Waypoint;
            yield return arrive;

            var dwell = ToilMaker.MakeToil("Dwell");
            dwell.defaultCompleteMode = ToilCompleteMode.Delay;
            dwell.initAction = () =>
            {
                var wp = Waypoint;
                var minimum = wp.Route is { waypoints: { Count: 1 } } ? 600 : 1;
                ticksLeftThisToil = Mathf.Max(wp.dwellTicks, minimum);
                pawn.pather.StopDead();
            };
            dwell.tickAction = WatchTick;
            yield return dwell;

            var advance = ToilMaker.MakeToil("Advance");
            advance.defaultCompleteMode = ToilCompleteMode.Instant;
            advance.initAction = () =>
            {
                var assignment = pawn.GetGuardAssignment();
                var next = Waypoint.Route?.Next(Waypoint, ref assignment.direction);
                if (next is null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                job.targetA = next;
                JumpToToil(travel);
            };
            yield return advance;
        }
    }
}
