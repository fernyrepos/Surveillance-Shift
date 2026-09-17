using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(HostilityResponseModeUtility), nameof(HostilityResponseModeUtility.DrawResponseButton))]
    public static class HostilityResponseModeUtility_DrawResponseButton_Patch
    {
        private const float ButtonGap = 4f;
        private static readonly Texture2D InPersonReportTex = ContentFinder<Texture2D>.Get("UI/InPersonReport");
        private static readonly Texture2D WalkieTalkieTex = ContentFinder<Texture2D>.Get("Things/Item/Special/WalkieTalkie");
        private static readonly Texture2D AlarmTriggerTex = ContentFinder<Texture2D>.Get("Things/Building/AlarmTrigger/AlarmTrigger_north");

        public static void Postfix(Rect rect, Pawn pawn)
        {
            if (State.IsLimitedAwareness is false || pawn.Faction != Faction.OfPlayer || pawn.RaceProps.Humanlike is false || pawn.IsPrisoner || pawn.IsSlave || pawn.guilt.IsGuilty || pawn == ModCompatibility.GetPerspectiveShiftAvatar() || Find.MainTabsRoot.OpenTab != null && Find.MainTabsRoot.OpenTab != MainButtonDefOf.Inspect || Find.Selector.SingleSelectedThing != pawn) return;

            var buttonRect = new Rect(rect.x - rect.width - ButtonGap, rect.y, rect.width, rect.height);
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

            Widgets.DrawHighlightIfMouseover(buttonRect);
            GUI.DrawTexture(buttonRect, icon);
            TooltipHandler.TipRegion(buttonRect, "SS_ThreatReportReactionDesc".Translate(label));

            if (Widgets.ButtonInvisible(buttonRect))
            {
                var options = new List<FloatMenuOption>();
                if (ModCompatibility.HasPerspectiveShiftAvatar())
                {
                    options.Add(new FloatMenuOption("SS_ReportInPerson".Translate(), () => data.reportReaction = ThreatReportReaction.ReportInPerson, InPersonReportTex, Color.white));
                    if (pawn.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie))
                    {
                        options.Add(new FloatMenuOption("SS_ReportViaRadio".Translate(), () => data.reportReaction = ThreatReportReaction.ReportViaRadio, WalkieTalkieTex, Color.white));
                    }
                }
                options.Add(new FloatMenuOption("SS_ReportViaButton".Translate(), () => data.reportReaction = ThreatReportReaction.ReportViaButton, AlarmTriggerTex, Color.white));
                options.Add(new FloatMenuOption("SS_ReportDoNotReport".Translate(), () => data.reportReaction = ThreatReportReaction.DoNotReport, HostilityResponseMode.Ignore.GetIcon(), Color.white));
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}
