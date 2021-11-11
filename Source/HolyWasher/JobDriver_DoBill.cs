using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HollyWasher
{
    public abstract class JobDriver_DoBill : JobDriver
    {
        public const TargetIndex tableTI = TargetIndex.A;
        public const TargetIndex objectTI = TargetIndex.B;
        public const TargetIndex haulTI = TargetIndex.C;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(tableTI);
            this.FailOnBurningImmobile(objectTI);
            this.FailOnDestroyedNullOrForbidden(objectTI);
            this.FailOnBurningImmobile(objectTI);
            yield return Toils_Reserve.Reserve(tableTI);
            yield return Toils_Reserve.Reserve(objectTI);
            yield return Toils_Goto.GotoThing(objectTI, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(objectTI);
            yield return Toils_Goto.GotoThing(tableTI, PathEndMode.InteractionCell);
            yield return Toils_Haul.PlaceHauledThingInCell(tableTI, null, false);
            yield return DoBill();
            yield return Store();
            yield return Toils_Reserve.Reserve(haulTI);
            yield return Toils_Haul.CarryHauledThingToCell(haulTI);
            yield return Toils_Haul.PlaceHauledThingInCell(haulTI, null, false);
            yield return Toils_Reserve.Release(objectTI);
            yield return Toils_Reserve.Release(haulTI);
            yield return Toils_Reserve.Release(tableTI);
        }

        protected abstract Toil DoBill();

        private Toil Store()
        {
            var toil = new Toil();
            toil.initAction = delegate
            {
                var actor = toil.actor;
                var curJob = actor.jobs.curJob;
                var objectThing = curJob.GetTarget(objectTI).Thing;

                if (curJob.bill.GetStoreMode() != BillStoreModeDefOf.DropOnFloor)
                {
                    if (StoreUtility.TryFindBestBetterStoreCellFor(objectThing, actor, actor.Map,
                        StoragePriority.Unstored, actor.Faction, out var vec))
                    {
                        actor.carryTracker.TryStartCarry(objectThing, 1);
                        curJob.SetTarget(haulTI, vec);
                        curJob.count = 99999;
                        return;
                    }
                }

                actor.carryTracker.TryStartCarry(objectThing, 1);
                actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out objectThing);

                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
            };
            return toil;
        }

        public override string GetReport()
        {
            if (pawn.jobs.curJob.RecipeDef != null)
            {
                return base.ReportStringProcessed(pawn.jobs.curJob.RecipeDef.jobString);
            }

            return base.GetReport();
        }
    }
}