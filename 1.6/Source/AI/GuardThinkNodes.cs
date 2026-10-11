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
            var assignment = pawn.GetGuardAssignment();
            if (assignment.IsActive is false || assignment.keepWatch is false) return false;
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
            var assignment = pawn.GetGuardAssignment();
            if (GuardUtility.DutyIsValid(pawn, out assignment) is false)
            {
                if (assignment.IsActive && pawn.Spawned && pawn.CanGuard() && IsPermanentlyBroken(assignment)) assignment.Clear();
                return null;
            }
            if (GuardUtility.NeedsAttention(pawn)) return null;
            return GuardUtility.MakeDutyJob(pawn, assignment);
        }

        private static bool IsPermanentlyBroken(GuardAssignment assignment)
        {
            return assignment.mode switch
            {
                GuardDutyMode.Post => assignment.post is null || assignment.post.Destroyed,
                GuardDutyMode.Bodyguard => assignment.protectee is null || assignment.protectee.Dead || assignment.protectee.Destroyed,
                _ => false
            };
        }
    }
}
