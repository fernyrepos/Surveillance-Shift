using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobGiver_AnimalThreatReaction : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (State.GuardingPawnsActive is false || pawn.Faction != Faction.OfPlayer || pawn.training.HasLearned(DefsOf.SS_KeepWatch) is false || PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return null;
            var data = pawn.GetSurveillanceData();
            if (data.alertResponse == AnimalAlertResponse.None || Find.TickManager.TicksGame - data.lastThreatReportTick <= GenDate.TicksPerHour) return null;
            return State.TryGetAnimalReactionJob(pawn, data);
        }
    }
}
