using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(WorldComponent_GravshipController), nameof(WorldComponent_GravshipController.InitiateTakeoff))]
    public static class WorldComponent_GravshipController_InitiateTakeoff_Patch
    {
        public static void Prefix(Building_GravEngine engine)
        {
            engine.Map.GetComponent<MapComponent_FogOfWar>().RecalculateVisibility(gravshipReveal: true, excludeCells: engine.ValidSubstructure);
        }
    }
}
