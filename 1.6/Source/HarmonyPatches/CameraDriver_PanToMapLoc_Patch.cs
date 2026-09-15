using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.PanToMapLoc), new[] { typeof(IntVec3) })]
    public static class CameraDriver_PanToMapLoc_Patch
    {
        public static bool Prefix(IntVec3 cell, ref bool __state)
        {
            __state = State.SuppressCameraLock(cell);
            return __state is false;
        }

        [HarmonyPriority(Priority.Low)]
        public static void Postfix(bool __state)
        {
            if (__state)
            {
                ModCompatibility.ClearPerspectiveShiftCameraLock();
            }
        }
    }
}
