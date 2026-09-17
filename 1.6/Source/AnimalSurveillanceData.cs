using Verse;

namespace SurveillanceShift
{
    public class AnimalSurveillanceData : IExposable
    {
        public bool patrolEnabled;
        public AnimalAlertResponse alertResponse = AnimalAlertResponse.None;
        public int lastThreatReportTick = -9999;

        public void ExposeData()
        {
            Scribe_Values.Look(ref patrolEnabled, "SS_patrolEnabled", false);
            Scribe_Values.Look(ref alertResponse, "SS_alertResponse", AnimalAlertResponse.None);
            Scribe_Values.Look(ref lastThreatReportTick, "SS_lastThreatReportTick", -9999);
        }
    }
}
