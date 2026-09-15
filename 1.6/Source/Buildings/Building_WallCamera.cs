using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class Building_WallCamera : Building_Camera
    {
        public float HalfConeAngle => (Extension?.coneAngle ?? 60f) / 2f;

        public override int SightRadius => Extension?.sightRadius ?? 26;

        public override void ComputeCameraFOV(bool[] visibleCells)
        {
            if (IsActive is false || IsMonitored is false) return;

            var rot = Rotation;
            var forwardCell = Position + rot.FacingCell;
            var backwardCell = Position + rot.Opposite.FacingCell;

            var forwardEdifice = forwardCell.InBounds(Map) ? Map.edificeGrid[Map.cellIndices.CellToIndex(forwardCell)] : null;
            var backwardEdifice = backwardCell.InBounds(Map) ? Map.edificeGrid[Map.cellIndices.CellToIndex(backwardCell)] : null;
            var posEdifice = Map.edificeGrid[Map.cellIndices.CellToIndex(Position)];

            if (forwardEdifice != null && forwardEdifice.CanBeSeenOver() is false && (backwardEdifice == null || backwardEdifice.CanBeSeenOver()))
            {
                rot = rot.Opposite;
            }

            var viewOrigin = Position;
            if (posEdifice != null && posEdifice.CanBeSeenOver() is false)
            {
                viewOrigin = Position + rot.FacingCell;
            }

            if (viewOrigin.InBounds(Map) is false) return;

            var mapSizeX = Map.Size.x;
            visibleCells[Position.z * mapSizeX + Position.x] = true;
            visibleCells[viewOrigin.z * mapSizeX + viewOrigin.x] = true;

            var maxSlope = Mathf.Tan(HalfConeAngle * Mathf.Deg2Rad);
            ShadowCaster.ComputeConeFieldOfView(viewOrigin, SightRadius, rot, maxSlope, Map, visibleCells);
        }
    }
}
