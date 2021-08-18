using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game.Ships
{
    public struct RoleData
    {
        readonly ShipDesign Ship;
        readonly ShipModule[] Modules;
        readonly RoleName HullRole; // role defined in the .hull file
        readonly RoleName DataRole; // role defined in the .design file
        readonly int SurfaceArea;

        // these are the outputs:
        public ShipCategory Category;
        public RoleName DesignRole;

        public RoleData(ShipDesign ship, ShipModule[] modules)
        {
            Ship        = ship;
            Modules     = modules;
            HullRole    = ship.HullRole;
            DataRole    = ship.Role;
            SurfaceArea = ship.BaseHull.SurfaceArea;
            Category    = ship.ShipCategory;
            DesignRole  = RoleName.disabled;
            DesignRole  = GetDesignRole();
        }

        RoleName GetDesignRole()
        {
            if (DataRole == RoleName.prototype)    return RoleName.prototype;
            if (DataRole == RoleName.supply)       return RoleName.supply;
            if (DataRole == RoleName.construction) return RoleName.construction;
            if (DataRole == RoleName.station)      return RoleName.station;
            if (DataRole == RoleName.platform)     return RoleName.platform;
            if (DataRole == RoleName.scout)        return RoleName.scout;
            if (DataRole == RoleName.troop)        return RoleName.troop;
            if (Ship.Name == "Subspace Projector") return RoleName.ssp;
            if (Ship.IsShipyard)                   return RoleName.shipyard;

            if (Ship.IsColonyShip || Modules.Any(ShipModuleType.Colony))
                return RoleName.colony;

            if (Ship.IsSupplyShip && Ship.Weapons.Length == 0)
                return RoleName.supply;

            if (DataRole == RoleName.freighter && Category == ShipCategory.Civilian &&
                SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.5f)
            {
                return RoleName.freighter;
            }

            // troops ship
            if (HullRole >= RoleName.freighter)
            {
                if (Modules.Any(ShipModuleType.Construction))
                    return RoleName.construction;

                if (SurfaceAreaPercentOf(m => m.IsTroopBay || m.TransporterTroopLanding > 0 || m.TroopCapacity > 0) > 0.1f
                    && Modules.Any(m => m.IsTroopBay) // At least 1 troop bay as well
                    && Modules.Any(m => m.TroopCapacity > 0)) // At least 1 troop capacity
                {
                    return RoleName.troopShip;
                }

                if (SurfaceAreaPercentOf(ShipModuleType.Bomb) > 0.05f)
                    return RoleName.bomber;

                if (SurfaceAreaPercentOf(m => m.ModuleType == ShipModuleType.Hangar && !m.IsSupplyBay && !m.IsTroopBay) > 0.1f)
                    return RoleName.carrier;

                if (SurfaceAreaPercentOf(m => m.ModuleType == ShipModuleType.Hangar && (m.IsSupplyBay || m.IsTroopBay)) > 0.1f)
                    return RoleName.support;
                // check freighter role. If ship is unclassified or is not a freighter hull and is classified civilian
                // check for useability as freighter.
                // small issue is that ships that are classified civilian will behave as civilian ships.
                // currently the category can not be set here while in the shipyard.
                if (Category <= ShipCategory.Civilian)
                {
                    // non freighter hull must be set to civilian to be set as freighters.
                    if (HullRole > RoleName.freighter)
                    {
                        if (SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.5f && Category == ShipCategory.Civilian)
                            return RoleName.freighter;
                    }
                    // freighter hull will be set to civilian if useable as freighter.
                    // if not useable as freighter it will set the cat to Unclassified
                    else if (HullRole == RoleName.freighter)
                    {
                        if (SurfaceAreaPercentOf(m => m.Cargo_Capacity > 0) >= 0.01f)
                        {
                            Category = ShipCategory.Civilian;
                            return RoleName.freighter;
                        }
                        // This is for updating the ship and no use if there is no ship. 
                        if (Category == ShipCategory.Civilian)
                        {
                            Category = ShipCategory.Unclassified;
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
                return RoleName.support;

            if (Category != ShipCategory.Unclassified)
            {
                switch (Category)
                {
                    case ShipCategory.Unclassified:
                        break;
                    case ShipCategory.Civilian:
                        break;
                    case ShipCategory.Recon:
                        return RoleName.scout;
                    case ShipCategory.Conservative:
                        break;
                    case ShipCategory.Neutral:
                        break;
                    case ShipCategory.Reckless:
                        break;
                    case ShipCategory.Kamikaze:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            RoleName fixRole = DataRole == RoleName.prototype ? DataRole : HullRole;
            switch (fixRole)
            {
                case RoleName.corvette:
                case RoleName.gunboat: return RoleName.corvette;
                case RoleName.carrier: return RoleName.battleship;
                case RoleName.capital: return RoleName.capital;
                case RoleName.destroyer:
                case RoleName.frigate: return RoleName.frigate;
                case RoleName.scout:
                case RoleName.fighter:
                    return Modules.Any(weapons => weapons.InstalledWeapon != null)
                        ? RoleName.fighter
                        : RoleName.scout;
            }
            return HullRole;
        }

        float SurfaceAreaPercentOf(Func<ShipModule, bool> predicate)
        {
            return Modules.SurfaceArea(predicate) / (float)SurfaceArea;
        }

        float SurfaceAreaPercentOf(ShipModuleType moduleType)
        {
            return Modules.SurfaceArea(moduleType) / (float)SurfaceArea;
        }

        public static void CreateDesignRoleToolTip(RoleName role, Rectangle designRoleRect, bool floatingText, Vector2 pos)
        {
            Graphics.Font roleFont = Fonts.Arial12;
            string text = $"Ship Role was Changed to {RoleDesignString(role)}";
            float floatTime = floatingText ? text.Length / 10 : 0;
            Vector2 spacing = roleFont.MeasureString(text);

            if (pos == Vector2.Zero)
            {
                float tipY = designRoleRect.Y - spacing.Y * 2 - designRoleRect.Height - roleFont.LineSpacing;
                pos = new Vector2(designRoleRect.Left, tipY);
            }

            ToolTip.CreateFloatingText(text, "", pos, floatTime.UpperBound(7));
        }

        static string RoleDesignString(RoleName role)
        {
            switch (role)
            {
                default:
                case RoleName.troop:
                case RoleName.disabled:     return "";
                case RoleName.platform:     return "'Platform'";
                case RoleName.station:      return "'Station'";
                case RoleName.construction: return "'Construction'";
                case RoleName.colony:       return "'Colony' (has a Colony Module)";
                case RoleName.supply:       return "'Supply'";
                case RoleName.freighter:    return "'Freighter'";
                case RoleName.troopShip:    return "'Troop Ship', as 10% of the ship space is taken by troop launch bays or barracks";
                case RoleName.support:      return "'Support', as 10% of ship space is taken by support modules";
                case RoleName.bomber:       return "'Bomber', as 10% of ship space is taken by bomb modules";
                case RoleName.carrier:      return "'Carrier', as 10% of ship space is taken by hangar modules";
                case RoleName.fighter:      return "'Fighter'";
                case RoleName.scout:        return "'Scout' since it has";
                case RoleName.gunboat:      return "'Gunboat'";
                case RoleName.drone:        return "'Drone'";
                case RoleName.corvette:     return "'Corvette'";
                case RoleName.frigate:      return "'Frigate'";
                case RoleName.destroyer:    return "'Destroyer'";
                case RoleName.cruiser:      return "'Cruiser'";
                case RoleName.battleship:   return "'battleship'";
                case RoleName.capital:      return "'Capital'";
                case RoleName.prototype:    return "'Prototype'";
            }
        }
    }
}
