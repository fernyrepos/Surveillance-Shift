using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    public class CompProperties_Telescope : CompProperties
    {
        public CompProperties_Telescope()
        {
            compClass = typeof(CompTelescope);
        }
    }

    public class CompTelescope : ThingComp
    {
        public Gizmo GetGizmo(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.Faction != Faction.OfPlayer) return null;

            if (ScopeView.IsActiveKind(pawn, ScopeKind.Telescope))
            {
                return new Command_Action
                {
                    defaultLabel = "SS_StopUsingTelescope".Translate(),
                    defaultDesc = "SS_StopUsingTelescopeDesc".Translate(),
                    icon = parent.def.uiIcon,
                    action = () =>
                    {
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        ScopeView.Stop(pawn);
                    }
                };
            }

            string disabledReason = null;
            if (pawn.Downed) disabledReason = "SS_TelescopeUnusable".Translate();
            else if (State.IsLimitedAwareness is false) disabledReason = "SS_TelescopeNeedsLimitedAwareness".Translate();

            return new Command_Action
            {
                defaultLabel = "SS_UseTelescope".Translate(),
                defaultDesc = "SS_UseTelescopeDesc".Translate(),
                icon = parent.def.uiIcon,
                disabled = disabledReason != null,
                disabledReason = disabledReason,
                action = () =>
                {
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    pawn.pather.StopDead();
                    pawn.jobs.StopAll();
                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefsOf.SS_UseTelescope, pawn), JobTag.Misc);
                }
            };
        }

        public static CompTelescope Find(Pawn pawn)
        {
            foreach (var thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps twc)
                {
                    var comp = twc.GetComp<CompTelescope>();
                    if (comp != null) return comp;
                }
            }
            return null;
        }
    }
}
