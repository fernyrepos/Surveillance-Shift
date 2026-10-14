using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class MapComponent_Guarding : MapComponent
    {
        private static readonly Dictionary<Color, Material> lineMaterials = new();

        public List<PatrolRoute> routes = new();
        public int activeRouteId = -1;
        private int nextRouteId = 1;
        private Dictionary<IntVec3, string> pendingPostNames = new();
        private List<IntVec3> pendingKeysScratch;
        private List<string> pendingValuesScratch;

        public MapComponent_Guarding(Map map) : base(map)
        {
        }

        public void SetPendingPostName(IntVec3 cell, string name)
        {
            if (name.NullOrEmpty()) pendingPostNames.Remove(cell);
            else pendingPostNames[cell] = name;
        }

        public string PendingPostName(IntVec3 cell) => pendingPostNames.TryGetValue(cell);

        public string TakePendingPostName(IntVec3 cell)
        {
            if (pendingPostNames.Count == 0 || pendingPostNames.Remove(cell, out var name) is false) return null;
            return name;
        }

        public PatrolRoute GetRoute(int id)
        {
            if (id < 0) return null;
            for (var i = 0; i < routes.Count; i++)
            {
                if (routes[i].id == id) return routes[i];
            }
            return null;
        }

        public PatrolRoute CreateRoute(string name = null)
        {
            var route = new PatrolRoute { id = nextRouteId++, name = name };
            routes.Add(route);
            activeRouteId = route.id;
            return route;
        }

        public PatrolRoute ActiveRoute => GetRoute(activeRouteId) ?? routes.FirstOrDefault() ?? CreateRoute();

        public void Register(Building_PatrolWaypoint wp)
        {
            var route = GetRoute(wp.routeId);
            if (route is null)
            {
                route = ActiveRoute;
                wp.routeId = route.id;
            }
            if (route.waypoints.Contains(wp) is false) route.waypoints.Add(wp);
            activeRouteId = route.id;
        }

        public void Unregister(Building_PatrolWaypoint wp)
        {
            var route = GetRoute(wp.routeId);
            if (route is null) return;
            route.waypoints.Remove(wp);
            if (route.waypoints.Count == 0 && route.name.NullOrEmpty())
            {
                routes.Remove(route);
                if (activeRouteId == route.id) activeRouteId = -1;
            }
        }

        public void MoveToRoute(Building_PatrolWaypoint wp, PatrolRoute destination)
        {
            if (wp.routeId == destination.id) return;
            Unregister(wp);
            wp.routeId = destination.id;
            destination.waypoints.Add(wp);
            activeRouteId = destination.id;
            wp.NotifyRouteChanged();
        }

        private readonly List<Building_GuardPost> posts = new();

        public List<Building_GuardPost> Posts => posts;

        public void Register(Building_GuardPost post)
        {
            if (posts.Contains(post) is false) posts.Add(post);
        }

        public void Unregister(Building_GuardPost post) => posts.Remove(post);

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref routes, "routes", LookMode.Deep);
            Scribe_Values.Look(ref nextRouteId, "nextRouteId", 1);
            Scribe_Values.Look(ref activeRouteId, "activeRouteId", -1);
            Scribe_Collections.Look(ref pendingPostNames, "pendingPostNames", LookMode.Value, LookMode.Value, ref pendingKeysScratch, ref pendingValuesScratch);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                routes ??= new List<PatrolRoute>();
                pendingPostNames ??= new Dictionary<IntVec3, string>();
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                if (pawn.Faction != Faction.OfPlayer) continue;
                var assignment = pawn.GetGuardAssignment();
                if (assignment.HasLegacy is false) continue;
                if (assignment.legacyMode == GuardDutyMode.Post && assignment.legacyPost is { } post) post.AssignComp?.ForceAddPawn(pawn);
                else if (assignment.legacyMode == GuardDutyMode.Route && GetRoute(assignment.legacyRouteId) is { } route && route.assignedPawns.Contains(pawn) is false) route.assignedPawns.Add(pawn);
                assignment.ClearLegacy();
            }
        }

        public override void MapComponentUpdate()
        {
            if (routes.Count == 0 || Find.CurrentMap != map) return;
            var overlayWanted = OverlayWanted(out var focus);
            if (overlayWanted is false) return;
            foreach (var route in routes)
            {
                if (focus != null && focus.Contains(route.id) is false) continue;
                if (route.waypoints.All(w => w.hideVisual)) continue;
                DrawRoute(route);
            }
        }

        private bool OverlayWanted(out HashSet<int> focus)
        {
            focus = null;
            if (Find.DesignatorManager.SelectedDesignator is Designator_Build build && build.PlacingDef == DefsOf.SS_PatrolWaypoint) return true;

            var selected = Find.Selector.SelectedObjects;
            if (selected.Count == 0) return false;
            foreach (var obj in selected)
            {
                if (obj is Building_PatrolWaypoint wp)
                {
                    focus ??= new HashSet<int>();
                    focus.Add(wp.routeId);
                }
                else if (obj is Pawn pawn && pawn.Map == map)
                {
                    foreach (var assigned in GuardUtility.AssignedRoutes(pawn))
                    {
                        focus ??= new HashSet<int>();
                        focus.Add(assigned.id);
                    }
                }
            }
            return focus != null;
        }

        private void DrawRoute(PatrolRoute route)
        {
            var wps = route.waypoints;
            if (wps.Count < 2) return;
            var material = MaterialFor(route.Color);
            for (var i = 0; i < wps.Count - 1; i++)
            {
                DrawLink(wps[i], wps[i + 1], material);
            }
            if (route.loopMode == RouteLoopMode.Loop && wps.Count > 2) DrawLink(wps[wps.Count - 1], wps[0], material);
        }

        private static void DrawLink(Building_PatrolWaypoint a, Building_PatrolWaypoint b, Material material)
        {
            if (a.Spawned is false || b.Spawned is false) return;
            var from = a.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
            var to = b.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
            GenDraw.DrawLineBetween(from, to, material, 0.18f);
        }

        private static Material MaterialFor(Color color)
        {
            if (lineMaterials.TryGetValue(color, out var material) is false)
            {
                material = SolidColorMaterials.SimpleSolidColorMaterial(new Color(color.r, color.g, color.b, 0.6f));
                lineMaterials[color] = material;
            }
            return material;
        }
    }
}
