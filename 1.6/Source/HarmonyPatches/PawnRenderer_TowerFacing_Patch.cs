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
            if (rotOverride.HasValue is false) return;
            var driver = pawnField(__instance)?.jobs?.curDriver;
            if (driver?.CurToil is { handlingFacing: true } && driver.GetType().Namespace == nameof(SurveillanceShift)) rotOverride = null;
        }
    }
}
