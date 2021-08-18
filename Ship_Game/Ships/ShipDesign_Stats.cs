using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    // This part of ShipDesign contains all the useful Stats
    // that are common across this ShipDesign
    public partial class ShipDesign
    {
        static readonly string[] RoleArray = typeof(RoleName).GetEnumNames();

        // Role assigned to the Hull
        public RoleName HullRole => BaseHull.Role;

        // TODO: Wtf is this?? DataRole ?
        public RoleName Role = RoleName.fighter;
        public ShipRole ShipRole => ResourceManager.ShipRoles[Role];

        // Role expressed by this ShipDesign's modules, calculated dynamically
        public RoleName DesignRole { get; private set; }

        public bool IsPlatformOrStation { get; private set; }
        public bool IsStation           { get; private set; }
        public bool IsConstructor       { get; private set; }
        public bool IsSubspaceProjector { get; private set; }
        public bool IsColonyShip        { get; private set; }
        public bool IsSupplyShip        { get; private set; }
        public bool IsFreighter         { get; private set; }
        public bool IsCandidateForTradingBuild { get; private set; }

        public bool IsSingleTroopShip { get; private set; }
        public bool IsTroopShip       { get; private set; }
        public bool IsBomber          { get; private set; }

        public float BaseCost { get; private set; }
        public float BaseWarpThrust { get; private set; }

        // Hangar Templates
        public ShipModule[] Hangars { get; private set; }
        public ShipModule[] AllFighterHangars { get; private set; }

        // Weapon Templates
        public Weapon[] Weapons { get; private set; }

        void InitializeCommonStats(ShipHull hull, DesignSlot[] designSlots)
        {
            if (ShipStyle.IsEmpty()) ShipStyle = hull.Style;
            if (IconPath.IsEmpty())  IconPath  = hull.IconPath;
            GridInfo.SurfaceArea = hull.SurfaceArea;

            ShipModule[] modules = designSlots.Select(ds => ResourceManager.GetModuleTemplate(ds.ModuleUID));
            float baseCost = 0f;
            float baseWarp = 0f;
            var hangars = new Array<ShipModule>();
            var weapons = new Array<Weapon>();

            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule m = modules[i];
                baseCost += m.Cost;
                baseWarp += m.WarpThrust;
                if (m.Is(ShipModuleType.Hangar))
                    hangars.Add(m);
                else if (m.Is(ShipModuleType.Colony))
                    IsColonyShip = true;
                else if (m.InstalledWeapon != null)
                    weapons.Add(m.InstalledWeapon);

                if (m.IsSupplyBay)
                    IsSupplyShip = true;
            }

            BaseCost = baseCost;
            BaseWarpThrust = baseWarp;
            Hangars = hangars.ToArray();
            AllFighterHangars = Hangars.Filter(h => h.IsFighterHangar);
            Weapons = weapons.ToArray();

            var roleData = new RoleData(this, modules);
            DesignRole = roleData.DesignRole;
            ShipCategory = roleData.Category;
            IsPlatformOrStation = DesignRole == RoleName.platform || DesignRole == RoleName.station;
            IsStation           = DesignRole == RoleName.station && !IsShipyard;
            IsConstructor       = DesignRole == RoleName.construction;
            IsSubspaceProjector = DesignRole == RoleName.ssp;
            IsSingleTroopShip = DesignRole == RoleName.troop;
            IsTroopShip       = DesignRole == RoleName.troop || DesignRole == RoleName.troopShip;
            IsBomber          = DesignRole == RoleName.bomber;
            IsFreighter       = DesignRole == RoleName.freighter && ShipCategory == ShipCategory.Civilian;
            IsCandidateForTradingBuild = IsFreighter && !IsConstructor;
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
