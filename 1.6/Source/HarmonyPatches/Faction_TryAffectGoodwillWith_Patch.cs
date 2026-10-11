using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryAffectGoodwillWith))]
    public static class Faction_TryAffectGoodwillWith_Patch
    {
        static bool OnFactionTile(Faction faction)
        {
            var parent = Find.CurrentMap?.Parent;
            return parent != null && parent.Faction == faction;
        }

        public static bool Prefix(Faction __instance, Faction other, int goodwillChange, HistoryEventDef reason, ref bool __result)
        {
            if (State.ApplyingPendingDebt || reason == HistoryEventDefOf.ReachNaturalGoodwill) return true;
            var targetFaction = __instance.IsPlayer ? other : other.IsPlayer ? __instance : null;
            if (State.IsLimitedAwareness && Current.ProgramState == ProgramState.Playing && goodwillChange < 0 && targetFaction != null && targetFaction.HostileTo(Faction.OfPlayer) is false && !OnFactionTile(targetFaction))
            {
                State.AddDebt(targetFaction, goodwillChange);
                __result = true;
                return false;
            }
            return true;
        }
    }
}
