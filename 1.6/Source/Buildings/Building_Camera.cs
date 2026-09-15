using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class Building_Camera : Building, IRenameable
    {
        protected const int BaseSightRadius = 24;

        private string customName;
        private CompPowerTrader powerComp;

        public string RenamableLabel
        {
            get => customName.NullOrEmpty() ? LabelCap : customName;
            set => customName = value;
        }

        public string BaseLabel => def.label;

        public string InspectLabel => RenamableLabel;

        public virtual int SightRadius => BaseSightRadius;

        public bool IsActive
        {
            get
            {
                if (Spawned is false || powerComp != null && powerComp.PowerOn is false) return false;
                return true;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            if (respawningAfterLoad is false && customName.NullOrEmpty() && Faction == Faction.OfPlayer)
            {
                Find.WindowStack.Add(new Dialog_RenameBuildingCamera(this));
            }
        }

        public virtual void ComputeCameraFOV(bool[] visibleCells)
        {
            if (IsActive is false) return;
            ShadowCaster.ComputeFieldOfView(Position, SightRadius, Map, visibleCells);
        }

        public override string GetInspectString()
        {
            var baseStr = base.GetInspectString();
            if (customName.NullOrEmpty()) return baseStr;
            return customName + (baseStr.NullOrEmpty() ? "" : "\n" + baseStr);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (Faction == Faction.OfPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Rename".Translate(),
                    icon = TexButton.Rename,
                    action = () => Find.WindowStack.Add(new Dialog_RenameBuildingCamera(this))
                };
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref customName, "customName");
        }
    }
}
