using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            if (input.ScrollIn && indexAtTop > 0)
                --indexAtTop;
            if (input.ScrollOut && indexAtTop + entriesToDisplay < Entries.Count)
                ++indexAtTop;

            if (!Screen.ModSel.Menu.HitTest(input.CursorPosition))
            {
                DestroySelectionBox();
                return false;
            }
            for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; ++i)
            {
                Entry e = Copied[i];

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
                else if (e.clickRect.HitTest(input.CursorPosition))
                {
                    SelectionBox = new Selector(e.clickRect);
                    e.clickRectHover = 1;
                    if (!Screen.Input.InGameSelect)
                        continue;

                    var module = (ShipModule)e.item;
                    Screen.SetActiveModule(module.UID, ShipDesignScreen.ActiveModuleState.Normal);
                    return true;
                }
                else
                    e.clickRectHover = 0;
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
            e.AddItem(mod);
        }

        private void OpenCategory(int categoryId)
        {
            if (Categories.TryGetValue(categoryId, out Entry e))
            {
                var category = (ModuleHeader)e.item;
                category.Expand(true, e);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SelectionBox?.Draw(spriteBatch);

            if (ResetOnNextDraw)
            {
                Entries.Clear();
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

            tmp = Screen.CreateDesignModule(template.UID);
            return true;
        }

        private void AddWeaponCategories()
        {
            foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
            {
                if (!IsModuleAvailable(template, out ShipModule tmp))
                    continue;

                if (tmp.isWeapon)
                {
                    Weapon w = tmp.InstalledWeapon;
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats)
                    {
                        if      (w.Tag_Flak)    AddCategoryItem(400, "Flak Cannon", tmp);
                        else if (w.Tag_Railgun) AddCategoryItem(401, "Magnetic Cannon", tmp);
                        else if (w.Tag_Array)   AddCategoryItem(401, "Beam Array", tmp);
                        else if (w.Tag_Tractor) AddCategoryItem(401, "Tractor Beam", tmp);
                        else if (w.Tag_Missile
                             && !w.Tag_Guided)  AddCategoryItem(401, "Unguided Rocket", tmp);
                        else                    AddCategoryItem(w.WeaponType.GetHashCode(), w.WeaponType, tmp);
                    }
                    else
                    {
                        AddCategoryItem(w.WeaponType.GetHashCode(), w.WeaponType, tmp);
                    }
                }
                else if (tmp.ModuleType == ShipModuleType.Bomb)
                {
                    AddCategoryItem("Bomb".GetHashCode(), "Bomb", tmp);
                }
            }
        }

        private void AddPowerCategories()
        {
            foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
            {
                if (!IsModuleAvailable(template, out ShipModule tmp))
                    continue;

                ShipModuleType type = tmp.ModuleType;
                if (type == ShipModuleType.PowerConduit)
                    type = ShipModuleType.PowerPlant; // force PowerConduit's into PowerPlant category

                if (type == ShipModuleType.PowerPlant|| type == ShipModuleType.FuelCell || type == ShipModuleType.Engine)
                    AddCategoryItem((int)type, type.ToString(), tmp);
            }
            OpenCategory((int)ShipModuleType.PowerPlant);
        }

        private void AddDefenseCategories()
        {
            foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
            {
                if (!IsModuleAvailable(template, out ShipModule tmp))
                    continue;
                
                ShipModuleType type = tmp.ModuleType;
                if (type == ShipModuleType.Shield ||
                    type == ShipModuleType.Countermeasure ||
                    (type == ShipModuleType.Armor && !tmp.isBulkhead && !tmp.isPowerArmour))
                {
                    AddCategoryItem((int)type, type.ToString(), tmp);
                }
                // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType
                // is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                else if (tmp.isPowerArmour && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6172, Localizer.Token(6172), tmp);
                }
                else if (tmp.isBulkhead && type == ShipModuleType.Armor)
                {
                    AddCategoryItem(6173, Localizer.Token(6173), tmp);
                }
            }
            OpenCategory((int)ShipModuleType.Shield);
        }

        private void AddSpecialCategories()
        {
            foreach (ShipModule template in ResourceManager.ShipModuleTemplates)
            {
                if (!IsModuleAvailable(template, out ShipModule tmp))
                    continue;

                ShipModuleType type = tmp.ModuleType;
                if (type == ShipModuleType.Troop    || type == ShipModuleType.Colony      ||
                    type == ShipModuleType.Command  || type == ShipModuleType.Storage     ||
                    type == ShipModuleType.Hangar   || type == ShipModuleType.Sensors     ||
                    type == ShipModuleType.Special  || type == ShipModuleType.Transporter ||
                    type == ShipModuleType.Ordnance || type == ShipModuleType.Construction)
                {
                    AddCategoryItem((int)type, type.ToString(), tmp);
                }
            }
            OpenCategory((int)ShipModuleType.Command);
        }

        private void DrawList(SpriteBatch spriteBatch)
        {
            Vector2 mousePos = Input.CursorPosition;
            for (int i = indexAtTop; i < Copied.Count && i < indexAtTop + entriesToDisplay; i++)
            {
                var bCursor = new Vector2(Screen.ModSel.Menu.X + 10, Screen.ModSel.Menu.Y + 45);
                Entry e = Copied[i];
                bCursor.Y = e.clickRect.Y;
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

                    if (e.clickRect.HitTest(mousePos))
                    {
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
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