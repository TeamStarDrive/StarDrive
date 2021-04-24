using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class ModuleSelection : Submenu
    {
        readonly ShipDesignScreen Screen;
        readonly FighterScrollList ChooseFighterSL;
        readonly ModuleSelectScrollList ModuleSelectList;
        readonly Submenu ActiveModSubMenu;
        readonly TexturedButton Obsolete;

        public ModuleSelection(ShipDesignScreen screen, in Rectangle window) : base(window)
        {
            Screen = screen;
            // rounded black background
            Background = new Selector(Rect.CutTop(25), new Color(0, 0, 0, 210));

            AddTab("Wpn");
            AddTab("Pwr");
            AddTab("Def");
            AddTab("Spc");
            ModuleSelectList = Add(new ModuleSelectScrollList(this, Screen));

            var acsub = new Rectangle(Rect.X, Rect.Bottom + 15, 305, 400);

            ActiveModSubMenu = Add(new Submenu(acsub));
            ActiveModSubMenu.AddTab("Active Module");
            // rounded black background
            ActiveModSubMenu.Background = new Selector(ActiveModSubMenu.Rect.CutTop(25), new Color(0, 0, 0, 210));
            int obsoleteW = ResourceManager.Texture("NewUI/icon_queue_delete").Width;
            int obsoleteH = ResourceManager.Texture("NewUI/icon_queue_delete").Height;
            Rectangle obsoletePos = new Rectangle((int)(ActiveModSubMenu.X + ActiveModSubMenu.Width - obsoleteW - 10), (int)ActiveModSubMenu.Y + 38, obsoleteW, obsoleteH);
            Obsolete = new TexturedButton(obsoletePos, "NewUI/icon_queue_delete", "NewUI/icon_queue_delete_hover1", "NewUI/icon_queue_delete_hover2");
            Obsolete.Tooltip = GameText.MarkThisModuleAsObsolete;
            var chooseFighterRect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y - 90, 240, 270);
            if (chooseFighterRect.Bottom > Screen.ScreenHeight)
            {
                int diff = chooseFighterRect.Bottom - Screen.ScreenHeight;
                chooseFighterRect.Height -= (diff + 10);
            }
            chooseFighterRect.Height = acsub.Height;

            var chooseFighterSub = new Submenu(chooseFighterRect);
            chooseFighterSub.AddTab("Choose Fighter");
            ChooseFighterSL = Add(new FighterScrollList(chooseFighterSub, Screen));
            ChooseFighterSL.EnableItemHighlight = true;
        }

        protected override void OnTabChangedEvt(int newIndex)
        {
            ModuleSelectList.SetActiveCategory(newIndex);
            base.OnTabChangedEvt(newIndex);
        }

        public void ResetActiveCategory()
        {
            ModuleSelectList.SetActiveCategory(SelectedIndex);
        }

        float ActiveModStatSpacing => ActiveModSubMenu.Width * 0.27f;

        public bool HitTest(InputState input)
        {
            return Rect.HitTest(input.CursorPosition) || ChooseFighterSL.HitTest(input);
        }

        public override bool HandleInput(InputState input)
        {
            if (HandleObsoleteInput(input))
                return true;

            return base.HandleInput(input);
        }

        bool HandleObsoleteInput(InputState input)
        {
            if (Obsolete.HandleInput(input))
            {
                ShipModule m = Screen.ActiveModule;
                if (input.LeftMouseClick && m != null)
                {
                    if (!m.IsObsolete())
                        EmpireManager.Player.ObsoletePlayerShipModules.Add(m.UID);
                    else
                        EmpireManager.Player.ObsoletePlayerShipModules.Remove(m.UID);

                    return true;
                }
            }

            return false;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (SelectedIndex == -1)
                SelectedIndex = 0; // this will trigger OnTabChangedEvt

            ActiveModSubMenu.Visible = Screen.ActiveModule != null || Screen.HighlightedModule != null;
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            if (ActiveModSubMenu.Visible)
            {
                DrawActiveModuleData(batch);
            }
        }

        static void DrawString(SpriteBatch batch, ref Vector2 cursorPos, string text, Graphics.Font font = null)
        {
            if (font == null) 
                font = Fonts.Arial8Bold;
            batch.DrawString(font, text, cursorPos, Color.SpringGreen);
            cursorPos.X += font.TextWidth(text);
        }

        static void DrawStringRed(SpriteBatch batch, ref Vector2 cursorPos, string text, Graphics.Font font = null)
        {
            if (font == null) 
                font = Fonts.Arial10;

            cursorPos.Y += 5;
            batch.DrawString(font, text, cursorPos, Color.Red);
            cursorPos.X += font.TextWidth(text)+2;
        }

        static void DrawString(SpriteBatch batch, ref Vector2 cursorPos, string text, Color color, Graphics.Font font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            batch.DrawString(font, text, cursorPos, color);
            cursorPos.X += font.TextWidth(text);
        }

        void DrawActiveModuleData(SpriteBatch batch)
        {
            ShipModule mod = Screen.ActiveModule ?? Screen.HighlightedModule;

            if (ActiveModSubMenu.SelectedIndex != 0 || mod == null)
                return;

            bool isObsolete = mod.IsObsolete();
            Color nameColor = isObsolete ? Color.Red : Color.White;
            Obsolete.BaseColor = nameColor;
            Obsolete.Draw(batch);
            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);
            //Added by McShooterz: Changed how modules names are displayed for allowing longer names
            var modTitlePos = new Vector2(ActiveModSubMenu.X + 10, ActiveModSubMenu.Y + 35);

            if (Fonts.Arial20Bold.TextWidth(moduleTemplate.NameText.Text) + 40 < ActiveModSubMenu.Width)
            {
                batch.DrawString(Fonts.Arial20Bold, moduleTemplate.NameText.Text,
                    modTitlePos, nameColor);
                modTitlePos.Y += (Fonts.Arial20Bold.LineSpacing + 6);
            }
            else
            {
                batch.DrawString(Fonts.Arial14Bold, moduleTemplate.NameText.Text,
                    modTitlePos, nameColor);
                modTitlePos.Y += (Fonts.Arial14Bold.LineSpacing + 4);
            }

            string rest = "";
            switch (moduleTemplate.Restrictions)
            {
                case Restrictions.IO:  rest = "Any Slot except E"; break;
                case Restrictions.I:   rest = "I, IO, IE or IOE";  break;
                case Restrictions.O:   rest = "O, IO, OE, or IOE"; break;
                case Restrictions.E:   rest = "E, IE, OE, or IOE"; break;
                case Restrictions.IOE: rest = "Any Slot";          break;
                case Restrictions.IE:  rest = "Any Slot except O"; break;
                case Restrictions.OE:  rest = "Any Slot except I"; break;
                case Restrictions.xI:  rest = "Only I";            break;
                case Restrictions.xIO: rest = "Only IO";           break;
                case Restrictions.xO:  rest = "Only O";            break;
            }

            // Concat ship class restrictions
            string shipRest    = "";
            bool specialString = false;
            bool modDestroyers = GlobalStats.ActiveModInfo?.useDestroyers == true;

            if (modDestroyers)
            {
                if (mod.DroneModule && mod.FighterModule && mod.CorvetteModule 
                    && mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule 
                    && mod.BattleshipModule && mod.BattleshipModule && mod.PlatformModule 
                    && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }

            if (GlobalStats.ActiveModInfo == null || !modDestroyers)
            {
                if (mod.FighterModule && mod.CorvetteModule && mod.FrigateModule 
                    && mod.CruiserModule && mod.CruiserModule && mod.CapitalModule 
                    && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                }
            }

            if (!specialString && !mod.DroneModule || (!mod.DestroyerModule && modDestroyers) 
                     || !mod.FighterModule || !mod.CorvetteModule || !mod.FrigateModule 
                     || !mod.CruiserModule || !mod.BattleshipModule  || !mod.CapitalModule 
                     || !mod.PlatformModule || !mod.StationModule || !mod.FreighterModule)
            {
                 if (mod.DroneModule)                         shipRest += "Dr ";
                 if (mod.FighterModule)                       shipRest += "Fi ";
                 if (mod.CorvetteModule)                      shipRest += "Co ";
                 if (mod.FrigateModule)                       shipRest += "Fr ";
                 if (mod.DestroyerModule && modDestroyers)    shipRest += "Dy ";
                 if (mod.CruiserModule)                       shipRest += "Cr ";
                 if (mod.BattleshipModule)                    shipRest += "Bs ";
                 if (mod.CapitalModule)                       shipRest += "Ca ";
                 if (mod.FreighterModule)                     shipRest += "Frt ";
                 if (mod.PlatformModule || mod.StationModule) shipRest += "Orb ";
            }

            batch.DrawString(Fonts.Arial8Bold, Localizer.Token(GameText.Restrictions)+": "+rest, modTitlePos, Color.Orange);
            modTitlePos.Y += Fonts.Arial8Bold.LineSpacing;
            batch.DrawString(Fonts.Arial8Bold, "Hulls: "+shipRest, modTitlePos, Color.LightSteelBlue);
            modTitlePos.Y += (Fonts.Arial8Bold.LineSpacing + 11);
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

                DrawString(batch, ref modTitlePos, sb.ToString(), Fonts.Arial8Bold);

                modTitlePos.Y += (Fonts.Arial8Bold.LineSpacing + 5);
                modTitlePos.X = startx;
            }

            string txt = Fonts.Arial12.ParseText(moduleTemplate.DescriptionText.Text,
                                                 ActiveModSubMenu.Width - 20);

            batch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
            modTitlePos.Y += (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
            float starty = modTitlePos.Y;
            modTitlePos.X = 10;
            float strength = mod.CalculateModuleOffenseDefense(Screen.ActiveHull.ModuleSlots.Length);
            DrawStat(ref modTitlePos, "Offense", strength, GameText.EstimatedOffensiveStrengthOfThe);

            if (mod.BombType == null && !mod.isWeapon || mod.InstalledWeapon == null)
            {
                DrawModuleStats(batch, mod, modTitlePos, starty);
            }
            else
            {
                DrawWeaponStats(batch, modTitlePos, mod, mod.InstalledWeapon, starty);
            }
        }

        void DrawStat(ref Vector2 cursor, LocalizedText text, float stat, LocalizedText toolTipId, bool isPercent = false)
        {
            if (stat.AlmostEqual(0))
                return;

            Screen.DrawStat(ref cursor, text, stat, Color.White, toolTipId, spacing: ActiveModStatSpacing, isPercent: isPercent);
        }

        void DrawStatCustomColor(ref Vector2 cursor, LocalizedText text, float stat, LocalizedText toolTipId, Color color, bool isPercent = true)
        {
            if (stat.AlmostEqual(0))
                return;
            Screen.DrawStat(ref cursor, text, stat, color, toolTipId, spacing: ActiveModStatSpacing, isPercent: isPercent);
        }

        void DrawModuleStats(SpriteBatch batch, ShipModule mod, Vector2 modTitlePos, float starty)
        {
            DrawStat(ref modTitlePos, GameText.Cost, mod.ActualCost, GameText.IndicatesTheProductionCostOf);
            DrawStat(ref modTitlePos, GameText.Mass2, mod.GetActualMass(EmpireManager.Player, 1), GameText.TT_Mass);
            DrawStat(ref modTitlePos, GameText.Health, mod.ActualMaxHealth, GameText.AModulesHealthRepresentsHow);

            float powerDraw = mod.Is(ShipModuleType.PowerPlant) ? mod.ActualPowerFlowMax : -mod.PowerDraw;
            DrawStat(ref modTitlePos, GameText.Power, powerDraw, GameText.IndicatesHowMuchPowerThis);
            DrawStat(ref modTitlePos, GameText.Defense, mod.MechanicalBoardingDefense, GameText.IndicatesTheCombatStrengthAdded);
            DrawStat(ref modTitlePos, Localizer.Token(GameText.Repair)+"+", mod.ActualBonusRepairRate, GameText.IndicatesTheBonusToOutofcombat);

            float maxDepth = modTitlePos.Y;
            modTitlePos.X = modTitlePos.X + 152f;
            modTitlePos.Y = starty;

            DrawStat(ref modTitlePos, GameText.Thrust, mod.thrust, GameText.IndicatesTheAmountOfThrust);
            DrawStat(ref modTitlePos, GameText.Warp, mod.WarpThrust, GameText.IndicatesTheAmountOfThrust2);
            DrawStat(ref modTitlePos, GameText.Turn, mod.TurnThrust, GameText.IndicatesTheAmountOfRotational);


            float shieldMax = mod.ActualShieldPowerMax;
            float amplifyShields = mod.AmplifyShields;
            DrawStat(ref modTitlePos, GameText.ShieldAmp, amplifyShields, GameText.WhenPoweredThisAmplifiesThe);

            if (mod.IsAmplified)
                DrawStatCustomColor(ref modTitlePos, GameText.ShldStr, shieldMax, GameText.IndicatesTheHitpointsOfThis, Color.Gold, isPercent: false);
            else
                DrawStat(ref modTitlePos, GameText.ShldStr, shieldMax, GameText.IndicatesTheHitpointsOfThis);

            DrawStat(ref modTitlePos, GameText.ShldSize, mod.shield_radius, GameText.IndicatesTheProtectiveRadiusOf);
            DrawStat(ref modTitlePos, GameText.Recharge, mod.shield_recharge_rate, GameText.IndicatesTheNumberOfHitpoints);
            DrawStat(ref modTitlePos, GameText.Crecharge, mod.shield_recharge_combat_rate, GameText.ThisShieldCanRechargeEven);

            // Doc: new shield resistances, UI info.
            Color shieldResistColor = Color.LightSkyBlue;
            DrawStatCustomColor(ref modTitlePos, GameText.KineticSr, mod.shield_kinetic_resist, GameText.IndicatesShieldBubblesResistanceTo, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.EnergySr, mod.shield_energy_resist, GameText.IndicatesShieldBubblesResistanceTo2, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.ExplSr, mod.shield_explosive_resist, GameText.IndicatesShieldBubblesResistanceTo3, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.MissileSr, mod.shield_missile_resist, GameText.IndicatesShieldBubblesResistanceTo4, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.FlakSr, mod.shield_flak_resist, GameText.IndicatesShieldBubblesResistanceTo5, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.HybridSr, mod.shield_hybrid_resist, GameText.IndicatesShieldBubblesResistanceTo6, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.RailSr, mod.shield_railgun_resist, GameText.IndicatesShieldBubblesResistanceTo7, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.SubSr, mod.shield_subspace_resist, GameText.IndicatesShieldBubblesResistanceTo8, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.WarpSr, mod.shield_warp_resist, GameText.IndicatesShieldBubblesResistanceTo9, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.BeamSr, mod.shield_beam_resist, GameText.IndicatesShieldBubblesResistanceTo10, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.SdDeflect, mod.shield_threshold, GameText.WeaponsWhichDoLessDamage2, shieldResistColor, isPercent: false);

            DrawStat(ref modTitlePos, GameText.Regenerate, mod.Regenerate, GameText.ThisModuleHasSelfRegeneration);
            DrawStat(ref modTitlePos, GameText.Range,  mod.SensorRange, GameText.IndicatesTheAdditionalSensorRange);
            DrawStat(ref modTitlePos, GameText.Range3, mod.SensorBonus, GameText.IndicatesSensorBonusAddedBy);
            DrawStat(ref modTitlePos, GameText.Heal, mod.HealPerTurn, GameText.IndicatesTheAmountTroopsAre);
            DrawStat(ref modTitlePos, GameText.Range,  mod.TransporterRange, GameText.IndicatesTheRangeOfThis);
            DrawStat(ref modTitlePos, GameText.TransPw, mod.TransporterPower, GameText.IndicatesThePowerUsedBy);
            DrawStat(ref modTitlePos, GameText.Delay, mod.TransporterTimerConstant, GameText.IndicatesTheDelayBetweenTransports);
            DrawStat(ref modTitlePos, GameText.TransOrd, mod.TransporterOrdnance, GameText.IndicatesTheAmountOfOrdnance4);
            DrawStat(ref modTitlePos, GameText.Assault, mod.TransporterTroopAssault, GameText.IndicatesTheNumberOfTroops4);
            DrawStat(ref modTitlePos, GameText.Land, mod.TransporterTroopLanding, GameText.IndicatesTheNumberOfTroops2);
            DrawStat(ref modTitlePos, GameText.Ordnance, mod.OrdinanceCapacity, GameText.IndicatesTheAmountOfOrdnance2);
            DrawStat(ref modTitlePos, GameText.CargoSpace,  mod.Cargo_Capacity, GameText.IndicatesTheTotalCargoSpace);
            DrawStat(ref modTitlePos, GameText.Ordnances, mod.OrdnanceAddedPerSecond, GameText.IndicatesTheAmountOfOrdnance3);
            DrawStat(ref modTitlePos, GameText.Inhibition, mod.InhibitionRadius, GameText.IndicatesTheWarpInhibitionRange);
            DrawStat(ref modTitlePos, GameText.Troops,  mod.TroopCapacity, GameText.IndicatesTheNumberOfTroops3);
            DrawStat(ref modTitlePos, GameText.PowerStore, mod.ActualPowerStoreMax, GameText.IndicatesTheAmountOfPower2);

            // added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
            // FB improved it to use the Power struct
            ShipModule[] modList = { mod };
            Power modNetWarpPowerDraw = Power.Calculate(modList, EmpireManager.Player, true);
            DrawStat(ref modTitlePos, GameText.PowerWarp, -modNetWarpPowerDraw.NetWarpPowerDraw, GameText.TheEffectivePowerDrainOf);

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM)
            {
                DrawStat(ref modTitlePos, GameText.Ecm2, mod.ECM, GameText.IndicatesTheChanceOfEcm, isPercent: true);

            }
            if (mod.ModuleType == ShipModuleType.Hangar)
            {
                DrawStat(ref modTitlePos, GameText.SpawnTimer, mod.hangarTimerConstant, GameText.HangarsAreCapableOfSustaning);
                DrawStat(ref modTitlePos, GameText.HangarSize, mod.MaximumHangarShipSize, GameText.ThisIsTheMaximumNumber);
            }
            if (mod.explodes)
            {
                DrawStatCustomColor(ref modTitlePos, GameText.ExpDmg, mod.ExplosionDamage, GameText.TheDamageCausedToNearby, Color.Red, isPercent: false);
                DrawStatCustomColor(ref modTitlePos, GameText.ExpRad, mod.ExplosionRadius / 16f, GameText.TheDamageRadiusOfThis, Color.Red, isPercent: false);
            }

            DrawStat(ref modTitlePos, GameText.KineticRes, mod.KineticResist, GameText.IndicatesResistanceToKinetictypeDamage, true);
            DrawStat(ref modTitlePos, GameText.EnergyRes, mod.EnergyResist, GameText.IndicatesResistanceToEnergyWeapon,  true);
            DrawStat(ref modTitlePos, GameText.GuidedRes, mod.GuidedResist, GameText.IndicatesResistanceToGuidedWeapon,  true);
            DrawStat(ref modTitlePos, GameText.MissileRes, mod.MissileResist, GameText.IndicatesResistanceToMissileWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.HybridRes, mod.HybridResist, GameText.IndicatesResistanceToHybridWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.BeamRes, mod.BeamResist, GameText.IndicatesResistanceToBeamWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.ExplRes, mod.ExplosiveResist, GameText.IndicatesResistanceToExplosiveDamage, isPercent: true);
            DrawStat(ref modTitlePos, GameText.IntcptRes, mod.InterceptResist, GameText.IndicatesResistanceToInterceptableWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.RailRes, mod.RailgunResist, GameText.IndicatesResistanceToRailgunWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.SpbRes, mod.SpaceBombResist, GameText.IndicatesResistanceToSpatialBomb, isPercent: true);
            DrawStat(ref modTitlePos, GameText.BombRes, mod.BombResist, GameText.IndicatesResistanceToBombardmentWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.BioRes, mod.BioWeaponResist, GameText.IndicatesResistanceToBiologicalWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.DroneRes, mod.DroneResist, GameText.IndicatesResistanceToDroneWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.WarpRes, mod.WarpResist, GameText.IndicatesResistanceToWarpWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.TorpRes, mod.TorpedoResist, GameText.IndicatesResistanceToTorpedoWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.CannonRes, mod.CannonResist, GameText.IndicatesResistanceToCannonWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.SubRes, mod.SubspaceResist, GameText.IndicatesResistanceToSubspaceWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.PdRes, mod.PDResist, GameText.IndicatesResistanceToPdWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.FlakRes, mod.FlakResist, GameText.IndicatesResistanceToFlakWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.ApRes, mod.APResist, GameText.IndicatesResistanceToArmourPiercing);
            DrawStat(ref modTitlePos, GameText.Deflection, mod.DamageThreshold, GameText.WeaponsWhichDoLessDamage);
            DrawStat(ref modTitlePos, GameText.EmpProt, mod.EMP_Protection, GameText.IndicatesTheAmountOfEmp2);
            DrawStat(ref modTitlePos, GameText.FireControl, mod.TargetingAccuracy, GameText.ThisValueRepresentsTheComplexity);
            DrawStat(ref modTitlePos, $"+{Localizer.Token(GameText.FcsPower)}", mod.TargetTracking, GameText.ThisIsABonusTo);

            if (mod.RepairDifficulty.NotZero()) 
                DrawStat(ref modTitlePos, GameText.Complexity, mod.RepairDifficulty, GameText.TheMoreComplexTheModule); // Complexity

            if (mod.numberOfColonists.Greater(0))
                DrawStat(ref modTitlePos, "Colonists", mod.numberOfColonists, GameText.ProsperInTerranWorldsAnd); // Number of Colonists

            if (mod.PermittedHangarRoles.Length == 0)
                return;

            var hangarOption  = ShipBuilder.GetDynamicHangarOptions(mod.hangarShipUID);
            string hangerShip = mod.GetHangarShipName();
            Ship hs = ResourceManager.GetShipTemplate(hangerShip, false);
            if (hs != null)
            {
                Color color   = ShipBuilder.GetHangarTextColor(mod.hangarShipUID);
                modTitlePos.Y = Math.Max(modTitlePos.Y, maxDepth) + Fonts.Arial12Bold.LineSpacing;
                Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y + 5);
                string name = hs.VanityName.IsEmpty() ? hs.Name : hs.VanityName;
                DrawString(batch, ref shipSelectionPos, string.Concat(Localizer.Token(GameText.Fighter), " : ", name), color, Fonts.Arial12Bold);
                shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y-20);
                shipSelectionPos.Y += Fonts.Arial12Bold.LineSpacing * 2;
                DrawStat(ref shipSelectionPos, "Ord. Cost", hs.ShipOrdLaunchCost, "");
                DrawStat(ref shipSelectionPos, "Weapons", hs.Weapons.Count, "");
                DrawStat(ref shipSelectionPos, "Health", hs.HealthMax, "");
                DrawStat(ref shipSelectionPos, "FTL", hs.MaxFTLSpeed, "");

                if (hangarOption != DynamicHangarOptions.Static)
                {
                    modTitlePos.Y = Math.Max(shipSelectionPos.Y, maxDepth) + Fonts.Arial10.LineSpacing + 5;
                    Vector2 bestShipSelectionPos = new Vector2(modTitlePos.X - 145f, modTitlePos.Y);
                    string bestShip = Fonts.Arial10.ParseText(GetDynamicHangarText(hangarOption), ActiveModSubMenu.Width - 20);
                    DrawString(batch, ref bestShipSelectionPos, bestShip, color, Fonts.Arial10);
                }
            }
        }

        string GetDynamicHangarText(DynamicHangarOptions hangarOption)
        {
            switch (hangarOption)
            {
                case DynamicHangarOptions.DynamicLaunch:
                    return "Hangar will launch more advanced ships, as they become available in your empire";
                case DynamicHangarOptions.DynamicInterceptor:
                    return "Hangar will launch more advanced ships which their designated ship category is 'Interceptor', " +
                           "as they become available in your empire. If no Fighters are available, the strongest ship will be launched";
                case DynamicHangarOptions.DynamicAntiShip:
                    return "Hangar will launch more advanced ships which their designated ship category is 'Anti-Ship', " +
                           "as they become available in your empire. If no Fighters are available, the strongest ship will be launched";
                default:
                    return "";
            }
        }

        void DrawWeaponStats(SpriteBatch batch, Vector2 cursor, ShipModule m, Weapon w, float startY)
        {
            Weapon wOrMirv = w; // We want some stats to show warhead stats and not weapon stats
            if (w.MirvWarheads > 0 && w.MirvWeapon.NotEmpty())
            {
                Weapon warhead = ResourceManager.CreateWeapon(w.MirvWeapon);
                wOrMirv        = warhead;
            }


            float range = ModifiedWeaponStat(w, WeaponStat.Range);
            float delay = ModifiedWeaponStat(w, WeaponStat.FireDelay) * GetHullFireRateBonus() + w.DelayedIgnition;
            float speed = ModifiedWeaponStat(w, WeaponStat.Speed);
            
            bool repair = w.isRepairBeam;
            bool isBeam = repair || w.isBeam;
            bool isBallistic = wOrMirv.explodes && wOrMirv.OrdinanceRequiredToFire > 0f;
            float beamMultiplier = isBeam ? w.BeamDuration * (repair ? -60f : +60f) : 0f;

            float rawDamage       = ModifiedWeaponStat(wOrMirv, WeaponStat.Damage) * GetHullDamageBonus();
            float beamDamage      = rawDamage * beamMultiplier;
            float ballisticDamage = rawDamage + rawDamage * EmpireManager.Player.data.OrdnanceEffectivenessBonus;
            float energyDamage    = rawDamage;

            float cost  = m.ActualCost;
            float power = m.ModuleType != ShipModuleType.PowerPlant ? -m.PowerDraw : m.PowerFlowMax;

            DrawStat(ref cursor, GameText.Cost, cost, GameText.IndicatesTheProductionCostOf);
            DrawStat(ref cursor, GameText.Mass2, m.GetActualMass(EmpireManager.Player, 1), GameText.TT_Mass);
            DrawStat(ref cursor, GameText.Health, m.ActualMaxHealth, GameText.AModulesHealthRepresentsHow);
            DrawStat(ref cursor, GameText.Power, power, GameText.IndicatesHowMuchPowerThis);
            DrawStat(ref cursor, GameText.Range, range, GameText.IndicatesTheMaximumRangeOf);
            if (!w.Tag_Guided)
            {
                float accuracy = w.BaseTargetError((int)Screen.DesignedShip.TargetingAccuracy).LowerBound(1) / 16;
                DrawStat(ref cursor, GameText.Accuracy, -1 * accuracy, GameText.WeaponTargetError);
            }
            if (isBeam)
            {
                GameText beamText = repair ? GameText.Repair : GameText.Damage;
                DrawStat(ref cursor, beamText, beamDamage, repair ? GameText.IndicatesTheMaximumAmountOf4 : GameText.IndicatesTheMaximumAmountOf);
                DrawStat(ref cursor, "Duration", w.BeamDuration, GameText.TheDurationABeamWill);
            }
            else
            {
                DrawStat(ref cursor, GameText.Damage, isBallistic ? ballisticDamage : energyDamage, GameText.IndicatesTheMaximumAmountOf);
            }

            if (wOrMirv.explodes)
            {
                DrawStat(ref cursor, "Blast Rad", wOrMirv.DamageRadius / 16, GameText.TheRadiusOfTheProjectiles);
            }

            if (wOrMirv.TerminalPhaseAttack)
            {
                DrawStat(ref cursor, "T.Range", wOrMirv.TerminalPhaseDistance, GameText.ThisMissileHasTerminalPhase);
                DrawStat(ref cursor, "T.Speed", wOrMirv.TerminalPhaseSpeedMod * speed, GameText.ThisIsTheSpeedThe);
            }

            if (w.DelayedIgnition.Greater(0))
                DrawStat(ref cursor, "Ignition", w.DelayedIgnition, GameText.ThisMissileHasDelayedIgnition);

            if (w.MirvWarheads > 0)
                DrawStat(ref cursor, "MIRV", w.MirvWarheads, GameText.ThisWeaponHasMirvMeaning);

            cursor.X += 152f;
            cursor.Y = startY;

            if (!isBeam) DrawStat(ref cursor, GameText.Speed, speed, GameText.IndicatesTheDistanceAProjectile);

            if (rawDamage > 0f)
            {
                int salvos      = w.SalvoCount.LowerBound(1);
                int projectiles = w.ProjectileCount > 0 ? w.ProjectileCount : 1;
                float dps = isBeam ? (beamDamage / delay)
                                   : (salvos / delay) * w.ProjectileCount 
                                                      * (isBallistic ? ballisticDamage : energyDamage) 
                                                      * w.MirvWarheads.LowerBound(1);

                DrawStat(ref cursor, "DPS", dps, GameText.IndicatesTheMaximumDamagePer);
                if (salvos > 1) DrawStat(ref cursor, "Salvo", salvos, GameText.ThisWeaponsFireASalvo);
                if (projectiles > 1) DrawStat(ref cursor, "Projectiles", projectiles, GameText.ThisWeaponFiresMoreThan);
            }

            if (w.FireImprecisionAngle > 0)
                DrawStat(ref cursor, "Imprecision", w.FireImprecisionAngle, GameText.MaximumImprecisionAngleInDegrees);

            DrawStat(ref cursor, "Pwr/s", w.BeamPowerCostPerSecond, GameText.TheAmountOfPowerThis);
            DrawStat(ref cursor, "Delay", delay, GameText.TimeBetweenShots);
            DrawStat(ref cursor, "EMP", w.EMPDamage, GameText.IndicatesTheAmountOfEmp);

            float siphon = w.SiphonDamage + w.SiphonDamage * beamMultiplier;
            DrawStat(ref cursor, "Siphon", siphon, GameText.IndicatesTheAmountOfShields);

            float tractor = w.MassDamage + w.MassDamage * beamMultiplier;
            DrawStat(ref cursor, "Tractor", tractor, GameText.IndicatesTheAmountOfDrag);

            float powerDamage = w.PowerDamage + w.PowerDamage * beamMultiplier;
            DrawStat(ref cursor, "Pwr Dmg", powerDamage, GameText.IndicatesTheAmountOfPower3);
            DrawStat(ref cursor, GameText.FireArc, m.FieldOfFire.ToDegrees(), GameText.AWeaponMayOnlyFire);
            DrawStat(ref cursor, "Ord / Shot", w.OrdinanceRequiredToFire, GameText.IndicatesTheAmountOfOrdnance);
            DrawStat(ref cursor, "Pwr / Shot", w.PowerRequiredToFire, GameText.IndicatesTheAmountOfPower);

            if (w.Tag_Guided && GlobalStats.HasMod && GlobalStats.ActiveModInfo.enableECM)
                DrawStatPercentLine(ref cursor, GameText.EcmResist, w.ECMResist, GameText.IndicatesTheResistanceOfThis);

            DrawResistancePercent(ref cursor, wOrMirv, "VS Armor", WeaponStat.Armor);
            DrawResistancePercent(ref cursor, wOrMirv, "VS Shield", WeaponStat.Shield);
            if (!wOrMirv.TruePD)
            {
                int actualArmorPen = wOrMirv.ArmorPen + (wOrMirv.Tag_Kinetic ? EmpireManager.Player.data.ArmorPiercingBonus : 0);
                if (actualArmorPen > wOrMirv.ArmorPen)
                    DrawStatCustomColor(ref cursor, GameText.ArmorPen, actualArmorPen, GameText.ArmorPenetrationEnablesThisWeapon, Color.Gold, isPercent: false);
                else
                    DrawStat(ref cursor, "Armor Pen", actualArmorPen, GameText.ArmorPenetrationEnablesThisWeapon);

                float actualShieldPenChance = EmpireManager.Player.data.ShieldPenBonusChance + wOrMirv.ShieldPenChance / 100;
                for (int i = 0; i < wOrMirv.ActiveWeaponTags.Length; ++i)
                {
                    CheckShieldPenModifier(wOrMirv.ActiveWeaponTags[i], ref actualShieldPenChance);
                }

                if (actualShieldPenChance.Greater(wOrMirv.ShieldPenChance / 100))
                    DrawStatCustomColor(ref cursor, GameText.ShieldPen, actualShieldPenChance.UpperBound(1), GameText.RandomChanceThisWeaponWill, Color.Gold);
                else
                    DrawStat(ref cursor, "Shield Pen", actualShieldPenChance.UpperBound(100), GameText.RandomChanceThisWeaponWill, isPercent: true);
            }
            DrawStat(ref cursor, GameText.Ordnance, m.OrdinanceCapacity, GameText.IndicatesTheAmountOfOrdnance2);
            DrawStat(ref cursor, GameText.Deflection, m.DamageThreshold, GameText.WeaponsWhichDoLessDamage);
            if (m.RepairDifficulty > 0) DrawStat(ref cursor, GameText.Complexity, m.RepairDifficulty, GameText.TheMoreComplexTheModule); // Complexity

            if (wOrMirv.TruePD)
            {
                WriteLine(ref cursor);
                DrawStringRed(batch, ref cursor, "Cannot Target Ships");
            }
            else if (wOrMirv.Excludes_Fighters || wOrMirv.Excludes_Corvettes || wOrMirv.Excludes_Capitals || wOrMirv.Excludes_Stations)
            {
                WriteLine(ref cursor);
                DrawStringRed(batch, ref cursor, "Cannot Target:", Fonts.Arial8Bold);

                if (wOrMirv.Excludes_Fighters)  WriteLine(batch, ref cursor, "Fighters");
                if (wOrMirv.Excludes_Corvettes) WriteLine(batch, ref cursor, "Corvettes");
                if (wOrMirv.Excludes_Capitals)  WriteLine(batch, ref cursor, "Capitals");
                if (wOrMirv.Excludes_Stations)  WriteLine(batch, ref cursor, "Stations");
            }
        }

        void CheckShieldPenModifier(WeaponTag tag, ref float actualShieldPenChance)
        {
            WeaponTagModifier weaponTag = EmpireManager.Player.data.WeaponTags[tag];
            actualShieldPenChance += weaponTag.ShieldPenetration;
        }

        void DrawStatPercentLine(ref Vector2 cursor, GameText text, float stat, LocalizedText tooltipId)
        {
            DrawStat(ref cursor, text, stat, tooltipId, isPercent: true);
            WriteLine(ref cursor);
        }

        void WriteLine(SpriteBatch batch, ref Vector2 cursor, string text)
        {
            batch.DrawString(Fonts.Arial8Bold, text, cursor, Color.Wheat);
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
            if (effect.NotEqual(1))
                Screen.DrawStatBadPercentLower1(ref cursor, description, effect, Color.White, GameText.IndicatesAnyBonusOrPenalty, ActiveModStatSpacing);
        }

        float GetHullDamageBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(Screen.ActiveHull.Hull, out HullBonus bonus))
                return 1f + bonus.DamageBonus;
            return 1f;
        }

        float GetHullFireRateBonus()
        {
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.UseHullBonuses &&
                ResourceManager.HullBonuses.TryGetValue(Screen.ActiveHull.Hull, out HullBonus bonus))
                return 1f - bonus.FireRateBonus;
            return 1f;
        }
    }
}
