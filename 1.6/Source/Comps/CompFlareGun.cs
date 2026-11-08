using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class CompProperties_FlareGun : CompProperties
    {
        public int chemfuelCost = 10;
        public int reloadTicks = 120;
        public int reportRadiusTiles = 20;

        public CompProperties_FlareGun()
        {
            compClass = typeof(CompFlareGun);
        }
    }

    public class CompFlareGun : ThingComp
    {
        public bool loaded;

        public CompProperties_FlareGun Props => (CompProperties_FlareGun)props;

        public IEnumerable<Gizmo> GetGizmos(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) yield break;

            string launchDisabled = null;
            if (loaded is false) launchDisabled = "SS_FlareNotLoaded".Translate(Props.chemfuelCost);
            else if (pawn.Downed) launchDisabled = "SS_FlareUnusable".Translate();
            else if (pawn.Map.roofGrid.Roofed(pawn.Position)) launchDisabled = "SS_FlareRoofed".Translate();

            yield return new Command_Action
            {
                defaultLabel = "SS_LaunchFlare".Translate(),
                defaultDesc = "SS_LaunchFlareDesc".Translate(Props.reportRadiusTiles),
                icon = parent.def.uiIcon,
                disabled = launchDisabled != null,
                disabledReason = launchDisabled,
                action = () => Launch(pawn, null)
            };

            if (loaded) yield break;

            yield return new Command_Action
            {
                defaultLabel = "SS_ReloadFlare".Translate(Props.chemfuelCost),
                defaultDesc = "SS_ReloadFlareDesc".Translate(Props.chemfuelCost),
                icon = ThingDefOf.Chemfuel.uiIcon,
                disabled = pawn.Downed,
                disabledReason = "SS_FlareUnusable".Translate(),
                action = () => StartReload(pawn)
            };
        }

        private Job MakeReloadJob(Pawn pawn, bool silent)
        {
            var cost = Props.chemfuelCost;
            var carried = pawn.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Chemfuel);
            Thing fuel = null;
            if (carried < cost)
            {
                fuel = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.Chemfuel), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, t => t.stackCount >= cost - carried && t.IsForbidden(pawn) is false && pawn.CanReserve(t, 1, cost - carried));
                if (fuel == null)
                {
                    if (silent is false) Messages.Message("SS_FlareNoChemfuel".Translate(cost), pawn, MessageTypeDefOf.RejectInput, false);
                    return null;
                }
            }
            var job = JobMaker.MakeJob(DefsOf.SS_ReloadFlareGun, fuel, parent);
            job.count = cost - carried;
            return job;
        }

        private void StartReload(Pawn pawn)
        {
            var job = MakeReloadJob(pawn, false);
            if (job != null) pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private void QueueReload(Pawn pawn)
        {
            if (pawn.Dead || pawn.Downed || pawn.Faction != Faction.OfPlayer) return;
            var job = MakeReloadJob(pawn, true);
            if (job != null) pawn.jobs.jobQueue.EnqueueLast(job, JobTag.Misc);
        }

        public void Launch(Pawn pawn, Thing hostile)
        {
            if (loaded is false || pawn.Spawned is false) return;
            loaded = false;
            var map = pawn.Map;
            DefsOf.RocketswarmLauncher_Fire.PlayOneShot(new TargetInfo(pawn.Position, map));
            var pos = pawn.DrawPos;
            FleckMaker.ThrowLightningGlow(pos, map, 3f);
            FleckMaker.ThrowFireGlow(pos, map, 2f);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.ExplosionFlash, 14f);
            State.SpawnPing(pawn.Position, map, true);
            QueueReload(pawn);
            if (CanReachAvatar(map) is false) return;
            var letter = hostile != null
                ? LetterMaker.MakeLetter("SS_FlareLetterLabel".Translate(), "SS_FlareHostileLetterText".Translate(pawn.LabelShort, hostile.LabelShort), LetterDefOf.ThreatBig, new LookTargets(hostile))
                : LetterMaker.MakeLetter("SS_FlareLetterLabel".Translate(), "SS_FlareLetterText".Translate(pawn.LabelShort), LetterDefOf.ThreatBig, new LookTargets(pawn));
            State.ReceiveLetterUnmuted(letter);
        }

        public bool CanReachAvatar(Map flareMap)
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar == null || ModCompatibility.IsPerspectiveShiftAuthentic() is false) return true;
            if (avatar.Spawned && avatar.Map == flareMap) return true;
            var tile = AvatarTile(avatar);
            if (tile < 0) return false;
            return Find.WorldGrid.ApproxDistanceInTiles(flareMap.Tile, tile) <= Props.reportRadiusTiles;
        }

        private static PlanetTile AvatarTile(Pawn avatar)
        {
            if (avatar.MapHeld != null) return avatar.MapHeld.Tile;
            var caravan = avatar.GetCaravan();
            return caravan != null ? caravan.Tile : PlanetTile.Invalid;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref loaded, "loaded");
        }

        public override string CompInspectStringExtra()
        {
            return (loaded ? "SS_FlareLoaded" : "SS_FlareEmpty").Translate();
        }

        public static CompFlareGun FindLoaded(Pawn pawn)
        {
            if (pawn.inventory == null) return null;
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps twc && twc.GetComp<CompFlareGun>() is { loaded: true } comp) return comp;
            }
            return null;
        }

        public static bool TryAutoFire(Pawn pawn, Thing hostile)
        {
            var comp = FindLoaded(pawn);
            if (comp == null || pawn.Spawned is false || pawn.Map.roofGrid.Roofed(pawn.Position)) return false;
            if (comp.CanReachAvatar(pawn.Map) is false) return false;
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            var avatarElsewhere = ModCompatibility.IsPerspectiveShiftAuthentic() && avatar != null && (avatar.Spawned is false || avatar.Map != pawn.Map);
            var noSignal = WalkieView.HasWalkie(pawn) is false && CompSignalHorn.FindSignalHorn(pawn) == null;
            if (avatarElsewhere is false && noSignal is false) return false;
            comp.Launch(pawn, hostile);
            return true;
        }
    }
}
