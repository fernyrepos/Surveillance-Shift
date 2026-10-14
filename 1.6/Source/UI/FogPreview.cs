using System.IO;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public class FogPreview
    {
        public const int Width = 640;
        public const int Height = 360;

        private const int Pixels = Width * Height;
        private const float CellPixels = 14f;
        private const float VisionCenterX = 210f;
        private const float VisionCenterY = 224f;
        private const float VisionRadius = 168f;
        private const float VisionSoftness = 20f;
        private const float UndiscoveredStart = 400f;
        private const float UndiscoveredSoftness = 40f;

        private static readonly Color Outline = new(0.06f, 0.06f, 0.06f);
        private static readonly Color ColonistBody = new(0.2f, 0.45f, 0.85f);
        private static readonly Color RaiderBody = new(0.75f, 0.14f, 0.12f);
        private static readonly Color Animal = new(0.5f, 0.34f, 0.2f);

        private readonly float[] withColonist = new float[Pixels * 3];
        private readonly float[] withAll = new float[Pixels * 3];
        private readonly float[] mask = new float[Pixels];
        private readonly float[] blurA = new float[Pixels * 3];
        private readonly float[] blurB = new float[Pixels * 3];
        private readonly byte[] bytes = new byte[Pixels * 4];

        private Texture2D texture;
        private int builtVersion = -1;

        public FogPreview()
        {
            BuildScene();
            BuildMask();
        }

        public Texture2D Texture
        {
            get
            {
                Refresh();
                return texture;
            }
        }

        public void Dispose()
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }

        private void Refresh()
        {
            if (texture == null)
            {
                texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false, true)
                {
                    name = "SS_FogPreview",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                builtVersion = -1;
            }
            if (builtVersion == FogStyle.Version) return;

            builtVersion = FogStyle.Version;
            Compose();
            texture.SetPixelData(bytes, 0);
            texture.Apply(false, false);
        }

        private void Compose()
        {
            var graded = FogStyle.UsesGrade;
            var level = FogStyle.HardcoreLevel;
            var undiscovered = FogStyle.UsesUndiscoveredFog;
            if (graded)
            {
                var source = FogStyle.PawnsVisibleInFog ? withAll : withColonist;
                Blur(source, blurA, blurB, Mathf.RoundToInt(FogStyle.Blur * CellPixels));
                Grade(blurA);
            }

            for (var y = 0; y < Height; y++)
            {
                var row = (Height - 1 - y) * Width;
                for (var x = 0; x < Width; x++)
                {
                    var i = y * Width + x;
                    var m = mask[i];
                    var o = (row + x) * 4;
                    for (var c = 0; c < 3; c++)
                    {
                        var visible = withAll[i * 3 + c];
                        var fogged = graded ? blurA[i * 3 + c] : level;
                        if (graded && undiscovered)
                        {
                            var unseen = Mathf.Clamp01((x - UndiscoveredStart) / UndiscoveredSoftness);
                            fogged = Mathf.Lerp(fogged, level, unseen);
                        }
                        bytes[o + c] = (byte)(Mathf.Clamp01(Mathf.Lerp(visible, fogged, m)) * 255f + 0.5f);
                    }
                    bytes[o + 3] = 255;
                }
            }
        }

        private static void Grade(float[] rgb)
        {
            var contrast = FogStyle.Contrast;
            var saturation = FogStyle.Saturation;
            var brightness = FogStyle.Brightness;

            for (var i = 0; i < rgb.Length; i += 3)
            {
                var r = rgb[i];
                var g = rgb[i + 1];
                var b = rgb[i + 2];
                var lum = r * 0.22f + g * 0.707f + b * 0.071f;
                r = Mathf.Lerp(lum, r, saturation);
                g = Mathf.Lerp(lum, g, saturation);
                b = Mathf.Lerp(lum, b, saturation);
                rgb[i] = ((r - 0.5f) * contrast + 0.5f) * brightness;
                rgb[i + 1] = ((g - 0.5f) * contrast + 0.5f) * brightness;
                rgb[i + 2] = ((b - 0.5f) * contrast + 0.5f) * brightness;
            }
        }

        private static void Blur(float[] src, float[] dst, float[] scratch, int radius)
        {
            System.Array.Copy(src, dst, src.Length);
            if (radius <= 0) return;

            for (var pass = 0; pass < 2; pass++)
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var c = 0; c < 3; c++)
                    {
                        BoxLine(dst, scratch, y * Width * 3 + c, 3, Width, radius);
                    }
                }
                for (var x = 0; x < Width; x++)
                {
                    for (var c = 0; c < 3; c++)
                    {
                        BoxLine(scratch, dst, x * 3 + c, Width * 3, Height, radius);
                    }
                }
            }
        }

        private static void BoxLine(float[] src, float[] dst, int origin, int step, int count, int radius)
        {
            var inv = 1f / (radius * 2 + 1);
            var sum = src[origin] * (radius + 1);
            for (var i = 1; i <= radius; i++)
            {
                sum += src[origin + Mathf.Min(i, count - 1) * step];
            }
            for (var i = 0; i < count; i++)
            {
                dst[origin + i * step] = sum * inv;
                sum += src[origin + Mathf.Min(i + radius + 1, count - 1) * step] - src[origin + Mathf.Max(i - radius, 0) * step];
            }
        }

        private void BuildMask()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var dx = x - VisionCenterX;
                    var dy = y - VisionCenterY;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var t = Mathf.Clamp01((distance - (VisionRadius - VisionSoftness)) / (VisionSoftness * 2f));
                    mask[y * Width + x] = t * t * (3f - 2f * t);
                }
            }
        }

        private void BuildScene()
        {
            var data = LoadImage();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var src = (y * Width + x) * 3;
                    var color = data != null
                        ? new Color(data[src] / 255f, data[src + 1] / 255f, data[src + 2] / 255f)
                        : new Color(0.3f, 0.3f, 0.3f);
                    Set(withColonist, x, y, color);
                }
            }

            Marker(withColonist, MarkerShape.Circle, VisionCenterX, VisionCenterY, ColonistBody);

            System.Array.Copy(withColonist, withAll, withColonist.Length);
            Marker(withAll, MarkerShape.Square, 464f, 124f, RaiderBody);
            Marker(withAll, MarkerShape.Square, 410f, 284f, RaiderBody);
            Marker(withAll, MarkerShape.Triangle, 580f, 224f, Animal);
        }

        private static byte[] LoadImage()
        {
            try
            {
                var path = Path.Combine(LoadedModManager.GetMod<SurveillanceShiftMod>().Content.RootDir, "Data", "FogPreview.rgb");
                if (File.Exists(path) is false) return null;
                var data = File.ReadAllBytes(path);
                return data.Length == Pixels * 3 ? data : null;
            }
            catch (System.Exception e)
            {
                Log.Warning("[Surveillance Shift] Could not load the fog preview image: " + e.Message);
                return null;
            }
        }

        private enum MarkerShape
        {
            Circle,
            Square,
            Triangle
        }

        private static void Marker(float[] buffer, MarkerShape shape, float cx, float cy, Color color)
        {
            const float outer = 17f;
            const float inner = 13f;
            Shape(buffer, shape, cx, cy, outer, Outline);
            Shape(buffer, shape, cx, cy, inner, color);
        }

        private static void Shape(float[] buffer, MarkerShape shape, float cx, float cy, float size, Color color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - size - 1f));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + size + 1f));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - size - 1f));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + size + 1f));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    float coverage;
                    switch (shape)
                    {
                        case MarkerShape.Circle:
                            coverage = size - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f;
                            break;
                        case MarkerShape.Square:
                            coverage = size * 0.85f - Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 0.5f;
                            break;
                        default:
                            coverage = Mathf.Min(size * 0.8f - dy, (dy + size) / 1.8f - Mathf.Abs(dx)) + 0.5f;
                            break;
                    }
                    coverage = Mathf.Clamp01(coverage);
                    if (coverage <= 0f) continue;
                    var i = (y * Width + x) * 3;
                    buffer[i] = Mathf.Lerp(buffer[i], color.r, coverage);
                    buffer[i + 1] = Mathf.Lerp(buffer[i + 1], color.g, coverage);
                    buffer[i + 2] = Mathf.Lerp(buffer[i + 2], color.b, coverage);
                }
            }
        }

        private static void Set(float[] buffer, int x, int y, Color color)
        {
            var i = (y * Width + x) * 3;
            buffer[i] = color.r;
            buffer[i + 1] = color.g;
            buffer[i + 2] = color.b;
        }
    }
}
