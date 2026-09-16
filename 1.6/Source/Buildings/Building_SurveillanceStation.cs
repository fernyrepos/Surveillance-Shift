using System;
using System.Collections.Generic;
using System.Text;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class Building_SurveillanceStation : Building
    {
        private StoragePriority priority = StoragePriority.Normal;
        private CompPowerTrader powerComp;

        public StoragePriority Priority
        {
            get => priority;
            set => priority = value;
        }

        public bool CanUseNow
        {
            get
            {
                if (Spawned is false || powerComp.PowerOn is false || Map.gameConditionManager.ElectricityDisabled(Map)) return false;
                return true;
            }
        }

        public bool HasCameras => Building_Camera.PipeNetHasCameras(GetComp<CompResource>().PipeNet);

        public bool IsBeingMonitored
        {
            get
            {
                if (CanUseNow is false) return false;
                var p = InteractionCell.GetFirstPawn(Map);
                return p != null && (p.CurJobDef == DefsOf.SS_OperateSurveillanceStation || p.CurJobDef == DefsOf.SS_ViewCameraFeeds) && p.CurJob.targetA.Thing == this;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(myPawn))
            {
                yield return opt;
            }

            if (myPawn == ModCompatibility.GetPerspectiveShiftAvatar())
            {
                yield return new FloatMenuOption("SS_ViewCameraFeeds".Translate(), () =>
                {
                    if (myPawn.Position == InteractionCell)
                    {
                        Find.WindowStack.Add(new Dialog_SurveillanceCameras(Map, GetComp<CompResource>().PipeNet));
                    }
                    else
                    {
                        var job = JobMaker.MakeJob(DefsOf.SS_ViewCameraFeeds, this);
                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                });
            }
            else
            {
                yield return new FloatMenuOption("SS_MonitorSurveillanceStation".Translate(), () =>
                {
                    var job = JobMaker.MakeJob(DefsOf.SS_OperateSurveillanceStation, this);
                    myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
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
            return text.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref priority, "priority", StoragePriority.Normal);
        }
    }
}
