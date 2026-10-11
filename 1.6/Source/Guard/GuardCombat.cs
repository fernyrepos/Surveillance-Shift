using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public static class GuardCombat
    {
        private const float MinScanRange = 20f;
        private const int IgnoreTicks = 600;

        private class Ignored
        {
            public Thing thing;
            public int untilTick;
        }

        private static readonly ConditionalWeakTable<Pawn, Ignored> ignored = new();

        public static bool CanEngage(Pawn pawn)
        {
            if (pawn.Downed || pawn.WorkTagIsDisabled(WorkTags.Violent)) return false;
            if (pawn.RaceProps.Humanlike) return pawn.equipment?.Primary != null;
            return pawn.kindDef.canMeleeAttack || pawn.equipment?.Primary != null || pawn.CurrentEffectiveVerb != null;
        }

        public static Thing FindThreat(Pawn pawn, IntVec3 anchor, float leash)
        {
            var verb = pawn.CurrentEffectiveVerb;
            var range = Mathf.Max(verb?.EffectiveRange ?? 0f, MinScanRange);
            const TargetScanFlags flags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable | TargetScanFlags.NeedReachableIfCantHitFromMyPos;
            var skip = SkippedThing(pawn);
            var target = AttackTargetFinder.BestAttackTarget(pawn, flags, t => t != skip, 0f, range, anchor, leash);
            return target?.Thing;
        }

        public static void Ignore(Pawn pawn, Thing thing)
        {
            ignored.Remove(pawn);
            ignored.Add(pawn, new Ignored { thing = thing, untilTick = Find.TickManager.TicksGame + IgnoreTicks });
        }

        private static Thing SkippedThing(Pawn pawn)
        {
            if (ignored.TryGetValue(pawn, out var entry) is false) return null;
            return Find.TickManager.TicksGame < entry.untilTick ? entry.thing : null;
        }

        public static bool TargetStillValid(Pawn pawn, Thing target, IntVec3 anchor, float leash)
        {
            if (target is null || target.Spawned is false || target.Destroyed || target.Map != pawn.Map) return false;
            if (target is Pawn other && other.Downed) return false;
            if (target is IAttackTarget attackTarget && attackTarget.ThreatDisabled(pawn)) return false;
            if (GenHostility.HostileTo(pawn, target) is false) return false;
            return target.Position.InHorDistOf(anchor, leash + 6f);
        }
    }
}
