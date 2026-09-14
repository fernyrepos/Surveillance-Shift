using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class State
    {
        public static AwarenessMode AwarenessMode = AwarenessMode.AreaOmniscience;

        public static bool IsLimitedAwareness => AwarenessMode == AwarenessMode.LimitedAwareness;

        private static Map cachedMap;
        private static MapComponent_FogOfWar cachedComp;

        public static bool IsVisible(this Map map, IntVec3 cell)
        {
            if (IsLimitedAwareness is false) return true;
            if (map != cachedMap)
            {
                cachedComp = map.GetComponent<MapComponent_FogOfWar>();
                cachedMap = map;
            }
            return cachedComp.IsVisible(cell);
        }

        public static void SpawnPing(IntVec3 cell, Map map, bool hostile)
        {
            var def = hostile ? DefsOf.SS_Mote_PingHostile : DefsOf.SS_Mote_PingFriendly;
            var mote = (Mote_Ping)ThingMaker.MakeThing(def);
            mote.exactPosition = cell.ToVector3Shifted();
            GenSpawn.Spawn(mote, cell, map);

            var sound = hostile ? DefsOf.SS_Sound_PingHostile : DefsOf.SS_Sound_PingFriendly;
            sound.PlayOneShotOnCamera(map);
        }

        public static bool SuppressCameraLock(IntVec3 cell)
        {
            if (IsLimitedAwareness is false || ModCompatibility.HasPerspectiveShiftAvatar() is false) return false;
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar is null || avatar.Map != Find.CurrentMap || cell == avatar.Position) return false;
            SpawnPing(cell, avatar.Map, false);
            return true;
        }
    }
}
