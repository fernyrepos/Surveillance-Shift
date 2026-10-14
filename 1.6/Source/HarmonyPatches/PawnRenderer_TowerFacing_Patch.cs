using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class PawnRenderer_TowerFacing_Patch
    {
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> pawnField = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

        [HarmonyPriority(Priority.Last)]
        public static void Prefix(PawnRenderer __instance, ref Rot4? rotOverride)
        {
            var pawn = pawnField(__instance);
            if (pawn?.jobs?.curDriver is not JobDriver_ManWatchtower) return;
            rotOverride = null;
        }
    }
}
