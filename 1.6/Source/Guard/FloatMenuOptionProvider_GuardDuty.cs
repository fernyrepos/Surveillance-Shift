using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class FloatMenuOptionProvider_GuardDuty : FloatMenuOptionProvider
    {
        public override bool Drafted => false;
        public override bool Undrafted => true;
        public override bool Multiselect => true;
        public override bool MechanoidCanDo => true;
        public override bool RequiresManipulation => false;

        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            return pawn.CanGuard() && pawn.Spawned && pawn.Downed is false;
        }

        public override bool TargetThingValid(Thing thing, FloatMenuContext context)
        {
            return thing is Building_GuardPost || thing is Building_PatrolWaypoint { Route: not null };
        }

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            var pawns = context.ValidSelectedPawns.ToList();
            if (pawns.Count == 0) return null;
            var label = (clickedThing as Building_GuardPost)?.RenamableLabel ?? (clickedThing as Building_PatrolWaypoint)?.Route?.RenamableLabel;
            if (label is null) return null;
            if (pawns.All(p => p.CanReach(clickedThing, PathEndMode.OnCell, Danger.Some)) is false)
            {
                return new FloatMenuOption("CannotPrioritizeWorkGiver".Translate("SS_GuardDutyJob".Translate(), label) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            }
            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("SS_PrioritizeGuarding".Translate(label), () =>
            {
                foreach (var pawn in pawns)
                {
                    if (clickedThing is Building_GuardPost post) GuardUtility.AssignPost(pawn, post);
                    else if (clickedThing is Building_PatrolWaypoint { Route: { } route }) GuardUtility.AssignRoute(pawn, route);
                    pawn.GetGuardAssignment().keepWatch = true;
                    if (GuardUtility.DutyIsValid(pawn, out var assignment) && GuardUtility.MakeDutyJob(pawn, assignment) is { } job) pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }), pawns[0], clickedThing);
        }
    }
}
