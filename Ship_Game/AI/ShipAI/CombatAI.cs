using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.AI.ShipMovement;
using System;
using static Ship_Game.AI.ShipAI;
using static Ship_Game.AI.ShipAI.TargetParameterTotals;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        public float VultureWeight               = 1;
        public float SelfDefenseWeight           = 1f;
        public float SmallAttackWeight           = 1f;
        public float MediumAttackWeight          = 1f;
        public float LargeAttackWeight           = 3f;
        public float PreferredEngagementDistance = 1500f;
        public float PirateWeight;
        private float AssistWeight;
        public Ship Owner;
        ShipAIPlan CombatTactic;
        CombatState CurrentCombatStance;

        public CombatAI()
        {
        }

        public CombatAI(Ship ship)
        {
            Owner = ship;
            UpdateTargetPriorities();
            CurrentCombatStance = ship.AI.CombatState;
            SetCombatTactics(ship.AI.CombatState);
        }

        public void ClearTargets()
        {
            Owner.AI.Target = null;
            Owner.AI.PotentialTargets.Clear();
        }

        public void UpdateTargetPriorities()
        {
            var ship = Owner;
            if (ship.SurfaceArea <= 0)
                return;

            int pd        = 0;
            int mains     = 0;
            int other     = 0;

            foreach(Weapon w in ship.Weapons)
            {
                if (w.isBeam || w.isMainGun || w.Module.XSIZE * w.Module.YSIZE > 4)
                {
                    mains += w.Module.XSIZE * w.Module.YSIZE;
                }
                else if (w.SalvoCount > 2 || w.Tag_PD || w.Tag_Flak)
                {
                    pd += w.Module.XSIZE * w.Module.YSIZE;
                }
                else if (w.DamageAmount > 0)
                    other++;
            }

            other += Owner.Carrier.AllFighterHangars.Length;

            LargeAttackWeight  = mains;
            SmallAttackWeight  = pd - mains;
            MediumAttackWeight = other;

            if (ship.loyalty.isFaction || ship.VelocityMaximum > 500)
                VultureWeight  = 2;
            if (ship.loyalty.isFaction)
                PirateWeight   = 3;
            AssistWeight = Owner.Mothership != null ? 1 : 0;
        }

        public float ApplyWeight(Ship nearbyShip, TargetParameterTotals nearbyTotals)
        {
            if (Owner == null) 
                return 0;
            float weight = 0;
            if (nearbyShip == null) 
                return weight;

            if (Owner.AI.Target == nearbyShip)
                weight += 1.5f;

            if (nearbyShip?.Weapons.Count == 0) 
                weight += PirateWeight;

            weight += VultureWeight * (1 - nearbyShip.HealthPercent);
            weight += SizeAttackWeight(weight, nearbyShip, nearbyTotals);
            weight += RangeWeight(nearbyShip, weight);
           
            if (nearbyShip.Weapons.Count < 1)
                weight -= 3;

            if (nearbyShip.AI.Target == Owner)
                weight += SelfDefenseWeight;

            if (nearbyShip.AI.Target?.GetLoyalty() == Owner.loyalty) 
                weight += AssistWeight;

            return weight;
        }

        private float RangeWeight(Ship nearbyShip, float weight)
        {
            float rangeToTarget = Owner.Center.Distance(nearbyShip.Center);
            if (Owner.Mothership != null)            
                rangeToTarget = Owner.Mothership.Center.Distance(nearbyShip.Center);
            
            if (Owner.AI.EscortTarget != null)            
                rangeToTarget = Owner.AI.EscortTarget.Center.Distance(nearbyShip.Center);
            

            if (rangeToTarget <= PreferredEngagementDistance)
            {
                weight += (int) Math.Ceiling(5 * (rangeToTarget / PreferredEngagementDistance));
            }
            else 
            {
                weight -= (int)Math.Ceiling(5 * (rangeToTarget / PreferredEngagementDistance));
            }       
            
            return weight;
        }

        float SizeAttackWeight(float weight, Ship target, TargetParameterTotals nearbyTargets)
        {
            float avgNearBySize = nearbyTargets.GetCharateristic(Paramter.Size);
            int surfaceArea = target.SurfaceArea;
            float priority = MediumAttackWeight;

            if (surfaceArea < avgNearBySize * 0.75f)
                priority = SmallAttackWeight;
            else if (surfaceArea > avgNearBySize * 1.25f)
                priority = LargeAttackWeight;


            switch (Owner.shipData.ShipCategory)
            {
                case ShipData.Category.Reckless:
                    weight += priority / 2f;
                    break;
                default:
                    weight += priority;
                    break;
            }
            weight *= target.DesignRole < ShipData.RoleName.troop ? 0.2f : 1;
            return weight;
        }

        public void SetCombatTactics(CombatState combatState)
        {
            if (CurrentCombatStance != combatState)
            {
                CurrentCombatStance = combatState;
                CombatTactic = null;
                Owner.shipStatusChanged = true; // FIX: force DesiredCombatRange update
            }

            if (CombatTactic == null)
            {
                switch (combatState)
                {
                    case CombatState.Artillery:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                    case CombatState.BroadsideLeft:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.BroadsideRight:
                        CombatTactic = new CombatTactics.BroadSides(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.OrbitLeft:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.OrbitRight:
                        CombatTactic = new CombatTactics.OrbitTarget(Owner.AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.AttackRuns:
                        CombatTactic = new CombatTactics.AttackRun(Owner.AI);
                        break;
                    case CombatState.HoldPosition:
                        CombatTactic = new CombatTactics.HoldPosition(Owner.AI);
                        break;
                    case CombatState.Evade:
                        CombatTactic = new CombatTactics.Evade(Owner.AI);
                        break;
                    case CombatState.AssaultShip:
                        CombatTactic = new CombatTactics.AssaultShipCombat(Owner.AI);
                        break;
                    case CombatState.OrbitalDefense:
                        break;
                    case CombatState.ShortRange:
                        CombatTactic = new CombatTactics.Artillery(Owner.AI);
                        break;
                }

            }
        }

        public void ExecuteCombatTactic(FixedSimTime timeStep) => CombatTactic?.Execute(timeStep, null);

    }
}