using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using KeepMortarsReady;

namespace RimWorld
{
    public class JobDriver_ReadyMortar : JobDriver_ManTurret
    {
        public static bool GunNeedsRefueling(Building b)
        {
            Building_TurretGun building_TurretGun = b as Building_TurretGun;
            if (building_TurretGun == null)
            {
                return false;
            }
            CompRefuelable compRefuelable = building_TurretGun.TryGetComp<CompRefuelable>();
            return compRefuelable != null && !compRefuelable.HasFuel && compRefuelable.Props.fuelIsMortarBarrel && !Find.Storyteller.difficulty.classicMortars;
        }

        public static bool GunNeedsLoading(Building b)
        {
            Building_TurretGun building_TurretGun = b as Building_TurretGun;
            if (building_TurretGun == null)
            {
                return false;
            }
            CompChangeableProjectile compChangeableProjectile = building_TurretGun.gun.TryGetComp<CompChangeableProjectile>();
            return compChangeableProjectile != null && !compChangeableProjectile.Loaded;
        }

        // We override this method just to hook into the "man" toil's tickAction
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil gotoTurret = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil checkForFuel = new Toil();
            Toil checkForAmmo = new Toil();

            checkForFuel.initAction = delegate ()
            {
                Pawn actor = checkForFuel.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                Building_TurretGun building_TurretGun = building as Building_TurretGun;
                if (JobDriver_ReadyMortar.GunNeedsRefueling(building))
                {
                    Thing fuel = JobDriver_ManTurret.FindFuelForTurret(this.pawn, building_TurretGun);
                    if (fuel == null)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                    }
                    else
                    {
                        actor.CurJob.targetB = fuel;
                    }
                }
                else
                {
                    this.JumpToToil(checkForAmmo);
                }
            };

            checkForAmmo.initAction = delegate ()
            {
                Pawn actor = checkForAmmo.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                Building_TurretGun building_TurretGun = building as Building_TurretGun;
                if (JobDriver_ReadyMortar.GunNeedsLoading(building))
                {
                    Thing ammo = JobDriver_ManTurret.FindAmmoForTurret(this.pawn, building_TurretGun);
                    if (ammo == null)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                    }
                    else
                    {
                        actor.CurJob.targetB = ammo;
                    }
                }
                else
                {
                    this.JumpToToil(gotoTurret);
                }
            };

            yield return checkForFuel;
            yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1, null);
            yield return checkForAmmo;
            yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1, null);
            yield return gotoTurret;

            foreach (Toil toil in base.MakeNewToils())
            {
                if (toil.defaultCompleteMode == ToilCompleteMode.Never && toil.tickAction != null)
                {
                    Debug.Log("Found man toil");

                    Action action = (Action) toil.tickAction.Clone();
                    toil.tickAction = delegate ()
                    {
                        action();

                        // During each tick while the mortar is manned, we need to check if it is ready to fire so we can stop the job
                        // before the mortar actually fires
                        MortarExt mortar = toil.actor.CurJob.targetA.Thing as MortarExt;
                        if (mortar.IsReady)
                        {
                            toil.actor.jobs.EndCurrentJob(JobCondition.Succeeded, true, true);
                        }
                        else if (!mortar.ShouldKeepReady)
                        {
                            toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                        }
                    };
                }
                yield return toil;
            }
        }
    }
}
