using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Selector), nameof(Selector.SelectorOnGUI))]
    public static class Selector_SelectorOnGUI_Patch
    {
        public static bool Prefix()
        {
            return ScopeView.BlocksWorldInteraction is false;
        }
    }

    [HarmonyPatch(typeof(MapInterface), nameof(MapInterface.HandleMapClicks))]
    public static class MapInterface_HandleMapClicks_Patch
    {
        public static bool Prefix()
        {
            return ScopeView.BlocksWorldInteraction is false;
        }
    }
}
