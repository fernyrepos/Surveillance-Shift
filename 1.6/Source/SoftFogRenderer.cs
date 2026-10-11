using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HotSwappable]
    public class SoftFogRenderer : IDisposable
    {
        public static bool Enabled = true;

        public static float FadeInSeconds = 0.45f;
        public static float FadeOutSeconds = 0.25f;
        public static int BlurRadius = 2;
        public static int BlurPasses = 2;
        public static bool HardWalls = true;
        public static bool HardOcclusion = true;
        public static int HardSharpness = 6;
        public static int BarrierRefreshTicks = 120;
        public static float EdgeWidth = 0.22f;
        public static float EdgeCenter = 0.52f;
        public static Color FogColor = new ColorInt(25, 25, 25).ToColor;

        private const int WorkerFrameMs = 8;
        private const int WorkerIdleMs = 40;
        private const int RenderQueue = 4300;

        private readonly Map map;
        private readonly int mapX;
        private readonly int mapZ;
        private readonly int cellCount;
        private readonly float altitude;

        private readonly object sync = new();
        private readonly bool[] pending;
        private readonly bool[] barriers;
        private readonly bool[] barrierScratch;
        private readonly bool[] barrierView;
        private readonly bool[] hardCells;
        private readonly bool[] hardScratch;
        private readonly bool[] hardView;
        private bool hasPending;
        private int lastBarrierTick = -99999;
        private byte[] readyBuffer;
        private byte[] hardReadyBuffer;
        private bool readyDirty;

        private byte[] writeBuffer;
        private byte[] hardWriteBuffer;
        private float[] current;
        private float[] target;
        private float[] blurA;
        private float[] blurB;
        private bool snapNext = true;

        private Thread worker;
        private volatile bool running;

        private Texture2D texture;
        private Texture2D hardTexture;
        private Material material;
        private Material hardMaterial;
        private Mesh mesh;

        public SoftFogRenderer(Map map)
        {
            this.map = map;
            mapX = map.Size.x;
            mapZ = map.Size.z;
            cellCount = mapX * mapZ;
            altitude = AltitudeLayer.MapDataOverlay.AltitudeFor() + 0.05f;

            pending = new bool[cellCount];
            barriers = new bool[cellCount];
            barrierScratch = new bool[cellCount];
            barrierView = new bool[cellCount];
            hardCells = new bool[cellCount];
            hardScratch = new bool[cellCount];
            hardView = new bool[cellCount];
            current = new float[cellCount];
            target = new float[cellCount];
            blurA = new float[cellCount];
            blurB = new float[cellCount];
            writeBuffer = new byte[cellCount * 4];
            readyBuffer = new byte[cellCount * 4];
            hardWriteBuffer = new byte[cellCount * 4];
            hardReadyBuffer = new byte[cellCount * 4];

            for (var i = 0; i < cellCount; i++)
            {
                current[i] = 1f;
                target[i] = 1f;
            }

            running = true;
            worker = new Thread(WorkerLoop)
            {
                Name = "SS_SoftFog_" + map.uniqueID,
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            };
            worker.Start();
        }

        public void Submit(bool[] visibleCells)
        {
            if (visibleCells.Length != cellCount) return;
            var refreshed = RefreshBarrierScratch();
            lock (sync)
            {
                Array.Copy(visibleCells, pending, cellCount);
                if (refreshed)
                {
                    Array.Copy(barrierScratch, barriers, cellCount);
                    Array.Copy(hardScratch, hardCells, cellCount);
                }
                hasPending = true;
            }
        }

        private bool RefreshBarrierScratch()
        {
            if (HardWalls is false) return false;
            var tick = Find.TickManager.TicksGame;
            if (tick - lastBarrierTick < BarrierRefreshTicks) return false;
            lastBarrierTick = tick;

            var edifices = map.edificeGrid.InnerArray;
            for (var i = 0; i < cellCount; i++)
            {
                var ed = edifices[i];
                barrierScratch[i] = ed != null && ed.CanBeSeenOver() is false && ModCompatibility.IsSeeThroughWall(ed) is false;
            }

            Array.Clear(hardScratch, 0, cellCount);
            for (var z = 0; z < mapZ; z++)
            {
                var row = z * mapX;
                for (var x = 0; x < mapX; x++)
                {
                    if (barrierScratch[row + x] is false) continue;
                    var zMin = Mathf.Max(z - 1, 0);
                    var zMax = Mathf.Min(z + 1, mapZ - 1);
                    var xMin = Mathf.Max(x - 1, 0);
                    var xMax = Mathf.Min(x + 1, mapX - 1);
                    for (var nz = zMin; nz <= zMax; nz++)
                    {
                        var nRow = nz * mapX;
                        for (var nx = xMin; nx <= xMax; nx++)
                        {
                            if (barrierScratch[nRow + nx] is false)
                            {
                                hardScratch[nRow + nx] = true;
                            }
                        }
                    }
                }
            }
            return true;
        }

        public void SubmitAllVisible()
        {
            lock (sync)
            {
                Array.Clear(pending, 0, cellCount);
                for (var i = 0; i < cellCount; i++)
                {
                    pending[i] = true;
                }
                hasPending = true;
            }
        }

        public void Draw()
        {
            EnsureResources();

            lock (sync)
            {
                if (readyDirty)
                {
                    texture.SetPixelData(readyBuffer, 0);
                    hardTexture.SetPixelData(hardReadyBuffer, 0);
                    readyDirty = false;
                    texture.Apply(false, false);
                    hardTexture.Apply(false, false);
                }
            }

            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
            if (HardOcclusion)
            {
                Graphics.DrawMesh(mesh, new Vector3(0f, 0.001f, 0f), Quaternion.identity, hardMaterial, 0);
            }
        }

        private void EnsureResources()
        {
            if (texture == null)
            {
                texture = new Texture2D(mapX, mapZ, TextureFormat.RGBA32, false, true)
                {
                    name = "SS_SoftFog_" + map.uniqueID,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
            }
            if (hardTexture == null)
            {
                hardTexture = new Texture2D(mapX, mapZ, TextureFormat.RGBA32, false, true)
                {
                    name = "SS_HardFog_" + map.uniqueID,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
            }
            if (material == null)
            {
                material = new Material(ShaderDatabase.Transparent)
                {
                    name = "SS_SoftFogMat",
                    mainTexture = texture,
                    color = Color.white,
                    renderQueue = RenderQueue
                };
            }
            if (hardMaterial == null)
            {
                hardMaterial = new Material(ShaderDatabase.Transparent)
                {
                    name = "SS_HardFogMat",
                    mainTexture = hardTexture,
                    color = Color.white,
                    renderQueue = RenderQueue + 1
                };
            }
            if (mesh == null)
            {
                mesh = new Mesh { name = "SS_SoftFogQuad" };
                mesh.SetVertices(new[]
                {
                    new Vector3(0f, altitude, 0f),
                    new Vector3(0f, altitude, mapZ),
                    new Vector3(mapX, altitude, mapZ),
                    new Vector3(mapX, altitude, 0f)
                });
                mesh.SetUVs(0, new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f)
                });
                mesh.SetIndices(new[] { 0, 1, 2, 0, 2, 3 }, MeshTopology.Triangles, 0);
                mesh.bounds = new Bounds(new Vector3(mapX * 0.5f, altitude, mapZ * 0.5f), new Vector3(mapX, 1f, mapZ));
            }
        }

        private void WorkerLoop()
        {
            var clock = Stopwatch.StartNew();
            var last = clock.Elapsed.TotalSeconds;

            while (running)
            {
                var now = clock.Elapsed.TotalSeconds;
                var dt = (float)(now - last);
                last = now;
                if (dt > 0.25f) dt = 0.25f;

                var rebuilt = false;
                lock (sync)
                {
                    if (hasPending)
                    {
                        hasPending = false;
                        for (var i = 0; i < cellCount; i++)
                        {
                            blurA[i] = pending[i] ? 0f : 1f;
                        }
                        Array.Copy(barriers, barrierView, cellCount);
                        Array.Copy(hardCells, hardView, cellCount);
                        rebuilt = true;
                    }
                }

                if (rebuilt)
                {
                    Blur(blurA, blurB, target);
                }

                var moved = Step(dt);

                if (moved || rebuilt)
                {
                    Rasterize();
                    lock (sync)
                    {
                        (writeBuffer, readyBuffer) = (readyBuffer, writeBuffer);
                        (hardWriteBuffer, hardReadyBuffer) = (hardReadyBuffer, hardWriteBuffer);
                        readyDirty = true;
                    }
                    Thread.Sleep(WorkerFrameMs);
                }
                else
                {
                    Thread.Sleep(WorkerIdleMs);
                }
            }
        }

        private void Blur(float[] src, float[] scratch, float[] dst)
        {
            var radius = Mathf.Max(0, BlurRadius);
            var passes = Mathf.Max(1, BlurPasses);
            if (radius == 0)
            {
                Array.Copy(src, dst, cellCount);
                return;
            }

            var a = src;
            var b = scratch;
            for (var p = 0; p < passes; p++)
            {
                BlurHorizontal(a, b, radius);
                BlurVertical(b, a, radius);
            }
            Contrast(a, dst);
        }

        private void Contrast(float[] src, float[] dst)
        {
            var half = Mathf.Max(0.001f, EdgeWidth) * 0.5f;
            var lo = EdgeCenter - half;
            var inv = 1f / (half * 2f);

            for (var i = 0; i < cellCount; i++)
            {
                var v = (src[i] - lo) * inv;
                if (v <= 0f)
                {
                    dst[i] = 0f;
                }
                else if (v >= 1f)
                {
                    dst[i] = 1f;
                }
                else
                {
                    dst[i] = v * v * (3f - 2f * v);
                }
            }
        }

        private void BlurHorizontal(float[] src, float[] dst, int radius)
        {
            var inv = 1f / (radius * 2 + 1);
            var hard = HardWalls;

            for (var z = 0; z < mapZ; z++)
            {
                var row = z * mapX;
                var x = 0;
                while (x < mapX)
                {
                    if (hard && barrierView[row + x])
                    {
                        dst[row + x] = src[row + x];
                        x++;
                        continue;
                    }

                    var s0 = x;
                    var e0 = x;
                    while (e0 + 1 < mapX && (hard is false || barrierView[row + e0 + 1] is false))
                    {
                        e0++;
                    }
                    BlurSpan(src, dst, row, 1, s0, e0, radius, inv);
                    x = e0 + 1;
                }
            }
        }

        private void BlurVertical(float[] src, float[] dst, int radius)
        {
            var inv = 1f / (radius * 2 + 1);
            var hard = HardWalls;

            for (var x = 0; x < mapX; x++)
            {
                var z = 0;
                while (z < mapZ)
                {
                    if (hard && barrierView[z * mapX + x])
                    {
                        dst[z * mapX + x] = src[z * mapX + x];
                        z++;
                        continue;
                    }

                    var s0 = z;
                    var e0 = z;
                    while (e0 + 1 < mapZ && (hard is false || barrierView[(e0 + 1) * mapX + x] is false))
                    {
                        e0++;
                    }
                    BlurSpan(src, dst, x, mapX, s0, e0, radius, inv);
                    z = e0 + 1;
                }
            }
        }

        private static void BlurSpan(float[] src, float[] dst, int origin, int stride, int s0, int e0, int radius, float inv)
        {
            var sum = src[origin + s0 * stride] * (radius + 1);
            for (var i = 1; i <= radius; i++)
            {
                sum += src[origin + Mathf.Min(s0 + i, e0) * stride];
            }
            for (var i = s0; i <= e0; i++)
            {
                dst[origin + i * stride] = sum * inv;
                sum += src[origin + Mathf.Min(i + radius + 1, e0) * stride] - src[origin + Mathf.Max(i - radius, s0) * stride];
            }
        }

        private bool Step(float dt)
        {
            if (snapNext)
            {
                snapNext = false;
                Array.Copy(target, current, cellCount);
                return true;
            }

            var inStep = dt / Mathf.Max(0.0001f, FadeInSeconds);
            var outStep = dt / Mathf.Max(0.0001f, FadeOutSeconds);
            var moved = false;

            for (var i = 0; i < cellCount; i++)
            {
                var c = current[i];
                var t = target[i];
                var d = t - c;
                if (d > 0.0005f)
                {
                    c += inStep;
                    current[i] = c > t ? t : c;
                    moved = true;
                }
                else if (d < -0.0005f)
                {
                    c -= outStep;
                    current[i] = c < t ? t : c;
                    moved = true;
                }
                else if (c != t)
                {
                    current[i] = t;
                    moved = true;
                }
            }

            return moved;
        }

        private void Rasterize()
        {
            var r = (byte)Mathf.Clamp(Mathf.RoundToInt(FogColor.r * 255f), 0, 255);
            var g = (byte)Mathf.Clamp(Mathf.RoundToInt(FogColor.g * 255f), 0, 255);
            var b = (byte)Mathf.Clamp(Mathf.RoundToInt(FogColor.b * 255f), 0, 255);
            var hard = HardOcclusion;
            var sharpness = Mathf.Max(1, HardSharpness);

            for (var i = 0; i < cellCount; i++)
            {
                var a = current[i];
                var o = i * 4;
                writeBuffer[o] = r;
                writeBuffer[o + 1] = g;
                writeBuffer[o + 2] = b;
                writeBuffer[o + 3] = (byte)(a * 255f);

                hardWriteBuffer[o] = r;
                hardWriteBuffer[o + 1] = g;
                hardWriteBuffer[o + 2] = b;
                hardWriteBuffer[o + 3] = hard && hardView[i] ? (byte)(Sharpen(a, sharpness) * 255f) : (byte)0;
            }
        }

        private static float Sharpen(float a, int power)
        {
            var result = a;
            for (var i = 1; i < power; i++)
            {
                result *= a;
            }
            return result;
        }

        public void Dispose()
        {
            running = false;
            var t = worker;
            worker = null;
            if (t != null && t.IsAlive)
            {
                t.Join(200);
            }
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
            if (hardTexture != null)
            {
                UnityEngine.Object.Destroy(hardTexture);
                hardTexture = null;
            }
            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
                material = null;
            }
            if (hardMaterial != null)
            {
                UnityEngine.Object.Destroy(hardMaterial);
                hardMaterial = null;
            }
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
                mesh = null;
            }
        }
    }
}
