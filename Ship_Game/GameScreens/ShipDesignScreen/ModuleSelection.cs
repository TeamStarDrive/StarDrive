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
        public void ResetLists() => WeaponSl.ResetOnNextDraw = true;
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
        public override bool HandleInput(InputState input)
        {
            if (WeaponSl.HandleInput(input))
                return true;

            ChooseFighterSL.HandleInput(input);
            ActiveModSubMenu.HandleInputNoReset();
            if (!base.HandleInput(input))
                return false;
            WeaponSl.ResetOnNextDraw = true;
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
            var modTitlePos = new Vector2(ActiveModSubMenu.Menu.X + 10, ActiveModSubMenu.Menu.Y + 35);

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
                WriteLine(ref modTitlePos);
            }
            if (!mod.isWeapon || mod.InstalledWeapon == null)
            {
                DrawModuleStats(mod, modTitlePos, starty);
            }
            else
            {
                DrawWeaponStats(modTitlePos, mod, mod.InstalledWeapon, starty);
            }
        }

        private void DrawModuleStats(ShipModule mod, Vector2 modTitlePos, float starty)
        {
            if (mod.Cost != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(128),
                    (float) mod.Cost * UniverseScreen.GamePaceStatic, 84);
                WriteLine(ref modTitlePos);
            }
            if (mod.Mass != 0)
            {
                float MassMod = (float) EmpireManager.Player.data.MassModifier;
                float ArmourMassMod = (float) EmpireManager.Player.data.ArmourMassModifier;

                if (mod.ModuleType == ShipModuleType.Armor)
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(123), (ArmourMassMod * mod.Mass) * MassMod, 79);
                }
                else
                {
                    ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(123), MassMod * mod.Mass, 79);
                }
                WriteLine(ref modTitlePos);
            }
            if (mod.HealthMax != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(124),
                    (float) mod.HealthMax + mod.HealthMax * (float) EmpireManager.Player.data.Traits.ModHpModifier,
                    80);
                WriteLine(ref modTitlePos);
            }
            float powerDraw;
            if (mod.ModuleType != ShipModuleType.PowerPlant)
            {
                powerDraw = -(float) mod.PowerDraw;
            }
            else
            {
                powerDraw = (mod.PowerDraw > 0f
                    ? (float) (-mod.PowerDraw)
                    : mod.PowerFlowMax + mod.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod);
            }
            if (powerDraw != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(125), powerDraw, 81);
                WriteLine(ref modTitlePos);
            }
            if (mod.MechanicalBoardingDefense != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2231), (float) mod.MechanicalBoardingDefense, 143);
                WriteLine(ref modTitlePos);
            }
            if (mod.BonusRepairRate != 0f)
            {
                ParentScreen.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"),
                    (float) (
                        (mod.BonusRepairRate + mod.BonusRepairRate * EmpireManager.Player.data.Traits.RepairMod) *
                        (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                         ResourceManager.HullBonuses.ContainsKey(ParentScreen.ActiveHull.Hull)
                            ? 1f + ResourceManager.HullBonuses[ParentScreen.ActiveHull.Hull].RepairBonus
                            : 1)), 97);
                WriteLine(ref modTitlePos);
            }
            //Shift to next Column
            float MaxDepth = modTitlePos.Y;
            modTitlePos.X = modTitlePos.X + 152f;
            modTitlePos.Y = starty;
            if (mod.thrust != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(131), (float) mod.thrust, 91);
                WriteLine(ref modTitlePos);
            }
            if (mod.WarpThrust != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2064), (float) mod.WarpThrust, 92);
                WriteLine(ref modTitlePos);
            }
            if (mod.TurnThrust != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2260), (float) mod.TurnThrust, 148);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_power_max != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(132),
                    mod.shield_power_max *
                    (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                     ResourceManager.HullBonuses.ContainsKey(ParentScreen.ActiveHull.Hull)
                        ? 1f + ResourceManager.HullBonuses[ParentScreen.ActiveHull.Hull].ShieldBonus
                        : 1f) + EmpireManager.Player.data.ShieldPowerMod * mod.shield_power_max, 93);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_radius != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(133), (float) mod.shield_radius, 94);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_recharge_rate != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(134), (float) mod.shield_recharge_rate, 95);
                WriteLine(ref modTitlePos);
            }

            // Doc: new shield resistances, UI info.

            if (mod.shield_kinetic_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6162), (float) mod.shield_kinetic_resist, 209
                    , Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_energy_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6163), (float) mod.shield_energy_resist, 210,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_explosive_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6164), (float) mod.shield_explosive_resist, 211,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_missile_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6165), (float) mod.shield_missile_resist, 212,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_flak_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6166), (float) mod.shield_flak_resist, 213,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_hybrid_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6167), (float) mod.shield_hybrid_resist, 214,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_railgun_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6168), (float) mod.shield_railgun_resist, 215,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_subspace_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6169), (float) mod.shield_subspace_resist, 216,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_warp_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6170), (float) mod.shield_warp_resist, 217,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_beam_resist != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6171), (float) mod.shield_beam_resist, 218,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.shield_threshold != 0)
            {
                ParentScreen.DrawStatColor(ref modTitlePos, Localizer.Token(6176), (float) mod.shield_threshold, 222,
                    Color.LightSkyBlue, isPercent: true);
                WriteLine(ref modTitlePos);
            }


            if (mod.SensorRange != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(126), (float) mod.SensorRange, 96);
                WriteLine(ref modTitlePos);
            }
            if (mod.SensorBonus != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6121), (float) mod.SensorBonus, 167);
                WriteLine(ref modTitlePos);
            }
            if (mod.HealPerTurn != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6131), mod.HealPerTurn, 174);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterRange != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(126), (float) mod.TransporterRange, 168);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterPower != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6123), (float) mod.TransporterPower, 169);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterTimerConstant != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6122), (float) mod.TransporterTimerConstant, 170);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterOrdnance != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6124), (float) mod.TransporterOrdnance, 171);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterTroopAssault != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6135), (float) mod.TransporterTroopAssault, 187);
                WriteLine(ref modTitlePos);
            }
            if (mod.TransporterTroopLanding != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6128), (float) mod.TransporterTroopLanding, 172);
                WriteLine(ref modTitlePos);
            }
            if (mod.OrdinanceCapacity != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2129), (float) mod.OrdinanceCapacity, 124);
                WriteLine(ref modTitlePos);
            }
            if (mod.Cargo_Capacity != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(119), (float) mod.Cargo_Capacity, 109);
                WriteLine(ref modTitlePos);
            }
            if (mod.OrdnanceAddedPerSecond != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6120), (float) mod.OrdnanceAddedPerSecond, 162);
                WriteLine(ref modTitlePos);
            }
            if (mod.InhibitionRadius != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2233), (float) mod.InhibitionRadius, 144);
                WriteLine(ref modTitlePos);
            }
            if (mod.TroopCapacity != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(336), (float) mod.TroopCapacity, 173);
                WriteLine(ref modTitlePos);
            }
            if (mod.PowerStoreMax != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(2235),
                    (float) (mod.PowerStoreMax + mod.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier),
                    145);
                WriteLine(ref modTitlePos);
            }
            //added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
            if (mod.PowerDrawAtWarp != 0f)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6011), (float) (-mod.PowerDrawAtWarp), 178);
                WriteLine(ref modTitlePos);
            }
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM && mod.ECM != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6004), (float) mod.ECM, 154, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.ModuleType == ShipModuleType.Hangar && mod.hangarTimerConstant != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(136), (float) mod.hangarTimerConstant, 98);
                WriteLine(ref modTitlePos);
            }
            if (mod.explodes)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Explodes", modTitlePos,
                    Color.OrangeRed);
                WriteLine(ref modTitlePos);
            }
            if (mod.KineticResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6142), (float) mod.KineticResist, 189,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.EnergyResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6143), (float) mod.EnergyResist, 190,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.GuidedResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6144), (float) mod.GuidedResist, 191,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.MissileResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6145), (float) mod.MissileResist, 192,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.HybridResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6146), (float) mod.HybridResist, 193,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.BeamResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6147), (float) mod.BeamResist, 194, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.ExplosiveResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6148), (float) mod.ExplosiveResist, 195,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.InterceptResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6149), (float) mod.InterceptResist, 196,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.RailgunResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6150), (float) mod.RailgunResist, 197,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.SpaceBombResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6151), (float) mod.SpaceBombResist, 198,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.BombResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6152), (float) mod.BombResist, 199, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.BioWeaponResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6153), (float) mod.BioWeaponResist, 200,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.DroneResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6154), (float) mod.DroneResist, 201,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.WarpResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6155), (float) mod.WarpResist, 202, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.TorpedoResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6156), (float) mod.TorpedoResist, 203,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.CannonResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6157), (float) mod.CannonResist, 204,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.SubspaceResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6158), (float) mod.SubspaceResist, 205,
                    isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.PDResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6159), (float) mod.PDResist, 206, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.FlakResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6160), (float) mod.FlakResist, 207, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.APResist != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6161), (float) mod.APResist, 208, isPercent: true);
                WriteLine(ref modTitlePos);
            }
            if (mod.DamageThreshold != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6175), (float) mod.DamageThreshold, 221);
                WriteLine(ref modTitlePos);
            }
            if (mod.EMP_Protection != 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6174), (float) mod.EMP_Protection, 219);
                WriteLine(ref modTitlePos);
            }
            if (mod.FixedTracking > 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, Localizer.Token(6187), (float) mod.FixedTracking, 231);
                WriteLine(ref modTitlePos);
            }
            if (mod.TargetTracking > 0)
            {
                ParentScreen.DrawStat(ref modTitlePos, "+" + Localizer.Token(6186), (float) mod.TargetTracking, 226);
                WriteLine(ref modTitlePos);
            }


            if (mod.PermittedHangarRoles.Length > 0)
            {
                modTitlePos.Y = Math.Max(modTitlePos.Y, MaxDepth) + (float) Fonts.Arial12Bold.LineSpacing;
                Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                    string.Concat(Localizer.Token(137), " : ", mod.hangarShipUID), shipSelectionPos, Color.Orange);
                Rectangle r = ChooseFighterSub.Menu;
                r.Y = r.Y + 25;
                r.Height = r.Height - 25;
                var sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
                sel.Draw();
                ParentScreen.UpdateHangarOptions(mod);
                ChooseFighterSub.Draw();
                ChooseFighterSL.Draw(ScreenManager.SpriteBatch);
                Vector2 bCursor = new Vector2((float) (ChooseFighterSub.Menu.X + 15),
                    (float) (ChooseFighterSub.Menu.Y + 25));
                for (int i = ChooseFighterSL.indexAtTop;
                    i < ChooseFighterSL.Entries.Count && i < ChooseFighterSL.indexAtTop +
                    ChooseFighterSL.entriesToDisplay;
                    i++)
                {
                    ScrollList.Entry e = ChooseFighterSL.Entries[i];
                    bCursor.Y = (float) e.clickRect.Y;
                    ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict[
                            ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath],
                        new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        (!string.IsNullOrEmpty((e.item as Ship).VanityName)
                            ? (e.item as Ship).VanityName
                            : (e.item as Ship).Name), tCursor, Color.White);
                    tCursor.Y = tCursor.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
            }
        }

        private void DrawWeaponStats(Vector2 cursor, ShipModule m, Weapon w, float startY)
        {
            float range = ModifiedWeaponStat(w, WeaponStat.Range);
            float delay = ModifiedWeaponStat(w, WeaponStat.FireDelay) * GetHullFireRateBonus();
            float speed = ModifiedWeaponStat(w, WeaponStat.Speed);

            bool repair = w.isRepairBeam;
            bool isBeam      = repair || w.isBeam;
            bool isBallistic = w.explodes && w.OrdinanceRequiredToFire > 0f;
            float beamMultiplier = isBeam ? w.BeamDuration * (repair ? -90f : +90f) : 0f;

            float rawDamage       = ModifiedWeaponStat(w, WeaponStat.Damage) * GetHullDamageBonus();
            float beamDamage      = rawDamage * beamMultiplier;
            float ballisticDamage = rawDamage + rawDamage * EmpireManager.Player.data.OrdnanceEffectivenessBonus;
            float energyDamage    = rawDamage + rawDamage * EmpireManager.Player.data.Traits.EnergyDamageMod;

            float cost      = m.Cost * UniverseScreen.GamePaceStatic;
            float mass      = m.Mass * EmpireManager.Player.data.MassModifier;
            float maxHealth = m.HealthMax + EmpireManager.Player.data.Traits.ModHpModifier * m.HealthMax;
            float power     = m.ModuleType != ShipModuleType.PowerPlant ? -m.PowerDraw : m.PowerFlowMax;

            DrawStatLine(ref cursor, Localizer.Token(128), cost, 84);
            DrawStatLine(ref cursor, Localizer.Token(123), mass, 79);
            DrawStatLine(ref cursor, Localizer.Token(124), maxHealth, 80);
            DrawStatLine(ref cursor, Localizer.Token(125), power, 81);
            DrawStatLine(ref cursor, Localizer.Token(126), range, 82);


            if (isBeam)
            {
                DrawStatLine(ref cursor, Localizer.Token(repair ? 135 : 127), beamDamage, repair ? 166 : 83);
                DrawStatLine(ref cursor, "Duration", w.BeamDuration, 188);
            }
            else
                DrawStatLine(ref cursor, Localizer.Token(127), isBallistic ? ballisticDamage : energyDamage, 83);

            cursor.X += 152f;
            cursor.Y = startY;

            if (!isBeam) DrawStatLine(ref cursor, Localizer.Token(129), speed, 85);

            if (rawDamage > 0f)
            {
                int salvos = w.SalvoCount > 0 ? w.SalvoCount : 1;
                float dps = isBeam 
                    ? (beamDamage / delay)
                    : (salvos / delay) * w.ProjectileCount * (isBallistic ? ballisticDamage : energyDamage);

                DrawStatLine(ref cursor, "DPS", dps, 86);
                if (salvos > 1) DrawStatLine(ref cursor, "Salvo", salvos, 182);
            }

            if (w.BeamPowerCostPerSecond > 0f)
                DrawStatLine(ref cursor, "Pwr/s", w.BeamPowerCostPerSecond, 87);
            DrawStatLine(ref cursor, "Delay", delay, 183);

            if (w.EMPDamage > 0f)
                DrawStatLine(ref cursor, "EMP", (1f / delay) * w.EMPDamage, 110);

            if (w.SiphonDamage > 0f)
            {
                float siphon = w.SiphonDamage + w.SiphonDamage * beamMultiplier;
                DrawStatLine(ref cursor, "Siphon", siphon, 184);
            }
            if (w.MassDamage > 0f)
            {
                float tractor = w.MassDamage + w.MassDamage * beamMultiplier;
                DrawStatLine(ref cursor, "Tractor", tractor, 185);
            }
            if (w.PowerDamage > 0f)
            {
                float powerDamage = w.PowerDamage + w.PowerDamage * beamMultiplier;
                DrawStatLine(ref cursor, "Pwr Dmg", powerDamage, 186);
            }

            DrawStatLine(ref cursor, Localizer.Token(130), m.FieldOfFire, 88);

            if (w.OrdinanceRequiredToFire > 0f)
                DrawStatLine(ref cursor, "Ord / Shot", w.OrdinanceRequiredToFire, 89);
            if (w.PowerRequiredToFire > 0f) DrawStatLine(ref cursor, "Pwr / Shot", w.PowerRequiredToFire, 90);

            if (w.Tag_Guided && GlobalStats.HasMod && GlobalStats.ActiveModInfo.enableECM)
                DrawStatPercentLine(ref cursor, Localizer.Token(6005), w.ECMResist, 155);

            if (w.EffectVsArmor != 1f)   DrawResistancePercent(ref cursor, w, "VS Armor", WeaponStat.Armor);
            if (w.EffectVSShields != 1f) DrawResistancePercent(ref cursor, w, "VS Shield", WeaponStat.Shield);
            if (w.ShieldPenChance > 0)   DrawStatLine(ref cursor, "Shield Pen", w.ShieldPenChance, 181);
            if (m.OrdinanceCapacity != 0)   DrawStatLine(ref cursor, Localizer.Token(2129), m.OrdinanceCapacity, 124);

            if (w.TruePD)
            {
                WriteLine(ref cursor, 2);
                cursor.X -= 152f;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Cannot Target Ships", cursor, Color.LightCoral);
            }
            else 
            if (w.Excludes_Fighters || w.Excludes_Corvettes ||
                w.Excludes_Capitals || w.Excludes_Stations)
            {
                WriteLine(ref cursor, 2);
                cursor.X -= 152f;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Cannot Target:", cursor, Color.LightCoral);
                cursor.X += 120f;

                if (w.Excludes_Fighters)
                {
                    if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useDrones)
                        WriteLine(ref cursor, "Drones");
                    WriteLine(ref cursor, "Fighters");
                }
                if (w.Excludes_Corvettes) WriteLine(ref cursor, "Corvettes");
                if (w.Excludes_Capitals) WriteLine(ref cursor, "Capitals");
                if (w.Excludes_Stations) WriteLine(ref cursor, "Stations");
            }
        }

        private void DrawStatPercentLine(ref Vector2 cursor, string text, float stat, int tooltipId)
        {
            ParentScreen.DrawStat(ref cursor, text, stat, tooltipId, isPercent: true);
            WriteLine(ref cursor);
        }
        private void DrawStatLine(ref Vector2 cursor, string text, float stat, int tooltipId)
        {
            ParentScreen.DrawStat(ref cursor, text, stat, tooltipId);
            WriteLine(ref cursor);
        }
        private void WriteLine(ref Vector2 cursor, string text)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, Color.LightCoral);
            WriteLine(ref cursor);
        }
        private static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }

        private enum WeaponStat
        {
            Damage, Range, Speed, FireDelay, Armor, Shield,
        }

        private static float GetStatForWeapon(WeaponStat stat, Weapon weapon)
        {
            switch (stat)
            {
                case WeaponStat.Damage:    return weapon.DamageAmount;
                case WeaponStat.Range:     return weapon.Range;
                case WeaponStat.Speed:     return weapon.ProjectileSpeed;
                case WeaponStat.FireDelay: return weapon.fireDelay;
                case WeaponStat.Armor:     return weapon.EffectVsArmor;
                case WeaponStat.Shield:    return weapon.EffectVSShields;
                default: return 0f;
            }
        }

        private static float GetStatBonusForWeaponTag(WeaponStat stat, string tagType)
        {
            WeaponTagModifier tagModifier = EmpireManager.Player.data.WeaponTags[tagType];
            switch (stat)
            {
                case WeaponStat.Damage:    return tagModifier.Damage;
                case WeaponStat.Range:     return tagModifier.Range;
                case WeaponStat.Speed:     return tagModifier.Speed;
                case WeaponStat.FireDelay: return tagModifier.Rate;
                case WeaponStat.Armor:     return tagModifier.ArmorDamage;
                case WeaponStat.Shield:    return tagModifier.ShieldDamage;
                default: return 0f;
            }
        }

        private static float ModifiedWeaponStat(Weapon weapon, WeaponStat stat)
        {
            float value = GetStatForWeapon(stat, weapon);
            string[] activeTags = weapon.GetActiveTagIds();
            foreach (string tag in activeTags)
                value += value * GetStatBonusForWeaponTag(stat, tag);
            return value;
        }

        private void DrawResistancePercent(ref Vector2 cursor, Weapon weapon, string description, WeaponStat stat)
        {
            float effect = ModifiedWeaponStat(weapon, stat) * 100f;
            DrawVSResist(ref cursor, description, $"{effect}%", 147);
            cursor.Y += Fonts.Arial12Bold.LineSpacing;
        }

        private void DrawVSResist(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            ParentScreen.DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen, 105);
        }

        private float GetHullDamageBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out HullBonus bonus))
                return 1f + bonus.DamageBonus;
            return 1f;
        }

        private float GetHullFireRateBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out HullBonus bonus))
                return 1f - bonus.FireRateBonus;
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