using System;
using System.Linq;
using SDGraphics;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Encapsulates all important ship stats separate from Ship itself.
    /// This is reused in multiple places such as:
    ///   Ship, ShipDesignScreen, ShipDesignIssues
    /// </summary>
    public class ShipStats
    {
        Ship S;
        IShipDesign Hull;
        public float Mass;
        
        public float Thrust;
        public float WarpThrust;
        public float TurnThrust;

        public float TurnRadsPerSec;

        public float MaxFTLSpeed;
        public float MaxSTLSpeed;

        public float FTLSpoolTime;

        public float TotalShieldAmplification; // total sum of applied amplifications
        public float ShieldAmplifyPerShield;
        public float ShieldMax;

        public bool IsStationary => Hull.HullRole == RoleName.station
                                 || Hull.HullRole == RoleName.platform;

        public ShipStats(Ship theShip)
        {
            S = theShip;
            Hull = theShip.ShipData;
        }

        public void Dispose()
        {
            S = null;
            Hull = null;
        }

        public void UpdateCoreStats()
        {
            Empire e = S.Loyalty;
            ShipModule[] modules = S.Modules;
            Hull = S.ShipData;

            float maxSensorBonus = 0f;
            int activeInternalSlots = 0;
            S.ActiveInternalModuleSlots = 0;
            S.BonusEMPProtection     = 0f;
            S.PowerStoreMax           = 0f;
            S.PowerFlowMax            = 0f;
            S.OrdinanceMax            = 0f;
            S.RepairRate              = 0f;
            S.CargoSpaceMax           = 0f;
            S.SensorRange             = 1000f;
            S.InhibitionRadius        = 0f;
            S.OrdAddedPerSecond       = 0f;
            S.HealPerTurn             = 0;
            S.ECMValue                = 0f;
            S.HasCommand              = S.IsPlatform || S.IsSubspaceProjector;
            S.TrackingPower           = 0;
            S.TargetingAccuracy       = 0;
            S.ResearchPerTurn         = 0;
            S.TotalRefining           = 0;
            S.MechanicalBoardingDefense = 0;

            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule module = modules[i];
                bool active = module.Active;
                if (active && module.HasInternalRestrictions) // active internal slots
                    activeInternalSlots += module.XSize * module.YSize;

                // FB - so destroyed/unpowered modules with repair wont have full repair rate
                S.RepairRate += module.ActualBonusRepairRate * (active && module.Powered ? 1f : 0.1f);

                if (active && (module.Powered || module.PowerDraw <= 0f))
                {
                    S.HasCommand |= module.IsCommandModule;
                    S.OrdinanceMax += module.OrdinanceCapacity;
                    S.CargoSpaceMax += module.CargoCapacity;
                    S.BonusEMPProtection += module.EMPProtection;
                    S.OrdAddedPerSecond += module.OrdnanceAddedPerSecond;
                    S.HealPerTurn += module.HealPerTurn;
                    S.InhibitionRadius = Math.Max(module.InhibitionRadius, S.InhibitionRadius);
                    S.SensorRange = Math.Max(module.SensorRange, S.SensorRange);
                    maxSensorBonus = Math.Max(module.SensorBonus, maxSensorBonus);
                    S.TargetingAccuracy = Math.Max(module.TargetingAccuracy, S.TargetingAccuracy);
                    S.TrackingPower += module.TargetTracking;
                    S.ECMValue = Math.Max(S.ECMValue, module.ECM).Clamped(0f, 1f);
                    S.ResearchPerTurn += module.ResearchPerTurn;
                    S.TotalRefining += module.Refining;
                    S.MechanicalBoardingDefense += module.MechanicalBoardingDefense;
                }
            }
            
            UpdateShieldAmplification();
            ShieldMax = UpdateShieldPowerMax(ShieldAmplifyPerShield);

            S.ShieldMax = ShieldMax;
            S.NetPower = Power.Calculate(modules, e);
            S.PowerStoreMax  = S.NetPower.PowerStoreMax;
            S.PowerFlowMax   = S.NetPower.PowerFlowMax;
            S.ShieldPercent = (100f * S.ShieldPower / S.ShieldMax.LowerBound(0.1f)).LowerBound(0);
            S.SensorRange   += maxSensorBonus;

            // Apply modifiers to stats
            if (S.IsPlatform)
            {
                S.SensorRange = S.SensorRange.LowerBound(10000);
                S.RepairRate = S.RepairRate.LowerBound(25);
            }

            S.SensorRange *= e.data.SensorModifier;
            S.SensorRange *= Hull.Bonuses.SensorModifier;

            // +percent based on level
            S.RepairRate += S.RepairRate * S.Level * GlobalStats.Defaults.BonusRepairPerCrewLevel;
            S.CargoSpaceMax = GetCargoSpace(S.CargoSpaceMax, Hull);

            S.SetActiveInternalSlotCount(activeInternalSlots);

            // TODO: are these used? (legacy?)
            //S.TrackingPower += 1 + e.data.Traits.Militaristic + (S.IsPlatform ? 3 : 0);
            //S.TargetingAccuracy += 1 + e.data.Traits.Militaristic + (S.IsPlatform ? 3 : 0);
        }

        public void UpdateMassRelated()
        {
            Empire e = S.Loyalty;
            ShipModule[] modules = S.Modules;

            Mass = InitializeMass(modules, e, S.SurfaceArea, S.OrdnancePercent);

            (Thrust, WarpThrust, TurnThrust) = GetThrust(modules);
            TurnRadsPerSec = GetTurnRadsPerSec(S.Level);

            MaxFTLSpeed  = GetFTLSpeed(Mass, e);
            MaxSTLSpeed  = GetSTLSpeed(Mass, e);
            FTLSpoolTime = GetFTLSpoolTime(modules, e);
        }

        public static float GetBaseCost(ShipModule[] modules)
        {
            float baseCost = 0f;
            for (int i = 0; i < modules.Length; i++)
                baseCost += modules[i].Cost;
            return baseCost;
        }

        public float GetCost(Empire e) => Hull.GetCost(e);

        public float InitializeMass(ShipModule[] modules, Empire loyalty, int surfaceArea, float ordnancePercent)
        {
            int minMass = (int)(surfaceArea * 0.5f * (1 + surfaceArea * 0.002f));
            float mass = minMass;

            for (int i = 0; i < modules.Length; i++)
                mass += modules[i].GetActualMass(loyalty, ordnancePercent, useMassModifier: false);

            mass *= loyalty.data.MassModifier; // apply overall mass modifier once 
            return Math.Max(mass, minMass);
        }

        public float GetMass(Empire loyalty)
        {
            if (loyalty == S.Loyalty || loyalty.data.MassModifier == S.Loyalty.data.MassModifier)
                return Mass;

            // convert this Mass into target empire mass
            float ratio = loyalty.data.MassModifier / S.Loyalty.data.MassModifier;
            return Mass * ratio;
        }

        public (float STL, float Warp, float Turn) GetThrust(ShipModule[] modules)
        {
            float stl  = 0f;
            float warp = 0f;
            float turn = 0f;

            if (IsStationary)
                return (0, 0, 0);

            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule m = modules[i];
                if (m.Active && (m.Powered || m.PowerDraw <= 0f))
                {
                    stl += m.Thrust;
                    warp += m.WarpThrust;
                    turn += m.TurnThrust;
                }
            }

            float modifier = Hull.Bonuses.SpeedModifier;
            return (STL: stl * modifier, Warp: warp * modifier, Turn: turn * modifier);
        }

        public float GetTurnRadsPerSec(int level)
        {
            float radsPerSec = TurnThrust / Mass / 700f;
            if (level > 0)
                radsPerSec += radsPerSec * level * 0.05f;

            radsPerSec *= 1 - S.TractorDamage / Mass;
            return Math.Min(radsPerSec, Ship.MaxTurnRadians);
        }

        public static float GetTurnRadsPerSec(IShipDesign s)
        {
            float radsPerSec = s.BaseTurnThrust / s.BaseMass / 700f;
            return Math.Min(radsPerSec, Ship.MaxTurnRadians);
        }

        public float GetFTLSpeed(float mass, Empire e)
        {
            if (WarpThrust.AlmostZero())
                return 0;
            return Math.Max(WarpThrust / mass * e.data.FTLModifier, Ship.LightSpeedConstant);
        }

        public static float GetFTLSpeed(IShipDesign s, Empire e)
        {
            if (s.BaseWarpThrust.AlmostZero())
                return 0;
            return Math.Max(s.BaseWarpThrust / s.BaseMass * e.data.FTLModifier, Ship.LightSpeedConstant);
        }

        public float GetSTLSpeed(float mass, Empire e)
        {
            float thrustWeightRatio = Thrust / mass;
            float speed = thrustWeightRatio * e.data.SubLightModifier;
            speed *= 1 - S.TractorDamage / mass;
            return Math.Min(speed, Ship.MaxSubLightSpeed);
        }

        public static float GetSTLSpeed(IShipDesign s, Empire e)
        {
            float thrustWeightRatio = s.BaseThrust / s.BaseMass;
            float speed = thrustWeightRatio * e.data.SubLightModifier;
            return Math.Min(speed, Ship.MaxSubLightSpeed);
        }

        public float GetFTLSpoolTime(ShipModule[] modules, Empire e)
        {
            float spoolTime = 0f;
            for (int i = 0; i < modules.Length; i++)
                spoolTime = Math.Max(spoolTime, modules[i].FTLSpoolTime);

            spoolTime *= e.data.SpoolTimeModifier;
            if (spoolTime <= 0f)
                spoolTime = 3f;
            return spoolTime;
        }

        public static float GetCargoSpace(float cargoMax, IShipDesign s)
        {
            return cargoMax * s.Bonuses.CargoModifier;
        }
        
        /// @return TRUE if ship can effectively warp the given distance in 1 jump
        public static bool IsWarpRangeGood(float neededRange, float powerDuration, float maxFTLSpeed)
        {
            float maxFTLRange = powerDuration * maxFTLSpeed;
            return maxFTLRange >= neededRange;
        }

        // This will also update shield max power of modules if there are amplifiers
        float UpdateShieldPowerMax(float shieldAmplify)
        {
            // NOTE: this can happen with serialized dead ships which we need to keep around in serialized Goals
            if (S.Modules.Length == 0)
                return 0f;

            float shieldMax = 0;
            foreach (ShipModule shield in S.GetShields())
            {
                if (shield.Active && shield.Powered)
                {
                    shield.UpdateShieldPowerMax(shieldAmplify);
                    shieldMax += shield.ActualShieldPowerMax;
                }
            }
            return shieldMax;
        }

        public void UpdateShieldAmplification()
        {
            TotalShieldAmplification = ShieldAmplifyPerShield = 0;

            // NOTE: this can happen with serialized dead ships which we need to keep around in serialized Goals
            if (S.Modules.Length == 0)
                return;

            int numMainShields = S.GetShields().Count(s => s.ModuleType == ShipModuleType.Shield);
            if (numMainShields == 0)
                return;

            var amplifiers = S.GetAmplifiers();
            foreach (ShipModule amplifier in amplifiers)
            {
                if (amplifier.Active && amplifier.Powered)
                    TotalShieldAmplification += amplifier.AmplifyShields;
            }

            ShieldAmplifyPerShield = TotalShieldAmplification / numMainShields;
        }

        public bool HasMainShields => S.GetShields().Any(s => s.ModuleType == ShipModuleType.Shield);
    }
}
