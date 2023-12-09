using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using SDGraphics;
using SDUtils;

namespace Ship_Game.GameScreens.ShipDesign;

// Specific metrics used in ShipDesignScreen analysis
public class ShipDesignStats
{
    public Ship S;
    public ShipModule[] PoweredWeapons;
    public Map<ShipModule, float> WeaponAccuracies;
    public int NumSlots;
    public int NumCmdModules;
    public int NumWeaponSlots;
    public int NumWeapons;
    public int NumOrdWeapons;
    public float WeaponsArea;
    public float KineticWeaponsArea;
    public int NumTroopBays;

    public float WeaponPowerNeeded;
    public float BurstOrdnance;
    public float AvgOrdnanceUsed;
    public float NetOrdnanceUsePerSec;
    public float AmmoTime;
    public float BeamPeakPowerNeeded;
    public float BeamAverageDuration;
    public float WeaponPowerNeededNoBeams;
    public float Strength;
    public float RelativeStrength;

    public int PointDefenseValue;
    public int TotalHangarArea;
    public bool HasPowerCells;
    public bool CanTargetFighters;
    public bool CanTargetCorvettes;
    public bool CanTargetCapitals;
    public bool HasEnergyWeapons;
    public ShipModule[] UnpoweredModules;

    public float PowerCapacity;
    public float PowerRecharge;
    public float PowerConsumed;
    public float EnergyDuration;
    public float PowerConsumedWithBeams;
    public float BurstEnergyDuration;
            
    public float ChargeAtWarp;
    public float WarpTime;


    public ShipDesignStats(Ship s, Empire player)
    {
        S = s;
        Update(player);
    }

    public void Update(Empire player)
    {
        // select only powered modules and powered weapons
        ShipModule[] modules = S.Modules.Filter(m => m.PowerDraw <= 0 || m.Powered);
        PoweredWeapons = modules.Filter(m => m.InstalledWeapon != null);
        Weapon[] weapons = PoweredWeapons.Select(m => m.InstalledWeapon);

        WeaponAccuracies = new Map<ShipModule, float>();
        foreach (var w in weapons)
            WeaponAccuracies[w.Module] = w.Tag_Guided ? 0f : w.BaseTargetError(S.TargetingAccuracy).LowerBound(1) / 16;

        int nSlots = S.Modules.Sum(m => m.Area);
        NumSlots       = nSlots;
        NumCmdModules  = S.Modules.Count(m => m.IsCommandModule);
        NumWeaponSlots = S.Weapons.Sum(w => w.Module.Area);
        NumWeapons    = weapons.Count(w => !w.TruePD);
        NumOrdWeapons = weapons.Count(w => !w.TruePD && w.OrdinanceRequiredToFire > 0);
        WeaponsArea   = weapons.Sum(w => !w.TruePD ? w.Module.Area : 0f);
        KineticWeaponsArea = weapons.Sum(w => !w.TruePD && w.OrdinanceRequiredToFire > 0 ? w.Module.Area : 0);
        NumTroopBays  = modules.Count(m => m.IsTroopBay);

        WeaponPowerNeeded = weapons.Sum(w => w.PowerFireUsagePerSecond);
        BurstOrdnance = weapons.Sum(w => w.TotalOrdnanceUsagePerFire);

        float bayOrdPerSec = modules.Sum(m => m.BayOrdnanceUsagePerSecond(player));
        float avgOrdPerSec = weapons.Sum(w => w.AverageOrdnanceUsagePerSecond);
        AvgOrdnanceUsed = bayOrdPerSec + avgOrdPerSec;
        NetOrdnanceUsePerSec = AvgOrdnanceUsed - S.OrdAddedPerSecond;
        AmmoTime = S.OrdinanceMax / NetOrdnanceUsePerSec;

        Weapon[] beamWeapons = weapons.Filter(w => w.IsBeam);
        if (beamWeapons.Length > 0)
        {
            BeamPeakPowerNeeded = beamWeapons.Sum(w => w.BeamPowerCostPerSecond);
            float beamTotalDuration = beamWeapons.Sum(w => w.BeamDuration);
            if (beamTotalDuration > 0)
                BeamAverageDuration = beamTotalDuration / beamWeapons.Length;
        }
        else
        {
            BeamPeakPowerNeeded = 0;
            BeamAverageDuration = 0;
        }

        WeaponPowerNeededNoBeams = weapons.Sum(w => !w.IsBeam ? w.PowerFireUsagePerSecond : 0);

        Strength = S.GetStrength();
        RelativeStrength = (float)Math.Round(Strength / nSlots, 2);

        PointDefenseValue = weapons.Sum(w => (w.TruePD?4:0) + (w.Tag_PD?1:0));
        TotalHangarArea   = modules.Sum(m => m.MaximumHangarShipSize);
        HasPowerCells    = modules.Any(m => m.ModuleType == ShipModuleType.FuelCell && m.PowerStoreMax > 0);
        CanTargetFighters  = weapons.Any(w => !w.TruePD && !w.ExcludesFighters);
        CanTargetCorvettes = weapons.Any(w => !w.TruePD && !w.ExcludesCorvettes);
        CanTargetCapitals  = weapons.Any(w => !w.ExcludesCapitals);
        HasEnergyWeapons   = weapons.Any(w => w.PowerRequiredToFire > 0 || w.BeamPowerCostPerSecond > 0);
        UnpoweredModules   = S.Modules.Filter(m => m.PowerDraw > 0 && !m.Powered && m.ModuleType != ShipModuleType.PowerConduit);

        PowerCapacity = S.PowerStoreMax;
        PowerRecharge = S.PowerFlowMax - S.NetPower.NetSubLightPowerDraw;
        PowerConsumed = WeaponPowerNeeded - PowerRecharge;
        EnergyDuration = HasEnergyWeapons && PowerConsumed > 0 ? PowerCapacity / PowerConsumed : 0f;
        PowerConsumedWithBeams = BeamPeakPowerNeeded + WeaponPowerNeededNoBeams - PowerRecharge;
        BurstEnergyDuration = PowerCapacity / PowerConsumedWithBeams;
        ChargeAtWarp = S.PowerFlowMax - S.NetPower.NetWarpPowerDraw;
        WarpTime = -PowerCapacity / ChargeAtWarp;
    }

