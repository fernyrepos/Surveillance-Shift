using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobGiver_ColonistThreatReaction : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (State.IsLimitedAwareness is false || pawn.Faction != Faction.OfPlayer || pawn.RaceProps.Humanlike is false || pawn.IsPrisoner || pawn.IsSlave || pawn.Downed || pawn.Awake() is false || pawn.InMentalState || pawn == ModCompatibility.GetPerspectiveShiftAvatar() || PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return null;
            var data = pawn.GetColonistSurveillanceData();
            if (pawn.Drafted && data.reportReaction != ThreatReportReaction.ReportViaRadio) return null;
            data.UpdateWalkieStatus(pawn);
            return State.TryGetReactionJob(pawn, data);
        }
    }
}
