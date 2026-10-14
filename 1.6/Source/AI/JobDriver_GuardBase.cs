using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public abstract class JobDriver_GuardBase : JobDriver
    {
        private const int ThreatScanInterval = 20;
        private const int NeedsCheckInterval = 250;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected void BindDutyFailure()
        {
            this.FailOn(() => pawn.IsHashIntervalTick(10) && GuardUtility.JobStillValid(pawn, job) is false);
        }

        protected void WatchTick()
        {
            if (pawn.IsHashIntervalTick(ThreatScanInterval) && GuardCombat.CanEngage(pawn))
            {
                var assignment = pawn.GetGuardAssignment();
                var anchor = GuardUtility.Anchor(pawn, assignment);
                if (GuardCombat.FindThreat(pawn, anchor, GuardUtility.Leash(assignment.stance)) != null)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
            }
            if (pawn.IsHashIntervalTick(NeedsCheckInterval) && GuardUtility.NeedsAttention(pawn))
            {
                EndJobWith(JobCondition.Succeeded);
            }
        }
    }
}
