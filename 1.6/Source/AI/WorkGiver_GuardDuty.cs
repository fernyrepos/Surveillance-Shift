using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class WorkGiver_GuardDuty : WorkGiver
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn.CanGuard() is false || pawn.GetGuardAssignment().IsActive is false;
        }

        public override Job NonScanJob(Pawn pawn)
        {
            if (GuardUtility.DutyIsValid(pawn, out var assignment) is false) return null;
            if (GuardCombat.CanEngage(pawn))
            {
                var target = GuardCombat.FindThreat(pawn, GuardUtility.Anchor(pawn, assignment), GuardUtility.Leash(assignment.stance));
                if (target != null) return JobMaker.MakeJob(DefsOf.SS_GuardEngage, target);
            }
            return GuardUtility.MakeDutyJob(pawn, assignment);
        }
    }
}
