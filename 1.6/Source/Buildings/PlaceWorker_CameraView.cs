using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class PlaceWorker_CameraView : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var extension = def.GetModExtension<CameraExtension>();
            var wall = typeof(Building_WallCamera).IsAssignableFrom(def.thingClass);
            var radius = extension?.sightRadius ?? 24;
            var cone = wall ? extension?.coneAngle ?? 60f : 0f;
            CameraViewPreview.Draw(Find.CurrentMap, center, rot, radius, cone, wall);
        }
    }
}
