using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using PipeSystem;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
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

        public static bool AlertsActive => SurveillanceShiftMod.settings.enableAlertInDirector || ModCompatibility.PerspectiveShiftAvailable is false || ModCompatibility.HasPerspectiveShiftAvatar();

        private const float MinSightRadius = 1.5f;
        private const int DownedSightRadius = 4;

        public static int SightRadius(this Pawn pawn)
        {
            if (pawn.Downed) return Mathf.RoundToInt(Mathf.Clamp(DownedSightRadius * pawn.SightFactor(), MinSightRadius, DownedSightRadius));
            return Mathf.RoundToInt(Mathf.Max(MinSightRadius, SurveillanceShiftMod.settings.fogViewDistance * pawn.SightFactor()));
        }

        public static float SightFactor(this Pawn pawn)
        {
            return pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
        }

        private static Map cachedMap;
        private static MapComponent_FogOfWar cachedComp;

        private static readonly ConditionalWeakTable<Pawn, AnimalSurveillanceData> surveillanceData = new();

        public static AnimalSurveillanceData GetSurveillanceData(this Pawn pawn)
        {
            return surveillanceData.GetValue(pawn, p => new AnimalSurveillanceData());
        }

        private static readonly ConditionalWeakTable<Pawn, ColonistSurveillanceData> colonistSurveillanceData = new();

        public static ColonistSurveillanceData GetColonistSurveillanceData(this Pawn pawn)
        {
            return colonistSurveillanceData.GetValue(pawn, p => new ColonistSurveillanceData());
        }

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
            ScopeView.Clear();
            pendingFactionDebts.Clear();
            retaliationMultipliers.Clear();
            factionKnownWealth.Clear();
            ModCompatibility.ResetCache();
            DubsMinimap_Radar_Patch.ResetCache();
        }

        public static bool SuppressCameraLock(IntVec3 cell)
        {
            if (SurveillanceShiftMod.settings.limitViewingMaps is false || IsLimitedAwareness is false || Current.ProgramState != ProgramState.Playing || WorldComponent_GravshipController.CutsceneInProgress || ModCompatibility.HasPerspectiveShiftAvatar() is false) return false;
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

        private const int ThreatCooldownTicks = 1200;

        public static void CheckForThreatsAndReact(Pawn pawn, ColonistSurveillanceData data)
        {
            data.UpdateWalkieStatus(pawn);
            if (IsLimitedAwareness is false || AlertsActive is false || data.reportReaction == ThreatReportReaction.DoNotReport || Find.TickManager.TicksGame - data.lastThreatReportTick < ThreatCooldownTicks) return;
            var map = pawn.Map;
            var hostile = FindHostile(pawn, map, surveying: true);
            if (hostile == null) return;
            var job = GetReactionJob(pawn, data, hostile);
            if (job != null) pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        public static Job TryGetReactionJob(Pawn pawn, ColonistSurveillanceData data)
        {
            if (IsLimitedAwareness is false || AlertsActive is false || data.reportReaction == ThreatReportReaction.DoNotReport || Find.TickManager.TicksGame - data.lastThreatReportTick < ThreatCooldownTicks) return null;
            var map = pawn.Map;
            var hostile = FindHostile(pawn, map, surveying: false);
            return hostile == null ? null : GetReactionJob(pawn, data, hostile);
        }

        private static Job GetReactionJob(Pawn pawn, ColonistSurveillanceData data, Pawn hostile)
        {
            data.lastThreatReportTick = Find.TickManager.TicksGame;
            var reaction = data.reportReaction;
            var avatar = ModCompatibility.GetPerspectiveShiftAvatar();
            if (reaction == ThreatReportReaction.ReportViaRadio)
            {
                if (avatar != null && avatar.Spawned && avatar.Map == pawn.Map && avatar.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie) && pawn.inventory.innerContainer.Contains(DefsOf.SS_WalkieTalkie))
                {
                    DeliverThreatSpotted(pawn, hostile, LetterDefOf.ThreatBig);
                    Building_Loudspeaker.BroadcastNews(pawn.Map, true);
                    return null;
                }
                reaction = ModCompatibility.HasPerspectiveShiftAvatar() ? ThreatReportReaction.ReportInPerson : ThreatReportReaction.ReportViaButton;
            }
            if (reaction == ThreatReportReaction.ReportInPerson && avatar != null && avatar.Spawned && avatar.Map == pawn.Map && avatar != pawn && pawn.CanReach(avatar, PathEndMode.Touch, Danger.Deadly))
            {
                return MakeReportJob(avatar, hostile);
            }
            var buttonJob = GetButtonReactionJob(pawn);
            if (buttonJob != null) return buttonJob;
            if (avatar != null && avatar.Spawned && avatar.Map == pawn.Map && avatar != pawn && pawn.CanReach(avatar, PathEndMode.Touch, Danger.Deadly))
            {
                return MakeReportJob(avatar, hostile);
            }
            DeliverThreatSpotted(pawn, hostile, LetterDefOf.ThreatBig);
            Building_Loudspeaker.BroadcastNews(pawn.Map, true);
            return null;
        }

        private static Job MakeReportJob(Pawn avatar, Pawn hostile)
        {
            var job = JobMaker.MakeJob(DefsOf.SS_ReportThreatInPerson, avatar, hostile);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            return job;
        }

        private static Job GetButtonReactionJob(Pawn pawn)
        {
            var job = Building_AlarmTrigger.GetAlarmTriggerJob(pawn);
            if (job == null && Building_AlarmTrigger.AnyAlarmActive(pawn.Map) is false)
            {
                Messages.Message("SS_NoAlarmTriggerFound".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.CautionInput);
            }
            return job;
        }

        public static Job TryGetAnimalReactionJob(Pawn pawn, AnimalSurveillanceData data, float minRadius = 0f)
        {
            if (AlertsActive is false) return null;
            var map = pawn.Map;
            var sightRadius = Mathf.Max(SightRadius(pawn), minRadius);
            Pawn hostile = null;
            foreach (var target in map.attackTargetsCache.TargetsHostileToColony)
            {
                if (target.Thing is Pawn p && p.Spawned && p.Dead is false && p.Downed is false && p.Position.Fogged(map) is false && pawn.Position.DistanceToSquared(p.Position) <= sightRadius * sightRadius && GenSight.LineOfSight(pawn.Position, p.Position, map, skipFirstCell: true))
                {
                    hostile = p;
                    break;
                }
            }
            if (hostile == null) return null;
            if (data.alertResponse == AnimalAlertResponse.ReportToHandler)
            {
                var recipient = ReportRecipient(pawn);
                if (recipient == null) return null;
                data.lastThreatReportTick = Find.TickManager.TicksGame;
                return JobMaker.MakeJob(DefsOf.SS_AnimalReportThreat, recipient, hostile);
            }
            data.lastThreatReportTick = Find.TickManager.TicksGame;
            var alarmJob = GetButtonReactionJob(pawn);
            if (alarmJob != null) alarmJob.targetB = hostile;
            return alarmJob;
        }

        private static Pawn ReportRecipient(Pawn animal)
        {
            var master = animal.playerSettings?.Master;
            if (master != null && master.Spawned && master.Map == animal.Map && master.Downed is false && animal.CanReach(master, PathEndMode.Touch, Danger.Deadly)) return master;
            Pawn best = null;
            var bestDistance = int.MaxValue;
            foreach (var colonist in animal.Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Downed || colonist.InMentalState) continue;
                var distance = animal.Position.DistanceToSquared(colonist.Position);
                if (distance >= bestDistance || animal.CanReach(colonist, PathEndMode.Touch, Danger.Deadly) is false) continue;
                best = colonist;
                bestDistance = distance;
            }
            return best;
        }

        private static Pawn FindHostile(Pawn pawn, Map map, bool surveying)
        {
            var watchtower = pawn.CurJobDef == DefsOf.SS_ManWatchtower && pawn.jobs.curDriver is JobDriver_ManWatchtower driver && driver.IsAtTop ? pawn.CurJob.targetA.Thing as Building_Watchtower : null;

            foreach (var target in map.attackTargetsCache.TargetsHostileToColony)
            {
                if (target.Thing is Pawn hostile && hostile.Spawned && hostile.Dead is false && hostile.Downed is false && hostile.Position.Fogged(map) is false)
                {
                    if (watchtower != null)
                    {
                        if (watchtower.CanWatchtowerSee(hostile.Position)) return hostile;
                    }
                    else if (surveying)
                    {
                        if (map.IsVisible(hostile.Position)) return hostile;
                    }
                    else
                    {
                        var sightRadius = SightRadius(pawn);
                        if (pawn.Position.DistanceToSquared(hostile.Position) <= sightRadius * sightRadius && GenSight.LineOfSight(pawn.Position, hostile.Position, map, skipFirstCell: true)) return hostile;
                    }
                }
            }
            return null;
        }

        public static void DeliverThreatSpotted(Pawn reporter, Thing hostile, LetterDef letterDef, bool inPerson = false)
        {
            MoteMaker.ThrowText(reporter.DrawPos, reporter.Map, "SS_HostileSpottedMote".Translate());
            SpawnPing(hostile.Position, reporter.Map, true);
            var fog = reporter.Map.GetComponent<MapComponent_FogOfWar>();
            if (hostile is Pawn p && fog.TryReportIncidentArrival(p))
            {
                return;
            }
            var desc = inPerson ? "SS_HostileSpottedInPersonDesc".Translate(reporter.LabelShort, hostile.LabelShort) : "SS_HostileSpottedDesc".Translate(reporter.LabelShort, hostile.LabelShort);
            ReceiveLetterUnmuted(LetterMaker.MakeLetter("SS_HostileSpotted".Translate(), desc, letterDef, new LookTargets(hostile)));
        }
    }
}
