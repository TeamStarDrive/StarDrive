using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.UI;

namespace Ship_Game
{
    public class ModuleSelectScrollList : ScrollList<ModuleSelectListItem>
    {
        public readonly ShipDesignScreen Screen;
        Empire Player => Screen.Player;

        public ModuleSelectScrollList(IClientArea rectSource, ShipDesignScreen screen) : base(rectSource)
        {
            Screen = screen;
            EnableItemHighlight = true;
        }

        public override void OnItemClicked(ScrollListItemBase item)
        {
            var weaponItem = (ModuleSelectListItem)item;
            if (weaponItem.Module != null)
            {
                Screen.SetActiveModule(weaponItem.Module.UID, ModuleOrientation.Normal, 0, DynamicHangarOptions.DynamicLaunch.ToString());
            }
            base.OnItemClicked(item);
        }

        bool CanNeverFitModuleGrid(ShipModule module)
        {
            if (Screen.Input.IsShiftKeyDown || Screen.CurrentDesign == null || module.XSize + module.YSize == 2)
                return false;
            return Screen.CanNeverFitModuleGrid(module);
        }

        bool ShouldBeFiltered(ShipModule m)
        {
            return CanNeverFitModuleGrid(m) 
                || m.IsObsolete(Player) && Screen.IsFilterOldModulesMode
                || m.IsMiningBay && Player.Universe.P.DisableMiningOps
                || m.ResearchPerTurn > 0 && Player.Universe.P.DisableResearchStations;
        }

        readonly Map<int, ModuleSelectListItem> Categories = new Map<int, ModuleSelectListItem>();

        void AddCategoryItem(int categoryId, string categoryName, ShipModule mod)
        {
            if (ShouldBeFiltered(mod))
                return;

            if (!Categories.TryGetValue(categoryId, out ModuleSelectListItem e))
            {
                e = AddItem(new(Player, categoryName));
                Categories.Add(categoryId, e);
            }
            e.AddSubItem(new ModuleSelectListItem(Player, mod));
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

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }

        // Tries to get a ModuleListItem, if the ShipModule is unlocked and available for current hull
        bool TryGetModuleListItem(ShipModule template, out ShipModule m)
        {
            if (Player.IsModuleUnlocked(template.UID))
            {
                if (IsModuleAvailableForHullRole(Screen.CurrentHull.Role, template))
                {
                    m = Screen.CreateModuleListItem(template);
                    return true;
                }
            }
            m = null;
            return false;
        }

        Array<ShipModule> SortedModules
        {
            get
            {
                var modules = new Array<ShipModule>(); // gather into a list so we can sort the modules logically
                foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
                    if (TryGetModuleListItem(template, out ShipModule tmp))
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
                if (m.IsWeapon)
                {
                    Weapon w = m.InstalledWeapon;
                    AddCategoryItem(w.WeaponType.GetHashCode(), w.WeaponType, m);
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

                if (type == ShipModuleType.PowerPlant || type == ShipModuleType.Engine)
                    AddCategoryItem((int)type, type.ToString(), m);
                else if (type == ShipModuleType.FuelCell)
                    AddCategoryItem((int)type, Localizer.Token(GameText.PowerCell), m);
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
                    (type == ShipModuleType.Armor && !m.IsBulkhead && !m.IsPowerArmor))
                {
                    AddCategoryItem((int)type, type.ToString(), m);
                }
                // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType
                // is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                else if (m.IsPowerArmor && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6172, Localizer.Token(GameText.PowerArmour), m);
                }
                else if (m.IsBulkhead && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6173, Localizer.Token(GameText.Bulkhead), m);
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

        static bool IsModuleAvailableForHullRole(RoleName role, ShipModule mod)
        {
            switch (role)
            {
                case RoleName.drone      when mod.DroneModule      == false:
                case RoleName.scout      when mod.FighterModule    == false:
                case RoleName.fighter    when mod.FighterModule    == false:
                case RoleName.corvette   when mod.CorvetteModule   == false:
                case RoleName.gunboat    when mod.CorvetteModule   == false:
                case RoleName.frigate    when mod.FrigateModule    == false:
                case RoleName.destroyer  when mod.DestroyerModule  == false:
                case RoleName.cruiser    when mod.CruiserModule    == false:
                case RoleName.battleship when mod.BattleshipModule == false:
                case RoleName.capital    when mod.CapitalModule    == false:
                case RoleName.freighter  when mod.FreighterModule  == false:
                case RoleName.platform   when mod.PlatformModule   == false:
                case RoleName.station    when mod.StationModule    == false: return false;
            }
            return true;
        }
    }
}
