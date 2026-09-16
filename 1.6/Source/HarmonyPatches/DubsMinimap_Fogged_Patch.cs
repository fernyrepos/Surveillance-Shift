using System.Reflection;
using HarmonyLib;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class DubsMinimap_Fogged_Patch
    {
        public static bool Prepare() => ModsConfig.IsActive("dubwise.dubsmintminimap");

        public static MethodBase TargetMethod() => AccessTools.Method("DubsMintMinimap.MainTabWindow_MiniMap:Fogged", new[] { typeof(Thing) });

        public static bool Prefix(Thing thing, ref bool __result)
        {
            if (State.IsLimitedAwareness is false || thing.Map.IsVisible(thing.Position)) return true;
            __result = true;
            return false;
        }
    }
}
