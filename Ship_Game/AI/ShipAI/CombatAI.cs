using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

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

        public CombatAI()
        {
        }

        public CombatAI(Ship ship)
        {
            Owner = ship;
            UpdateCombatAI(ship);
        }

        public void UpdateCombatAI(Ship ship)
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
           // SetFleetWeights();
        }

        //public void SetFleetWeights()
        //{
        //    var ship = Owner;
        //    FleetDataNode node = Owner?.AI.FleetNode;
        //    if (Owner?.fleet == null || node == null)
        //    {
        //        PreferredEngagementDistance = ship.maxWeaponsRange * 0.75f;
        //        return;
        //    }

        //    VultureWeight               = node.VultureWeight;
        //    PreferredEngagementDistance = node.OrdersRadius;
        //    SelfDefenseWeight           = node.DefenderWeight;
        //    LargeAttackWeight          += LargeAttackWeight * node.SizeWeight;
        //    SmallAttackWeight          += SmallAttackWeight * (1 - node.SizeWeight);
        //    MediumAttackWeight         += MediumAttackWeight * -Math.Abs(node.SizeWeight);
        //    PirateWeight               -= node.DPSWeight;
        //    AssistWeight               += node.AssistWeight;
        //}

        public float ApplyWeight(Ship nearbyShip)
        {
            if (Owner == null) return 0;
            float weight = 0;
            if (nearbyShip == null) return weight;

            if (Owner.AI.Target as Ship == nearbyShip)
                weight += 3;
            if (nearbyShip?.Weapons.Count == 0)
            {
                weight = weight + PirateWeight;
            }

            if (nearbyShip.Health / nearbyShip.HealthMax < 0.5f)
            {
                weight = weight + VultureWeight;
            }
            int surfaceArea = nearbyShip.SurfaceArea;
            if (surfaceArea < 30)
            {
                switch (Owner.shipData.ShipCategory)
                {
                    case ShipData.Category.Fighter:
                        weight += SmallAttackWeight * 2f;
                        break;
                    case ShipData.Category.Bomber:
                        weight += SmallAttackWeight / 2f;
                        break;
                    default:
                        weight += SmallAttackWeight;
                        break;
                }
            }
            if (surfaceArea > 30 && surfaceArea < 100)
            {                
                weight += Owner.shipData.ShipCategory == ShipData.Category.Bomber ?  MediumAttackWeight *= 1.5f : MediumAttackWeight;                
            }
            if (surfaceArea > 100)
            {
                switch (Owner.shipData.ShipCategory) {
                    case ShipData.Category.Fighter:
                        weight += LargeAttackWeight /2f;
                        break;
                    case ShipData.Category.Bomber:
                        weight += LargeAttackWeight * 2f;
                        break;
                    default:
                        weight += LargeAttackWeight;
                        break;
                }
            }
            float rangeToTarget = Vector2.Distance(nearbyShip.Center, Owner.Center);

            if (rangeToTarget <= PreferredEngagementDistance)
            {
                weight += (int)Math.Ceiling(5 * ((PreferredEngagementDistance -
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
            if (nearbyShip.Weapons.Count < 1)
                weight -= 3;
            if (nearbyShip.AI.Target == Owner)
                weight += SelfDefenseWeight;
            if (nearbyShip.AI.Target?.GetLoyalty() == Owner.loyalty)
            {
                weight += AssistWeight;
            }
            return weight;
        }
    }
}