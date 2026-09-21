using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class Building_SurveillanceRouter : Building
    {
        private CompPowerTrader powerComp;
        public ThingWithComps linkedDevice;

        public bool IsActive => Spawned && powerComp.PowerOn && Map.gameConditionManager.ElectricityDisabled(Map) is false;
        public PipeNet PipeNet => GetComp<CompResource>().PipeNet;

        public bool HasCameras => Building_Camera.PipeNetHasCameras(PipeNet);

        public bool IsBeingMonitored
        {
            get
            {
                if (IsActive is false || linkedDevice is null || linkedDevice.ParentHolder is not Pawn_InventoryTracker inv) return false;
                var pawn = inv.pawn;
                return pawn.Spawned && pawn.CurJobDef == DefsOf.SS_MonitorRemoteDevice && pawn.Map == Map;
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

            if (State.IsLimitedAwareness is false) yield break;
            var device = CompRemoteSurveillanceDevice.FindDevice(myPawn);
            if (device is null) yield break;
            if (linkedDevice != null && linkedDevice != device)
            {
                yield return new FloatMenuOption("SS_AlreadyLinkedToTablet".Translate(), null);
            }
            else if (myPawn.CanReach(this, PathEndMode.Touch, Danger.Some) is false)
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }
            else
            {
                yield return new FloatMenuOption("SS_LinkDeviceToRouter".Translate(), () =>
                            {
                                var job = JobMaker.MakeJob(DefsOf.SS_LinkDeviceToRouter, this, device);
                                myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            });
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref linkedDevice, "linkedDevice");
        }
    }
}
