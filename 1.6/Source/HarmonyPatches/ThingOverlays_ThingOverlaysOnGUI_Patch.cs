using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(ThingOverlays), nameof(ThingOverlays.ThingOverlaysOnGUI))]
    public static class ThingOverlays_ThingOverlaysOnGUI_Patch
    {
        private static bool ShowOverlay(bool contains, Thing thing)
        {
            return contains || thing is Pawn pawn && pawn.jobs?.curDriver is JobDriver_ManWatchtower driver && driver.forcedBodyOffset.HasValue && driver.forcedBodyOffset.Value.y > 0f;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var contains = AccessTools.Method(typeof(CellRect), nameof(CellRect.Contains), new[] { typeof(IntVec3) });
            var helper = AccessTools.Method(typeof(ThingOverlays_ThingOverlaysOnGUI_Patch), nameof(ShowOverlay));
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (i >= 2 && codes[i].Calls(contains))
                {
                    yield return new CodeInstruction(codes[i - 2].opcode, codes[i - 2].operand);
                    yield return new CodeInstruction(OpCodes.Call, helper);
                }
            }
        }
    }
}
