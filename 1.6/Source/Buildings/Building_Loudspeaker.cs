using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace SurveillanceShift
{
    public class Building_Loudspeaker : Building
    {
        private CompPowerTrader powerComp;

        public bool IsPowered => powerComp.PowerOn && Map.gameConditionManager.ElectricityDisabled(Map) is false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public void Alert(bool isHostile)
        {
            if (IsPowered is false) return;
            if (isHostile)
            {
                DefsOf.SS_Sound_LoudspeakerHostile.PlayOneShotOnCamera(Map);
                foreach (var colonist in Map.mapPawns.FreeColonistsSpawned.ToList())
                {
                    if (colonist.Awake() is false)
                    {
                        RestUtility.WakeUp(colonist);
                    }
                }
                return;
            }
            DefsOf.SS_Sound_LoudspeakerNeutral.PlayOneShotOnCamera(Map);
        }

        public static bool BroadcastNews(Map map, bool isHostile)
        {
            foreach (var speaker in map.listerBuildings.AllBuildingsColonistOfClass<Building_Loudspeaker>())
            {
                if (speaker.IsPowered is false) continue;
                speaker.Alert(isHostile);
                return true;
            }
            return false;
        }
    }
}
