using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.GameScreens.ShipDesign
{
    // Specific metrics used in ShipDesignScreen analysis
    public class ShipDesignStats
    {
        public Ship S;
        public Map<ShipModule, float> WeaponAccuracies;
        public int NumSlots;
        public int NumCmdModules;
        public int NumWeaponSlots;
        public int NumHangarSlots;
        public int NumWeapons;
        public int NumOrdWeapons;
        public int NumTroopBays;

        public float WeaponPowerNeeded;
        public float BurstOrdnance;
        public float AvgOrdnanceUsed;
        public float AmmoTime;
        public float BeamPeakPowerNeeded;
        public float BeamLongestDuration;
        public float WeaponPowerNeededNoBeams;
        public float Offense;
        public float Defense;

        public int PointDefenseValue;
        public int TotalHangarArea;
        public bool HasPowerCells;
        public bool CanTargetFighters;
        public bool CanTargetCorvettes;
        public bool CanTargetCapitals;
        public bool HasEnergyWeapons;
        public bool UnpoweredModules;

        public float PowerCapacity;
        public float PowerRecharge;
        public float PowerConsumed;
        public float EnergyDuration;
        public float PowerConsumedWithBeams;
        public float BurstEnergyDuration;
            
        public float DrawAtWarp;
        public float WarpTime;

        public ShipDesignStats(Ship s)
        {
            S = s;
            Update();
        }

        public void Update()
        {
            // select only powered modules and powered weapons
            ShipModule[] modules = S.Modules.Filter(m => m.PowerDraw <= 0 || m.Powered);
            Weapon[] weapons = modules.FilterSelect(m => m.InstalledWeapon != null, m => m.InstalledWeapon);

            WeaponAccuracies = new Map<ShipModule, float>();
            foreach (var w in weapons)
                WeaponAccuracies[w.Module] = w.Tag_Guided ? 0f : w.BaseTargetError(S.TargetingAccuracy).LowerBound(1) / 16;

            int nSlots = S.Modules.Sum(m => m.Area);
            NumSlots       = nSlots;
            NumCmdModules  = S.Modules.Count(m => m.IsCommandModule);
            NumWeaponSlots = S.Weapons.Sum(w => w.Module.Area);
            NumHangarSlots = S.Modules.Sum(m => (m.IsTroopBay || m.IsSupplyBay || m.MaximumHangarShipSize > 0) ? m.Area : 0);
            NumWeapons    = weapons.Count(w => !w.TruePD);
            NumOrdWeapons = weapons.Count(w => !w.TruePD && w.OrdinanceRequiredToFire > 0);
            NumTroopBays  = modules.Count(m => m.IsTroopBay);

            WeaponPowerNeeded = weapons.Sum(w => w.PowerFireUsagePerSecond);
            BurstOrdnance = weapons.Sum(w => w.BurstOrdnanceUsagePerSecond);

            float bayOrdPerSec = modules.Sum(m => m.BayOrdnanceUsagePerSecond);
            float avgOrdPerSec = weapons.Sum(w => w.AverageOrdnanceUsagePerSecond);
            AvgOrdnanceUsed = bayOrdPerSec + avgOrdPerSec;

            AmmoTime = S.OrdinanceMax / (AvgOrdnanceUsed - S.OrdAddedPerSecond);
            BeamPeakPowerNeeded = weapons.Sum(w => w.isBeam ? w.BeamPowerCostPerSecond : 0);
            BeamLongestDuration = weapons.Max(w => w.isBeam ? w.BeamDuration : 0);
            WeaponPowerNeededNoBeams = weapons.Sum(w => !w.isBeam ? w.PowerFireUsagePerSecond : 0);

            Offense = modules.Sum(m => m.CalculateModuleOffense());
            Defense = modules.Sum(m => m.CalculateModuleDefense(nSlots));

            PointDefenseValue = weapons.Sum(w => (w.TruePD?4:0) + (w.Tag_PD?1:0));
            TotalHangarArea   = modules.Sum(m => m.MaximumHangarShipSize);
            HasPowerCells    = modules.Any(m => m.ModuleType == ShipModuleType.FuelCell && m.PowerStoreMax > 0);
            CanTargetFighters  = weapons.Any(w => !w.Excludes_Fighters);
            CanTargetCorvettes = weapons.Any(w => !w.Excludes_Corvettes);
            CanTargetCapitals  = weapons.Any(w => !w.Excludes_Capitals);
            HasEnergyWeapons   = weapons.Any(w => w.PowerRequiredToFire > 0 || w.BeamPowerCostPerSecond > 0);
            UnpoweredModules   = S.Modules.Any(m => m.PowerDraw > 0 && !m.Powered && m.ModuleType != ShipModuleType.PowerConduit);

            PowerCapacity = S.PowerStoreMax;
            PowerRecharge = S.PowerFlowMax - S.NetPower.NetSubLightPowerDraw;
            PowerConsumed = WeaponPowerNeeded - PowerRecharge;
            EnergyDuration = HasEnergyWeapons && PowerConsumed > 0 ? PowerCapacity / PowerConsumed : 0f;
            PowerConsumedWithBeams = BeamPeakPowerNeeded + WeaponPowerNeededNoBeams - PowerRecharge;
            BurstEnergyDuration = PowerCapacity / PowerConsumedWithBeams;
            DrawAtWarp = S.PowerFlowMax - S.NetPower.NetWarpPowerDraw;
            WarpTime = -PowerCapacity / DrawAtWarp;
        }

        public bool IsWarpCapable() => S.MaxFTLSpeed > 0 && !S.IsPlatformOrStation;
        public bool HasEnergyWepsPositive() => HasEnergyWeapons && PowerConsumed > 0;
        public bool HasEnergyWepsNegative() => !HasEnergyWepsPositive();
        public bool HasBeams() => BeamLongestDuration > 0 && PowerConsumedWithBeams > 0;
        public bool HasBeamDurationPositive() => HasBeams() && BurstEnergyDuration >= BeamLongestDuration;
        public bool HasBeamDurationNegative() => !HasBeamDurationPositive();
    }
}
