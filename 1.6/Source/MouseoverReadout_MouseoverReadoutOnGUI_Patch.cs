using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
    public static class MouseoverReadout_MouseoverReadoutOnGUI_Patch
    {
        public static bool Prefix()
        {
            if (State.IsLimitedAwareness is false || Find.CurrentMap is null) return true;

            var cell = UI.MouseCell();
            if (cell.InBounds(Find.CurrentMap) && Find.CurrentMap.IsVisible(cell) is false) return false;
            return true;
        }
    }
}
