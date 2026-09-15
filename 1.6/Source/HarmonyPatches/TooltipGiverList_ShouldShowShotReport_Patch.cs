using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(TooltipGiverList), nameof(TooltipGiverList.ShouldShowShotReport))]
    public static class TooltipGiverList_ShouldShowShotReport_Patch
    {
        public static bool Prefix(Thing t)
        {
            if (State.IsLimitedAwareness is false || t.Spawned is false) return true;
            return t.Map.IsVisible(t.Position);
        }
    }
}
