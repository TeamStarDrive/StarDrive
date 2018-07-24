using System;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public static class ShipResupply // Created by Fat Bastard to deal with resupply logic
    {
        private static float DamageThreshold(ShipData.Category category)
        {
            float threshold;
            switch (category)
            {
                default:                             threshold = 0.45f; break;
                case ShipData.Category.Unclassified: threshold = 0.25f; break;
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

            //float resupplyTroopThreshold = 0.5f;
            return ship.Carrier.NeedResupplyTroops; // threshold should be calculated here  0.5. change the carrier class method after implementation.
        }

        private static bool OrdnanceLow(Ship ship)
        {
            float ordnanceThreshold = ship.InCombat ? 0.05f : 0.25f;
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

            float kineticEnergyRatioTheshold = ship.AI.HasPriorityOrder ? 0.8f : 0.6f;
            return (float)numKineticWeapons / numWeapons >= kineticEnergyRatioTheshold;
        }

        private static bool CanNotCreateEnoughOdrnance(Ship ship)
        {
            if (ship.OrdAddedPerSecond < 1 || ship.OrdinanceMax > 0)
                return true;

            if (ship.OrdinanceMax < 1)
                return false; // doesnt care about ordnance since it has no storage for ordnance

            float ordnanceProductionThreshold;

            if (ship.AI.HasPriorityOrder)
                ordnanceProductionThreshold = 200;
            else
                ordnanceProductionThreshold = ship.InCombat ? 50 : 100;

            return ship.OrdinanceMax / ship.OrdAddedPerSecond > ordnanceProductionThreshold;
        }

        private static bool HangarShipReactorsDamaged(Ship ship)
        {
            return ship.Mothership?.Active == true && ship.PowerCurrent <= 1f && ship.PowerFlowMax < ship.PowerDraw;
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