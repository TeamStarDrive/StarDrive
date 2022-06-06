using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    // This part of ShipDesign contains all the useful Stats
    // that are common across this ShipDesign
    public partial class ShipDesign
    {
        // Role assigned to the Hull, such as `Cruiser`
        public RoleName HullRole => BaseHull.Role;

        static readonly string[] RoleArray = typeof(RoleName).GetEnumNames();
        public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        public bool IsPlatformOrStation { get; private set; }
        public bool IsStation           { get; private set; }
        public bool IsConstructor       { get; private set; }
        public bool IsSubspaceProjector { get; private set; }
        public bool IsColonyShip        { get; private set; }
        public bool IsSupplyCarrier     { get; private set; } // this ship launches supply ships
        public bool IsSupplyShuttle     { get; private set; }
        public bool IsFreighter         { get; private set; }
        public bool IsCandidateForTradingBuild { get; private set; }

        public bool IsSingleTroopShip { get; private set; }
        public bool IsTroopShip       { get; private set; }
        public bool IsBomber          { get; private set; }

        public float BaseCost       { get; private set; }
        public float BaseWarpThrust { get; private set; }
        public bool  BaseCanWarp    { get; private set; }

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
            Grid = new ModuleGridFlyweight(Name, info, designSlots);

            float baseCost = 0f;
            float baseWarp = 0f;
            var hangars = new Array<ShipModule>();
            var weapons = new Array<Weapon>();
            HashSet<string> invalidModules = null;

            for (int i = 0; i < designSlots.Length; i++)
            {
                string uid = designSlots[i].ModuleUID;
                if (!ResourceManager.GetModuleTemplate(uid, out ShipModule m))
                {
                    if (invalidModules == null)
                        invalidModules = new HashSet<string>();
                    invalidModules.Add(uid);
                    continue;
                }

                baseCost += m.Cost;
                baseWarp += m.WarpThrust;
                if (m.Is(ShipModuleType.Hangar))
                    hangars.Add(m);
                else if (m.Is(ShipModuleType.Colony))
                    IsColonyShip = true;
                else if (m.InstalledWeapon != null)
                    weapons.Add(m.InstalledWeapon);

                if (m.IsSupplyBay)
                    IsSupplyCarrier = true;
            }

            if (invalidModules != null)
            {
                InvalidModules = string.Join(" ", invalidModules);
                Log.Warning(ConsoleColor.Red, $"ShipDesign '{Name}' InvalidModules='{InvalidModules}' Source='{Source.FullName}'");
            }

            BaseCost = baseCost;
            BaseWarpThrust = baseWarp;
            BaseCanWarp = baseWarp > 0;

            Hangars = hangars.ToArray();
            AllFighterHangars = Hangars.Filter(h => h.IsFighterHangar);
            Weapons = weapons.ToArray();

            // Updating the Design Role is always done in the Shipyard
            // However, it can be overriden with --fix-roles to update all ship designs
            if (updateRole || GlobalStats.FixDesignRoleAndCategory)
            {
                var modules = designSlots.Select(ds => ResourceManager.GetModuleTemplate(ds.ModuleUID));
                var roleData = new RoleData(this, modules);
                Role = roleData.DesignRole;
                ShipCategory = roleData.Category;
            }

            IsPlatformOrStation = Role == RoleName.platform || Role == RoleName.station;
            IsStation           = Role == RoleName.station && !IsShipyard;
            IsConstructor       = Role == RoleName.construction;
            IsSubspaceProjector = Role == RoleName.ssp;
            IsSupplyShuttle     = Role == RoleName.supply;
            IsSingleTroopShip = Role == RoleName.troop;
            IsTroopShip       = Role == RoleName.troop || Role == RoleName.troopShip;
            IsBomber          = Role == RoleName.bomber;
            IsFreighter       = Role == RoleName.freighter && ShipCategory == ShipCategory.Civilian;
            IsCandidateForTradingBuild = IsFreighter && !IsConstructor;

            // make sure SingleTroopShips are set to Conservative internal damage tolerance
            if (IsSingleTroopShip)
                ShipCategory = ShipCategory.Conservative;
        }

        public float GetCost(Empire e)
        {
            if (FixedCost > 0)
                return FixedCost * CurrentGame.ProductionPace;

            float cost = BaseCost * CurrentGame.ProductionPace;
            cost += Bonuses.StartingCost;
            cost += cost * e.data.Traits.ShipCostMod;
            cost *= 1f - Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
            if (IsPlatformOrStation)
                cost *= 0.7f;

            return (int)cost;
        }

        public float GetMaintenanceCost(Empire empire, int troopCount)
        {
            return ShipMaintenance.GetBaseMaintenance(this, empire, troopCount);
        }

        public string GetRole()
        {
            return RoleArray[(int)Role -1];
        }

        public static string GetRole(RoleName role)
        {
            int roleNum = (int)role - 1;
            return RoleArray[roleNum];
        }
    }
}
