using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        public static readonly bool PerspectiveShiftAvailable;
        public static readonly bool CAIAvailable;

        private static FieldInfo psStateAvatarField;
        private static FieldInfo psStateCameraLockPositionField;
        private static FieldInfo psStateCurrentModeField;
        private static FieldInfo psAvatarPawnField;
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
