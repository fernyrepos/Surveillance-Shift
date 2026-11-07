using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class CompProperties_SignalHorn : CompProperties
    {
        public CompProperties_SignalHorn()
        {
            compClass = typeof(CompSignalHorn);
        }
    }

    public class CompSignalHorn : ThingComp
    {
        private const int CooldownTicks = 2500;
        public int lastBlownTick = -CooldownTicks;
        public bool CanBlowNow => Find.TickManager.TicksGame >= lastBlownTick + CooldownTicks;

        public Gizmo GetGizmo(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) return null;

            return new Command_Action
            {
                defaultLabel = "SS_BlowSignalHorn".Translate(),
                defaultDesc = "SS_BlowSignalHornDesc".Translate(),
                icon = parent.def.uiIcon,
                defaultIconColor = parent.DrawColor,
                disabled = CanBlowNow is false,
                disabledReason = "SS_SignalHornCooldown".Translate(),
                action = () => Blow(pawn)
            };
        }

        public void Blow(Pawn pawn)
        {
            if (CanBlowNow is false) return;
            lastBlownTick = Find.TickManager.TicksGame;
            DefsOf.SS_Sound_SignalHorn.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            var letter = LetterMaker.MakeLetter("SS_SignalHornLetterLabel".Translate(), "SS_SignalHornLetterText".Translate(pawn.LabelShort), LetterDefOf.ThreatBig, new LookTargets(pawn));
            State.ReceiveLetterUnmuted(letter);
            if (ModCompatibility.IsPerspectiveShiftAuthentic() && pawn == ModCompatibility.GetPerspectiveShiftAvatar())
            {
                RallyColonists(pawn);
            }
        }

        private static void RallyColonists(Pawn avatar)
        {
            var map = avatar.Map;
            foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist == avatar || colonist.Dead || colonist.Downed || colonist.InMentalState || colonist.WorkTagIsDisabled(WorkTags.Violent) || colonist.health.capacities.CapableOf(PawnCapacityDefOf.Moving) is false) continue;
                if (colonist.drafter != null) colonist.drafter.Drafted = true;
                var cell = CellFinder.RandomClosewalkCellNear(avatar.Position, map, 4, c => colonist.CanReach(c, PathEndMode.OnCell, Danger.Deadly));
                var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                colonist.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        public override bool AllowStackWith(Thing other)
        {
            var otherComp = other is ThingWithComps twc ? twc.GetComp<CompSignalHorn>() : null;
            return otherComp != null && CanBlowNow && otherComp.CanBlowNow && parent.DrawColor == other.DrawColor;
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            if (piece is ThingWithComps twc && twc.GetComp<CompSignalHorn>() is { } comp)
            {
                comp.lastBlownTick = lastBlownTick;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastBlownTick, "lastBlownTick", -CooldownTicks);
        }

        public static CompSignalHorn FindSignalHorn(Pawn pawn)
        {
            foreach (var eq in pawn.equipment.AllEquipmentListForReading)
            {
                var comp = eq.GetComp<CompSignalHorn>();
                if (comp != null) return comp;
            }
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps twc)
                {
                    var comp = twc.GetComp<CompSignalHorn>();
                    if (comp != null) return comp;
                }
            }
            return null;
        }
    }
}
