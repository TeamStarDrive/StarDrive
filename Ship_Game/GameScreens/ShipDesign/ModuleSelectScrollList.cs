using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class ModuleSelectScrollList : ScrollList<ModuleSelectListItem>
    {
        public readonly ShipDesignScreen Screen;

        public ModuleSelectScrollList(Submenu weaponList, ShipDesignScreen shipDesignScreen) : base(weaponList)
        {
            Screen = shipDesignScreen;
            EnableItemHighlight = true;
        }

        public override void OnItemClicked(ScrollListItemBase item)
        {
            var weaponItem = (ModuleSelectListItem)item;
            if (weaponItem.Module != null)
            {
                Screen.SetActiveModule(weaponItem.Module, ModuleOrientation.Normal, 0f);
            }
            base.OnItemClicked(item);
        }

        bool IsBadModuleSize(ShipModule module)
        {
            if (Screen.Input.IsShiftKeyDown || Screen.ActiveHull == null || module.XSIZE + module.YSIZE == 2)
                return false;
            return Screen.IsBadModuleSize(module);
        }

        bool IsGoodModuleSize(ShipModule module)
        {
            return !IsBadModuleSize(module);
        }

        readonly Map<int, ModuleSelectListItem> Categories = new Map<int, ModuleSelectListItem>();

        void AddCategoryItem(int categoryId, string categoryName, ShipModule mod)
        {
            if (!IsGoodModuleSize(mod))
                return;
            if (!Categories.TryGetValue(categoryId, out ModuleSelectListItem e))
            {
                e = AddItem(new ModuleSelectListItem(categoryName));
                Categories.Add(categoryId, e);
            }
            e.AddSubItem(new ModuleSelectListItem(mod));
        }

        bool OpenCategory(int categoryId)
        {
            if (Categories.TryGetValue(categoryId, out ModuleSelectListItem e))
            {
                e.Expand(true);
                return true;
            }
            return false;
        }

        void OpenCategoryByIndex(int index)
        {
            if (index < NumEntries)
            {
                this[index].Expand(true);
            }
        }

        public void SetActiveCategory(int index)
        {
            Reset();
            Categories.Clear();
            switch (index)
            {
                default:
                case 0: AddWeaponCategories();  break;
                case 1: AddPowerCategories();   break;
                case 2: AddDefenseCategories(); break;
                case 3: AddSpecialCategories(); break;
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);
        }

        bool IsModuleAvailable(ShipModule template, out ShipModule tmp)
        {
            tmp = null;

            if (!EmpireManager.Player.IsModuleUnlocked(template.UID) || template.UID == "Dummy")
                return false;

            if (RestrictedModCheck(Screen.ActiveHull.Role, template))
                return false;

            tmp = Screen.CreateDesignModule(template);
            return true;
        }

        Array<ShipModule> SortedModules
        {
            get
            {
                var modules = new Array<ShipModule>(); // gather into a list so we can sort the modules logically
                foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
                    if (IsModuleAvailable(template, out ShipModule tmp))
                        modules.Add(tmp);
                
                modules.Sort((a, b) =>
                {
                    // PowerConduit must always be first, so give them ordinal 0
                    int ta = a.ModuleType == ShipModuleType.PowerConduit ? 0 : (int)a.ModuleType;
                    int tb = b.ModuleType == ShipModuleType.PowerConduit ? 0 : (int)b.ModuleType;
                    if (ta != tb) return ta - tb;

                    // then by Area
                    int aa = a.Area;
                    int ab = b.Area;
                    if (aa != ab) return aa - ab;

                    // and finally by UID
                    return string.Compare(a.UID, b.UID, StringComparison.Ordinal);
                });
                return modules;
            }
        }

        void AddWeaponCategories()
        {
            foreach (ShipModule m in SortedModules)
            {
                if (m.isWeapon)
                {
                    Weapon w = m.InstalledWeapon;
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats)
                    {
                        if      (w.Tag_Flak)    AddCategoryItem(400, "Flak Cannon", m);
                        else if (w.Tag_Railgun) AddCategoryItem(401, "Magnetic Cannon", m);
                        else if (w.Tag_Array)   AddCategoryItem(401, "Beam Array", m);
                        else if (w.Tag_Tractor) AddCategoryItem(401, "Tractor Beam", m);
                        else if (w.Tag_Missile
                             && !w.Tag_Guided)  AddCategoryItem(401, "Unguided Rocket", m);
                        else                    AddCategoryItem(w.WeaponType.GetHashCode(), w.WeaponType, m);
                    }
                    else
                    {
                        AddCategoryItem(w.WeaponType.GetHashCode(), w.WeaponType, m);
                    }
                }
                else if (m.ModuleType == ShipModuleType.Bomb)
                {
                    AddCategoryItem("Bomb".GetHashCode(), "Bomb", m);
                }
            }
            OpenCategoryByIndex(0);
        }

        void AddPowerCategories()
        {
            foreach (ShipModule m in SortedModules)
            {
                ShipModuleType type = m.ModuleType;
                if (type == ShipModuleType.PowerConduit) // force PowerConduit into PowerPlant category
                    type = ShipModuleType.PowerPlant;

                if (type == ShipModuleType.PowerPlant || type == ShipModuleType.FuelCell || type == ShipModuleType.Engine)
                    AddCategoryItem((int)type, type.ToString(), m);
            }
            if (!OpenCategory((int)ShipModuleType.PowerPlant))
                OpenCategoryByIndex(0);
        }

        void AddDefenseCategories()
        {
            foreach (ShipModule m in SortedModules)
            {
                ShipModuleType type = m.ModuleType;
                if (type == ShipModuleType.Shield ||
                    type == ShipModuleType.Countermeasure ||
                    (type == ShipModuleType.Armor && !m.isBulkhead && !m.isPowerArmour))
                {
                    AddCategoryItem((int)type, type.ToString(), m);
                }
                // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType
                // is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                else if (m.isPowerArmour && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6172, Localizer.Token(6172), m);
                }
                else if (m.isBulkhead && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6173, Localizer.Token(6173), m);
                }
            }
            if (!OpenCategory((int)ShipModuleType.Shield))
                OpenCategoryByIndex(0);
        }

        void AddSpecialCategories()
        {
            foreach (ShipModule m in SortedModules)
            {
                ShipModuleType type = m.ModuleType;
                if (type == ShipModuleType.Troop    || type == ShipModuleType.Colony      ||
                    type == ShipModuleType.Command  || type == ShipModuleType.Storage     ||
                    type == ShipModuleType.Hangar   || type == ShipModuleType.Sensors     ||
                    type == ShipModuleType.Special  || type == ShipModuleType.Transporter ||
                    type == ShipModuleType.Ordnance || type == ShipModuleType.Construction)
                    AddCategoryItem((int)type, type.ToString(), m);
            }
            OpenCategory((int)ShipModuleType.Command);
            OpenCategory((int)ShipModuleType.Storage);
        }

        static bool RestrictedModCheck(ShipData.RoleName role, ShipModule mod)
        {
            if (mod.FighterModule   || mod.CorvetteModule || mod.FrigateModule || mod.StationModule ||
                mod.DestroyerModule || mod.CruiserModule  || mod.CarrierModule || mod.CapitalModule ||
                mod.FreighterModule || mod.PlatformModule || mod.DroneModule)
            {
                if (role == ShipData.RoleName.drone     && mod.DroneModule     == false) return true;
                if (role == ShipData.RoleName.scout     && mod.FighterModule   == false) return true;
                if (role == ShipData.RoleName.fighter   && mod.FighterModule   == false) return true;
                if (role == ShipData.RoleName.corvette  && mod.CorvetteModule  == false) return true;
                if (role == ShipData.RoleName.gunboat   && mod.CorvetteModule  == false) return true;
                if (role == ShipData.RoleName.frigate   && mod.FrigateModule   == false) return true;
                if (role == ShipData.RoleName.destroyer && mod.DestroyerModule == false) return true;
                if (role == ShipData.RoleName.cruiser   && mod.CruiserModule   == false) return true;
                if (role == ShipData.RoleName.carrier   && mod.CarrierModule   == false) return true;
                if (role == ShipData.RoleName.capital   && mod.CapitalModule   == false) return true;
                if (role == ShipData.RoleName.freighter && mod.FreighterModule == false) return true;
                if (role == ShipData.RoleName.platform  && mod.PlatformModule  == false) return true;
                if (role == ShipData.RoleName.station   && mod.StationModule   == false) return true;
            }
            else if (mod.FightersOnly)
            {
                if (role == ShipData.RoleName.fighter)  return true;
                if (role == ShipData.RoleName.scout)    return true;
                if (role == ShipData.RoleName.corvette) return true;
                if (role == ShipData.RoleName.gunboat)  return true;
            }
            return false;
        }
    }
}