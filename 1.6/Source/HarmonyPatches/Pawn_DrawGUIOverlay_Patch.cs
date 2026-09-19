using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DrawGUIOverlay))]
    public static class Pawn_DrawGUIOverlay_Patch
    {
        private static Material magnifierMat;
        private static Material MagnifierMat => magnifierMat ??= MaterialPool.MatFrom("UI/ScoutMagnifyingGlass", ShaderDatabase.MetaOverlay);
        private const float OverlayAltitudeOffset = 0.21951221f;
        private const float PhaseSeed = 397f;
        private const float PulseFrequency = 4f;
        private static readonly List<Pawn> queuedScouts = new();

        public static bool Prefix(Pawn __instance)
        {
            if (State.IsLimitedAwareness is false || __instance == ModCompatibility.GetPerspectiveShiftAvatar()) return true;

            var map = __instance.Map ?? __instance.Corpse?.Map;
            if (map is null) return true;
            return map.IsVisible(__instance.Corpse?.Position ?? __instance.Position);
        }

        public static void Postfix(Pawn __instance)
        {
            var lord = __instance.GetLord();
            if (lord?.LordJob is not LordJob_FriendlyScouts job || job.negotiated || lord.ownedPawns.Count == 0 || job.Negotiator != __instance) return;
            queuedScouts.Add(__instance);
        }

        public static void RenderQueued()
        {
            if (queuedScouts.Count == 0) return;
            foreach (var pawn in queuedScouts)
            {
                if (pawn.Spawned is false) continue;
                var drawPos = pawn.DrawPos;
                drawPos.x += pawn.def.size.x - 0.52f;
                drawPos.z += pawn.def.size.z - 0.45f;
                drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + OverlayAltitudeOffset;

                var pulse = (Mathf.Sin((Time.realtimeSinceStartup + PhaseSeed * (pawn.thingIDNumber % 571)) * PulseFrequency) + 1f) * 0.5f;
                pulse = 0.3f + pulse * 0.7f;
                var mat = FadedMaterialPool.FadedVersionOf(MagnifierMat, pulse);
                Graphics.DrawMesh(MeshPool.plane05, Matrix4x4.TRS(drawPos, Quaternion.identity, Vector3.one), mat, 0);
            }
            queuedScouts.Clear();
        }
    }
}
