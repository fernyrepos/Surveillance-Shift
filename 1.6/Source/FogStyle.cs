using UnityEngine;
using Verse;

namespace SurveillanceShift
{
    public static class FogStyle
    {
        public const float BlurMax = 1f;
        public const float ContrastMin = 0.4f;
        public const float ContrastMax = 2f;
        public const float SaturationMax = 1.5f;
        public const float BrightnessMax = 1.5f;

        public const bool DefaultHardcore = false;
        public const bool DefaultPawnsVisible = false;
        public const bool DefaultSelectInFog = false;
        public const bool DefaultHardcoreUndiscovered = true;
        public const float DefaultBlur = 0.75f;
        public const float DefaultContrast = 1.65f;
        public const float DefaultSaturation = 1.5f;
        public const float DefaultBrightness = 0.45f;

        public static bool HardcoreFog = DefaultHardcore;
        public static bool PawnsVisibleInFog = DefaultPawnsVisible;
        public static bool SelectInFog = DefaultSelectInFog;
        public static bool HardcoreUndiscovered = DefaultHardcoreUndiscovered;
        public static float Blur = DefaultBlur;
        public static float Contrast = DefaultContrast;
        public static float Saturation = DefaultSaturation;
        public static float Brightness = DefaultBrightness;

        public static int Version;

        public static float HardcoreLevel => Mathf.Clamp01(25f / 255f * Brightness / DefaultBrightness);

        public static bool UsesGrade => HardcoreFog is false && FogShader.Available;

        public static bool CanSelectInFog => UsesGrade && SelectInFog;

        public static bool UsesUndiscoveredFog => UsesGrade && HardcoreUndiscovered;

        public static bool HidesFoggedPawns => State.IsLimitedAwareness && UsesGrade && PawnsVisibleInFog is false;

        public static void Touch()
        {
            Version++;
        }

        public static void Reset()
        {
            HardcoreFog = DefaultHardcore;
            PawnsVisibleInFog = DefaultPawnsVisible;
            SelectInFog = DefaultSelectInFog;
            HardcoreUndiscovered = DefaultHardcoreUndiscovered;
            ResetSliders();
        }

        public static void ResetSliders()
        {
            Blur = DefaultBlur;
            Contrast = DefaultContrast;
            Saturation = DefaultSaturation;
            Brightness = DefaultBrightness;
            Touch();
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref HardcoreFog, "SS_FogHardcore", true);
            Scribe_Values.Look(ref PawnsVisibleInFog, "SS_FogPawnsVisible", DefaultPawnsVisible);
            Scribe_Values.Look(ref SelectInFog, "SS_FogSelectInFog", DefaultSelectInFog);
            Scribe_Values.Look(ref HardcoreUndiscovered, "SS_FogHardcoreUndiscovered", DefaultHardcoreUndiscovered);
            Scribe_Values.Look(ref Blur, "SS_FogBlur", DefaultBlur);
            Scribe_Values.Look(ref Contrast, "SS_FogContrast", DefaultContrast);
            Scribe_Values.Look(ref Saturation, "SS_FogSaturation", DefaultSaturation);
            Scribe_Values.Look(ref Brightness, "SS_FogBrightness", DefaultBrightness);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Touch();
            }
        }
    }
}
