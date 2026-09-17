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

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled("SS_HideColonistBar".Translate(), ref settings.hideColonistBarAuthentic, "SS_HideColonistBarDesc".Translate());
            listing.CheckboxLabeled("SS_EnableScouts".Translate(), ref settings.enableScouts);
            listing.CheckboxLabeled("SS_LimitViewingMaps".Translate(), ref settings.limitViewingMaps);
            listing.CheckboxLabeled("SS_AnnounceVisits".Translate(), ref settings.announceVisits);
            listing.CheckboxLabeled("SS_EnableAlertInDirector".Translate(), ref settings.enableAlertInDirector);

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

            listing.Label("SS_FogViewDistance".Translate() + ": " + settings.fogViewDistance);
            settings.fogViewDistance = (int)listing.Slider(settings.fogViewDistance, 10f, 100f);

            listing.End();
        }
    }
}
