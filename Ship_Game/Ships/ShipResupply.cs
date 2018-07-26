using System;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public static class ShipResupply // Created by Fat Bastard to deal with resupply logic. July 2018
    {
        private const float OrdnanceThresholdCombat            = 0.05f;
        private const float OrdnanceThresholdNonCombat         = 0.5f;
        private const float ResupplyTroopThreshold             = 0.5f;
        private const float KineticEnergyRatioWithPriority     = 0.9f;
        private const float KineticEnergyRatioWithOutPriority  = 0.6f;
        private const int OrdnanceProductionThresholdPriority  = 200;
        private const int OrdnanceProductionThresholdNonCombat = 100;
        private const int OrdnanceProductionThresholdCombat    = 50;
        public const float ResupplyShuttleOrdnanceThreshold    = 0.5f;
        public const float ShipDestroyThreshold                = 0.5f;

        private static float DamageThreshold(ShipData.Category category)
        {
            float threshold;
            switch (category)
            {
                default:                             threshold = 0.45f; break;
                case ShipData.Category.Unclassified: threshold = 0.35f; break;
                case ShipData.Category.Civilian:     threshold = 0.85f; break;
                case ShipData.Category.Recon:        threshold = 0.65f; break;
                case ShipData.Category.Kamikaze:     threshold = 0.0f;  break;
            }
            return threshold;
        }

        public static ResupplyReason Resupply(Ship ship)
        {
            if (ship.DesignRole < ShipData.RoleName.colony || ship.DesignRole == ShipData.RoleName.troop 
                                                           || ship.AI.State == AI.AIState.Resupply)
                return ResupplyReason.NotNeeded;

            if (!ship.hasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged(ship))
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededLowHealth(ship))
                return ResupplyReason.LowHealth;

            if (ResupplyNeededLowOrdnance(ship))
                return ResupplyReason.LowOrdnance;

            if (ResupplyNeededLowTroops(ship))
                return ResupplyReason.LowTroops;

            return ResupplyReason.NotNeeded;
        }

        public static bool DoneResupplying(Ship ship)
        {
            return HealthOk(ship) && OrdnanceOk(ship) && TroopsOk(ship);
        }

        private static bool ResupplyNeededLowHealth(Ship ship)
        {
            return ship.HealthPercent < DamageThreshold(ship.shipData.ShipCategory)
                   && !ship.AI.HasPriorityTarget;
        }

        private static bool ResupplyNeededLowOrdnance(Ship ship)
        {
            return OrdnanceLow(ship) && HighKineticToEnergyRatio(ship) 
                                     && CanNotAddEnoughOrdnance(ship);
        }

        private static bool ResupplyNeededLowTroops(Ship ship)
        {
            if (ship.AI.HasPriorityTarget)
                return false;

            return ship.Carrier.TroopsMissingVsTroopCapacity < ResupplyTroopThreshold; 
        }

        private static bool OrdnanceLow(Ship ship)
        {
            float threshold = ship.InCombat ? OrdnanceThresholdCombat 
                                            : OrdnanceThresholdNonCombat;
            return ship.OrdnancePercent < threshold;
        }

        private static bool HighKineticToEnergyRatio(Ship ship)
        {
            if (ship.OrdinanceMax < 1 || ship.Weapons.Count == 0)
                return false;

            if (!ship.InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            int numWeapons        = ship.Weapons.Count(weapon => weapon.Module.Active && !weapon.TruePD);
            int numKineticWeapons = ship.Weapons.Count(weapon => weapon.Module.Active 
                                                                 && weapon.OrdinanceRequiredToFire > 0
                                                                 && !weapon.TruePD);

            float ratioTheshold = ship.AI.HasPriorityTarget ? KineticEnergyRatioWithPriority 
                                                           : KineticEnergyRatioWithOutPriority;

            float ratio = (float)numKineticWeapons / numWeapons;
            if (ship.AI.HasPriorityTarget && ratio < 1f)
                return false; // if player ordered a specific attack and the ship has energy weapons, continue to fight
            return ratio >= ratioTheshold;
        }

        private static bool CanNotAddEnoughOrdnance(Ship ship)
        {
            if (ship.OrdAddedPerSecond < 1 || ship.OrdinanceMax > 0)
                return true;

            if (ship.OrdinanceMax < 1)
                return false; // doesnt care about ordnance since it has no storage for ordnance

            int productionThreshold;

            if (ship.AI.HasPriorityTarget)
                productionThreshold = OrdnanceProductionThresholdPriority;
            else
                productionThreshold = ship.InCombat ? OrdnanceProductionThresholdCombat 
                                                            : OrdnanceProductionThresholdNonCombat;

            return ship.OrdinanceMax / ship.OrdAddedPerSecond > productionThreshold;
        }

        private static bool HangarShipReactorsDamaged(Ship ship)
        {
            return ship.Mothership?.Active == true && ship.PowerCurrent <= 1f 
                                                   && ship.PowerFlowMax < ship.PowerDraw;
        }

        private static bool HealthOk(Ship ship)
        {
            float threshold = ship.InCombat ? (DamageThreshold(ship.shipData.ShipCategory) * 1.2f).Clamped(0,1) : 0.99f;
            return ship.HealthPercent >= threshold;
        }

        private static bool OrdnanceOk(Ship ship)
        {
            float threshold = ship.InCombat ? OrdnanceThresholdCombat * 2 
                                            : OrdnanceThresholdNonCombat;

            return ship.OrdnancePercent >= threshold;
        }

        private static bool TroopsOk(Ship ship)
        {
            if (ship.InCombat || ship.TroopCapacity == 0)
                return true;

            return ship.Carrier.TroopsMissingVsTroopCapacity  >= 1f;
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
}