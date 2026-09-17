using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(WorldComponent_GravshipController), nameof(WorldComponent_GravshipController.InitiateLanding))]
    public static class WorldComponent_GravshipController_InitiateLanding_Patch
    {
        public static void Postfix(Map map)
        {
            map.GetComponent<MapComponent_FogOfWar>().RecalculateVisibility(gravshipReveal: true);
        }
    }
}
