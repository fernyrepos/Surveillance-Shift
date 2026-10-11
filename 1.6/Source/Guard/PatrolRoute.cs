using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class PatrolRoute : IExposable, IRenameable
    {
        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.35f, 0.30f),
            new Color(0.30f, 0.65f, 0.95f),
            new Color(0.40f, 0.85f, 0.45f),
            new Color(0.98f, 0.80f, 0.25f),
            new Color(0.75f, 0.45f, 0.95f),
            new Color(0.98f, 0.60f, 0.25f),
            new Color(0.35f, 0.90f, 0.85f),
            new Color(0.95f, 0.55f, 0.80f)
        };

        public int id;
        public string name;
        public RouteLoopMode loopMode;
        public List<Building_PatrolWaypoint> waypoints = new();

        public Color Color => Palette[(id - 1 + Palette.Length * 4) % Palette.Length];

        public string RenamableLabel
        {
            get => name.NullOrEmpty() ? BaseLabel : name;
            set => name = value;
        }

        public string BaseLabel => "SS_RouteDefaultName".Translate(id);
        public string InspectLabel => RenamableLabel;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref loopMode, "loopMode", RouteLoopMode.Loop);
            Scribe_Collections.Look(ref waypoints, "waypoints", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                waypoints ??= new List<Building_PatrolWaypoint>();
                waypoints.RemoveAll(w => w is null);
            }
        }

        public Building_PatrolWaypoint Nearest(IntVec3 from)
        {
            Building_PatrolWaypoint best = null;
            var bestDist = int.MaxValue;
            foreach (var wp in waypoints)
            {
                if (wp.Spawned is false) continue;
                var dist = wp.Position.DistanceToSquared(from);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = wp;
                }
            }
            return best;
        }

        public Building_PatrolWaypoint Next(Building_PatrolWaypoint current, ref int direction)
        {
            if (waypoints.Count == 0) return null;
            var index = current is null ? -1 : waypoints.IndexOf(current);
            if (index < 0) return null;
            if (waypoints.Count == 1) return waypoints[0];

            if (loopMode == RouteLoopMode.Loop)
            {
                direction = 1;
                return waypoints[(index + 1) % waypoints.Count];
            }

            if (direction == 0) direction = 1;
            var next = index + direction;
            if (next < 0 || next >= waypoints.Count)
            {
                direction = -direction;
                next = index + direction;
            }
            return waypoints[Mathf.Clamp(next, 0, waypoints.Count - 1)];
        }

        public void MoveWaypoint(Building_PatrolWaypoint wp, int delta)
        {
            var index = waypoints.IndexOf(wp);
            if (index < 0) return;
            var target = Mathf.Clamp(index + delta, 0, waypoints.Count - 1);
            if (target == index) return;
            waypoints.RemoveAt(index);
            waypoints.Insert(target, wp);
        }
    }
}
