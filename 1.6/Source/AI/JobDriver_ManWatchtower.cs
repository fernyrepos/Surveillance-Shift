using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_ManWatchtower : JobDriver
    {
        private const int SkillIntervalTicks = 60;
        private const int ScanDurationMin = 220;
        private const int ScanDurationMax = 450;
        private const int PaceDurationTicks = 60;
        private const int StepDurationTicks = 35;
        private const float MoteForwardOffset = 0.38f;
        private Building_Watchtower Watchtower => (Building_Watchtower)job.GetTarget(TargetIndex.A).Thing;
        private float BodyYOffset => 9f;
        private Mote scanningMote;
        public Vector3? forcedBodyOffset;
        private bool isAtTop;
        private Toil returnToLadder;
        public bool gracefulExitStarted;
        private int currentCycle;
        private int maxCycles;
        private Vector2 currentVantage;
        private Vector2 nextVantage;
        private Vector2 startVantage;
        public override Vector3 ForcedBodyOffset => forcedBodyOffset.HasValue ? forcedBodyOffset.Value : Vector3.zero;
        public bool IsAtTop => isAtTop;
        public bool IsElevated => forcedBodyOffset.HasValue && forcedBodyOffset.Value.y > 0f;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            var climbLadder = ToilMaker.MakeToil("ClimbLadder");
            climbLadder.handlingFacing = true;
            climbLadder.defaultCompleteMode = ToilCompleteMode.Delay;
            climbLadder.initAction = () =>
            {
                maxCycles = Rand.RangeInclusive(20, 30);
                climbLadder.actor.jobs.curDriver.ticksLeftThisToil = Watchtower.Extension.climbTicks;
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.North.FacingCell);
            };
            climbLadder.tickAction = () =>
            {
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.North.FacingCell);
                var progress = 1f - (float)ticksLeftThisToil / Watchtower.Extension.climbTicks;
                forcedBodyOffset = new Vector3(0f, BodyYOffset, Mathf.Lerp(0f, Watchtower.Extension.climbHeight, progress));
            };
            yield return climbLadder;

            var stepToPlatform = ToilMaker.MakeToil("StepToPlatform");
            stepToPlatform.handlingFacing = true;
            stepToPlatform.defaultCompleteMode = ToilCompleteMode.Delay;
            stepToPlatform.initAction = () =>
            {
                startVantage = new Vector2(0f, Watchtower.Extension.climbHeight);
                currentVantage = Watchtower.Extension.vantageOffsets.RandomElement();
                stepToPlatform.actor.jobs.curDriver.ticksLeftThisToil = StepDurationTicks;
                FaceWalkDirection(startVantage, currentVantage);
            };
            stepToPlatform.tickAction = () =>
            {
                var cur = Vector2.Lerp(startVantage, currentVantage, 1f - (float)ticksLeftThisToil / StepDurationTicks);
                forcedBodyOffset = new Vector3(cur.x, BodyYOffset, cur.y);
            };
            yield return stepToPlatform;

            var watch = ToilMaker.MakeToil("MakeNewToils");
            watch.handlingFacing = true;
            watch.defaultCompleteMode = ToilCompleteMode.Delay;
            watch.initAction = () =>
            {
                isAtTop = true;
                forcedBodyOffset = new Vector3(currentVantage.x, BodyYOffset, currentVantage.y);
                if (scanningMote == null)
                {
                    AttachObservationMote();
                }
                OrientFacingOutward();
                watch.actor.jobs.curDriver.ticksLeftThisToil = Rand.RangeInclusive(ScanDurationMin, ScanDurationMax);
            };
            watch.tickAction = () =>
            {
                UpdateMoteTransform();
                if (pawn.IsHashIntervalTick(SkillIntervalTicks))
                {
                    pawn.skills?.Learn(SkillDefOf.Shooting, 0.035f);
                    pawn.skills?.Learn(SkillDefOf.Intellectual, 0.015f);
                }
            };
            yield return watch;

            var pace = ToilMaker.MakeToil("MakeNewToils");
            pace.handlingFacing = true;
            pace.defaultCompleteMode = ToilCompleteMode.Delay;
            pace.initAction = () =>
            {
                startVantage = currentVantage;
                var options = Watchtower.Extension.vantageOffsets;
                nextVantage = options.Count > 1 ? options.RandomElementByWeight(v => v != startVantage ? 1f : 0f) : startVantage;
                pace.actor.jobs.curDriver.ticksLeftThisToil = PaceDurationTicks;
                FaceWalkDirection(startVantage, nextVantage);
                UpdateMoteTransform();
            };
            pace.tickAction = () =>
            {
                var cur = Vector2.Lerp(startVantage, nextVantage, 1f - (float)ticksLeftThisToil / PaceDurationTicks);
                forcedBodyOffset = new Vector3(cur.x, BodyYOffset, cur.y);
                UpdateMoteTransform();
            };
            pace.AddFinishAction(() =>
            {
                currentVantage = nextVantage;
                currentCycle++;
            });
            yield return pace;

            var checkShift = ToilMaker.MakeToil("CheckShift");
            checkShift.initAction = () =>
            {
                if (currentCycle < maxCycles)
                {
                    JumpToToil(watch);
                }
                else
                {
                    ReadyForNextToil();
                }
            };
            yield return checkShift;

            returnToLadder = ToilMaker.MakeToil("ReturnToLadder");
            returnToLadder.handlingFacing = true;
            returnToLadder.defaultCompleteMode = ToilCompleteMode.Delay;
            returnToLadder.initAction = () =>
            {
                CleanupObservationMote();
                startVantage = new Vector2(forcedBodyOffset.Value.x, forcedBodyOffset.Value.z);
                nextVantage = new Vector2(0f, Watchtower.Extension.climbHeight);
                returnToLadder.actor.jobs.curDriver.ticksLeftThisToil = StepDurationTicks;
                FaceWalkDirection(startVantage, nextVantage);
            };
            returnToLadder.tickAction = () =>
            {
                var cur = Vector2.Lerp(startVantage, nextVantage, 1f - (float)ticksLeftThisToil / StepDurationTicks);
                forcedBodyOffset = new Vector3(cur.x, BodyYOffset, cur.y);
            };
            yield return returnToLadder;

            var descendLadder = ToilMaker.MakeToil("DescendLadder");
            descendLadder.handlingFacing = true;
            descendLadder.defaultCompleteMode = ToilCompleteMode.Delay;
            descendLadder.initAction = () =>
            {
                isAtTop = false;
                descendLadder.actor.jobs.curDriver.ticksLeftThisToil = Watchtower.Extension.climbTicks;
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.North.FacingCell);
            };
            descendLadder.tickAction = () =>
            {
                pawn.rotationTracker.FaceCell(pawn.Position + Rot4.North.FacingCell);
                var progress = 1f - (float)ticksLeftThisToil / Watchtower.Extension.climbTicks;
                forcedBodyOffset = new Vector3(0f, BodyYOffset, Mathf.Lerp(Watchtower.Extension.climbHeight, 0f, progress));
            };
            descendLadder.AddFinishAction(() => forcedBodyOffset = Vector3.zero);
            yield return descendLadder;

            AddFinishAction(_ =>
            {
                CleanupObservationMote();
                forcedBodyOffset = Vector3.zero;
                isAtTop = false;
            });
        }

        public void BeginGracefulExit()
        {
            gracefulExitStarted = true;
            JumpToToil(returnToLadder);
        }

        private void FaceWalkDirection(Vector2 from, Vector2 to)
        {
            var diff = to - from;
            if (diff.sqrMagnitude < 0.001f) return;
            var rot = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
                ? diff.x > 0f ? Rot4.East : Rot4.West
                : diff.y > 0f ? Rot4.North : Rot4.South;
            pawn.rotationTracker.FaceCell(pawn.Position + rot.FacingCell);
        }

        private void OrientFacingOutward()
        {
            Rot4 rot;
            if (currentVantage.y <= Watchtower.Extension.climbHeight + 0.35f)
            {
                rot = currentVantage.x > 0.2f ? Rand.Bool ? Rot4.South : Rot4.East
                    : currentVantage.x < -0.2f ? Rand.Bool ? Rot4.South : Rot4.West : Rot4.South;
            }
            else
            {
                rot = currentVantage.x > 0.2f ? Rand.Bool ? Rot4.North : Rot4.East
                    : currentVantage.x < -0.2f ? Rand.Bool ? Rot4.North : Rot4.West : Rot4.North;
            }
            pawn.rotationTracker.FaceCell(pawn.Position + rot.FacingCell);
            UpdateMoteTransform();
        }

        private void UpdateMoteTransform()
        {
            if (scanningMote != null)
            {
                scanningMote.Rotation = pawn.Rotation;
                scanningMote.exactRotation = pawn.Rotation.AsAngle;
                var pos = pawn.DrawPos + pawn.Rotation.FacingCell.ToVector3() * MoteForwardOffset;
                pos.y += pawn.Rotation == Rot4.North ? -0.05f : 0.05f;
                scanningMote.exactPosition = pos;
            }
        }

        private void AttachObservationMote()
        {
            CleanupObservationMote();
            scanningMote = MoteMaker.MakeAttachedOverlay(pawn, Watchtower.Extension.moteDef, Vector3.zero);
            UpdateMoteTransform();
        }

        private void CleanupObservationMote()
        {
            if (scanningMote != null && scanningMote.Destroyed is false)
            {
                scanningMote.Destroy();
                scanningMote = null;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forcedBodyOffset, "forcedBodyOffset");
            Scribe_Values.Look(ref isAtTop, "isAtTop");
            Scribe_Values.Look(ref currentCycle, "currentCycle");
            Scribe_Values.Look(ref maxCycles, "maxCycles", 5);
            Scribe_Values.Look(ref currentVantage, "currentVantage");
            Scribe_Values.Look(ref nextVantage, "nextVantage");
            Scribe_Values.Look(ref startVantage, "startVantage");
            Scribe_Values.Look(ref gracefulExitStarted, "gracefulExitStarted");
        }
    }
}
