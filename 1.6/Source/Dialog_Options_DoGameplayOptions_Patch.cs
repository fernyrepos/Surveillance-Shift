using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Dialog_Options), "DoGameplayOptions")]
    [HarmonyPriority(Priority.Low)]
    public static class Dialog_Options_DoGameplayOptions_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var injected = false;
            foreach (var inst in instructions)
            {
                if (!injected && inst.opcode == OpCodes.Ldstr && (string)inst.operand == "MaxNumberOfPlayerSettlements")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Dialog_Options_DoGameplayOptions_Patch), nameof(InjectButton)));
                    injected = true;
                }
                yield return inst;
            }
        }

        private static void InjectButton(Listing_Standard listing)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (listing.ButtonTextLabeledPct("SS_AwarenessSettings".Translate(), "Modify".Translate(), 0.6f, TextAnchor.UpperLeft))
                {
                    Find.WindowStack.Add(new Page_ChooseAwareness());
                }
            }
        }
    }
}
