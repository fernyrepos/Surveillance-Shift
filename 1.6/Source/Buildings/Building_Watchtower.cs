using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class Building_Watchtower : Building
    {
        private StoragePriority priority = StoragePriority.Normal;
        private WatchtowerExtension extension;
        private Graphic topGraphic;
        private GraphicData cachedTopGraphicData;

        public WatchtowerExtension Extension => extension ??= def.GetModExtension<WatchtowerExtension>();

        private Graphic TopGraphic
        {
            get
            {
                var data = Extension.topGraphicData;
                if (data != null && data != cachedTopGraphicData)
                {
                    cachedTopGraphicData = data;
                    topGraphic = data.Graphic;
                }
                return topGraphic;
            }
        }

        public StoragePriority Priority
        {
            get => priority;
            set => priority = value;
        }

        public bool IsManned => MannedPawn != null;

        public bool IsMannedByAvatar => MannedPawn != null && MannedPawn == ModCompatibility.GetPerspectiveShiftAvatar();

        public Pawn MannedPawn
        {
            get
            {
                if (Spawned is false) return null;
                var occupant = InteractionCell.GetFirstPawn(Map);
                if (occupant != null && occupant.CurJobDef == DefsOf.SS_ManWatchtower && occupant.CurJob?.targetA.Thing == this && occupant.jobs?.curDriver is JobDriver_ManWatchtower driver && driver.IsAtTop)
                {
                    return occupant;
                }
                foreach (var p in Map.mapPawns.AllPawnsSpawned)
                {
                    if (p.CurJobDef == DefsOf.SS_ManWatchtower && p.CurJob?.targetA.Thing == this && p.jobs?.curDriver is JobDriver_ManWatchtower d && d.IsAtTop)
                    {
                        return p;
                    }
                }
                return null;
            }
        }

        public void ComputeWatchtowerFOV(bool[] visibleCells)
        {
            if (IsManned is false) return;
            ShadowCaster.ComputeFieldOfView(this.OccupiedRect().CenterCell, Extension.sightRadius, Map, visibleCells, Extension.seeOverMountains);
        }

        public bool CanWatchtowerSee(IntVec3 targetCell)
        {
            var center = this.OccupiedRect().CenterCell;
            var radius = Extension.sightRadius;
            if (center.DistanceToSquared(targetCell) > radius * radius) return false;
            if (Extension.seeOverMountains)
            {
                return Map.roofGrid.Roofed(targetCell) is false;
            }
            return GenSight.LineOfSight(center, targetCell, Map, skipFirstCell: true);
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (phase == DrawPhase.Draw)
            {
                var pos = drawLoc;
                pos.y = AltitudeLayer.PawnUnused.AltitudeFor() + 1;
                TopGraphic.Draw(pos, Rotation, this);
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(myPawn))
            {
                yield return opt;
            }

            if (myPawn.Drafted) yield break;

            yield return new FloatMenuOption("SS_ManWatchtower".Translate(), () =>
            {
                var job = JobMaker.MakeJob(DefsOf.SS_ManWatchtower, this);
                myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
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
                    defaultLabel = "Priority".Translate() + ": " + priority.Label().CapitalizeFirst(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>();
                        foreach (StoragePriority pr in Enum.GetValues(typeof(StoragePriority)))
                        {
                            if (pr == StoragePriority.Unstored) continue;
                            var localPr = pr;
                            options.Add(new FloatMenuOption(localPr.Label().CapitalizeFirst(), () => priority = localPr));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
        }

        public override string GetInspectString()
        {
            var text = new StringBuilder(base.GetInspectString());
            text.AppendInNewLine("Priority".Translate() + ": " + priority.Label().CapitalizeFirst());
            text.AppendInNewLine(IsManned ? "SS_WatchtowerManned".Translate() : "SS_WatchtowerUnmanned".Translate());
            return text.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref priority, "priority", StoragePriority.Normal);
        }
    }
}
