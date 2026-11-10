using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        public static readonly bool PerspectiveShiftAvailable;
        public static readonly bool CAIAvailable;
        public static readonly bool VehicleFrameworkAvailable;
        public static readonly bool AsAboveSoBelowAvailable;
        public static readonly bool ThemingModAvailable;

        private static readonly HashSet<ThingDef> SeeThroughWallDefs = new();

        private static FieldInfo psStateAvatarField;
        private static FieldInfo psStateCameraLockPositionField;
        private static FieldInfo psStateCurrentModeField;
        private static FieldInfo psAvatarPawnField;
        private static FieldInfo psAvatarLeanTargetField;
        private static MethodInfo bandsBandedMethod;
        private static MethodInfo bandsCountMethod;
        private static MethodInfo bandsSurfaceMethod;
        private static MethodInfo bandsRectMethod;
        private static Type vehiclePawnType;
        private static PropertyInfo vehiclePassengersProperty;
        private static FieldInfo caiSettingsField;
        private static FieldInfo caiFogEnabledField;

        private static int lastCacheFrame = -1;
        private static Pawn cachedAvatar;
        private static bool cachedHasAvatar;
        private static bool cachedIsAuthentic;

        static ModCompatibility()
        {
            ThemingModAvailable = ModsConfig.IsActive("ferny.themingformodpack");
            PerspectiveShiftAvailable = ModsConfig.IsActive("ferny.PerspectiveShift");
            if (PerspectiveShiftAvailable)
            {
                var stateType = AccessTools.TypeByName("PerspectiveShift.State");
                var avatarType = AccessTools.TypeByName("PerspectiveShift.Avatar");
                psStateAvatarField = AccessTools.Field(stateType, "Avatar");
                psStateCameraLockPositionField = AccessTools.Field(stateType, "CameraLockPosition");
                psStateCurrentModeField = AccessTools.Field(stateType, "CurrentMode");
                psAvatarPawnField = AccessTools.Field(avatarType, "pawn");
                psAvatarLeanTargetField = AccessTools.Field(avatarType, "LeanTarget");
            }

            VehicleFrameworkAvailable = ModsConfig.IsActive("SmashPhil.VehicleFramework");
            if (VehicleFrameworkAvailable)
            {
                vehiclePawnType = AccessTools.TypeByName("Vehicles.VehiclePawn");
                vehiclePassengersProperty = vehiclePawnType == null ? null : AccessTools.Property(vehiclePawnType, "AllPawnsAboard");
                VehicleFrameworkAvailable = vehiclePassengersProperty != null;
            }

            AsAboveSoBelowAvailable = ModsConfig.IsActive("astryl.AsAboveSoBelow2");
            if (AsAboveSoBelowAvailable)
            {
                var bandsType = AccessTools.TypeByName("AsAboveSoBelow.ABBands");
                bandsBandedMethod = AccessTools.Method(bandsType, "Banded", new[] { typeof(Map) });
                bandsCountMethod = AccessTools.Method(bandsType, "BandCount", new[] { typeof(Map) });
                bandsSurfaceMethod = AccessTools.Method(bandsType, "SurfaceBand", new[] { typeof(Map) });
                bandsRectMethod = AccessTools.Method(bandsType, "RectOfBand", new[] { typeof(Map), typeof(int) });
                AsAboveSoBelowAvailable = bandsBandedMethod != null && bandsCountMethod != null && bandsSurfaceMethod != null && bandsRectMethod != null;
            }

            CAIAvailable = ModsConfig.IsActive("Krkr.rule56");
            if (CAIAvailable)
            {
                var finderType = AccessTools.TypeByName("CombatAI.Finder");
                caiSettingsField = AccessTools.Field(finderType, "Settings");

                var caiSettingsType = AccessTools.TypeByName("CombatAI.Settings");
                caiFogEnabledField = AccessTools.Field(caiSettingsType, "FogOfWar_Enabled");

                var exposeDataMethod = AccessTools.Method(caiSettingsType, "ExposeData");
                var lockdownMethod = AccessTools.Method(typeof(ModCompatibility), nameof(CAISettings_ExposeData_Postfix));
                SurveillanceShiftMod.harmony.Patch(exposeDataMethod, postfix: new HarmonyMethod(lockdownMethod));

                DisableCAIFog(caiSettingsField.GetValue(null));
            }
        }

        private static bool seeThroughBuilt;

        private static void BuildSeeThroughWallDefs()
        {
            var rebuildGlassCompType = AccessTools.TypeByName("ReBuildDoorsAndCorners.CompProperties_GlassWall");
            var stevesWallsExtType = AccessTools.TypeByName("StevesWalls.GlassWallsExtension");
            if (rebuildGlassCompType == null && stevesWallsExtType == null) return;

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (rebuildGlassCompType != null && def.comps != null && def.comps.Exists(c => rebuildGlassCompType.IsInstanceOfType(c)))
                {
                    SeeThroughWallDefs.Add(def);
                    continue;
                }
                if (stevesWallsExtType != null && def.modExtensions != null && def.modExtensions.Exists(e => stevesWallsExtType.IsInstanceOfType(e)))
                {
                    SeeThroughWallDefs.Add(def);
                }
            }
            Log.Message($"[SurveillanceShift] Found {SeeThroughWallDefs.Count} see-through wall defs: {string.Join(", ", SeeThroughWallDefs.Select(d => d.defName))}");
        }

        public static bool IsSeeThroughWall(Building ed)
        {
            if (seeThroughBuilt is false)
            {
                seeThroughBuilt = true;
                BuildSeeThroughWallDefs();
            }
            return ed != null && SeeThroughWallDefs.Contains(ed.def);
        }

        public static void CAISettings_ExposeData_Postfix(object __instance)
        {
            DisableCAIFog(__instance);
        }

        private static void DisableCAIFog(object settingsInstance)
        {
            if (settingsInstance != null)
            {
                caiFogEnabledField.SetValue(settingsInstance, false);
            }
        }

        public static void ResetCache()
        {
            lastCacheFrame = -1;
            cachedAvatar = null;
            cachedHasAvatar = false;
            cachedIsAuthentic = false;
        }

        private static void EnsureFrameCache()
        {
            var curFrame = Time.frameCount;
            if (curFrame == lastCacheFrame) return;
            lastCacheFrame = curFrame;

            if (PerspectiveShiftAvailable is false) return;
            var avatarObj = psStateAvatarField.GetValue(null);
            cachedHasAvatar = avatarObj != null;
            cachedAvatar = cachedHasAvatar ? (Pawn)psAvatarPawnField.GetValue(avatarObj) : null;
            var modeObj = psStateCurrentModeField.GetValue(null);
            cachedIsAuthentic = modeObj != null && (int)modeObj == 1;
        }

        public static void ShareDiscoveredWithUpperLevels(Map map, bool[] discovered)
        {
            if (AsAboveSoBelowAvailable is false || (bool)bandsBandedMethod.Invoke(null, new object[] { map }) is false) return;
            var surface = (int)bandsSurfaceMethod.Invoke(null, new object[] { map });
            var count = (int)bandsCountMethod.Invoke(null, new object[] { map });
            openAirTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("AB_OpenAir");
            wallTopTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("AB_WallTop");
            var surfaceRect = (CellRect)bandsRectMethod.Invoke(null, new object[] { map, surface });
            var sizeX = map.Size.x;
            for (var band = surface + 1; band < count; band++)
            {
                var rect = (CellRect)bandsRectMethod.Invoke(null, new object[] { map, band });
                var offset = rect.minZ - surfaceRect.minZ;
                var rows = Mathf.Min(rect.Height, surfaceRect.Height);
                for (var dz = 0; dz < rows; dz++)
                {
                    var from = (surfaceRect.minZ + dz) * sizeX;
                    var to = (surfaceRect.minZ + dz + offset) * sizeX;
                    for (var x = rect.minX; x <= rect.maxX; x++)
                    {
                        if (discovered[from + x] is false) continue;
                        var terrain = map.terrainGrid.TerrainAt(to + x);
                        if (terrain != null && (terrain == openAirTerrain || terrain == wallTopTerrain)) discovered[to + x] = true;
                    }
                }
            }
        }

        private static TerrainDef openAirTerrain;
        private static TerrainDef wallTopTerrain;

        private static readonly Dictionary<int, CellRect[]> bandRects = new Dictionary<int, CellRect[]>();

        public static CellRect SightBounds(Map map, IntVec3 origin)
        {
            if (AsAboveSoBelowAvailable is false || (bool)bandsBandedMethod.Invoke(null, new object[] { map }) is false) return CellRect.WholeMap(map);
            var count = (int)bandsCountMethod.Invoke(null, new object[] { map });
            if (bandRects.TryGetValue(map.uniqueID, out var rects) is false || rects.Length != count)
            {
                rects = new CellRect[count];
                for (var band = 0; band < count; band++)
                {
                    rects[band] = (CellRect)bandsRectMethod.Invoke(null, new object[] { map, band });
                }
                bandRects[map.uniqueID] = rects;
            }
            foreach (var rect in rects)
            {
                if (rect.Contains(origin)) return rect;
            }
            return CellRect.WholeMap(map);
        }

        private static bool HasUpperLevelThing(Map map, int index)
        {
            var things = map.thingGrid.ThingsListAtFast(index);
            for (var i = 0; i < things.Count; i++)
            {
                var category = things[i].def.category;
                if (category == ThingCategory.Building || category == ThingCategory.Item) return true;
            }
            return false;
        }

        public static bool MergeLowerLevelVision(Map map, bool[] visible, ref bool[] view)
        {
            if (AsAboveSoBelowAvailable is false || IsPerspectiveShiftAuthentic() || (bool)bandsBandedMethod.Invoke(null, new object[] { map }) is false) return false;
            if (view == null || view.Length != visible.Length)
            {
                view = new bool[visible.Length];
            }
            Array.Copy(visible, view, visible.Length);
            var count = (int)bandsCountMethod.Invoke(null, new object[] { map });
            var sizeX = map.Size.x;
            openAirTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("AB_OpenAir");
            wallTopTerrain ??= DefDatabase<TerrainDef>.GetNamedSilentFail("AB_WallTop");
            var first = (CellRect)bandsRectMethod.Invoke(null, new object[] { map, 0 });
            var rows = first.Height;
            var below = new bool[rows * sizeX];
            for (var band = 0; band < count; band++)
            {
                var rect = (CellRect)bandsRectMethod.Invoke(null, new object[] { map, band });
                var offset = rect.minZ - first.minZ;
                var bandRows = Mathf.Min(rect.Height, rows);
                for (var dz = 0; dz < bandRows; dz++)
                {
                    var cellRow = (first.minZ + dz + offset) * sizeX;
                    var belowRow = dz * sizeX;
                    for (var x = rect.minX; x <= rect.maxX; x++)
                    {
                        var terrain = map.terrainGrid.TerrainAt(cellRow + x);
                        if (terrain != null && (terrain == openAirTerrain || terrain == wallTopTerrain))
                        {
                            if (below[belowRow + x] && HasUpperLevelThing(map, cellRow + x) is false) view[cellRow + x] = true;
                        }
                        else
                        {
                            below[belowRow + x] = visible[cellRow + x];
                        }
                    }
                }
            }
            return true;
        }

        public static int VehicleSightRadius(Pawn pawn)
        {
            if (VehicleFrameworkAvailable is false || vehiclePawnType.IsInstanceOfType(pawn) is false) return 0;
            var aboard = vehiclePassengersProperty.GetValue(pawn) as List<Pawn>;
            var radius = 0;
            if (aboard != null)
            {
                foreach (var passenger in aboard)
                {
                    if (passenger.Dead || passenger.IsColonist is false) continue;
                    radius = Mathf.Max(radius, passenger.SightRadius());
                }
            }
            return radius;
        }

        public static bool TryGetAvatarLeanCell(Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (PerspectiveShiftAvailable is false || psAvatarLeanTargetField == null) return false;
            if (pawn == null || pawn != GetPerspectiveShiftAvatar()) return false;
            var avatarObj = psStateAvatarField.GetValue(null);
            if (avatarObj == null || psAvatarLeanTargetField.GetValue(avatarObj) is not Vector3 lean) return false;
            if (lean.sqrMagnitude < 0.01f) return false;
            var candidate = pawn.Position + new IntVec3(Mathf.RoundToInt(lean.x), 0, Mathf.RoundToInt(lean.z));
            if (candidate == pawn.Position || candidate.InBounds(pawn.Map) is false) return false;
            cell = candidate;
            return true;
        }

        public static Pawn GetPerspectiveShiftAvatar()
        {
            EnsureFrameCache();
            return cachedAvatar;
        }

        public static bool HasPerspectiveShiftAvatar()
        {
            EnsureFrameCache();
            return cachedHasAvatar;
        }

        public static void ClearPerspectiveShiftCameraLock()
        {
            if (PerspectiveShiftAvailable is false) return;
            psStateCameraLockPositionField.SetValue(null, null);
        }

        public static void SetPerspectiveShiftCameraLock(Vector3 pos)
        {
            if (PerspectiveShiftAvailable is false) return;
            psStateCameraLockPositionField.SetValue(null, pos);
        }

        public static bool HasPerspectiveShiftCameraLock()
        {
            if (PerspectiveShiftAvailable is false) return false;
            return psStateCameraLockPositionField.GetValue(null) as Vector3? != null;
        }

        public static bool IsPerspectiveShiftAuthentic()
        {
            EnsureFrameCache();
            return cachedIsAuthentic && cachedHasAvatar;
        }

        public static bool IsPerspectiveShiftCameraLockedTo(IntVec3 pos)
        {
            EnsureFrameCache();
            if (PerspectiveShiftAvailable is false) return false;
            var lockPos = psStateCameraLockPositionField.GetValue(null) as Vector3?;
            return lockPos.HasValue && lockPos.Value.ToIntVec3() == pos;
        }
    }
}
