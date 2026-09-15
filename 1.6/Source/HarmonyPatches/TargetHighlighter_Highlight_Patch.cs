using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(TargetHighlighter), nameof(TargetHighlighter.Highlight))]
    public static class TargetHighlighter_Highlight_Patch
    {
        public static bool Prefix(GlobalTargetInfo target)
        {
            if (State.IsLimitedAwareness is false || target.IsValid is false || target.IsMapTarget is false || target.Map is null) return true;
            return target.Map.IsVisible(target.Cell);
        }
    }
}
