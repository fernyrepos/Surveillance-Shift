using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public static class GuardUtility
    {
        private static readonly ConditionalWeakTable<Pawn, GuardAssignment> assignments = new();

        public static GuardAssignment GetGuardAssignment(this Pawn pawn)
        {
            return assignments.GetValue(pawn, _ => new GuardAssignment());
        }

        public static void ReplaceAssignment(Pawn pawn, GuardAssignment assignment)
        {
            assignments.Remove(pawn);
            assignments.Add(pawn, assignment);
        }

        public static bool CanGuard(this Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer || pawn.Dead || pawn.Destroyed) return false;
            if (pawn.RaceProps.Animal) return pawn.training != null && pawn.training.HasLearned(DefsOf.SS_KeepWatch);
            if (pawn.RaceProps.IsMechanoid) return pawn.IsColonyMech && pawn.kindDef.isFighter;
            if (pawn.RaceProps.Humanlike) return (pawn.IsFreeNonSlaveColonist || pawn.IsSlaveOfColony) && pawn.DevelopmentalStage.Baby() is false;
            return false;
        }

        public static IEnumerable<Pawn> EligibleGuards(Map map)
        {
            return map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.Spawned && p.CanGuard());
        }

        public static float Leash(GuardStance stance)
        {
            return stance switch
            {
                GuardStance.Hold => 9f,
                GuardStance.Pursue => 45f,
                _ => 22f
            };
        }

        public static string DutyLabel(Pawn pawn)
        {
            var assignment = pawn.GetGuardAssignment();
            switch (assignment.mode)
            {
                case GuardDutyMode.Post:
                    return assignment.post?.RenamableLabel ?? "SS_DutyNone".Translate().ToString();
                case GuardDutyMode.Route:
                    var route = pawn.MapHeld?.GetComponent<MapComponent_Guarding>()?.GetRoute(assignment.routeId);
                    return route is null ? "SS_DutyNone".Translate().ToString() : "SS_DutyRoute".Translate(route.RenamableLabel).ToString();
                case GuardDutyMode.Bodyguard:
                    return assignment.protectee is null ? "SS_DutyNone".Translate().ToString() : "SS_DutyBodyguard".Translate(assignment.protectee.LabelShort).ToString();
                default:
                    return "SS_DutyNone".Translate().ToString();
            }
        }

        private static void ReleasePost(Pawn pawn)
        {
            var assignment = pawn.GetGuardAssignment();
            if (assignment.mode == GuardDutyMode.Post) assignment.post?.GetComp<CompAssignableToPawn_GuardPost>()?.ForceRemovePawn(pawn);
        }

        public static void Unassign(Pawn pawn)
        {
            var assignment = pawn.GetGuardAssignment();
            if (assignment.IsActive is false) return;
            ReleasePost(pawn);
            assignment.Clear();
            Refresh(pawn);
        }

        public static void AssignPost(Pawn pawn, Building_GuardPost post)
        {
            var comp = post.GetComp<CompAssignableToPawn_GuardPost>();
            if (comp != null)
            {
                comp.SyncFromOccupants();
                while (comp.AssignedPawnsForReading.Count(p => p != pawn) >= comp.MaxAssignedPawnsCount)
                {
                    Unassign(comp.AssignedPawnsForReading.First(p => p != pawn));
                }
            }
            ReleasePost(pawn);
            var assignment = pawn.GetGuardAssignment();
            assignment.Clear();
            assignment.mode = GuardDutyMode.Post;
            assignment.post = post;
            comp?.ForceAddPawn(pawn);
            Refresh(pawn);
        }

        public static void AssignRoute(Pawn pawn, PatrolRoute route)
        {
            ReleasePost(pawn);
            var assignment = pawn.GetGuardAssignment();
            assignment.Clear();
            assignment.mode = GuardDutyMode.Route;
            assignment.routeId = route.id;
            Refresh(pawn);
        }

        public static void AssignBodyguard(Pawn pawn, Pawn protectee)
        {
            ReleasePost(pawn);
            var assignment = pawn.GetGuardAssignment();
            assignment.Clear();
            assignment.mode = GuardDutyMode.Bodyguard;
            assignment.protectee = protectee;
            Refresh(pawn);
        }

        public static void SetStance(Pawn pawn, GuardStance stance)
        {
            pawn.GetGuardAssignment().stance = stance;
        }

        private static void Refresh(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.jobs is null) return;
            if (pawn.CurJob != null && IsGuardJob(pawn.CurJobDef)) pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            else pawn.jobs.CheckForJobOverride();
        }

        public static bool IsGuardJob(JobDef def)
        {
            return def == DefsOf.SS_GuardPostDuty || def == DefsOf.SS_PatrolDuty || def == DefsOf.SS_BodyguardDuty || def == DefsOf.SS_GuardEngage;
        }

        public static bool DutyIsValid(Pawn pawn, out GuardAssignment assignment)
        {
            assignment = pawn.GetGuardAssignment();
            if (pawn.CanGuard() is false || pawn.Spawned is false || assignment.keepWatch is false) return false;
            switch (assignment.mode)
            {
                case GuardDutyMode.Post:
                    return assignment.post is { Spawned: true } post && post.Map == pawn.Map;
                case GuardDutyMode.Route:
                    return pawn.Map.GetComponent<MapComponent_Guarding>().GetRoute(assignment.routeId) is { waypoints: { Count: > 0 } };
                case GuardDutyMode.Bodyguard:
                    return assignment.protectee is { Spawned: true, Dead: false } protectee && protectee.Map == pawn.Map;
                default:
                    return false;
            }
        }

        public static bool JobStillValid(Pawn pawn, JobDef def)
        {
            if (DutyIsValid(pawn, out var assignment) is false) return false;
            return def switch
            {
                _ when def == DefsOf.SS_GuardPostDuty => assignment.mode == GuardDutyMode.Post,
                _ when def == DefsOf.SS_PatrolDuty => assignment.mode == GuardDutyMode.Route,
                _ when def == DefsOf.SS_BodyguardDuty => assignment.mode == GuardDutyMode.Bodyguard,
                _ => true
            };
        }

        public static IntVec3 Anchor(Pawn pawn, GuardAssignment assignment)
        {
            switch (assignment.mode)
            {
                case GuardDutyMode.Post when assignment.post is { Spawned: true } post:
                    return post.Position;
                case GuardDutyMode.Route when assignment.lastWaypoint is { Spawned: true } wp:
                    return wp.Position;
                case GuardDutyMode.Bodyguard when assignment.protectee is { Spawned: true } protectee:
                    return protectee.Position;
                default:
                    return pawn.Position;
            }
        }

        public static Job TryReportFirst(Pawn pawn)
        {
            if (pawn.RaceProps.Animal is false || pawn.Faction != Faction.OfPlayer) return null;
            var data = pawn.GetSurveillanceData();
            if (data.alertResponse == AnimalAlertResponse.None || Find.TickManager.TicksGame - data.lastThreatReportTick <= GenDate.TicksPerHour) return null;
            return State.TryGetAnimalReactionJob(pawn, data, Leash(pawn.GetGuardAssignment().stance));
        }

        public static bool NeedsAttention(Pawn pawn)
        {
            var needs = pawn.needs;
            if (needs is null) return false;
            if (needs.food != null && (int)needs.food.CurCategory >= (int)HungerCategory.UrgentlyHungry) return true;
            if (needs.rest != null && (int)needs.rest.CurCategory >= (int)RestCategory.VeryTired) return true;
            if (needs.energy != null && needs.energy.CurLevelPercentage < 0.2f) return true;
            if (pawn.RaceProps.Humanlike && pawn.timetable?.CurrentAssignment == TimeAssignmentDefOf.Sleep) return true;
            return HealthAIUtility.ShouldSeekMedicalRest(pawn);
        }

        public static Job MakeDutyJob(Pawn pawn, GuardAssignment assignment)
        {
            if (DutyIsValid(pawn, out assignment) is false) return null;
            switch (assignment.mode)
            {
                case GuardDutyMode.Post:
                    return MakePostJob(pawn, assignment);
                case GuardDutyMode.Route:
                    return MakePatrolJob(pawn, assignment);
                case GuardDutyMode.Bodyguard:
                    return MakeBodyguardJob(pawn, assignment);
                default:
                    return null;
            }
        }

        public static IntVec3 PostSlotCell(Pawn pawn, Building_GuardPost post)
        {
            var index = post.Occupants.OrderBy(p => p.thingIDNumber).ToList().IndexOf(pawn);
            if (index <= 0) return post.Position;
            var map = post.Map;
            var seen = 0;
            foreach (var cell in GenRadial.RadialCellsAround(post.Position, 3f, false))
            {
                if (cell.InBounds(map) is false || cell.Standable(map) is false || cell.GetEdifice(map) != null) continue;
                if (++seen == index) return cell;
            }
            return post.Position;
        }

        private static Job MakePostJob(Pawn pawn, GuardAssignment assignment)
        {
            var post = assignment.post;
            var slot = PostSlotCell(pawn, post);
            if (pawn.CanReserveAndReach(post, PathEndMode.OnCell, Danger.Some) is false || pawn.CanReach(slot, PathEndMode.OnCell, Danger.Some) is false) return null;
            var job = JobMaker.MakeJob(DefsOf.SS_GuardPostDuty, post, post.Position + post.Rotation.FacingCell, slot);
            job.expiryInterval = 2500;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }

        private static Job MakePatrolJob(Pawn pawn, GuardAssignment assignment)
        {
            var route = pawn.Map.GetComponent<MapComponent_Guarding>().GetRoute(assignment.routeId);
            var current = assignment.lastWaypoint is { Spawned: true } last && route.waypoints.Contains(last) ? last : null;
            var target = current is null ? route.Nearest(pawn.Position) : route.Next(current, ref assignment.direction);
            if (target is null || pawn.CanReach(target, PathEndMode.OnCell, Danger.Some) is false) return null;
            var job = JobMaker.MakeJob(DefsOf.SS_PatrolDuty, target);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            return job;
        }

        private static Job MakeBodyguardJob(Pawn pawn, GuardAssignment assignment)
        {
            var protectee = assignment.protectee;
            if (pawn.CanReach(protectee, PathEndMode.Touch, Danger.Some) is false) return null;
            var job = JobMaker.MakeJob(DefsOf.SS_BodyguardDuty, protectee);
            job.expiryInterval = 1200;
            job.checkOverrideOnExpire = true;
            return job;
        }
    }
}
