using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos))]
    public static class Pawn_EquipmentTracker_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn_EquipmentTracker __instance)
        {
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            var pawn = __instance.pawn;
            foreach (var eq in __instance.AllEquipmentListForReading)
            {
                var horn = eq.GetComp<CompSignalHorn>();
                if (horn is null) continue;
                var hornGizmo = horn.GetGizmo(pawn);
                if (hornGizmo != null) yield return hornGizmo;
            }
        }
    }
}
