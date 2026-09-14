using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(MainTabWindow_Wildlife), "Pawns", MethodType.Getter)]
    public static class MainTabWindow_Wildlife_Pawns_Patch
    {
        public static void Postfix(ref IEnumerable<Pawn> __result)
        {
            if (State.IsLimitedAwareness is false || Find.CurrentMap is null) return;
            __result = __result.Where(p => p.Spawned && Find.CurrentMap.IsVisible(p.Position));
        }
    }
}
