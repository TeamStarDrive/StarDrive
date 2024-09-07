using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Universe.SolarBodies;
using static Ship_Game.Ships.Ship;

namespace Ship_Game.Ships;

// This part of ShipDesign contains all the useful Stats
// that are common across this ShipDesign
public partial class ShipDesign
{
    // Role assigned to the Hull, such as `Cruiser`
    public RoleName HullRole => BaseHull.Role;

    public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

    public bool IsPlatformOrStation { get; private set; }
    public bool IsStation           { get; private set; }
    public bool IsConstructor       { get; private set; }
    public bool IsSubspaceProjector { get; private set; }
    public bool IsDysonSwarmController     { get; private set; }
    public bool IsColonyShip        { get; private set; }
    public bool IsSupplyCarrier     { get; private set; } // this ship launches supply ships
    public bool IsSupplyShuttle     { get; private set; }
    public bool IsFreighter         { get; private set; }
    public bool IsResearchStation   { get; private set; }
    public bool IsMiningStation { get; private set; }
    public bool IsCandidateForTradingBuild { get; private set; }
    public bool IsUnitTestShip      { get; private set; }

    public bool IsSingleTroopShip { get; private set; }
    public bool IsTroopShip       { get; private set; }
    public bool IsBomber          { get; private set; }

    public float BaseCost       { get; private set; }
    public float BaseStrength   { get; private set; }
    public float BaseThrust     { get; private set; }
    public float BaseTurnThrust { get; private set; }
    public float BaseWarpThrust { get; private set; }
    public bool  BaseCanWarp    { get; private set; }
    public float BaseMass       { get; private set; }
    public float BaseCargoSpace { get; private set; }
    public float BaseResearchPerTurn { get; private set; }
    public float BaseRefiningPerTurn { get; private set; }
    public byte NumConstructionModules { get; private set; }

    public Power NetPower;

    public float StartingColonyGoods { get; private set; }
    public int NumBuildingsDeployed { get; private set; }

    // Hangar Templates
    public ShipModule[] Hangars { get; private set; }
    public ShipModule[] AllFighterHangars { get; private set; }

    // Weapon Templates
    public Weapon[] Weapons { get; private set; }

    // All invalid modules in this design
    // If this is not null, this ship cannot be spawned, but can still be listed and loaded in Shipyard
    public string InvalidModules { get; private set; }

