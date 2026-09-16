using HarmonyLib;
using RimWorld;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    public static class ColonistBar_ColonistBarOnGUI_Patch
    {
        public static bool Prefix() => SurveillanceShiftMod.settings.HideColonistBar is false;
    }
}
