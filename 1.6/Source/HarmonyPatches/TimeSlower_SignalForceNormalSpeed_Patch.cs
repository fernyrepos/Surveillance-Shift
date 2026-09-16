using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class TimeSlower_SignalForceNormalSpeed_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeed));
            yield return AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeedShort));
        }

        public static bool Prefix()
        {
            return State.IsSlowdownMuted is false;
        }
    }
}
