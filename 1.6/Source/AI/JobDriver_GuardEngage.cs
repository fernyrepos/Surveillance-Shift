using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_GuardEngage : JobDriver
    {
        private const int RepathInterval = 40;
        private const int CoverCheckInterval = 150;
        private const int GiveUpAfterFailures = 4;

        private int nextRepathTick;
        private int nextCoverTick;
        private int failures;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.IsHashIntervalTick(10) && GuardUtility.DutyIsValid(pawn, out _) is false);
            var fight = ToilMaker.MakeToil("Fight");
            fight.defaultCompleteMode = ToilCompleteMode.Never;
            fight.tickAction = FightTick;
            fight.AddFinishAction(() => pawn.mindState.enemyTarget = null);
            yield return fight;
        }

        private void FightTick()
        {
            var assignment = pawn.GetGuardAssignment();
            var anchor = GuardUtility.Anchor(pawn, assignment);
            var leash = GuardUtility.Leash(assignment.stance);
            var target = job.targetA.Thing;

            if (GuardCombat.TargetStillValid(pawn, target, anchor, leash) is false)
            {
                target = GuardCombat.FindThreat(pawn, anchor, leash);
                if (target is null)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                job.targetA = target;
                failures = 0;
                nextRepathTick = 0;
            }

            pawn.mindState.enemyTarget = target;
            pawn.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;

            var verb = pawn.TryGetAttackVerb(target);
            if (verb is null)
            {
                GuardCombat.Ignore(pawn, target);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (verb.verbProps.IsMeleeAttack) MeleeTick(target, verb);
            else RangedTick(target, verb, anchor, leash);
        }

        private void MeleeTick(Thing target, Verb verb)
        {
            if (pawn.Position.AdjacentTo8WayOrInside(target.Position))
            {
                if (pawn.pather.Moving) pawn.pather.StopDead();
                if (pawn.stances.FullBodyBusy is false) pawn.meleeVerbs.TryMeleeAttack(target, verb);
                return;
            }
            if (Find.TickManager.TicksGame < nextRepathTick && pawn.pather.Moving) return;
            nextRepathTick = Find.TickManager.TicksGame + RepathInterval;
            if (pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly)) pawn.pather.StartPath(target, PathEndMode.Touch);
            else GiveUp(target);
        }

        private void RangedTick(Thing target, Verb verb, IntVec3 anchor, float leash)
        {
            var now = Find.TickManager.TicksGame;
            var canHit = verb.CanHitTarget(target);

            if (canHit)
            {
                if (pawn.pather.Moving is false)
                {
                    pawn.TryStartAttack(target);
                    if (now >= nextCoverTick && CoverUtility.CalculateOverallBlockChance(pawn, target.Position, pawn.Map) < 0.05f)
                    {
                        nextCoverTick = now + CoverCheckInterval;
                        MoveToCastPosition(target, verb, anchor, leash, false);
                    }
                }
                return;
            }

            if (now < nextRepathTick && pawn.pather.Moving) return;
            nextRepathTick = now + RepathInterval;
            if (MoveToCastPosition(target, verb, anchor, leash, true) is false) GiveUp(target);
        }

        private bool MoveToCastPosition(Thing target, Verb verb, IntVec3 anchor, float leash, bool required)
        {
            var request = new CastPositionRequest
            {
                caster = pawn,
                target = target,
                verb = verb,
                maxRangeFromTarget = verb.EffectiveRange,
                locus = anchor,
                maxRangeFromLocus = leash,
                wantCoverFromTarget = true,
                maxRegions = 40
            };
            if (CastPositionFinder.TryFindCastPosition(request, out var cell) is false) return required is false;
            if (cell != pawn.Position) pawn.pather.StartPath(cell, PathEndMode.OnCell);
            return true;
        }

        private void GiveUp(Thing target)
        {
            if (++failures < GiveUpAfterFailures) return;
            GuardCombat.Ignore(pawn, target);
            EndJobWith(JobCondition.Incompletable);
        }
    }
}
