using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class MortarExt : Building_TurretGun
    {
        public bool ShouldKeepReady = true;

        // Determines if the mortar is currently ready to fire at a moment's notice
        public bool IsReady
        {
            get
            {
                if (this.gun != null)
                {
                    bool isLoaded = this.IsLoaded;

                    bool coolingDown = this.burstCooldownTicksLeft > 0;
                    KeepMortarsReady.Debug.Log("Cooldown complete: " + !coolingDown);

                    if (!isLoaded || coolingDown)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool IsFueled
        {
            get
            {
                if (this.gun != null)
                {
                    CompRefuelable compRefuelable = this.TryGetComp<CompRefuelable>();
                    if (compRefuelable == null)
                    {
                        return true;
                    }

                    bool hasFuel = compRefuelable.HasFuel;
                    KeepMortarsReady.Debug.Log("Fueled: " + hasFuel);

                    bool classicMortars = Find.Storyteller.difficulty.classicMortars;

                    return hasFuel || classicMortars;
                }
                return true;
            }
        }

        public bool IsLoaded
        {
            get
            {
                if (this.gun != null)
                {
                    CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
                    if (compChangeableProjectile == null)
                    {
                        return true;
                    }

                    ThingDef def = compChangeableProjectile.LoadedShell;
                    KeepMortarsReady.Debug.Log("Loaded: " + def);

                    StorageSettings allowedShellsSettings = compChangeableProjectile.allowedShellsSettings;

                    return compChangeableProjectile.Loaded && allowedShellsSettings.AllowedToAccept(def);
                }
                return true;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.ShouldKeepReady, "ShouldKeepReady", false, false);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (this.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "KeepMortarsReady_CommandKeepReady".Translate(),
                    defaultDesc = "KeepMortarsReady_CommandKeepReadyDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/KeepReady", true),
                    toggleAction = delegate ()
                    {
                        this.ShouldKeepReady = !this.ShouldKeepReady;
                    },
                    isActive = (() => this.ShouldKeepReady)
                };
            }
        }
    }
}
