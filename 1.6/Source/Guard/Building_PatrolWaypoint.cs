using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Building_PatrolWaypoint : Building, IPriorityTabTarget, IBehaviorTabTarget
    {
        private static readonly int[] DwellOptions = { 0, 300, 900, 1800, 3600 };

        public int routeId = -1;
        public int dwellTicks;
        public bool hideVisual;

        public MapComponent_Guarding Guarding => Map?.GetComponent<MapComponent_Guarding>();
        public PatrolRoute Route => Guarding?.GetRoute(routeId);

        public StoragePriority Priority
        {
            get => Route?.priority ?? StoragePriority.Normal;
            set
            {
                if (Route is { } route) route.priority = value;
            }
        }

        public GuardStance Stance
        {
            get => Route?.stance ?? GuardStance.Defend;
            set
            {
                if (Route is { } route) route.stance = value;
            }
        }

        public bool VisualHidden => hideVisual;

        public void SetVisualHidden(bool hidden)
        {
            hideVisual = hidden;
            if (Spawned) Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
        }
        public override Color DrawColor => Route?.Color ?? base.DrawColor;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad is false)
            {
                map.GetComponent<MapComponent_Guarding>().Register(this);
                NotifyRouteChanged();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            var guarding = Guarding;
            base.DeSpawn(mode);
            guarding?.Unregister(this);
        }

        public void NotifyRouteChanged()
        {
            graphicInt = null;
            if (Spawned) Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref routeId, "SS_routeId", -1);
            Scribe_Values.Look(ref dwellTicks, "SS_dwellTicks");
            Scribe_Values.Look(ref hideVisual, "SS_hideVisual");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            var route = Route;
            if (route is null) yield break;

            yield return GuardMenus.HideVisualGizmo(this);

            yield return new Command_Action
            {
                defaultLabel = "SS_WaypointRoute".Translate(route.RenamableLabel),
                defaultDesc = "SS_WaypointRouteDesc".Translate(),
                icon = GuardTextures.Route,
                defaultIconColor = route.Color,
                action = OpenRouteMenu
            };

            yield return new Command_Action
            {
                defaultLabel = "SS_RenameRoute".Translate(),
                defaultDesc = "SS_RenameRouteDesc".Translate(),
                icon = TexButton.Rename,
                action = () => Find.WindowStack.Add(new Dialog_RenameRoute(route))
            };

            yield return new Command_Action
            {
                defaultLabel = "SS_WaypointEarlier".Translate(),
                defaultDesc = "SS_WaypointEarlierDesc".Translate(),
                icon = TexButton.ReorderUp,
                action = () => route.MoveWaypoint(this, -1)
            };

            yield return new Command_Action
            {
                defaultLabel = "SS_WaypointLater".Translate(),
                defaultDesc = "SS_WaypointLaterDesc".Translate(),
                icon = TexButton.ReorderDown,
                action = () => route.MoveWaypoint(this, 1)
            };

            yield return new Command_Action
            {
                defaultLabel = "SS_WaypointDwell".Translate(dwellTicks.ToStringTicksToPeriod()),
                defaultDesc = "SS_WaypointDwellDesc".Translate(),
                icon = GuardTextures.Post,
                action = CycleDwell
            };

            yield return new Command_Action
            {
                defaultLabel = ("SS_RouteMode" + route.loopMode).Translate(),
                defaultDesc = "SS_RouteModeDesc".Translate(),
                icon = GuardTextures.Route,
                action = () => route.loopMode = route.loopMode == RouteLoopMode.Loop ? RouteLoopMode.PingPong : RouteLoopMode.Loop
            };

            yield return new Command_Action
            {
                defaultLabel = "SS_SelectRoute".Translate(),
                defaultDesc = "SS_SelectRouteDesc".Translate(),
                icon = GuardTextures.Route,
                action = () =>
                {
                    Find.Selector.ClearSelection();
                    foreach (var wp in route.waypoints.ToList())
                    {
                        Find.Selector.Select(wp, false);
                    }
                }
            };
        }

        private void CycleDwell()
        {
            var index = System.Array.IndexOf(DwellOptions, dwellTicks);
            dwellTicks = DwellOptions[(index + 1) % DwellOptions.Length];
        }

        private void OpenRouteMenu()
        {
            var guarding = Guarding;
            var options = new List<FloatMenuOption>();
            foreach (var route in guarding.routes.ToList())
            {
                var destination = route;
                options.Add(new FloatMenuOption(destination.RenamableLabel, () => MoveSelectedTo(guarding, destination)));
            }
            options.Add(new FloatMenuOption("SS_NewRoute".Translate(), () => MoveSelectedTo(guarding, guarding.CreateRoute())));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void MoveSelectedTo(MapComponent_Guarding guarding, PatrolRoute destination)
        {
            foreach (var wp in Find.Selector.SelectedObjects.OfType<Building_PatrolWaypoint>().ToList())
            {
                guarding.MoveToRoute(wp, destination);
            }
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            var route = Route;
            if (route is null) return text;
            var lines = new List<string>();
            if (text.NullOrEmpty() is false) lines.Add(text);
            lines.Add("SS_WaypointInspect".Translate(route.RenamableLabel, route.waypoints.IndexOf(this) + 1, route.waypoints.Count, dwellTicks.ToStringTicksToPeriod()).ToString());
            var names = route.assignedPawns.Where(p => p is { Dead: false }).Select(p => p.LabelShortCap).ToList();
            lines.Add(names.Count == 0 ? "SS_RouteUnmanned".Translate().ToString() : "SS_RouteMannedBy".Translate(names.ToCommaList()).ToString());
            lines.Add("Priority".Translate() + ": " + route.priority.Label().CapitalizeFirst());
            lines.Add("SS_GuardStance".Translate(GuardMenus.StanceLabel(route.stance)).ToString().Replace("\n", " "));
            return string.Join("\n", lines);
        }

        public override void Print(SectionLayer layer)
        {
            if (VisualHidden is false) base.Print(layer);
        }

        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            if (VisualHidden || Find.CameraDriver.CurrentZoom > CameraZoomRange.Middle) return;
            var route = Route;
            if (route is null) return;
            GenMapUI.DrawThingLabel(this, (route.waypoints.IndexOf(this) + 1).ToString(), route.Color);
        }
    }
}
