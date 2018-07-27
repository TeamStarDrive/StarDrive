﻿namespace Ship_Game.Ships
{
    public struct ShipRessuply
    {
        private readonly Ship Ship;
        private const float OrdnanceThresholdCombat            = 0.05f;
        private const float OrdnanceThresholdNonCombat         = 0.25f;
        private const float ResupplyTroopThreshold             = 0.5f;
        private const float KineticEnergyRatioWithPriority     = 0.9f;
        private const float KineticEnergyRatioWithOutPriority  = 0.6f;
        private const int OrdnanceProductionThresholdPriority  = 200;
        private const int OrdnanceProductionThresholdNonCombat = 100;
        private const int OrdnanceProductionThresholdCombat    = 50;

        public const float ResupplyShuttleOrdnanceThreshold    = 0.5f;
        public const float ShipDestroyThreshold                = 0.5f;
        public const float RepairDroneThreshold                = 0.75f; // internal modules percentage
        public const float RepairDroneRange                    = 20000f;

        public ShipRessuply(Ship ship)
        {
            Ship = ship;
        }

        private static float DamageThreshold(ShipData.Category category)
        {
            float threshold;
            switch (category)
            {
                default: threshold                             = 0.45f; break;
                case ShipData.Category.Unclassified: threshold = 0.35f; break;
                case ShipData.Category.Civilian: threshold     = 0.85f; break;
                case ShipData.Category.Recon: threshold        = 0.65f; break;
                case ShipData.Category.Kamikaze: threshold     = 0.0f; break;
            }
            return threshold;
        }

        public ResupplyReason Resupply()
        {
            if (Ship.DesignRole < ShipData.RoleName.colony || Ship.DesignRole == ShipData.RoleName.troop
                                                           || Ship.DesignRole == ShipData.RoleName.supply
                                                           || Ship.AI.State == AI.AIState.Resupply
                                                           || Ship.AI.State == AI.AIState.ResupplyEscort)
                return ResupplyReason.NotNeeded;

            if (!Ship.hasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged())
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededLowHealth())
                return ResupplyReason.LowHealth;

            if (ResupplyNeededLowOrdnance())
                return ResupplyReason.LowOrdnance;

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

        private bool ResupplyNeededLowHealth()
        {
            return Ship.HealthPercent < DamageThreshold(Ship.shipData.ShipCategory)
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
            float threshold = Ship.InCombat ? OrdnanceThresholdCombat
                                            : OrdnanceThresholdNonCombat;
            return Ship.OrdnancePercent < threshold;
        }

        private bool HighKineticToEnergyRatio()
        {
            if (Ship.OrdinanceMax < 1 || Ship.Weapons.Count == 0)
                return false;

            if (!Ship.InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            int numWeapons = Ship.Weapons.Count(weapon => weapon.Module.Active && !weapon.TruePD);
            int numKineticWeapons = Ship.Weapons.Count(weapon => weapon.Module.Active
                                                                 && weapon.OrdinanceRequiredToFire > 0
                                                                 && !weapon.TruePD);

            float ratioTheshold = Ship.AI.HasPriorityTarget ? KineticEnergyRatioWithPriority
                                                            : KineticEnergyRatioWithOutPriority;

            float ratio = (float)numKineticWeapons / numWeapons;
            if (Ship.AI.HasPriorityTarget && ratio < 1f)
                return false; // if player ordered a specific attack and the ship has energy weapons, continue to fight
            return ratio >= ratioTheshold;
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
            float threshold = Ship.InCombat ? (DamageThreshold(Ship.shipData.ShipCategory) * 1.2f).Clamped(0, 1) : 0.9f;
            return Ship.HealthPercent >= threshold && Ship.hasCommand;
        }

        private bool OrdnanceOk()
        {
            float threshold = Ship.InCombat ? OrdnanceThresholdCombat * 4
                                            : 0.99f;

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
        LowOrdnance,
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