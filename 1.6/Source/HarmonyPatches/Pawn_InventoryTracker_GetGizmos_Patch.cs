using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.GetGizmos))]
    public static class Pawn_InventoryTracker_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn_InventoryTracker __instance)
        {
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            var pawn = __instance.pawn;
            foreach (var thing in __instance.innerContainer)
            {
                if (thing is not ThingWithComps twc) continue;
                var horn = twc.GetComp<CompSignalHorn>();
                if (horn != null)
                {
                    var hornGizmo = horn.GetGizmo(pawn);
                    if (hornGizmo != null) yield return hornGizmo;
                }
                var device = twc.GetComp<CompRemoteSurveillanceDevice>();
                if (device != null)
                {
                    var deviceGizmo = device.GetGizmo(pawn);
                    if (deviceGizmo != null) yield return deviceGizmo;
                }
            }
        }
    }
}
