using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class InspectStripButtons
    {
        private const float ButtonSize = 24f;
        private const float RimHudGap = 4f;
        private const string RimHudId = "jaxe.rimhud";

        private static readonly Texture2D InPersonReportTex = ContentFinder<Texture2D>.Get("UI/InPersonReport");
        private static readonly Texture2D WalkieTalkieTex = ContentFinder<Texture2D>.Get("Things/Item/Special/WalkieTalkie");
        private static readonly Texture2D AlarmTriggerTex = ContentFinder<Texture2D>.Get("Things/Building/AlarmTrigger/AlarmTrigger_north");

        static InspectStripButtons()
        {
            if (ModsConfig.IsActive(RimHudId) is false) return;
            var draw = AccessTools.Method("RimHUD.Interface.Screen.InspectPaneButtons:Draw");
            if (draw is null) return;
            SurveillanceShiftMod.harmony.Patch(draw, transpiler: new HarmonyMethod(typeof(InspectStripButtons), nameof(RimHudTranspiler)));
        }

        public static IEnumerable<CodeInstruction> RimHudTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var drawResponse = AccessTools.Method(typeof(HostilityResponseModeUtility), nameof(HostilityResponseModeUtility.DrawResponseButton));
            var allowResponse = AccessTools.Method("RimHUD.Interface.Screen.InspectPaneButtons:AllowResponse");
            var hookStart = AccessTools.Method(typeof(InspectStripButtons), nameof(RimHudHookStart));
            var hookAfterResponse = AccessTools.Method(typeof(InspectStripButtons), nameof(RimHudHookAfterResponse));
            var result = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(allowResponse) && result.Count > 0)
                {
                    var anchorIndex = result.Count - 1;
                    var first = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(result[anchorIndex]);
                    result.Insert(anchorIndex, first);
                    result.Insert(anchorIndex + 1, new CodeInstruction(OpCodes.Ldarg_2));
                    result.Insert(anchorIndex + 2, new CodeInstruction(OpCodes.Call, hookStart));
                }
                result.Add(instruction);
                if (instruction.Calls(drawResponse))
                {
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    result.Add(new CodeInstruction(OpCodes.Ldarg_2));
                    result.Add(new CodeInstruction(OpCodes.Call, hookAfterResponse));
                }
            }
            return result;
        }

        public static void RimHudHookStart(Rect bounds, ref float offset)
        {
            if (Find.Selector.SingleSelectedThing is not Pawn pawn || GuardButtonsWanted(pawn) is false) return;
            if (pawn.RaceProps.Animal) DrawAlert(RimHudRect(bounds, ref offset), pawn);
        }

        public static void RimHudHookAfterResponse(Rect bounds, ref float offset)
        {
            if (Find.Selector.SingleSelectedThing is not Pawn pawn || ThreatButtonWanted(pawn) is false) return;
            DrawThreatReport(RimHudRect(bounds, ref offset), pawn);
        }

        private static Rect RimHudRect(Rect bounds, ref float offset)
        {
            offset += ButtonSize;
            var result = new Rect(bounds.xMax - offset, bounds.y + (bounds.height - ButtonSize) / 2f, ButtonSize, ButtonSize);
            offset += RimHudGap;
            return result;
        }

        private static bool GuardButtonsWanted(Pawn pawn)
        {
            return Find.Selector.NumSelected == 1 && pawn.RaceProps.Humanlike is false && pawn.Spawned && pawn.CanGuard();
        }

        private static bool ThreatButtonWanted(Pawn pawn)
        {
            if (State.IsLimitedAwareness is false || pawn.Faction != Faction.OfPlayer || pawn.RaceProps.Humanlike is false || pawn.IsPrisoner || pawn.IsSlave || pawn.guilt.IsGuilty) return false;
            return pawn != ModCompatibility.GetPerspectiveShiftAvatar() && Find.Selector.NumSelected == 1;
        }

        public static void VanillaPostfix(Rect rect, ref float lineEndWidth)
        {
            if (ModsConfig.IsActive(RimHudId) || Find.Selector.SingleSelectedThing is not Pawn pawn) return;
            if (ThreatButtonWanted(pawn)) DrawThreatReport(VanillaRect(rect, ref lineEndWidth), pawn);
            if (GuardButtonsWanted(pawn) is false) return;
            if (pawn.RaceProps.Animal) DrawAlert(VanillaRect(rect, ref lineEndWidth), pawn);
        }

        private static Rect VanillaRect(Rect rect, ref float lineEndWidth)
        {
            var result = new Rect(rect.width - 24f - lineEndWidth - ButtonSize, 0f, ButtonSize, ButtonSize);
            lineEndWidth += ButtonSize;
            return result;
        }

        private static void DrawAlert(Rect rect, Pawn pawn)
        {
            var data = pawn.GetSurveillanceData();
            Widgets.DrawHighlightIfMouseover(rect);
            GUI.DrawTexture(rect, AlertIcon(data.alertResponse));
            TooltipHandler.TipRegion(rect, "SS_AlertResponseTooltip".Translate(("SS_" + data.alertResponse).Translate()));
            if (Widgets.ButtonInvisible(rect)) OpenAlertMenu(pawn);
        }

        public static void OpenAlertMenu(Pawn pawn)
        {
            var data = pawn.GetSurveillanceData();
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("SS_None".Translate(), () => data.alertResponse = AnimalAlertResponse.None, HostilityResponseMode.Ignore.GetIcon(), Color.white),
                new FloatMenuOption("SS_ReportToHandler".Translate(), () => data.alertResponse = AnimalAlertResponse.ReportToHandler, InPersonReportTex, Color.white),
                new FloatMenuOption("SS_UseAlarmTrigger".Translate(), () => data.alertResponse = AnimalAlertResponse.UseAlarmTrigger, AlarmTriggerTex, Color.white)
            }));
        }

        public static string AlertLabel(Pawn pawn)
        {
            return ("SS_" + pawn.GetSurveillanceData().alertResponse).Translate();
        }

        public static string ThreatLabel(Pawn pawn)
        {
            var data = pawn.GetColonistSurveillanceData();
            data.UpdateWalkieStatus(pawn);
            return data.reportReaction switch
            {
                ThreatReportReaction.ReportInPerson => "SS_ReportInPerson".Translate(),
                ThreatReportReaction.ReportViaRadio => "SS_ReportViaRadio".Translate(),
                ThreatReportReaction.ReportViaButton => "SS_ReportViaButton".Translate(),
                _ => "SS_ReportDoNotReport".Translate()
            };
        }

        private static Texture2D AlertIcon(AnimalAlertResponse response)
        {
            return response switch
            {
                AnimalAlertResponse.ReportToHandler => InPersonReportTex,
                AnimalAlertResponse.UseAlarmTrigger => AlarmTriggerTex,
                _ => HostilityResponseMode.Ignore.GetIcon()
            };
        }

        private static void DrawThreatReport(Rect rect, Pawn pawn)
        {
            var data = pawn.GetColonistSurveillanceData();
            data.UpdateWalkieStatus(pawn);

            Texture2D icon;
            string label;
            switch (data.reportReaction)
            {
                case ThreatReportReaction.ReportInPerson:
                    icon = InPersonReportTex;
                    label = "SS_ReportInPerson".Translate();
                    break;
                case ThreatReportReaction.ReportViaRadio:
                    icon = WalkieTalkieTex;
                    label = "SS_ReportViaRadio".Translate();
                    break;
                case ThreatReportReaction.ReportViaButton:
                    icon = AlarmTriggerTex;
                    label = "SS_ReportViaButton".Translate();
                    break;
                default:
                    icon = HostilityResponseMode.Ignore.GetIcon();
                    label = "SS_ReportDoNotReport".Translate();
                    break;
            }

            Widgets.DrawHighlightIfMouseover(rect);
            GUI.DrawTexture(rect, icon);
            TooltipHandler.TipRegion(rect, "SS_ThreatReportReactionDesc".Translate(label));
            if (Widgets.ButtonInvisible(rect)) OpenThreatMenu(pawn);
        }

        public static void OpenThreatMenu(Pawn pawn)
        {
            var data = pawn.GetColonistSurveillanceData();
            data.UpdateWalkieStatus(pawn);
            var options = new List<FloatMenuOption>();
            if (ModCompatibility.HasPerspectiveShiftAvatar())
            {
                options.Add(ReactionOption("SS_ReportInPerson", data, ThreatReportReaction.ReportInPerson, InPersonReportTex));
                if (pawn.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie))
                {
                    options.Add(ReactionOption("SS_ReportViaRadio", data, ThreatReportReaction.ReportViaRadio, WalkieTalkieTex));
                }
            }
            options.Add(ReactionOption("SS_ReportViaButton", data, ThreatReportReaction.ReportViaButton, AlarmTriggerTex));
            options.Add(ReactionOption("SS_ReportDoNotReport", data, ThreatReportReaction.DoNotReport, HostilityResponseMode.Ignore.GetIcon()));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static FloatMenuOption ReactionOption(string key, ColonistSurveillanceData data, ThreatReportReaction reaction, Texture2D icon)
        {
            return new FloatMenuOption(key.Translate(), () =>
            {
                data.reportReaction = reaction;
                data.reactionConfigured = true;
            }, icon, Color.white);
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.DoInspectPaneButtons))]
    public static class MainTabWindow_Inspect_DoInspectPaneButtons_Patch
    {
        public static void Postfix(Rect rect, ref float lineEndWidth) => InspectStripButtons.VanillaPostfix(rect, ref lineEndWidth);
    }
}
