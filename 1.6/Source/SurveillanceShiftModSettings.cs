using Verse;

namespace SurveillanceShift
{
    public class SurveillanceShiftModSettings : ModSettings
    {
        public bool hideColonistBarAuthentic = true;

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
        }
    }
}
