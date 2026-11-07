using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.LovePartnerRelationExists))]
    public static class LovePartnerRelationUtility_LovePartnerRelationExists_Patch
    {
        private static Type dialogType;
        private static FieldInfo compField;
        private static bool resolved;

        public static bool Prefix(ref bool __result)
        {
            var windows = Current.ProgramState == ProgramState.Playing ? Find.WindowStack?.Windows : null;
            if (windows == null || windows.Count == 0) return true;
            if (resolved is false)
            {
                resolved = true;
                dialogType = AccessTools.TypeByName("DubsMintMenus.Dialog_AssignBuildingOwner");
                compField = dialogType == null ? null : AccessTools.Field(dialogType, "comp");
            }
            if (compField == null) return true;
            for (var i = 0; i < windows.Count; i++)
            {
                if (windows[i].GetType() != dialogType) continue;
                if (compField.GetValue(windows[i]) is not CompAssignableToPawn_Guard) continue;
                __result = false;
                return false;
            }
            return true;
        }
    }
}
