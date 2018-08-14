using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class WeaponScrollList : ScrollList
    {
        private readonly ShipDesignScreen Screen;
        private Selector SelectionBox;
        private readonly InputState Input;
        public bool ResetOnNextDraw = true;

        public WeaponScrollList(Submenu weaponList, ShipDesignScreen shipDesignScreen) : base(weaponList)
        {
            Screen = shipDesignScreen;
            Input = Screen.Input;
        }

        private void DestroySelectionBox()
        {
            SelectionBox?.RemoveFromParent();
            SelectionBox = null;
        }
        public override bool HandleInput(InputState input)
        {
            base.HandleInput(input);

            if (!Screen.ModSel.Menu.HitTest(input.CursorPosition))
            {
                DestroySelectionBox();
                return false;
            }
            foreach (Entry e in VisibleExpandedEntries)
            {
                if (e.item is ModuleHeader moduleHeader)
                {
                    if (moduleHeader.HandleInput(input, e))
                    {
                        DestroySelectionBox();
                        return true;
                    }
                    if (moduleHeader.Hover)
                        DestroySelectionBox();
                }
                else if (e.CheckHover(input))
                {
                    SelectionBox = e.CreateSelector();
                    if (!Screen.Input.InGameSelect)
                        continue;

                    var module = (ShipModule)e.item;
                    Screen.SetActiveModule(module, ModuleOrientation.Normal, 0f);
                    return true;
                }
            }
            return false;
        }

        private bool IsBadModuleSize(ShipModule module)
        {
            if (Input.IsShiftKeyDown || Screen.ActiveHull == null || module.XSIZE + module.YSIZE == 2)
                return false;
            return Screen.IsBadModuleSize(module);
        }

        private bool IsGoodModuleSize(ShipModule module)
        {
            return !IsBadModuleSize(module);
        }

        private readonly Map<int, Entry> Categories = new Map<int, Entry>();

        private void AddCategoryItem(int categoryId, string categoryName, ShipModule mod)
        {
            if (!IsGoodModuleSize(mod))
                return;
            if (!Categories.TryGetValue(categoryId, out Entry e))
            {
                e = AddItem(new ModuleHeader(categoryName, 240));
                Categories.Add(categoryId, e);
            }
            e.AddSubItem(mod);
        }

        private bool OpenCategory(int categoryId)
        {
            if (Categories.TryGetValue(categoryId, out Entry e))
            {
                var category = (ModuleHeader)e.item;
                category.Expand(true, e);
                return true;
            }
            return false;
        }

        private void OpenCategoryByIndex(int index)
        {
            if (index < NumEntries)
            {
                var category = (ModuleHeader)EntryAt(index).item;
                category.Expand(true, EntryAt(index));
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SelectionBox?.Draw(spriteBatch);

            if (ResetOnNextDraw)
            {
                Reset();
                Categories.Clear();
                if      (Screen.ModSel.Tabs[0].Selected) AddWeaponCategories();
                else if (Screen.ModSel.Tabs[1].Selected) AddPowerCategories();
                else if (Screen.ModSel.Tabs[2].Selected) AddDefenseCategories();
                else if (Screen.ModSel.Tabs[3].Selected) AddSpecialCategories();
                ResetOnNextDraw = false;
            }

            DrawList(spriteBatch);
            base.Draw(spriteBatch);
        }

        private bool IsModuleAvailable(ShipModule template, out ShipModule tmp)
        {
            tmp = null;

            if (!EmpireManager.Player.IsModuleUnlocked(template.UID) || template.UID == "Dummy")
                return false;

            if (RestrictedModCheck(Screen.ActiveHull.Role, template))
                return false;

            tmp = Screen.CreateDesignModule(template);
            return true;
        }

        private Array<ShipModule> SortedModules
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

        private void AddWeaponCategories()
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

        private void AddPowerCategories()
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

        private void AddDefenseCategories()
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

        private void AddSpecialCategories()
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

        private void DrawList(SpriteBatch spriteBatch)
        {
            Vector2 mousePos = Input.CursorPosition;
            foreach (Entry e in VisibleExpandedEntries)
            {
                var bCursor = new Vector2(Screen.ModSel.Menu.X + 10, e.Y);
                if (e.item is ModuleHeader header)
                {
                    header.Draw(Screen.ScreenManager, bCursor);
                }
                else if (e.item is ShipModule mod)
                {
                    bCursor.X += 5f;
                    Texture2D modTexture = mod.ModuleTexture;
                    var modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, modTexture.Width, modTexture.Height);
                    float aspectRatio = (float)modTexture.Width / modTexture.Height;
                    float w = modRect.Width;
                    float h;
                    for (h = modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
                    {
                        w -= aspectRatio * 1.6f;
                    }
                    modRect.Width  = (int)w;
                    modRect.Height = (int)h;
                    spriteBatch.Draw(modTexture, modRect, Color.White);

                    var tCursor = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);

                    string moduleName = Localizer.Token(mod.NameIndex);
                    if (Fonts.Arial12Bold.MeasureString(moduleName).X + 90 < Screen.ModSel.Menu.Width)
                    {
                        spriteBatch.DrawString(Fonts.Arial12Bold, moduleName, tCursor, Color.White);
                        tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        spriteBatch.DrawString(Fonts.Arial11Bold, moduleName, tCursor, Color.White);
                        tCursor.Y += Fonts.Arial11Bold.LineSpacing;
                    }

                    string restriction = mod.Restrictions.ToString();
                    spriteBatch.DrawString(Fonts.Arial8Bold, restriction, tCursor, Color.Orange);
                    tCursor.X += Fonts.Arial8Bold.MeasureString(restriction).X;

                    if (mod.IsRotatable)
                    {
                        var rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 20, 22);
                        spriteBatch.Draw(ResourceManager.Texture("UI/icon_can_rotate"), rotateRect, Color.White);
                        if (rotateRect.HitTest(mousePos))
                        {
                            ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys");
                        }
                    }

                    e.CheckHover(mousePos);
                }
            }
        }

        private static bool RestrictedModCheck(ShipData.RoleName role, ShipModule mod)
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