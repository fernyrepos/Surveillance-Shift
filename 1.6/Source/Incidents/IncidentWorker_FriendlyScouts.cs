using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class IncidentWorker_FriendlyScouts : IncidentWorker
    {
        private const int MinScouts = 1;
        private const int MaxScouts = 3;

        public override bool CanFireNowSub(IncidentParms parms) => SurveillanceShiftMod.settings.enableScouts && State.IsLimitedAwareness && Faction.OfPlayer.def.techLevel != TechLevel.Animal && base.CanFireNowSub(parms) && CandidateFactions().Any();

        private static IEnumerable<Faction> CandidateFactions() =>
            Find.FactionManager.AllFactions.Where(f => f.IsPlayer is false && f.defeated is false && f.Hidden is false && f.HostileTo(Faction.OfPlayer) is false && f.temporary is false && f.def.humanlikeFaction);

        public override float BaseChanceThisGame => base.BaseChanceThisGame * SurveillanceShiftMod.settings.friendlyScoutFrequency;

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            if (parms.faction == null && CandidateFactions().TryRandomElement(out parms.faction) is false || RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral) is false) return false;

            var count = Rand.RangeInclusive(MinScouts, MaxScouts);
            var list = new List<Pawn>();
            for (var i = 0; i < count; i++)
            {
                var pKind = parms.faction.RandomPawnKind();
                var p = PawnGenerator.GeneratePawn(pKind, parms.faction);
                var loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
                GenSpawn.Spawn(p, loc, map);
                list.Add(p);
            }
            if (RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out var result) is false)
            {
                result = list[0].Position;
            }
            var lordJob = new LordJob_FriendlyScouts(parms.faction, result);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            SendStandardLetter("SS_LetterFriendlyScoutsLabel".Translate(parms.faction.Name), "SS_LetterFriendlyScoutsText".Translate(parms.faction.NameColored), LetterDefOf.NeutralEvent, parms, list[0]);
            return true;
        }
    }
}
