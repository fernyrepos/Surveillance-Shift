using System.Collections.Generic;
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

            if (__instance.Faction == Faction.OfPlayer && __instance.RaceProps.Animal && State.GuardingPawnsActive && __instance.training.HasLearned(DefsOf.SS_KeepWatch))
            {
                var data = __instance.GetSurveillanceData();
                yield return new Command_Toggle
                {
                    defaultLabel = "SS_EnablePatrol".Translate(),
                    defaultDesc = "SS_EnablePatrolDesc".Translate(),
                    icon = DefsOf.SS_KeepWatch.Icon,
                    isActive = () => data.patrolEnabled,
                    toggleAction = () => data.patrolEnabled = data.patrolEnabled is false
                };

                yield return new Command_Action
                {
                    defaultLabel = "SS_AlertResponse".Translate(("SS_" + data.alertResponse.ToString()).Translate()),
                    defaultDesc = "SS_AlertResponseDesc".Translate(),
                    icon = DefsOf.SS_KeepWatch.Icon,
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("SS_None".Translate(), () => data.alertResponse = AnimalAlertResponse.None),
                            new FloatMenuOption("SS_ReportToHandler".Translate(), () => data.alertResponse = AnimalAlertResponse.ReportToHandler),
                            new FloatMenuOption("SS_UseAlarmTrigger".Translate(), () => data.alertResponse = AnimalAlertResponse.UseAlarmTrigger)
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
        }
    }
}
