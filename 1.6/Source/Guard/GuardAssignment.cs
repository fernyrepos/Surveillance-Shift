using Verse;

namespace SurveillanceShift
{
    public class GuardAssignment : IExposable
    {
        public GuardDutyMode mode;
        public GuardStance stance = GuardStance.Defend;
        public Building_GuardPost post;
        public int routeId = -1;
        public Pawn protectee;
        public Building_PatrolWaypoint lastWaypoint;
        public int direction = 1;
        public bool keepWatch = true;

        public bool IsActive => mode != GuardDutyMode.None;

        public void Clear()
        {
            mode = GuardDutyMode.None;
            post = null;
            routeId = -1;
            protectee = null;
            lastWaypoint = null;
            direction = 1;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref mode, "mode", GuardDutyMode.None);
            Scribe_Values.Look(ref stance, "stance", GuardStance.Defend);
            Scribe_Values.Look(ref routeId, "routeId", -1);
            Scribe_Values.Look(ref direction, "direction", 1);
            Scribe_Values.Look(ref keepWatch, "keepWatch", true);
            Scribe_References.Look(ref post, "post");
            Scribe_References.Look(ref protectee, "protectee");
            Scribe_References.Look(ref lastWaypoint, "lastWaypoint");
        }
    }
}
