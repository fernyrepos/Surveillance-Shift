using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SurveillanceShift
{
    [StaticConstructorOnStartup]
    public class Building_AlarmTrigger : Building
    {
        private const int AutoAlarmCooldownTicks = 1200;
        private static readonly ColorInt AlarmRed = new(255, 20, 20, 255);
        private static readonly Texture2D LinkIcon = ContentFinder<Texture2D>.Get("UI/Commands/Attack");

        private CompPowerTrader powerComp;
        private int lastTriggeredTick = -AutoAlarmCooldownTicks;
        private bool alarmActive;
        private List<Thing> linkedTargets = new();
        private Dictionary<int, ColorInt> originalLightColors = new();

        public List<Thing> LinkedTargets => linkedTargets;
        public bool CanTriggerNow => powerComp.PowerOn && Map.gameConditionManager.ElectricityDisabled(Map) is false;
        public bool AlarmActive => alarmActive;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ResetAlarm();
            base.DeSpawn(mode);
        }

        public void ToggleAlarm()
        {
            if (alarmActive) ResetAlarm();
            else ActivateAlarm();
        }

        public void ActivateAlarm()
        {
            if (alarmActive || CanTriggerNow is false) return;
            lastTriggeredTick = Find.TickManager.TicksGame;
            alarmActive = true;

            var soundPlayed = false;
            for (var i = linkedTargets.Count - 1; i >= 0; i--)
            {
                var target = linkedTargets[i];
                if (target.Destroyed)
                {
                    linkedTargets.RemoveAt(i);
                    continue;
                }

                if (target is Building_PowerSwitch ps && FlickUtility.WantsToBeOn(ps))
                {
                    var flickable = ps.GetComp<CompFlickable>();
                    flickable.DoFlick();
                    flickable.wantSwitchOn = false;
                }
                else if (target is Building_Loudspeaker speaker)
                {
                    if (speaker.IsPowered && soundPlayed is false)
                    {
                        speaker.Alert(Position, true);
                        soundPlayed = true;
                    }
                }
                else
                {
                    var glower = target.TryGetComp<CompGlower>();
                    if (originalLightColors.ContainsKey(target.thingIDNumber) is false)
                    {
                        originalLightColors[target.thingIDNumber] = glower.GlowColor;
                    }
                    glower.GlowColor = AlarmRed;
                    glower.UpdateLit(Map);
                }
            }
            if (soundPlayed is false)
            {
                State.SpawnPing(Position, Map, true);
                DefsOf.SS_Sound_Alarm.PlayOneShotOnCamera(Map);
            }
        }

        public static void SoundNearestAlarm(Map map, Pawn monitorPawn)
        {
            if (map.listerBuildings.AllBuildingsColonistOfClass<Building_AlarmTrigger>().Any(t => t.AlarmActive)) return;
            Building_AlarmTrigger best = null;
            var bestDist = float.MaxValue;
            foreach (var t in map.listerBuildings.AllBuildingsColonistOfClass<Building_AlarmTrigger>())
            {
                var dist = (monitorPawn.Position - t.Position).LengthHorizontalSquared;
                if (t.AlarmActive is false && t.CanTriggerNow && Find.TickManager.TicksGame >= t.lastTriggeredTick + AutoAlarmCooldownTicks && monitorPawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Some) && dist < bestDist)
                {
                    bestDist = dist;
                    best = t;
                }
            }
            if (best == null) return;
            monitorPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(DefsOf.SS_PushAlarmTrigger, best), JobTag.Misc);
        }

        public static void CheckThreatsAndSoundAlarm(Map map, Pawn monitorPawn)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns.Any(p => p.HostileTo(Faction.OfPlayer) && p.Dead is false && p.Downed is false && map.IsVisible(p.Position)))
            {
                SoundNearestAlarm(map, monitorPawn);
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (var linkedTarget in linkedTargets)
            {
                if (linkedTarget is { Spawned: true } target)
                {
                    GenDraw.DrawLineBetween(this.TrueCenter(), target.TrueCenter());
                }
            }
        }

        public void ResetAlarm()
        {
            if (alarmActive is false) return;
            lastTriggeredTick = Find.TickManager.TicksGame;
            alarmActive = false;
            SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(Position, Map));

            foreach (var twc in linkedTargets.OfType<ThingWithComps>())
            {
                if (twc.Spawned && twc.Map == Map && originalLightColors.TryGetValue(twc.thingIDNumber, out var origCol))
                {
                    var glower = twc.GetComp<CompGlower>();
                    glower.GlowColor = origCol;
                    glower.UpdateLit(Map);
                }
            }
            originalLightColors.Clear();

            var pings = Map.listerThings.ThingsOfDef(DefsOf.SS_Mote_PingHostile).Concat(Map.listerThings.ThingsOfDef(DefsOf.SS_Mote_PingFriendly));
            foreach (var ping in pings.ToList())
            {
                if (ping.Position == Position || linkedTargets.Any(t => t.Position == ping.Position))
                {
                    ping.Destroy();
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(myPawn))
            {
                yield return opt;
            }

            if (myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some) is false)
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }
            else
            {
                var label = alarmActive ? "SS_ResetAlarm".Translate() : "SS_PushAlarm".Translate();
                yield return new FloatMenuOption(label, () =>
                {
                    var job = JobMaker.MakeJob(DefsOf.SS_PushAlarmTrigger, this);
                    myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            yield return new Command_Action
            {
                defaultLabel = "SS_LinkAlarmBuilding".Translate(),
                defaultDesc = "SS_LinkAlarmBuildingDesc".Translate(),
                icon = LinkIcon,
                action = () =>
                Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetBuildings = true,
                    canTargetPawns = false,
                    validator = t => t.Thing != null && t.Thing.Faction == Faction.OfPlayer && (t.Thing.TryGetComp<CompGlower>() != null && t.Thing.TryGetComp<CompPowerTrader>() != null || t.Thing is Building_PowerSwitch || t.Thing is Building_Loudspeaker)
                }, target =>
                {
                    if (linkedTargets.Contains(target.Thing))
                    {
                        linkedTargets.Remove(target.Thing);
                        Messages.Message("SS_UnlinkedFromAlarm".Translate(target.Thing.LabelShort), this, MessageTypeDefOf.NeutralEvent, false);
                    }
                    else
                    {
                        linkedTargets.Add(target.Thing);
                        Messages.Message("SS_LinkedToAlarm".Translate(target.Thing.LabelShort), this, MessageTypeDefOf.PositiveEvent, false);
                    }
                })
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastTriggeredTick, "lastTriggeredTick", -AutoAlarmCooldownTicks);
            Scribe_Values.Look(ref alarmActive, "alarmActive");
            Scribe_Collections.Look(ref linkedTargets, "linkedTargets", LookMode.Reference);
            Scribe_Collections.Look(ref originalLightColors, "originalLightColors", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                linkedTargets ??= new List<Thing>();
                originalLightColors ??= new Dictionary<int, ColorInt>();
                linkedTargets.RemoveAll(x => x == null);
            }
        }
    }
}
