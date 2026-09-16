using PipeSystem;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public static class State
    {
        public static AwarenessMode AwarenessMode = AwarenessMode.AreaOmniscience;

        public static Building_Camera ActiveFeedCamera;
        public static Map ActiveFeedMap;
        public static PipeNet ActiveFeedPipeNet;

        public static bool IsLimitedAwareness => AwarenessMode == AwarenessMode.LimitedAwareness;

        private static Map cachedMap;
        private static MapComponent_FogOfWar cachedComp;

        public static bool IsVisible(this Map map, IntVec3 cell)
        {
            if (IsLimitedAwareness is false || map is null) return true;
            if (map != cachedMap)
            {
                cachedComp = map.GetComponent<MapComponent_FogOfWar>();
                cachedMap = map;
            }
            return cachedComp.IsVisible(cell);
        }

        public static GlobalTargetInfo ResolveTarget(GlobalTargetInfo target)
        {
            if (target.Pawn is { Dead: true } pawn && pawn.Corpse is Corpse corpse)
            {
                return new GlobalTargetInfo(corpse);
            }
            return target;
        }

        public static bool IsTargetFogged(GlobalTargetInfo target)
        {
            target = ResolveTarget(target);
            if (target.Map is null) return false;
            return target.Map.IsVisible(target.Cell) is false;
        }

        public static int MutedSlowdownTick = -1;

        public static void MuteSlowdown() => MutedSlowdownTick = Find.TickManager.TicksGame;

        public static bool IsSlowdownMuted => Find.TickManager.TicksGame == MutedSlowdownTick;

        public static void SpawnPing(IntVec3 cell, Map map, bool hostile)
        {
            var def = hostile ? DefsOf.SS_Mote_PingHostile : DefsOf.SS_Mote_PingFriendly;
            var mote = (Mote_Ping)ThingMaker.MakeThing(def);
            mote.exactPosition = cell.ToVector3Shifted();
            GenSpawn.Spawn(mote, cell, map);

            var sound = hostile ? DefsOf.SS_Sound_PingHostile : DefsOf.SS_Sound_PingFriendly;
            sound.PlayOneShotOnCamera(map);
        }

        public static void ClearCaches()
        {
            cachedMap = null;
            cachedComp = null;
            ClearActiveFeed();
        }

        public static bool SuppressCameraLock(IntVec3 cell)
        {
            if (IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing || WorldComponent_GravshipController.CutsceneInProgress || ModCompatibility.HasPerspectiveShiftAvatar() is false) return false;
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (avatar is null || avatar.Map != Find.CurrentMap || cell == avatar.Position) return false;
            SpawnPing(cell, avatar.Map, false);
            return true;
        }

        public static void ClearActiveFeed()
        {
            ActiveFeedCamera = null;
            ActiveFeedMap = null;
            ActiveFeedPipeNet = null;
        }

        public static void DrawFeedReturnButton()
        {
            if (ActiveFeedCamera == null || ActiveFeedCamera.Spawned is false)
            {
                ClearActiveFeed();
            }
            else if (ModCompatibility.HasPerspectiveShiftAvatar() && ModCompatibility.IsPerspectiveShiftCameraLockedTo(ActiveFeedCamera.Position) is false)
            {
                ClearActiveFeed();
            }
            else
            {
                var btnW = 220f;
                var panelH = 70f;
                var panelY = UI.screenHeight - 150f - panelH;
                var panelRect = new Rect(UI.screenWidth / 2f - btnW / 2f - 10f, panelY, btnW + 20f, panelH);
                Widgets.DrawWindowBackground(panelRect);
                var btnRect = new Rect(panelRect.xMin + 10f, panelRect.yMin + 10f, btnW, 50f);
                if (Widgets.ButtonText(btnRect, "SS_StopViewing".Translate()))
                {
                    var map = ActiveFeedMap;
                    var net = ActiveFeedPipeNet;
                    ClearActiveFeed();
                    ModCompatibility.ClearPerspectiveShiftCameraLock();
                    Find.WindowStack.Add(new Dialog_SurveillanceCameras(map, net));
                }
            }
        }
    }
}
