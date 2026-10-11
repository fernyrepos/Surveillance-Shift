using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.def is null || __instance.RaceProps is null) return;
            var loading = Scribe.mode == LoadSaveMode.LoadingVars;
            var saving = Scribe.mode == LoadSaveMode.Saving;
            var isPlayer = Find.FactionManager != null && __instance.Faction == Faction.OfPlayer;
            if (__instance.RaceProps.Animal && (isPlayer || loading))
            {
                __instance.GetSurveillanceData().ExposeData();
            }
            if (__instance.RaceProps.Humanlike && (isPlayer || loading))
            {
                __instance.GetColonistSurveillanceData().ExposeData();
            }
            if (loading || saving && isPlayer)
            {
                ExposeGuardAssignment(__instance, saving);
            }
        }

        private static void ExposeGuardAssignment(Pawn pawn, bool saving)
        {
            var assignment = saving ? pawn.GetGuardAssignment() : null;
            if (saving && assignment.IsActive is false) return;
            Scribe_Deep.Look(ref assignment, "SS_guardAssignment");
            if (saving is false && assignment != null) GuardUtility.ReplaceAssignment(pawn, assignment);
        }
    }
}
