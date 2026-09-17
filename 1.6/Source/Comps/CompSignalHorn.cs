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
                disabled = CanBlowNow is false,
                disabledReason = "SS_SignalHornCooldown".Translate(),
                action = () => Blow(pawn)
            };
        }

        public void Blow(Pawn pawn)
        {
            if (CanBlowNow is false) return;
            lastBlownTick = Find.TickManager.TicksGame;
            SoundDefOf.PsychicPulseGlobal.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            var letter = LetterMaker.MakeLetter("SS_SignalHornLetterLabel".Translate(), "SS_SignalHornLetterText".Translate(pawn.LabelShort), LetterDefOf.ThreatBig, new LookTargets(pawn));
            State.ReceiveLetterUnmuted(letter);
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
