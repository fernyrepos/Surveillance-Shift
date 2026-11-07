using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(MusicManagerPlay), nameof(MusicManagerPlay.DangerMusicMode), MethodType.Getter)]
    [HarmonyAfter("CombatMusicPatchMod")]
    public static class MusicManagerPlay_DangerMusicMode_Patch
    {
        private const int RefreshIntervalTicks = 30;

        private static int lastUpdateTick = -10000;
        private static bool cachedKnownThreat;

        public static void Postfix(MusicManagerPlay __instance, ref bool __result)
        {
            if (__result is false || State.IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing) return;
            if (__instance.OverrideDangerMode || Find.Scenario.OverrideDangerMusic) return;
            var now = Find.TickManager.TicksGame;
            if (now > lastUpdateTick + RefreshIntervalTicks || now < lastUpdateTick)
            {
                lastUpdateTick = now;
                cachedKnownThreat = AnyKnownThreat();
            }
            if (cachedKnownThreat is false) __result = false;
        }

        private static bool AnyKnownThreat()
        {
            var maps = Find.Maps;
            for (var i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                foreach (var target in map.attackTargetsCache.TargetsHostileToColony)
                {
                    if (DangerWatcher.CachedAffectsStoryDangerDelegate(target) is false) continue;
                    if (target.Thing is Pawn { Spawned: true } pawn && pawn.IsPsychologicallyInvisible() is false && map.IsVisible(pawn.Position)) return true;
                    if (target.Thing is Building_TurretGun { Spawned: true } turret && map.IsVisible(turret.Position)) return true;
                }
            }
            return false;
        }
    }
}
