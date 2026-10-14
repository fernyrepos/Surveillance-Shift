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
                Vector3 offset;
                if (p.Rotation == Rot4.North)
                {
                    offset = new Vector3(0f, 0f, 0.65f);
                    yOffset = -5f;
                }
                else if (p.Rotation == Rot4.South)
                {
                    offset = new Vector3(0f, 0f, -0.22f);
                    yOffset = 0f;
                }
                else if (p.Rotation == Rot4.East)
                {
                    offset = new Vector3(0.4f, 0f, -0.05f);
                    yOffset = 0f;
                }
                else
                {
                    offset = new Vector3(-0.4f, 0f, -0.05f);
                    yOffset = 0f;
                }
                exactPosition = p.DrawPos + offset;
            }
        }
    }
}
