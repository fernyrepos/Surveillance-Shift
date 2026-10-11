using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    [HotSwappable]
    public class JobDriver_UseBinoculars : JobDriver
    {
        private Mote binocularMote;
        private IntVec3 lastFacedCell = IntVec3.Invalid;
        private Rot4 lastMoteRotation = Rot4.Invalid;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Downed || CompBinoculars.Find(pawn) is null);

            var watch = ToilMaker.MakeToil("UseBinoculars");
            watch.handlingFacing = true;
            watch.defaultCompleteMode = ToilCompleteMode.Never;
            watch.initAction = () =>
            {
                ScopeView.Begin(pawn, ScopeKind.Binoculars, DefsOf.SS_UseBinoculars);
                lastMoteRotation = pawn.Rotation;
                binocularMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_Binoculars, ScopeView.MoteOffsetFor(lastMoteRotation));
            };
            watch.tickAction = () =>
            {
                if (ScopeView.IsActive(pawn) is false)
                {
                    ScopeView.Begin(pawn, ScopeKind.Binoculars, DefsOf.SS_UseBinoculars);
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
                    if (binocularMote != null && binocularMote.Destroyed is false)
                    {
                        binocularMote.Destroy();
                    }
                    binocularMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_Binoculars, ScopeView.MoteOffsetFor(lastMoteRotation));
                }
            };
            yield return watch;

            AddFinishAction(_ =>
            {
                ScopeView.End(pawn);
                if (binocularMote != null && binocularMote.Destroyed is false)
                {
                    binocularMote.Destroy();
                    binocularMote = null;
                }
            });
        }
    }
}
