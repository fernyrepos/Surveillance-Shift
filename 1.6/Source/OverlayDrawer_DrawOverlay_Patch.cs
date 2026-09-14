using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(OverlayDrawer), nameof(OverlayDrawer.DrawOverlay), new[] { typeof(Thing), typeof(OverlayTypes) })]
    public static class OverlayDrawer_DrawOverlay_Patch
    {
        public static bool Prefix(Thing t)
        {
            if (State.IsLimitedAwareness is false || t.Spawned is false) return true;
            var map = t.Map;
            if (map is null) return true;
            return map.IsVisible(t.Position);
        }
    }
}
