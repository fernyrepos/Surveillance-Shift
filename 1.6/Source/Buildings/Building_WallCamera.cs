using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Building_WallCamera : Building_Camera
    {
        public float HalfConeAngle => ViewConeAngle / 2f;

        public override int SightRadius => Extension?.sightRadius ?? 26;

        protected override float ViewConeAngle => Extension?.coneAngle ?? 60f;

        public override void ComputeCameraFOV(bool[] visibleCells)
        {
            if (IsActive is false || IsMonitored is false) return;
            ComputeCone(Map, Position, Rotation, SightRadius, ViewConeAngle, visibleCells);
        }

        public static void ComputeCone(Map map, IntVec3 position, Rot4 rotation, int radius, float coneAngle, bool[] visibleCells)
        {
            var rot = rotation;
            var forwardCell = position + rot.FacingCell;
            var backwardCell = position + rot.Opposite.FacingCell;

            var forwardEdifice = forwardCell.InBounds(map) ? map.edificeGrid[map.cellIndices.CellToIndex(forwardCell)] : null;
            var backwardEdifice = backwardCell.InBounds(map) ? map.edificeGrid[map.cellIndices.CellToIndex(backwardCell)] : null;
            var posEdifice = map.edificeGrid[map.cellIndices.CellToIndex(position)];

            if (forwardEdifice != null && forwardEdifice.CanBeSeenOver() is false && (backwardEdifice == null || backwardEdifice.CanBeSeenOver()))
            {
                rot = rot.Opposite;
            }

            var viewOrigin = position;
            if (posEdifice != null && posEdifice.CanBeSeenOver() is false)
            {
                viewOrigin = position + rot.FacingCell;
            }

            if (viewOrigin.InBounds(map) is false) return;

            var mapSizeX = map.Size.x;
            visibleCells[position.z * mapSizeX + position.x] = true;
            visibleCells[viewOrigin.z * mapSizeX + viewOrigin.x] = true;

            var maxSlope = Mathf.Tan(coneAngle / 2f * Mathf.Deg2Rad);
            ShadowCaster.ComputeConeFieldOfView(viewOrigin, radius, rot, maxSlope, map, visibleCells);
        }
    }
}
