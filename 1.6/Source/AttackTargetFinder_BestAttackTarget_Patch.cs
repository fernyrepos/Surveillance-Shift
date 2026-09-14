using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.BestAttackTarget))]
    public static class AttackTargetFinder_BestAttackTarget_Patch
    {
        public static void Prefix(IAttackTargetSearcher searcher, ref Predicate<Thing> validator)
        {
            if (State.IsLimitedAwareness is false || searcher.Thing?.Map is null || searcher.Thing.Faction != Faction.OfPlayer) return;

            var origValidator = validator;
            validator = t =>
            {
                if (t.Map.IsVisible(t.Position) is false) return false;
                return origValidator is null || origValidator(t);
            };
        }
    }
}
