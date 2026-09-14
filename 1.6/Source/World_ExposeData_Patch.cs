using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(World), nameof(World.ExposeData))]
    public static class World_ExposeData_Patch
    {
        public static void Postfix()
        {
            Scribe_Values.Look(ref State.AwarenessMode, "SS_AwarenessMode", AwarenessMode.AreaOmniscience);
        }
    }
}
