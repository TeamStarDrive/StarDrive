using System;
using NAudio.Wave;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    [StarDataType]
    public struct ShipResupply // Created by Fat Bastard to centralize all ship supply logic
    {
        public const float OrdnanceThresholdCombat            = 0.1f;
        public const float OrdnanceThresholdNonCombat         = 0.35f;
        public const float OrdnanceThresholdNonCombatOrbital  = 0.85f;
        public const float KineticRatioThreshold              = 0.75f;
        public const int OrdnanceProductionThresholdPriority  = 400;
        public const int OrdnanceProductionThresholdNonCombat = 150;
        public const int OrdnanceProductionThresholdCombat    = 75;
        public const float ResupplyShuttleOrdnanceThreshold   = 0.4f;

        public const float RepairDroneThreshold = 0.9f;
        public const float RepairDoneThreshold  = 0.99f;
        public const float RepairDroneRange     = 20000f;
        public const int NumTurnsForGoodResearchSupply = 30;
        public const int NumTurnsForGoodRefiningSupply = 40;

        [StarData] readonly Ship Ship;
        [StarData] float IncomingOrdnance;
        [StarData] bool InCombat;

        public ShipResupply(Ship ship)
        {
            Ship     = ship;
            InCombat = false;
            IncomingOrdnance = 0;
        }

        public bool InTradeBlockade => Ship.IsResearchStation && Ship.HealthPercent < DamageThreshold(ShipCategory.Civilian);
        public static bool HasGoodTotalSupplyForResearch(IShipDesign ship)
        {
            if (!ship.IsResearchStation) 
                return true;

            float totalResearchPerTurn = ship.BaseResearchPerTurn * GlobalStats.Defaults.ResearchStationProductionPerResearch;
            return ship.BaseCargoSpace / totalResearchPerTurn >= NumTurnsForGoodResearchSupply;
        }

        public static bool HasGoodTotalSupplyForRefining(IShipDesign ship)
        {
            if (!ship.IsMiningStation)
                return true;

            float totalRefiningPerTurn = ship.BaseRefiningPerTurn * GlobalStats.Defaults.MiningStationFoodPerOneRefining;
            return ship.BaseCargoSpace*0.5f / totalRefiningPerTurn >= NumTurnsForGoodRefiningSupply;
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

            float baseThreshold = GlobalStats.Defaults.ShipDestroyThreshold;
            threshold = threshold * (1 - baseThreshold) + baseThreshold;
            return threshold;
        }

        public ResupplyReason Resupply()
        {
            if (!Ship.HasCommand || !Ship.IsAlive)
                return ResupplyReason.NoCommand;

            if (Ship.DesignRole == RoleName.construction 
                || Ship.IsSingleTroopShip
                || Ship.IsSupplyShuttle
                || (Ship.AI.HasPriorityOrder || Ship.AI.HasPriorityTarget) && Ship.AI.State != AIState.Bombard && !Ship.Resupplying)
            {
                return ResupplyReason.NotNeeded;
            }

            InCombat = Ship.AI.BadGuysNear || Ship.AI.State == AIState.Bombard;

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
            if (Ship.InternalSlotsHealthPercent < GlobalStats.Defaults.ShipDestroyThreshold) // ship is dying or in init
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

        bool OrdnanceLow()
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

        bool HighKineticToEnergyRatio()
        {
            if (Ship.OrdinanceMax < 1 || Ship.Weapons.IsEmpty && Ship.BombBays.IsEmpty && Ship.Carrier.AllHangars.Length == 0)
                return false;

            if (!InCombat)
                return true; // ships not in combat will want to resupply if they have Ordnance storage from this method point of view

            float kineticAreaRatio = KineticAreaRatio(Ship.Weapons);
            if (Ship.Loyalty.isPlayer && Ship.AI.HasPriorityTarget && kineticAreaRatio.Less(1))
                return false; // if player ordered a specific attack and the ship has energy weapons, continue to fight

            return kineticAreaRatio > KineticRatioThreshold; 

            float KineticAreaRatio(Array<Weapon> weapons)
            {
                float kineticArea = 0;
                float totalArea = 0;
                foreach (Weapon w in weapons)
                {
                    if (!w.TruePD && w.Module is { Active: true })
                    {
                        totalArea += w.Module.Area;
                        if (w.OrdinanceRequiredToFire > 0)
                            kineticArea += w.Module.Area;
                    }
                }

                return kineticArea / totalArea;
            }
        }

        bool InsufficientOrdnanceProduction()
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