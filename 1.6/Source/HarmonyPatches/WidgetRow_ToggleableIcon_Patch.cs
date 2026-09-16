using HarmonyLib;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(WidgetRow), nameof(WidgetRow.ToggleableIcon))]
    public static class WidgetRow_ToggleableIcon_Patch
    {
        public static bool Prefix(Texture2D tex) => tex != TexButton.ShowColonistBar || SurveillanceShiftMod.settings.HideColonistBar is false;
    }
}
