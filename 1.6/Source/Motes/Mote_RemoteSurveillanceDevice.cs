using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HotSwappable]
    public class Mote_RemoteSurveillanceDevice : MoteAttached
    {
        public override void TimeInterval(float deltaTime)
        {
            base.TimeInterval(deltaTime);
            if (link1.Linked && link1.Target.Thing is Pawn p)
            {
                Rotation = p.Rotation;
                exactRotation = p.Rotation.AsAngle;
                var pos = p.DrawPos + p.Rotation.FacingCell.ToVector3() * 0.5f;
                yOffset = p.Rotation == Rot4.North ? -5f : 0;
                if (p.Rotation == Rot4.North)
                {
                    pos.z += 0.20f;
                }
                else
                {
                    pos.z -= 0.20f;
                }
                exactPosition = pos;
            }
        }
    }
}
