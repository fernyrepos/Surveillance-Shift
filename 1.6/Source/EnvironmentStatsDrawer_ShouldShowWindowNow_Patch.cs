using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(EnvironmentStatsDrawer), nameof(EnvironmentStatsDrawer.ShouldShowWindowNow))]
    public static class EnvironmentStatsDrawer_ShouldShowWindowNow_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            if (State.IsLimitedAwareness is false || Find.CurrentMap is null) return true;
            var cell = UI.MouseCell();
            if (cell.InBounds(Find.CurrentMap) && Find.CurrentMap.IsVisible(cell) is false) return __result = false;
            return true;
        }
    }
}
