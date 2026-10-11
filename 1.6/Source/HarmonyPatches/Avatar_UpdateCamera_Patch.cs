using System.Reflection;
using HarmonyLib;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class Avatar_UpdateCamera_Patch
    {
        public static bool Prepare() => ModCompatibility.PerspectiveShiftAvailable;

        public static MethodBase TargetMethod() =>
            AccessTools.Method("PerspectiveShift.Avatar:UpdateCamera");

        public static bool Prefix()
        {
            if (ScopeView.FreeCameraActive is false)
            {
                ScopeView.RestoreCameraDolly();
                return true;
            }
            ScopeView.EnableCameraDolly();
            return false;
        }
    }
}
