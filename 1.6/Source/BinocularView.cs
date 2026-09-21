using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public static class BinocularView
    {
        public static int ScopeRadius = 4;

        private static Pawn activePawn;
        private static IntVec3 lastScopeCell = IntVec3.Invalid;
        private static bool dollyOverridden;

        private const int StopButtonId = 74212093;

        public static Pawn ActivePawn => activePawn;

        public static bool IsActive(Pawn pawn) => pawn != null && activePawn == pawn;

        public static bool BlocksWorldInteraction => activePawn != null && activePawn.Spawned && activePawn.Dead is false && activePawn.Map == Find.CurrentMap;

        public static bool FreeCameraActive => activePawn != null && activePawn == ModCompatibility.GetPerspectiveShiftAvatar() && activePawn.Spawned && activePawn.Map == Find.CurrentMap;

        public static void Begin(Pawn pawn)
        {
            if (activePawn != null && activePawn != pawn)
            {
                Stop(activePawn);
            }
            activePawn = pawn;
            lastScopeCell = IntVec3.Invalid;
            ModCompatibility.ClearPerspectiveShiftCameraLock();
        }

        public static void End(Pawn pawn)
        {
            if (activePawn != pawn) return;
            Clear();
        }

        public static void Clear()
        {
            activePawn = null;
            lastScopeCell = IntVec3.Invalid;
            RestoreCameraDolly();
        }

        public static void EnableCameraDolly()
        {
            if (dollyOverridden) return;
            var config = Find.CameraDriver?.config;
            if (config == null || config.GetType().Name != "CameraMapConfig_Avatar") return;
            var normal = new CameraMapConfig_Normal();
            config.dollyRateKeys = normal.dollyRateKeys;
            config.dollyRateScreenEdge = normal.dollyRateScreenEdge;
            dollyOverridden = true;
        }

        public static void RestoreCameraDolly()
        {
            if (dollyOverridden is false) return;
            dollyOverridden = false;
            var config = Find.CameraDriver?.config;
            if (config == null) return;
            config.dollyRateKeys = 0f;
            config.dollyRateScreenEdge = 0f;
        }

        public static void Stop(Pawn pawn)
        {
            if (pawn.CurJobDef == DefsOf.SS_UseBinoculars)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            End(pawn);
        }

        public static bool IsActiveOn(Map map)
        {
            return activePawn != null && activePawn.Spawned && activePawn.Dead is false && activePawn.Map == map && Find.CurrentMap == map;
        }

        public static IntVec3 ScopeCenter(Map map)
        {
            var cell = Find.CameraDriver.MapPosition;
            return new IntVec3(Mathf.Clamp(cell.x, 0, map.Size.x - 1), 0, Mathf.Clamp(cell.z, 0, map.Size.z - 1));
        }

        public static bool ScopeMoved(Map map)
        {
            return IsActiveOn(map) && ScopeCenter(map) != lastScopeCell;
        }

        public static void ApplyFieldOfView(Map map, bool[] visibleCells)
        {
            if (IsActiveOn(map) is false) return;

            Array.Clear(visibleCells, 0, visibleCells.Length);

            var center = ScopeCenter(map);
            lastScopeCell = center;

            var origin = activePawn.Position;
            var mapX = map.Size.x;
            var mapZ = map.Size.z;
            var radius = Mathf.Max(1, ScopeRadius);
            var radiusSq = radius * radius;

            var zMin = Mathf.Max(center.z - radius, 0);
            var zMax = Mathf.Min(center.z + radius, mapZ - 1);
            var xMin = Mathf.Max(center.x - radius, 0);
            var xMax = Mathf.Min(center.x + radius, mapX - 1);

            for (var z = zMin; z <= zMax; z++)
            {
                var dz = z - center.z;
                var row = z * mapX;
                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = x - center.x;
                    if (dx * dx + dz * dz > radiusSq) continue;
                    var idx = row + x;
                    if (HasSightTo(origin, new IntVec3(x, 0, z), map))
                    {
                        visibleCells[idx] = true;
                    }
                }
            }
        }

        private static bool HasSightTo(IntVec3 origin, IntVec3 target, Map map)
        {
            foreach (var cell in GenSight.PointsOnLineOfSight(origin, target))
            {
                if (cell == origin || cell == target) continue;
                if (cell.CanBeSeenOver(map) is false) return false;
            }
            return true;
        }

        public static void DrawStopButton()
        {
            if (activePawn == null) return;
            if (activePawn.Spawned is false || activePawn.Dead || activePawn.CurJobDef != DefsOf.SS_UseBinoculars)
            {
                Clear();
                return;
            }

            const float btnW = 220f;
            const float btnH = 50f;
            const float pad = 10f;
            var panelRect = new Rect(UI.screenWidth / 2f - btnW / 2f - pad, UI.screenHeight - 150f - (btnH + pad * 2f), btnW + pad * 2f, btnH + pad * 2f);

            Find.WindowStack.ImmediateWindow(StopButtonId, panelRect, WindowLayer.GameUI, () =>
            {
                if (Widgets.ButtonText(new Rect(pad, pad, btnW, btnH), "SS_StopUsingBinoculars".Translate()))
                {
                    Stop(activePawn);
                }
            });
        }
    }
}
