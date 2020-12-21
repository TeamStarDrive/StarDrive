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
                    && Modules.Any(m => m.IsTroopBay) // At least 1 troop bay as well
                    && Modules.Any(m => m.TroopCapacity > 0)) // At least 1 troop capacity
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

        public static void CreateDesignRoleToolTip(ShipData.RoleName role, Rectangle designRoleRect, bool floatingText, Vector2 pos)
        {
            SpriteFont roleFont = Fonts.Arial12;
            string text         = $"Ship Role was Changed to {RoleDesignString(role)}";
            float floatTime     = floatingText ? text.Length / 10 : 0;
            Vector2 spacing     = roleFont.MeasureString(text);

            if (pos == Vector2.Zero)
            {
                float tipY = designRoleRect.Y - spacing.Y * 2 - designRoleRect.Height - roleFont.LineSpacing;
                pos = new Vector2(designRoleRect.Left, tipY);
            }

            ToolTip.CreateFloatingText(text, "", pos, floatTime.UpperBound(7));
        }

        private static string RoleDesignString(ShipData.RoleName role)
        {
            switch (role)
            {
                default:
                case ShipData.RoleName.troop:
                case ShipData.RoleName.disabled:     return "";
                case ShipData.RoleName.platform:     return "'Platform'";
                case ShipData.RoleName.station:      return "'Station'";
                case ShipData.RoleName.construction: return "'Construction'";
                case ShipData.RoleName.colony:       return "'Colony' (has a Colony Module)";
                case ShipData.RoleName.supply:       return "'Supply'";
                case ShipData.RoleName.freighter:    return "'Freighter'";
                case ShipData.RoleName.troopShip:    return "'Troop Ship', as 10% of the ship space is taken by troop launch bays or barracks";
                case ShipData.RoleName.support:      return "'Support', as 10% of ship space is taken by support modules";
                case ShipData.RoleName.bomber:       return "'Bomber', as 10% of ship space is taken by bomb modules";
                case ShipData.RoleName.carrier:      return "'Carrier', as 10% of ship space is taken by hangar modules";
                case ShipData.RoleName.fighter:      return "'Fighter'";
                case ShipData.RoleName.scout:        return "'Scout' since it has";
                case ShipData.RoleName.gunboat:      return "'Gunboat'";
                case ShipData.RoleName.drone:        return "'Drone'";
                case ShipData.RoleName.corvette:     return "'Corvette'";
                case ShipData.RoleName.frigate:      return "'Frigate'";
                case ShipData.RoleName.destroyer:    return "'Destroyer'";
                case ShipData.RoleName.cruiser:      return "'Cruiser'";
                case ShipData.RoleName.capital:      return "'Capital'";
                case ShipData.RoleName.prototype:    return "'Prototype'";
            }
        }
    }
}