    public bool IsWarpCapable() => S.MaxFTLSpeed > 0 && !S.IsPlatformOrStation;
    public bool HasEnergyWepsPositive() => HasEnergyWeapons && PowerConsumed > 0;
    public bool HasEnergyWepsNegative() => HasEnergyWeapons && PowerConsumed <= 0;
    public bool HasBeams() => BeamAverageDuration > 0 && PowerConsumedWithBeams > 0;
    public bool HasBeamDurationPositive() => HasBeams() && BurstEnergyDuration >= BeamAverageDuration;
    public bool HasBeamDurationNegative() => HasBeams() && BurstEnergyDuration < BeamAverageDuration;

    public bool HasFiniteWarp() => ChargeAtWarp < 0 && WarpTime <= 60*10;
    public bool HasInfiniteWarp() => ChargeAtWarp > 0 || WarpTime > 60*10;

    public bool HasAmplifiedMains() => S.Stats.TotalShieldAmplification > 0 && S.Stats.HasMainShields;
    public bool HasRegularShields() => S.ShieldMax > 0 && !HasAmplifiedMains();
    public bool HasAmplifiedShields() => S.ShieldMax > 0 && HasAmplifiedMains();

    public bool HasOrdnance() => S.OrdinanceMax > 0;
    public bool HasOrdFinite() => HasOrdnance() && NetOrdnanceUsePerSec > 0;
    public bool HasOrdInfinite() => HasOrdnance() && NetOrdnanceUsePerSec <= 0;
    public bool ProducesResearch() => S.ResearchPerTurn> 0;
    public bool RefinesResources() => S.TotalRefining > 0;
    public float ResearchTime() => ProducesResearch() 
        ? S.CargoSpaceMax / (S.ResearchPerTurn*GlobalStats.Defaults.ResearchStationProductionPerResearch) : 0;
    public float RefiningTime() => RefinesResources()
        ? (S.CargoSpaceMax - S.RefiningCargoSpaceMax) / (S.TotalRefining * GlobalStats.Defaults.MiningStationFoodPerOneRefining) : 0;

    public int CompletionPercent => S?.ShipData.GetCompletionPercent() ?? 0;
}
