using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawGUIOverlay))]
    public static class Pawn_DrawGUIOverlay_Patch
    {
        public static bool Prefix(Pawn __instance)
        {
            if (State.IsLimitedAwareness is false || __instance == ModCompatibility.GetPerspectiveShiftAvatar() || ModCompatibility.IsPerspectiveShiftAuthentic() is false && __instance.Faction == Faction.OfPlayer) return true;

            var map = __instance.Map ?? __instance.Corpse?.Map;
            if (map is null) return true;
            return map.IsVisible(__instance.Corpse?.Position ?? __instance.Position);
        }
    }
}
