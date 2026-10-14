using System.Linq;
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
        private ScopeKind opticKind;
        private int ticksPassed;
        private Effecter progressBar;
        private Pawn currentLeadScout;
        private Mote scanningMote;
        private Rot4 moteRotation = Rot4.Invalid;

        public LordToil_EnemyScoutObserve(IntVec3 spot, int duration, ScopeKind opticKind)
        {
            this.spot = spot;
            this.duration = duration;
            this.opticKind = opticKind;
        }

        private ThingDef OpticMoteDef => opticKind == ScopeKind.Telescope ? DefsOf.SS_Mote_Spyglass : DefsOf.SS_Mote_Binoculars;

        private ThingDef OpticItemDef => opticKind == ScopeKind.Telescope ? DefsOf.SS_Telescope : DefsOf.SS_Binoculars;

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
                    EquipOptic(leadScout);
                }
                leadScout.rotationTracker.FaceCell(leadScout.Map.Center);
                progressBar ??= EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
                progressBar.EffectTick(leadScout, TargetInfo.Invalid);
                var mote = ((SubEffecter_ProgressBar)progressBar.children[0]).mote;
                mote.progress = Mathf.Clamp01((float)ticksPassed / duration);
                mote.offsetZ = -0.5f;
                mote.alwaysShow = true;
                if (scanningMote == null || scanningMote.Destroyed || scanningMote.link1.Target.Thing != leadScout || moteRotation != leadScout.Rotation)
                {
                    if (scanningMote != null && scanningMote.Destroyed is false) scanningMote.Destroy();
                    moteRotation = leadScout.Rotation;
                    scanningMote = MoteMaker.MakeAttachedOverlay(leadScout, OpticMoteDef, ScopeView.MoteOffsetFor(moteRotation));
                }
                scanningMote.Maintain();
            }
            else
            {
                Cleanup();
            }
        }

        private void EquipOptic(Pawn scout)
        {
            var itemDef = OpticItemDef;
            if (scout.inventory.innerContainer.Any(t => t.def == itemDef)) return;
            var item = ThingMaker.MakeThing(itemDef);
            scout.inventory.innerContainer.TryAdd(item);
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
