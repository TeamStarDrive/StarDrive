using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    using static ShipMaintenance;

    public sealed partial class ShipDesignScreen // refactored by Fat Bastard
    {
        public override void Draw(SpriteBatch batch) 
        {
            GameTime gameTime = Game1.Instance.GameTime;
            ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);

            Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.starfield);
            ScreenManager.RenderSceneObjects();

            if (ToggleOverlay)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
                DrawEmptySlots(batch);
                DrawModules(batch);
                DrawTacticalData(batch);
                DrawUnpoweredTex(batch);
                batch.End();
            }

            batch.Begin();
            if (ActiveModule != null && !ModSel.HitTest(Input))
                DrawActiveModule(batch);

            DrawUi();
            selector?.Draw(batch);
            ArcsButton.DrawWithShadowCaps(ScreenManager);
            if (Debug)
                DrawDebug();

            base.Draw(batch);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        private void DrawEmptySlots(SpriteBatch spriteBatch)
        {
            Texture2D concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1");

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.Module != null)
                {
                    slot.Draw(spriteBatch, concreteGlass, Color.Gray);
                    continue;
                }

                if (slot.Root.Module != null)
                    continue;

                bool valid        = ActiveModule == null || slot.CanSlotSupportModule(ActiveModule);
                Color activeColor = valid ? Color.LightGreen : Color.Red;
                slot.Draw(spriteBatch, concreteGlass, activeColor);
                if (slot.InPowerRadius)
                {
                    Color yellow = ActiveModule != null ? new Color(Color.Yellow, 150) : Color.Yellow;
                    slot.Draw(spriteBatch, concreteGlass, yellow);
                }
                spriteBatch.DrawString(Fonts.Arial20Bold, " " + slot.Restrictions, slot.PosVec2, Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
            }
        }

        private void DrawModules(SpriteBatch spriteBatch)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.ModuleUID == null || slot.Tex == null)
                    continue;

                DrawModuleTex(slot.Orientation, spriteBatch, slot, slot.ModuleRect);

                if (slot.Module != HoveredModule)
                {
                    if (!Input.LeftMouseHeld() || !Input.IsAltKeyDown || slot.Module.ModuleType != ShipModuleType.Turret
                            || (HighlightedModule?.Facing.AlmostEqual(slot.Module.Facing) ?? false))
                        continue;
                }
                spriteBatch.DrawRectangle(slot.ModuleRect, Color.White, 2f);
            }
        }

        private static void DrawModuleTex(ModuleOrientation orientation, SpriteBatch spriteBatch, SlotStruct slot, Rectangle r, ShipModule template = null) 
        {
            SpriteEffects effects = SpriteEffects.None;
            float rotation        = 0f;
            Texture2D texture     = template == null ? slot.Tex : ResourceManager.Texture(template.IconTexturePath);
            int xSize             = template == null ? slot.Module.XSIZE * 16 : r.Width;
            int ySize             = template == null ? slot.Module.YSIZE * 16 : r.Height;

            switch (orientation)
            {
                case ModuleOrientation.Left:
                {
                    int w    = xSize;
                    int h    = ySize;
                    r.Width  = h; // swap width & height
                    r.Height = w;
                    rotation = -1.57079637f;
                    r.Y     += h;
                    break;
                }
                case ModuleOrientation.Right:
                {
                    int w    = ySize;
                    int h    = xSize;
                    r.Width  = w;
                    r.Height = h;
                    rotation = 1.57079637f;
                    r.X     += h;
                    break;
                }
                case ModuleOrientation.Rear:
                {
                    effects = SpriteEffects.FlipVertically;
                    break;
                }
                case ModuleOrientation.Normal:
                {
                    if (slot?.SlotReference.Position.X > 256f
                        && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        effects = SpriteEffects.FlipHorizontally;
                    break;
                }
            }
            spriteBatch.Draw(texture, r, null, Color.White, rotation, Vector2.Zero, effects, 1f);
        }

        private void DrawTacticalData(SpriteBatch spriteBatch)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.ModuleUID  == null 
                    || slot.Tex     == null 
                    || slot.Module  != HighlightedModule 
                    && !ShowAllArcs)
                     continue;

                Vector2 center = slot.Center();
                var mirrored = new MirrorSlot();
                if (IsSymmetricDesignMode)
                    mirrored = GetMirrorSlot(slot, slot.Module.XSIZE, slot.Orientation);

                if (slot.Module.shield_power_max > 0f)
                    DrawShieldRadius(center, slot, spriteBatch, mirrored);

                if (slot.Module.ModuleType == ShipModuleType.Turret && Input.LeftMouseHeld())
                    DrawFireArcText(center, slot, mirrored);

                if (slot.Module.ModuleType == ShipModuleType.Hangar)
                    DrawHangarShipText(center, slot, mirrored);

                DrawWeaponArcs(center, slot, spriteBatch, mirrored);
            }
        }

        private void DrawUnpoweredTex(SpriteBatch spriteBatch)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.Module == null)
                    continue;

                if (slot.Module == HighlightedModule && Input.LeftMouseHeld() && slot.Module.ModuleType == ShipModuleType.Turret)
                    continue;

                Vector2 lightOrigin = new Vector2(8f, 8f);
                if (slot.Module.PowerDraw <= 0f 
                    || slot.Module.Powered 
                    || slot.Module.ModuleType == ShipModuleType.PowerConduit)
                        continue;

                spriteBatch.Draw(ResourceManager.Texture("UI/lightningBolt"),
                    slot.Center(), null, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
            }
        }

        private void DrawShieldRadius(Vector2 center, SlotStruct slot, SpriteBatch spriteBatch, MirrorSlot mirrored)
        {
            spriteBatch.DrawCircle(center, slot.Module.ShieldHitRadius, Color.LightGreen);
            if (!IsSymmetricDesignMode || !IsMirrorSlotValid(slot, mirrored))
                return;

            spriteBatch.DrawCircle(mirrored.Center, mirrored.Slot.Module.ShieldHitRadius, Color.LightGreen);
        }

        private void DrawFireArcText(Vector2 center, SlotStruct slot, MirrorSlot mirrored)
        {
            Color color = Color.Black;
            color.A     = 140;

            DrawRectangle(slot.ModuleRect, Color.White, color);
            DrawString(center, 0, 1, Color.Orange, slot.Module.Facing.ToString(CultureInfo.CurrentCulture));
            if (!IsSymmetricDesignMode || !IsMirrorSlotValid(slot, mirrored))
                return;

            DrawRectangle(mirrored.Slot.ModuleRect, Color.White, color);
            DrawString(mirrored.Center, 0, 1, Color.Orange, mirrored.Slot.Module.Facing.ToString(CultureInfo.CurrentCulture));

            ToolTip.ShipYardArcTip();
        }

        private void DrawHangarShipText(Vector2 center, SlotStruct slot, MirrorSlot mirrored)
        {
            Color color = Color.Black;
            color.A     = 140;

            Color shipNameColor = ShipBuilder.IsDynamicLaunch(slot.Module.hangarShipUID) ? Color.Gold : Color.White;
            DrawRectangle(slot.ModuleRect, Color.Teal, color);
            DrawString(center, 0, 0.4f, shipNameColor, slot.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
            if (!IsSymmetricDesignMode || !IsMirrorSlotValid(slot, mirrored))
                return;

            shipNameColor = ShipBuilder.IsDynamicLaunch(slot.Module.hangarShipUID) ? Color.Gold : Color.White;
            DrawRectangle(mirrored.Slot.ModuleRect, Color.Teal, color);
            DrawString(mirrored.Center, 0, 0.4f, shipNameColor, mirrored.Slot.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
        }

        private void DrawArc(Vector2 center, SlotStruct slot, Color drawcolor, SpriteBatch spriteBatch, MirrorSlot mirrored)
        {
            Texture2D arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);
            var origin       = new Vector2(250f, 250f);
            Rectangle toDraw = center.ToRect(500, 500);

            spriteBatch.Draw(arcTexture, toDraw, null, drawcolor, slot.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);
            if (!IsSymmetricDesignMode || !IsMirrorSlotValid(slot, mirrored))
                return;

            Rectangle mirrorRect = mirrored.Center.ToRect(500, 500);
            spriteBatch.Draw(arcTexture, mirrorRect, null, drawcolor, mirrored.Slot.Root.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);
        }

        private void DrawWeaponArcs(Vector2 center, SlotStruct slot, SpriteBatch spriteBatch, MirrorSlot mirrored)
        {
            Weapon w = slot.Module.InstalledWeapon;
            if (w == null)
                return;
            if (w.Tag_Cannon && !w.Tag_Energy)        DrawArc(center, slot, new Color(255, 255, 0, 255), spriteBatch, mirrored);
            else if (w.Tag_Railgun || w.Tag_Subspace) DrawArc(center, slot, new Color(255, 0, 255, 255), spriteBatch, mirrored);
            else if (w.Tag_Cannon)                    DrawArc(center, slot, new Color(0, 255, 0, 255), spriteBatch, mirrored);
            else if (!w.isBeam)                       DrawArc(center, slot, new Color(255, 0, 0, 255), spriteBatch, mirrored);
            else                                      DrawArc(center, slot, new Color(0, 0, 255, 255), spriteBatch, mirrored);
        }

        private void DrawActiveModule(SpriteBatch spriteBatch)
        {
            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            var r = new Rectangle((int)Input.CursorPosition.X, (int)Input.CursorPosition.Y,
                          (int)(16 * ActiveModule.XSIZE * Camera.Zoom),
                          (int)(16 * ActiveModule.YSIZE * Camera.Zoom));
            DrawModuleTex(ActiveModState, spriteBatch, null, r, moduleTemplate);
            if (!(ActiveModule.shield_power_max > 0f))
                return;

            var center = new Vector2(Input.CursorPosition.X, Input.CursorPosition.Y);
            if (ActiveModState == ModuleOrientation.Normal || ActiveModState == ModuleOrientation.Rear)
                center += new Vector2(moduleTemplate.XSIZE * 16 / 2f, moduleTemplate.YSIZE * 16 / 2f);
            else
                center += new Vector2(moduleTemplate.YSIZE * 16 / 2f, moduleTemplate.XSIZE * 16 / 2f);

            DrawCircle(center, ActiveModule.ShieldHitRadius * Camera.Zoom, Color.LightGreen);
        }

        private void DrawDebug()
        {
            float width2 = ScreenWidth / 2f;
            var pos  = new Vector2(width2 - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
            HelperFunctions.DrawDropShadowText(ScreenManager, "Debug", pos, Fonts.Arial20Bold);
            pos      = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
            HelperFunctions.DrawDropShadowText(ScreenManager, Operation.ToString(), pos, Fonts.Arial20Bold);
            #if SHIPYARD
                string ratios = $"I: {TotalI}       O: {TotalO}      E: {TotalE}      IO: {TotalIO}      " +
                                $"IE: {TotalIE}      OE: {TotalOE}      IOE: {TotalIOE}";
                pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Ratios).X / 2, 180f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, Ratios, pos, Fonts.Arial20Bold);
            #endif
        }

        private void DrawHullSelection()
        {
            Rectangle  r = HullSelectionSub.Menu;
            r.Y      += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(ScreenManager.SpriteBatch);
            HullSL.Draw(ScreenManager.SpriteBatch);
            Vector2 mousePos = Mouse.GetState().Pos();
            HullSelectionSub.Draw();

            foreach (ScrollList.Entry e in HullSL.VisibleExpandedEntries)
            {
                var bCursor = new Vector2(HullSelectionSub.Menu.X + 10, e.Y);
                if (e.item is ModuleHeader header)
                {
                    header.Draw(ScreenManager, bCursor);
                }
                else if (e.item is ShipData ship)
                {
                    bCursor.X += 10f;
                    ScreenManager.SpriteBatch.Draw(ship.Icon, new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(ship.HullRole, EmpireManager.Player), tCursor, Color.Orange);
                    
                    e.CheckHover(mousePos);
                }
            }
        }

        private void DrawShipInfoPanel()
        {
            float hitPoints                = 0f;
            float mass                     = 0f;
            float powerCapacity            = 0f;
            float ordnanceCap              = 0f;
            float powerFlow                = 0f;
            float shieldPower              = 0f;
            float thrust                   = 0f;
            float afterThrust              = 0f;
            float cargoSpace               = 0f;
            int size                       = 0;
            float cost                     = 0f;
            float warpThrust               = 0f;
            float turnThrust               = 0f;
            float warpableMass             = 0f;
            float warpDraw                 = 0f;
            float ftlCount                 = 0f;
            float ftlSpeed                 = 0f;
            float repairRate               = 0f;
            float sensorRange              = 0f;
            float sensorBonus              = 0f;
            float ordnanceUsed             = 0f;
            float ordnanceRecoverd         = 0f;
            float weaponPowerNeeded        = 0f;
            float warpSpoolTimer           = 0f;
            float empResist                = 0f;
            float offense                  = 0f;
            float defense                  = 0f;
            float targets                  = 0;
            float totalEcm                 = 0f;
            float beamPeakPowerNeeded      = 0f;
            float beamLongestDuration      = 0f;
            float weaponPowerNeededNoBeams = 0f;
            bool hasBridge                 = false;
            bool emptySlots                = true;
            bool bEnergyWeapons            = false;
            int troopCount                 = 0;
            int fixedTargets               = 0;
            int numWeaponSlots             = 0;
            HullBonus bonus                 = ActiveHull.Bonuses;

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                bool wasOffenseDefenseAdded = false;
                size += 1;
                if (slot.Root.ModuleUID == null)
                    emptySlots = false;
                if (slot.Module == null)
                    continue;
                if (slot.Module.InstalledWeapon != null)
                    numWeaponSlots += slot.Module.Area;

                hitPoints     += slot.Module.ActualMaxHealth;
                mass          += slot.Module.ActualMass;
                troopCount    += slot.Module.TroopCapacity;
                powerCapacity += slot.Module.ActualPowerStoreMax;
                ordnanceCap   += slot.Module.OrdinanceCapacity;
                powerFlow     += slot.Module.ActualPowerFlowMax;
                cost          += slot.Module.Cost;
                cargoSpace    += slot.Module.Cargo_Capacity;

                if (slot.Module.PowerDraw <= 0) // some modules might not need power to operate, we still need their offense
                {
                    offense  += slot.Module.CalculateModuleOffense();
                    defense  += slot.Module.CalculateModuleDefense(ModuleGrid.SlotsCount);
                    wasOffenseDefenseAdded = true;
                }
                if (!slot.Module.Powered)
                    continue;

                empResist        += slot.Module.EMP_Protection;
                warpableMass     += slot.Module.WarpMassCapacity;
                warpDraw         += slot.Module.PowerDrawAtWarp;
                shieldPower      += slot.Module.ActualShieldPowerMax;
                thrust           += slot.Module.thrust;
                warpThrust       += slot.Module.WarpThrust;
                turnThrust       += slot.Module.TurnThrust;
                repairRate       += slot.Module.ActualBonusRepairRate;
                ordnanceRecoverd += slot.Module.OrdnanceAddedPerSecond;
                targets          += slot.Module.TargetTracking;
                totalEcm          = Math.Max(slot.Module.ECM, totalEcm);
                sensorRange       = Math.Max(slot.Module.SensorRange, sensorRange);
                sensorBonus       = Math.Max(slot.Module.SensorBonus, sensorBonus);
                fixedTargets      = Math.Max(slot.Module.FixedTracking, fixedTargets);

                if (slot.Module.IsCommandModule)
                    hasBridge = true;
                if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.PowerRequiredToFire > 0)
                    bEnergyWeapons = true;
                if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.BeamPowerCostPerSecond > 0)
                    bEnergyWeapons = true;
                if (slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier > warpSpoolTimer)
                    warpSpoolTimer = slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier;
                if (slot.Module.FTLSpeed > 0f)
                {
                    ftlCount += 1f;
                    ftlSpeed += slot.Module.FTLSpeed;
                }
                if (!wasOffenseDefenseAdded)
                {
                    offense += slot.Module.CalculateModuleOffense();
                    defense += slot.Module.CalculateModuleDefense(ModuleGrid.SlotsCount);
                }
                //added by gremlin collect weapon stats                  
                if (!slot.Module.isWeapon && slot.Module.BombType == null)
                    continue;

                Weapon weapon      = slot.Module.BombType == null ? slot.Module.InstalledWeapon : ResourceManager.WeaponsDict[slot.Module.BombType];
                ordnanceUsed      += weapon.OrdnanceUsagePerSecond;
                weaponPowerNeeded += weapon.PowerFireUsagePerSecond;
                // added by Fat Bastard for Energy power calcs
                if (weapon.isBeam) 
                {
                    beamPeakPowerNeeded += weapon.BeamPowerCostPerSecond;
                    beamLongestDuration  = Math.Max(beamLongestDuration, weapon.BeamDuration);
                }
                else
                    weaponPowerNeededNoBeams += weapon.PowerFireUsagePerSecond; // FB: need non beam weapons power cost to add to the beam peak power cost
            }
            Power netPower = Power.Calculate(ModuleGrid.Modules, EmpireManager.Player, ShieldsBehaviorList.ActiveValue);

            // Other modification to the ship and draw values

            empResist += size; // FB: so the player will know the true EMP Tolerance
            targets   += fixedTargets;

            // @todo WTF is this?
            mass += (ActiveHull.ModuleSlots.Length / 2f);
            mass *= EmpireManager.Player.data.MassModifier;
            if (mass < (ActiveHull.ModuleSlots.Length / 2f))
                mass = (ActiveHull.ModuleSlots.Length / 2f);

            float powerRecharge = powerFlow - netPower.NetSubLightPowerDraw;
            float speed         = thrust / mass;
            float turn          = MathHelper.ToDegrees(turnThrust / mass / 700f); 
            float warpSpeed     = (warpThrust / (mass + 0.1f)) * EmpireManager.Player.data.FTLModifier * bonus.SpeedModifier;  // Added by McShooterz: hull bonus speed;
            string warpString   = warpSpeed.GetNumberString();
            float modifiedSpeed = speed * EmpireManager.Player.data.SubLightModifier * bonus.SpeedModifier;
            float afterSpeed    = (afterThrust / (mass + 0.1f)) * EmpireManager.Player.data.SubLightModifier; 

            Vector2 cursor   = new Vector2((StatsSub.Menu.X + 10), (ShipStats.Menu.Y + 18));

            DrawHullBonuses();

            float upkeep;
            if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                upkeep = GetMaintCostShipyardProportional(ActiveHull, cost, EmpireManager.Player); // FB: this is not working 
            else
                upkeep = GetMaintenanceCost(ActiveHull, (int)cost, EmpireManager.Player);

            DrawStatColor(ref cursor, TintedValue("Upkeep Cost", upkeep, 175, Color.White));
            DrawStatColor(ref cursor, TintedValue("Total Module Slots", size, 230, Color.White));
            DrawStatColor(ref cursor, TintedValue(115, (int)mass, 79, Color.White));
            WriteLine(ref cursor);

            DrawStatColor(ref cursor, TintedValue(110, powerCapacity, 100, Color.LightSkyBlue));
            DrawStatColor(ref cursor, TintedValue(111, powerRecharge, 101, Color.LightSkyBlue));

            float fDrawAtWarp;
            DrawPowerDrawAtWarp();
            DrawEnergyStats();
            DrawPeakPowerStats();
            DrawFtlTime();
            WriteLine(ref cursor);

            DrawStatColor(ref cursor, TintedValue(113, hitPoints, 103, Color.Goldenrod));
            if (repairRate > 0)  DrawStatColor(ref cursor, TintedValue(6013, repairRate, 236, Color.Goldenrod)); // Added by McShooterz: draw total repair
            if (shieldPower > 0) DrawStatColor(ref cursor, TintedValue(114, shieldPower, 104, Color.Goldenrod));
            if (empResist > 0)   DrawStatColor(ref cursor, TintedValue(6177, empResist, 220, Color.Goldenrod));
            if (totalEcm > 0)    DrawStatColor(ref cursor, TintedValue(6189, totalEcm, 234, Color.Goldenrod));
            WriteLine(ref cursor);

            DrawPropulsion();
            DrawStatColor(ref cursor, TintedValue(116, modifiedSpeed, 105, Color.DarkSeaGreen));
            DrawStatColor(ref cursor, TintedValue(117, turn, 107, Color.DarkSeaGreen));
            if (afterSpeed > 0) DrawStatColor(ref cursor, TintedValue("Afterburner Speed", afterSpeed, 105, Color.DarkSeaGreen));
            WriteLine(ref cursor);

            DrawOrdnance();
            if (troopCount > 0) DrawStatColor(ref cursor, TintedValue(6132, troopCount, 180, Color.IndianRed));
            WriteLine(ref cursor);

            if (cargoSpace > 0) DrawStatColor(ref cursor, TintedValue(119, cargoSpace * bonus.CargoModifier, 109, Color.White));
            if (targets > 0)    DrawStatColor(ref cursor, TintedValue(6188, targets + 1f, 232, Color.White));
            if (sensorRange > 0)
            {
                float modifiedSensorRange = (sensorRange + sensorBonus) * bonus.SensorModifier;
                DrawStatColor(ref cursor, TintedValue(6130, modifiedSensorRange, 235, Color.White));
            }

            float strength = ShipBuilder.GetModifiedStrength(size, numWeaponSlots, offense, defense, ActiveHull.Role, turn);
            if (strength > 0) DrawStatColor(ref cursor, TintedValue(6190, strength, 227, Color.White));

            var cursorReq = new Vector2(StatsSub.Menu.X - 180, ShipStats.Menu.Y + Fonts.Arial12Bold.LineSpacing + 5);
            if (ActiveHull.Role != ShipData.RoleName.platform)
                DrawRequirement(ref cursorReq, Localizer.Token(120), hasBridge, 1983);

            DrawRequirement(ref cursorReq, Localizer.Token(121), emptySlots, 1982);

            // Local methods

            void DrawHullBonuses()
            {
                BeginVLayout(cursor, Fonts.Arial12Bold.LineSpacing + 2);

                if (bonus.Hull.NotEmpty()) //Added by McShooterz: Draw Hull Bonuses
                {
                    if (bonus.ArmoredBonus     != 0 || bonus.ShieldBonus != 0
                        || bonus.SensorBonus   != 0 || bonus.SpeedBonus != 0
                        || bonus.CargoBonus    != 0 || bonus.DamageBonus != 0
                        || bonus.FireRateBonus != 0 || bonus.RepairBonus != 0
                        || bonus.CostBonus != 0)
                    {
                        Label(Localizer.Token(6015), Fonts.Verdana14Bold, Color.Orange);
                    }

                    HullBonus(bonus.ArmoredBonus, Localizer.HullArmorBonus);
                    HullBonus(bonus.ShieldBonus, Localizer.HullShieldBonus);
                    HullBonus(bonus.SensorBonus, Localizer.HullSensorBonus);
                    HullBonus(bonus.SpeedBonus, Localizer.HullSpeedBonus);
                    HullBonus(bonus.CargoBonus, Localizer.HullCargoBonus);
                    HullBonus(bonus.DamageBonus, Localizer.HullDamageBonus);
                    HullBonus(bonus.FireRateBonus, Localizer.HullFireRateBonus);
                    HullBonus(bonus.RepairBonus, Localizer.HullRepairBonus);
                    HullBonus(bonus.CostBonus, Localizer.HullCostBonus);
                }
                cursor = EndLayout();
                cost = ((int)cost + bonus.StartingCost) * (1f - bonus.CostBonus) * UniverseScreen.GamePaceStatic;
                DrawStatColor(ref cursor, TintedValue(109, cost, 99, Color.White));  
            }

            void HullBonus(float stat, string text)
            {
                if (stat > 0 || stat < 0) return;
                Label($"{stat * 100f}%  {text}", Fonts.Verdana12, Color.Orange);
            }

            void DrawOrdnance()
            {
                if (ordnanceRecoverd > 0) DrawStatColor(ref cursor, TintedValue("Ordnance Created / s", ordnanceRecoverd, 162, Color.IndianRed));
                if (!(ordnanceCap > 0))
                    return;

                DrawStatColor(ref cursor, TintedValue(118, ordnanceCap, 108, Color.IndianRed));
                if (ordnanceUsed - ordnanceRecoverd > 0)
                {
                    float ammoTime = ordnanceCap / (ordnanceUsed - ordnanceRecoverd);
                    DrawStatColor(ref cursor, TintedValue("Ammo Time", ammoTime, 164, Color.IndianRed));
                }
                else
                    DrawStatOrdnance(ref cursor, "Ammo Time", "INF", 164);
            }

            void DrawPropulsion()
            {
                if (GlobalStats.HardcoreRuleset)
                {
                    string massstring = mass.GetNumberString();
                    string wmassstring = warpableMass.GetNumberString();
                    string warpmassstring = string.Concat(massstring, "/", wmassstring);
                    if (mass > warpableMass)
                        DrawStatBad(ref cursor, "Warpable Mass:", warpmassstring, 153);
                    else
                        DrawStat(ref cursor, "Warpable Mass:", warpmassstring, 153);

                    DrawRequirement(ref cursor, "Warp Capable", mass <= warpableMass);
                    if (ftlCount > 0f)
                    {
                        float harcoreSpeed = ftlSpeed / ftlCount;
                        DrawStatColor(ref cursor, TintedValue(2170, harcoreSpeed, 135, Color.LightSkyBlue));
                    }
                }
                else
                    DrawStatPropulsion(ref cursor, string.Concat(Localizer.Token(2170), ":"), warpString, 135);

                if (warpSpeed > 0 && warpSpoolTimer > 0) DrawStatColor(ref cursor, TintedValue("FTL Spool", warpSpoolTimer, 177, Color.DarkSeaGreen));
            }

            void DrawPowerDrawAtWarp() //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            {
                fDrawAtWarp = powerFlow - netPower.NetWarpPowerDraw;
                if (warpSpeed > 0)
                    DrawStatColor(ref cursor, TintedValue(112, fDrawAtWarp, 102, Color.LightSkyBlue));
            }


            void DrawFtlTime()
            {
                if (!(warpSpeed > 0))
                    return;

                float fWarpTime = (-powerCapacity / fDrawAtWarp) * 0.9f;
                if (fDrawAtWarp < 0)
                    DrawStatColor(ref cursor, TintedValue("FTL Time", fWarpTime, 176, Color.LightSkyBlue));
                else if (fWarpTime > 900)
                    DrawStatEnergy(ref cursor, "FTL Time:", "INF", 176);
                else
                    DrawStatEnergy(ref cursor, "FTL Time:", "INF", 176);
            }

            void DrawEnergyStats()
            {
                if (!bEnergyWeapons)
                    return;

                float powerConsumed = weaponPowerNeeded - powerRecharge;
                if (powerConsumed > 0) // There is power drain from ship's reserves when firing its energy weapons after taking into acount recharge
                {
                    DrawStatColor(ref cursor, NormalValue("Excess Wpn Pwr Drain", -powerConsumed, 243, Color.LightSkyBlue));
                    float energyDuration = powerCapacity / powerConsumed;
                    DrawStatColor(ref cursor, TintedValue("Wpn Fire Power Time", energyDuration, 163, Color.LightSkyBlue));
                }
                else
                    DrawStatEnergy(ref cursor, "Wpn Fire Power Time:", "INF", 163);

            }

            void DrawPeakPowerStats()
            {
                // FB: @todo  using Beam Longest Duration for peak power calculation in case of variable beam durations in the ship will show the player he needs 
                // more power than actually needed. Need to find a better way to show accurate numbers to the player in such case
                if (!(beamLongestDuration > 0))
                    return;

                float powerConsumedWithBeams = beamPeakPowerNeeded + weaponPowerNeededNoBeams - powerRecharge;
                if (!(powerConsumedWithBeams > 0))
                    return;

                DrawStatColor(ref cursor, NormalValue("Burst Wpn Pwr Drain", - powerConsumedWithBeams, 244, Color.LightSkyBlue));
                float burstEnergyDuration = powerCapacity / powerConsumedWithBeams;
                if (burstEnergyDuration < beamLongestDuration)
                    DrawStatColor(ref cursor, BadValue("Burst Wpn Pwr Time", burstEnergyDuration, 245, Color.LightSkyBlue));
                else
                    DrawStatEnergy(ref cursor, "Burst Wpn Pwr Time:", "INF", 245);
            }
        }

        private void DrawRequirement(ref Vector2 cursor, string words, bool met, int tooltipId = 0, float lineSpacing = 2)
        {
            float amount = 165f;
            SpriteFont font = Fonts.Arial12Bold;
            if (GlobalStats.IsGermanFrenchOrPolish) amount = amount + 35f;
            cursor.Y += lineSpacing > 0 ? Fonts.Arial12Bold.LineSpacing + lineSpacing : 0;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, cursor, met ? Color.LightGreen : Color.LightPink);
            string stats = met ? "OK" : "X";
            cursor.X += amount - Fonts.Arial12Bold.MeasureString(stats).X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stats, cursor, met ? Color.LightGreen : Color.LightPink);
            cursor.X -= amount - Fonts.Arial12Bold.MeasureString(stats).X;
            if (tooltipId > 0) CheckToolTip(tooltipId, cursor, words, stats, font, MousePos);
        }

        public void DrawStat(ref Vector2 cursor, string words, string stat, int tooltipId, Color nameColor, Color statColor, float spacing = 165f, float lineSpacing = 2)
        {
            SpriteFont font = Fonts.Arial12Bold;
            cursor.Y += lineSpacing > 0 ? font.LineSpacing + lineSpacing : 0;

            var statCursor = new Vector2(cursor.X + Spacing(spacing), cursor.Y);
            Vector2 statNameCursor = FontSpace(statCursor, -40, words, font);

            DrawString(statNameCursor, nameColor, words, font);
            DrawString(statCursor, statColor, stat, font);
            CheckToolTip(tooltipId, cursor, words, stat, font, MousePos);
        }

        public void DrawStat(ref Vector2 cursor, string words, float stat, Color color, int tooltipId, bool doGoodBadTint = true, bool isPercent = false, float spacing = 165)
        {
            StatValue sv = isPercent ? TintedPercent(words, stat, tooltipId, color, spacing, 0)
                                     :   TintedValue(words, stat, tooltipId, color, spacing, 0);
            DrawStatColor(ref cursor, sv);
        }

        private void DrawStatEnergy(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.LightSkyBlue, Color.LightGreen);
        }

        private void DrawStatPropulsion(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.DarkSeaGreen, Color.LightGreen);
        }

        private void DrawStatOrdnance(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen);
        }

        private void DrawStatBad(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightPink);
        }

        private void DrawStat(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen);
        }

        private static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }

        private void DrawUi()
        {
            EmpireUI.Draw(ScreenManager.SpriteBatch);
            DrawShipInfoPanel();

            CategoryList.Draw(ScreenManager.SpriteBatch);

            DrawTitle(ScreenWidth * 0.375f, "Repair Options");
            DrawTitle(ScreenWidth * 0.5f, "Behavior Presets");

            if (GlobalStats.WarpBehaviorsEnabled) // FB: enable shield warp state
            {
                DrawTitle(ScreenWidth * 0.65f, "Shields State At Warp");
                ShieldsBehaviorList.Draw(ScreenManager.SpriteBatch);
            }

            float transitionOffset = (float) Math.Pow(TransitionPosition, 2);

            Rectangle r = BlackBar;
            if (IsTransitioning)
                r.Y += (int)(transitionOffset * 50f);
            ScreenManager.SpriteBatch.FillRectangle(r, Color.Black);


            r = BottomSep;
            if (IsTransitioning)
                r.Y += (int) (transitionOffset * 50f);
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(77, 55, 25));


            r = SearchBar;
            if (IsTransitioning)
                r.Y += (int)(transitionOffset * 50f);
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));


            SpriteFont font = Fonts.Arial20Bold.MeasureString(ActiveHull.Name).X <= (SearchBar.Width - 5)
                            ? Fonts.Arial20Bold : Fonts.Arial12Bold;
            var cursor1 = new Vector2(SearchBar.X + 3, r.Y + 14 - font.LineSpacing / 2);
            ScreenManager.SpriteBatch.DrawString(font, ActiveHull.Name, cursor1, Color.White);


            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));

            {
                var cursor = new Vector2(r.X + 3, r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
            }

            if (IsTransitioning)
            {
                //SaveButton.Y += (int)(transitionOffset * 50f);
                //LoadButton.Y += (int)(transitionOffset * 50f);
                //ToggleOverlayButton.Y += (int)(transitionOffset * 50f);
                //SymmetricDesignButton.Y += (int)(transitionOffset * 50f);
            }

            ModSel.Draw(ScreenManager.SpriteBatch);
            
            DrawHullSelection();

            if (IsActive)
                ToolTip.Draw(ScreenManager.SpriteBatch);

            void DrawTitle(float x, string title)
            {
                int buttonHeight = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10;
                var pos = new Vector2(x, buttonHeight);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, title, pos, Color.Orange);
            }
        }

        private enum ValueTint
        {
            None,
            Bad,
            GoodBad
        }

        private struct StatValue
        {
            public string Title;
            public Color TitleColor;
            public float Value;
            public int Tooltip;
            public ValueTint Tint;
            public bool IsPercent;
            public float Spacing;
            public int LineSpacing;

            public Color ValueColor => Tint == ValueTint.GoodBad ? (Value > 0f ? Color.LightGreen : Color.LightPink) :
                Tint == ValueTint.Bad ? Color.LightPink : Color.White;

            public string ValueText => IsPercent ? Value.ToString("P1") : Value.GetNumberString();
        }

        private static StatValue NormalValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.None, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue BadValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.Bad, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue TintedValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue TintedValue(int titleId, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = Localizer.Token(titleId)+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue TintedPercent(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, IsPercent = true, Spacing = spacing, LineSpacing = lineSpacing };

        private void DrawStatColor(ref Vector2 cursor, StatValue stat)
        {
            SpriteFont font = Fonts.Arial12Bold;
            //const float spacing = 165f;

            WriteLine(ref cursor);
            cursor.Y += stat.LineSpacing;

            Vector2 statCursor = new Vector2(cursor.X + Spacing(stat.Spacing), cursor.Y);
            DrawString(FontSpace(statCursor, -40, stat.Title, font), stat.TitleColor, stat.Title, font); // @todo Replace with DrawTitle?

            string valueText = stat.ValueText;
            DrawString(statCursor, stat.ValueColor, valueText, font);
            CheckToolTip(stat.Tooltip, cursor, stat.Title, valueText, font, MousePos);
        }
    }
}