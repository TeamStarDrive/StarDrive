using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game.Ships
{
    public struct RoleData
    {
        private readonly ShipModule[] Modules;
        private readonly ShipData.RoleName HullRole;
        private readonly ShipData.RoleName DataRole;
        private readonly int SurfaceArea;
        private readonly Ship Ship;
        private readonly ShipData.Category Category;
        public ShipData.RoleName DesignRole;

        public RoleData(Ship ship, ShipModule[] modules)
        {
            Modules     = modules;
            HullRole    = ship.shipData.HullRole;
            DataRole    = ship.shipData.Role;
            SurfaceArea = ship.SurfaceArea;
            Ship        = ship;
            Category    = ship.shipData.ShipCategory;
            DesignRole  = ShipData.RoleName.disabled;
            DesignRole  = GetDesignRole();
        }

        public RoleData(ShipData activeHull, ShipModule[] modules)
        {
            Modules     = modules;
            HullRole    = activeHull.HullRole;
            DataRole    = activeHull.Role;
            SurfaceArea = activeHull.ModuleSlots.Length;
            Ship        = null;
            Category    = activeHull.ShipCategory;
            DesignRole  = ShipData.RoleName.disabled;
            DesignRole  = GetDesignRole();
        }

        private ShipData.RoleName GetDesignRole()
        {
            if (Ship != null)
            {
                if (Ship.shipData.Role == ShipData.RoleName.prototype)
                    return ShipData.RoleName.prototype;

                if (Ship.IsConstructor)
                    return ShipData.RoleName.construction;
                if (Ship.IsSubspaceProjector)
                    return ShipData.RoleName.ssp;
                if (Ship.shipData.IsShipyard)
                    return ShipData.RoleName.shipyard;

                if (Ship.isColonyShip || Modules.Any(ShipModuleType.Colony))
                    return ShipData.RoleName.colony;

                switch (Ship.shipData.Role)
                {
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.platform: return Ship.shipData.Role;
                    case ShipData.RoleName.scout:    return ShipData.RoleName.scout;
                    case ShipData.RoleName.troop:    return ShipData.RoleName.troop;
                }

                if (Ship.IsSupplyShip && Ship.Weapons.Count == 0)
                    return ShipData.RoleName.supply;
                
                if (HullRole == ShipData.RoleName.freighter && Category == ShipData.Category.Civilian
                                && SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.5f)
                {
                    return ShipData.RoleName.freighter;
                }
            }

            // troops ship
            if (HullRole >= ShipData.RoleName.freighter)
            {
                if (Modules.Any(ShipModuleType.Construction))
                    return ShipData.RoleName.construction;

                if (SurfaceAreaPercentOf(m => m.IsTroopBay || m.TransporterTroopLanding > 0 || m.TroopCapacity > 0) > 0.1f
                    && Modules.Any(m => m.IsTroopBay)) // At least 1 troop bay as well
                {
                    return ShipData.RoleName.troopShip;
                }

                if (SurfaceAreaPercentOf(ShipModuleType.Bomb) > 0.05f)
                    return ShipData.RoleName.bomber;

                if (SurfaceAreaPercentOf(m => m.ModuleType == ShipModuleType.Hangar && !m.IsSupplyBay && !m.IsTroopBay) > 0.1f)
                    return ShipData.RoleName.carrier;

                if (SurfaceAreaPercentOf(m => m.ModuleType == ShipModuleType.Hangar && (m.IsSupplyBay || m.IsTroopBay)) > 0.1f)
                    return ShipData.RoleName.support;
                // check freighter role. If ship is unclassified or is not a freighter hull and is classified civilian
                // check for useability as freighter.
                // small issue is that ships that are classified civilian will behave as civilian ships.
                // currently the category can not be set here while in the shipyard.
                if (Ship == null || Category <= ShipData.Category.Civilian)
                {
                    // non freighter hull must be set to civilian to be set as freighters.
                    if (HullRole > ShipData.RoleName.freighter)
                    {
                        if (SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.5f && Category == ShipData.Category.Civilian)
                            return ShipData.RoleName.freighter;
                    }
                    // freighter hull will be set to civilian if useable as freighter.
                    // if not useable as freighter it will set the cat to Unclassified
                    else if (HullRole == ShipData.RoleName.freighter)
                    {
                        if (SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.01f)
                        {
                            if (Ship != null)
                                Ship.shipData.ShipCategory = ShipData.Category.Civilian;
                            return ShipData.RoleName.freighter;
                        }
                        // This is for updating the ship and no use if there is no ship. 
                        if (Ship?.shipData.ShipCategory == ShipData.Category.Civilian)
                        {
                            Ship.shipData.ShipCategory = ShipData.Category.Unclassified;
                            Log.Warning($"Freighter {Ship.Name} category was reverted to unclassified as it cant be used as civilian ship");
                        }
                    }
                }
            }

            float pSpecial = SurfaceAreaPercentOf(m =>
                m.TransporterOrdnance > 0
                || m.IsSupplyBay
                || m.InhibitionRadius > 0
                || m.InstalledWeapon?.DamageAmount < 1  &&
                (  m.InstalledWeapon.MassDamage > 0
                 || m.InstalledWeapon.EMPDamage > 0
                 || m.InstalledWeapon.RepulsionDamage > 0
                 || m.InstalledWeapon.SiphonDamage > 0
                 || m.InstalledWeapon.TroopDamageChance > 0
                 || m.InstalledWeapon.isRepairBeam 
                 || m.InstalledWeapon.IsRepairDrone)
            );

            if (pSpecial > 0.10f)
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
                    case ShipData.Category.Conservative:
                        break;
                    case ShipData.Category.Neutral:
                        break;
                    case ShipData.Category.Reckless:
                        break;
                    case ShipData.Category.Kamikaze:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ShipData.RoleName fixRole = DataRole == ShipData.RoleName.prototype ? DataRole : HullRole;
            switch (fixRole)
            {
                case ShipData.RoleName.corvette:
                case ShipData.RoleName.gunboat: return ShipData.RoleName.corvette;
                case ShipData.RoleName.carrier:
                case ShipData.RoleName.capital: return ShipData.RoleName.capital;
                case ShipData.RoleName.destroyer:
                case ShipData.RoleName.frigate: return ShipData.RoleName.frigate;
                case ShipData.RoleName.scout:
                case ShipData.RoleName.fighter:
                    return Modules.Any(weapons => weapons.InstalledWeapon != null)
                        ? ShipData.RoleName.fighter
                        : ShipData.RoleName.scout;
            }
            return HullRole;
        }

        private float SurfaceAreaPercentOf(Func<ShipModule, bool> predicate)
        {
            return Modules.SurfaceArea(predicate) / (float)SurfaceArea;
        }

        private float SurfaceAreaPercentOf(ShipModuleType moduleType)
        {
            return Modules.SurfaceArea(moduleType) / (float)SurfaceArea;
        }

        public static void CreateDesignRoleToolTip(ShipData.RoleName designRole, Rectangle designRoleRect)
            => CreateDesignRoleToolTip(designRole, Fonts.Arial12, designRoleRect);

        public static void CreateDesignRoleToolTip(ShipData.RoleName role, SpriteFont roleFont, Rectangle designRoleRect)
        {
            string text = $"Autoassigned Role Change\n{RoleDesignString(role)}";
            Vector2 spacing = roleFont.MeasureString(text);
            var pos = new Vector2(designRoleRect.Left,
                designRoleRect.Y - spacing.Y - designRoleRect.Height - roleFont.LineSpacing);
            ToolTip.CreateTooltip(text, "", pos);
        }

        private static string RoleDesignString(ShipData.RoleName role)
        {
            switch (role)
            {
                case ShipData.RoleName.disabled:     return "";
                case ShipData.RoleName.platform:     return "Has Platform hull";
                case ShipData.RoleName.station:      return "Has Station hull";
                case ShipData.RoleName.construction: return "Has construction role";
                case ShipData.RoleName.colony:       return "Has Colony Module";
                case ShipData.RoleName.supply:       return "Has supply role";
                case ShipData.RoleName.freighter:    return "Has freighter hull";
                case ShipData.RoleName.troop:        return "";
                case ShipData.RoleName.troopShip: return "10% of the ship space taken by troop launch bays";
                case ShipData.RoleName.support:   return "10% of ship space taken by support modules";
                case ShipData.RoleName.bomber:    return "10% of ship space taken by bomb modules";
                case ShipData.RoleName.carrier:   return "10% of ship space taken by hangar modules";
                case ShipData.RoleName.fighter:   return "Has fighter hull";
                case ShipData.RoleName.scout:     return "Fighter hull with no weapons";
                case ShipData.RoleName.gunboat:   return "Not assigned";
                case ShipData.RoleName.drone:     return "Has Drone hull";
                case ShipData.RoleName.corvette:  return "Has corvette hull";
                case ShipData.RoleName.frigate:   return "Has Frigate hull";
                case ShipData.RoleName.destroyer: return "Has destroyer role";
                case ShipData.RoleName.cruiser:   return "Has cruiser role";
                case ShipData.RoleName.capital:   return "Has Capital hull";
                case ShipData.RoleName.prototype: return "Has Colony Module";
            }
            return "";
        }
    }
}
