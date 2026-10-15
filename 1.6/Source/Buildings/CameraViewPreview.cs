using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public static class CameraViewPreview
    {
        private const int RefreshTicks = 30;
        private static readonly Color EdgeColor = new Color(1f, 1f, 1f, 0.9f);

        private static readonly List<IntVec3> cells = new();
        private static bool[] buffer;
        private static Map cachedMap;
        private static IntVec3 cachedPosition;
        private static Rot4 cachedRotation;
        private static int cachedRadius;
        private static float cachedCone;
        private static bool cachedWall;
        private static int cachedTick = -9999;

        private static readonly MethodInfo drawFieldEdges = FindDrawFieldEdges();
        private static readonly object[] drawArgs = BuildArgs();

        private static MethodInfo FindDrawFieldEdges()
        {
            foreach (var method in typeof(GenDraw).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != nameof(GenDraw.DrawFieldEdges)) continue;
                var parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(List<IntVec3>) && parameters[1].ParameterType == typeof(Color)) return method;
            }
            return null;
        }

        private static object[] BuildArgs()
        {
            if (drawFieldEdges is null) return null;
            var parameters = drawFieldEdges.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 2; i < parameters.Length; i++)
            {
                var value = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                args[i] = value is System.DBNull ? null : value;
            }
            return args;
        }

        private static void DrawEdges()
        {
            if (drawFieldEdges is null) return;
            drawArgs[0] = cells;
            drawArgs[1] = EdgeColor;
            drawFieldEdges.Invoke(null, drawArgs);
        }

        public static void Draw(Map map, IntVec3 position, Rot4 rotation, int radius, float coneAngle, bool wallMounted)
        {
            if (map is null || position.InBounds(map) is false) return;

            var tick = Find.TickManager.TicksGame;
            if (map != cachedMap || position != cachedPosition || rotation != cachedRotation || radius != cachedRadius || coneAngle != cachedCone || wallMounted != cachedWall || Mathf.Abs(tick - cachedTick) >= RefreshTicks)
            {
                cachedMap = map;
                cachedPosition = position;
                cachedRotation = rotation;
                cachedRadius = radius;
                cachedCone = coneAngle;
                cachedWall = wallMounted;
                cachedTick = tick;
                Rebuild(map, position, rotation, radius, coneAngle, wallMounted);
            }

            DrawEdges();
        }

        private static void Rebuild(Map map, IntVec3 position, Rot4 rotation, int radius, float coneAngle, bool wallMounted)
        {
            var count = map.cellIndices.NumGridCells;
            if (buffer == null || buffer.Length != count)
            {
                buffer = new bool[count];
            }
            else
            {
                System.Array.Clear(buffer, 0, count);
            }

            if (wallMounted)
            {
                Building_WallCamera.ComputeCone(map, position, rotation, radius, coneAngle, buffer);
            }
            else
            {
                ShadowCaster.ComputeFieldOfView(position, radius, map, buffer);
            }

            cells.Clear();
            var minX = Mathf.Max(0, position.x - radius - 1);
            var maxX = Mathf.Min(map.Size.x - 1, position.x + radius + 1);
            var minZ = Mathf.Max(0, position.z - radius - 1);
            var maxZ = Mathf.Min(map.Size.z - 1, position.z + radius + 1);
            var sizeX = map.Size.x;
            for (var z = minZ; z <= maxZ; z++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (buffer[z * sizeX + x])
                    {
                        cells.Add(new IntVec3(x, 0, z));
                    }
                }
            }
        }
    }
}
