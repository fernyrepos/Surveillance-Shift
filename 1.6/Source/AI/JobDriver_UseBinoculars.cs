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
        private const int FaceIntervalTicks = 30;
        private Mote binocularMote;

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
                BinocularView.Begin(pawn);
                binocularMote = MoteMaker.MakeAttachedOverlay(pawn, DefsOf.SS_Mote_Binoculars, Vector3.zero);
            };
            watch.tickAction = () =>
            {
                if (BinocularView.IsActive(pawn) is false)
                {
                    BinocularView.Begin(pawn);
                }
                if (pawn.IsHashIntervalTick(FaceIntervalTicks) && Find.CurrentMap == pawn.Map)
                {
                    var center = BinocularView.ScopeCenter(pawn.Map);
                    if (center != pawn.Position)
                    {
                        pawn.rotationTracker.FaceCell(center);
                    }
                }
            };
            yield return watch;

            AddFinishAction(_ =>
            {
                BinocularView.End(pawn);
                if (binocularMote != null && binocularMote.Destroyed is false)
                {
                    binocularMote.Destroy();
                    binocularMote = null;
                }
            });
        }
    }
}
