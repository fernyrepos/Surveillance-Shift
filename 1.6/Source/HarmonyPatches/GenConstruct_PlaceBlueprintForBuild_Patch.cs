using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild))]
    public static class GenConstruct_PlaceBlueprintForBuild_Patch
    {
        public static void Postfix(BuildableDef sourceDef, IntVec3 center, Map map, Faction faction)
        {
            if (sourceDef != DefsOf.SS_GuardPost || faction != Faction.OfPlayer || Current.ProgramState != ProgramState.Playing) return;
            map.GetComponent<MapComponent_Guarding>().SetPendingPostName(center, null);
            Find.WindowStack.Add(new Dialog_NameNewGuardPost(map, center));
        }
    }
}
