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
            if (State.IsLimitedAwareness is false) return true;
            if (obj is Thing t && t != ModCompatibility.GetPerspectiveShiftAvatar())
            {
                if (ModCompatibility.IsPerspectiveShiftAuthentic() is false && t.Faction == Faction.OfPlayer) return true;
                return t.Map.IsVisible(t.Position);
            }
            return true;
        }
    }
}
