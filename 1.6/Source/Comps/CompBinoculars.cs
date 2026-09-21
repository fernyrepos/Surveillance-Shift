using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class CompProperties_Binoculars : CompProperties
    {
        public CompProperties_Binoculars()
        {
            compClass = typeof(CompBinoculars);
        }
    }

    public class CompBinoculars : ThingComp
    {
        public Gizmo GetGizmo(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) return null;

            if (BinocularView.IsActive(pawn))
            {
                return new Command_Action
                {
                    defaultLabel = "SS_StopUsingBinoculars".Translate(),
                    defaultDesc = "SS_StopUsingBinocularsDesc".Translate(),
                    icon = parent.def.uiIcon,
                    action = () =>
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        BinocularView.Stop(pawn);
                    }
                };
            }

            string disabledReason = null;
            if (pawn.Downed) disabledReason = "SS_BinocularsUnusable".Translate();
            else if (State.IsLimitedAwareness is false) disabledReason = "SS_BinocularsNeedsLimitedAwareness".Translate();

            return new Command_Action
            {
                defaultLabel = "SS_UseBinoculars".Translate(),
                defaultDesc = "SS_UseBinocularsDesc".Translate(),
                icon = parent.def.uiIcon,
                disabled = disabledReason != null,
                disabledReason = disabledReason,
                action = () =>
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    pawn.pather.StopDead();
                    pawn.jobs.StopAll();
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefsOf.SS_UseBinoculars, pawn), JobTag.Misc);
                }
            };
        }

        public static CompBinoculars Find(Pawn pawn)
        {
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps twc)
                {
                    var comp = twc.GetComp<CompBinoculars>();
                    if (comp != null) return comp;
                }
            }
            return null;
        }
    }
}
