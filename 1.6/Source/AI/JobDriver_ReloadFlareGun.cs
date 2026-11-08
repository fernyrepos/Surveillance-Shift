using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurveillanceShift
{
    public class JobDriver_ReloadFlareGun : JobDriver
    {
        private CompFlareGun Gun => (job.targetB.Thing as ThingWithComps)?.GetComp<CompFlareGun>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return job.targetA.HasThing is false || pawn.Reserve(job.targetA, job, 1, job.count, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => Gun == null || Gun.loaded);

            if (job.targetA.HasThing)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
                yield return Toils_Haul.TakeToInventory(TargetIndex.A, () => job.count);
            }

            var reload = Toils_General.Wait(Gun?.Props.reloadTicks ?? 120).WithProgressBarToilDelay(TargetIndex.B);
            yield return reload;

            yield return Toils_General.Do(() =>
            {
                var cost = Gun.Props.chemfuelCost;
                var stacks = pawn.inventory.innerContainer.Where(t => t.def == ThingDefOf.Chemfuel).ToList();
                if (stacks.Sum(t => t.stackCount) < cost) return;
                foreach (var stack in stacks)
                {
                    var take = System.Math.Min(cost, stack.stackCount);
                    stack.SplitOff(take).Destroy();
                    cost -= take;
                    if (cost <= 0) break;
                }
                Gun.loaded = true;
            });
        }
    }
}
