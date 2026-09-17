using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class JobDriver_AnimalReportThreat : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            var report = ToilMaker.MakeToil("Report");
            report.initAction = () =>
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "SS_HostileSpottedMote".Translate());
                DefsOf.SS_Sound_PingHostile.PlayOneShotOnCamera(pawn.Map);
                var hostile = job.targetB.Thing as Pawn;
                var letterText = "SS_HostileSpottedDesc".Translate(pawn.LabelShort, hostile.LabelShort);
                State.ReceiveLetterUnmuted(LetterMaker.MakeLetter("SS_HostileSpotted".Translate(), letterText, LetterDefOf.ThreatSmall, new LookTargets(hostile)));
            };
            yield return report;
        }
    }
}
