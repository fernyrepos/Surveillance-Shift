using Verse;

namespace SurveillanceShift
{
    public class SurveillanceShiftModSettings : ModSettings
    {
        public bool hideColonistBarAuthentic = true;
        public bool enableScouts = true;
        public bool limitViewingMaps = true;
        public bool announceVisits;
        public bool enableAlertInDirector;
        public float friendlyScoutFrequency = 1f;
        public float hostileScoutFrequency = 1f;
        public int relationsLiePenalty = 40;
        public int relationsIgnorePenalty = 25;
        public int relationsTruthBoost = 10;
        public int fogViewDistance = 30;

        public bool HideColonistBar
        {
            get
            {
                var hide = hideColonistBarAuthentic && State.IsLimitedAwareness && ModCompatibility.IsPerspectiveShiftAuthentic();
                if (hide) Find.PlaySettings.showColonistBar = false;
                return hide;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hideColonistBarAuthentic, "hideColonistBarAuthentic", true);
            Scribe_Values.Look(ref enableScouts, "enableScouts", true);
            Scribe_Values.Look(ref limitViewingMaps, "limitViewingMaps", true);
            Scribe_Values.Look(ref announceVisits, "announceVisits", false);
            Scribe_Values.Look(ref enableAlertInDirector, "enableAlertInDirector", false);
            Scribe_Values.Look(ref friendlyScoutFrequency, "friendlyScoutFrequency", 1f);
            Scribe_Values.Look(ref hostileScoutFrequency, "hostileScoutFrequency", 1f);
            Scribe_Values.Look(ref relationsLiePenalty, "relationsLiePenalty", 40);
            Scribe_Values.Look(ref relationsIgnorePenalty, "relationsIgnorePenalty", 25);
            Scribe_Values.Look(ref relationsTruthBoost, "relationsTruthBoost", 10);
            Scribe_Values.Look(ref fogViewDistance, "fogViewDistance", 30);
        }
    }
}
