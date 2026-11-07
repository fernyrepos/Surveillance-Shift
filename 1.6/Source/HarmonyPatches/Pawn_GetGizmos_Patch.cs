using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            if (__instance.Faction != Faction.OfPlayer || __instance.Spawned is false || __instance.CanGuard() is false) yield break;
            var mech = __instance.RaceProps.IsMechanoid;
            if (mech is false && __instance.RaceProps.Animal is false) yield break;
            if (mech && GuardUtility.AssignedPosts(__instance).Any() is false && GuardUtility.AssignedRoutes(__instance).Any() is false) yield break;

            var assignment = __instance.GetGuardAssignment();
            yield return new Command_Toggle
            {
                defaultLabel = "SS_KeepWatch".Translate(),
                defaultDesc = (mech ? "SS_KeepWatchMechDesc" : "SS_KeepWatchDesc").Translate(),
                icon = GuardTextures.AnimalWatch,
                isActive = () => assignment.keepWatch,
                toggleAction = () =>
                {
                    assignment.keepWatch = assignment.keepWatch is false;
                    GuardUtility.NotifyChanged(__instance);
                }
            };
        }
    }
}
