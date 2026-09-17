using Verse;

namespace SurveillanceShift
{
    public class ColonistSurveillanceData : IExposable
    {
        public ThreatReportReaction reportReaction = ThreatReportReaction.ReportViaButton;
        public int lastThreatReportTick = -9999;
        public bool hadWalkie;
        public bool reactionConfigured;

        public void UpdateWalkieStatus(Pawn pawn)
        {
            var hasWalkie = pawn.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie);
            if (hasWalkie && hadWalkie is false)
            {
                hadWalkie = true;
                if (reactionConfigured is false)
                {
                    reportReaction = ThreatReportReaction.ReportViaRadio;
                }
            }
            else if (hasWalkie is false && hadWalkie)
            {
                hadWalkie = false;
                if (reactionConfigured is false || reportReaction == ThreatReportReaction.ReportViaRadio)
                {
                    reportReaction = ModCompatibility.HasPerspectiveShiftAvatar() ? ThreatReportReaction.ReportInPerson : ThreatReportReaction.ReportViaButton;
                }
            }
            else if (reactionConfigured is false)
            {
                reportReaction = hasWalkie ? ThreatReportReaction.ReportViaRadio : ModCompatibility.HasPerspectiveShiftAvatar() ? ThreatReportReaction.ReportInPerson : ThreatReportReaction.ReportViaButton;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref reportReaction, "SS_reportReaction", ThreatReportReaction.ReportViaButton);
            Scribe_Values.Look(ref lastThreatReportTick, "SS_lastThreatReportTick", -9999);
            Scribe_Values.Look(ref hadWalkie, "SS_hadWalkie", false);
            Scribe_Values.Look(ref reactionConfigured, "SS_reactionConfigured", false);
        }
    }
}
