using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class CompProperties_GuardPostAssignable : CompProperties_AssignableToPawn
    {
        public CompProperties_GuardPostAssignable()
        {
            compClass = typeof(CompAssignableToPawn_GuardPost);
            drawAssignmentOverlay = false;
            drawUnownedAssignmentOverlay = false;
        }
    }

    public class CompProperties_PatrolAssignable : CompProperties_AssignableToPawn
    {
        public CompProperties_PatrolAssignable()
        {
            compClass = typeof(CompAssignableToPawn_Patrol);
            drawAssignmentOverlay = false;
            drawUnownedAssignmentOverlay = false;
        }
    }

    public abstract class CompAssignableToPawn_Guard : CompAssignableToPawn
    {
        public override IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                return parent.Spawned ? GuardUtility.EligibleGuards(parent.Map) : Enumerable.Empty<Pawn>();
            }
        }

        public override bool AssignedAnything(Pawn pawn) => false;

        public override string GetAssignmentGizmoDesc() => "SS_AssignGuardsDesc".Translate(MaxAssignedPawnsCount);

        public override string GetAssignmentGizmoLabel() => "SS_AssignGuards".Translate();
    }

    public class CompAssignableToPawn_GuardPost : CompAssignableToPawn_Guard
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            assignedPawns.RemoveAll(p => p is null || p.Dead || p.Destroyed);
            return base.CompGetGizmosExtra();
        }

        public override void TryAssignPawn(Pawn pawn)
        {
            if (assignedPawns.Contains(pawn)) return;
            while (assignedPawns.Count >= MaxAssignedPawnsCount)
            {
                var bumped = assignedPawns[0];
                assignedPawns.RemoveAt(0);
                GuardUtility.NotifyChanged(bumped);
            }
            base.TryAssignPawn(pawn);
            GuardUtility.NotifyChanged(pawn);
        }

        public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
        {
            var had = assignedPawns.Contains(pawn);
            base.TryUnassignPawn(pawn, sort, uninstall);
            if (had) GuardUtility.NotifyChanged(pawn);
        }
    }

    public class CompAssignableToPawn_Patrol : CompAssignableToPawn_Guard
    {
        private PatrolRoute Route => (parent as Building_PatrolWaypoint)?.Route;

        public override string GetAssignmentGizmoDesc() => "SS_AssignPatrolDesc".Translate();

        private void Sync()
        {
            assignedPawns.Clear();
            if (Route is { } route)
            {
                route.assignedPawns.RemoveAll(p => p is null || p.Dead || p.Destroyed);
                assignedPawns.AddRange(route.assignedPawns);
            }
            SortAssignedPawns();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Sync();
            return base.CompGetGizmosExtra();
        }

        public override void TryAssignPawn(Pawn pawn)
        {
            var route = Route;
            if (route is null || route.assignedPawns.Contains(pawn)) return;
            route.assignedPawns.Add(pawn);
            Sync();
            GuardUtility.NotifyChanged(pawn);
        }

        public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
        {
            var route = Route;
            if (route is null || route.assignedPawns.Remove(pawn) is false) return;
            Sync();
            GuardUtility.NotifyChanged(pawn);
        }

        public override void PostExposeData()
        {
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
        }

        public override void PostSwapMap()
        {
        }
    }
}
