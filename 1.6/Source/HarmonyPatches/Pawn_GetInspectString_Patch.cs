using System.Linq;
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
            if (Prefs.DevMode is false || __instance.Spawned is false || __instance.RaceProps.Humanlike || __instance.CanGuard() is false || GuardUtility.HasAnyDuty(__instance) is false) return;
            var status = Status(__instance, __instance.GetGuardAssignment());
            __result = __result.NullOrEmpty() ? status : __result + "\n" + status;
        }

        private static string Status(Pawn pawn, GuardAssignment assignment)
        {
            var builder = new StringBuilder("Guard: ");
            if (assignment.keepWatch is false) return builder.Append("keep watch off").ToString();
            if (GuardUtility.DutyIsValid(pawn, out _) is false) return builder.Append("no usable duty (").Append(GuardUtility.AssignedPosts(pawn).Count()).Append(" spots, ").Append(GuardUtility.AssignedRoutes(pawn).Count()).Append(" routes)").ToString();
            if (GuardUtility.IsGuardJob(pawn.CurJobDef)) return builder.Append("on ").Append(pawn.CurJobDef.defName).ToString();
            if (GuardUtility.NeedsAttention(pawn)) return builder.Append("needs attention (hungry/tired/hurt)").ToString();
            if (PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return builder.Append("player-forced job").ToString();
            return builder.Append("idle, current job ").Append(pawn.CurJobDef?.defName ?? "none").ToString();
        }
    }
}
