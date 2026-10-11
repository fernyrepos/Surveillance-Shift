using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Building_GuardPost : Building, IRenameable
    {
        private string customName;

        public string RenamableLabel
        {
            get => customName.NullOrEmpty() ? BaseLabel : customName;
            set => customName = value;
        }

        public string BaseLabel => def.LabelCap;
        public string InspectLabel => RenamableLabel;
        public override string Label => customName.NullOrEmpty() ? base.Label : customName;

        public IEnumerable<Pawn> Occupants
        {
            get
            {
                if (Spawned is false) return Enumerable.Empty<Pawn>();
                return Map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.GetGuardAssignment() is { mode: GuardDutyMode.Post } a && a.post == this);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad || customName.NullOrEmpty() is false) return;
            var pending = map.GetComponent<MapComponent_Guarding>().TakePendingPostName(Position);
            if (pending.NullOrEmpty() is false) customName = pending;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref customName, "SS_customName");
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
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            var names = Occupants.Select(p => p.LabelShortCap).ToList();
            var line = names.Count == 0 ? "SS_PostUnmanned".Translate().ToString() : "SS_PostMannedBy".Translate(names.ToCommaList()).ToString();
            return text.NullOrEmpty() ? line : text + "\n" + line;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            var origin = Position.ToVector3Shifted();
            GenDraw.DrawArrowRotated(origin + Rotation.FacingCell.ToVector3() * 1.1f, Rotation.AsAngle, false);
        }

        public override void Print(SectionLayer layer)
        {
            if (Map.GetComponent<MapComponent_Guarding>().showPosts) base.Print(layer);
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (customName.NullOrEmpty() || Map.GetComponent<MapComponent_Guarding>().showPosts is false || Find.CameraDriver.CurrentZoom > CameraZoomRange.Close) return;
            GenMapUI.DrawThingLabel(this, customName, Color.white);
        }
    }
}
