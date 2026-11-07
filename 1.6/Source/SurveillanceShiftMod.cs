using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class SurveillanceShiftMod : Mod
    {
        public static SurveillanceShiftModSettings settings;
        public static Harmony harmony;
        public SurveillanceShiftMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<SurveillanceShiftModSettings>();
            harmony = new Harmony("ferny.SurveillanceShift");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => Content.Name;

        private enum SettingsTab
        {
            General,
            Companions,
            Scouts,
        }

        private SettingsTab activeTab = SettingsTab.General;
        private Vector2 scrollPosition;
        private float scrollHeight = 9999999f;
        private readonly List<TabRecord> tabRecords = new List<TabRecord>();

        public override void DoSettingsWindowContents(Rect rect)
        {
            var footerRect = new Rect(rect.x, rect.yMax - 34f, rect.width, 30f);
            var tabRect = rect;
            tabRect.yMin += 32f;
            tabRect.yMax -= 40f;

            tabRecords.Clear();
            AddTab(SettingsTab.General, "SS_TabGeneral");
            AddTab(SettingsTab.Companions, "SS_TabCompanions");
            AddTab(SettingsTab.Scouts, "SS_TabScouts");

            Widgets.DrawMenuSection(tabRect);
            TabDrawer.DrawTabs(tabRect, tabRecords);

            var inner = tabRect.ContractedBy(12f);
            var viewRect = new Rect(0f, 0f, inner.width - 30f, scrollHeight);
            Widgets.BeginScrollView(inner, ref scrollPosition, viewRect);
            var listing = new Listing_Standard();
            listing.maxOneColumn = true;
            listing.Begin(viewRect);
            var startY = listing.curY;

            switch (activeTab)
            {
                case SettingsTab.General: DoGeneralTab(listing); break;
                case SettingsTab.Companions: DoCompanionsTab(listing); break;
                default: DoScoutsTab(listing); break;
            }

            scrollHeight = listing.curY - startY;
            listing.End();
            Widgets.EndScrollView();

            DoResetButton(footerRect);
        }

        private void AddTab(SettingsTab tab, string labelKey)
        {
            tabRecords.Add(new TabRecord(labelKey.Translate(), delegate
            {
                if (activeTab == tab) return;
                activeTab = tab;
                scrollPosition = Vector2.zero;
                scrollHeight = 9999999f;
            }, activeTab == tab));
        }

        private static void DoGeneralTab(Listing_Standard listing)
        {
            listing.CheckboxLabeled("SS_HideColonistBar".Translate(), ref settings.hideColonistBarAuthentic, "SS_HideColonistBarDesc".Translate());
            listing.CheckboxLabeled("SS_GroupThreatReports".Translate(), ref settings.groupThreatReports, "SS_GroupThreatReportsDesc".Translate());
            listing.CheckboxLabeled("SS_LimitViewingMaps".Translate(), ref settings.limitViewingMaps);
            listing.CheckboxLabeled("SS_EnableAlertInDirector".Translate(), ref settings.enableAlertInDirector);
        }

        private static string RequirementLabel(AnimalUnfogRequirement requirement)
        {
            switch (requirement)
            {
                case AnimalUnfogRequirement.Guard: return "SS_AnimalUnfogGuard".Translate();
                case AnimalUnfogRequirement.KeepWatch: return "SS_KeepWatch".Translate().CapitalizeFirst();
                default: return "None".Translate();
            }
        }

        private static void DoCompanionsTab(Listing_Standard listing)
        {
            listing.CheckboxLabeled("SS_AnimalsUnfog".Translate(), ref settings.animalsUnfog, "SS_AnimalsUnfogDesc".Translate());
            if (settings.animalsUnfog)
            {
                var row = listing.GetRect(30f);
                Widgets.Label(row.LeftPart(0.6f), "SS_AnimalUnfogRequirement".Translate());
                TooltipHandler.TipRegion(row, "SS_AnimalUnfogRequirementDesc".Translate());
                if (Widgets.ButtonText(row.RightPart(0.4f), RequirementLabel(settings.animalUnfogRequirement)))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (AnimalUnfogRequirement requirement in Enum.GetValues(typeof(AnimalUnfogRequirement)))
                    {
                        var chosen = requirement;
                        options.Add(new FloatMenuOption(RequirementLabel(chosen), delegate { settings.animalUnfogRequirement = chosen; }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            listing.CheckboxLabeled("SS_MechsUnfog".Translate(), ref settings.mechsUnfog, "SS_MechsUnfogDesc".Translate());
        }

        private static void DoScoutsTab(Listing_Standard listing)
        {
            listing.CheckboxLabeled("SS_EnableScouts".Translate(), ref settings.enableScouts);
            listing.CheckboxLabeled("SS_AnnounceVisits".Translate(), ref settings.announceVisits);

            listing.Label("SS_FriendlyScoutFrequency".Translate() + ": " + settings.friendlyScoutFrequency.ToStringPercent());
            settings.friendlyScoutFrequency = listing.Slider(settings.friendlyScoutFrequency, 0f, 5f);

            listing.Label("SS_HostileScoutFrequency".Translate() + ": " + settings.hostileScoutFrequency.ToStringPercent());
            settings.hostileScoutFrequency = listing.Slider(settings.hostileScoutFrequency, 0f, 5f);

            listing.Label("SS_RelationsLiePenalty".Translate() + ": " + settings.relationsLiePenalty);
            settings.relationsLiePenalty = (int)listing.Slider(settings.relationsLiePenalty, 0f, 100f);

            listing.Label("SS_RelationsIgnorePenalty".Translate() + ": " + settings.relationsIgnorePenalty);
            settings.relationsIgnorePenalty = (int)listing.Slider(settings.relationsIgnorePenalty, 0f, 100f);

            listing.Label("SS_RelationsTruthBoost".Translate() + ": " + settings.relationsTruthBoost);
            settings.relationsTruthBoost = (int)listing.Slider(settings.relationsTruthBoost, 0f, 100f);
        }

        private void DoResetButton(Rect rect)
        {
            var buttonRect = new Rect(rect.xMax - 220f, rect.y, 220f, rect.height);
            if (Widgets.ButtonText(buttonRect, "SS_ResetToDefault".Translate()) is false) return;

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("SS_ResetThisTab".Translate(TabLabel(activeTab)), delegate
                {
                    ResetTab(activeTab);
                    settings.Write();
                }),
                new FloatMenuOption("SS_ResetAll".Translate(), ConfirmResetAll),
            }));
        }

        private static void ConfirmResetAll()
        {
            var dialog = Dialog_MessageBox.CreateConfirmation("SS_ResetAllConfirm".Translate(), ResetAll, true);
            dialog.layer = WindowLayer.Super;
            Find.WindowStack.Add(dialog);
        }

        private static string TabLabel(SettingsTab tab)
        {
            switch (tab)
            {
                case SettingsTab.General: return "SS_TabGeneral".Translate();
                case SettingsTab.Companions: return "SS_TabCompanions".Translate();
                default: return "SS_TabScouts".Translate();
            }
        }

        private static void ResetAll()
        {
            foreach (SettingsTab tab in Enum.GetValues(typeof(SettingsTab)))
            {
                ResetTab(tab);
            }
            settings.Write();
        }

        private static void ResetTab(SettingsTab tab)
        {
            var d = new SurveillanceShiftModSettings();
            switch (tab)
            {
                case SettingsTab.General:
                    settings.hideColonistBarAuthentic = d.hideColonistBarAuthentic;
                    settings.groupThreatReports = d.groupThreatReports;
                    settings.limitViewingMaps = d.limitViewingMaps;
                    settings.enableAlertInDirector = d.enableAlertInDirector;
                    break;

                case SettingsTab.Companions:
                    settings.animalsUnfog = d.animalsUnfog;
                    settings.animalUnfogRequirement = d.animalUnfogRequirement;
                    settings.mechsUnfog = d.mechsUnfog;
                    break;

                default:
                    settings.enableScouts = d.enableScouts;
                    settings.announceVisits = d.announceVisits;
                    settings.friendlyScoutFrequency = d.friendlyScoutFrequency;
                    settings.hostileScoutFrequency = d.hostileScoutFrequency;
                    settings.relationsLiePenalty = d.relationsLiePenalty;
                    settings.relationsIgnorePenalty = d.relationsIgnorePenalty;
                    settings.relationsTruthBoost = d.relationsTruthBoost;
                    break;
            }
        }
    }
}
