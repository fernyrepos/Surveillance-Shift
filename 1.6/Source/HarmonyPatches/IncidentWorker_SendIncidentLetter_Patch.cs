using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.SendIncidentLetter))]
    public static class IncidentWorker_SendIncidentLetter_Patch
    {
        public static bool Prefix(TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, IncidentDef def, NamedArgument[] textArgs)
        {
            if (SurveillanceShiftMod.settings.announceVisits || State.IsLimitedAwareness is false || parms.target is not Map map || def.HasModExtension<MuteArrivalExtension>() is false) return true;

            var pawns = new List<Pawn>();
            foreach (var item in lookTargets.targets)
            {
                if (item.Thing is Pawn p)
                {
                    pawns.Add(p);
                }
            }
            if (parms.faction != null)
            {
                foreach (var allPawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (allPawn.Faction == parms.faction && allPawn.IsPrisonerOfColony is false && allPawn.IsSlaveOfColony is false && pawns.Contains(allPawn) is false)
                    {
                        pawns.Add(allPawn);
                    }
                }
            }

            if (pawns.Count == 0) return true;

            var formattedText = baseLetterText.Formatted(textArgs);
            var text = parms.customLetterText.NullOrEmpty() ? formattedText : parms.customLetterText.Formatted(formattedText.Named("BASETEXT"));
            var formattedLabel = baseLetterLabel.Formatted(textArgs);
            var label = parms.customLetterLabel.NullOrEmpty() ? formattedLabel : parms.customLetterLabel.Formatted(formattedLabel.Named("BASELABEL"));

            var letter = LetterMaker.MakeLetter(label, text, parms.customLetterDef ?? baseLetterDef, lookTargets, parms.faction, parms.quest, parms.letterHyperlinkThingDefs);
            var hediffs = new List<HediffDef>();
            if (parms.letterHyperlinkHediffDefs.NullOrEmpty() is false)
            {
                hediffs.AddRange(parms.letterHyperlinkHediffDefs);
            }
            if (def.letterHyperlinkHediffDefs.NullOrEmpty() is false)
            {
                hediffs.AddRange(def.letterHyperlinkHediffDefs);
            }
            letter.hyperlinkHediffDefs = hediffs;
            var comp = map.GetComponent<MapComponent_FogOfWar>();
            comp.QueueArrival(letter, pawns, parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer));
            parms.silent = true;
            return false;
        }
    }
}
