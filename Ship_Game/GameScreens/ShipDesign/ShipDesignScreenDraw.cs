using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    using static ShipMaintenance;

    public sealed partial class ShipDesignScreen // refactored by Fat Bastard
    {
        public override void Draw(SpriteBatch batch)
        {
            GameTime gameTime = StarDriveGame.Instance.GameTime;
            ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);

            Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.StarField);
            ScreenManager.RenderSceneObjects();

            if (ToggleOverlay)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
                DrawEmptySlots(batch);
                DrawModules(batch);
                DrawUnpoweredTex(batch);
                DrawTacticalOverlays(batch);
                DrawModuleSelections(batch);
                batch.End();
            }

            batch.Begin();
            if (ActiveModule != null && !ModSel.HitTest(Input))
                DrawActiveModule(batch);

            DrawUi();
            selector?.Draw(batch);
            ArcsButton.DrawWithShadowCaps(batch);
            if (Debug)
                DrawDebug();

            base.Draw(batch);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        bool GetSlotForModule(ShipModule module, out SlotStruct slot)
        {
            slot = module == null ? null : ModuleGrid.SlotsList.FirstOrDefault(s => s.Module == module);
            return slot != null;
        }

        void DrawModuleSelections(SpriteBatch batch)
        {
            if (GetSlotForModule(HighlightedModule, out SlotStruct highlighted))
            {
                DrawRectangle(highlighted.ModuleRect, Color.DarkOrange, 1.25f);

                if (Input.LeftMouseHeld())
                {
                    if (GetMirrorSlot(highlighted, out MirrorSlot mirrored))
                    {
                        DrawRectangle(highlighted.ModuleRect, Color.DarkOrange.Alpha(0.33f), 1.25f);
                    }
                }
            }
        }

        void DrawEmptySlots(SpriteBatch batch)
        {
            SubTexture concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1");

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.Module != null)
                {
                    slot.Draw(batch, concreteGlass, Color.Gray);
                    continue;
                }

                if (slot.Root.Module != null)
                    continue;

                bool valid        = ActiveModule == null || slot.CanSlotSupportModule(ActiveModule);
                Color activeColor = valid ? Color.LightGreen : Color.Red;
                slot.Draw(batch, concreteGlass, activeColor);
                if (slot.InPowerRadius)
                {
                    Color yellow = ActiveModule != null ? new Color(Color.Yellow, 150) : Color.Yellow;
                    slot.Draw(batch, concreteGlass, yellow);
                }
                batch.DrawString(Fonts.Arial20Bold, " " + slot.Restrictions, slot.PosVec2, Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
            }
        }

        void DrawModules(SpriteBatch spriteBatch)
        {
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.ModuleUID != null && slot.Tex != null)
                    DrawModuleTex(slot.Orientation, spriteBatch, slot, slot.ModuleRect);
            }
        }

        static void DrawModuleTex(ModuleOrientation orientation, SpriteBatch spriteBatch, SlotStruct slot, Rectangle r, ShipModule template = null)
        {
            SpriteEffects effects = SpriteEffects.None;
            float rotation        = 0f;
            SubTexture texture    = template == null ? slot.Tex : ResourceManager.Texture(template.IconTexturePath);
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
            spriteBatch.Draw(texture, r, Color.White, rotation, Vector2.Zero, effects, 1f);
        }

        void DrawTacticalOverlays(SpriteBatch batch)
        {
            // if ShowAllArcs is enabled, then we can accidentally render
            // tactical overlays twice. This helps avoid that
            var alreadyDrawn = new HashSet<SlotStruct>();

            void DrawTacticalOverlays(SlotStruct s)
            {
                if (s.ModuleUID == null || s.Tex == null || alreadyDrawn.Contains(s))
                    return;

                alreadyDrawn.Add(s);

                if (s.Module.shield_power_max > 0f)
                    DrawShieldRadius(batch, s);

                if (s.Module.ModuleType == ShipModuleType.Turret && Input.LeftMouseHeld())
                {
                    DrawFireArcText(s);
                    if (IsSymmetricDesignMode)
                        ToolTip.ShipYardArcTip();
                }

                if (s.Module.ModuleType == ShipModuleType.Hangar)
                    DrawHangarShipText(s);

                DrawWeaponArcs(batch, s);

                if (IsSymmetricDesignMode && GetMirrorSlot(s, out MirrorSlot mirrored))
                {
                    DrawTacticalOverlays(mirrored.Slot);
                }
            }

            // we need to draw highlighted module first to get correct focus color
            foreach (SlotStruct s in ModuleGrid.SlotsList)
                if (s.Module == HighlightedModule)
                    DrawTacticalOverlays(s);

            if (ShowAllArcs) // draw all the rest
            {
                foreach (SlotStruct s in ModuleGrid.SlotsList)
                    DrawTacticalOverlays(s);
            }
        }

        void DrawUnpoweredTex(SpriteBatch spriteBatch)
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
                    slot.Center, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
            }
        }

        void DrawShieldRadius(SpriteBatch batch, SlotStruct slot)
        {
            batch.DrawCircle(slot.Center, slot.Module.ShieldHitRadius, Color.LightGreen);
        }

        void DrawFireArcText(SlotStruct slot)
        {
            Color fill = Color.Black.Alpha(0.33f);
            Color edge = (slot.Module == HighlightedModule) ? Color.DarkOrange : fill;
            DrawRectangle(slot.ModuleRect, edge, fill);
            DrawString(slot.Center, 0, 1, Color.Orange, slot.Module.Facing.ToString(CultureInfo.CurrentCulture));
        }

        void DrawHangarShipText(SlotStruct s)
        {
            Color color = Color.Black.Alpha(0.33f);
            Color textC = ShipBuilder.GetHangarTextColor(s.Module.hangarShipUID);
            DrawRectangle(s.ModuleRect, textC, color);
            DrawString(s.Center, 0, 0.4f, textC, s.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
        }

        void DrawArc(SpriteBatch batch, Weapon w, SlotStruct slot, Color color)
        {
            SubTexture arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);

            float multiplier = w.Range / 2500f;
            var texOrigin = new Vector2(250f, 250f);
            var size = new Vector2(500f, 500f) * multiplier;
            Rectangle rect = slot.Center.ToRect((int)size.X, (int)size.Y);

            float radians = slot.Module.Facing.ToRadians();
            batch.Draw(arcTexture, rect, color.Alpha(0.75f), radians, texOrigin, SpriteEffects.None, 1f);

            Vector2 direction = radians.RadiansToDirection();
            Vector2 start = slot.Center;
            Vector2 end = start + direction * w.Range*0.75f;
            batch.DrawLine(start, end, color.Alpha(0.1f), 5);

            Vector2 textPos = start.LerpTo(end, 0.2f);
            float textRot = radians + (float)(Math.PI/2);
            Vector2 offset = direction.RightVector() * 14f;
            if (direction.X > 0f)
            {
                textRot -= (float)Math.PI;
                offset = -offset;
            }

            string rangeText = $"Range: {w.Range.String(0)}";
            float textWidth = Fonts.Arial20Bold.TextWidth(rangeText);

            batch.DrawString(Fonts.Arial20Bold, rangeText,
                textPos+offset, color.Alpha(0.2f),
                textRot, new Vector2(textWidth/2, 10f), 1f, SpriteEffects.None, 1f);
        }

        void DrawWeaponArcs(SpriteBatch batch, SlotStruct slot)
        {
            Weapon w = slot.Module.InstalledWeapon;
            if (w == null)
                return;
            if (w.Tag_Cannon && !w.Tag_Energy)        DrawArc(batch, w, slot, new Color(255, 255, 0, 255));
            else if (w.Tag_Railgun || w.Tag_Subspace) DrawArc(batch, w, slot, new Color(255, 0, 255, 255));
            else if (w.Tag_Cannon)                    DrawArc(batch, w, slot, new Color(0, 255, 0, 255));
            else if (!w.isBeam)                       DrawArc(batch, w, slot, new Color(255, 0, 0, 255));
            else                                      DrawArc(batch, w, slot, new Color(0, 0, 255, 255));
        }

        void DrawActiveModule(SpriteBatch spriteBatch)
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

        void DrawDebug()
        {
            var pos = new Vector2(CenterX - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
            HelperFunctions.DrawDropShadowText(ScreenManager.SpriteBatch, "Debug", pos, Fonts.Arial20Bold);
            pos = new Vector2(CenterX - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
            HelperFunctions.DrawDropShadowText(ScreenManager.SpriteBatch, Operation.ToString(), pos, Fonts.Arial20Bold);
        }

        void DrawHullSelection(SpriteBatch batch)
        {
            Rectangle  r = HullSelectionSub.Menu;
            r.Y      += 25;
            r.Height -= 25;
            var sel = new Selector(r, new Color(0, 0, 0, 210));
            sel.Draw(batch);
            HullSL.Draw(batch);
            Vector2 mousePos = Mouse.GetState().Pos();
            HullSelectionSub.Draw(batch);

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
                    batch.Draw(ship.Icon, new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                    var tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    batch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
                    tCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    batch.DrawString(Fonts.Arial8Bold, Localizer.GetRole(ship.HullRole, EmpireManager.Player), tCursor, Color.Orange);

                    e.CheckHover(mousePos);
                }
            }
        }

        // @todo - need to make all these calcs in one place. Right now they are also done in Ship.cs
        void DrawShipInfoPanel()
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
                cost          += slot.Module.ActualCost;
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
                ordnanceUsed     += slot.Module.BayOrdnanceUsagePerSecond;

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

                Weapon weapon = slot.Module.InstalledWeapon;
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

            mass += (ActiveHull.ModuleSlots.Length);
            mass *= EmpireManager.Player.data.MassModifier;
            cost += (int)(cost * EmpireManager.Player.data.Traits.ShipCostMod);

            float powerRecharge = powerFlow - netPower.NetSubLightPowerDraw;
            float speed         = thrust / mass;
            float turn          = MathHelper.ToDegrees(turnThrust / mass / 700f);
            float warpSpeed     = (warpThrust / (mass + 0.1f)) * EmpireManager.Player.data.FTLModifier * bonus.SpeedModifier;  // Added by McShooterz: hull bonus speed;
            string warpString   = warpSpeed.GetNumberString();
            float modifiedSpeed = speed * EmpireManager.Player.data.SubLightModifier * bonus.SpeedModifier;
            float afterSpeed    = (afterThrust / (mass + 0.1f)) * EmpireManager.Player.data.SubLightModifier;

            var cursor = new Vector2((StatsSub.Menu.X + 10), (ShipStats.Menu.Y + 18));

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
            bool isOrbital = ActiveHull.Role == ShipData.RoleName.platform || ActiveHull.Role ==  ShipData.RoleName.station;
            if (isOrbital) offense /= 2;
            float strength = ShipBuilder.GetModifiedStrength(size, numWeaponSlots, offense, defense, ActiveHull.Role, turn);
            if (strength > 0) DrawStatColor(ref cursor, TintedValue(6190, strength, 227, Color.White));

            var cursorReq = new Vector2(StatsSub.Menu.X - 180, ShipStats.Menu.Y + Fonts.Arial12Bold.LineSpacing + 5);
            if (ActiveHull.Role != ShipData.RoleName.platform)
                DrawRequirement(ref cursorReq, Localizer.Token(120), hasBridge, 1983);

            DrawRequirement(ref cursorReq, Localizer.Token(121), emptySlots, 1982);

            void DrawHullBonuses()
            {
                if (bonus.Hull.NotEmpty()) //Added by McShooterz: Draw Hull Bonuses
                {
                    if (bonus.ArmoredBonus     != 0 || bonus.ShieldBonus != 0
                        || bonus.SensorBonus   != 0 || bonus.SpeedBonus != 0
                        || bonus.CargoBonus    != 0 || bonus.DamageBonus != 0
                        || bonus.FireRateBonus != 0 || bonus.RepairBonus != 0
                        || bonus.CostBonus != 0)
                    {
                        DrawString(cursor, Color.Orange, Localizer.Token(6015), Fonts.Verdana14Bold);
                        cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                    }

                    void HullBonus(float stat, string text)
                    {
                        if (stat > 0 || stat < 0) return;
                        DrawString(cursor, Color.Orange, $"{stat * 100f}%  {text}", Fonts.Verdana12);
                        cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
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
                cost += bonus.StartingCost * CurrentGame.Pace; // apply flat discount or extra price
                cost *= (1f - bonus.CostBonus); // now apply % discount
                DrawStatColor(ref cursor, TintedValue(109, cost, 99, Color.White));
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

        void DrawRequirement(ref Vector2 cursor, string words, bool met, int tooltipId = 0, float lineSpacing = 2)
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

        void DrawStatEnergy(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.LightSkyBlue, Color.LightGreen);
        }

        void DrawStatPropulsion(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.DarkSeaGreen, Color.LightGreen);
        }

        void DrawStatOrdnance(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen);
        }

        void DrawStatBad(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightPink);
        }

        void DrawStat(ref Vector2 cursor, string words, string stat, int tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen);
        }

        static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }

        void DrawUi()
        {
            EmpireUI.Draw(ScreenManager.SpriteBatch);
            DrawShipInfoPanel();

            CategoryList.Draw(ScreenManager.SpriteBatch);

            DrawTitle(ScreenWidth * 0.375f, "Repair Options");
            DrawTitle(ScreenWidth * 0.5f, "Behavior Presets");
            DrawTitle(ScreenWidth * 0.65f, "Hangar Designation");
            HangarOptionsList.Draw(ScreenManager.SpriteBatch);

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

            DrawHullSelection(ScreenManager.SpriteBatch);

            if (IsActive)
                ToolTip.Draw(ScreenManager.SpriteBatch);

            void DrawTitle(float x, string title)
            {
                int buttonHeight = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10;
                var pos = new Vector2(x, buttonHeight);
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, title, pos, Color.Orange);
            }
        }

        enum ValueTint
        {
            None,
            Bad,
            GoodBad
        }

        struct StatValue
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

        static StatValue NormalValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.None, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue BadValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.Bad, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue TintedValue(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue TintedValue(int titleId, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = Localizer.Token(titleId)+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue TintedPercent(string title, float value, int tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, IsPercent = true, Spacing = spacing, LineSpacing = lineSpacing };

        void DrawStatColor(ref Vector2 cursor, StatValue stat)
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