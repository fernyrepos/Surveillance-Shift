using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public class Mote_Ping : Mote
    {
        private const float DetectionRadiusSq = 10f * 10f;
        private const float GraceDelaySeconds = 2f;
        private const float MaxLifespanSeconds = 30f;
        private const float TargetMaxScale = 5f;
        private const float GrowthRate = 3f;
        private const float SpawnInterval = 1.25f;
        private const int TextureSize = 256;
        private const float RingRadius = 0.45f;
        private const float RingFalloff = 0.02f;

        private static readonly MaterialPropertyBlock PropertyBlock = new();
        private readonly List<float> ringScales = new();
        private float spawnTimer;
        private float age;

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
                graphic.MatSingle.renderQueue = 4400;
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
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, true)
            {
                name = "SS_PingRing",
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: true);
            return texture;
        }

        public override bool EndOfLife => age >= MaxLifespanSeconds;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            solidTimeOverride = float.MaxValue;
            ringScales.Add(0.5f);
        }

        public override void TimeInterval(float deltaTime)
        {
            age += deltaTime;
            if (age >= MaxLifespanSeconds)
            {
                Destroy();
            }
            else
            {
                spawnTimer += deltaTime;
                if (spawnTimer >= SpawnInterval)
                {
                    spawnTimer -= SpawnInterval;
                    ringScales.Add(0.5f);
                }

                for (var i = ringScales.Count - 1; i >= 0; i--)
                {
                    ringScales[i] += GrowthRate * deltaTime;
                    if (ringScales[i] >= TargetMaxScale)
                    {
                        ringScales.RemoveAt(i);
                    }
                }

                if (age >= GraceDelaySeconds && Find.TickManager.TicksGame % 15 == 0)
                {
                    CheckProximity();
                }
            }
        }

        private void CheckProximity()
        {
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar != null && avatar.MapHeld == Map)
            {
                if (avatar.PositionHeld.DistanceToSquared(Position) <= DetectionRadiusSq)
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

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var mat = Graphic.MatSingle;
            var baseColor = def.graphicData.color;
            var mesh = MeshPool.plane10;
            foreach (var scale in ringScales)
            {
                var alpha = Mathf.Clamp01(1f - scale / TargetMaxScale) * 0.85f;
                PropertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha));
                var matrix = Matrix4x4.TRS(drawLoc, Quaternion.identity, new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(mesh, matrix, mat, 0, null, 0, PropertyBlock);
            }
        }
    }
}
