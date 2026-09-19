using System.Collections.Generic;
using System.Text;
using PipeSystem;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class Building_Camera : Building, IRenameable
    {
        protected const int BaseSightRadius = 24;

        public string customName;
        private CompPowerTrader powerComp;
        private CompResource compResource;
        private CameraExtension extension;

        public string RenamableLabel
        {
            get => customName.NullOrEmpty() ? LabelCap : customName;
            set => customName = value;
        }

        public string BaseLabel => def.label;

        public string InspectLabel => RenamableLabel;

        public CameraExtension Extension => extension ??= def.GetModExtension<CameraExtension>();

        public virtual int SightRadius => Extension?.sightRadius ?? BaseSightRadius;

        public bool IsActive
        {
            get
            {
                if (Spawned is false || powerComp.PowerOn is false) return false;
                return true;
            }
        }

        public static bool PipeNetHasCameras(PipeNet net)
        {
            if (net is null) return false;
            foreach (var connector in net.connectors)
            {
                if (connector.parent is Building_Camera cam && cam.IsActive) return true;
            }
            return false;
        }

        public virtual bool IsMonitored
        {
            get
            {
                if (IsActive is false) return false;
                if (ModCompatibility.IsPerspectiveShiftCameraLockedTo(Position)) return true;
                if (compResource.PipeNet == null) return false;
                foreach (var connector in compResource.PipeNet.connectors)
                {
                    if (connector.parent is Building_SurveillanceStation station && station.IsBeingMonitored || connector.parent is Building_SurveillanceRouter router && router.IsBeingMonitored)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            compResource = GetComp<CompResource>();
            if (respawningAfterLoad is false && customName.NullOrEmpty() && Faction == Faction.OfPlayer)
            {
                Find.WindowStack.Add(new Dialog_RenameBuildingCamera(this));
            }
        }

        public virtual void ComputeCameraFOV(bool[] visibleCells)
        {
            if (IsActive is false || IsMonitored is false) return;
            ShadowCaster.ComputeFieldOfView(Position, SightRadius, Map, visibleCells);
        }

        public override string GetInspectString()
        {
            var text = new StringBuilder();
            if (customName.NullOrEmpty() is false)
            {
                text.AppendLine(customName);
            }
            text.Append(IsMonitored ? "SS_CameraMonitored".Translate() : "SS_CameraUnmonitored".Translate());
            text.AppendInNewLine(base.GetInspectString());
            return text.ToString().TrimEndNewlines();
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
                    defaultLabel = customName.NullOrEmpty() ? "SS_Name".Translate() : "Rename".Translate(),
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
