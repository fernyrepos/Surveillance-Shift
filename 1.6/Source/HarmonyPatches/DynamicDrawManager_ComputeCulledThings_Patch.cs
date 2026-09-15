using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.ComputeCulledThings))]
    public static class DynamicDrawManager_ComputeCulledThings_Patch
    {
        private static bool IsElevated(bool offscreen, Thing thing)
        {
            return thing is Pawn pawn && pawn.jobs?.curDriver is JobDriver_ManWatchtower driver && driver.forcedBodyOffset.HasValue && driver.forcedBodyOffset.Value.y > 0f || offscreen;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var defField = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            var drawOffscreenField = AccessTools.Field(typeof(ThingDef), nameof(ThingDef.drawOffscreen));
            var helper = AccessTools.Method(typeof(DynamicDrawManager_ComputeCulledThings_Patch), nameof(IsElevated));
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (i >= 2 && codes[i].LoadsField(drawOffscreenField) && codes[i - 1].LoadsField(defField))
                {
                    yield return new CodeInstruction(codes[i - 2].opcode, codes[i - 2].operand);
                    yield return new CodeInstruction(OpCodes.Call, helper);
                }
            }
        }
    }
}
