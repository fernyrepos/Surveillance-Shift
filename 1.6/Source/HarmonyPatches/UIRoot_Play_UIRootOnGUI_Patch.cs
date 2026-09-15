using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class UIRoot_Play_UIRootOnGUI_Patch
    {
        public static void Postfix()
        {
            if (Event.current.type == EventType.Repaint && State.ActiveFeedCamera != null && ModCompatibility.HasPerspectiveShiftAvatar() is false)
            {
                State.DrawFeedReturnButton();
            }
        }
    }
}
