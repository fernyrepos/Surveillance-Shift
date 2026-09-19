using LudeonTK;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public static class DebugActions
    {
        [DebugAction("Surveillance Shift", "Spawn friendly scouts", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnFriendlyScouts()
        {
            FireIncident(DefsOf.SS_FriendlyScouts);
        }

        [DebugAction("Surveillance Shift", "Spawn enemy scouts", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnEnemyScouts()
        {
            FireIncident(DefsOf.SS_EnemyScouts);
        }

        [DebugAction("Surveillance Shift", "Trigger scout retaliation", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TriggerScoutRetaliation()
        {
            if (IncidentWorker_EnemyScouts.CandidateFactions().TryRandomElement(out var faction) is false)
            {
                Log.Message("[SurveillanceShift] No hostile faction available for scout retaliation");
            }
            else
            {
                State.IncrementRetaliation(faction);
                Log.Message($"[SurveillanceShift] Retaliation multiplier for {faction.Name} is now {State.GetRetaliationMultiplier(faction)}");
                FireIncident(DefsOf.SS_EnemyScouts, faction);
            }
        }

        [DebugAction("Surveillance Shift", "Reset scout retaliation", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResetScoutRetaliation()
        {
            foreach (var faction in IncidentWorker_EnemyScouts.CandidateFactions())
            {
                State.ResetRetaliation(faction);
            }
            Log.Message("[SurveillanceShift] Scout retaliation multipliers cleared for all hostile factions");
        }

        private static void FireIncident(IncidentDef def, Faction faction = null)
        {
            var parms = StorytellerUtility.DefaultParmsNow(def.category, Find.CurrentMap);
            parms.forced = true;
            parms.faction = faction;
            def.Worker.TryExecute(parms);
        }
    }
}
