using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

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
                    if (!Screen.Input.InGameSelect) continue;
                    Screen.SetActiveModule(ShipModule.CreateNoParent(((ShipModule)e.item).UID));
                    Screen.ResetModuleState();
                    return true;
                }
                else
                    e.clickRectHover = 0;
            }



            return false;

        }

        private void DrawTab1()
        {
            if (ResetOnNextDraw)
            {
                Entries.Clear();
                var weaponCategories = new HashSet<string>();

                Action<string> addCategoryItem = (category) =>
                {
                    if (weaponCategories.Contains(category)) return;                    
                    weaponCategories.Add(category);                    
                    AddItem(new ModuleHeader(category, 240f));
                };

                Array<ShipModule> modules = new Array<ShipModule>();
                foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                {
                    if (!EmpireManager.Player.IsModuleUnlocked(module.Key) || module.Value.UID == "Dummy")
                        continue;

                    ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                    tmp.SetAttributesNoParent();                    
       
                    if (RestrictedModCheck(Screen.ActiveHull.Role, tmp))
                        continue;

                    if (tmp.isWeapon)
                    {
                        if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats)
                        {
                            if      (tmp.InstalledWeapon.Tag_Flak)    addCategoryItem("Flak Cannon");
                            else if (tmp.InstalledWeapon.Tag_Railgun) addCategoryItem("Magnetic Cannon");
                            else if (tmp.InstalledWeapon.Tag_Array)   addCategoryItem("Beam Array");
                            else if (tmp.InstalledWeapon.Tag_Tractor) addCategoryItem("Tractor Beam");
                            else if (tmp.InstalledWeapon.Tag_Missile
                                 && !tmp.InstalledWeapon.Tag_Guided)  addCategoryItem("Unguided Rocket");
                            else                                      addCategoryItem(tmp.InstalledWeapon.WeaponType);
                        }
                        else
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(module.Value);
                            addCategoryItem(tmp.InstalledWeapon.WeaponType);
                        }
                    }
                    else if (tmp.ModuleType == ShipModuleType.Bomb)
                    {
                        if (CheckBadModuleSize(tmp)) continue;
                        modules.Add(module.Value);
                        addCategoryItem("Bomb");
                    }
                }

                foreach (Entry e in Entries)
                {
                    foreach (ShipModule module in modules)
                    {            
                        ShipModule tmp = module;
                        tmp.SetAttributesNoParent();
                        bool restricted =
                            tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule ||
                            tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule ||
                            tmp.PlatformModule || tmp.DroneModule;
                        if (!restricted && tmp.FightersOnly &&
                                 Screen.ActiveHull.Role != ShipData.RoleName.fighter &&
                                 Screen.ActiveHull.Role != ShipData.RoleName.scout &&
                                 Screen.ActiveHull.Role != ShipData.RoleName.corvette &&
                                 Screen.ActiveHull.Role != ShipData.RoleName.gunboat)
                            continue;
                        
                        if (tmp.isWeapon)
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats)
                            {
                                if (tmp.InstalledWeapon.Tag_Flak || tmp.InstalledWeapon.Tag_Array ||
                                    tmp.InstalledWeapon.Tag_Railgun || tmp.InstalledWeapon.Tag_Tractor ||
                                    (tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided))
                                {
                                    if ((e.item as ModuleHeader).Text == "Flak Cannon" && tmp.InstalledWeapon.Tag_Flak)
                                        e.AddItem(module);
                                    if ((e.item as ModuleHeader).Text == "Magnetic Cannon" &&
                                        tmp.InstalledWeapon.Tag_Railgun)
                                        e.AddItem(module);
                                    if ((e.item as ModuleHeader).Text == "Beam Array" &&
                                        tmp.InstalledWeapon.Tag_Array)
                                        e.AddItem(module);
                                    if ((e.item as ModuleHeader).Text == "Tractor Beam" &&
                                        tmp.InstalledWeapon.Tag_Tractor)
                                        e.AddItem(module);
                                    if ((e.item as ModuleHeader).Text == "Unguided Rocket" &&
                                        tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided)
                                        e.AddItem(module);
                                }
                                else if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                {
                                    e.AddItem(module);
                                }
                            }
                            else
                            {
                                if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                {
                                    e.AddItem(module);
                                }
                            }
                        }
                        else if (tmp.ModuleType == ShipModuleType.Bomb && (e.item as ModuleHeader).Text == "Bomb")
                        {
                            e.AddItem(module);
                        }
                    }
                }
                ResetOnNextDraw = false;
            }
            DrawList();
        }
        private bool CheckBadModuleSize(ShipModule module)
        {
            if (Input.IsShiftKeyDown || Screen.ActiveHull == null || module.XSIZE + module.YSIZE == 2) return false;

            bool doesntFit = false;          
            foreach (SlotStruct s in Screen.Slots)
                s.SetValidity(module);
            foreach (SlotStruct slot in Screen.Slots)
            {
                if (Screen.SlotStructFits(slot, module))
                {
                    doesntFit = false;
                    break;
                }
              
                if (module.YSIZE != module.XSIZE)
                    if (Screen.SlotStructFits(slot, module , rotated: true))
                    {
                        doesntFit = false;                        
                        break;
                    }
                doesntFit = true;            
            }

            foreach (SlotStruct s in Screen.Slots)
                s.SetValidity();



            return doesntFit;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            SelectionBox?.Draw(spriteBatch);

            if (Screen.ModSel.Tabs[0].Selected)
            {
                DrawTab1();

            }
            Array<ShipModule> modules = new Array<ShipModule>();
            if (Screen.ModSel.Tabs[2].Selected)
            {
                if (ResetOnNextDraw)
                {
                    Entries.Clear();
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.IsModuleUnlocked(module.Key) || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;
                        
                        if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield ||
                             tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead &&
                            !tmp.isPowerArmour)
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(tmp);
                            if (!ModuleCategories.Contains(tmp.ModuleType.ToString()))
                            {
                                ModuleCategories.Add(tmp.ModuleType.ToString());
                                ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                                AddItem(type);
                            }
                        }

                        // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                        if (tmp.isPowerArmour && tmp.ModuleType == ShipModuleType.Armor )
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(tmp);
                            if (!ModuleCategories.Contains(Localizer.Token(6172)))
                            {
                                ModuleCategories.Add(Localizer.Token(6172));
                                ModuleHeader type = new ModuleHeader(Localizer.Token(6172), 240f);
                                AddItem(type);
                            }
                        }
                        if (tmp.isBulkhead && tmp.ModuleType == ShipModuleType.Armor)
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(tmp);
                            if (!ModuleCategories.Contains(Localizer.Token(6173)))
                            {
                                ModuleCategories.Add(Localizer.Token(6173));
                                ModuleHeader type = new ModuleHeader(Localizer.Token(6173), 240f);
                                AddItem(type);
                            }
                        }
                    }
                    foreach (Entry e in Entries)
                    {
                        foreach (var module in modules)
                        {
                            ShipModule tmp = module;


                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;

                            if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield ||
                                 tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead &&
                                !tmp.isPowerArmour && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
                            {
                                e.AddItem(module);
                            }
                            if (tmp.isPowerArmour && (e.item as ModuleHeader).Text == Localizer.Token(6172))
                            {
                                e.AddItem(module);
                            }
                            if (tmp.isBulkhead && (e.item as ModuleHeader).Text == Localizer.Token(6173))
                            {
                                e.AddItem(module);
                            }
                            tmp = null;
                        }
                    }
                    ResetOnNextDraw = false;
                }
                DrawList();
            }
            if (Screen.ModSel.Tabs[1].Selected)
            {
                if (ResetOnNextDraw)
                {
                    Entries.Clear();
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.IsModuleUnlocked(module.Key) || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();
                        if (RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;
                        
                        if (tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell ||
                             tmp.ModuleType == ShipModuleType.PowerPlant ||
                             tmp.ModuleType == ShipModuleType.PowerConduit)
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(tmp);
                            if (!ModuleCategories.Contains(tmp.ModuleType.ToString()))
                            {

                                ModuleCategories.Add(tmp.ModuleType.ToString());
                                ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                                AddItem(type);
                            }
                        }
                    }
                    foreach (Entry e in Entries)
                    {
                        string cat = (e.item as ModuleHeader).Text;
                        foreach (ShipModule module in modules)
                        {                            
                            if (cat == module.ModuleType.ToString())
                            {
                                e.AddItem(module);
                            }
                        }
                    }
                    ResetOnNextDraw = false;
                }
                DrawList();
            }
            if (Screen.ModSel.Tabs[3].Selected)
            {
                if (ResetOnNextDraw)
                {
                    Entries.Clear();
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.IsModuleUnlocked(module.Key) || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();
                        if (RestrictedModCheck(Screen.ActiveHull.Role, tmp)) continue;                        
                        if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony ||
                             tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage ||
                             tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors ||
                             tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter ||
                             tmp.ModuleType == ShipModuleType.Ordnance ||
                             tmp.ModuleType == ShipModuleType.Construction) )
                        {
                            if (CheckBadModuleSize(tmp)) continue;
                            modules.Add(tmp);

                            if (ModuleCategories.Contains(tmp.ModuleType.ToString())) continue;
                                
                            ModuleCategories.Add(tmp.ModuleType.ToString());
                            ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                            AddItem(type);
                        }
                    }
                    foreach (Entry e in Entries)
                    {
                        string cat = (e.item as ModuleHeader).Text;
                        foreach (ShipModule module in modules)
                        {
                            if (cat == module.ModuleType.ToString())
                            {
                                e.AddItem(module);
                            }
                        }
                    }
                    ResetOnNextDraw = false;
                }
                DrawList();
            }
            base.Draw(spriteBatch);
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
                    Rectangle modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y,
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width,
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height);
                    Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
                    Vector2 vector21 =
                        new Vector2(
                            (float)(ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width / 2),
                            (float)(ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height / 2));
                    float aspectRatio =
                        (float)ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width /
                        (float)ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height;
                    float w = (float)modRect.Width;
                    for (h = (float)modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
                    {
                        w = w - aspectRatio * 1.6f;
                    }
                    modRect.Width = (int)w;
                    modRect.Height = (int)h;
                    Screen.ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath], modRect, Color.White);
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
                    if (moduleTemplate.InstalledWeapon != null && moduleTemplate.ModuleType != ShipModuleType.Turret ||
                        moduleTemplate.XSIZE != moduleTemplate.YSIZE)
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
            if (mod.FighterModule || mod.CorvetteModule || mod.FrigateModule || mod.StationModule ||
                mod.DestroyerModule || mod.CruiserModule
                || mod.CarrierModule || mod.CapitalModule || mod.FreighterModule || mod.PlatformModule ||
                mod.DroneModule)
            {
                if (role == ShipData.RoleName.drone && mod.DroneModule == false) return true;
                if (role == ShipData.RoleName.scout && mod.FighterModule == false) return true;
                if (role == ShipData.RoleName.fighter && mod.FighterModule == false) return true;
                if (role == ShipData.RoleName.corvette && mod.CorvetteModule == false) return true;
                if (role == ShipData.RoleName.gunboat && mod.CorvetteModule == false) return true;
                if (role == ShipData.RoleName.frigate && mod.FrigateModule == false) return true;
                if (role == ShipData.RoleName.destroyer && mod.DestroyerModule == false) return true;
                if (role == ShipData.RoleName.cruiser && mod.CruiserModule == false) return true;
                if (role == ShipData.RoleName.carrier && mod.CarrierModule == false) return true;
                if (role == ShipData.RoleName.capital && mod.CapitalModule == false) return true;
                if (role == ShipData.RoleName.freighter && mod.FreighterModule == false) return true;
                if (role == ShipData.RoleName.platform && mod.PlatformModule == false) return true;
                if (role == ShipData.RoleName.station && mod.StationModule == false) return true;
            }
            else if (mod.FightersOnly)
            {
                if (role == ShipData.RoleName.fighter) return true;
                if (role == ShipData.RoleName.scout) return true;
                if (role == ShipData.RoleName.corvette) return true;
                if (role == ShipData.RoleName.gunboat) return true;
            }

            return false;
        }
    }
}