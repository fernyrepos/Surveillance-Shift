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

        public static IEnumerable<Building_GuardPost> AssignedPosts(Pawn pawn)
        {
            var guarding = pawn.Map?.GetComponent<MapComponent_Guarding>();
            if (guarding is null) return Enumerable.Empty<Building_GuardPost>();
            return guarding.Posts.Where(p => p.IsAssigned(pawn));
        }

        public static IEnumerable<PatrolRoute> AssignedRoutes(Pawn pawn)
        {
            var guarding = pawn.Map?.GetComponent<MapComponent_Guarding>();
            if (guarding is null) return Enumerable.Empty<PatrolRoute>();
            return guarding.routes.Where(r => r.waypoints.Count > 0 && r.assignedPawns.Contains(pawn));
        }

        public static bool ProtecteeValid(Pawn pawn, GuardAssignment assignment)
        {
            return assignment.protectee is { Spawned: true, Dead: false } protectee && protectee.Map == pawn.Map;
        }

        public static bool HasAnyDuty(Pawn pawn)
        {
            if (pawn.CanGuard() is false || pawn.Spawned is false) return false;
            return ProtecteeValid(pawn, pawn.GetGuardAssignment()) || AssignedPosts(pawn).Any() || AssignedRoutes(pawn).Any();
        }

        public static void NotifyChanged(Pawn pawn)
        {
            if (pawn is null) return;
            pawn.GetGuardAssignment().Clear();
            Refresh(pawn);
        }

        public static void AssignPost(Pawn pawn, Building_GuardPost post)
        {
            post.AssignComp?.TryAssignPawn(pawn);
        }

        public static void AssignRoute(Pawn pawn, PatrolRoute route)
        {
            if (route.assignedPawns.Contains(pawn)) return;
            route.assignedPawns.Add(pawn);
            NotifyChanged(pawn);
        }

        public static void SetProtectee(Pawn pawn, Pawn protectee)
        {
            pawn.GetGuardAssignment().protectee = protectee;
            NotifyChanged(pawn);
        }

        private static void Refresh(Pawn pawn)
        {
            if (pawn.Spawned is false || pawn.jobs is null) return;
            if (pawn.CurJob != null && IsGuardJob(pawn.CurJobDef)) pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            else pawn.jobs.CheckForJobOverride();
        }

        public static bool IsGuardJob(JobDef def)
        {
            return def == DefsOf.SS_GuardPostDuty || def == DefsOf.SS_PatrolDuty || def == DefsOf.SS_BodyguardDuty || def == DefsOf.SS_GuardEngage || def == DefsOf.SS_ManWatchtower;
        }

        private static bool CurrentDutyValid(Pawn pawn, GuardAssignment assignment)
        {
            switch (assignment.mode)
            {
                case GuardDutyMode.Post:
                    return assignment.post is { Spawned: true } post && post.Map == pawn.Map && post.IsAssigned(pawn);
                case GuardDutyMode.Route:
                    return assignment.route is { waypoints: { Count: > 0 } } route && route.assignedPawns.Contains(pawn) && pawn.Map.GetComponent<MapComponent_Guarding>().routes.Contains(route);
                case GuardDutyMode.Bodyguard:
                    return ProtecteeValid(pawn, assignment);
                default:
                    return false;
            }
        }

        private static bool PostUsable(Pawn pawn, Building_GuardPost post)
        {
            if (post.Spawned is false || post.Map != pawn.Map || post.Faction != Faction.OfPlayer) return false;
            if (post is Building_Watchtower)
            {
                if (pawn.RaceProps.Humanlike is false || pawn == ModCompatibility.GetPerspectiveShiftAvatar()) return false;
                return pawn.CanReserveAndReach(post, PathEndMode.InteractionCell, Danger.Some, post.GuardSlots, 0);
            }
            return pawn.CanReserveAndReach(post, PathEndMode.OnCell, Danger.Some, post.GuardSlots, 0);
        }

        private static bool RouteUsable(Pawn pawn, PatrolRoute route)
        {
            var target = route.Nearest(pawn.Position);
            return target != null && pawn.CanReach(target, PathEndMode.OnCell, Danger.Some);
        }

        private const int BodyguardPriority = (int)StoragePriority.Critical;

        public static bool SelectDuty(Pawn pawn, GuardAssignment assignment)
        {
            if (pawn.CanGuard() is false || pawn.Spawned is false || assignment.keepWatch is false)
            {
                assignment.Clear();
                return false;
            }
            if (assignment.forced && CurrentDutyValid(pawn, assignment)) return true;

            var previousMode = assignment.mode;
            var previousPost = assignment.post;
            var previousRoute = assignment.route;
            var previousWaypoint = assignment.lastWaypoint;
            var previousDirection = assignment.direction;
            assignment.Clear();

            var bestScore = (priority: -1, current: false, distance: float.MaxValue);
            Building_GuardPost bestPost = null;
            PatrolRoute bestRoute = null;
            var bestBodyguard = false;

            bool Better(int priority, bool current, float distance)
            {
                if (priority != bestScore.priority) return priority > bestScore.priority;
                if (current != bestScore.current) return current;
                return distance < bestScore.distance;
            }

            if (ProtecteeValid(pawn, assignment))
            {
                bestScore = (BodyguardPriority, previousMode == GuardDutyMode.Bodyguard, 0f);
                bestBodyguard = true;
            }

            foreach (var post in AssignedPosts(pawn))
            {
                var priority = (int)post.Priority;
                var current = previousMode == GuardDutyMode.Post && previousPost == post;
                var distance = post.Position.DistanceToSquared(pawn.Position);
                if (Better(priority, current, distance) is false || PostUsable(pawn, post) is false) continue;
                bestScore = (priority, current, distance);
                bestPost = post;
                bestRoute = null;
                bestBodyguard = false;
            }

            foreach (var route in AssignedRoutes(pawn))
            {
                var priority = (int)route.priority;
                var current = previousMode == GuardDutyMode.Route && previousRoute == route;
                var distance = (route.Nearest(pawn.Position)?.Position ?? pawn.Position).DistanceToSquared(pawn.Position);
                if (Better(priority, current, distance) is false || RouteUsable(pawn, route) is false) continue;
                bestScore = (priority, current, distance);
                bestPost = null;
                bestRoute = route;
                bestBodyguard = false;
            }

            if (bestBodyguard)
            {
                assignment.mode = GuardDutyMode.Bodyguard;
                return true;
            }
            if (bestPost != null)
            {
                assignment.mode = GuardDutyMode.Post;
                assignment.post = bestPost;
                return true;
            }
            if (bestRoute != null)
            {
                assignment.mode = GuardDutyMode.Route;
                assignment.route = bestRoute;
                if (bestRoute == previousRoute)
                {
                    assignment.lastWaypoint = previousWaypoint;
                    assignment.direction = previousDirection;
                }
                return true;
            }
            return false;
        }

        public static bool DutyIsValid(Pawn pawn, out GuardAssignment assignment)
        {
            assignment = pawn.GetGuardAssignment();
            if (pawn.CanGuard() is false || pawn.Spawned is false || assignment.keepWatch is false) return false;
            if (CurrentDutyValid(pawn, assignment)) return true;
            return SelectDuty(pawn, assignment);
        }

        public static bool JobStillValid(Pawn pawn, Job job)
        {
            if (DutyIsValid(pawn, out var assignment) is false) return false;
            var def = job.def;
            if (def == DefsOf.SS_GuardPostDuty || def == DefsOf.SS_ManWatchtower) return assignment.mode == GuardDutyMode.Post && assignment.post == job.targetA.Thing;
            if (def == DefsOf.SS_PatrolDuty) return assignment.mode == GuardDutyMode.Route && job.targetA.Thing is Building_PatrolWaypoint waypoint && waypoint.Route == assignment.route;
            if (def == DefsOf.SS_BodyguardDuty) return assignment.mode == GuardDutyMode.Bodyguard;
            return true;
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

        public static Job MakeDutyJob(Pawn pawn)
        {
            var assignment = pawn.GetGuardAssignment();
            if (pawn.CanGuard() is false || pawn.Spawned is false || SelectDuty(pawn, assignment) is false) return null;
            return MakeJobForCurrentDuty(pawn, assignment);
        }

        public static Job MakeJobForCurrentDuty(Pawn pawn, GuardAssignment assignment)
        {
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

        public static void ForceDuty(Pawn pawn, Building_GuardPost post)
        {
            var assignment = pawn.GetGuardAssignment();
            assignment.keepWatch = true;
            assignment.Clear();
            assignment.mode = GuardDutyMode.Post;
            assignment.post = post;
            assignment.forced = true;
        }

        public static void ForceDuty(Pawn pawn, PatrolRoute route)
        {
            var assignment = pawn.GetGuardAssignment();
            assignment.keepWatch = true;
            assignment.Clear();
            assignment.mode = GuardDutyMode.Route;
            assignment.route = route;
            assignment.forced = true;
        }

        public static IntVec3 PostSlotCell(Pawn pawn, Building_GuardPost post)
        {
            var index = post.AssignedGuards.OrderBy(p => p.thingIDNumber).ToList().IndexOf(pawn);
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
            if (post is Building_Watchtower tower)
            {
                if (PostUsable(pawn, tower) is false) return null;
                return JobMaker.MakeJob(DefsOf.SS_ManWatchtower, tower);
            }
            var slot = PostSlotCell(pawn, post);
            if (pawn.CanReserveAndReach(post, PathEndMode.OnCell, Danger.Some, post.GuardSlots, 0) is false || pawn.CanReach(slot, PathEndMode.OnCell, Danger.Some) is false) return null;
            var job = JobMaker.MakeJob(DefsOf.SS_GuardPostDuty, post, post.Position + post.Rotation.FacingCell, slot);
            job.expiryInterval = 2500;
            job.checkOverrideOnExpire = true;
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }

        private static Job MakePatrolJob(Pawn pawn, GuardAssignment assignment)
        {
            var route = assignment.route;
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
