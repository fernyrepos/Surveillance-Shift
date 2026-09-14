using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public class Mote_Ping : Mote
    {
        private const float DetectionRadiusSq = 25f;
        private const float TargetMaxScale = 5f;
        private const float GrowthRate = 3f;
        private const int TextureSize = 256;
        private const float RingRadius = 0.40f;
        private const float RingFalloff = 0.055f;

        private float currentScale = 0.5f;

        static Mote_Ping()
        {
            var ring = GenerateRingTexture();
            ApplyTexture(DefsOf.SS_Mote_PingHostile, ring);
            ApplyTexture(DefsOf.SS_Mote_PingFriendly, ring);
        }

        private static void ApplyTexture(ThingDef def, Texture2D texture)
        {
            if (def.graphicData.Graphic is Graphic_Single graphic)
            {
                graphic.MatSingle.mainTexture = texture;
                graphic.MatSingle.renderQueue = 3950;
            }
        }

        private static Texture2D GenerateRingTexture()
        {
            var pixels = new Color[TextureSize * TextureSize];
            var center = (TextureSize - 1) / 2f;
            for (int x = 0; x < TextureSize; x++)
            {
                for (int y = 0; y < TextureSize; y++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var d = (dist - RingRadius) / RingFalloff;
                    var alpha = Mathf.Exp(-d * d);
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, true);
            texture.name = "SS_PingRing";
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true);
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        public override bool EndOfLife => false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            solidTimeOverride = float.MaxValue;
            linearScale = new Vector3(currentScale, 1f, currentScale);
        }

        public override void TimeInterval(float deltaTime)
        {
            currentScale += GrowthRate * deltaTime;
            if (currentScale > TargetMaxScale)
            {
                currentScale = 0.5f;
            }
            linearScale = new Vector3(currentScale, 1f, currentScale);

            if (Find.TickManager.TicksGame % 15 == 0)
            {
                CheckProximity();
            }
        }

        private void CheckProximity()
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar != null)
            {
                if (avatar.MapHeld == Map && avatar.PositionHeld.DistanceToSquared(Position) <= DetectionRadiusSq)
                {
                    Destroy();
                }
            }
            else
            {
                foreach (var colonist in Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist.Position.DistanceToSquared(Position) <= DetectionRadiusSq)
                    {
                        Destroy();
                        return;
                    }
                }
            }
        }

        public override float Alpha => Mathf.Clamp01(1f - currentScale / TargetMaxScale) * 0.85f;
    }
}
