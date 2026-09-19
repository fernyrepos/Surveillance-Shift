using HarmonyLib;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        public static bool Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            if (__instance.curDriver is JobDriver_ManWatchtower driver && (driver.IsElevated || driver.gracefulExitStarted))
            {
                driver.pendingJob = newJob;
                if (driver.gracefulExitStarted is false)
                {
                    driver.BeginGracefulExit();
                }
                return false;
            }
            return true;
        }
    }
}
