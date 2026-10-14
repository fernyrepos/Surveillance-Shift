using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class ThinkNode_ConditionalGuardDuty : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Drafted || pawn.RaceProps.Humanlike || pawn.CanGuard() is false) return false;
            if (pawn.GetGuardAssignment().keepWatch is false || GuardUtility.HasAnyDuty(pawn) is false) return false;
            return PawnUtility.PlayerForcedJobNowOrSoon(pawn) is false;
        }
    }

    public class JobGiver_GuardEngage : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (GuardUtility.DutyIsValid(pawn, out var assignment) is false || GuardCombat.CanEngage(pawn) is false) return null;
            var anchor = GuardUtility.Anchor(pawn, assignment);
            var target = GuardCombat.FindThreat(pawn, anchor, GuardUtility.Leash(assignment.stance));
            if (target is null) return null;
            var report = GuardUtility.TryReportFirst(pawn);
            if (report != null) return report;
            return JobMaker.MakeJob(DefsOf.SS_GuardEngage, target);
        }
    }

    public class JobGiver_GuardDuty : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (GuardUtility.NeedsAttention(pawn)) return null;
            return GuardUtility.MakeDutyJob(pawn);
        }
    }
}
