using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [StaticConstructorOnStartup]
    [HotSwappable]
    public class MapComponent_FogOfWar : MapComponent
    {
        private const int StandardSightRadius = 26;
        private const int DownedSightRadius = 4;
        private const int UpdateIntervalTicks = 12;
        private const int SectionSize = 16;
        private static readonly float FogAltitude = AltitudeLayer.Weather.AltitudeFor() + 0.05f;

        private static readonly Material DarkFogMat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, Color.black, 3900);

        private bool[] visibleCells;
        private bool[] prevCells;
        private readonly Dictionary<int, Mesh> sectionMeshes = new();
        private readonly List<Vector3> sectionVertices = new(SectionSize * SectionSize * 4);
        private readonly List<Vector2> sectionUVs = new(SectionSize * SectionSize * 4);
        private readonly List<int> sectionIndices = new(SectionSize * SectionSize * 6);
        private int sectionsX;
        private int sectionsZ;
        private bool firstPass = true;
        private List<MutedArrival> mutedArrivals = new();

        public class MutedArrival : IExposable
        {
            public Letter letter;
            public List<Pawn> pawns = new();
            public bool isHostile;

            public void ExposeData()
            {
                Scribe_Deep.Look(ref letter, "letter");
                Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
                Scribe_Values.Look(ref isHostile, "isHostile");
            }
        }

        public MapComponent_FogOfWar(Map map) : base(map)
        {
            visibleCells = new bool[map.cellIndices.NumGridCells];
            prevCells = new bool[map.cellIndices.NumGridCells];
        }

        public bool IsVisible(IntVec3 cell)
        {
            if ((uint)cell.x >= (uint)map.Size.x || (uint)cell.z >= (uint)map.Size.z) return false;
            return visibleCells[cell.z * map.Size.x + cell.x];
        }

        public void QueueArrival(Letter letter, List<Pawn> pawns, bool isHostile)
        {
            mutedArrivals.Add(new MutedArrival
            {
                letter = letter,
                pawns = pawns,
                isHostile = isHostile
            });
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (State.IsLimitedAwareness)
            {
                LongEventHandler.ExecuteWhenFinished(RecalculateVisibility);
            }
        }

        public override void MapComponentTick()
        {
            if (State.IsLimitedAwareness && Find.TickManager.TicksGame % UpdateIntervalTicks == 0)
            {
                RecalculateVisibility();
                ProcessMutedArrivals();
            }
        }

        public override void MapComponentUpdate()
        {
            if (State.IsLimitedAwareness is false || Find.CurrentMap != map) return;

            var viewRect = Find.CameraDriver.CurrentViewRect;
            var minSX = Mathf.Max(0, viewRect.minX / SectionSize);
            var maxSX = Mathf.Min(sectionsX - 1, viewRect.maxX / SectionSize);
            var minSZ = Mathf.Max(0, viewRect.minZ / SectionSize);
            var maxSZ = Mathf.Min(sectionsZ - 1, viewRect.maxZ / SectionSize);

            for (var sz = minSZ; sz <= maxSZ; sz++)
            {
                var row = sz * sectionsX;
                for (var sx = minSX; sx <= maxSX; sx++)
                {
                    if (sectionMeshes.TryGetValue(row + sx, out var mesh))
                    {
                        Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, DarkFogMat, 0);
                    }
                }
            }
        }

        private void RecalculateVisibility()
        {
            var count = map.cellIndices.NumGridCells;
            if (visibleCells.Length != count)
            {
                visibleCells = new bool[count];
                prevCells = new bool[count];
                firstPass = true;
            }
            (visibleCells, prevCells) = (prevCells, visibleCells);
            Array.Clear(visibleCells, 0, count);

            if (ModCompatibility.IsPerspectiveShiftAuthentic())
            {
                var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
                if (avatar != null && avatar.Spawned && avatar.Map == map && avatar.Dead is false)
                {
                    var radius = avatar.Downed ? DownedSightRadius : StandardSightRadius;
                    ShadowCaster.ComputeFieldOfView(avatar.Position, radius, map, visibleCells);
                }
            }
            else
            {
                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn.Dead is false)
                    {
                        var radius = pawn.Downed ? DownedSightRadius : StandardSightRadius;
                        ShadowCaster.ComputeFieldOfView(pawn.Position, radius, map, visibleCells);
                    }
                }
            }

            RebuildSections();
        }

        private void RebuildSections()
        {
            sectionsX = (map.Size.x + SectionSize - 1) / SectionSize;
            sectionsZ = (map.Size.z + SectionSize - 1) / SectionSize;
            var mapX = map.Size.x;
            var mapZ = map.Size.z;

            for (var sz = 0; sz < sectionsZ; sz++)
            {
                var z0 = sz * SectionSize;
                var zMax = Mathf.Min(z0 + SectionSize, mapZ);
                var zSpan = zMax - z0;

                for (var sx = 0; sx < sectionsX; sx++)
                {
                    var x0 = sx * SectionSize;
                    var xMax = Mathf.Min(x0 + SectionSize, mapX);
                    var totalCells = (xMax - x0) * zSpan;

                    var changed = firstPass;
                    var foggedCount = 0;

                    for (var z = z0; z < zMax; z++)
                    {
                        var row = z * mapX;
                        for (var x = x0; x < xMax; x++)
                        {
                            var idx = row + x;
                            if (visibleCells[idx] is false)
                            {
                                foggedCount++;
                            }
                            if (changed is false && visibleCells[idx] != prevCells[idx])
                            {
                                changed = true;
                            }
                        }
                    }

                    if (changed is false) continue;

                    var key = sz * sectionsX + sx;

                    if (foggedCount == 0)
                    {
                        if (sectionMeshes.Remove(key, out var cleared))
                        {
                            UnityEngine.Object.Destroy(cleared);
                        }
                        continue;
                    }

                    sectionVertices.Clear();
                    sectionUVs.Clear();
                    sectionIndices.Clear();

                    if (foggedCount == totalCells)
                    {
                        AddQuad(x0, z0, xMax, zMax);
                    }
                    else
                    {
                        for (var z = z0; z < zMax; z++)
                        {
                            var row = z * mapX;
                            for (var x = x0; x < xMax; x++)
                            {
                                if (visibleCells[row + x] is false)
                                {
                                    AddQuad(x, z, x + 1, z + 1);
                                }
                            }
                        }
                    }

                    if (sectionMeshes.TryGetValue(key, out var mesh) is false)
                    {
                        mesh = new Mesh
                        {
                            name = "SSFog_" + sx + "_" + sz
                        };
                        sectionMeshes[key] = mesh;
                    }
                    mesh.Clear();
                    mesh.SetVertices(sectionVertices);
                    mesh.SetUVs(0, sectionUVs);
                    mesh.SetIndices(sectionIndices, MeshTopology.Triangles, 0);
                }
            }

            firstPass = false;
        }

        private void AddQuad(float x1, float z1, float x2, float z2)
        {
            var v = sectionVertices.Count;
            sectionVertices.Add(new Vector3(x1, FogAltitude, z1));
            sectionVertices.Add(new Vector3(x1, FogAltitude, z2));
            sectionVertices.Add(new Vector3(x2, FogAltitude, z2));
            sectionVertices.Add(new Vector3(x2, FogAltitude, z1));
            sectionUVs.Add(new Vector2(0f, 0f));
            sectionUVs.Add(new Vector2(0f, 1f));
            sectionUVs.Add(new Vector2(1f, 1f));
            sectionUVs.Add(new Vector2(1f, 0f));
            sectionIndices.Add(v);
            sectionIndices.Add(v + 1);
            sectionIndices.Add(v + 2);
            sectionIndices.Add(v);
            sectionIndices.Add(v + 2);
            sectionIndices.Add(v + 3);
        }

        private void ProcessMutedArrivals()
        {
            for (int i = mutedArrivals.Count - 1; i >= 0; i--)
            {
                var arrival = mutedArrivals[i];
                arrival.pawns.RemoveAll(p => p.Dead || p.Discarded || p.Spawned && p.Map != map);

                if (arrival.pawns.Count == 0)
                {
                    mutedArrivals.RemoveAt(i);
                    continue;
                }

                foreach (var pawn in arrival.pawns)
                {
                    if (pawn.Spawned && pawn.Position.InBounds(map) && visibleCells[map.cellIndices.CellToIndex(pawn.Position)])
                    {
                        arrival.letter.lookTargets = new LookTargets(pawn);
                        Find.LetterStack.ReceiveLetter(arrival.letter);
                        State.SpawnPing(pawn.Position, map, arrival.isHostile);
                        mutedArrivals.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            foreach (var mesh in sectionMeshes.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            sectionMeshes.Clear();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref mutedArrivals, "SS_MutedArrivals", LookMode.Deep);
        }
    }
}
