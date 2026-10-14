using Verse;

namespace SurveillanceShift
{
    public class GuardAssignment : IExposable
    {
        public GuardDutyMode mode;
        public Building_GuardPost post;
        public PatrolRoute route;
        public Pawn protectee;
        public Building_PatrolWaypoint lastWaypoint;
        public int direction = 1;
        public bool keepWatch = true;
        public bool forced;

        public GuardDutyMode legacyMode;
        public Building_GuardPost legacyPost;
        public int legacyRouteId = -1;

        public bool IsActive => mode != GuardDutyMode.None;

        public bool NeedsSaving => protectee != null || keepWatch is false;

        public bool HasLegacy => legacyMode == GuardDutyMode.Post && legacyPost != null || legacyMode == GuardDutyMode.Route && legacyRouteId >= 0;

        public GuardStance stance
        {
            get
            {
                switch (mode)
                {
                    case GuardDutyMode.Post:
                        return post?.stance ?? GuardStance.Defend;
                    case GuardDutyMode.Route:
                        return route?.stance ?? GuardStance.Defend;
                    default:
                        return GuardStance.Defend;
                }
            }
        }

        public void Clear()
        {
            mode = GuardDutyMode.None;
            post = null;
            route = null;
            lastWaypoint = null;
            direction = 1;
            forced = false;
        }

        public void ClearLegacy()
        {
            legacyMode = GuardDutyMode.None;
            legacyPost = null;
            legacyRouteId = -1;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref keepWatch, "keepWatch", true);
            Scribe_References.Look(ref protectee, "protectee");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Values.Look(ref legacyMode, "mode", GuardDutyMode.None);
                Scribe_Values.Look(ref legacyRouteId, "routeId", -1);
            }
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_References.Look(ref legacyPost, "post");
            }
        }
    }
}
