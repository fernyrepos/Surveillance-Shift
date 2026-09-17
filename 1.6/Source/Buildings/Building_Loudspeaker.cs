using RimWorld;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    public class Building_Loudspeaker : Building
    {
        private CompPowerTrader powerComp;

        public bool IsPowered => powerComp.PowerOn && Map.gameConditionManager.ElectricityDisabled(Map) is false;
        public bool IsLinkedToAlarmTrigger
        {
            get
            {
                foreach (var trigger in Map.listerBuildings.AllBuildingsColonistOfClass<Building_AlarmTrigger>())
                {
                    if (trigger.LinkedTargets.Contains(this)) return true;
                }
                return false;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public void Alert(bool isHostile)
        {
            if (IsPowered is false || isHostile is false || IsLinkedToAlarmTrigger is false) return;
            DefsOf.SS_Sound_Alarm.PlayOneShotOnCamera(Map);
            foreach (var colonist in Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Awake() is false)
                {
                    RestUtility.WakeUp(colonist);
                }
            }
        }

        public static bool BroadcastNews(Map map, bool isHostile)
        {
            Building_Loudspeaker chosen = null;
            foreach (var speaker in map.listerBuildings.AllBuildingsColonistOfClass<Building_Loudspeaker>())
            {
                if (speaker.IsPowered is false) continue;
                chosen ??= speaker;
                if (isHostile && speaker.IsLinkedToAlarmTrigger)
                {
                    chosen = speaker;
                    break;
                }
            }
            if (chosen == null) return false;
            chosen.Alert(isHostile);
            return true;
        }
    }
}
