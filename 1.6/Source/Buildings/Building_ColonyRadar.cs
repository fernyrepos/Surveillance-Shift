using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    public class Building_ColonyRadar : Building
    {
        private CompPowerTrader powerComp;

        public bool IsActive => Spawned && powerComp.PowerOn && Map.gameConditionManager.ElectricityDisabled(Map) is false;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public override string GetInspectString()
        {
            var text = new StringBuilder(base.GetInspectString());
            text.AppendInNewLine(IsActive ? "SS_RadarActive".Translate() : "SS_RadarOffline".Translate());
            return text.ToString().TrimEndNewlines();
        }

        public static bool HasActiveRadar(Map map)
        {
            return map.listerBuildings.AllBuildingsColonistOfClass<Building_ColonyRadar>().Any(r => r.IsActive);
        }
    }
}
