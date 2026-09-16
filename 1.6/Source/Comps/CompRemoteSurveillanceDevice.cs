using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class CompProperties_RemoteSurveillanceDevice : CompProperties
    {
        public CompProperties_RemoteSurveillanceDevice()
        {
            compClass = typeof(CompRemoteSurveillanceDevice);
        }
    }

    public class CompRemoteSurveillanceDevice : ThingComp
    {
        private Building_SurveillanceRouter linkedRouter;

        public Building_SurveillanceRouter LinkedRouter => linkedRouter;

        public void LinkTo(Building_SurveillanceRouter router)
        {
            if (linkedRouter != null && linkedRouter.linkedDevice == parent) linkedRouter.linkedDevice = null;
            if (router.linkedDevice != null) router.linkedDevice.GetComp<CompRemoteSurveillanceDevice>().NotifyRouterUnlinked(router);
            linkedRouter = router;
            router.linkedDevice = (ThingWithComps)parent;
        }

        public void NotifyRouterUnlinked(Building_SurveillanceRouter router)
        {
            if (linkedRouter == router) linkedRouter = null;
        }

        public Gizmo GetGizmo(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) return null;
            var avatar = pawn == ModCompatibility.GetPerspectiveShiftAvatar();
            string disabledReason = null;
            if (linkedRouter is null) disabledReason = "SS_DeviceNotLinked".Translate();
            else if (linkedRouter.IsActive is false) disabledReason = "SS_LinkedRouterOffline".Translate();

            if (avatar)
            {
                return new Command_Action
                {
                    defaultLabel = "SS_AccessFeedsRemote".Translate(),
                    defaultDesc = "SS_AccessFeedsRemoteDesc".Translate(),
                    icon = parent.def.uiIcon,
                    disabled = disabledReason != null,
                    disabledReason = disabledReason,
                    action = () =>
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        Find.WindowStack.Add(new Dialog_SurveillanceCameras(pawn.Map, linkedRouter.PipeNet));
                    }
                };
            }

            if (pawn.CurJobDef == DefsOf.SS_MonitorRemoteDevice)
            {
                return new Command_Action
                {
                    defaultLabel = "SS_AbortMonitoring".Translate(),
                    defaultDesc = "SS_AbortMonitoringDesc".Translate(),
                    icon = parent.def.uiIcon,
                    action = () =>
                    {
                        pawn.pather.StopDead();
                        pawn.jobs.StopAll();
                    }
                };
            }

            return new Command_Action
            {
                defaultLabel = "SS_UseRemoteSurveillanceDevice".Translate(),
                defaultDesc = "SS_AccessFeedsRemoteDesc".Translate(),
                icon = parent.def.uiIcon,
                disabled = disabledReason != null,
                disabledReason = disabledReason,
                action = () =>
                {
                    pawn.pather.StopDead();
                    pawn.jobs.StopAll();
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefsOf.SS_MonitorRemoteDevice, pawn), JobTag.Misc);
                }
            };
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref linkedRouter, "linkedRouter");
        }

        public static ThingWithComps FindDevice(Pawn pawn)
        {
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps twc && twc.HasComp<CompRemoteSurveillanceDevice>()) return twc;
            }
            return null;
        }

        public static Building_SurveillanceRouter FindLinkedRouter(Pawn pawn)
        {
            var device = FindDevice(pawn);
            return device is null ? null : device.GetComp<CompRemoteSurveillanceDevice>().LinkedRouter;
        }
    }
}
