using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(WorldComponent_GravshipController), nameof(WorldComponent_GravshipController.Notify_LandingAreaConfirmationStarted))]
    public static class WorldComponent_GravshipController_Notify_LandingAreaConfirmationStarted_Patch
    {
        public static void Postfix(WorldComponent_GravshipController __instance, GravshipLandingMarker marker)
        {
            var map = marker.Spawned ? marker.Map : __instance.landingMap;
            map.GetComponent<MapComponent_FogOfWar>().RecalculateVisibility(gravshipReveal: true);
        }
    }
}
