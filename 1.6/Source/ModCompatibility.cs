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

        private static readonly HashSet<ThingDef> SeeThroughWallDefs = new();

        private static FieldInfo psStateAvatarField;
        private static FieldInfo psStateCameraLockPositionField;
        private static FieldInfo psStateCurrentModeField;
        private static FieldInfo psAvatarPawnField;
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
            PerspectiveShiftAvailable = ModsConfig.IsActive("ferny.PerspectiveShift");
            if (PerspectiveShiftAvailable)
            {
                var stateType = AccessTools.TypeByName("PerspectiveShift.State");
                var avatarType = AccessTools.TypeByName("PerspectiveShift.Avatar");
                psStateAvatarField = AccessTools.Field(stateType, "Avatar");
                psStateCameraLockPositionField = AccessTools.Field(stateType, "CameraLockPosition");
                psStateCurrentModeField = AccessTools.Field(stateType, "CurrentMode");
                psAvatarPawnField = AccessTools.Field(avatarType, "pawn");
            }

            VehicleFrameworkAvailable = ModsConfig.IsActive("SmashPhil.VehicleFramework");
            if (VehicleFrameworkAvailable)
            {
                vehiclePawnType = AccessTools.TypeByName("Vehicles.VehiclePawn");
                vehiclePassengersProperty = vehiclePawnType == null ? null : AccessTools.Property(vehiclePawnType, "AllPawnsAboard");
                VehicleFrameworkAvailable = vehiclePassengersProperty != null;
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
