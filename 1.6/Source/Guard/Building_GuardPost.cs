using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Building_GuardPost : Building, IRenameable, IPriorityTabTarget, IBehaviorTabTarget
    {
        private string customName;
        private StoragePriority priority = StoragePriority.Normal;
        public GuardStance stance = GuardStance.Defend;
        public bool hideVisual;

        public string RenamableLabel
        {
            get => customName.NullOrEmpty() ? BaseLabel : customName;
            set => customName = value;
        }

        public string BaseLabel => def.LabelCap;
        public string InspectLabel => RenamableLabel;
        public override string Label => customName.NullOrEmpty() ? base.Label : customName;

        public StoragePriority Priority
        {
            get => priority;
            set => priority = value;
        }

        public GuardStance Stance
        {
            get => stance;
            set => stance = value;
        }

        public CompAssignableToPawn_GuardPost AssignComp => GetComp<CompAssignableToPawn_GuardPost>();

        public virtual int GuardSlots => AssignComp?.MaxAssignedPawnsCount ?? 1;

        public virtual bool IsMarker => true;

        public IReadOnlyList<Pawn> AssignedGuards => (IReadOnlyList<Pawn>)AssignComp?.AssignedPawnsForReading ?? System.Array.Empty<Pawn>();

        public bool IsAssigned(Pawn pawn) => AssignComp?.AssignedPawnsForReading.Contains(pawn) ?? false;

        public virtual bool VisualHidden => hideVisual;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<MapComponent_Guarding>().Register(this);
            if (respawningAfterLoad || customName.NullOrEmpty() is false) return;
            var pending = map.GetComponent<MapComponent_Guarding>().TakePendingPostName(Position);
            if (pending.NullOrEmpty() is false) customName = pending;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var guarding = Map.GetComponent<MapComponent_Guarding>();
            base.DeSpawn(mode);
            guarding.Unregister(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref customName, "SS_customName");
            Scribe_Values.Look(ref priority, "priority", StoragePriority.Normal);
            Scribe_Values.Look(ref stance, "SS_stance", GuardStance.Defend);
            Scribe_Values.Look(ref hideVisual, "SS_hideVisual");
        }

        public void SetVisualHidden(bool hidden)
        {
            hideVisual = hidden;
            if (Spawned) Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "SS_RenamePost".Translate(),
                defaultDesc = "SS_RenamePostDesc".Translate(),
                icon = TexButton.Rename,
                action = () => Find.WindowStack.Add(new Dialog_RenameGuardThing(this))
            };

            if (IsMarker is false) yield break;

            yield return GuardMenus.HideVisualGizmo(this);
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            var lines = new List<string>();
            if (text.NullOrEmpty() is false) lines.Add(text);
            var names = AssignedGuards.Select(p => p.LabelShortCap).ToList();
            lines.Add(names.Count == 0 ? "SS_PostUnmanned".Translate().ToString() : "SS_PostMannedBy".Translate(names.ToCommaList()).ToString());
            lines.Add("Priority".Translate() + ": " + priority.Label().CapitalizeFirst());
            if (IsMarker) lines.Add("SS_GuardStance".Translate(GuardMenus.StanceLabel(stance)).ToString().Replace("\n", " "));
            return string.Join("\n", lines);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (IsMarker is false) return;
            var origin = Position.ToVector3Shifted();
            GenDraw.DrawArrowRotated(origin + Rotation.FacingCell.ToVector3() * 1.1f, Rotation.AsAngle, false);
        }

        public override void Print(SectionLayer layer)
        {
            if (VisualHidden is false) base.Print(layer);
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (customName.NullOrEmpty() || VisualHidden || Find.CameraDriver.CurrentZoom > CameraZoomRange.Close) return;
            GenMapUI.DrawThingLabel(this, customName, Color.white);
        }
    }
}
