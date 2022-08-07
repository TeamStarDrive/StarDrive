using System;
using SDGraphics;
using SDUtils;
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
        public const float ResupplyShuttleOrdnanceThreshold    = 0.4f;

        public const float ShipDestroyThreshold = GlobalStats.ShipDestroyThreshold;
        public const float RepairDroneThreshold = 0.9f;
        public const float RepairDoneThreshold  = 0.99f;
        public const float RepairDroneRange     = 20000f;
        public float IncomingOrdnance;
        private bool InCombat;

        public ShipResupply(Ship ship)
        {
            Ship     = ship;
            InCombat = false;
            IncomingOrdnance = 0;
        }

        public static float DamageThreshold(ShipCategory category)
        {
            float threshold;
            switch (category)
            {
                default:
                case ShipCategory.Civilian:     threshold = 0.95f; break;
                case ShipCategory.Recon:        threshold = 0.85f; break;
                case ShipCategory.Neutral:      threshold = 0.75f; break;
                case ShipCategory.Unclassified: threshold = 0.7f;  break;
                case ShipCategory.Conservative: threshold = 0.8f;  break;
                case ShipCategory.Reckless:     threshold = 0.5f;  break;
                case ShipCategory.Kamikaze:     threshold = 0.0f;  break;
            }

            threshold = threshold * (1 - ShipDestroyThreshold) + ShipDestroyThreshold;
            return threshold;
        }

        public ResupplyReason Resupply()
        {
            if (Ship.DesignRole == RoleName.construction 
                || Ship.IsSingleTroopShip
                || Ship.IsSupplyShuttle
                || (Ship.AI.HasPriorityOrder || Ship.AI.HasPriorityTarget) && Ship.AI.State != AIState.Bombard && !Ship.Resupplying)
            {
                return ResupplyReason.NotNeeded;
            }

            InCombat = Ship.AI.BadGuysNear || Ship.AI.State == AIState.Bombard;
            if (!Ship.HasCommand)
                return ResupplyReason.NoCommand;

            if (HangarShipReactorsDamaged())
                return ResupplyReason.FighterReactorsDamaged;

            if (ResupplyNeededLowHealth())
                return ResupplyReason.LowHealth;

            if (ResupplyNeededLowOrdnance())
            {
                if (InCombat)
                    return ResupplyReason.LowOrdnanceCombat;

                return Ship.IsPlatformOrStation ? ResupplyReason.RequestResupplyForOrbital 
                                                : ResupplyReason.LowOrdnanceNonCombat;
            }

            if (ResupplyNeededLowTroops())
                return ResupplyReason.LowTroops;

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
                Ship.AI.OrderReturnToHangarDeferred();
            else
                Ship.AI.GoOrbitNearestPlanetAndResupply(true);
        }

        private bool ResupplyNeededLowHealth()
        {
            if (Ship.InternalSlotsHealthPercent < ShipDestroyThreshold) // ship is dying or in init
                return false;

            return Ship.InternalSlotsHealthPercent < DamageThreshold(Ship.ShipData.ShipCategory)
                   && !Ship.AI.HasPriorityTarget;
        }

        private bool ResupplyNeededLowOrdnance()
        {
            if (Ship.OrdinanceMax < 1)
                return false;

            return OrdnanceLow() && HighKineticToEnergyRatio()
                                 && InsufficientOrdnanceProduction();
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
                resupplyTroopThreshold = Ship.OnHighAlert ? 0.25f : 0.99f;

            if (Ship.Carrier.HasTroopBays) // Counting troops in missions as well for troop carriers
                return (Ship.Carrier.TroopsMissingVsTroopCapacity).LessOrEqual(resupplyTroopThreshold);

            // Ships with Barracks only
            return ((float)Ship.TroopCount / Ship.TroopCapacity).LessOrEqual(resupplyTroopThreshold) && Ship.OnLowAlert;
        }

        private bool OrdnanceLow()
        {
            if (PlayerKamikaze)
                return false; // Only player manual command will convince Kamikaze ship to resupply

            if (Ship.Ordinance < Ship.OrdnanceMin)
                return true;

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

            int prodThresholdSeconds;

            if (Ship.AI.HasPriorityTarget)
                prodThresholdSeconds = OrdnanceProductionThresholdPriority;
            else
                prodThresholdSeconds = InCombat ? OrdnanceProductionThresholdCombat
                                                : OrdnanceProductionThresholdNonCombat;

            float secondsUntilMaxOrd = (Ship.OrdinanceMax - Ship.Ordinance) / Ship.OrdAddedPerSecond;
            return secondsUntilMaxOrd > prodThresholdSeconds;
        }

        private bool HangarShipReactorsDamaged()
        {
            return Ship.Mothership?.Active == true && Ship.PowerCurrent <= 1f
                                                   && Ship.PowerFlowMax < Ship.PowerDraw;
        }

        private bool HealthOk()
        {
            float threshold = InCombat ? (DamageThreshold(Ship.ShipData.ShipCategory) * 1.2f).Clamped(0, 1)
                                       : RepairDoneThreshold;

            float healthTypeToCheck = InCombat ? Ship.InternalSlotsHealthPercent
                                               : Ship.HealthPercent;

            return healthTypeToCheck >= threshold && Ship.HasCommand;
        }

        private bool OrdnanceOk()
        {
            float threshold = InCombat ? OrdnanceThresholdCombat : OrdnanceThresholdNonCombat;
            if (Ship.Resupplying)
                threshold = 0.99f;

            return Ship.OrdnancePercent >= threshold;
        }

        private bool TroopsOk()
        {
            if (InCombat || Ship.TroopCapacity == 0)
                return true;

            return Ship.Carrier.TroopsMissingVsTroopCapacity >= 1f;
        }

        bool PlayerKamikaze => Ship.ShipData.ShipCategory == ShipCategory.Kamikaze && Ship.Loyalty.isPlayer;

        public void ChangeIncomingOrdnance(float amount)
        {
            IncomingOrdnance = (IncomingOrdnance + amount).LowerBound(0);
        }

        public void ResetIncomingOrdnance(SupplyType supplyType)
        {
            IncomingOrdnance = 0;
        }

        public bool AcceptExternalSupply(SupplyType supplyType)
        {
            switch (supplyType)
            {
                case SupplyType.All:
                    break;
                case SupplyType.Rearm:
                    if (Ship.ShipData.IsSupplyCarrier)
                        return false;
                    return OrdnancePercentageWithIncoming < (Ship.AI.BadGuysNear ? ResupplyShuttleOrdnanceThreshold : 1f);
                        
                case SupplyType.Repair:
                    break;
                case SupplyType.Troops:
                    break;
            }
            return false;
        }

        public Status ShipStatusWithPendingRearm()
        {
            return Ship.OrdnanceStatusWithIncoming(IncomingOrdnance);
        }

        public float MissingOrdnanceWithIncoming => (Ship.OrdinanceMax - Ship.Ordinance + IncomingOrdnance).LowerBound(0);

        public float OrdnancePercentageWithIncoming => Ship.OrdinanceMax == 0
            ? 1 : ((Ship.Ordinance + IncomingOrdnance) / Ship.OrdinanceMax).UpperBound(1f);

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
        RequestResupplyForOrbital
    }

    public enum SupplyType
    {
        All,
        Rearm,
        Repair,
        Troops
    }
}