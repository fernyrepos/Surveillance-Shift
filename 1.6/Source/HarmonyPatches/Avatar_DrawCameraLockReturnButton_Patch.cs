using System.Reflection;
using HarmonyLib;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class Avatar_DrawCameraLockReturnButton_Patch
    {
        public static bool Prepare() => ModCompatibility.PerspectiveShiftAvailable;

        public static MethodBase TargetMethod() =>
            AccessTools.Method("PerspectiveShift.Avatar:DrawCameraLockReturnButton");

        public static bool Prefix()
        {
            if (State.ActiveFeedCamera != null)
            {
                State.DrawFeedReturnButton();
                return false;
            }
            return true;
        }
    }
}
