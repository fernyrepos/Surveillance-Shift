using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class ThinkNode_ConditionalAnimalPatrolling : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer || State.GuardingPawnsActive is false || pawn.training.HasLearned(DefsOf.SS_KeepWatch) is false) return false;
            return pawn.GetSurveillanceData().patrolEnabled;
        }
    }

    public class JobGiver_AnimalPatrol : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.needs.food != null && pawn.needs.food.CurLevelPercentage < 0.2f || pawn.needs.rest != null && pawn.needs.rest.CurLevelPercentage < 0.2f) return null;

            var spots = pawn.Map.listerBuildings.allBuildingsColonist
                .Where(b => b.def.defName.StartsWith("GuardingP_") && b.def.defName.EndsWith("Patrol"));

            foreach (var spot in spots.InRandomOrder())
            {
                if (spot.Position == pawn.Position) continue;
                if (pawn.CanReserveAndReach(spot.Position, PathEndMode.OnCell, Danger.Some))
                {
                    return JobMaker.MakeJob(JobDefOf.Goto, spot.Position);
                }
            }
            return null;
        }
    }
}
