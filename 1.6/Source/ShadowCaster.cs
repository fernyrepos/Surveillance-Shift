using UnityEngine;
using Verse;

namespace SurveillanceShift
{
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

        private static void ScanOctant(int octant, int originX, int originZ, int radius, int radiusSq, int mapSizeX, int mapSizeZ, Building[] edifices, bool[] visibleCells)
        {
            var stackIndex = 0;
            Stack[0] = new Column
            {
                depth = 1,
                minSlope = 0f,
                maxSlope = 1f
            };

            while (stackIndex >= 0)
            {
                var col = Stack[stackIndex--];
                if (col.depth > radius) continue;

                var minRow = Mathf.FloorToInt(col.depth * col.minSlope);
                var maxRow = Mathf.CeilToInt(col.depth * col.maxSlope);

                var prevOpaque = false;

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
                    if (dx * dx + dz * dz <= radiusSq)
                    {
                        visibleCells[idx] = true;
                    }

                    var ed = edifices[idx];
                    var isOpaque = ed != null && ed.def.Fillage == FillCategory.Full;

                    if (isOpaque)
                    {
                        if (prevOpaque is false && row > minRow && stackIndex < StackCapacity - 1)
                        {
                            Stack[++stackIndex] = new Column
                            {
                                depth = col.depth + 1,
                                minSlope = col.minSlope,
                                maxSlope = (row - 0.5f) / col.depth
                            };
                        }
                        prevOpaque = true;
                    }
                    else
                    {
                        if (prevOpaque)
                        {
                            col.minSlope = (row - 0.5f) / col.depth;
                        }
                        prevOpaque = false;
                    }
                }

                if (prevOpaque is false && stackIndex < StackCapacity - 1)
                {
                    Stack[++stackIndex] = new Column
                    {
                        depth = col.depth + 1,
                        minSlope = col.minSlope,
                        maxSlope = col.maxSlope
                    };
                }
            }
        }
    }
}
