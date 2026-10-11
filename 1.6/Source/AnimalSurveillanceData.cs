using Verse;

namespace SurveillanceShift
{
    public class AnimalSurveillanceData : IExposable
    {
        public AnimalAlertResponse alertResponse = AnimalAlertResponse.ReportToHandler;
        public int lastThreatReportTick = -9999;

        public void ExposeData()
        {
            Scribe_Values.Look(ref alertResponse, "SS_alertResponse", AnimalAlertResponse.ReportToHandler);
            Scribe_Values.Look(ref lastThreatReportTick, "SS_lastThreatReportTick", -9999);
        }
    }
}
