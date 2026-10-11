using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class Dialog_RenameGuardThing : Dialog_Rename<Building_GuardPost>
    {
        public Dialog_RenameGuardThing(Building_GuardPost post) : base(post)
        {
        }
    }

    public class Dialog_RenameRoute : Dialog_Rename<PatrolRoute>
    {
        public Dialog_RenameRoute(PatrolRoute route) : base(route)
        {
        }
    }

    public class PendingPostName : IRenameable
    {
        private readonly Map map;
        private readonly IntVec3 cell;

        public PendingPostName(Map map, IntVec3 cell)
        {
            this.map = map;
            this.cell = cell;
        }

        public string RenamableLabel
        {
            get => map.GetComponent<MapComponent_Guarding>().PendingPostName(cell) ?? "";
            set => map.GetComponent<MapComponent_Guarding>().SetPendingPostName(cell, value);
        }

        public string BaseLabel => DefsOf.SS_GuardPost.LabelCap;
        public string InspectLabel => RenamableLabel;
    }

    public class Dialog_NameNewGuardPost : Dialog_Rename<PendingPostName>
    {
        public Dialog_NameNewGuardPost(Map map, IntVec3 cell) : base(new PendingPostName(map, cell))
        {
            curName = "";
        }
    }
}