    void InitializeCommonStats(ShipHull hull, DesignSlot[] designSlots, bool updateRole = false)
    {
        if (ShipStyle.IsEmpty()) ShipStyle = hull.Style;
        if (IconPath.IsEmpty())  IconPath  = hull.IconPath;

        var info = GridInfo;
        info.SurfaceArea = hull.SurfaceArea;
        GridInfo = info;
        Grid = new(Name, info, designSlots);

        float baseCost = 0f;
        float baseThrust = 0f;
        float baseTurnThrust = 0f;
        float baseWarp = 0f;
        float baseMass = 0f;
        float baseOffense = 0f;
        float baseDefense = 0f;
        float baseCargoSpace = 0f;
        int offensiveSlots = 0;
        float startingColonyGoods = 0f;
        int numBuildingsDeployed = 0;
        float baseResearchPerTurn = 0;
        float baseProcessingPerTurn = 0;
        byte numConstructionModules = 0;

        var mTemplates = new Array<ShipModule>();
        var hangars = new Array<ShipModule>();
        var weapons = new Array<Weapon>();
        HashSet<string> invalidModules = null;

        for (int i = 0; i < designSlots.Length; i++)
        {
            string uid = designSlots[i].ModuleUID;
            if (!ResourceManager.GetModuleTemplate(uid, out ShipModule m))
            {
                invalidModules ??= new();
                invalidModules.Add(uid);
                continue;
            }

            mTemplates.Add(m);
            baseCost += m.Cost;
            baseThrust += m.Thrust;
            baseTurnThrust += m.TurnThrust;
            baseWarp += m.WarpThrust;
            baseMass += m.Mass; // WARNING: this is the unmodified mass, without any bonuses
            baseCargoSpace += m.CargoCapacity;
            baseResearchPerTurn += m.ResearchPerTurn;
            baseProcessingPerTurn += m.Refining;

            if (m.ModuleType == ShipModuleType.Construction)
                numConstructionModules++;

            if (m.Is(ShipModuleType.Hangar))
                hangars.Add(m);
            else if (m.Is(ShipModuleType.Colony))
                IsColonyShip = true;
            else if (m.InstalledWeapon != null)
            {
                offensiveSlots += m.Area;
                weapons.Add(m.InstalledWeapon);
            }

            if (m.IsSupplyBay)
                IsSupplyCarrier = true;
            if (m.IsTroopBay || m.IsSupplyBay || m.MaximumHangarShipSize > 0)
                offensiveSlots += m.Area;
            if (m.DeployBuildingOnColonize.NotEmpty())
                ++numBuildingsDeployed;

            startingColonyGoods += m.NumberOfEquipment + m.NumberOfFood;
            baseDefense += m.CalculateModuleDefense(info.SurfaceArea);
            baseOffense += m.CalculateModuleOffense();
        }

        if (invalidModules != null)
        {
            InvalidModules = string.Join(" ", invalidModules);
            Log.Warning(ConsoleColor.Red, $"ShipDesign '{Name}' InvalidModules='{InvalidModules}' Source='{Source.FullName}'");
        }

        BaseCost = baseCost;
        BaseStrength = ShipBuilder.GetModifiedStrength(info.SurfaceArea, offensiveSlots, baseOffense, baseDefense);
        BaseThrust = baseThrust;
        BaseTurnThrust = baseTurnThrust;
        BaseWarpThrust = baseWarp;
        BaseCanWarp = baseWarp > 0;
        BaseMass = baseMass;
        BaseCargoSpace = baseCargoSpace;
        BaseResearchPerTurn = baseResearchPerTurn;
        BaseRefiningPerTurn = baseProcessingPerTurn;

        StartingColonyGoods = startingColonyGoods;
        NumBuildingsDeployed = numBuildingsDeployed;
        NumConstructionModules = numConstructionModules;

        Hangars = hangars.ToArray();
        AllFighterHangars = Hangars.Filter(h => h.IsFighterHangar);
        Weapons = weapons.ToArray();

        NetPower = Power.Calculate(mTemplates, null, designModule: true);

        // Updating the Design Role is always done in the Shipyard
        // However, it can be overriden with --fix-roles to update all ship designs
        if (updateRole || GlobalStats.FixDesignRoleAndCategory)
        {
            var modules = designSlots.Select(ds => ResourceManager.GetModuleTemplate(ds.ModuleUID));
            var roleData = new RoleData(this, modules);
            Role = roleData.DesignRole;
            ShipCategory = roleData.Category;
        }

        IsPlatformOrStation = Role is RoleName.platform or RoleName.station;
        IsStation           = Role == RoleName.station && !IsShipyard;
        IsConstructor       = Role == RoleName.construction;
        IsSubspaceProjector = Role == RoleName.ssp;
        IsSupplyShuttle     = Role == RoleName.supply;
        IsSingleTroopShip = Role == RoleName.troop;
        IsTroopShip       = Role is RoleName.troop or RoleName.troopShip;
        IsBomber          = Role == RoleName.bomber;
        IsFreighter       = Role == RoleName.freighter && ShipCategory == ShipCategory.Civilian;
        IsCandidateForTradingBuild = IsFreighter && !IsConstructor;
        IsResearchStation = IsPlatformOrStation && BaseResearchPerTurn > 0;
        IsMiningStation   = IsPlatformOrStation && BaseRefiningPerTurn > 0;
        IsDysonSwarmController  = Name == DysonSwarm.DysonSwarmControllerName;

        // only enable this flag for non-testing environment
        IsUnitTestShip = !GlobalStats.IsUnitTest && Name.StartsWith("TEST_");

        // make sure SingleTroopShips are set to Conservative internal damage tolerance
        if (IsSingleTroopShip)
            ShipCategory = ShipCategory.Conservative;
    }

