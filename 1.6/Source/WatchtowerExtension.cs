using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class WatchtowerExtension : DefModExtension
    {
        public GraphicData topGraphicData;
        public ThingDef moteDef;
        public float climbHeight = 7f;
        public int climbTicks = 100;
        public List<Vector2> vantageOffsets;
    }
}
