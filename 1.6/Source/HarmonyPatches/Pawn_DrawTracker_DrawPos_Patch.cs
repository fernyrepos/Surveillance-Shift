using HarmonyLib;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Pawn_DrawTracker_DrawPos_Patch
    {
        public static void Postfix(Pawn_DrawTracker __instance, ref Vector3 __result)
        {
            if (__instance.pawn.jobs?.curDriver is JobDriver_ManWatchtower watchtower && watchtower.forcedBodyOffset.HasValue)
            {
                __result.y = watchtower.forcedBodyOffset.Value.y;
            }
        }
    }
}