    // Ignore pace is for maintenance calc
    public float GetCost(Empire e, bool ignorePace)
    {
        float pace = ignorePace ? 1 : e.Universe.ProductionPace;
        if (FixedCost > 0)
            return FixedCost * pace;

        float cost = BaseCost * pace;
        cost += Bonuses.StartingCost;
        cost += cost * e.data.Traits.ShipCostMod;
        cost *= 1f - Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
        if (IsPlatformOrStation)
            cost *= 0.7f;

        return (int)cost;
    }

    public float GetMaintenanceCost(Empire empire)
    {
        return ShipMaintenance.GetBaseMaintenance(this, empire, 0);
    }

    public string GetRole()
    {
        return Role.ToString();
    }

    // if this is a valid design which hasn't been broken by bugs kraken
    public bool IsValidDesign => !Deleted && NumDesignSlots != 0 && InvalidModules == null;

    // this is used exclusively by colony screen build ships list
    public bool IsBuildableByPlayer(Empire player)
    {
        return IsValidDesign && !IsUnitTestShip && !IsCarrierOnly && !ShipRole.Protected
            && (player.Universe.P.ShowAllDesigns || IsPlayerDesign);
    }

    // used by AutomationWindow and TechLine focusing
    public bool IsShipGoodToBuild(Empire e)
    {
        if (!IsValidDesign || IsUnitTestShip)
            return false;
        if (IsPlatformOrStation || IsCarrierOnly)
            return true;
        return IsShipGoodForGoals(e);
    }

    // Is this ship good for goals?
    bool IsShipGoodForGoals(Empire e)
    {
        if (!ShipResupply.HasGoodTotalSupplyForResearch(this))
        { 
            if (Name == e.data.DefaultResearchStation)
                Log.Error($"{e.Name}: Default Research Station ({Name}) does not have enough cargo of acceptable research time!");

            return false;
        }

        if (!ShipResupply.HasGoodTotalSupplyForRefining(this))
        {
            if (Name == e.data.DefaultMiningStation)
                Log.Error($"{e.Name}: Default Mining Station ({Name}) does not have enough cargo of acceptable refining time!");

            return false;
        }

        if (IsPlatformOrStation)
            return true;

        float neededRange = GlobalStats.Defaults.MinAcceptableShipWarpRange;
        if (neededRange <= 0f)
            return true;

        float maxFTLSpeed = ShipStats.GetFTLSpeed(this, e);
        bool good = IsWarpRangeGood(neededRange, maxFTLSpeed);
        return good;
    }

    public bool IsWarpRangeGood(float neededRange, float maxFTLSpeed)
    {
        if (neededRange <= 0f) return true;
        float powerDuration = NetPower.PowerDuration(MoveState.Warp, NetPower.PowerStoreMax);
        return ShipStats.IsWarpRangeGood(neededRange, powerDuration, maxFTLSpeed);
    }

    public bool CanBeAddedToBuildableShips(Empire empire)
    {
        return IsValidDesign && !IsUnitTestShip
            && Role != RoleName.prototype
            && Role != RoleName.disabled
            && Role != RoleName.supply
            && !ShipRole.Protected
            && (empire.isPlayer || IsShipGoodForGoals(empire))
            && (!IsPlayerDesign || empire.Universe.P.AIUsesPlayerDesigns || empire.isPlayer);
    }

    public int GetCompletionPercent()
    {
        int slots = DesignSlots.Sum(s => s.Size.X * s.Size.Y);
        return slots == SurfaceArea ? 100 : (int)((slots / (float)SurfaceArea) * 100);
    }
}
