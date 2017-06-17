using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class ModuleSelection : Submenu
    {
        private WeaponScrollList WeaponSl;
        private readonly ShipDesignScreen ParentScreen;
        public Rectangle Window;
        private Submenu ActiveModSubMenu;
        private readonly ScreenManager ScreenManager;
        private Submenu ChooseFighterSub;
        private ScrollList ChooseFighterSL;
        public Rectangle Choosefighterrect;
        public void ResetLists() => WeaponSl.Reset = true;
        public ModuleSelection(ShipDesignScreen parentScreen, Rectangle window) : base(parentScreen.ScreenManager, window, true)
        {
            ParentScreen = parentScreen;
            Window = window;
            ScreenManager = parentScreen.ScreenManager;
            LoadContent();
        }
        public void LoadContent()
        {
            AddTab("Wpn");
            AddTab("Pwr");
            AddTab("Def");
            AddTab("Spc");
            WeaponSl = new WeaponScrollList(this, ParentScreen);
            var active = new Rectangle(Window.X, Window.Y + Window.Height + 15, Window.Width, 300);
            //activeModWindow = new Menu1(ScreenManager, active);
            var acsub = new Rectangle(active.X, Window.Y + Window.Height + 15, 305, 320);
            if (ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 760)
            {
                acsub.Height = acsub.Height + 120;
            }
            ActiveModSubMenu = new Submenu(ScreenManager, acsub);
            ActiveModSubMenu.AddTab("Active Module");
            Choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y - 90, 240, 270);
            if (Choosefighterrect.Y + Choosefighterrect.Height >
                ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                int diff = Choosefighterrect.Y + Choosefighterrect.Height - ScreenManager.GraphicsDevice
                               .PresentationParameters.BackBufferHeight;
                Choosefighterrect.Height = Choosefighterrect.Height - (diff + 10);
            }
            Choosefighterrect.Height = acsub.Height;
            ChooseFighterSub = new Submenu(ScreenManager, Choosefighterrect);
            ChooseFighterSub.AddTab("Choose Fighter");
            ChooseFighterSL = new ScrollList(ChooseFighterSub, 40);
        }
        public bool HandleInput(InputState input)
        {
            if (WeaponSl.HandleInput(input))
                return true;

            ChooseFighterSL.HandleInput(input);
            ActiveModSubMenu.HandleInputNoReset();
            if (!base.HandleInput(input)) return false;
            WeaponSl.Reset = true;
            WeaponSl.indexAtTop = 0;
            return false;
            //base.HandleInput(ParentScreen);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            WeaponSl.Draw(spriteBatch);
            if (ParentScreen.ActiveModule != null)
            {
                //activeModWindow.Draw();
                ActiveModSubMenu.Draw();
                DrawActiveModuleData();
            }
            ChooseFighterSL.Draw(spriteBatch);
            base.Draw();
        }

        private string ParseText(string text, float width, SpriteFont font)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(' ');
            for (int i = 0; i < strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (font.MeasureString(string.Concat(line, word)).Length() > width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }
        private void DrawString(ref Vector2 cursorPos, string text, SpriteFont font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            ScreenManager.SpriteBatch.DrawString(font, text, cursorPos, Color.SpringGreen);
            cursorPos.X = cursorPos.X + Fonts.Arial8Bold.MeasureString(text).X;
        }
        private void DrawActiveModuleData()
        {
            float powerDraw;
            //ActiveModSubMenu.Draw();
            Rectangle r = ActiveModSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
            sel.Draw();
            ShipModule mod = ParentScreen.ActiveModule;

            if (ParentScreen.ActiveModule == null && ParentScreen.HighlightedModule != null)
            {
                mod = ParentScreen.HighlightedModule;
            }
            else if (ParentScreen.ActiveModule != null)
            {
                mod = ParentScreen.ActiveModule;
            }

            if (mod != null)
            {
                mod.HealthMax = ResourceManager.GetModuleTemplate(mod.UID).HealthMax;
            }
            if (!ActiveModSubMenu.Tabs[0].Selected || mod == null)
                return;

            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);

            //Added by McShooterz: Changed how modules names are displayed for allowing longer names
            Vector2 modTitlePos = new Vector2((float)(ActiveModSubMenu.Menu.X + 10),
                (float)(ActiveModSubMenu.Menu.Y + 35));
            if (Fonts.Arial20Bold.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X + 16 <
                ActiveModSubMenu.Menu.Width)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 6);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial14Bold.LineSpacing + 4);
            }
            string rest = "";
            if (moduleTemplate.Restrictions == Restrictions.IO)
            {
                rest = "Any Slot except E";
            }
            else if (moduleTemplate.Restrictions == Restrictions.I)
            {
                rest = "I, IO, IE or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.O)
            {
                rest = "O, IO, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.E)
            {
                rest = "E, IE, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IOE)
            {
                rest = "Any Slot";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IE)
            {
                rest = "Any Slot except O";
            }
            else if (moduleTemplate.Restrictions == Restrictions.OE)
            {
                rest = "Any Slot except I";
            }

            // Concat ship class restrictions
            string shipRest = "";
            bool specialString = false;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones &&
                GlobalStats.ActiveModInfo.useDestroyers)
            {
                if (!mod.FightersOnly && mod.DroneModule && mod.FighterModule && mod.CorvetteModule &&
                    mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule &&
                    mod.CarrierModule && mod.CarrierModule && mod.PlatformModule && mod.StationModule &&
                    mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.DroneModule && mod.FighterModule && mod.CorvetteModule &&
                         mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule &&
                         mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule &&
                         mod.FreighterModule)
                {
                    shipRest = "All Crewed";
                    specialString = true;
                }
                else if (mod.FighterModule && !mod.DroneModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule &&
                         !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Fighters Only";
                    specialString = true;
                }
                else if (mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule &&
                         !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Drones Only";
                    specialString = true;
                }
                else if (mod.FightersOnly && !specialString)
                {
                    shipRest = "Fighters/Corvettes Only";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule &&
                         !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule &&
                         !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule &&
                         !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }
            if (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.useDrones &&
                GlobalStats.ActiveModInfo.useDestroyers)
            {
                if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule &&
                    mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule &&
                    mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
                else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule &&
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Fighters Only";
                    specialString = true;
                }
                else if (mod.FightersOnly && !specialString)
                {
                    shipRest = "Fighters/Corvettes Only";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule &&
                         !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones &&
                !GlobalStats.ActiveModInfo.useDestroyers)
            {
                if (!mod.FightersOnly && mod.DroneModule && mod.FighterModule && mod.CorvetteModule &&
                    mod.FrigateModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule &&
                    mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.DroneModule && mod.FighterModule && mod.CorvetteModule &&
                         mod.FrigateModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule &&
                         mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Crewed";
                    specialString = true;
                }
                else if (mod.FighterModule && !mod.DroneModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Fighters Only";
                    specialString = true;
                }
                else if (mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Drones Only";
                    specialString = true;
                }
                else if (mod.FightersOnly && !specialString)
                {
                    shipRest = "Fighters/Corvettes Only";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule &&
                         !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule &&
                         !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }
            if (GlobalStats.ActiveModInfo == null || (!GlobalStats.ActiveModInfo.useDrones &&
                                                      !GlobalStats.ActiveModInfo.useDestroyers))
            {
                if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule &&
                    mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule &&
                    mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
                else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule &&
                         !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule &&
                         !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Fighters Only";
                    specialString = true;
                }
                else if (mod.FightersOnly && !specialString)
                {
                    shipRest = "Fighters/Corvettes Only";
                    specialString = true;
                }
                else if (!mod.FightersOnly && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }

            else if (!specialString && (!mod.DroneModule && GlobalStats.ActiveModInfo != null &&
                                        GlobalStats.ActiveModInfo.useDrones) || !mod.FighterModule ||
                     !mod.CorvetteModule || !mod.FrigateModule ||
                     (!mod.DestroyerModule && GlobalStats.ActiveModInfo != null &&
                      GlobalStats.ActiveModInfo.useDestroyers) || !mod.CruiserModule || !mod.CruiserModule ||
                     !mod.CarrierModule || !mod.CapitalModule || !mod.PlatformModule || !mod.StationModule ||
                     !mod.FreighterModule)
            {
                if (mod.DroneModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                    shipRest += "Dr ";
                if (mod.FighterModule)
                    shipRest += "F ";
                if (mod.CorvetteModule)
                    shipRest += "CO ";
                if (mod.FrigateModule)
                    shipRest += "FF ";
                if (mod.DestroyerModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDestroyers)
                    shipRest += "DD ";
                if (mod.CruiserModule)
                    shipRest += "CC ";
                if (mod.CarrierModule)
                    shipRest += "CV ";
                if (mod.CapitalModule)
                    shipRest += "CA ";
                if (mod.FreighterModule)
                    shipRest += "Frt ";
                if (mod.PlatformModule || mod.StationModule)
                    shipRest += "Stat ";
            }

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(Localizer.Token(122), ": ", rest),
                modTitlePos, Color.Orange);
            modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Hulls: ", shipRest), modTitlePos,
                Color.LightSteelBlue);
            modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing + 11);
            int startx = (int)modTitlePos.X;
            if (moduleTemplate.IsWeapon && moduleTemplate.BombType == null)
            {
                var weaponTemplate = ResourceManager.GetWeaponTemplate(moduleTemplate.WeaponType);

                var sb = new StringBuilder();
                if (weaponTemplate.Tag_Guided) sb.Append("GUIDED ");
                if (weaponTemplate.Tag_Intercept) sb.Append("INTERCEPTABLE ");
                if (weaponTemplate.Tag_Energy) sb.Append("ENERGY ");
                if (weaponTemplate.Tag_Hybrid) sb.Append("HYBRID ");
                if (weaponTemplate.Tag_Kinetic) sb.Append("KINETIC ");
                if (weaponTemplate.Tag_Explosive && !weaponTemplate.Tag_Flak) sb.Append("EXPLOSIVE ");
                if (weaponTemplate.Tag_Subspace) sb.Append("SUBSPACE ");
                if (weaponTemplate.Tag_Warp) sb.Append("WARP ");
                if (weaponTemplate.Tag_PD) sb.Append("POINT DEFENSE ");
                if (weaponTemplate.Tag_Flak) sb.Append("FLAK ");

                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats &&
                    (weaponTemplate.Tag_Missile && !weaponTemplate.Tag_Guided))
                    sb.Append("ROCKET ");
                else if (weaponTemplate.Tag_Missile)
                    sb.Append("MISSILE ");

                if (weaponTemplate.Tag_Tractor) sb.Append("TRACTOR ");
                if (weaponTemplate.Tag_Beam) sb.Append("BEAM ");
                if (weaponTemplate.Tag_Array) sb.Append("ARRAY ");
                if (weaponTemplate.Tag_Railgun) sb.Append("RAILGUN ");
                if (weaponTemplate.Tag_Torpedo) sb.Append("TORPEDO ");
                if (weaponTemplate.Tag_Bomb) sb.Append("BOMB ");
                if (weaponTemplate.Tag_BioWeapon) sb.Append("BIOWEAPON ");
                if (weaponTemplate.Tag_SpaceBomb) sb.Append("SPACEBOMB ");
                if (weaponTemplate.Tag_Drone) sb.Append("DRONE ");
                if (weaponTemplate.Tag_Cannon) sb.Append("CANNON ");
                DrawString(ref modTitlePos, sb.ToString(), Fonts.Arial8Bold);

                modTitlePos.Y = modTitlePos.Y + (Fonts.Arial8Bold.LineSpacing + 5);
                modTitlePos.X = startx;
            }

            string txt = ParseText(Localizer.Token(moduleTemplate.DescriptionIndex),
                (float)(ActiveModSubMenu.Menu.Width - 20), Fonts.Arial12);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
            modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
            float starty = modTitlePos.Y;
            float strength = mod.CalculateModuleOffenseDefense(ParentScreen.ActiveHull.ModuleSlots.Length);
            if (strength > 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, "Offense", (float)strength, 227);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
            }
            if (!mod.isWeapon || mod.InstalledWeapon == null)
            {
                if (mod.Cost != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(128),
                        (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.Mass != 0)
                {
                    float MassMod = (float)EmpireManager.Player.data.MassModifier;
                    float ArmourMassMod = (float)EmpireManager.Player.data.ArmourMassModifier;

                    if (mod.ModuleType == ShipModuleType.Armor)
                    {
                        ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(123), (ArmourMassMod * mod.Mass) * MassMod, 79);
                    }
                    else
                    {
                        ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(123), MassMod * mod.Mass, 79);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HealthMax != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(124),
                        (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.Player.data.Traits.ModHpModifier,
                        80);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ModuleType != ShipModuleType.PowerPlant)
                {
                    powerDraw = -(float)mod.PowerDraw;
                }
                else
                {
                    powerDraw = (mod.PowerDraw > 0f
                        ? (float)(-mod.PowerDraw)
                        : mod.PowerFlowMax + mod.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod);
                }
                if (powerDraw != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(125), powerDraw, 81);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.MechanicalBoardingDefense != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2231), (float)mod.MechanicalBoardingDefense, 143);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BonusRepairRate != 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"),
                        (float)(
                            (mod.BonusRepairRate + mod.BonusRepairRate * EmpireManager.Player.data.Traits.RepairMod) *
                            (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                             ResourceManager.HullBonuses.ContainsKey(ParentScreen.ActiveHull.Hull)
                                ? 1f + ResourceManager.HullBonuses[ParentScreen.ActiveHull.Hull].RepairBonus
                                : 1)), 97);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                //Shift to next Column
                float MaxDepth = modTitlePos.Y;
                modTitlePos.X = modTitlePos.X + 152f;
                modTitlePos.Y = starty;
                if (mod.thrust != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(131), (float)mod.thrust, 91);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.WarpThrust != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2064), (float)mod.WarpThrust, 92);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TurnThrust != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2260), (float)mod.TurnThrust, 148);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_power_max != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(132),
                        mod.shield_power_max *
                        (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                         ResourceManager.HullBonuses.ContainsKey(ParentScreen.ActiveHull.Hull)
                            ? 1f + ResourceManager.HullBonuses[ParentScreen.ActiveHull.Hull].ShieldBonus
                            : 1f) + EmpireManager.Player.data.ShieldPowerMod * mod.shield_power_max, 93);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_radius != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(133), (float)mod.shield_radius, 94);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_recharge_rate != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(134), (float)mod.shield_recharge_rate, 95);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }

                // Doc: new shield resistances, UI info.

                if (mod.shield_kinetic_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6162), (float)mod.shield_kinetic_resist, 209
                        , Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_energy_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6163), (float)mod.shield_energy_resist, 210,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_explosive_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6164), (float)mod.shield_explosive_resist, 211,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_missile_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6165), (float)mod.shield_missile_resist, 212,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_flak_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6166), (float)mod.shield_flak_resist, 213,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_hybrid_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6167), (float)mod.shield_hybrid_resist, 214,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_railgun_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6168), (float)mod.shield_railgun_resist, 215,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_subspace_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6169), (float)mod.shield_subspace_resist, 216,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_warp_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6170), (float)mod.shield_warp_resist, 217,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_beam_resist != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6171), (float)mod.shield_beam_resist, 218,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_threshold != 0)
                {
                    ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6176), (float)mod.shield_threshold, 222,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.SensorRange != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.SensorRange, 96);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SensorBonus != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6121), (float)mod.SensorBonus, 167);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HealPerTurn != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6131), mod.HealPerTurn, 174);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterRange != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.TransporterRange, 168);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterPower != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6123), (float)mod.TransporterPower, 169);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTimerConstant != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6122), (float)mod.TransporterTimerConstant, 170);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterOrdnance != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6124), (float)mod.TransporterOrdnance, 171);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTroopAssault != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6135), (float)mod.TransporterTroopAssault, 187);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTroopLanding != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6128), (float)mod.TransporterTroopLanding, 172);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdinanceCapacity != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2129), (float)mod.OrdinanceCapacity, 124);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.Cargo_Capacity != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(119), (float)mod.Cargo_Capacity, 109);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdnanceAddedPerSecond != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6120), (float)mod.OrdnanceAddedPerSecond, 162);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InhibitionRadius != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2233), (float)mod.InhibitionRadius, 144);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TroopCapacity != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(336), (float)mod.TroopCapacity, 173);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.PowerStoreMax != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2235),
                        (float)(mod.PowerStoreMax + mod.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier),
                        145);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                //added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
                if (mod.PowerDrawAtWarp != 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6011), (float)(-mod.PowerDrawAtWarp), 178);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM && mod.ECM != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6004), (float)mod.ECM, 154, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ModuleType == ShipModuleType.Hangar && mod.hangarTimerConstant != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(136), (float)mod.hangarTimerConstant, 98);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.explodes)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Explodes", modTitlePos,
                        Color.OrangeRed);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.KineticResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6142), (float)mod.KineticResist, 189,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.EnergyResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6143), (float)mod.EnergyResist, 190,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.GuidedResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6144), (float)mod.GuidedResist, 191,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.MissileResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6145), (float)mod.MissileResist, 192,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HybridResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6146), (float)mod.HybridResist, 193,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BeamResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6147), (float)mod.BeamResist, 194, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ExplosiveResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6148), (float)mod.ExplosiveResist, 195,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InterceptResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6149), (float)mod.InterceptResist, 196,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.RailgunResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6150), (float)mod.RailgunResist, 197,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SpaceBombResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6151), (float)mod.SpaceBombResist, 198,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BombResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6152), (float)mod.BombResist, 199, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BioWeaponResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6153), (float)mod.BioWeaponResist, 200,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.DroneResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6154), (float)mod.DroneResist, 201,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.WarpResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6155), (float)mod.WarpResist, 202, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TorpedoResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6156), (float)mod.TorpedoResist, 203,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.CannonResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6157), (float)mod.CannonResist, 204,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SubspaceResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6158), (float)mod.SubspaceResist, 205,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.PDResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6159), (float)mod.PDResist, 206, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.FlakResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6160), (float)mod.FlakResist, 207, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.APResist != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6161), (float)mod.APResist, 208, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.DamageThreshold != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6175), (float)mod.DamageThreshold, 221);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.EMP_Protection != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6174), (float)mod.EMP_Protection, 219);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.FixedTracking > 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6187), (float)mod.FixedTracking, 231);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TargetTracking > 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "+" + Localizer.Token(6186), (float)mod.TargetTracking, 226);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.PermittedHangarRoles.Length > 0)
                {
                    modTitlePos.Y = Math.Max(modTitlePos.Y, MaxDepth) + (float)Fonts.Arial12Bold.LineSpacing;
                    Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                        string.Concat(Localizer.Token(137), " : ", mod.hangarShipUID), shipSelectionPos, Color.Orange);
                    r = ChooseFighterSub.Menu;
                    r.Y = r.Y + 25;
                    r.Height = r.Height - 25;
                    sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
                    sel.Draw();
                    ParentScreen.UpdateHangarOptions(mod);
                    ChooseFighterSub.Draw();
                    ChooseFighterSL.Draw(ScreenManager.SpriteBatch);
                    Vector2 bCursor = new Vector2((float)(ChooseFighterSub.Menu.X + 15),
                        (float)(ChooseFighterSub.Menu.Y + 25));
                    for (int i = ChooseFighterSL.indexAtTop;
                        i < ChooseFighterSL.Entries.Count && i < ChooseFighterSL.indexAtTop +
                        ChooseFighterSL.entriesToDisplay;
                        i++)
                    {
                        ScrollList.Entry e = ChooseFighterSL.Entries[i];
                        bCursor.Y = (float)e.clickRect.Y;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[
                                ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath],
                            new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            (!string.IsNullOrEmpty((e.item as Ship).VanityName)
                                ? (e.item as Ship).VanityName
                                : (e.item as Ship).Name), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    //if (ParentScreen.selector != null)
                    //{
                    //    ParentScreen.selector.Draw();
                    //    return;
                    //}
                }
                return;
            }
            else
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic,
                    84);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(123),
                    (float)EmpireManager.Player.data.MassModifier * mod.Mass, 79);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(124),
                    (float)mod.HealthMax + EmpireManager.Player.data.Traits.ModHpModifier * mod.HealthMax, 80);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(125),
                    (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(126),
                    (float)ModifiedWeaponStat(mod.InstalledWeapon, "range"), 82);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                if (!mod.InstalledWeapon.explodes || mod.InstalledWeapon.OrdinanceRequiredToFire <= 0f)
                {
                    if (mod.InstalledWeapon.isRepairBeam)
                    {
                        ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(135),
                            (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * -90f *
                            mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 166);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        ParentScreen.DrawStat(ref modTitlePos, "Duration", (float)mod.InstalledWeapon.BeamDuration, 188);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else if (mod.InstalledWeapon.isBeam)
                    {
                        ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(127),
                            (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * 90f *
                            mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 83);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        ParentScreen.DrawStat(ref modTitlePos, "Duration", (float)mod.InstalledWeapon.BeamDuration, 188);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(127),
                            (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus(), 83);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                }
                else
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(127),
                        (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                        EmpireManager.Player.data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount, 83);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                modTitlePos.X = modTitlePos.X + 152f;
                modTitlePos.Y = starty;
                if (!mod.InstalledWeapon.isBeam && !mod.InstalledWeapon.isRepairBeam)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(129),
                        (float)ModifiedWeaponStat(mod.InstalledWeapon, "speed"), 85);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.DamageAmount > 0f)
                {
                    if (mod.InstalledWeapon.isBeam)
                    {
                        float dps = (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() *
                                    90f * mod.InstalledWeapon.BeamDuration /
                                    (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus());
                        ParentScreen.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else if (mod.InstalledWeapon.explodes && mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
                    {
                        if (mod.InstalledWeapon.SalvoCount <= 1)
                        {
                            float dps =
                                1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                                ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                                 EmpireManager.Player.data.OrdnanceEffectivenessBonus *
                                 mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            ParentScreen.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else
                        {
                            float dps = (float)mod.InstalledWeapon.SalvoCount /
                                        (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") *
                                         GetHullFireRateBonus()) *
                                        ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") *
                                         GetHullDamageBonus() +
                                         EmpireManager.Player.data.OrdnanceEffectivenessBonus *
                                         mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            ParentScreen.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                            ParentScreen.DrawStat(ref modTitlePos, "Salvo", (float)mod.InstalledWeapon.SalvoCount, 182);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        }
                    }
                    else if (mod.InstalledWeapon.SalvoCount <= 1)
                    {
                        float dps =
                            1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                            ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                             (float)mod.InstalledWeapon.DamageAmount *
                             EmpireManager.Player.data.Traits.EnergyDamageMod);
                        dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                        ParentScreen.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        float dps = (float)mod.InstalledWeapon.SalvoCount /
                                    (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                                    ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                                     (float)mod.InstalledWeapon.DamageAmount *
                                     EmpireManager.Player.data.Traits.EnergyDamageMod);
                        dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                        ParentScreen.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        ParentScreen.DrawStat(ref modTitlePos, "Salvo", (float)mod.InstalledWeapon.SalvoCount, 182);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                }
                if (mod.InstalledWeapon.BeamPowerCostPerSecond > 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "Pwr/s", (float)mod.InstalledWeapon.BeamPowerCostPerSecond, 87);
                    modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                }
                ParentScreen.DrawStat(ref modTitlePos, "Delay", mod.InstalledWeapon.fireDelay, 183);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                if (mod.InstalledWeapon.EMPDamage > 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "EMP",
                        1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                        (float)mod.InstalledWeapon.EMPDamage, 110);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.SiphonDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.SiphonDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.SiphonDamage;
                    ParentScreen.DrawStat(ref modTitlePos, "Siphon", damage, 184);
                    modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.MassDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.MassDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.MassDamage;
                    ParentScreen.DrawStat(ref modTitlePos, "Tractor", damage, 185);
                    modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.PowerDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.PowerDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.PowerDamage;
                    ParentScreen.DrawStat(ref modTitlePos, "Pwr Dmg", damage, 186);
                    modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                }
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(130), (float)mod.FieldOfFire, 88);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                if (mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "Ord / Shot", (float)mod.InstalledWeapon.OrdinanceRequiredToFire,
                        89);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.PowerRequiredToFire > 0f)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "Pwr / Shot", (float)mod.InstalledWeapon.PowerRequiredToFire, 90);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.Tag_Guided && GlobalStats.ActiveModInfo != null &&
                    GlobalStats.ActiveModInfo.enableECM)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6005), (float)mod.InstalledWeapon.ECMResist, 155,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.EffectVsArmor != 1f)
                {
                    if (mod.InstalledWeapon.EffectVsArmor <= 1f)
                    {
                        float effectVsArmor = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
                        DrawVSResistBad(ref modTitlePos, "VS Armor",
                            string.Concat(effectVsArmor.ToString("#"), "%"), 147);
                    }
                    else
                    {
                        float single = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
                        DrawVSResist(ref modTitlePos, "VS Armor", string.Concat(single.ToString("#"), "%"), 147);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.EffectVSShields != 1f)
                {
                    if (mod.InstalledWeapon.EffectVSShields <= 1f)
                    {
                        float effectVSShields = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                        DrawVSResistBad(ref modTitlePos, "VS Shield",
                            string.Concat(effectVSShields.ToString("#"), "%"), 147);
                    }
                    else
                    {
                        float effectVSShields1 = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                        DrawVSResist(ref modTitlePos, "VS Shield",
                            string.Concat(effectVSShields1.ToString("#"), "%"), 147);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.ShieldPenChance > 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, "Shield Pen", mod.InstalledWeapon.ShieldPenChance, 181);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdinanceCapacity != 0)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2129), (float)mod.OrdinanceCapacity, 124);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.InstalledWeapon.TruePD)
                {
                    string fireRest = "Cannot Target Ships";
                    modTitlePos.Y = modTitlePos.Y + 2 * ((float)Fonts.Arial12Bold.LineSpacing);
                    modTitlePos.X = modTitlePos.X - 152f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos,
                        Color.LightCoral);
                    return;
                }
                if (!mod.InstalledWeapon.TruePD && mod.InstalledWeapon.Excludes_Fighters ||
                    mod.InstalledWeapon.Excludes_Corvettes || mod.InstalledWeapon.Excludes_Capitals ||
                    mod.InstalledWeapon.Excludes_Stations)
                {
                    string fireRest = "Cannot Target:";
                    modTitlePos.Y = modTitlePos.Y + 2 * ((float)Fonts.Arial12Bold.LineSpacing);
                    modTitlePos.X = modTitlePos.X - 152f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos,
                        Color.LightCoral);
                    modTitlePos.X = modTitlePos.X + 120f;

                    if (mod.InstalledWeapon.Excludes_Fighters)
                    {
                        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                        {
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Drones", modTitlePos,
                                Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Fighters", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Corvettes)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Corvettes", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Capitals)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Capitals", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Stations)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Stations", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }

                    return;
                }
                else
                    return;
            }
        }
        //Added by McShooterz: modifies weapon stats to reflect weapon tag bonuses
        private float ModifiedWeaponStat(Weapon weapon, string stat)
        {
            float value = 0;

            switch (stat)
            {
                case "damage":
                    value = weapon.DamageAmount;
                    break;
                case "range":
                    value = weapon.Range;
                    break;
                case "speed":
                    value = weapon.ProjectileSpeed;
                    break;
                case "firedelay":
                    value = weapon.fireDelay;
                    break;
                case "armor":
                    value = weapon.EffectVsArmor;
                    break;
                case "shield":
                    value = weapon.EffectVSShields;
                    break;
            }

            if (weapon.Tag_Missile)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Missile"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Energy)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Energy"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Torpedo)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Torpedo"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Kinetic)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Kinetic"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Hybrid)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Hybrid"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Railgun)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Railgun"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Explosive)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Explosive"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Guided)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Guided"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Intercept)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Intercept"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_PD)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["PD"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_SpaceBomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_BioWeapon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Drone)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Drone"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Subspace)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Subspace"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Warp)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Warp"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Cannon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Cannon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Beam)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Beam"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Bomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Bomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ShieldDamage;
                        break;
                }
            }
            return value;
        }
        private float GetHullDamageBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out bonus))
            {
                return 1f + bonus.DamageBonus;
            }
            return 1f;
        }
        private void DrawVSResist(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            ParentScreen.DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 105);
        }

        private void DrawVSResistBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            ParentScreen.DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 105);
        }
        private float GetHullFireRateBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out bonus))
            {
                return 1f - bonus.FireRateBonus;
            }
            return 1f;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            ChooseFighterSub = null;
            WeaponSl?.Dispose(ref WeaponSl);
            ChooseFighterSL?.Dispose(ref ChooseFighterSL);            
        }
    }
}