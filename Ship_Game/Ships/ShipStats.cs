using System;

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
        ShipData Hull;
        public float Mass;
        
        public float Thrust;
        public float WarpThrust;
        public float TurnThrust;

        public float VelocityMax;
        public float TurnRadsPerSec;

        public float MaxFTLSpeed;
        public float MaxSTLSpeed;

        public float FTLSpoolTime;

        public float TotalShieldAmplification; // total sum of applied amplifications
        public float ShieldAmplifyPerShield;
        public float ShieldMax;

        public bool IsStationary => Hull.HullRole == ShipData.RoleName.station
                                 || Hull.HullRole == ShipData.RoleName.platform;

        public ShipStats(Ship theShip)
        {
            S = theShip;
            Hull = theShip.shipData;
        }

        public void Dispose()
        {
            S = null;
            Hull = null;
        }

        public void UpdateCoreStats()
        {
            Empire e = S.loyalty;
            ShipModule[] modules = S.Modules;
            Hull = S.shipData;

            float maxSensorBonus = 0f;
            S.ActiveInternalSlotCount = 0;
            S.BonusEMP_Protection     = 0f;
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
            S.hasCommand              = S.IsPlatform;
            S.TrackingPower           = 0;
            S.TargetingAccuracy       = 0;

            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule module = modules[i];
                // active internal slots
                if (module.HasInternalRestrictions && module.Active)
                    S.ActiveInternalSlotCount += module.XSIZE * module.YSIZE;

                S.RepairRate += module.Active ? module.ActualBonusRepairRate : module.ActualBonusRepairRate / 10; // FB - so destroyed modules with repair wont have full repair rate

                if (module.Active && (module.Powered || module.PowerDraw <= 0f))
                {
                    S.hasCommand |= module.IsCommandModule;
                    S.OrdinanceMax        += module.OrdinanceCapacity;
                    S.CargoSpaceMax       += module.Cargo_Capacity;
                    S.BonusEMP_Protection += module.EMP_Protection;
                    S.OrdAddedPerSecond   += module.OrdnanceAddedPerSecond;
                    S.HealPerTurn         += module.HealPerTurn;
                    S.InhibitionRadius  = Math.Max(module.InhibitionRadius, S.InhibitionRadius);
                    S.SensorRange       = Math.Max(module.SensorRange, S.SensorRange);
                    maxSensorBonus      = Math.Max(module.SensorBonus, maxSensorBonus);
                    S.TargetingAccuracy = Math.Max(module.TargetingAccuracy, S.TargetingAccuracy);
                    S.TrackingPower    += module.TargetTracking;
                    S.ECMValue = Math.Max(S.ECMValue, module.ECM).Clamped(0f, 1f);
                    module.AddModuleTypeToList(module.ModuleType, isTrue: module.InstalledWeapon?.isRepairBeam == true, addToList: S.RepairBeams);
                }
            }
            
            UpdateShieldAmplification();
            ShieldMax = UpdateShieldPowerMax(ShieldAmplifyPerShield);

            S.shield_max = ShieldMax;
            S.NetPower = Power.Calculate(modules, e);
            S.PowerStoreMax  = S.NetPower.PowerStoreMax;
            S.PowerFlowMax   = S.NetPower.PowerFlowMax;
            S.shield_percent = (100.0 * S.shield_power / S.shield_max.LowerBound(0.1f)).LowerBound(0);
            S.SensorRange   += maxSensorBonus;

            // Apply modifiers to stats
            if (S.IsPlatform) S.SensorRange = S.SensorRange.LowerBound(10000);
            S.SensorRange   *= e.data.SensorModifier;
            S.SensorRange   *= Hull.Bonuses.SensorModifier;
            S.CargoSpaceMax *= Hull.Bonuses.CargoModifier;
            S.RepairRate    += (float)(S.RepairRate * S.Level * 0.05);
            
            // TODO: are these used? (legacy?)
            //S.TrackingPower += 1 + e.data.Traits.Militaristic + (S.IsPlatform ? 3 : 0);
            //S.TargetingAccuracy += 1 + e.data.Traits.Militaristic + (S.IsPlatform ? 3 : 0);
        }

        public void UpdateMassRelated()
        {
            Empire e = S.loyalty;
            ShipModule[] modules = S.Modules;

            Mass = InitializeMass(modules, e, S.SurfaceArea, S.OrdnancePercent);

            (Thrust, WarpThrust, TurnThrust) = GetThrust(modules);
            UpdateVelocityMax();
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

        public float GetCost(float baseCost, Empire e, bool isOrbital)
        {
            if (Hull.HasFixedCost)
                return Hull.FixedCost * CurrentGame.ProductionPace;

            float cost = baseCost * CurrentGame.ProductionPace;
            cost += Hull.Bonuses.StartingCost;
            cost += cost * e.data.Traits.ShipCostMod;
            cost *= 1f - Hull.Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
            if (isOrbital)
                cost *= 0.7f;

            return (int)cost;
        }

        public float InitializeMass(ShipModule[] modules, Empire loyalty, int surfaceArea, float ordnancePercent)
        {
            float minMass = surfaceArea * 0.5f * (1 + surfaceArea / 500);
            float mass = minMass;

            for (int i = 0; i < modules.Length; i++)
                mass += modules[i].GetActualMass(loyalty, ordnancePercent, useMassModifier: false);

            mass *= loyalty.data.MassModifier; // apply overall mass modifier once 
            return Math.Max(mass, minMass);
        }

        public float GetMass(Empire loyalty)
        {
            if (loyalty == S.loyalty || loyalty.data.MassModifier == S.loyalty.data.MassModifier)
                return Mass;

            // convert this Mass into target empire mass
            float ratio = loyalty.data.MassModifier / S.loyalty.data.MassModifier;
            return Mass * ratio;
        }

        public (float STL, float Warp, float Turn) GetThrust(ShipModule[] modules)
        {
            float stl = 0f;
            float warp = 0f;
            float turn = 0f;
            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule m = modules[i];
                if (m.Active && (m.Powered || m.PowerDraw <= 0f))
                {
                    stl += m.thrust;
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
            return Math.Min(radsPerSec, Ship.MaxTurnRadians);
        }

        public float UpdateVelocityMax()
        {
            VelocityMax = Thrust / Mass;
            return VelocityMax;
        }


        public float GetFTLSpeed(float mass, Empire e)
        {
            if (WarpThrust.AlmostZero())
                return 0;
            return Math.Max(WarpThrust / mass * e.data.FTLModifier, Ship.LightSpeedConstant);
        }

        public float GetSTLSpeed(float mass, Empire e)
        {
            float thrustWeightRatio = Thrust / mass;
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

        // This will also update shield max power of modules if there are amplifiers
        float UpdateShieldPowerMax(float shieldAmplify)
        {
            ShipModule[] shields = S.Shields;
            var mainShields = shields.Filter(s => s.ModuleType == ShipModuleType.Shield);
            if (mainShields.Length == 0)
                return 0;

            float shieldMax = 0;
            for (int i = 0; i < shields.Length; i++)
            {
                ShipModule shield = shields[i];
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

            ShipModule[] amplifiers = S.Amplifiers;
            if (amplifiers.Length == 0)
                return;

            var mainShields = S.Shields.Filter(s => s.ModuleType == ShipModuleType.Shield);
            int numShields = mainShields.Length;
            if (numShields == 0)
                return;

            for (int i = 0; i < amplifiers.Length; i++)
            {
                ShipModule amplifier = amplifiers[i];
                if (amplifier.Active && amplifier.Powered)
                    TotalShieldAmplification += amplifier.AmplifyShields;
            }

            ShieldAmplifyPerShield = TotalShieldAmplification / numShields;
        }

        public bool HasMainShields => S.Shields.Any(s => s.ModuleType == ShipModuleType.Shield);
    }
}
