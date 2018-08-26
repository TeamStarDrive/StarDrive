using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Ships
{
    public struct RoleData
    {
        private readonly ShipModule[] Modules;
        private readonly ShipData.RoleName HullRole;
        private readonly ShipData.RoleName DataRole;
        private readonly int Size;
        private readonly Ship Ship;
        private readonly ShipData.Category Category;
        public ShipData.RoleName DesignRole;

        public RoleData(Ship ship, ShipModule[] modules)
        {
            Modules    = modules;
            HullRole   = ship.shipData.HullRole;
            DataRole   = ship.shipData.Role;
            Size       = ship.Size;
            Ship       = ship;
            Category   = ship.shipData.ShipCategory;
            DesignRole = ShipData.RoleName.disabled;
            DesignRole = GetDesignRole();
        }

        public RoleData(ShipData activeHull, ShipModule[] modules)
        {
            Modules    = modules;
            HullRole   = activeHull.HullRole;
            DataRole   = activeHull.Role;
            Size       = activeHull.ModuleSlots.Length;
            Ship       = null;
            Category   = activeHull.ShipCategory;
            DesignRole = ShipData.RoleName.disabled;
            DesignRole = GetDesignRole();
        }

        private ShipData.RoleName GetDesignRole()
        {
            var ship = Ship;
            var modules = Modules;
            var hullRole = HullRole;


            if (ship != null)
            {
                if (ship.isConstructor)
                    return ShipData.RoleName.construction;
                if (ship.isColonyShip || modules.Any(colony => colony.ModuleType == ShipModuleType.Colony))
                    return ShipData.RoleName.colony;
                switch (ship.shipData.Role)
                {
                    case ShipData.RoleName.troop:
                        return ShipData.RoleName.troop;
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.platform:
                        return ship.shipData.Role;
                    case ShipData.RoleName.scout:
                        return ShipData.RoleName.scout;
                }

                if (ship.IsSupplyShip && ship.Weapons.Count == 0)
                    return ShipData.RoleName.supply;
            }
            //troops ship
            if (hullRole >= ShipData.RoleName.freighter)
            {
                float pTroops = PercentageOfShipByModules(modules.FilterBy(troopbay => troopbay.IsTroopBay), Size);
                float pTrans =
                    PercentageOfShipByModules(modules.FilterBy(troopbay => troopbay.TransporterTroopLanding > 0), Size);
                float troops = PercentageOfShipByModules(modules.FilterBy(module => module.TroopCapacity > 0), Size);
                if (pTrans + pTroops + troops > .1f)
                    return ShipData.RoleName.troopShip;
                if (PercentageOfShipByModules(
                        modules.FilterBy(bombBay => bombBay.ModuleType == ShipModuleType.Bomb), Size) > .05f)
                    return ShipData.RoleName.bomber;
                //carrier

                ShipModule[] carrier = modules.FilterBy(hangar => hangar.ModuleType == ShipModuleType.Hangar && !hangar.IsSupplyBay && !hangar.IsTroopBay);
                ShipModule[] support = modules.FilterBy(hangar => hangar.ModuleType == ShipModuleType.Hangar && (hangar.IsSupplyBay || hangar.IsTroopBay));

                if (PercentageOfShipByModules(carrier, Size) > .1)
                    return ShipData.RoleName.carrier;
                if (PercentageOfShipByModules(support, Size) > .1)
                    return ShipData.RoleName.support;
            }
            float pSpecial = PercentageOfShipByModules(modules.FilterBy(module =>
                module.TransporterOrdnance > 0
                || module.IsSupplyBay
                || module.InhibitionRadius > 0
                || module.InstalledWeapon != null && module.InstalledWeapon.DamageAmount < 1 &&
                (module.InstalledWeapon.MassDamage > 0
                 || module.InstalledWeapon.EMPDamage > 0
                 || module.InstalledWeapon.RepulsionDamage > 0
                 || module.InstalledWeapon.SiphonDamage > 0
                 || module.InstalledWeapon.TroopDamageChance > 0
                 || module.InstalledWeapon.isRepairBeam || module.InstalledWeapon.IsRepairDrone)
            ), Size);
            pSpecial += PercentageOfShipByModules(
                modules.FilterBy(repair => repair.InstalledWeapon?.IsRepairDrone == true), Size);

            if (pSpecial > .10f)
                return ShipData.RoleName.support;
            if (Category != ShipData.Category.Unclassified)
            {
                switch (Category)
                {
                    case ShipData.Category.Unclassified:
                        break;
                    case ShipData.Category.Civilian:
                        break;
                    case ShipData.Category.Recon:
                        return ShipData.RoleName.scout;
                    case ShipData.Category.Combat:
                        break;
                    case ShipData.Category.AntiShip:
                        break;
                    case ShipData.Category.Interceptor:
                        break;
                    case ShipData.Category.Kamikaze:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ShipData.RoleName fixRole = DataRole == ShipData.RoleName.prototype ? DataRole : hullRole;

            switch (fixRole)
            {
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.gunboat:
                    return ShipData.RoleName.corvette;
                case ShipData.RoleName.carrier:
                case ShipData.RoleName.capital:
                    return ShipData.RoleName.capital;
                case ShipData.RoleName.destroyer:
                case ShipData.RoleName.frigate:
                    return ShipData.RoleName.frigate;
                case ShipData.RoleName.scout:
                case ShipData.RoleName.fighter:
                    return modules.Any(weapons => weapons.InstalledWeapon != null)
                        ? ShipData.RoleName.fighter
                        : ShipData.RoleName.scout;
            }

            return hullRole;
        }

        public static float PercentageOfShipByModules(ShipModule[] modules, int size)
        {
            int area = 0;
            int count = modules.Length;
            for (int i = 0; i < count; ++i)
            {
                ShipModule module = modules[i];
                area += module.XSIZE * module.YSIZE;
            }
            return area > 0 ? area / (float)size : 0.0f;
        }

        public void CreateDesignRoleToolTip(Rectangle designRoleRect) => CreateDesignRoleToolTip(DesignRole, Fonts.Arial12, designRoleRect, true);
        public static void CreateDesignRoleToolTip(ShipData.RoleName designRole, Rectangle designRoleRect) 
            => CreateDesignRoleToolTip(designRole, Fonts.Arial12, designRoleRect, true);

        public static void CreateDesignRoleToolTip(ShipData.RoleName role, SpriteFont roleFont, Rectangle designRoleRect, bool alwaysShow = false)
        {
            var text = $"Autoassigned Role Change\n{RoleDesignString(role)}";
            var spacing = roleFont.MeasureString(text);
            var pos = new Vector2(designRoleRect.Left,
                designRoleRect.Y - spacing.Y - designRoleRect.Height - roleFont.LineSpacing);
            ToolTip.CreateTooltip(text, pos, alwaysShow);
        }

        private static string RoleDesignString(ShipData.RoleName role)
        {
            string roleInfo = "";
            switch (role)
            {
                case ShipData.RoleName.disabled:
                    break;
                case ShipData.RoleName.platform:
                    roleInfo = "Has Platform hull";
                    break;
                case ShipData.RoleName.station:
                    roleInfo = "Has Station hull";
                    break;
                case ShipData.RoleName.construction:
                    roleInfo = "Has construction role";
                    break;
                case ShipData.RoleName.colony:
                    roleInfo = "Has Colony Module";
                    break;
                case ShipData.RoleName.supply:
                    roleInfo = "Has supply role";
                    break;
                case ShipData.RoleName.freighter:
                    roleInfo = "Has freighter hull";
                    break;
                case ShipData.RoleName.troop:
                    break;
                case ShipData.RoleName.troopShip:
                    roleInfo = "10% of the ship space taken by troop launch bays";
                    break;
                case ShipData.RoleName.support:
                    roleInfo = "10% of ship space taken by support modules";
                    break;
                case ShipData.RoleName.bomber:
                    roleInfo = "10% of ship space taken by bomb modules";
                    break;
                case ShipData.RoleName.carrier:
                    roleInfo = "10% of ship space taken by hangar modules";
                    break;
                case ShipData.RoleName.fighter:
                    roleInfo = "Has fighter hull";
                    break;
                case ShipData.RoleName.scout:
                    roleInfo = "Fighter hull with no weapons";
                    break;
                case ShipData.RoleName.gunboat:
                    roleInfo = "Not assigned";
                    break;
                case ShipData.RoleName.drone:
                    roleInfo = "Has Drone hull";
                    break;
                case ShipData.RoleName.corvette:
                    roleInfo = "Has corvette hull";
                    break;
                case ShipData.RoleName.frigate:
                    roleInfo = "Has Frigate hull";
                    break;
                case ShipData.RoleName.destroyer:
                    roleInfo = "Has destroyer role";
                    break;
                case ShipData.RoleName.cruiser:
                    roleInfo = "Has cruiser role";
                    break;
                case ShipData.RoleName.capital:
                    roleInfo = "Has Capital hull";
                    break;
                case ShipData.RoleName.prototype:
                    roleInfo = "Has Colony Module";
                    break;
                default:
                    {
                        break;
                    }
            }
            return roleInfo;
        }
    }
}
