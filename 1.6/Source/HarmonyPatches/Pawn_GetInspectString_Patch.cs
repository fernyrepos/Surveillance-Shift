using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Pawn_GetInspectString_Patch
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            if (Prefs.DevMode is false || __instance.Spawned is false || __instance.RaceProps.Humanlike || __instance.CanGuard() is false) return;
            var assignment = __instance.GetGuardAssignment();
            if (assignment.IsActive is false) return;
            __result = __result.NullOrEmpty() ? Status(__instance, assignment) : __result + "\n" + Status(__instance, assignment);
        }

        private static string Status(Pawn pawn, GuardAssignment assignment)
        {
            var builder = new StringBuilder("Guard: ");
            if (assignment.keepWatch is false) return builder.Append("keep watch off").ToString();
            if (GuardUtility.DutyIsValid(pawn, out _) is false) return builder.Append("duty invalid").ToString();
            if (GuardUtility.IsGuardJob(pawn.CurJobDef)) return builder.Append("on ").Append(pawn.CurJobDef.defName).ToString();
            if (GuardUtility.NeedsAttention(pawn)) return builder.Append("needs attention (hungry/tired/hurt)").ToString();
            if (PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return builder.Append("player-forced job").ToString();
            if (assignment.mode == GuardDutyMode.Post && assignment.post != null)
            {
                if (pawn.CanReserve(assignment.post) is false) return builder.Append("spot reserved by ").Append(pawn.Map.reservationManager.FirstRespectedReserver(assignment.post, pawn)?.LabelShort ?? "?").ToString();
                if (pawn.CanReach(assignment.post, PathEndMode.OnCell, Danger.Some) is false) return builder.Append("cannot reach spot").ToString();
            }
            return builder.Append("idle, current job ").Append(pawn.CurJobDef?.defName ?? "none").ToString();
        }
    }
}
