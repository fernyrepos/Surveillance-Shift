using HarmonyLib;
using Verse.AI;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    public static class Pawn_JobTracker_EndCurrentJob_Patch
    {
        public static bool Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob)
        {
            if (condition != JobCondition.InterruptForced || startNewJob is false)
            {
                return true;
            }
            if (__instance.curDriver is JobDriver_ManWatchtower driver && driver.IsElevated)
            {
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
