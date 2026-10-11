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

    public class CompAssignableToPawn_GuardPost : CompAssignableToPawn
    {
        private Building_GuardPost Post => parent as Building_GuardPost;

        public override IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                return parent.Spawned ? GuardUtility.EligibleGuards(parent.Map) : Enumerable.Empty<Pawn>();
            }
        }

        public void SyncFromOccupants()
        {
            assignedPawns.Clear();
            if (Post is { Spawned: true } post) assignedPawns.AddRange(post.Occupants);
            SortAssignedPawns();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            SyncFromOccupants();
            return base.CompGetGizmosExtra();
        }

        public override string GetAssignmentGizmoLabel() => "SS_AssignGuardToPost".Translate();

        public override string GetAssignmentGizmoDesc() => "SS_AssignGuardToPostDesc".Translate(MaxAssignedPawnsCount);

        public override bool AssignedAnything(Pawn pawn) => pawn.GetGuardAssignment().IsActive;

        public override void TryAssignPawn(Pawn pawn)
        {
            if (Post is null || assignedPawns.Contains(pawn)) return;
            GuardUtility.AssignPost(pawn, Post);
        }

        public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
        {
            var assignment = pawn.GetGuardAssignment();
            if (assignment.mode == GuardDutyMode.Post && assignment.post == Post) GuardUtility.Unassign(pawn);
            else base.TryUnassignPawn(pawn, sort, uninstall);
        }

        public override void PostExposeData()
        {
        }
    }
}
