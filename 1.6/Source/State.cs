using System.Collections.Generic;
using PipeSystem;
using RimWorld;
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

        public static void SpawnPing(IntVec3 cell, Map map, bool hostile)
        {
            var def = hostile ? DefsOf.SS_Mote_PingHostile : DefsOf.SS_Mote_PingFriendly;
            var mote = (Mote_Ping)ThingMaker.MakeThing(def);
            mote.exactPosition = cell.ToVector3Shifted();
            GenSpawn.Spawn(mote, cell, map);

            var sound = hostile ? DefsOf.SS_Sound_PingHostile : DefsOf.SS_Sound_PingFriendly;
            sound.PlayOneShotOnCamera(map);
        }

        public static bool SuppressMuting;

        public static void ReceiveLetterUnmuted(Letter letter)
        {
            SuppressMuting = true;
            try
            {
                Find.LetterStack.ReceiveLetter(letter);
            }
            finally
            {
                SuppressMuting = false;
            }
        }

        public static void ClearCaches()
        {
            cachedMap = null;
            cachedComp = null;
            ClearActiveFeed();
            pendingFactionDebts.Clear();
            retaliationMultipliers.Clear();
            factionKnownWealth.Clear();
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

        public static bool ApplyingPendingDebt;
        private const int RetaliationMultiplierStep = 2;
        private static Dictionary<int, int> pendingFactionDebts = new();
        private static Dictionary<int, int> retaliationMultipliers = new();
        private static Dictionary<int, float> factionKnownWealth = new();

        public static void AddDebt(Faction faction, int debt)
        {
            if (faction.IsPlayer) return;
            var id = faction.loadID;
            pendingFactionDebts.TryGetValue(id, out var current);
            pendingFactionDebts[id] = current + debt;
        }

        public static int GetDebt(Faction faction)
        {
            pendingFactionDebts.TryGetValue(faction.loadID, out var debt);
            return debt;
        }

        public static void ClearDebt(Faction faction)
        {
            pendingFactionDebts.Remove(faction.loadID);
        }

        public static void ApplyDebt(Faction faction, int extraChange = 0)
        {
            var debt = GetDebt(faction);
            ClearDebt(faction);
            var totalChange = debt + extraChange;
            if (totalChange != 0)
            {
                ApplyingPendingDebt = true;
                try
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, totalChange);
                }
                finally
                {
                    ApplyingPendingDebt = false;
                }
            }
        }

        public static int GetRetaliationMultiplier(Faction faction)
        {
            retaliationMultipliers.TryGetValue(faction.loadID, out var mult);
            return mult <= 0 ? 1 : mult;
        }

        public static void IncrementRetaliation(Faction faction)
        {
            retaliationMultipliers[faction.loadID] = GetRetaliationMultiplier(faction) * RetaliationMultiplierStep;
        }

        public static void ResetRetaliation(Faction faction) => retaliationMultipliers.Remove(faction.loadID);

        public static void SetKnownWealth(Faction faction, float wealth) => factionKnownWealth[faction.loadID] = wealth;

        public static bool TryGetKnownWealth(Faction faction, out float wealth)
        {
            return factionKnownWealth.TryGetValue(faction.loadID, out wealth);
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref AwarenessMode, "SS_AwarenessMode", AwarenessMode.AreaOmniscience);
            Scribe_Collections.Look(ref pendingFactionDebts, "SS_pendingFactionDebts", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref retaliationMultipliers, "SS_retaliationMultipliers", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref factionKnownWealth, "SS_factionKnownWealth", LookMode.Value, LookMode.Value);
            pendingFactionDebts ??= new Dictionary<int, int>();
            retaliationMultipliers ??= new Dictionary<int, int>();
            factionKnownWealth ??= new Dictionary<int, float>();
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
