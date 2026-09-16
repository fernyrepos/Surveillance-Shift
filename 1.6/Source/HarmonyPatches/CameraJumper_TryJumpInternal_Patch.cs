using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(CameraJumper), "TryJumpInternal", new[] { typeof(IntVec3), typeof(Map), typeof(CameraJumper.MovementMode) })]
    public static class CameraJumper_TryJumpInternal_Patch
    {
        public static bool Prefix(IntVec3 cell, Map map)
        {
            if (State.IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing || WorldComponent_GravshipController.CutsceneInProgress || Find.CurrentGravship != null || map.IsVisible(cell)) return true;

            State.SpawnPing(cell, map, false);
            return false;
        }
    }
}
