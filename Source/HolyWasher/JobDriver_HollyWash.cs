using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace HollyWasher
{
    public class JobDriver_HollyWash : JobDriver_DoBill
    {
        private readonly FieldInfo ApparelWornByCorpseInt = typeof(Apparel).GetField("wornByCorpseInt",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private float workCycle;
        private float workCycleProgress;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override Toil DoBill()
        {
            var actor = GetActor();
            var curJob = actor.jobs.curJob;
            var objectThing = curJob.GetTarget(objectTI).Thing;
            var tableThing = curJob.GetTarget(tableTI).Thing as Building_WorkTable;

            var toil = new Toil
            {
                initAction = delegate
                {
                    curJob.bill.Notify_DoBillStarted(actor);
                    workCycleProgress = workCycle = Math.Max(curJob.bill.recipe.workAmount, 10f);
                },
                tickAction = delegate
                {
                    if (objectThing == null || objectThing.Destroyed)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }

                    workCycleProgress -= actor.GetStatValue(StatDefOf.WorkToMake);

                    tableThing?.UsedThisTick();
                    //if (!tableThing.UsableNow)
                    //{
                    //    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    //}

                    if (!(workCycleProgress <= 0))
                    {
                        return;
                    }

                    var skillDef = curJob.RecipeDef.workSkill;
                    if (skillDef != null)
                    {
                        var skill = actor.skills.GetSkill(skillDef);

                        if (skill != null)
                        {
                            skill.Learn(0.11f * curJob.RecipeDef.workSkillLearnFactor);
                        }
                    }

                    actor.GainComfortFromCellIfPossible();

                    if (objectThing is Apparel mendApparel)
                    {
                        ApparelWornByCorpseInt.SetValue(mendApparel, false);
                    }

                    var list = new List<Thing> { objectThing };
                    curJob.bill.Notify_IterationCompleted(actor, list);

                    workCycleProgress = workCycle;
                    ReadyForNextToil();
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };


            toil.WithEffect(() => curJob.bill.recipe.effectWorking, tableTI);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(tableTI, () => objectThing.HitPoints / (float)objectThing.MaxHitPoints,
                false, 0.5f);
            toil.FailOn(() => curJob.bill.suspended || curJob.bill.DeletedOrDereferenced ||
                              curJob.GetTarget(tableTI).Thing is IBillGiver billGiver &&
                              !billGiver.CurrentlyUsableForBills());
            return toil;
        }
    }
}