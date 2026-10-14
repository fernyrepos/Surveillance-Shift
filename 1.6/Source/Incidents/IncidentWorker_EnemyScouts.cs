using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class IncidentWorker_EnemyScouts : IncidentWorker
    {
        public override bool CanFireNowSub(IncidentParms parms) => SurveillanceShiftMod.settings.enableScouts && State.IsLimitedAwareness && base.CanFireNowSub(parms) && CandidateFactions().Any();

        public static IEnumerable<Faction> CandidateFactions() =>
            Find.FactionManager.AllFactions.Where(f => f.IsPlayer is false && f.defeated is false && f.Hidden is false && f.HostileTo(Faction.OfPlayer) && f.temporary is false && f.def.humanlikeFaction);

        public override float BaseChanceThisGame => base.BaseChanceThisGame * SurveillanceShiftMod.settings.hostileScoutFrequency;

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            if (parms.faction == null && CandidateFactions().TryRandomElement(out parms.faction) is false || RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Hostile) is false) return false;

            var mult = State.GetRetaliationMultiplier(parms.faction);
            var count = Mathf.Clamp(Rand.RangeInclusive(2, 8) * mult, 1, 30);
            var pawns = new List<Pawn>();
            for (var i = 0; i < count; i++)
            {
                var pKind = parms.faction.RandomPawnKind();
                var p = PawnGenerator.GeneratePawn(pKind, parms.faction);
                var loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
                GenSpawn.Spawn(p, loc, map);
                pawns.Add(p);
            }

            var scoutSpot = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 8);
            var lordJob = new LordJob_EnemyScouts(parms.faction, scoutSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, pawns);

            var label = mult > 1
                ? "SS_LetterRetaliationLabel".Translate(parms.faction.Name)
                : "SS_LetterEnemyScoutsLabel".Translate(parms.faction.Name);
            var text = mult > 1
                ? "SS_LetterRetaliationText".Translate(parms.faction.NameColored)
                : "SS_LetterEnemyScoutsText".Translate(parms.faction.NameColored);

            SendIncidentLetter(label, text, LetterDefOf.ThreatSmall, parms, pawns[0], def);
            return true;
        }
    }
}
