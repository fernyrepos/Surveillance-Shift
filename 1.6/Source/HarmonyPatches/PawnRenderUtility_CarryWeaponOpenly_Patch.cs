using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.CarryWeaponOpenly))]
    public static class PawnRenderUtility_CarryWeaponOpenly_Patch
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result || pawn.CurJob is not { } job) return;
            if (job.def == DefsOf.SS_GuardPostDuty)
            {
                __result = job.targetA.Thing is Building_GuardPost { IsMarker: true, showWeapon: true };
            }
            else if (job.def == DefsOf.SS_BodyguardDuty)
            {
                __result = true;
            }
            else if (job.def == DefsOf.SS_PatrolDuty)
            {
                __result = job.targetA.Thing is Building_PatrolWaypoint { Route.showWeapon: true };
            }
        }
    }
}
