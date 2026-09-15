using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(SilhouetteUtility), nameof(SilhouetteUtility.ShouldDrawSilhouette))]
    public static class SilhouetteUtility_ShouldDrawSilhouette_Patch
    {
        public static bool Prefix(Thing thing, ref bool __result)
        {
            if (State.IsLimitedAwareness is false || thing.Spawned is false || thing == ModCompatibility.GetPerspectiveShiftAvatar()) return true;

            if (thing.Map.IsVisible(thing.Position) is false)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
