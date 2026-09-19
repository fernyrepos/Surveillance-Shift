using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetFloatMenuOptions))]
    public static class Pawn_GetFloatMenuOptions_Patch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> values, Pawn __instance, Pawn selPawn)
        {
            foreach (var opt in values)
            {
                yield return opt;
            }

            if (__instance.Dead || __instance.Downed || selPawn.Dead || selPawn.Downed || selPawn.Faction != Faction.OfPlayer || selPawn.RaceProps.Humanlike is false || selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) is false || selPawn.CanReach(__instance, PathEndMode.Touch, Danger.Deadly) is false) yield break;

            var lord = __instance.GetLord();
            if (lord?.LordJob is LordJob_FriendlyScouts scoutsJob && scoutsJob.negotiated is false && scoutsJob.Negotiator == __instance)
            {
                yield return new FloatMenuOption("SS_ConverseWithScout".Translate(), () =>
                {
                    var job = JobMaker.MakeJob(DefsOf.SS_NegotiateWithScout, __instance);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }
}
