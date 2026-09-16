using System.Reflection;
using HarmonyLib;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class State_ControlsFrozen_Patch
    {
        public static bool Prepare() => ModCompatibility.PerspectiveShiftAvailable;

        public static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter("PerspectiveShift.State:ControlsFrozen");

        public static void Postfix(ref bool __result)
        {
            if (__result) return;
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar?.jobs?.curDriver is JobDriver_ManWatchtower driver && driver.IsClimbing)
            {
                __result = true;
            }
        }
    }
}
