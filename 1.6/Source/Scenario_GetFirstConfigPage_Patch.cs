using HarmonyLib;
using RimWorld;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Scenario), nameof(Scenario.GetFirstConfigPage))]
    [HarmonyAfter("PerspectiveShiftMod")]
    public static class Scenario_GetFirstConfigPage_Patch
    {
        public static void Postfix(ref Page __result)
        {
            var curr = __result;
            Page configPawns = null;
            Page perspectivePage = null;

            while (curr != null)
            {
                if (curr is Page_ConfigureStartingPawns)
                {
                    configPawns = curr;
                }
                if (curr.GetType().Name == "Page_ChoosePerspective")
                {
                    perspectivePage = curr;
                    break;
                }
                curr = curr.next;
            }

            if (perspectivePage != null && perspectivePage.prev is not Page_ChooseAwareness)
            {
                var awareness = new Page_ChooseAwareness();
                var prev = perspectivePage.prev;

                prev.next = awareness;
                awareness.prev = prev;

                awareness.next = perspectivePage;
                perspectivePage.prev = awareness;
            }
            else if (configPawns != null && configPawns.next is not Page_ChooseAwareness && perspectivePage == null)
            {
                var awareness = new Page_ChooseAwareness();
                var oldNext = configPawns.next;

                configPawns.next = awareness;
                awareness.prev = configPawns;

                if (oldNext != null)
                {
                    awareness.next = oldNext;
                    oldNext.prev = awareness;
                }
                else
                {
                    awareness.nextAct = configPawns.nextAct;
                    configPawns.nextAct = null;
                }
            }
        }
    }
}
