using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public struct ShipResupply
    {
        private readonly Ship Ship;
        public const float OrdnanceThresholdCombat             = 0.01f;
        public const float OrdnanceThresholdNonCombat          = 0.1f;
        public const float OrdnanceThresholdSupplyShipsNear    = 0.5f;
        private const float ResupplyTroopThreshold             = 0.5f;
        private const float KineticEnergyRatioWithPriority     = 0.9f;
        private const float KineticEnergyRatioWithOutPriority  = 0.6f;
        private const int OrdnanceProductionThresholdPriority  = 200;
        private const int OrdnanceProductionThresholdNonCombat = 100;
        private const int OrdnanceProductionThresholdCombat    = 50;

        public const float ResupplyShuttleOrdnanceThreshold    = 0.5f;
        public const float ShipDestroyThreshold                = 0.5f;
        public const float RepairDroneThreshold                = 0.9f;
        public const float RepairDoneThreshold                 = 0.9f;
        public const float RepairDroneRange                    = 20000f;

        public ShipResupply(Ship ship)
        {
            Ship = ship;
        }

        private static float DamageThreshold(ShipData.Category category)
        {
            float threshold;
            switch (category)
            {
                default:
                case ShipData.Category.Civilian: threshold     = 0.85f; break;
                case ShipData.Category.Recon: threshold        = 0.65f; break;
                case ShipData.Category.Netural: threshold      = 0.5f; break;
                case ShipData.Category.Unclassified: threshold = 0.4f; break;
                case ShipData.Category.Conservative: threshold = 0.35f; break;
                case ShipData.Category.Reckless: threshold     = 0.2f; break;
                case ShipData.Category.Kamikaze: threshold     = 0.0f; break;
            }

            threshold = threshold * (1- ShipDestroyThreshold) + ShipDestroyThreshold;
            return threshold;
        }

        public ResupplyReason Resupply(bool forceSupplyStateCheck = false)
        {
            if (Ship.DesignRole < ShipData.RoleName.colony || Ship.DesignRole == ShipData.RoleName.troop
                                                           || Ship.DesignRole == ShipData.RoleName.supply)
                return ResupplyReason.NotNeeded;

            // this saves calculating supply again for ships already in supply states. 
            // but sometimes we want to get the reason (like displaying it to the player when he selects a ship in resupply)
            if (!forceSupplyStateCheck && (Ship.AI.State == AIState.Resupply
                                       || Ship.AI.State == AIState.ResupplyEscort))
                return ResupplyReason.NotNeeded;

            if (!Ship.hasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged())
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededLowHealth())
                return ResupplyReason.LowHealth;

            if (ResupplyNeededLowOrdnance())
                return Ship.InCombat ? ResupplyReason.LowOrdnanceCombat : ResupplyReason.LowOrdnanceNonCombat;

            if (ResupplyNeededLowTroops())
                return ResupplyReason.LowTroops;

            return ResupplyReason.NotNeeded;
        }

        public bool DoneResupplying(SupplyType supplyType = SupplyType.All)
        {
            switch (supplyType)
            {
                default:
                    return HealthOk() && OrdnanceOk() && TroopsOk();
                case SupplyType.Rearm:
                    return OrdnanceOk();
                case SupplyType.Repair:
                    return HealthOk();
                case SupplyType.Troops:
                    return TroopsOk();
            }
        }

        public void ResupplyFromButton()
        {
            if (Ship.Mothership != null)
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
            return OrdnanceLow() && HighKineticToEnergyRatio()
                                 && InsufficientOrdnanceProduction();
        }

        private bool ResupplyNeededLowTroops()
        {
            if (Ship.AI.HasPriorityTarget)
                return false;

            return Ship.Carrier.TroopsMissingVsTroopCapacity < ResupplyTroopThreshold;
        }

        private bool OrdnanceLow()
        {
            if (Ship.shipData.ShipCategory == ShipData.Category.Kamikaze
                && Ship.loyalty.isPlayer)
                return false; // only player manual command will convince Kamikaze ship to resupply

            float threshold;
            float ordnancePercent = Ship.OrdnancePercent;
            if (Ship.InCombat)
                threshold = OrdnanceThresholdCombat;
            else
            {
                if (ordnancePercent < OrdnanceThresholdSupplyShipsNear
                        && (Ship.Mothership != null || Ship.AI.FriendliesNearby.Any(supply => supply.SupplyShipCanSupply)))
                    return true; // FB: let supply shuttles supply ships with partly depleted reserves

                threshold = OrdnanceThresholdNonCombat;
            }
            return ordnancePercent < threshold;
        }

        private bool HighKineticToEnergyRatio()
        {
            if (Ship.OrdinanceMax < 1 || Ship.Weapons.Count == 0 && Ship.BombCount == 0)
                return false;

            if (!Ship.InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            int numWeapons = Ship.Weapons.Count(weapon => weapon.Module.Active && !weapon.TruePD);
            int numKineticWeapons = Ship.Weapons.Count(weapon => weapon.Module.Active
                                                                 && weapon.OrdinanceRequiredToFire > 0
                                                                 && !weapon.TruePD);

            float ratioThreshold = Ship.AI.HasPriorityTarget ? KineticEnergyRatioWithPriority
                                                             : KineticEnergyRatioWithOutPriority;

            float ratio = (float)numKineticWeapons / numWeapons;
            if (Ship.AI.HasPriorityTarget && ratio < 1f)
                return false; // if player ordered a specific attack and the ship has energy weapons, continue to fight
            return ratio >= ratioThreshold;
        }

        private bool InsufficientOrdnanceProduction()
        {
            if (Ship.OrdAddedPerSecond < 1 && Ship.OrdinanceMax > 0)
                return true;

            if (Ship.OrdinanceMax < 1)
                return false; // doesnt care about ordnance since it has no storage for ordnance

            int productionThreshold;

            if (Ship.AI.HasPriorityTarget)
                productionThreshold = OrdnanceProductionThresholdPriority;
            else
                productionThreshold = Ship.InCombat ? OrdnanceProductionThresholdCombat
                                                    : OrdnanceProductionThresholdNonCombat;

            return Ship.OrdinanceMax / Ship.OrdAddedPerSecond > productionThreshold;
        }

        private bool HangarShipReactorsDamaged()
        {
            return Ship.Mothership?.Active == true && Ship.PowerCurrent <= 1f
                                                   && Ship.PowerFlowMax < Ship.PowerDraw;
        }

        private bool HealthOk()
        {
            float threshold     = Ship.InCombat ? (DamageThreshold(Ship.shipData.ShipCategory) * 1.2f).Clamped(0, 1) 
                                                : RepairDoneThreshold;

            float healthTypeToCheck = Ship.InCombat ? Ship.InternalSlotsHealthPercent
                                                    : Ship.HealthPercent;

            return healthTypeToCheck >= threshold && Ship.hasCommand;
        }

        private bool OrdnanceOk()
        {
            float threshold = Ship.InCombat ? OrdnanceThresholdCombat * 4 : 0.99f;
            return Ship.OrdnancePercent >= threshold;
        }

        private bool TroopsOk()
        {
            if (Ship.InCombat || Ship.TroopCapacity == 0)
                return true;

            return Ship.Carrier.TroopsMissingVsTroopCapacity >= 1f;
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
        NoCommand
    }

    public enum SupplyType
    {
        All,
        Rearm,
        Repair,
        Troops
    }
}