using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.AdjustedRaidPoints))]
    public static class IncidentWorker_Raid_AdjustedRaidPoints_Patch
    {
        public static void Postfix(ref float __result, Faction faction, IIncidentTarget target)
        {
            if (State.IsLimitedAwareness && target is Map map && State.TryGetKnownWealth(faction, out var knownWealth) && map.wealthWatcher.WealthTotal > 0f)
            {
                __result *= Mathf.Clamp(knownWealth / map.wealthWatcher.WealthTotal, 0.25f, 2.5f);
            }
        }
    }
}
