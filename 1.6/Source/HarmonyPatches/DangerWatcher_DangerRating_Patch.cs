using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(DangerWatcher), nameof(DangerWatcher.DangerRating), MethodType.Getter)]
    public static class DangerWatcher_DangerRating_Patch
    {
        public static void Postfix(DangerWatcher __instance, ref StoryDanger __result)
        {
            if (State.IsLimitedAwareness is false || __result == StoryDanger.None) return;
            foreach (var target in __instance.map.attackTargetsCache.TargetsHostileToColony)
            {
                if (target.Thing is Pawn { Spawned: true } pawn && pawn.Map.IsVisible(pawn.Position) is false) continue;
                if (target.Thing is Building_TurretGun { Spawned: true } turret && turret.Map.IsVisible(turret.Position) is false) continue;
                return;
            }
            __result = StoryDanger.None;
        }
    }
}
