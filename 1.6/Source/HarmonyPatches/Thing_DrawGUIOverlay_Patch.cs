using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    public static class Thing_DrawGUIOverlay_Patch
    {
        public static bool Prefix(Thing __instance)
        {
            if (State.IsLimitedAwareness is false) return true;

            var map = __instance.Map;
            if (map is null) return true;
            return map.IsVisible(__instance.Position);
        }
    }
}
