using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [HarmonyPatch]
    public static class DubsMinimap_Radar_Patch
    {
        private const int CountBallHoldValue = 100;
        private static FieldInfo radarOffField;
        private static FieldInfo countballsField;
        private static bool lastRadarOn;
        private const int RadarCheckIntervalTicks = 30;
        private static Map cachedRadarMap;
        private static int nextRadarCheckTick;
        private static bool cachedHasRadar;

        public static bool Prepare() => ModsConfig.IsActive("dubwise.dubsmintminimap");

        public static void ResetCache()
        {
            cachedRadarMap = null;
            cachedHasRadar = false;
            lastRadarOn = false;
            nextRadarCheckTick = 0;
        }

        public static MethodBase TargetMethod()
        {
            radarOffField = AccessTools.Field("DubsMintMinimap.MainTabWindow_MiniMap:radarOff");
            countballsField = AccessTools.Field("DubsMintMinimap.MainTabWindow_MiniMap:countballs");
            return AccessTools.Method("DubsMintMinimap.MainTabWindow_MiniMap:DoWindowContents");
        }

        public static void Prefix(object __instance)
        {
            if (State.IsLimitedAwareness is false) return;
            var map = Find.CurrentMap;
            if (map != cachedRadarMap || Find.TickManager.TicksGame >= nextRadarCheckTick)
            {
                cachedHasRadar = Building_ColonyRadar.HasActiveRadar(map);
                cachedRadarMap = map;
                nextRadarCheckTick = Find.TickManager.TicksGame + RadarCheckIntervalTicks;
            }
            if (cachedHasRadar != lastRadarOn)
            {
                (cachedHasRadar ? DefsOf.Sound_MapBoot : DefsOf.Sound_MapFail).PlayOneShotOnCamera();
                lastRadarOn = cachedHasRadar;
            }
            radarOffField.SetValue(__instance, cachedHasRadar is false);
            countballsField.SetValue(__instance, CountBallHoldValue);
        }
    }
}
