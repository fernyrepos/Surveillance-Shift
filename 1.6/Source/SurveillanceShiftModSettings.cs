using Verse;

namespace SurveillanceShift
{
    public enum AnimalUnfogRequirement
    {
        None,
        Guard,
        KeepWatch
    }

    public class SurveillanceShiftModSettings : ModSettings
    {
        public bool hideColonistBarAuthentic = true;
        public bool groupThreatReports = true;
        public bool animalsUnfog;
        public AnimalUnfogRequirement animalUnfogRequirement;
        public bool mechsUnfog;
        public bool enableScouts = true;
        public bool limitViewingMaps = true;
        public bool announceVisits;
        public bool enableAlertInDirector;
        public float friendlyScoutFrequency = 1f;
        public float hostileScoutFrequency = 1f;
        public int relationsLiePenalty = 40;
        public int relationsIgnorePenalty = 25;
        public int relationsTruthBoost = 10;

        private bool barForcedHidden;

        public bool HideColonistBar
        {
            get
            {
                var hide = hideColonistBarAuthentic && State.IsLimitedAwareness && ModCompatibility.IsPerspectiveShiftAuthentic();
                if (hide)
                {
                    if (Find.PlaySettings.showColonistBar)
                    {
                        Find.PlaySettings.showColonistBar = false;
                        barForcedHidden = true;
                        Write();
                    }
                }
                else if (barForcedHidden)
                {
                    barForcedHidden = false;
                    Find.PlaySettings.showColonistBar = true;
                    Write();
                }
                return hide;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref barForcedHidden, "barForcedHidden", false);
            Scribe_Values.Look(ref hideColonistBarAuthentic, "hideColonistBarAuthentic", true);
            Scribe_Values.Look(ref groupThreatReports, "groupThreatReports", true);
            Scribe_Values.Look(ref animalsUnfog, "animalsUnfog", false);
            Scribe_Values.Look(ref animalUnfogRequirement, "animalUnfogRequirement", AnimalUnfogRequirement.None);
            Scribe_Values.Look(ref mechsUnfog, "mechsUnfog", false);
            Scribe_Values.Look(ref enableScouts, "enableScouts", true);
            Scribe_Values.Look(ref limitViewingMaps, "limitViewingMaps", true);
            Scribe_Values.Look(ref announceVisits, "announceVisits", false);
            Scribe_Values.Look(ref enableAlertInDirector, "enableAlertInDirector", false);
            Scribe_Values.Look(ref friendlyScoutFrequency, "friendlyScoutFrequency", 1f);
            Scribe_Values.Look(ref hostileScoutFrequency, "hostileScoutFrequency", 1f);
            Scribe_Values.Look(ref relationsLiePenalty, "relationsLiePenalty", 40);
            Scribe_Values.Look(ref relationsIgnorePenalty, "relationsIgnorePenalty", 25);
            Scribe_Values.Look(ref relationsTruthBoost, "relationsTruthBoost", 10);
        }
    }
}
