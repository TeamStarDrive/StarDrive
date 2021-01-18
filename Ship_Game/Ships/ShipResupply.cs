using System;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public struct ShipResupply // Created by Fat Bastard to centralize all ship supply logic
    {
        private readonly Ship Ship;
        public const float OrdnanceThresholdCombat             = 0.1f;
        public const float OrdnanceThresholdNonCombat          = 0.35f;
        public const float OrdnanceThresholdNonCombatOrbital   = 0.85f;
        public const float KineticToEnergyRatio                = 0.6f;
        private const int OrdnanceProductionThresholdPriority  = 400;
        private const int OrdnanceProductionThresholdNonCombat = 150;
        private const int OrdnanceProductionThresholdCombat    = 75;
        public const Status ResupplyShuttleOrdnanceThreshold   = Status.Average;

        public const float ShipDestroyThreshold = 0.5f;
        public const float RepairDroneThreshold = 0.9f;
        public const float RepairDoneThreshold  = 0.9f;
        public const float RepairDroneRange     = 20000f;
        public Map<SupplyType, float> IncomingSupply;
        private bool InCombat;

        public ShipResupply(Ship ship)
        {
            Ship           = ship;
            InCombat       = false;
            IncomingSupply = new Map<SupplyType, float>();
            foreach (SupplyType supply in Enum.GetValues(typeof(SupplyType)))
                IncomingSupply.Add(supply, 0);
        }

        private static float DamageThreshold(ShipData.Category category)
        {
            float threshold;
            switch (category)
            {
                default:
                case ShipData.Category.Civilian:     threshold = 0.95f; break;
                case ShipData.Category.Recon:        threshold = 0.85f; break;
                case ShipData.Category.Neutral:      threshold = 0.75f; break;
                case ShipData.Category.Unclassified: threshold = 0.7f;  break;
                case ShipData.Category.Conservative: threshold = 0.8f;  break;
                case ShipData.Category.Reckless:     threshold = 0.5f;  break;
                case ShipData.Category.Kamikaze:     threshold = 0.0f;  break;
            }

            threshold = threshold * (1 - ShipDestroyThreshold) + ShipDestroyThreshold;
            return threshold;
        }

        public ResupplyReason Resupply(bool forceSupplyStateCheck = false)
        {
            if (Ship.DesignRole == ShipData.RoleName.construction 
                || Ship.DesignRole == ShipData.RoleName.troop
                || Ship.DesignRole == ShipData.RoleName.supply
                || Ship.AI.HasPriorityOrder && Ship.AI.State != AIState.Bombard)
            {
                return ResupplyReason.NotNeeded;
            }

            InCombat = Ship.InCombat || Ship.AI.State == AIState.Bombard;
            // this saves calculating supply again for ships already in supply states. 
            // but sometimes we want to get the reason (like displaying it to the player when he selects a ship in resupply)
            if (!forceSupplyStateCheck && (Ship.AI.State == AIState.Resupply
                                       || Ship.AI.State == AIState.ResupplyEscort))
            {
                return ResupplyReason.NotNeeded;
            }

            if (!Ship.hasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged())
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededLowHealth())
                return ResupplyReason.LowHealth;

            if (ResupplyNeededLowOrdnance())
                return InCombat ? ResupplyReason.LowOrdnanceCombat : ResupplyReason.LowOrdnanceNonCombat;

            if (ResupplyNeededLowTroops())
                return ResupplyReason.LowTroops;

            /*
            if (ResupplyNeededOrdnanceNotFull())
                return ResupplyReason.RequestResupplyFromPlanet; */

            return ResupplyReason.NotNeeded;
        }

        public bool DoneResupplying(SupplyType supplyType = SupplyType.All)
        {
            switch (supplyType)
            {
                case SupplyType.Rearm:  return OrdnanceOk();
                case SupplyType.Repair: return HealthOk();
                case SupplyType.Troops: return TroopsOk();
                default:                return HealthOk() && OrdnanceOk() && TroopsOk();
            }
        }

        public void ResupplyFromButton()
        {
            if (Ship.IsHangarShip)
                Ship.AI.OrderReturnToHangar();
            else
                Ship.AI.GoOrbitNearestPlanetAndResupply(true);
        }

        private bool ResupplyNeededLowHealth()
        {
            if (Ship.InternalSlotsHealthPercent < ShipDestroyThreshold) // ship is dying or in init
                return false;

            return Ship.InternalSlotsHealthPercent < DamageThreshold(Ship.shipData.ShipCategory)
                   && !Ship.AI.HasPriorityTarget;
        }

        private bool ResupplyNeededLowOrdnance()
        {
            if (Ship.OrdinanceMax < 1)
                return false;

            return OrdnanceLow() && HighKineticToEnergyRatio()
                                 && InsufficientOrdnanceProduction();
        }

        // FB - Disabled for now - done in systems by geodetic manager
        private bool ResupplyNeededOrdnanceNotFull() 
        {
            if (Ship.InCombat
                || Ship.OrdinanceMax < 1
                || Ship.loyalty.isFaction
                || Ship.IsPlatformOrStation
                || !Ship.IsHomeDefense
                || !Ship.IsHangarShip
                || Ship.OrdAddedPerSecond > 0
                || Ship.OrdnancePercent > 0.99f)
            {
                return false;
            }

            return true;
        }

        private bool ResupplyNeededLowTroops()
        {
            // Logic shortcuts
            if (Ship.TroopCapacity < 1
                || !Ship.Carrier.SendTroopsToShip && (Ship.AI.HasPriorityTarget || PlayerKamikaze))
            {
                return false;
            }

            float resupplyTroopThreshold = 0;
            if (Ship.Carrier.SendTroopsToShip)
                resupplyTroopThreshold = Ship.InCombat ? 0.25f : 0.99f;

            if (Ship.Carrier.HasTroopBays) // Counting troops in missions as well for troop carriers
                return (Ship.Carrier.TroopsMissingVsTroopCapacity).LessOrEqual(resupplyTroopThreshold);

            // Ships with Barracks only
            return ((float)Ship.TroopCount / Ship.TroopCapacity).LessOrEqual(resupplyTroopThreshold) && !Ship.InCombat;
        }

        private bool OrdnanceLow()
        {
            if (PlayerKamikaze)
                return false; // Only player manual command will convince Kamikaze ship to resupply

            float threshold = InCombat 
                              ? OrdnanceThresholdCombat 
                              : Ship.IsPlatformOrStation ? OrdnanceThresholdNonCombatOrbital : OrdnanceThresholdNonCombat;
            
            return Ship.OrdnancePercent < threshold;
        }

        private bool HighKineticToEnergyRatio()
        {
            if (Ship.OrdinanceMax < 1 || Ship.Weapons.IsEmpty && Ship.BombBays.IsEmpty)
                return false;

            if (!InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            int numWeapons        = Ship.Weapons.Count(weapon => weapon.Module.Active && !weapon.TruePD);
            int numKineticWeapons = Ship.Weapons.Count(weapon => weapon.Module.Active
                                                                 && weapon.OrdinanceRequiredToFire > 0
                                                                 && !weapon.TruePD);

            if (Ship.AI.HasPriorityTarget && numKineticWeapons < numWeapons)
                return false; // if player ordered a specific attack and the ship has energy weapons, continue to fight

            float ratio = (float)numKineticWeapons / numWeapons;
            return ratio.GreaterOrEqual(KineticToEnergyRatio); 
        }

        private bool InsufficientOrdnanceProduction()
        {
            if (Ship.OrdAddedPerSecond < 1 && Ship.OrdinanceMax > 0)
                return true;

            if (Ship.OrdinanceMax < 1)
                return false; // does not care about ordnance since it has no storage for ordnance

            int productionThreshold;

            if (Ship.AI.HasPriorityTarget)
                productionThreshold = OrdnanceProductionThresholdPriority;
            else
                productionThreshold = InCombat ? OrdnanceProductionThresholdCombat
                                                    : OrdnanceProductionThresholdNonCombat;

            return (Ship.OrdinanceMax - Ship.Ordinance) / Ship.OrdAddedPerSecond > productionThreshold;
        }

        private bool HangarShipReactorsDamaged()
        {
            return Ship.Mothership?.Active == true && Ship.PowerCurrent <= 1f
                                                   && Ship.PowerFlowMax < Ship.PowerDraw;
        }

        private bool HealthOk()
        {
            float threshold = InCombat ? (DamageThreshold(Ship.shipData.ShipCategory) * 1.2f).Clamped(0, 1)
                                       : RepairDoneThreshold;

            float healthTypeToCheck = InCombat ? Ship.InternalSlotsHealthPercent
                                               : Ship.HealthPercent;

            return healthTypeToCheck >= threshold && Ship.hasCommand;
        }

        private bool OrdnanceOk()
        {
            float threshold = InCombat ? OrdnanceThresholdCombat  : OrdnanceThresholdNonCombat;
            return Ship.OrdnancePercent >= threshold;
        }

        private bool TroopsOk()
        {
            if (InCombat || Ship.TroopCapacity == 0)
                return true;

            return Ship.Carrier.TroopsMissingVsTroopCapacity >= 1f;
        }

        private bool PlayerKamikaze => Ship.shipData.ShipCategory == ShipData.Category.Kamikaze && Ship.loyalty.isPlayer;

        public void ChangeIncomingSupply(SupplyType supplyType, float amount)
        {
            float currentIncoming = IncomingSupply[supplyType];
            currentIncoming += amount;
            IncomingSupply[supplyType] = Math.Max(currentIncoming, 0);
        }
        public bool AcceptExternalSupply(SupplyType supplyType)
        {
            switch (supplyType)
            {
                case SupplyType.All:
                    break;
                case SupplyType.Rearm:
                    if (Ship.IsSupplyShip)
                        return false;
                    Status status = ShipStatusWithPendingResupply(supplyType);
                    return status < (Ship.AI.BadGuysNear ? ResupplyShuttleOrdnanceThreshold : Status.Maximum);
                        
                case SupplyType.Repair:
                    break;
                case SupplyType.Troops:
                    break;
            }
            return false;
        }
        public Status ShipStatusWithPendingResupply(SupplyType supplyType)
        {
            float amount = IncomingSupply[supplyType];
            // for easier debugging keeping this as two statements
            return Ship.OrdnanceStatusWithincoming(amount);
        }
    }
    public enum ResupplyReason
    {
        NotNeeded,
        LowHealth,
        LowOrdnanceCombat,
        LowOrdnanceNonCombat,
        LowTroops,
        FighterReactorsDamaged,
        NoCommand,
        RequestResupplyFromPlanet
    }

    public enum SupplyType
    {
        All,
        Rearm,
        Repair,
        Troops
    }
}