using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.RaceProps.Animal && __instance.Faction == Faction.OfPlayer)
            {
                __instance.GetSurveillanceData().ExposeData();
            }
            if (__instance.RaceProps.Humanlike && __instance.Faction == Faction.OfPlayer)
            {
                __instance.GetColonistSurveillanceData().ExposeData();
            }
        }
    }
}
