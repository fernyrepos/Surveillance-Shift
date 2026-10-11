using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public enum ScopeKind
    {
        Binoculars,
        Telescope
    }

    [HotSwappable]
    public static class ScopeView
    {
        public static int ScopeRadius = 4;
        public static int BinocularOffset = 5;

        private static Pawn activePawn;
        private static ScopeKind activeKind;
        private static JobDef activeJobDef;
        private static IntVec3 lastScopeCell = IntVec3.Invalid;
        private static bool dollyOverridden;

        private const int StopButtonId = 74212093;

        public static Pawn ActivePawn => activePawn;

        public static bool IsActive(Pawn pawn) => pawn != null && activePawn == pawn;

        public static bool IsActiveKind(Pawn pawn, ScopeKind kind) => IsActive(pawn) && activeKind == kind;

        public static bool BlocksWorldInteraction => activePawn != null && activePawn.Spawned && activePawn.Dead is false && activePawn.Map == Find.CurrentMap;

        public static bool FreeCameraActive => activePawn != null && activePawn == ModCompatibility.GetPerspectiveShiftAvatar() && activePawn.Spawned && activePawn.Map == Find.CurrentMap;

        public static void Begin(Pawn pawn, ScopeKind kind, JobDef jobDef)
        {
            if (activePawn != null && activePawn != pawn)
            {
                Stop(activePawn);
            }
            activePawn = pawn;
            activeKind = kind;
            activeJobDef = jobDef;
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
            activeJobDef = null;
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
            if (activePawn == pawn && activeJobDef != null && pawn.CurJobDef == activeJobDef)
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
            var radius = Mathf.Max(1, ScopeRadius);

            if (activeKind == ScopeKind.Binoculars)
            {
                var offset = Mathf.Max(0, BinocularOffset);
                ApplyCircle(map, origin, new IntVec3(center.x - offset, 0, center.z), radius, visibleCells);
                ApplyCircle(map, origin, new IntVec3(center.x + offset, 0, center.z), radius, visibleCells);
            }
            else
            {
                ApplyCircle(map, origin, center, radius, visibleCells);
            }
        }

        private static void ApplyCircle(Map map, IntVec3 origin, IntVec3 center, int radius, bool[] visibleCells)
        {
            var mapX = map.Size.x;
            var mapZ = map.Size.z;
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
                    var idx = row + x;
                    if (visibleCells[idx]) continue;
                    var dx = x - center.x;
                    if (dx * dx + dz * dz > radiusSq) continue;
                    if (HasSightTo(origin, new IntVec3(x, 0, z), map))
                    {
                        visibleCells[idx] = true;
                    }
                }
            }
        }

        public static Vector3 MoteOffsetFor(Rot4 rotation)
        {
            var facing = rotation.FacingCell;
            return new Vector3(facing.x, 0f, facing.z) * 0.65f;
        }

        public static ThingDef RequiredItemFor(ThingDef moteDef)
        {
            if (moteDef == DefsOf.SS_Mote_Binoculars) return DefsOf.SS_Binoculars;
            if (moteDef == DefsOf.SS_Mote_Spyglass) return DefsOf.SS_Telescope;
            return null;
        }

        private static bool HasSightTo(IntVec3 origin, IntVec3 target, Map map)
        {
            foreach (var cell in GenSight.PointsOnLineOfSight(origin, target))
            {
                if (cell == origin || cell == target) continue;
                var ed = cell.GetEdifice(map);
                if (ed != null && ModCompatibility.IsSeeThroughWall(ed)) continue;
                if (cell.CanBeSeenOver(map) is false) return false;
            }
            return true;
        }

        public static void DrawStopButton()
        {
            if (activePawn == null) return;
            if (activePawn.Spawned is false || activePawn.Dead || activeJobDef == null || activePawn.CurJobDef != activeJobDef)
            {
                Clear();
                return;
            }

            var labelKey = activeKind == ScopeKind.Telescope ? "SS_StopUsingTelescope" : "SS_StopUsingBinoculars";

            const float btnW = 220f;
            const float btnH = 50f;
            const float pad = 10f;
            var panelRect = new Rect(UI.screenWidth / 2f - btnW / 2f - pad, UI.screenHeight - 150f - (btnH + pad * 2f), btnW + pad * 2f, btnH + pad * 2f);

            Find.WindowStack.ImmediateWindow(StopButtonId, panelRect, WindowLayer.GameUI, () =>
            {
                if (Widgets.ButtonText(new Rect(pad, pad, btnW, btnH), labelKey.Translate()))
                {
                    Stop(activePawn);
                }
            });
        }
    }
}
