using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public static class HorrorMusicVisibility
    {
        public const int RefreshIntervalTicks = 30;

        public static bool Applies => State.IsLimitedAwareness && Current.ProgramState == ProgramState.Playing;

        public static bool IsRitualPawn(Pawn pawn)
        {
            var lord = pawn.GetLord();
            return lord != null && (lord.LordJob is LordJob_PsychicRitual || lord.LordJob is LordJob_HateChant);
        }

        public static bool IsHostileEntity(Pawn pawn)
        {
            return (pawn.RaceProps.IsAnomalyEntity || pawn.IsSubhuman) && pawn.Downed is false && pawn.IsOnHoldingPlatform is false && pawn.IsPsychologicallyInvisible() is false && pawn.HostileTo(Faction.OfPlayer);
        }

        public static bool Seen(Pawn pawn) => pawn.Spawned && pawn.Map.IsVisible(pawn.Position);
    }

    [HarmonyPatch(typeof(HorrorCombatTransition), nameof(HorrorCombatTransition.IsTransitionSatisfied))]
    public static class HorrorCombatTransition_IsTransitionSatisfied_Patch
    {
        private static int lastUpdateTick = -10000;
        private static bool cached;

        public static void Postfix(ref bool __result)
        {
            if (__result is false || HorrorMusicVisibility.Applies is false) return;
            var now = Find.TickManager.TicksGame;
            if (now > lastUpdateTick + HorrorMusicVisibility.RefreshIntervalTicks || now < lastUpdateTick)
            {
                lastUpdateTick = now;
                cached = Compute();
            }
            __result = cached;
        }

        private static bool Compute()
        {
            foreach (var map in Find.Maps)
            {
                if (map.mapPawns.ColonistsSpawnedCount <= 0) continue;
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (HorrorMusicVisibility.Seen(pawn) is false) continue;
                    if (HorrorMusicVisibility.IsRitualPawn(pawn) || HorrorMusicVisibility.IsHostileEntity(pawn)) return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HorrorTensionTransition), nameof(HorrorTensionTransition.IsTransitionSatisfied))]
    public static class HorrorTensionTransition_IsTransitionSatisfied_Patch
    {
        private static int lastUpdateTick = -10000;
        private static bool cached;

        public static void Postfix(ref bool __result)
        {
            if (__result is false || HorrorMusicVisibility.Applies is false) return;
            var now = Find.TickManager.TicksGame;
            if (now > lastUpdateTick + HorrorMusicVisibility.RefreshIntervalTicks || now < lastUpdateTick)
            {
                lastUpdateTick = now;
                cached = Compute();
            }
            __result = cached;
        }

        private static bool Compute()
        {
            foreach (var map in Find.Maps)
            {
                var conditions = map.gameConditionManager;
                if (conditions.ConditionIsActive(GameConditionDefOf.UnnaturalDarkness) || conditions.ConditionIsActive(GameConditionDefOf.DeathPall) || conditions.ConditionIsActive(GameConditionDefOf.BloodRain)) return true;
            }
            foreach (var map in Find.Maps)
            {
                foreach (var spawner in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingGroundSpawner))
                {
                    if (map.IsVisible(spawner.Position)) return true;
                }
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (HorrorMusicVisibility.Seen(pawn) && HorrorMusicVisibility.IsRitualPawn(pawn)) return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HorrorRelaxTransition), nameof(HorrorRelaxTransition.IsTransitionSatisfied))]
    public static class HorrorRelaxTransition_IsTransitionSatisfied_Patch
    {
        private static int lastUpdateTick = -10000;
        private static bool cached;

        public static void Postfix(ref bool __result)
        {
            if (__result is false || HorrorMusicVisibility.Applies is false) return;
            var now = Find.TickManager.TicksGame;
            if (now > lastUpdateTick + HorrorMusicVisibility.RefreshIntervalTicks || now < lastUpdateTick)
            {
                lastUpdateTick = now;
                cached = Compute();
            }
            __result = cached;
        }

        private static bool Compute()
        {
            foreach (var map in Find.Maps)
            {
                if (map.weatherManager.curWeather == WeatherDefOf.GrayPall || map.gameConditionManager.ConditionIsActive(GameConditionDefOf.GrayPall)) return true;
            }
            foreach (var pocketMap in Find.World.pocketMaps)
            {
                var pocket = pocketMap.Map;
                if (pocket != null && (pocket.generatorDef == MapGeneratorDefOf.Labyrinth || pocket.generatorDef == MapGeneratorDefOf.Undercave)) return true;
            }
            foreach (var map in Find.Maps)
            {
                if (AnyVisible(map, ThingDefOf.PitGate) || AnyVisible(map, ThingDefOf.FleshmassHeart) || AnyVisible(map, ThingDefOf.Noctolith)) return true;
            }
            return false;
        }

        private static bool AnyVisible(Map map, ThingDef def)
        {
            foreach (var thing in map.listerThings.ThingsOfDef(def))
            {
                if (map.IsVisible(thing.Position)) return true;
            }
            return false;
        }
    }
}
