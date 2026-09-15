using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [HotSwappable]
    public static class ShadowCaster
    {
        private const int StackCapacity = 512;
        private static readonly Column[] Stack = new Column[StackCapacity];

        private struct Column
        {
            public int depth;
            public float minSlope;
            public float maxSlope;
        }

        public static void ComputeFieldOfView(IntVec3 origin, int radius, Map map, bool[] visibleCells)
        {
            var mapSizeX = map.Size.x;
            var mapSizeZ = map.Size.z;
            var edifices = map.edificeGrid.InnerArray;
            visibleCells[origin.z * mapSizeX + origin.x] = true;
            var radiusSq = radius * radius;

            for (var octant = 0; octant < 8; octant++)
            {
                ScanOctant(octant, origin.x, origin.z, radius, radiusSq, mapSizeX, mapSizeZ, edifices, visibleCells);
            }
        }

        public static void ComputeConeFieldOfView(IntVec3 origin, int radius, Rot4 rot, float maxSlope, Map map, bool[] visibleCells)
        {
            var mapSizeX = map.Size.x;
            var mapSizeZ = map.Size.z;
            var edifices = map.edificeGrid.InnerArray;
            visibleCells[origin.z * mapSizeX + origin.x] = true;
            var radiusSq = radius * radius;

            int oct1, oct2;
            if (rot == Rot4.North) { oct1 = 1; oct2 = 2; }
            else if (rot == Rot4.South) { oct1 = 5; oct2 = 6; }
            else if (rot == Rot4.East) { oct1 = 0; oct2 = 7; }
            else { oct1 = 3; oct2 = 4; }

            ScanOctant(oct1, origin.x, origin.z, radius, radiusSq, mapSizeX, mapSizeZ, edifices, visibleCells, 0f, maxSlope);
            ScanOctant(oct2, origin.x, origin.z, radius, radiusSq, mapSizeX, mapSizeZ, edifices, visibleCells, 0f, maxSlope);
        }

        private static void ScanOctant(int octant, int originX, int originZ, int radius, int radiusSq, int mapSizeX, int mapSizeZ, Building[] edifices, bool[] visibleCells, float minSlope = 0f, float maxSlope = 1f)
        {
            var stackIndex = 0;
            Stack[0] = new Column
            {
                depth = 1,
                minSlope = minSlope,
                maxSlope = maxSlope
            };

            while (stackIndex >= 0)
            {
                var col = Stack[stackIndex--];
                if (col.depth > radius || col.minSlope >= col.maxSlope) continue;

                var minRow = Mathf.FloorToInt(col.depth * col.minSlope + 0.5f);
                var maxRow = Mathf.FloorToInt(col.depth * col.maxSlope + 0.5f);
                if (minRow > maxRow) continue;

                var prevOpaque = false;
                var curMinSlope = col.minSlope;

                for (var row = minRow; row <= maxRow; row++)
                {
                    int cx, cz;
                    switch (octant)
                    {
                        case 0: cx = originX + col.depth; cz = originZ + row; break;
                        case 1: cx = originX + row; cz = originZ + col.depth; break;
                        case 2: cx = originX - row; cz = originZ + col.depth; break;
                        case 3: cx = originX - col.depth; cz = originZ + row; break;
                        case 4: cx = originX - col.depth; cz = originZ - row; break;
                        case 5: cx = originX - row; cz = originZ - col.depth; break;
                        case 6: cx = originX + row; cz = originZ - col.depth; break;
                        default: cx = originX + col.depth; cz = originZ - row; break;
                    }

                    if ((uint)cx >= (uint)mapSizeX || (uint)cz >= (uint)mapSizeZ) continue;

                    var idx = cz * mapSizeX + cx;
                    var dx = cx - originX;
                    var dz = cz - originZ;

                    var ed = edifices[idx];
                    var isWall = ed != null && ed.CanBeSeenOver() is false;

                    var isPinched = false;
                    if (isWall is false && dx != 0 && dz != 0)
                    {
                        var sx = dx > 0 ? 1 : -1;
                        var sz = dz > 0 ? 1 : -1;
                        var ed1 = edifices[cz * mapSizeX + (cx - sx)];
                        var ed2 = edifices[(cz - sz) * mapSizeX + cx];
                        if (ed1 != null && ed1.CanBeSeenOver() is false && ed2 != null && ed2.CanBeSeenOver() is false)
                        {
                            isPinched = true;
                        }
                    }

                    var isOpaque = isWall || isPinched;

                    if (isPinched is false && dx * dx + dz * dz <= radiusSq)
                    {
                        visibleCells[idx] = true;
                    }

                    if (isOpaque)
                    {
                        var newMaxSlope = (row - 0.5f) / col.depth;
                        if (prevOpaque is false && newMaxSlope > curMinSlope && stackIndex < StackCapacity - 1)
                        {
                            Stack[++stackIndex] = new Column
                            {
                                depth = col.depth + 1,
                                minSlope = curMinSlope,
                                maxSlope = newMaxSlope
                            };
                        }
                        prevOpaque = true;
                    }
                    else
                    {
                        if (prevOpaque)
                        {
                            curMinSlope = (row - 0.5f) / col.depth;
                        }
                        prevOpaque = false;
                    }
                }

                if (prevOpaque is false && curMinSlope < col.maxSlope && stackIndex < StackCapacity - 1)
                {
                    Stack[++stackIndex] = new Column
                    {
                        depth = col.depth + 1,
                        minSlope = curMinSlope,
                        maxSlope = col.maxSlope
                    };
                }
            }
        }
    }
}
