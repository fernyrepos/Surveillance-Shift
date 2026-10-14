using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DynamicDrawPhaseAt))]
    public static class Pawn_DynamicDrawPhaseAt_Fog_Patch
    {
        public static bool Prefix(Pawn __instance, DrawPhase phase)
        {
            if (phase != DrawPhase.Draw || FogStyle.HidesFoggedPawns is false) return true;
            var map = __instance.MapHeld;
            if (map is null || map != Find.CurrentMap) return true;
            return map.IsVisible(__instance.PositionHeld);
        }
    }
}
