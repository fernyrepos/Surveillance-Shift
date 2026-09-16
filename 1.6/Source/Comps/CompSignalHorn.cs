using RimWorld;
using Verse;
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
        private const int CooldownTicks = 1200;
        private int lastBlownTick = -CooldownTicks;

        public Gizmo GetGizmo(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) return null;

            var onCooldown = Find.TickManager.TicksGame < lastBlownTick + CooldownTicks;
            return new Command_Action
            {
                defaultLabel = "SS_BlowSignalHorn".Translate(),
                defaultDesc = "SS_BlowSignalHornDesc".Translate(),
                icon = parent.def.uiIcon,
                disabled = onCooldown,
                disabledReason = onCooldown ? "SS_SignalHornCooldown".Translate() : null,
                action = () => Blow(pawn)
            };
        }

        private void Blow(Pawn pawn)
        {
            lastBlownTick = Find.TickManager.TicksGame;
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            var letter = LetterMaker.MakeLetter("SS_SignalHornLetterLabel".Translate(), "SS_SignalHornLetterText".Translate(pawn.LabelShort), LetterDefOf.ThreatBig, new LookTargets(pawn));
            Find.LetterStack.ReceiveLetter(letter);
            State.SpawnPing(pawn.Position, pawn.Map, true);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref lastBlownTick, "lastBlownTick", -CooldownTicks);
        }
    }
}
