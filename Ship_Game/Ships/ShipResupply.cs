using System;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public static class ShipResupply // Created by Fat Bastard to deal with resupply logic
    {
        private const float OrdnanceThresholdCombat            = 0.05f;
        private const float OrdnanceThresholdNonCombat         = 0.25f;
        private const float ResupplyTroopThreshold             = 0.5f;
        private const float KineticEnergyRatioWithPriority     = 0.8f;
        private const float KineticEnergyRatioWithOutPriority  = 0.6f;
        private const int OrdnanceProductionThresholdPriority  = 200;
        private const int OrdnanceProductionThresholdNonCombat = 100;
        private const int OrdnanceProductionThresholdCombat    = 50;

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
            if (ship.DesignRole < ShipData.RoleName.colony || ship.DesignRole == ShipData.RoleName.troop || ship.AI.State == AI.AIState.Resupply)
                return ResupplyReason.NotNeeded;

            if (!ship.hasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged(ship))
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededBecauseOfHealth(ship))
                return ResupplyReason.LowHealth;

            if (ResupplyNeededBecauseOfOrdnance(ship))
                return ResupplyReason.LowOrdnance;

            if (ResupplyNeededBecauseOfTroops(ship))
                return ResupplyReason.LowTroops;

            return ResupplyReason.NotNeeded;
        }

        public static bool DoneResupplying(Ship ship)
        {
            return HealthOk(ship) && OrdnanceOk(ship) && TroopsOk(ship);
        }

        private static bool ResupplyNeededBecauseOfHealth(Ship ship)
        {
            return ship.Health / ship.HealthMax < DamageThreshold(ship.shipData.ShipCategory);
        }

        private static bool ResupplyNeededBecauseOfOrdnance(Ship ship)
        {
            return OrdnanceLow(ship) && CurrentKineticEnergyRatioRequiresRessuply(ship) && CanNotCreateEnoughOdrnance(ship);
        }

        private static bool ResupplyNeededBecauseOfTroops(Ship ship)
        {
            if (ship.AI.HasPriorityOrder)
                return false;

            return ship.Carrier.TroopsMissingVsTroopCapacity < ResupplyTroopThreshold; 
        }

        private static bool OrdnanceLow(Ship ship)
        {
            float ordnanceThreshold = ship.InCombat ? OrdnanceThresholdCombat : OrdnanceThresholdNonCombat;
            return ship.Ordinance / ship.OrdinanceMax < ordnanceThreshold;
        }

        private static bool CurrentKineticEnergyRatioRequiresRessuply(Ship ship)
        {
            if (ship.OrdinanceMax < 1)
                return false;

            if (!ship.InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            int numWeapons        = ship.Weapons.Count(weapon => weapon.Module.Active);
            int numKineticWeapons = ship.Weapons.Count(weapon => weapon.Module.Active && weapon.OrdinanceRequiredToFire > 0);

            float ratioTheshold = ship.AI.HasPriorityOrder ? KineticEnergyRatioWithPriority : KineticEnergyRatioWithOutPriority;
            return (float)numKineticWeapons / numWeapons >= ratioTheshold;
        }

        private static bool CanNotCreateEnoughOdrnance(Ship ship)
        {
            if (ship.OrdAddedPerSecond < 1 || ship.OrdinanceMax > 0)
                return true;

            if (ship.OrdinanceMax < 1)
                return false; // doesnt care about ordnance since it has no storage for ordnance

            int ordnanceProductionThreshold;

            if (ship.AI.HasPriorityOrder)
                ordnanceProductionThreshold = OrdnanceProductionThresholdPriority;
            else
                ordnanceProductionThreshold = ship.InCombat ? OrdnanceProductionThresholdCombat : OrdnanceProductionThresholdNonCombat;

            return ship.OrdinanceMax / ship.OrdAddedPerSecond > ordnanceProductionThreshold;
        }

        private static bool HangarShipReactorsDamaged(Ship ship)
        {
            return ship.Mothership?.Active == true && ship.PowerCurrent <= 1f && ship.PowerFlowMax < ship.PowerDraw;
        }

        private static bool HealthOk(Ship ship)
        {
            float damageThreshold = ship.InCombat ? DamageThreshold(ship.shipData.ShipCategory) * 1.2f : DamageThreshold(ship.shipData.ShipCategory);
            return ship.Health / ship.HealthMax >= damageThreshold;
        }

        private static bool OrdnanceOk(Ship ship)
        {
            float ordnanceThreshold = ship.InCombat ? OrdnanceThresholdCombat * 2 : OrdnanceThresholdNonCombat;
            return ship.Ordinance / ship.OrdinanceMax >= ordnanceThreshold;
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