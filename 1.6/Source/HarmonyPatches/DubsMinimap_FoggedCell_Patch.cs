using System.Reflection;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class DubsMinimap_FoggedCell_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("dubwise.dubsmintminimap");

        public static MethodBase TargetMethod() => AccessTools.Method("DubsMintMinimap.MainTabWindow_MiniMap:Fogged", new[] { typeof(IntVec3), typeof(Map) });

        public static bool Prefix(IntVec3 c, Map map, ref bool __result)
        {
            if (State.IsLimitedAwareness is false || map.IsVisible(c)) return true;
            __result = true;
            return false;
        }
    }
}
