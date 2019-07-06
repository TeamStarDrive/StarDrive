using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.AI.ShipMovement;
using System;

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
            UpdateTargetPriorities(ship);
            CurrentCombatStance = ship.AI.CombatState;
            SetCombatTactics(ship.AI.CombatState);
        }

        public void UpdateTargetPriorities(Ship ship)
        {
            if (ship.SurfaceArea <= 0)
                return;

            byte pd        = 0;
            byte mains     = 0;
            float fireRate = 0;
            foreach(Weapon w in ship.Weapons)
            {
                if(w.isBeam || w.isMainGun || w.Module.XSIZE*w.Module.YSIZE > 4)
                    mains++;
                if(w.SalvoCount > 2 || w.Tag_PD )
                    pd++;
                fireRate += w.fireDelay;
            }
            if (ship.Weapons.Count > 0)
                fireRate /= ship.Weapons.Count;

            LargeAttackWeight  = mains > 2 ? 3 : fireRate > 0.5 ? 2 : 0;
            SmallAttackWeight  = mains == 0 && fireRate < .1 && pd > 1 ? 3 : 0;
            MediumAttackWeight = mains < 3 && fireRate > .1 ? 3 : 0;
            if (ship.loyalty.isFaction || ship.velocityMaximum > 500)
                VultureWeight  = 2;
            if (ship.loyalty.isFaction)
                PirateWeight   = 3;
            AssistWeight = 0;
        }

        public float ApplyWeight(Ship nearbyShip)
        {
            if (Owner == null) 
                return 0;
            float weight = 0;
            if (nearbyShip == null) 
                return weight;

            if (Owner.AI.Target as Ship == nearbyShip)
                weight += 3;

            if (nearbyShip?.Weapons.Count == 0) 
                weight = weight + PirateWeight;

            if (nearbyShip.Health / nearbyShip.HealthMax < 0.5f) 
                weight += VultureWeight;

            weight += SizeAttackWeight(weight, nearbyShip);
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
            float rangeToTarget = Vector2.Distance(nearbyShip.Center, Owner.Center);

            if (rangeToTarget <= PreferredEngagementDistance)
            {
                weight += (int) Math.Ceiling(5 * ((PreferredEngagementDistance -
                                                   Vector2.Distance(Owner.Center, nearbyShip.Center))
                                                  / PreferredEngagementDistance));
            }
            else if (rangeToTarget > PreferredEngagementDistance + Owner.velocityMaximum * 5)
            {
                weight += weight - 2.5f * (rangeToTarget /
                                           (PreferredEngagementDistance +
                                            Owner.velocityMaximum * 5));
            }
            if (Owner.Mothership != null)
            {
                rangeToTarget = Vector2.Distance(nearbyShip.Center, Owner.Mothership.Center);
                if (rangeToTarget < PreferredEngagementDistance)
                    weight += 1;
            }
            if (Owner.AI.EscortTarget != null)
            {
                rangeToTarget = Vector2.Distance(nearbyShip.Center, Owner.AI.EscortTarget.Center);
                if (rangeToTarget < 5000)
                    weight += 1;
                else
                    weight -= 2;
                if (nearbyShip.AI.Target == Owner.AI.EscortTarget)
                    weight += 1;
            }
            return weight;
        }

        float SizeAttackWeight(float weight, Ship target)
        {
            int surfaceArea = target.SurfaceArea;
            float priority = MediumAttackWeight;
            if (surfaceArea < 30)
                priority = SmallAttackWeight;
            else if (surfaceArea > 100)
                priority = LargeAttackWeight;


            switch (Owner.shipData.ShipCategory)
            {
                case ShipData.Category.Reckless:
                    weight += priority / 2f;
                    break;
                case ShipData.Category.Neutral:
                    weight += priority * 2f;
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

        public void ExecuteCombatTactic(float elapsedTime) => CombatTactic.Execute(elapsedTime, null);

    }
}