using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_UseTelescope : JobDriver
    {
        private Mote telescopeMote;
        private IntVec3 lastFacedCell = IntVec3.Invalid;
        private Rot4 lastMoteRotation = Rot4.Invalid;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Downed || CompTelescope.Find(pawn) is null);

            var watch = ToilMaker.MakeToil("UseTelescope");
            watch.handlingFacing = true;
            watch.defaultCompleteMode = ToilCompleteMode.Never;
            watch.initAction = () =>
            {
                ScopeView.Begin(pawn, ScopeKind.Telescope, DefsOf.SS_UseTelescope);
                lastMoteRotation = pawn.Rotation;
                telescopeMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_Spyglass, ScopeView.MoteOffsetFor(lastMoteRotation));
            };
            watch.tickAction = () =>
            {
                if (ScopeView.IsActive(pawn) is false)
                {
                    ScopeView.Begin(pawn, ScopeKind.Telescope, DefsOf.SS_UseTelescope);
                }
                if (Find.CurrentMap == pawn.Map)
                {
                    var center = ScopeView.ScopeCenter(pawn.Map);
                    if (center != pawn.Position && center != lastFacedCell)
                    {
                        lastFacedCell = center;
                        pawn.rotationTracker.FaceCell(center);
                    }
                }
                if (pawn.Rotation != lastMoteRotation)
                {
                    lastMoteRotation = pawn.Rotation;
                    if (telescopeMote != null && telescopeMote.Destroyed is false)
                    {
                        telescopeMote.Destroy();
                    }
                    telescopeMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_Spyglass, ScopeView.MoteOffsetFor(lastMoteRotation));
                }
            };
            yield return watch;

            AddFinishAction(_ =>
            {
                ScopeView.End(pawn);
                if (telescopeMote != null && telescopeMote.Destroyed is false)
                {
                    telescopeMote.Destroy();
                    telescopeMote = null;
                }
            });
        }
    }
}
