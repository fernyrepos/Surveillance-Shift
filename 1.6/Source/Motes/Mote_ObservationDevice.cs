using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HotSwappable]
    public class Mote_ObservationDevice : MoteAttached
    {
        private const float ForwardOffset = 0.5f;

        public override void TimeInterval(float deltaTime)
        {
            base.TimeInterval(deltaTime);
            Follow();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Follow();
            base.DrawAt(exactPosition, flip);
        }

        private void Follow()
        {
            if (link1.Linked && link1.Target.Thing is Pawn p)
            {
                Rotation = p.Rotation;
                exactRotation = p.Rotation.AsAngle;
                var pos = p.DrawPos + p.Rotation.FacingCell.ToVector3() * ForwardOffset;
                yOffset = p.Rotation == Rot4.North ? -1.25f : -0.5f;
                pos.z += 0.35f;
                exactPosition = pos;
            }
        }
    }
}
