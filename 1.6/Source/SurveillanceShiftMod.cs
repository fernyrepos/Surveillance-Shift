using HarmonyLib;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class SurveillanceShiftMod : Mod
    {
        public static SurveillanceShiftModSettings settings;

        public SurveillanceShiftMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<SurveillanceShiftModSettings>();
            new Harmony("ferny.SurveillanceShift").PatchAll();
        }

        public override string SettingsCategory() => Content.Name;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("SS_HideColonistBar".Translate(), ref settings.hideColonistBarAuthentic, "SS_HideColonistBarDesc".Translate());
            listing.End();
        }
    }
}
