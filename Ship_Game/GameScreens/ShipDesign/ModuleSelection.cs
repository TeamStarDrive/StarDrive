using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Text;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Universe;
using Ship_Game.UI;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public class ModuleSelection : Submenu
    {
        readonly ShipDesignScreen Screen;
        UniverseState Universe => Screen.ParentUniverse.UState;
        Empire Player => Screen.Player;

        readonly FighterScrollList ChooseFighterSL;
        readonly SubmenuScrollList<FighterListItem> ChooseFighterSub;
        readonly ModuleSelectScrollList ModuleSelectList;
        readonly Submenu ActiveModSubMenu;
        readonly TexturedButton Obsolete;

        public ModuleSelection(ShipDesignScreen screen, LocalPos pos, Vector2 size)
            : base(pos, size, new LocalizedText[]{ "Wpn", "Pwr", "Def", "Spc" })
        {
            Screen = screen;
            // rounded black background
            SetBackground(Colors.TransparentBlackFill);
            base.PerformLayout(); // necessary

            ModuleSelectList = base.Add(new ModuleSelectScrollList(this, Screen));

            RectF acsub = new(Rect.X, Rect.Bottom + 15, 305, 400);
            ActiveModSubMenu = base.Add(new Submenu(acsub, "Active Module"));
            // rounded black background
            ActiveModSubMenu.SetBackground(Colors.TransparentBlackFill);

            // obsolete button
            int obsoleteW = ResourceManager.Texture("NewUI/icon_queue_delete").Width;
            int obsoleteH = ResourceManager.Texture("NewUI/icon_queue_delete").Height;
            var obsoletePos = new RectF(ActiveModSubMenu.X + ActiveModSubMenu.Width - obsoleteW - 10, ActiveModSubMenu.Y + 38, obsoleteW, obsoleteH);
            Obsolete = new(obsoletePos, "NewUI/icon_queue_delete", "NewUI/icon_queue_delete_hover1", "NewUI/icon_queue_delete_hover2");
            Obsolete.Tooltip = GameText.MarkThisModuleAsObsolete;
            
            RectF fighterR = acsub.Move(acsub.W + 20, 0);
            ChooseFighterSub = base.Add(new SubmenuScrollList<FighterListItem>(fighterR, "Choose Fighter"));
            ChooseFighterSub.SetBackground(Colors.TransparentBlackFill);
            
            ChooseFighterSL = ChooseFighterSub.Add(new FighterScrollList(ChooseFighterSub, Screen)
            {
                EnableItemHighlight = true
            });
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
            return base.HitTest(input.CursorPosition) || ChooseFighterSL.HitTest(input);
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
                    if (!m.IsObsolete(Player))
                        Player.ObsoletePlayerShipModules.Add(m.UID);
                    else
                        Player.ObsoletePlayerShipModules.Remove(m.UID);

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
            ChooseFighterSub.Visible = ChooseFighterSL.GetFighterHangar() != null;

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

        // Gets the tech cost of the tech which unlocks the module provided, this is for modders in debug
        float DebugGetModuleTechCost(ShipModule module)
        {
            foreach (TechEntry tech in Player.TechEntries)
            {
                if (tech.GetUnlockableModules(Player).Any(m => m.ModuleUID == module.UID))
                    return tech.TechCost;
            }

            return 0;
        }

        void DrawActiveModuleData(SpriteBatch batch)
        {
            ShipModule mod = Screen.ActiveModule ?? Screen.HighlightedModule;

            if (ActiveModSubMenu.SelectedIndex != 0 || mod == null)
                return;

            bool isObsolete = mod.IsObsolete(Player);
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

            if (Screen.ParentUniverse.Debug)
            {
                batch.DrawString(Fonts.Arial12, $"Debug Tech Cost: {DebugGetModuleTechCost(mod).String(1)}", modTitlePos, Color.Gold);
                modTitlePos.Y += (Fonts.Arial12.LineSpacing + 4);
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
            string shipRest = "";
            bool specialString = false;
            bool destroyers = GlobalStats.Defaults.UseDestroyers;

            if (destroyers)
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

            if (!destroyers)
            {
                if (mod.FighterModule && mod.CorvetteModule && mod.FrigateModule 
                    && mod.CruiserModule && mod.CruiserModule && mod.CapitalModule 
                    && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                }
            }

            if (!specialString && !mod.DroneModule || (!mod.DestroyerModule && destroyers) 
                     || !mod.FighterModule || !mod.CorvetteModule || !mod.FrigateModule 
                     || !mod.CruiserModule || !mod.BattleshipModule  || !mod.CapitalModule 
                     || !mod.PlatformModule || !mod.StationModule || !mod.FreighterModule)
            {
                 if (mod.DroneModule)                         shipRest += "Dr ";
                 if (mod.FighterModule)                       shipRest += "Fi ";
                 if (mod.CorvetteModule)                      shipRest += "Co ";
                 if (mod.FrigateModule)                       shipRest += "Fr ";
                 if (mod.DestroyerModule && destroyers)       shipRest += "Dy ";
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
            if (moduleTemplate.IsWeapon)
            {
                var sb = new StringBuilder();
                var t = ResourceManager.GetWeaponTemplate(moduleTemplate.WeaponType);
                if (t.Tag_Guided)    sb.Append("GUIDED ");
                if (t.Tag_Intercept) sb.Append("INTERCEPTABLE ");
                if (t.Tag_Energy)    sb.Append("ENERGY ");
                if (t.Tag_Plasma)    sb.Append("PLASMA ");
                if (t.Tag_Kinetic)   sb.Append("KINETIC ");
                if (t.Explodes)      sb.Append("EXPLOSIVE ");
                if (t.Tag_PD)        sb.Append("POINT DEFENSE ");
                if (t.Tag_Missile)   sb.Append("MISSILE ");
                if (t.Tag_Beam)      sb.Append("BEAM ");
                if (t.Tag_Torpedo)   sb.Append("TORPEDO ");
                if (t.Tag_Bomb)      sb.Append("BOMB ");
                if (t.Tag_BioWeapon) sb.Append("BIOWEAPON ");
                if (t.Tag_SpaceBomb) sb.Append("SPACEBOMB ");
                if (t.Tag_Drone)     sb.Append("DRONE ");
                if (t.Tag_Cannon)    sb.Append("CANNON ");

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

            float strength = mod.CalculateModuleOffenseDefense(Screen.CurrentHull.SurfaceArea, forceRecalculate: mod.IsFighterHangar);
            DrawStat(ref modTitlePos, "Offense", strength, GameText.TT_ShipOffense);

            if (mod.BombType == null && !mod.IsWeapon || mod.InstalledWeapon == null)
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
            DrawStat(ref modTitlePos, GameText.Cost, mod.ActualCost(Universe), GameText.IndicatesTheProductionCostOf);
            DrawStat(ref modTitlePos, GameText.Mass2, mod.GetActualMass(Player, 1), GameText.TT_Mass);
            DrawStat(ref modTitlePos, GameText.Health, mod.ActualMaxHealth, GameText.AModulesHealthRepresentsHow);

            float powerDraw = mod.ActualPowerFlowMax - mod.PowerDraw;
            DrawStat(ref modTitlePos, GameText.Power, powerDraw, GameText.IndicatesHowMuchPowerThis);
            DrawStat(ref modTitlePos, GameText.Defense, mod.MechanicalBoardingDefense, GameText.IndicatesTheCombatStrengthAdded);
            DrawStat(ref modTitlePos, Localizer.Token(GameText.Repair)+"+", mod.ActualBonusRepairRate, GameText.IndicatesTheBonusToOutofcombat);

            float maxDepth = modTitlePos.Y;
            modTitlePos.X = modTitlePos.X + 152f;
            modTitlePos.Y = starty;

            DrawStat(ref modTitlePos, GameText.Thrust, mod.Thrust, GameText.IndicatesTheAmountOfThrust);
            DrawStat(ref modTitlePos, GameText.Warp, mod.WarpThrust, GameText.IndicatesTheAmountOfThrust2);
            DrawStat(ref modTitlePos, GameText.Turn, mod.TurnThrust, GameText.IndicatesTheAmountOfRotational);


            float shieldMax = mod.ActualShieldPowerMax;
            float amplifyShields = mod.AmplifyShields;
            DrawStat(ref modTitlePos, GameText.ShieldAmp, amplifyShields, GameText.WhenPoweredThisAmplifiesThe);

            if (mod.IsAmplified)
                DrawStatCustomColor(ref modTitlePos, GameText.ShldStr, shieldMax, GameText.IndicatesTheHitpointsOfThis, Color.Gold, isPercent: false);
            else
                DrawStat(ref modTitlePos, GameText.ShldStr, shieldMax, GameText.IndicatesTheHitpointsOfThis);

            DrawStat(ref modTitlePos, GameText.ShldSize, mod.ShieldRadius, GameText.IndicatesTheProtectiveRadiusOf);
            DrawStat(ref modTitlePos, GameText.Recharge, mod.ShieldRechargeRate, GameText.IndicatesTheNumberOfHitpoints);
            DrawStat(ref modTitlePos, GameText.Crecharge, mod.ShieldRechargeCombatRate, GameText.ThisShieldCanRechargeEven);

            // Doc: new shield resistances, UI info.
            Color shieldResistColor = Color.LightSkyBlue;
            DrawStatCustomColor(ref modTitlePos, GameText.KineticSr, mod.ShieldKineticResist, GameText.IndicatesShieldBubblesResistanceTo, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.EnergySr, mod.ShieldEnergyResist, GameText.IndicatesShieldBubblesResistanceTo2, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.ExplSr, mod.ShieldExplosiveResist, GameText.IndicatesShieldBubblesResistanceTo3, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.MissileSr, mod.ShieldMissileResist, GameText.IndicatesShieldBubblesResistanceTo4, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.HybridSr, mod.ShieldPlasmaResist, GameText.IndicatesShieldBubblesResistanceTo6, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.BeamSr, mod.ShieldBeamResist, GameText.IndicatesShieldBubblesResistanceTo10, shieldResistColor);
            DrawStatCustomColor(ref modTitlePos, GameText.SdDeflect, mod.ShieldDeflection, GameText.WeaponsWhichDoLessDamage2, shieldResistColor, isPercent: false);

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
            DrawStat(ref modTitlePos, GameText.CargoSpace,  mod.CargoCapacity, GameText.TT_CargoSpace);
            DrawStat(ref modTitlePos, GameText.ResearchPerTurnModule, mod.ResearchPerTurn, GameText.ResearchPerTurnStatTip);
            DrawStat(ref modTitlePos, GameText.RefiningModule, mod.Refining, GameText.RefiningPerTurnStatTip);
            DrawStat(ref modTitlePos, GameText.Ordnances, mod.OrdnanceAddedPerSecond, GameText.TT_OrdnanceCreated);
            DrawStat(ref modTitlePos, GameText.Inhibition, mod.InhibitionRadius, GameText.IndicatesTheWarpInhibitionRange);
            DrawStat(ref modTitlePos, GameText.Troops,  mod.TroopCapacity, GameText.IndicatesTheNumberOfTroops3);
            DrawStat(ref modTitlePos, GameText.PowerStore, mod.ActualPowerStoreMax, GameText.IndicatesTheAmountOfPower2);

            // added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
            // FB improved it to use the Power struct
            ShipModule[] modList = { mod };
            Power modNetWarpPowerDraw = Power.Calculate(modList, Player, true);
            DrawStat(ref modTitlePos, GameText.PowerWarp, -modNetWarpPowerDraw.NetWarpPowerDraw, GameText.TheEffectivePowerDrainOf);

            if (GlobalStats.Defaults.EnableECM)
            {
                DrawStat(ref modTitlePos, GameText.Ecm2, mod.ECM, GameText.IndicatesTheChanceOfEcm, isPercent: true);
            }
            if (mod.ModuleType == ShipModuleType.Hangar)
            {
                DrawStat(ref modTitlePos, GameText.SpawnTimer, mod.HangarTimerConstant, GameText.HangarsAreCapableOfSustaning);
                DrawStat(ref modTitlePos, GameText.HangarSize, mod.MaximumHangarShipSize, GameText.ThisIsTheMaximumNumber);
            }
            if (mod.Explodes)
            {
                DrawStatCustomColor(ref modTitlePos, GameText.ExpDmg, mod.ExplosionDamage, GameText.TheDamageCausedToNearby, Color.Red, isPercent: false);
                DrawStatCustomColor(ref modTitlePos, GameText.ExpRad, mod.ExplosionRadius / 16f, GameText.TheDamageRadiusOfThis, Color.Red, isPercent: false);
            }

            DrawStat(ref modTitlePos, GameText.KineticRes, mod.KineticResist, GameText.IndicatesResistanceToKinetictypeDamage, true);
            DrawStat(ref modTitlePos, GameText.EnergyRes, mod.EnergyResist, GameText.IndicatesResistanceToEnergyWeapon,  true);
            DrawStat(ref modTitlePos, GameText.MissileRes, mod.MissileResist, GameText.IndicatesResistanceToMissileWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.HybridRes, mod.PlasmaResist, GameText.IndicatesResistanceToHybridWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.BeamRes, mod.BeamResist, GameText.IndicatesResistanceToBeamWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.ExplRes, mod.ExplosiveResist, GameText.IndicatesResistanceToExplosiveDamage, isPercent: true);
            DrawStat(ref modTitlePos, GameText.TorpRes, mod.TorpedoResist, GameText.IndicatesResistanceToTorpedoWeapon, isPercent: true);
            DrawStat(ref modTitlePos, GameText.ApRes, mod.APResist, GameText.IndicatesResistanceToArmourPiercing);
            DrawStat(ref modTitlePos, GameText.Deflection, mod.Deflection, GameText.WeaponsWhichDoLessDamage);
            DrawStat(ref modTitlePos, GameText.EmpProt, mod.EMPProtection, GameText.IndicatesTheAmountOfEmp2);
            DrawStat(ref modTitlePos, GameText.FireControl, mod.TargetingAccuracy, GameText.ThisValueRepresentsTheComplexity);
            DrawStat(ref modTitlePos, $"+{Localizer.Token(GameText.FcsPower)}", mod.TargetTracking, GameText.ThisIsABonusTo);

            if (mod.RepairDifficulty.NotZero()) 
                DrawStat(ref modTitlePos, GameText.Complexity, mod.RepairDifficulty, GameText.TheMoreComplexTheModule); // Complexity

            if (mod.NumberOfColonists.Greater(0))
                DrawStat(ref modTitlePos, "Colonists", mod.NumberOfColonists, GameText.ProsperInTerranWorldsAnd); // Number of Colonists

            if (mod.PermittedHangarRoles.Length == 0 && !mod.IsSupplyBay && !mod.IsTroopBay)
                return;

            var hangarOption  = ShipBuilder.GetDynamicHangarOptions(mod.HangarShipUID);
            string hangarShip = mod.GetHangarShipName(Player);
            Ship hs = ResourceManager.GetShipTemplate(hangarShip, false);
            if (hs != null)
            {
                Color color   = ShipBuilder.GetHangarTextColor(mod.HangarShipUID);
                modTitlePos.Y = Math.Max(modTitlePos.Y, maxDepth) + Fonts.Arial12Bold.LineSpacing;
                Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y + 5);
                string name = hs.VanityName.IsEmpty() ? hs.Name : hs.VanityName;
                DrawString(batch, ref shipSelectionPos, string.Concat(hs.DesignRole.ToString().ToUpper(), " : ", name), color, Fonts.Arial12Bold);
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
            IWeaponTemplate wOrMirv = w.T; // We want some stats to show warhead stats and not weapon stats
            if (wOrMirv.IsMirv)
            {
                wOrMirv = ResourceManager.GetWeaponTemplate(w.MirvWeapon);
            }

            float range = ModifiedWeaponStat(w, WeaponStat.Range);
            float delay = ModifiedWeaponStat(w, WeaponStat.FireDelay) * GetHullFireRateBonus() + w.DelayedIgnition;
            float speed = ModifiedWeaponStat(w, WeaponStat.Speed);
            
            bool repair = w.IsRepairBeam;
            bool isBeam = repair || w.IsBeam;
            bool isBallistic = wOrMirv.Explodes && wOrMirv.OrdinanceRequiredToFire > 0f;
            float beamMultiplier = isBeam ? w.BeamDuration * (repair ? -60f : +60f) : 0f;

            float rawDamage       = ModifiedWeaponStat(wOrMirv, WeaponStat.Damage) * GetHullDamageBonus();
            float beamDamage      = rawDamage * beamMultiplier;
            float ballisticDamage = rawDamage + rawDamage * Player.data.OrdnanceEffectivenessBonus;
            float energyDamage    = rawDamage;

            float cost = m.ActualCost(Universe);
            float power = m.ModuleType != ShipModuleType.PowerPlant ? -m.PowerDraw : m.PowerFlowMax;

            DrawStat(ref cursor, GameText.Cost, cost, GameText.IndicatesTheProductionCostOf);
            DrawStat(ref cursor, GameText.Mass2, m.GetActualMass(Player, 1), GameText.TT_Mass);
            DrawStat(ref cursor, GameText.Health, m.ActualMaxHealth, GameText.AModulesHealthRepresentsHow);
            DrawStat(ref cursor, GameText.Power, power, GameText.IndicatesHowMuchPowerThis);
            DrawStat(ref cursor, GameText.Range, range, GameText.IndicatesTheMaximumRangeOf);
            if (!w.Tag_Guided)
            {
                float accuracy = w.BaseTargetError(Screen.DesignedShip.TargetingAccuracy);
                accuracy       = accuracy > 0 ? accuracy.LowerBound(1) / 16 : 0;
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

            if (wOrMirv.Explodes)
            {
                DrawStat(ref cursor, "Blast Rad", wOrMirv.ExplosionRadius / 16, GameText.TheRadiusOfTheProjectiles);
            }

            if (wOrMirv.TerminalPhaseAttack)
            {
                DrawStat(ref cursor, "T.Range", wOrMirv.TerminalPhaseDistance, GameText.ThisMissileHasTerminalPhase);
                DrawStat(ref cursor, "T.Speed", wOrMirv.TerminalPhaseSpeedMod * speed, GameText.ThisIsTheSpeedThe);
            }

            if (w.DelayedIgnition.Greater(0))
                DrawStat(ref cursor, "Ignition", w.DelayedIgnition, GameText.ThisMissileHasDelayedIgnition);

            if (wOrMirv.ProjectileCount > 1 && w.IsMirv)
                DrawStat(ref cursor, "MIRV", wOrMirv.ProjectileCount, GameText.ThisWeaponHasMirvMeaning);

            cursor.X += 152f;
            cursor.Y = startY;

            if (!isBeam) DrawStat(ref cursor, GameText.Speed, speed, GameText.IndicatesTheDistanceAProjectile);

            if (rawDamage > 0f)
            {
                int salvos      = w.SalvoCount.LowerBound(1);
                int projectiles = w.ProjectileCount > 0 ? w.ProjectileCount : 1;
                float dps = isBeam ? (beamDamage / delay)
                                   : (salvos / delay) * w.ProjectileCount 
                                                      * (isBallistic ? ballisticDamage : energyDamage);

                if (wOrMirv.ProjectileCount > 1 && w.IsMirv)
                    dps *= wOrMirv.ProjectileCount;

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

            float tractor = w.TractorDamage + w.TractorDamage * beamMultiplier;
            DrawStat(ref cursor, "Tractor", tractor, GameText.IndicatesTheAmountOfDrag);

            float powerDamage = w.PowerDamage + w.PowerDamage * beamMultiplier;
            DrawStat(ref cursor, "Pwr Dmg", powerDamage, GameText.IndicatesTheAmountOfPower3);
            DrawStat(ref cursor, GameText.FireArc, m.FieldOfFire.ToDegrees(), GameText.AWeaponMayOnlyFire);
            DrawStat(ref cursor, "Ord / Shot", w.OrdinanceRequiredToFire, GameText.IndicatesTheAmountOfOrdnance);
            DrawStat(ref cursor, "Pwr / Shot", w.PowerRequiredToFire, GameText.IndicatesTheAmountOfPower);

            if (w.Tag_Guided && GlobalStats.Defaults.EnableECM)
                DrawStatPercentLine(ref cursor, GameText.EcmResist, w.ECMResist, GameText.IndicatesTheResistanceOfThis);

            DrawResistancePercent(ref cursor, wOrMirv, "VS Armor", WeaponStat.Armor);
            DrawResistancePercent(ref cursor, wOrMirv, "VS Shield", WeaponStat.Shield);
            if (!wOrMirv.TruePD)
            {
                int actualArmorPen = wOrMirv.ArmorPen + (wOrMirv.Tag_Kinetic ? Player.data.ArmorPiercingBonus : 0);
                if (actualArmorPen > wOrMirv.ArmorPen)
                    DrawStatCustomColor(ref cursor, GameText.ArmorPen, actualArmorPen, GameText.ArmorPenetrationEnablesThisWeapon, Color.Gold, isPercent: false);
                else
                    DrawStat(ref cursor, "Armor Pen", actualArmorPen, GameText.ArmorPenetrationEnablesThisWeapon);

                float actualShieldPenChance = Player.data.ShieldPenBonusChance + wOrMirv.ShieldPenChance / 100;
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
            DrawStat(ref cursor, GameText.Deflection, m.Deflection, GameText.WeaponsWhichDoLessDamage);
            if (m.RepairDifficulty > 0) DrawStat(ref cursor, GameText.Complexity, m.RepairDifficulty, GameText.TheMoreComplexTheModule); // Complexity

            if (wOrMirv.TruePD)
            {
                WriteLine(ref cursor);
                DrawStringRed(batch, ref cursor, "Cannot Target Ships");
            }
            else if (wOrMirv.ExcludesFighters || wOrMirv.ExcludesCorvettes || wOrMirv.ExcludesCapitals || wOrMirv.ExcludesStations)
            {
                WriteLine(ref cursor);
                DrawStringRed(batch, ref cursor, "Cannot Target:", Fonts.Arial8Bold);

                if (wOrMirv.ExcludesFighters)  WriteLine(batch, ref cursor, "Fighters");
                if (wOrMirv.ExcludesCorvettes) WriteLine(batch, ref cursor, "Corvettes");
                if (wOrMirv.ExcludesCapitals)  WriteLine(batch, ref cursor, "Capitals");
                if (wOrMirv.ExcludesStations)  WriteLine(batch, ref cursor, "Stations");
            }
        }

        void CheckShieldPenModifier(WeaponTag tag, ref float actualShieldPenChance)
        {
            WeaponTagModifier weaponTag = Player.data.WeaponTags[tag];
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

        static float GetStatForWeapon(WeaponStat stat, IWeaponTemplate weapon)
        {
            switch (stat)
            {
                case WeaponStat.Damage:    return weapon.DamageAmount;
                case WeaponStat.Range:     return weapon.BaseRange;
                case WeaponStat.Speed:     return weapon.ProjectileSpeed;
                case WeaponStat.FireDelay: return weapon.NetFireDelay;
                case WeaponStat.Armor:     return weapon.EffectVsArmor;
                case WeaponStat.Shield:    return weapon.EffectVsShields;
                default: return 0f;
            }
        }

        float ModifiedWeaponStat(IWeaponTemplate weapon, WeaponStat stat)
        {
            float value = GetStatForWeapon(stat, weapon);
            foreach (WeaponTag tag in weapon.ActiveWeaponTags)
                value += value * Player.data.GetStatBonusForWeaponTag(stat, tag);
            return value;
        }

        void DrawResistancePercent(ref Vector2 cursor, IWeaponTemplate weapon, string description, WeaponStat stat)
        {
            float effect = ModifiedWeaponStat(weapon, stat);
            if (effect.NotEqual(1))
                Screen.DrawStatBadPercentLower1(ref cursor, description, effect, Color.White, GameText.IndicatesAnyBonusOrPenalty, ActiveModStatSpacing);
        }

        float GetHullDamageBonus()
        {
            if (GlobalStats.Defaults.UseHullBonuses)
                return 1f + Screen.CurrentHull.Bonuses.DamageBonus;
            return 1f;
        }

        float GetHullFireRateBonus()
        {
            if (GlobalStats.Defaults.UseHullBonuses)
                return 1f - Screen.CurrentHull.Bonuses.FireRateBonus;
            return 1f;
        }
    }
}
