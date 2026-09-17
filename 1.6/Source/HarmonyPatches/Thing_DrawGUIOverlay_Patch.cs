using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class Thing_DrawGUIOverlay_Patch
    {
        static Thing_DrawGUIOverlay_Patch()
        {
            var prefix = AccessTools.Method(typeof(Thing_DrawGUIOverlay_Patch), nameof(Prefix));
            foreach (var method in TargetMethods())
            {
                SurveillanceShiftMod.harmony.Patch(method, prefix: new HarmonyMethod(prefix));
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Thing), nameof(Thing.DrawGUIOverlay));
            foreach (var type in typeof(Thing).AllSubclasses())
            {
                if (typeof(Pawn).IsAssignableFrom(type)) continue;
                var method = AccessTools.DeclaredMethod(type, nameof(Thing.DrawGUIOverlay));
                if (method != null) yield return method;
            }
        }
        public static bool Prefix(Thing __instance)
        {
            if (State.IsLimitedAwareness is false) return true;

            var map = __instance.Map;
            if (map is null) return true;
            return map.IsVisible(__instance.Position);
        }
    }
}
