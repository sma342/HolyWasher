using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace HollyWasher
{
	public class JobDriver_HollyWash : JobDriver_DoBill {
	public override bool TryMakePreToilReservations(bool errorOnFailed)
{
    return true;
}
	
    	
    
        private FieldInfo ApparelWornByCorpseInt = typeof(Apparel).GetField("wornByCorpseInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private float workCycle;
        private float workCycleProgress;

        protected override Toil DoBill()
        {

            Pawn actor = GetActor();
            Job curJob = actor.jobs.curJob;
            Thing objectThing = curJob.GetTarget(objectTI).Thing;
            Building_WorkTable tableThing = curJob.GetTarget(tableTI).Thing as Building_WorkTable;

            Toil toil = new Toil();
            toil.initAction = delegate
            {
                curJob.bill.Notify_DoBillStarted(actor);
                this.workCycleProgress = this.workCycle = Math.Max(curJob.bill.recipe.workAmount, 10f);

            };
            toil.tickAction = delegate
            {
                if (objectThing == null || objectThing.Destroyed)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }

                workCycleProgress -= StatExtension.GetStatValue(actor, StatDefOf.WorkToMake, true);

                tableThing.UsedThisTick();
                //if (!tableThing.UsableNow)
                //{
                //    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                //}

                if (workCycleProgress <= 0)
                {

                    float skillPerc = 0.5f;

                    SkillDef skillDef = curJob.RecipeDef.workSkill;
                    if (skillDef != null)
                    {
                        SkillRecord skill = actor.skills.GetSkill(skillDef);

                        if (skill != null)
                        {
                            skillPerc = (float)skill.Level / 20f;

                            skill.Learn(0.11f * curJob.RecipeDef.workSkillLearnFactor);
                        }
                    }

                    actor.GainComfortFromCellIfPossible();

                    Apparel mendApparel = objectThing as Apparel;
                    if (mendApparel != null)
                    {
                        ApparelWornByCorpseInt.SetValue(mendApparel, false);
                    }

                    List<Thing> list = new List<Thing>();
                    list.Add(objectThing);
                    curJob.bill.Notify_IterationCompleted(actor, list);

                    workCycleProgress = workCycle;
                    ReadyForNextToil();
                }
            };


            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.WithEffect(() => curJob.bill.recipe.effectWorking, tableTI);
            toil.PlaySustainerOrSound(() => toil.actor.CurJob.bill.recipe.soundWorking);
            toil.WithProgressBar(tableTI, delegate
            {
                return (float)objectThing.HitPoints / (float)objectThing.MaxHitPoints;
            }, false, 0.5f);
            toil.FailOn(() =>
            {
                IBillGiver billGiver = curJob.GetTarget(tableTI).Thing as IBillGiver;

                return curJob.bill.suspended || curJob.bill.DeletedOrDereferenced || (billGiver != null && !billGiver.CurrentlyUsableForBills());
            });
            return toil;
        }
    }
}
