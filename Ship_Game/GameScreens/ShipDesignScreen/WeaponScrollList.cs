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
            for (int index = indexAtTop;
                index < Copied.Count
                && index < indexAtTop + entriesToDisplay;
                ++index)
            {
                Entry e = Copied[index];

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

        private readonly Map<int, Entry> Categories = new Map<int, Entry>();

        private void AddCategoryItem(int categoryId, string categoryName, ShipModule mod)
        {
            if (!Categories.TryGetValue(categoryId, out Entry e))
            {
                e = AddItem(new ModuleHeader(categoryName, 240f));
                Categories.Add(categoryId, e);
            }
            e.AddItem(mod);
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

            DrawList();
            base.Draw(spriteBatch);
        }

        private bool IsModuleAvailable(ShipModule mod, out ShipModule tmp)
        {
            tmp = null;

            if (!EmpireManager.Player.IsModuleUnlocked(mod.UID) || mod.UID == "Dummy")
                return false;

            if (RestrictedModCheck(Screen.ActiveHull.Role, mod))
                return false;

            tmp = Screen.CreateDesignModule(mod.UID);
            return true;
        }

        private void AddWeaponCategories()
        {
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!IsModuleAvailable(module.Value, out ShipModule tmp))
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
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!IsModuleAvailable(module.Value, out ShipModule tmp))
                    continue;

                if (tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell ||
                    tmp.ModuleType == ShipModuleType.PowerPlant ||
                    tmp.ModuleType == ShipModuleType.PowerConduit)
                {
                    AddCategoryItem((int)tmp.ModuleType, tmp.ModuleType.ToString(), tmp);
                }
            }
        }

        private void AddDefenseCategories()
        {
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!IsModuleAvailable(module.Value, out ShipModule tmp))
                    continue;

                if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield ||
                     tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead && !tmp.isPowerArmour)
                {
                    if (IsBadModuleSize(tmp) == false)
                        AddCategoryItem((int)tmp.ModuleType, tmp.ModuleType.ToString(), tmp);
                }
                // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType
                // is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                else if (tmp.isPowerArmour && tmp.ModuleType == ShipModuleType.Armor)
                {
                    if (IsBadModuleSize(tmp) == false) 
                        AddCategoryItem(6172, Localizer.Token(6172), tmp);
                }
                else if (tmp.isBulkhead && tmp.ModuleType == ShipModuleType.Armor)
                {
                    if (IsBadModuleSize(tmp) == false) 
                        AddCategoryItem(6173, Localizer.Token(6173), tmp);
                }
            }
        }

        private void AddSpecialCategories()
        {
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!IsModuleAvailable(module.Value, out ShipModule tmp))
                    continue;

                if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony ||
                     tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage ||
                     tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors ||
                     tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter ||
                     tmp.ModuleType == ShipModuleType.Ordnance ||
                     tmp.ModuleType == ShipModuleType.Construction))
                {
                    AddCategoryItem((int)tmp.ModuleType, tmp.ModuleType.ToString(), tmp);
                }
            }
        }

        private void DrawList()
        {
            float h;
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = Input.CursorPosition;//  new Vector2(x, (float) state.Y);
            Vector2 bCursor = new Vector2((float)(Screen.ModSel.Menu.X + 10), (float)(Screen.ModSel.Menu.Y + 45));
            for (int i = indexAtTop;
                i < Copied.Count && i < indexAtTop + entriesToDisplay;
                i++)
            {
                bCursor = new Vector2((float)(Screen.ModSel.Menu.X + 10), (float)(Screen.ModSel.Menu.Y + 45));
                ScrollList.Entry e = Copied[i];
                bCursor.Y = (float)e.clickRect.Y;
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).Draw(Screen.ScreenManager, bCursor);
                }
                else if (e.item is ShipModule mod)
                {
                    bCursor.X += 5f;
                    ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);
                    var modTexture = moduleTemplate.ModuleTexture;
                    Rectangle modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, modTexture.Width, modTexture.Height);
                    Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
                    Vector2 vector21 =
                        new Vector2((modTexture.Width / 2f), (modTexture.Height / 2f));
                    float aspectRatio = (float)modTexture.Width / modTexture.Height;
                    float w = modRect.Width;
                    for (h = modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
                    {
                        w = w - aspectRatio * 1.6f;
                    }
                    modRect.Width = (int)w;
                    modRect.Height = (int)h;
                    Screen.ScreenManager.SpriteBatch.Draw(modTexture, modRect, Color.White);
                    //Added by McShooterz: allow longer modules names
                    Vector2 tCursor = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);
                    if (Fonts.Arial12Bold.MeasureString(Localizer.Token((e.item as ShipModule).NameIndex)).X + 90 <
                        Screen.ModSel.Menu.Width)
                    {
                        Screen.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        Screen.ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold,
                            Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float)Fonts.Arial11Bold.LineSpacing;
                    }
                    Screen.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, moduleTemplate.Restrictions.ToString(),
                        tCursor, Color.Orange);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(moduleTemplate.Restrictions.ToString()).X;
                    if (moduleTemplate.IsRotatable)
                    {
                        Rectangle rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 20, 22);
                        Screen.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_can_rotate"],
                            rotateRect, Color.White);
                        if (rotateRect.HitTest(MousePos))
                        {
                            ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys");
                        }
                    }
                    if (e.clickRect.HitTest(MousePos))
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