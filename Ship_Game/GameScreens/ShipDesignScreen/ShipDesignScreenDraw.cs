using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.UI;

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

            Close.Draw(batch);
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
                    if (slot?.SlotReference.Position.X > 256f)
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
                MirrorSlot mirrored = new MirrorSlot();
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

                Rectangle? nullable8 = null;
                spriteBatch.Draw(ResourceManager.Texture("UI/lightningBolt"),
                    slot.Center(), nullable8, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
            }
        }

        private void DrawShieldRadius(Vector2 center, SlotStruct slot, SpriteBatch spriteBatch, MirrorSlot mirrored)
        {
            spriteBatch.DrawCircle(center, slot.Module.ShieldHitRadius, Color.LightGreen);
            if (!IsSymmetricDesignMode || !IsMirrorModuleValid(slot.Module, mirrored.Slot?.Root.Module))
                return;

            Vector2 mirroredCenter = mirrored.Slot.Center();
            spriteBatch.DrawCircle(mirroredCenter, mirrored.Slot.Module.ShieldHitRadius, Color.LightGreen);
        }

        private void DrawFireArcText(Vector2 center, SlotStruct slot, MirrorSlot mirrored)
        {
            Color color = Color.Black;
            color.A     = 140;

            DrawRectangle(slot.ModuleRect, Color.White, color);
            DrawString(center, 0, 1, Color.Orange, slot.Module.Facing.ToString(CultureInfo.CurrentCulture));
            if (!IsSymmetricDesignMode || !IsMirrorModuleValid(slot.Module, mirrored.Slot?.Root.Module))
                return;

            Vector2 mirroredCenter = mirrored.Slot.Center();
            DrawRectangle(mirrored.Slot.ModuleRect, Color.White, color);
            DrawString(mirroredCenter, 0, 1, Color.Orange, mirrored.Slot.Module.Facing.ToString(CultureInfo.CurrentCulture));

            ToolTip.ShipYardArcTip();
        }

        private void DrawHangarShipText(Vector2 center, SlotStruct slot, MirrorSlot mirrored)
        {
            Color color = Color.Black;
            color.A     = 140;

            DrawRectangle(slot.ModuleRect, Color.Teal, color);
            DrawString(center, 0, 0.4f, Color.White, slot.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
            if (!IsSymmetricDesignMode || !IsMirrorModuleValid(slot.Module, mirrored.Slot?.Root.Module))
                return;

            Vector2 mirroredCenter = mirrored.Slot.Center();
            DrawRectangle(mirrored.Slot.ModuleRect, Color.Teal, color);
            DrawString(mirroredCenter, 0, 0.4f, Color.White, mirrored.Slot.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
        }

        private void DrawArc(Vector2 center, SlotStruct slot, Color drawcolor, SpriteBatch spriteBatch, MirrorSlot mirrored)
        {
            Texture2D arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);
            Vector2 origin       = new Vector2(250f, 250f);
            Rectangle toDraw     = new Rectangle((int)center.X, (int)center.Y, 500, 500);

            spriteBatch.Draw(arcTexture, toDraw, null, drawcolor, slot.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);
            if (!IsSymmetricDesignMode || !IsMirrorModuleValid(slot.Module, mirrored.Slot?.Root.Module))
                return;

            Vector2 mirroredCenter = mirrored.Slot.Center();
            Rectangle mirrortoDraw = new Rectangle((int)mirroredCenter.X, (int)mirroredCenter.Y, 500, 500);
            spriteBatch.Draw(arcTexture, mirrortoDraw, null, drawcolor, mirrored.Slot.Root.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);
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
            Rectangle r = new Rectangle((int)Input.CursorPosition.X, (int)Input.CursorPosition.Y,
                          (int)(16 * ActiveModule.XSIZE * Camera.Zoom),
                          (int)(16 * ActiveModule.YSIZE * Camera.Zoom));
            DrawModuleTex(ActiveModState, spriteBatch, null, r, moduleTemplate);
            if (!(ActiveModule.shield_power_max > 0f))
                return;

            Vector2 center = new Vector2(Input.CursorPosition.X, Input.CursorPosition.Y);
            if (ActiveModState == ModuleOrientation.Normal || ActiveModState == ModuleOrientation.Rear)
                center += new Vector2(moduleTemplate.XSIZE * 16 / 2f, moduleTemplate.YSIZE * 16 / 2f);
            else
                center += new Vector2(moduleTemplate.YSIZE * 16 / 2f, moduleTemplate.XSIZE * 16 / 2f);

            DrawCircle(center, ActiveModule.ShieldHitRadius * Camera.Zoom, Color.LightGreen);
        }

        private void DrawDebug()
        {
            float width2 = ScreenWidth / 2f;
            Vector2 pos  = new Vector2(width2 - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
            HelperFunctions.DrawDropShadowText(ScreenManager, "Debug", pos, Fonts.Arial20Bold);
            pos          = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
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
            r.Y         += 25;
            r.Height    -= 25;
            Selector sel = new Selector(r, new Color(0, 0, 0, 210));
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
            float powerDraw                = 0f;
            float powerCapacity            = 0f;
            float ordnanceCap              = 0f;
            float powerFlow                = 0f;
            float shieldPower              = 0f;
            float thrust                   = 0f;
            float afterThrust              = 0f;
            float cargoSpace               = 0f;
            float size                     = 0f;
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
            float upkeep                   = 0f;
            float warpSpoolTimer           = 0f;
            float empResist                = 0f;
            float offense                  = 0f;
            float defense                  = 0f;
            float strength                 = 0;
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
            HullBonus bonus                 = ActiveHull.Bonuses;

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                bool wasOffenseDefenseAdded = false;
                size += 1f;
                if (slot.Root.ModuleUID == null)
                    emptySlots = false;
                if (slot.Module == null)
                    continue;

                hitPoints     += slot.Module.ActualMaxHealth;
                mass          += slot.Module.ActualMass(slot.InPowerRadius);
                troopCount    += slot.Module.TroopCapacity;
                powerCapacity += slot.Module.ActualPowerStoreMax;
                ordnanceCap   += slot.Module.OrdinanceCapacity;
                powerFlow     += slot.Module.ActualPowerFlowMax;
                cost          += slot.Module.Cost;
                cargoSpace    += slot.Module.Cargo_Capacity;

                if (slot.Module.PowerDraw <= 0) // some modules might not need power to operate, we still need their offense
                {
                    offense  += slot.Module.CalculateModuleOffense();
                    defense  += slot.Module.CalculateModuleDefense((int)size);
                    wasOffenseDefenseAdded = true;
                }
                if (!slot.Module.Powered)
                    continue;

                empResist        += slot.Module.EMP_Protection;
                warpableMass     += slot.Module.WarpMassCapacity;
                powerDraw        += slot.Module.PowerDraw;
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
                    defense += slot.Module.CalculateModuleDefense((int)size);
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
            Power netPower = Power.Calculate(ModuleGrid.Modules, EmpireManager.Player, ShieldsWarpBehavior.OnFullChargeAtWarpExit);
            // Other modification to the ship and draw values

            empResist += size; // FB: so the player will know the true EMP Tolerance
            targets   += fixedTargets;
            mass      += (float) (ActiveHull.ModuleSlots.Length / 2f);
            mass      *= EmpireManager.Player.data.MassModifier;

            if (mass < (float) (ActiveHull.ModuleSlots.Length / 2f))
                mass = (float) (ActiveHull.ModuleSlots.Length / 2f);

            float powerRecharge = powerFlow - netPower.NetSubLightPowerDraw;
            float speed         = thrust / mass;
            float turn          = (float)MathHelper.ToDegrees(turnThrust / mass / 700f); 
            float warpSpeed     = (warpThrust / (mass + 0.1f)) * EmpireManager.Player.data.FTLModifier * bonus.SpeedModifier;  // Added by McShooterz: hull bonus speed;
            float single        = warpSpeed / 1000f;
            string warpString   = string.Concat(single.ToString("#.0"), "k");
            float modifiedSpeed = speed * EmpireManager.Player.data.SubLightModifier * bonus.SpeedModifier;
            float afterSpeed    = (afterThrust / (mass + 0.1f)) * EmpireManager.Player.data.SubLightModifier; 

            Vector2 cursor   = new Vector2((float) (StatsSub.Menu.X + 10), (float) (ShipStats.Menu.Y + 18));

            DrawHullBonuses();

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                upkeep = GetMaintCostShipyardProportional(ActiveHull, cost, EmpireManager.Player); // FB: this is not working 
            else
                upkeep = GetMaintenanceCost(ActiveHull, (int)cost, EmpireManager.Player);

            DrawStatColor(ref cursor, TintedValue("Upkeep Cost:", upkeep, 175, Color.White));
            DrawStatColor(ref cursor, TintedValue("Total Module Slots:", size, 230, Color.White));
            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(115), ":"), (int)mass, 79, Color.White));
            WriteLine(ref cursor);

            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(110), ":"), powerCapacity, 100, Color.LightSkyBlue));
            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(111), ":"), powerRecharge, 101, Color.LightSkyBlue));

            float fDrawAtWarp;
            DrawPowerDrawAtWarp();
            DrawEnergyStats();
            DrawPeakPowerStats();
            DrawFtlTime();
            WriteLine(ref cursor);

            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(113), ":"), hitPoints, 103, Color.Goldenrod));
            if (repairRate > 0)  DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6013), ":"), repairRate, 236, Color.Goldenrod)); // Added by McShooterz: draw total repair
            if (shieldPower > 0) DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(114), ":"), shieldPower, 104, Color.Goldenrod));
            if (empResist > 0)   DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6177), ":"), empResist, 220, Color.Goldenrod));
            if (totalEcm > 0)    DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6189), ":"), totalEcm, 234, Color.Goldenrod));
            WriteLine(ref cursor);

            DrawPropulsion();
            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(116), ":"), modifiedSpeed, 105, Color.DarkSeaGreen));
            DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(117), ":"), turn, 107, Color.DarkSeaGreen));
            if (afterSpeed > 0) DrawStatColor(ref cursor, TintedValue("Afterburner Speed:", afterSpeed, 105, Color.DarkSeaGreen));
            WriteLine(ref cursor);

            DrawOrdnance();
            if (troopCount > 0) DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6132), ":"), troopCount, 180, Color.IndianRed));
            WriteLine(ref cursor);

            if (cargoSpace > 0) DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(119), ":"), cargoSpace * bonus.CargoModifier, 109, Color.White));
            if (targets > 0)    DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6188), ":"), targets + 1f, 232, Color.White));
            if (sensorRange > 0)
            {
                float modifiedSensorRange = (sensorRange + sensorBonus) * bonus.SensorModifier;
                DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6130), ":"), modifiedSensorRange, 235, Color.White));
            }

            strength = defense > offense ? offense * 2 : defense + offense;
            if (strength > 0)     DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(6190), ":"), strength, 227, Color.White));

            Vector2 cursorReq = new Vector2((float) (StatsSub.Menu.X - 180), (float) (ShipStats.Menu.Y + (Fonts.Arial12Bold.LineSpacing) + 5 ));
            if (ActiveHull.Role != ShipData.RoleName.platform)
                DrawRequirement(ref cursorReq, Localizer.Token(120), hasBridge, 1983);

            DrawRequirement(ref cursorReq, Localizer.Token(121), emptySlots, 1982);

            // Local methods

            void DrawHullBonuses()
            {
                BeginVLayout(cursor, Fonts.Arial12Bold.LineSpacing + 2);

                if (bonus.Hull.NotEmpty()) //Added by McShooterz: Draw Hull Bonuses
                {
                    if (bonus.ArmoredBonus != 0
                        || bonus.ShieldBonus != 0
                        || bonus.SensorBonus != 0
                        || bonus.SpeedBonus != 0
                        || bonus.CargoBonus != 0
                        || bonus.DamageBonus != 0
                        || bonus.FireRateBonus != 0
                        || bonus.RepairBonus != 0
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
                DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(109), ":"), cost, 99, Color.White));  
            }

            void HullBonus(float stat, string text)
            {
                if (stat > 0 || stat < 0) return;
                Label($"{stat * 100f}%  {text}", Fonts.Verdana12, Color.Orange);
            }

            void DrawOrdnance()
            {
                if (ordnanceRecoverd > 0) DrawStatColor(ref cursor, TintedValue("Ordnance Created / s:", ordnanceRecoverd, 162, Color.IndianRed));
                if (!(ordnanceCap > 0))
                    return;

                DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(118), ":"), ordnanceCap, 108, Color.IndianRed));
                if (ordnanceUsed - ordnanceRecoverd > 0)
                {
                    float ammoTime = ordnanceCap / (ordnanceUsed - ordnanceRecoverd);
                    DrawStatColor(ref cursor, TintedValue("Ammo Time:", ammoTime, 164, Color.IndianRed));
                }
                else
                    DrawStatOrdnance(ref cursor, "Ammo Time:", "INF", 164);
            }

            void DrawPropulsion()
            {
                if (GlobalStats.HardcoreRuleset)
                {
                    string massstring = GetNumberString(mass);
                    string wmassstring = GetNumberString(warpableMass);
                    string warpmassstring = string.Concat(massstring, "/", wmassstring);
                    if (mass > warpableMass)
                        DrawStatBad(ref cursor, "Warpable Mass:", warpmassstring, 153);
                    else
                        DrawStat(ref cursor, "Warpable Mass:", warpmassstring, 153);

                    DrawRequirement(ref cursor, "Warp Capable", mass <= warpableMass);
                    if (ftlCount > 0f)
                    {
                        float harcoreSpeed = ftlSpeed / ftlCount;
                        DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(2170), ":"), harcoreSpeed, 135, Color.LightSkyBlue));
                    }
                }
                else
                    DrawStatPropulsion(ref cursor, string.Concat(Localizer.Token(2170), ":"), warpString, 135);

                if (warpSpeed > 0 && warpSpoolTimer > 0) DrawStatColor(ref cursor, TintedValue("FTL Spool:", warpSpoolTimer, 177, Color.DarkSeaGreen));
            }

            void DrawPowerDrawAtWarp() //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            {
                if (warpDraw != 0) // FB: not sure why do we need this If statment
                {
                    fDrawAtWarp = powerFlow - netPower.NetWarpPowerDraw;
                    if (warpSpeed > 0)
                        DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102, Color.LightSkyBlue));
                }
                else
                {
                    fDrawAtWarp = powerFlow - netPower.NetWarpPowerDraw;
                    if (warpSpeed > 0)
                        DrawStatColor(ref cursor, TintedValue(string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102, Color.LightSkyBlue));
                }
            }


            void DrawFtlTime()
            {
                if (!(warpSpeed > 0))
                    return;

                float fWarpTime = (-powerCapacity / fDrawAtWarp) * 0.9f;
                if (fDrawAtWarp < 0)
                    DrawStatColor(ref cursor, TintedValue("FTL Time:", fWarpTime, 176, Color.LightSkyBlue));
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
                    DrawStatColor(ref cursor, NormalValue("Excess Wpn Pwr Drain:", -powerConsumed, 243, Color.LightSkyBlue));
                    float energyDuration = powerCapacity / powerConsumed;
                    DrawStatColor(ref cursor, TintedValue("Wpn Fire Power Time:", energyDuration, 163, Color.LightSkyBlue));
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

                DrawStatColor(ref cursor, NormalValue("Burst Wpn Pwr Drain:", - powerConsumedWithBeams, 244, Color.LightSkyBlue));
                float burstEnergyDuration = powerCapacity / powerConsumedWithBeams;
                if (burstEnergyDuration < beamLongestDuration)
                    DrawStatColor(ref cursor, BadValue("Burst Wpn Pwr Time:", burstEnergyDuration, 245, Color.LightSkyBlue));
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

        public void DrawStat(ref Vector2 cursor, string words, string stat, int tooltipId, Color nameColor,Color statColor, float spacing = 165f, float lineSpacing = 2)
        {
            SpriteFont font        = Fonts.Arial12Bold;
            float amount           = Spacing(spacing);
            cursor.Y              += lineSpacing > 0 ? font.LineSpacing + lineSpacing : 0;
            Vector2 statCursor     = new Vector2(cursor.X + amount, cursor.Y);
            Vector2 statNameCursor = FontSpace(statCursor, -40, words, font);
            Color color            = nameColor;
            DrawString(statNameCursor, color, words, font);
            color                  = statColor;
            DrawString(statCursor, color, stat, font);
            CheckToolTip(tooltipId, cursor, words, stat, font, MousePos);
        }

        public void DrawStat(ref Vector2 cursor, string words, float stat, Color color, int tooltipId, bool doGoodBadTint = true, bool isPercent = false, float spacing = 165)
        {
            if (isPercent)
                DrawStatColor(ref cursor, TintedPercent(words, stat, tooltipId, color, spacing, 0));
            else
                DrawStatColor(ref cursor, TintedValue(words, stat, tooltipId, color, spacing, 0));
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

            //Defaults based on hull types
            //Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
            if (ActiveHull.Role == ShipData.RoleName.freighter && Fml)
            {
                CategoryList.ActiveIndex = 1;
                Fml = false;
            }
            //Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
            else if (ActiveHull.Role == ShipData.RoleName.scout && Fml)
            {
                CategoryList.ActiveIndex = 2;
                Fml = false;
            }
            //All other hulls default to unclassified.
            else if (Fml)
            {
                CategoryList.ActiveIndex = 0;
                Fml = false;
            }

            //Loads the Category from the ShipDesign XML of the ship being loaded, and loads this OVER the hull type default, very importantly.
            if (Fmlevenmore && CategoryList.SetActiveEntry(LoadCategory.ToString()))
            {
                Fmlevenmore = false;
            }

            CategoryList.Draw(ScreenManager.SpriteBatch);
            CarrierOnlyBox.Draw(ScreenManager.SpriteBatch);
            const string classifTitle = "Behaviour Presets";
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, classifTitle, ClassifCursor, Color.Orange);
            float transitionOffset = (float) Math.Pow((double) TransitionPosition, 2);
            Rectangle r = BlackBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, Color.Black);
            r = BottomSep;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(77, 55, 25));
            r = SearchBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));
            if (Fonts.Arial20Bold.MeasureString(ActiveHull.Name).X <= (float) (SearchBar.Width - 5))
            {
                Vector2 cursor = new Vector2((float) (SearchBar.X + 3),
                    (float) (r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, ActiveHull.Name, cursor, Color.White);
            }
            else
            {
                Vector2 cursor = new Vector2(SearchBar.X + 3,
                    r.Y + 14 - Fonts.Arial12Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ActiveHull.Name, cursor, Color.White);
            }
            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));

            {
                Vector2 cursor = new Vector2(r.X + 3,r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
            }
            r = SaveButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            SaveButton.Draw(ScreenManager.SpriteBatch, r);
            r = LoadButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            LoadButton.Draw(ScreenManager.SpriteBatch, r);
            r = ToggleOverlayButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ToggleOverlayButton.Draw(ScreenManager.SpriteBatch, r);
            r = SymmetricDesignButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            SymmetricDesignButton.Draw(ScreenManager.SpriteBatch, r);

            ModSel.Draw(ScreenManager.SpriteBatch);
            
            DrawHullSelection();
 
            foreach (ToggleButton button in CombatStatusButtons)
            {
                button.Draw(ScreenManager);
            }
            if (IsActive)
            {
                ToolTip.Draw(ScreenManager.SpriteBatch);
            }
        }

        private enum ValueTint
        {
            None,
            Bad,
            GoodBad,
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

            public string ValueText => IsPercent ? Value.ToString("P1") : NumericText;

            private string NumericText => GetNumberString(Value);
        }

        private static StatValue NormalValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.None, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue BadValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.Bad, Spacing = spacing, LineSpacing = lineSpacing };

        private static StatValue TintedValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

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