using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class ModuleSelection : Submenu
    {
        WeaponScrollList WeaponSl;
        readonly ShipDesignScreen ParentScreen;
        public Rectangle Window;
        Submenu ActiveModSubMenu;
        readonly ScreenManager ScreenManager;
        Submenu ChooseFighterSub;
        public FighterScrollList ChooseFighterSL;
        public Rectangle Choosefighterrect;

        public void ResetLists() => WeaponSl.ResetOnNextDraw = true;

        public ModuleSelection(ShipDesignScreen parentScreen, Rectangle window) : base(window)
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
            var acsub = new Rectangle(active.X, Window.Y + Window.Height + 15, 305, 370);

            ActiveModSubMenu = new Submenu(acsub);
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
            ChooseFighterSub = new Submenu(Choosefighterrect);
            ChooseFighterSub.AddTab("Choose Fighter");
            ChooseFighterSL = new FighterScrollList(ChooseFighterSub, ParentScreen);
        }

        public bool HitTest(InputState input)
        {
            return Window.HitTest(input.CursorPosition) || ChooseFighterSL.HitTest(input);
        }

        public bool HandleInput(InputState input, ShipModule activeModule, ShipModule highlightedModule)
        {
            if (HitTest(input) && WeaponSl.HandleInput(input))
                return true;

            ChooseFighterSL.HandleInput(input, activeModule, highlightedModule);
            ActiveModSubMenu.HandleInput(input);
            if (!base.HandleInput(input))
                return false;

            ResetLists();
            return false;
        }

        public new void Draw(SpriteBatch batch)
        {
            Rectangle r = Rect;
            r.Y += 25;
            r.Height -= 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);

            WeaponSl.Draw(batch);
            if (ParentScreen.ActiveModule != null || ParentScreen.HighlightedModule != null)
            {
                ActiveModSubMenu.Draw(batch);
                DrawActiveModuleData();
            }
            ChooseFighterSL.Draw(batch);
            base.Draw(batch);
        }

        void DrawString(ref Vector2 cursorPos, string text, SpriteFont font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            ScreenManager.SpriteBatch.DrawString(font, text, cursorPos, Color.SpringGreen);
            cursorPos.X = cursorPos.X + Fonts.Arial8Bold.MeasureString(text).X;
        }

        void DrawString(ref Vector2 cursorPos, string text, Color color, SpriteFont font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            ScreenManager.SpriteBatch.DrawString(font, text, cursorPos, color);
            cursorPos.X = cursorPos.X + font.MeasureString(text).X;
        }

        void DrawActiveModuleData()
        {
            Rectangle r = ActiveModSubMenu.Rect;
            int down = 25;
            r.Y += down;
            r.Height -= down;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            ShipModule mod = ParentScreen.ActiveModule ?? ParentScreen.HighlightedModule;

            if (!ActiveModSubMenu.Tabs[0].Selected || mod == null)
                return;

            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);

            //Added by McShooterz: Changed how modules names are displayed for allowing longer names
            var modTitlePos = new Vector2(ActiveModSubMenu.X + 10, ActiveModSubMenu.Y + 35);

            if (Fonts.Arial20Bold.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X + 16 <
                ActiveModSubMenu.Width)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y += (Fonts.Arial20Bold.LineSpacing + 6);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y += (Fonts.Arial14Bold.LineSpacing + 4);
            }
            string rest = "";
            switch (moduleTemplate.Restrictions)
            {
                case Restrictions.IO:  rest = "Any Slot except E"; break;
                case Restrictions.I:   rest = "I, IO, IE or IOE"; break;
                case Restrictions.O:   rest = "O, IO, OE, or IOE"; break;
                case Restrictions.E:   rest = "E, IE, OE, or IOE"; break;
                case Restrictions.IOE: rest = "Any Slot"; break;
                case Restrictions.IE:  rest = "Any Slot except O"; break;
                case Restrictions.OE:  rest = "Any Slot except I"; break;
                case Restrictions.xI:  rest = "Only I"; break;
                case Restrictions.xIO: rest = "Only IO"; break;
                case Restrictions.xO:  rest = "Only O"; break;
            }

            // Concat ship class restrictions
            string shipRest = "";
            bool specialString = false;

            bool modDrones = GlobalStats.ActiveModInfo?.useDrones == true;
            bool modDestroyers = GlobalStats.ActiveModInfo?.useDestroyers == true;
            if (modDrones && modDestroyers)
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

                /////

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

                /////

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


            if (GlobalStats.ActiveModInfo == null || (!modDrones && !modDestroyers))
            {
                if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule &&
                    mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule &&
                    mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                }
                else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule &&
                         !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule &&
                         !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "Fighters Only";
                }
                else if (mod.FightersOnly && !specialString)
                {
                    shipRest = "Fighters/Corvettes Only";
                }
                else if (!mod.FightersOnly && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule &&
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                }
            }
            else if (!specialString &&
                     (!mod.DroneModule && modDrones) || (!mod.DestroyerModule && modDestroyers) ||
                     !mod.FighterModule || !mod.CorvetteModule || !mod.FrigateModule || !mod.CruiserModule  ||
                     !mod.CruiserModule || !mod.CarrierModule  || !mod.CapitalModule || !mod.PlatformModule ||
                     !mod.StationModule || !mod.FreighterModule)
            {
                if (mod.DroneModule && modDrones) shipRest += "Dr ";
                if (mod.FighterModule)  shipRest += "F ";
                if (mod.CorvetteModule) shipRest += "CO ";
                if (mod.FrigateModule)  shipRest += "FF ";
                if (mod.DestroyerModule && modDestroyers) shipRest += "DD ";
                if (mod.CruiserModule) shipRest += "CC ";
                if (mod.CarrierModule) shipRest += "CV ";
                if (mod.CapitalModule) shipRest += "CA ";
                if (mod.FreighterModule) shipRest += "Frt ";
                if (mod.PlatformModule || mod.StationModule) shipRest += "Stat ";
            }

            //DrawString(ref modTitlePos, string.Concat(Localizer.Token(122), ": ", rest));
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.Token(122)+": "+rest, modTitlePos, Color.Orange);
            modTitlePos.Y = modTitlePos.Y + Fonts.Arial8Bold.LineSpacing;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, "Hulls: "+shipRest, modTitlePos, Color.LightSteelBlue);
            modTitlePos.Y = modTitlePos.Y + (Fonts.Arial8Bold.LineSpacing + 11);
            int startx = (int)modTitlePos.X;
            if (moduleTemplate.IsWeapon && moduleTemplate.BombType == null)
            {
                var weaponTemplate = ResourceManager.GetWeaponTemplate(moduleTemplate.WeaponType);

                var sb = new StringBuilder();
                if (weaponTemplate.Tag_Guided)    sb.Append("GUIDED ");
                if (weaponTemplate.Tag_Intercept) sb.Append("INTERCEPTABLE ");
                if (weaponTemplate.Tag_Energy)    sb.Append("ENERGY ");
                if (weaponTemplate.Tag_Hybrid)    sb.Append("HYBRID ");
                if (weaponTemplate.Tag_Kinetic)   sb.Append("KINETIC ");
                if (weaponTemplate.Tag_Explosive && !weaponTemplate.Tag_Flak) sb.Append("EXPLOSIVE ");
                if (weaponTemplate.Tag_Subspace)  sb.Append("SUBSPACE ");
                if (weaponTemplate.Tag_Warp)      sb.Append("WARP ");
                if (weaponTemplate.Tag_PD)        sb.Append("POINT DEFENSE ");
                if (weaponTemplate.Tag_Flak)      sb.Append("FLAK ");

                if (GlobalStats.ActiveModInfo?.expandedWeaponCats == true && weaponTemplate.Tag_Missile && !weaponTemplate.Tag_Guided)
                    sb.Append("ROCKET ");
                else if (weaponTemplate.Tag_Missile)
                    sb.Append("MISSILE ");

                if (weaponTemplate.Tag_Tractor)   sb.Append("TRACTOR ");
                if (weaponTemplate.Tag_Beam)      sb.Append("BEAM ");
                if (weaponTemplate.Tag_Array)     sb.Append("ARRAY ");
                if (weaponTemplate.Tag_Railgun)   sb.Append("RAILGUN ");
                if (weaponTemplate.Tag_Torpedo)   sb.Append("TORPEDO ");
                if (weaponTemplate.Tag_Bomb)      sb.Append("BOMB ");
                if (weaponTemplate.Tag_BioWeapon) sb.Append("BIOWEAPON ");
                if (weaponTemplate.Tag_SpaceBomb) sb.Append("SPACEBOMB ");
                if (weaponTemplate.Tag_Drone)     sb.Append("DRONE ");
                if (weaponTemplate.Tag_Cannon)    sb.Append("CANNON ");
                DrawString(ref modTitlePos, sb.ToString(), Fonts.Arial8Bold);

                modTitlePos.Y = modTitlePos.Y + (Fonts.Arial8Bold.LineSpacing + 5);
                modTitlePos.X = startx;
            }

            string txt = Fonts.Arial12.ParseText(Localizer.Token(moduleTemplate.DescriptionIndex),
                                                 ActiveModSubMenu.Width - 20);

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
            modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
            float starty = modTitlePos.Y;
            modTitlePos.X = 10;
            float strength = mod.CalculateModuleOffenseDefense(ParentScreen.ActiveHull.ModuleSlots.Length);
            DrawStat(ref modTitlePos, "Offense", strength, 227);

            if (mod.BombType == null && !mod.isWeapon || mod.InstalledWeapon == null)
            {
                DrawModuleStats(mod, modTitlePos, starty);
            }
            else
            {
                DrawWeaponStats(modTitlePos, mod, mod.InstalledWeapon, starty);
            }
        }
        void DrawStat(ref Vector2 cursor, string text, float stat, int toolTipId, bool isPercent = false)
        {
            if (stat.AlmostEqual(0.0f))
                return;
            ParentScreen.DrawStat(ref cursor, text, stat, Color.White, toolTipId, spacing: ActiveModSubMenu.Width * 0.33f, isPercent: isPercent);
        }

        void DrawStat(ref Vector2 cursor, int textId, float stat, int toolTipId, bool isPercent = false)
        {
            if (stat.AlmostEqual(0.0f))
                return;
            ParentScreen.DrawStat(ref cursor, Localizer.Token(textId), stat, Color.White, toolTipId, spacing: ActiveModSubMenu.Width * 0.33f, isPercent: isPercent);
        }

        void DrawStat(ref Vector2 cursor, string text, string stat, int toolTipId)
        {
            if (stat.IsEmpty())
                return;
            ParentScreen.DrawStat(ref cursor, text, stat, toolTipId, Color.White, Color.LightGreen, spacing: ActiveModSubMenu.Width * 0.33f, lineSpacing: 0);
            WriteLine(ref cursor);
        }

        void DrawStatShieldResist(ref Vector2 cursor, int titleId, float stat, int toolTipId, bool isPercent = true)
        {
            if (stat.AlmostEqual(0.0f))
                return;
            ParentScreen.DrawStat(ref cursor, Localizer.Token(titleId), stat, Color.LightSkyBlue, toolTipId, spacing: ActiveModSubMenu.Width * 0.33f, isPercent: isPercent);
        }

        void DrawString(ref Vector2 cursor, string text, bool valueCheck)
        {
            if (!valueCheck)
                return;
            WriteLine(ref cursor);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, Color.OrangeRed);
    }

        void DrawModuleStats(ShipModule mod, Vector2 modTitlePos, float starty)
        {
            DrawStat(ref modTitlePos, 128, mod.ActualCost, 84);
            float mass = mod.Mass * EmpireManager.Player.data.MassModifier;
            if (mod.ModuleType == ShipModuleType.Armor)
                mass *= EmpireManager.Player.data.ArmourMassModifier;

            DrawStat(ref modTitlePos, 123, mass, 79);
            DrawStat(ref modTitlePos, 124, mod.ActualMaxHealth, 80);
            float powerDraw;
            if (mod.ModuleType != ShipModuleType.PowerPlant)
            {
                powerDraw = -mod.PowerDraw;
            }
            else
            {
                powerDraw = (mod.PowerDraw > 0f ? -mod.PowerDraw : mod.ActualPowerFlowMax);
            }
            DrawStat(ref modTitlePos, Localizer.Token(125), powerDraw, 81);
            DrawStat(ref modTitlePos, Localizer.Token(2231), mod.MechanicalBoardingDefense, 143);
            DrawStat(ref modTitlePos, Localizer.Token(135)+"+", mod.ActualBonusRepairRate, 97);

            float maxDepth = modTitlePos.Y;
            modTitlePos.X = modTitlePos.X + 152f;
            modTitlePos.Y = starty;

            DrawStat(ref modTitlePos, Localizer.Token(131), mod.thrust, 91);
            DrawStat(ref modTitlePos, Localizer.Token(2064), mod.WarpThrust, 92);
            DrawStat(ref modTitlePos, Localizer.Token(2260), mod.TurnThrust, 148);

            float shieldMax = mod.ActualShieldPowerMax;
            DrawStat(ref modTitlePos, Localizer.Token(132), shieldMax, 93);

            DrawStat(ref modTitlePos, Localizer.Token(133), mod.shield_radius, 94);
            DrawStat(ref modTitlePos, Localizer.Token(134), mod.shield_recharge_rate, 95);
            DrawStat(ref modTitlePos, Localizer.Token(1994), mod.shield_recharge_combat_rate, 1993);

            // Doc: new shield resistances, UI info.
            DrawStatShieldResist(ref modTitlePos, 6162, mod.shield_kinetic_resist, 209);
            DrawStatShieldResist(ref modTitlePos, 6163, mod.shield_energy_resist, 210);
            DrawStatShieldResist(ref modTitlePos, 6164, mod.shield_explosive_resist, 211);
            DrawStatShieldResist(ref modTitlePos, 6165, mod.shield_missile_resist, 212);
            DrawStatShieldResist(ref modTitlePos, 6166, mod.shield_flak_resist, 213);
            DrawStatShieldResist(ref modTitlePos, 6167, mod.shield_hybrid_resist, 214);
            DrawStatShieldResist(ref modTitlePos, 6168, mod.shield_railgun_resist, 215);
            DrawStatShieldResist(ref modTitlePos, 6169, mod.shield_subspace_resist, 216);
            DrawStatShieldResist(ref modTitlePos, 6170, mod.shield_warp_resist, 217);
            DrawStatShieldResist(ref modTitlePos, 6171, mod.shield_beam_resist, 218);
            DrawStatShieldResist(ref modTitlePos, 6176, mod.shield_threshold, 222, isPercent: false);

            DrawStat(ref modTitlePos, 126,  mod.SensorRange, 96);
            DrawStat(ref modTitlePos, 6121, mod.SensorBonus, 167);
            DrawStat(ref modTitlePos, 6131, mod.HealPerTurn, 174);
            DrawStat(ref modTitlePos, 126,  mod.TransporterRange, 168);
            DrawStat(ref modTitlePos, 6123, mod.TransporterPower, 169);
            DrawStat(ref modTitlePos, 6122, mod.TransporterTimerConstant, 170);
            DrawStat(ref modTitlePos, 6124, mod.TransporterOrdnance, 171);
            DrawStat(ref modTitlePos, 6135, mod.TransporterTroopAssault, 187);
            DrawStat(ref modTitlePos, 6128, mod.TransporterTroopLanding, 172);
            DrawStat(ref modTitlePos, 2129, mod.OrdinanceCapacity, 124);
            DrawStat(ref modTitlePos, 119,  mod.Cargo_Capacity, 109);
            DrawStat(ref modTitlePos, 6120, mod.OrdnanceAddedPerSecond, 162);
            DrawStat(ref modTitlePos, 2233, mod.InhibitionRadius, 144);
            DrawStat(ref modTitlePos, 336,  mod.TroopCapacity, 173);
            DrawStat(ref modTitlePos, 2235, mod.ActualPowerStoreMax, 145);

            //added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
            // FB improved it to use the Power struct
            ShipModule[] modlist = { mod };
            Power modNetWarpPowerDraw = Power.Calculate(modlist, EmpireManager.Player, ParentScreen.ActiveHull.ShieldsBehavior, true);
            DrawStat(ref modTitlePos, Localizer.Token(6011), -modNetWarpPowerDraw.NetWarpPowerDraw, 178);

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM)
            {
                DrawStat(ref modTitlePos, Localizer.Token(6004), mod.ECM, 154, isPercent: true);

            }
            if (mod.ModuleType == ShipModuleType.Hangar)
            {
                DrawStat(ref modTitlePos, Localizer.Token(136), mod.hangarTimerConstant, 98);
            }
            if (mod.explodes)
            {
                DrawString(ref modTitlePos, "Explodes", mod.explodes);
                DrawStat(ref modTitlePos, Localizer.Token(1998), mod.ExplosionDamage, 238);
                DrawStat(ref modTitlePos, Localizer.Token(1997), mod.ExplosionRadius, 239);
            }
            DrawStat(ref modTitlePos, Localizer.Token(6142), mod.KineticResist, 189, true);
            DrawStat(ref modTitlePos, Localizer.Token(6143), mod.EnergyResist, 190,  true);
            DrawStat(ref modTitlePos, Localizer.Token(6144), mod.GuidedResist, 191,  true);
            DrawStat(ref modTitlePos, Localizer.Token(6145), mod.MissileResist, 192, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6146), mod.HybridResist, 193, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6147), mod.BeamResist, 194, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6148), mod.ExplosiveResist, 195, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6149), mod.InterceptResist, 196, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6150), mod.RailgunResist, 197, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6151), mod.SpaceBombResist, 198, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6152), mod.BombResist, 199, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6153), mod.BioWeaponResist, 200, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6154), mod.DroneResist, 201, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6155), mod.WarpResist, 202, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6156), mod.TorpedoResist, 203, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6157), mod.CannonResist, 204, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6158), mod.SubspaceResist, 205, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6159), mod.PDResist, 206, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6160), mod.FlakResist, 207, isPercent: true);
            DrawStat(ref modTitlePos, Localizer.Token(6161), mod.APResist, 208);
            DrawStat(ref modTitlePos, Localizer.Token(6175), mod.DamageThreshold, 221);
            DrawStat(ref modTitlePos, Localizer.Token(6174), mod.EMP_Protection, 219);
            DrawStat(ref modTitlePos, Localizer.Token(6187), mod.FixedTracking, 231);
            DrawStat(ref modTitlePos, $"+{Localizer.Token(6186)}", mod.TargetTracking, 226);
            if (mod.RepairDifficulty > 0) DrawStat(ref modTitlePos, Localizer.Token(1992), mod.RepairDifficulty, 241); // Complexity

            if (mod.PermittedHangarRoles.Length == 0)
                return;
            DynamicHangarOptions hangarOption = ShipBuilder.GetDynamicHangarOptions(mod.hangarShipUID);
            if (hangarOption != DynamicHangarOptions.Static)
            {
                modTitlePos.Y = Math.Max(modTitlePos.Y, maxDepth) + Fonts.Arial10.LineSpacing + 10;
                Vector2 bestShipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
                string bestShip = Fonts.Arial12Bold.ParseText(GetDynamicHangarText(), ActiveModSubMenu.Width - 20);
                Color color = ShipBuilder.GetHangarTextColor(mod.hangarShipUID);
                DrawString(ref bestShipSelectionPos, bestShip, color, Fonts.Arial12Bold);
                return;
            }
            Ship ship = ResourceManager.GetShipTemplate(mod.hangarShipUID, false);
            if (ship == null) return;
            modTitlePos.Y = Math.Max(modTitlePos.Y, maxDepth) + Fonts.Arial12Bold.LineSpacing;
            Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
            string name = ship.VanityName.IsEmpty() ? ship.Name : ship.VanityName;
            DrawString(ref shipSelectionPos, string.Concat(Localizer.Token(137), " : ", name), Fonts.Arial20Bold);
            shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
            shipSelectionPos.Y += Fonts.Arial12Bold.LineSpacing *2;
            DrawStat(ref shipSelectionPos, "LaunchCost", ship.ShipOrdLaunchCost, -1);
            DrawStat(ref shipSelectionPos, "Weapons", ship.Weapons.Count, -1);
            DrawStat(ref shipSelectionPos, "Health", ship.HealthMax, -1);
            DrawStat(ref shipSelectionPos, "FTL", ship.maxFTLSpeed, -1);

            string GetDynamicHangarText()
            {
                switch (hangarOption)
                {
                    case DynamicHangarOptions.DynamicLaunch:
                        return "Hangar will launch more advanced ships, as they become available in your empire";
                    case DynamicHangarOptions.DynamicInterceptor:
                        return "Hangar will launch more advanced ships which their designated ship category is 'Fighter', " +
                               "as they become available in your empire. If no Fighters are available, the strongest ship will be launched";
                    case DynamicHangarOptions.DynamicAntiShip:
                        return "Hangar will launch more advanced ships which their designated ship category is 'Bomber', " +
                               "as they become available in your empire. If no Fighters are available, the strongest ship will be launched";
                    default:
                        return "";
                }
            }
        }

        void DrawWeaponStats(Vector2 cursor, ShipModule m, Weapon w, float startY)
        {
            float range = ModifiedWeaponStat(w, WeaponStat.Range);
            float delay = ModifiedWeaponStat(w, WeaponStat.FireDelay) * GetHullFireRateBonus();
            float speed = ModifiedWeaponStat(w, WeaponStat.Speed);
            
            bool repair = w.isRepairBeam;
            bool isBeam = repair || w.isBeam;
            bool isBallistic = w.explodes && w.OrdinanceRequiredToFire > 0f;
            float beamMultiplier = isBeam ? w.BeamDuration * (repair ? -60f : +60f) : 0f;

            float rawDamage = ModifiedWeaponStat(w, WeaponStat.Damage) * GetHullDamageBonus();
            float beamDamage      = rawDamage * beamMultiplier;
            float ballisticDamage = rawDamage + rawDamage * EmpireManager.Player.data.OrdnanceEffectivenessBonus;
            float energyDamage    = rawDamage;

            float cost  = m.ActualCost;
            float mass  = m.Mass * EmpireManager.Player.data.MassModifier;
            float power = m.ModuleType != ShipModuleType.PowerPlant ? -m.PowerDraw : m.PowerFlowMax;

            DrawStat(ref cursor, Localizer.Token(128), cost, 84);
            DrawStat(ref cursor, Localizer.Token(123), mass, 79);
            DrawStat(ref cursor, Localizer.Token(124), m.ActualMaxHealth, 80);
            DrawStat(ref cursor, Localizer.Token(125), power, 81);
            DrawStat(ref cursor, Localizer.Token(126), range, 82);

            if (isBeam)
            {
                DrawStat(ref cursor, Localizer.Token(repair ? 135 : 127), beamDamage, repair ? 166 : 83);
                DrawStat(ref cursor, "Duration", w.BeamDuration, 188);
            }
            else
                DrawStat(ref cursor, Localizer.Token(127), isBallistic ? ballisticDamage : energyDamage, 83);

            cursor.X += 152f;
            cursor.Y = startY;

            if (!isBeam) DrawStat(ref cursor, Localizer.Token(129), speed, 85);

            if (rawDamage > 0f)
            {
                int salvos = w.SalvoCount > 0 ? w.SalvoCount : 1;
                int projectiles = w.ProjectileCount > 0 ? w.ProjectileCount : 1;
                float dps = isBeam ? (beamDamage / delay)
                                   : (salvos / delay) * w.ProjectileCount * (isBallistic ? ballisticDamage : energyDamage);

                DrawStat(ref cursor, "DPS", dps, 86);
                if (salvos > 1) DrawStat(ref cursor, "Salvo", salvos, 182);
                if (projectiles > 1) DrawStat(ref cursor, "Projectiles", projectiles, 242);
            }


            DrawStat(ref cursor, "Pwr/s", w.BeamPowerCostPerSecond, 87);
            DrawStat(ref cursor, "Delay", delay, 183);


            DrawStat(ref cursor, "EMP", w.EMPDamage, 110);
            float siphon = w.SiphonDamage + w.SiphonDamage * beamMultiplier;
            DrawStat(ref cursor, "Siphon", siphon, 184);

            float tractor = w.MassDamage + w.MassDamage * beamMultiplier;
            DrawStat(ref cursor, "Tractor", tractor, 185);
            float powerDamage = w.PowerDamage + w.PowerDamage * beamMultiplier;
            DrawStat(ref cursor, "Pwr Dmg", powerDamage, 186);

            DrawStat(ref cursor, Localizer.Token(130), m.FieldOfFire.ToDegrees(), 88);
            DrawStat(ref cursor, "Ord / Shot", w.OrdinanceRequiredToFire, 89);


            DrawStat(ref cursor, "Pwr / Shot", w.PowerRequiredToFire, 90);

            if (w.Tag_Guided && GlobalStats.HasMod && GlobalStats.ActiveModInfo.enableECM)
                DrawStatPercentLine(ref cursor, Localizer.Token(6005), w.ECMResist, 155);

            //if (w.EffectVsArmor != 1f)
                DrawResistancePercent(ref cursor, w, "VS Armor", WeaponStat.Armor);
            //if (w.EffectVSShields != 1f)
                DrawResistancePercent(ref cursor, w, "VS Shield", WeaponStat.Shield);
            //if (w.ShieldPenChance > 0)
                DrawStat(ref cursor, "Shield Pen", w.ShieldPenChance / 100, 181, isPercent: true);
                DrawStat(ref cursor, Localizer.Token(2129), m.OrdinanceCapacity, 124);

            if (m.RepairDifficulty > 0) DrawStat(ref cursor, Localizer.Token(1992), m.RepairDifficulty, 241); // Complexity

            if (w.TruePD)
            {
                WriteLine(ref cursor);
                DrawString(ref cursor, "Cannot Target Ships" );
            }
            else
            if (w.Excludes_Fighters || w.Excludes_Corvettes ||
                w.Excludes_Capitals || w.Excludes_Stations)
            {
                WriteLine(ref cursor);
                DrawString(ref cursor, "Cannot Target:");

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

        void DrawStatPercentLine(ref Vector2 cursor, string text, float stat, int tooltipId)
        {
            DrawStat(ref cursor, text, stat, tooltipId, isPercent: true);
            WriteLine(ref cursor);
        }

        void WriteLine(ref Vector2 cursor, string text)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, text, cursor, Color.LightCoral);
            WriteLine(ref cursor);
        }

        static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }

        static float GetStatForWeapon(WeaponStat stat, Weapon weapon)
        {
            switch (stat)
            {
                case WeaponStat.Damage:    return weapon.DamageAmount;
                case WeaponStat.Range:     return weapon.BaseRange;
                case WeaponStat.Speed:     return weapon.ProjectileSpeed;
                case WeaponStat.FireDelay: return weapon.NetFireDelay;
                case WeaponStat.Armor:     return weapon.EffectVsArmor;
                case WeaponStat.Shield:    return weapon.EffectVSShields;
                default: return 0f;
            }
        }

        static float ModifiedWeaponStat(Weapon weapon, WeaponStat stat)
        {
            float value = GetStatForWeapon(stat, weapon);
            foreach (WeaponTag tag in weapon.ActiveWeaponTags)
                value += value * EmpireManager.Player.data.GetStatBonusForWeaponTag(stat, tag);
            return value;
        }

        void DrawResistancePercent(ref Vector2 cursor, Weapon weapon, string description, WeaponStat stat)
        {
            float effect = ModifiedWeaponStat(weapon, stat);
            if (effect != 1f) DrawStat(ref cursor, description, effect, 147, isPercent: true);
        }

        float GetHullDamageBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out HullBonus bonus))
                return 1f + bonus.DamageBonus;
            return 1f;
        }

        float GetHullFireRateBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(ParentScreen.ActiveHull.Hull, out HullBonus bonus))
                return 1f - bonus.FireRateBonus;
            return 1f;
        }
    }
}