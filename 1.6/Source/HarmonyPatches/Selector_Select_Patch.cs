using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Selector), nameof(Selector.Select))]
    public static class Selector_Select_Patch
    {
        public static bool Prefix(object obj)
        {
            if (State.IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing || WorldComponent_GravshipController.CutsceneInProgress || obj is not Thing thing) return true;
            var map = thing.MapHeld;
            if (map == null) return true;
            return map.IsVisible(thing);
        }
    }
}
