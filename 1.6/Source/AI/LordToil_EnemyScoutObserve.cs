using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurveillanceShift
{
    public class LordToil_EnemyScoutObserve : LordToil
    {
        private IntVec3 spot;
        private int duration;
        private int ticksPassed;
        private Effecter progressBar;
        private Pawn currentLeadScout;
        private Mote scanningMote;

        public LordToil_EnemyScoutObserve(IntVec3 spot, int duration)
        {
            this.spot = spot;
            this.duration = duration;
        }

        public override void UpdateAllDuties()
        {
            foreach (var pawn in lord.ownedPawns)
            {
                pawn.mindState.duty = pawn == currentLeadScout ? new PawnDuty(DutyDefOf.Idle, spot) : new PawnDuty(DutyDefOf.WanderClose, spot);
            }
        }

        public override void LordToilTick()
        {
            ticksPassed++;
            var leadScout = lord.ownedPawns.Find(p => p.Spawned && p.Dead is false && p.Downed is false);
            if (leadScout != null)
            {
                if (leadScout != currentLeadScout)
                {
                    currentLeadScout = leadScout;
                    UpdateAllDuties();
                }
                leadScout.rotationTracker.FaceCell(leadScout.Map.Center);
                progressBar ??= EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
                progressBar.EffectTick(leadScout, TargetInfo.Invalid);
                var mote = ((SubEffecter_ProgressBar)progressBar.children[0]).mote;
                mote.progress = Mathf.Clamp01((float)ticksPassed / duration);
                mote.offsetZ = -0.5f;
                mote.alwaysShow = true;
                if (scanningMote == null || scanningMote.Destroyed || scanningMote.link1.Target.Thing != leadScout)
                {
                    scanningMote = MoteMaker.MakeAttachedOverlay(leadScout, DefsOf.SS_Mote_Binoculars, Vector3.zero);
                }
                scanningMote.Maintain();
            }
            else
            {
                Cleanup();
            }
        }

        public override void Cleanup()
        {
            progressBar?.Cleanup();
            progressBar = null;
            if (scanningMote != null && scanningMote.Destroyed is false) scanningMote.Destroy();
            scanningMote = null;
        }
    }
}
