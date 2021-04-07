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
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);

            Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.StarField);
            ScreenManager.RenderSceneObjects();

            if (ToggleOverlay)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, Camera.Transform);
                DrawEmptySlots(batch);
                DrawModules(batch);
                DrawUnpoweredTex(batch);
                DrawTacticalOverlays(batch);
                DrawModuleSelections();
                DrawProjectedModuleRect();
                batch.End();
            }

            batch.Begin();
            if (ActiveModule != null && !ModuleSelectComponent.HitTest(Input))
            {
                DrawActiveModule(batch);
            }

            DrawTargetReference();

            
            DrawUi(batch, elapsed);
            ArcsButton.DrawWithShadowCaps(batch);
            switch (DesignIssues.CurrentWarningLevel)
            {
                case ShipDesignIssues.WarningLevel.None:                                                      break;
                case ShipDesignIssues.WarningLevel.Informative: InformationButton.DrawWithShadowCaps(batch);  break;
                default:                                        DesignIssuesButton.DrawWithShadowCaps(batch); break;
            }

            if (Debug)
                DrawDebug();

            base.Draw(batch, elapsed);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        void DrawTargetReference()
        {
            if (Camera.Zoom > 0.4f) return;
            float x = GlobalStats.XRES / 2f - CameraPosition.X * Camera.Zoom;
            float y = GlobalStats.YRES / 2f - CameraPosition.Y * Camera.Zoom;

            float focalPoint = Ship.TargetErrorFocalPoint;
            float radius = (focalPoint + shipSO.WorldBoundingSphere.Radius) * Camera.Zoom;
            float topEdge = y - radius;

            if (shipSO.WorldBoundingSphere.Radius < Ship.TargetErrorFocalPoint * 0.3f)
            {
                DrawRangeCircle(x, y, 0.25f);
            }

            float[] rangeCircles = new float[5] { 0.5f, 1, 2, 4, 6 };
            foreach(float range in rangeCircles)
            {
                if (!DrawRangeCircle(x, y, range)) break;
            }
        }

        bool DrawRangeCircle(float x, float y, float multiplier)
        {
            float focalPoint = Ship.TargetErrorFocalPoint * multiplier;
            float radius     = (focalPoint) * Camera.Zoom;
            float topEdge    = y - radius;
            float rightEdge  = x + radius;
            float leftEdge   = x - radius;
            float bottomEdge = y + radius;
            float thickness  = 0.5f + (multiplier / 7);

            Weapon weapon = ActiveModule?.InstalledWeapon ?? HighlightedModule?.InstalledWeapon;
            if (weapon == null && WeaponAccuracyList.Count > 0)
            {
                weapon = WeaponAccuracyList.FindMax((k, v) => v).Key.InstalledWeapon;
            }

            float weaponRange = weapon?.GetActualRange() ?? float.MaxValue;

            // weapon range is exceeded by range circle so draw weapon range circle instead
            if (weaponRange < focalPoint)
            {
                DrawCircle(new Vector2(x, y), weaponRange * Camera.Zoom, Color.OrangeRed.Alpha(0.5f), 1);
                DrawString(new Vector2(x, y - weaponRange * Camera.Zoom - 10), 0, 1, Color.OrangeRed.Alpha(0.5f), Localizer.Token(GameText.Range3) + " : " + weaponRange);

                radius = weaponRange * Camera.Zoom;
            }

            Vector2 source = new Vector2(x, y);
            Vector2 target = new Vector2(x, y - focalPoint);
            float error = weapon?.BaseTargetError((int)FireControlLevel, focalPoint, EmpireManager.Player) ?? 0;
            error = (weapon?.AdjustedImpactPoint(source, target, new Vector2(error, error)) - source ?? Vector2.Zero).X;

            if (weapon != null)
            {
                DrawCircle(new Vector2(x, y - radius), error * Camera.Zoom, Color.White, 1);
                DrawCircle(new Vector2(x, y + radius), error * Camera.Zoom, Color.White, 1);
                DrawCircle(new Vector2(x - radius, y), error * Camera.Zoom, Color.White, 1);
            }

            // range circle is within weapon range. so draw it. 
            if (weaponRange > focalPoint)
            {
                DrawCircle(new Vector2(x, y), radius, Color.Red, thickness);
                DrawString(new Vector2(rightEdge + 20, y), .95f, 1, Color.Red, $"{Localizer.Token(GameText.Range)} {focalPoint}");

                if (multiplier > 1)
                {
                    DrawCircle(new Vector2(x, topEdge), 100 * 8 * Camera.Zoom, Color.Red);
                    DrawString(new Vector2(x, topEdge + (20 + 100 * 8) * Camera.Zoom), 0, 1, Color.Red, Localizer.Token(GameText.Capital));
                }

                if (multiplier > 0.25f)
                {
                    DrawCircle(new Vector2(leftEdge, y), 30 * 8 * Camera.Zoom, Color.Red);
                    if (multiplier > 0.5 || Camera.Zoom > 0.2f)
                    {
                        DrawString(new Vector2(leftEdge, y + 20 + (30 * 8) * Camera.Zoom), 0, 1, Color.Red, Localizer.Token(GameText.Cruiser));
                    }
                }

                DrawCircle(new Vector2(x, bottomEdge), 10 * 8 * Camera.Zoom, Color.Red);
                DrawString(new Vector2(x, bottomEdge + (10 + 10 * 8) * Camera.Zoom), 0, 1, Color.Red, Localizer.Token(GameText.Fighter));
                return true;
            }
            return false;
        }

        bool GetSlotForModule(ShipModule module, out SlotStruct slot)
        {
            slot = module == null ? null : ModuleGrid.SlotsList.FirstOrDefault(s => s.Module == module);
            return slot != null;
        }

        void DrawModuleSelections()
        {
            if (GetSlotForModule(HighlightedModule, out SlotStruct highlighted))
            {
                DrawRectangle(highlighted.ModuleRect, Color.DarkOrange, 1.25f);
                if (IsSymmetricDesignMode && GetMirrorSlotStruct(highlighted, out SlotStruct mirrored))
                    DrawRectangle(mirrored.ModuleRect, Color.DarkOrange.Alpha(0.66f), 1.25f);
            }
        }

        void DrawProjectedModuleRect()
        {
            if (ProjectedSlot == null || ActiveModule == null)
                return;

            bool fits = ModuleGrid.ModuleFitsAtSlot(ProjectedSlot, ActiveModule);
            DrawRectangle(ProjectedSlot.GetProjectedRect(ActiveModule), fits ? Color.LightGreen : Color.Red, 1.5f);

            if (IsSymmetricDesignMode 
                && GetMirrorProjectedSlot(ProjectedSlot, ActiveModule.XSIZE, ActiveModule.Orientation, out SlotStruct mirrored))
            {
                bool mirrorFits = ModuleGrid.ModuleFitsAtSlot(mirrored, ActiveModule);
                DrawRectangle(mirrored.GetProjectedRect(ActiveModule), mirrorFits 
                    ? Color.LightGreen.Alpha(0.66f) : Color.Red.Alpha(0.66f), 1.5f);
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

        static void DrawModuleTex(ModuleOrientation orientation, SpriteBatch spriteBatch, 
            SlotStruct slot, Rectangle r, ShipModule template = null, float alpha = 1)
        {
            SpriteEffects effects = SpriteEffects.None;
            float rotation        = 0f;
            SubTexture texture    = template == null ? slot.Tex : ResourceManager.Texture(template.IconTexturePath);
            int xSize             = template == null ? slot.Module.XSIZE * 16 : r.Width;
            int ySize             = template == null ? slot.Module.YSIZE * 16 : r.Height;

            switch (orientation)
            {
                case ModuleOrientation.Left when !HelperFunctions.GetOrientedModuleTexture(template ?? slot.Module, ref texture, orientation):
                    {
                        int w    = xSize;
                        int h    = ySize;
                        r.Width  = h; // swap width & height
                        r.Height = w;
                        rotation = -1.57079637f;
                        r.Y     += h;
                        break;
                    }
                case ModuleOrientation.Right when !HelperFunctions.GetOrientedModuleTexture(template ?? slot.Module, ref texture, orientation):
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
                    HelperFunctions.GetOrientedModuleTexture(template ?? slot.Module, ref texture, orientation);
                    effects = SpriteEffects.FlipVertically;
                    break;
                case ModuleOrientation.Normal:
                    if (slot?.SlotReference.Position.X > 256f && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        effects = SpriteEffects.FlipHorizontally;

                    break;
            }

            spriteBatch.Draw(texture, r, Color.White.Alpha(alpha), rotation, Vector2.Zero, effects, 1f);
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

                DrawWeaponArcs(batch, s, FireControlLevel);

                if (IsSymmetricDesignMode && GetMirrorSlotStruct(s, out SlotStruct mirrored))
                {
                    DrawTacticalOverlays(mirrored);
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
                {
                    continue;
                }
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
            DrawString(slot.Center, 0, 1, Color.Orange, slot.Module.FacingDegrees.ToString(CultureInfo.CurrentCulture));
        }

        void DrawHangarShipText(SlotStruct s)
        {
            Color color = Color.Black.Alpha(0.33f);
            Color textC = ShipBuilder.GetHangarTextColor(s.Module.hangarShipUID);
            DrawRectangle(s.ModuleRect, textC, color);
            DrawString(s.Center, 0, 0.4f, textC, s.Module.hangarShipUID.ToString(CultureInfo.CurrentCulture));
        }

        static void DrawArc(SpriteBatch batch, float shipFacing, Weapon w, ShipModule m,
                            Vector2 posOnScreen, float sizeOnScreen, Color color, float level)
        {
            SubTexture arcTexture = Empire.Universe.GetArcTexture(m.FieldOfFire.ToDegrees());

            var texOrigin = new Vector2(250f, 250f);
            
            Rectangle rect = posOnScreen.ToRect((int)sizeOnScreen, (int)sizeOnScreen);

            float radians = (shipFacing + m.FacingRadians);
            batch.Draw(arcTexture, rect, color.Alpha(0.75f), radians, texOrigin, SpriteEffects.None, 1f);

            Vector2 direction = radians.RadiansToDirection();
            Vector2 start     = posOnScreen;
            Vector2 end = start + direction * sizeOnScreen;
            batch.DrawLine(start, start.LerpTo(end, 0.45f), color.Alpha(0.25f), 3);
            //

            end = start + direction * sizeOnScreen;
            
            Vector2 textPos = start.LerpTo(end, 0.16f);
            float textRot   = radians + RadMath.HalfPI;
            Vector2 offset  = direction.RightVector() * 6f;
            if (direction.X > 0f)
            {
                textRot -= RadMath.PI;
                offset = -offset;
            }

            string rangeText = $"Range: {w.BaseRange.String(0)}";
            float textWidth  = Fonts.Arial8Bold.TextWidth(rangeText);
            batch.DrawString(Fonts.Arial8Bold, rangeText,
                textPos + offset, color.Alpha(0.3f),
                textRot, new Vector2(textWidth / 2, 10f), 1f, SpriteEffects.None, 1f);
        }

        // @note This is reused in DebugInfoScreen as well
        public static void DrawWeaponArcs(SpriteBatch batch, float shipFacing, 
            Weapon w, ShipModule module, Vector2 posOnScreen, float sizeOnScreen, float shipLevel)
        {
            Color color;
            if (w.Tag_Cannon && !w.Tag_Energy)        color = new Color(255, 255, 0, 255);
            else if (w.Tag_Railgun || w.Tag_Subspace) color = new Color(255, 0, 255, 255);
            else if (w.Tag_Cannon)                    color = new Color(0, 255, 0, 255);
            else if (!w.isBeam)                       color = new Color(255, 0, 0, 255);
            else                                      color = new Color(0, 0, 255, 255);
            DrawArc(batch, shipFacing, w, module, posOnScreen, sizeOnScreen, color, shipLevel);
        }

        public static void DrawWeaponArcs(SpriteBatch batch, SlotStruct slot, float shipLevel)
        {
            Weapon w = slot.Module.InstalledWeapon;
            if (w == null)
                return;
            DrawWeaponArcs(batch, 0f, w, slot.Module, slot.Center, 500f, shipLevel);
        }

        void DrawWeaponArcs(SpriteBatch batch, ShipModule module, Vector2 screenPos, float facing = 0f)
        {
            Weapon w = module.InstalledWeapon;
            if (w == null)
                return;

            int cx = (int)(8f * module.XSIZE * Camera.Zoom);
            int cy = (int)(8f * module.YSIZE * Camera.Zoom);
            DrawWeaponArcs(batch, facing, module.InstalledWeapon, ActiveModule, screenPos + new Vector2(cx, cy), 500f, FireControlLevel);
        }

        void DrawActiveModule(SpriteBatch spriteBatch)
        {
            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            int width  = (int)(16f * ActiveModule.XSIZE * Camera.Zoom);
            int height = (int)(16f * ActiveModule.YSIZE * Camera.Zoom);
            var r = new Rectangle((int)Input.CursorX, (int)Input.CursorY, width, height);
            DrawModuleTex(ActiveModState, spriteBatch, null, r, moduleTemplate);
            DrawWeaponArcs(spriteBatch, ActiveModule, r.PosVec());

            int mirrorX = DrawActiveMirrorModule(spriteBatch, moduleTemplate, r.X);

            if (ActiveModule.shield_power_max.AlmostZero())
                return;

            Vector2 normalizeShieldCircle;
            var center = new Vector2(Input.CursorPosition.X, Input.CursorPosition.Y);
            if (ActiveModState == ModuleOrientation.Normal || ActiveModState == ModuleOrientation.Rear)
                normalizeShieldCircle = new Vector2(moduleTemplate.XSIZE * 8f, moduleTemplate.YSIZE * 8f);
            else
                normalizeShieldCircle = new Vector2(moduleTemplate.YSIZE * 8f, moduleTemplate.XSIZE * 8f);

            center += normalizeShieldCircle;
            DrawCircle(center, ActiveModule.ShieldHitRadius * Camera.Zoom, Color.LightGreen);
            if (IsSymmetricDesignMode)
            {
                Vector2 mirrorCenter = new Vector2(mirrorX, Input.CursorPosition.Y);
                mirrorCenter += normalizeShieldCircle;
                DrawCircle(mirrorCenter, ActiveModule.ShieldHitRadius * Camera.Zoom, Color.LightGreen.Alpha(0.5f));
            }
        }

        int DrawActiveMirrorModule(SpriteBatch spriteBatch, ShipModule moduleTemplate, int activeModuleX)
        {
            if (!IsSymmetricDesignMode)
                return 0;

            int effectiveWidth = ActiveModState == ModuleOrientation.Left || ActiveModState == ModuleOrientation.Right
                               ? moduleTemplate.YSIZE
                               : moduleTemplate.XSIZE;

            int res          = GlobalStats.XRES / 2;
            int zoomOffset   = (int)(effectiveWidth * 16 * (2.65 - Camera.Zoom)); // 2.65 is maximum zoom
            int cameraOffset = (int)(res - CameraPosition.X * Camera.Zoom);
            int mirrorX      = (int)(cameraOffset + (cameraOffset - Input.CursorPosition.X - 40 * effectiveWidth));
            mirrorX         += zoomOffset;

            if (MirroredModulesTooClose(mirrorX, activeModuleX, effectiveWidth))
                return 0;

            // Log.Info($"x: {Input.CursorPosition.X} Zoom: {Camera.Zoom} Mirror: {x} Camera: {CameraPosition.X}");
            var rMir = new Rectangle(mirrorX, (int)Input.CursorY,
                                    (int)(16 * ActiveModule.XSIZE * Camera.Zoom),
                                    (int)(16 * ActiveModule.YSIZE * Camera.Zoom));
            ModuleOrientation orientation = ActiveModState;
            switch (ActiveModState)
            {
                case ModuleOrientation.Left:  orientation = ModuleOrientation.Right; break;
                case ModuleOrientation.Right: orientation = ModuleOrientation.Left;  break;
            }

            DrawModuleTex(orientation, spriteBatch, null, rMir, moduleTemplate, 0.5f);
            float mirroredFacingOffset = 0f;
            if (orientation != ActiveModState)
            {
                // this is a hack... we pretend the ship is rotated 180 degrees to draw the arcs facing
                // the correct direction.... :D
                mirroredFacingOffset = ActiveModule.FacingRadians - ConvertOrientationToFacing(orientation).ToRadians();
            }
            DrawWeaponArcs(spriteBatch, ActiveModule, rMir.PosVec(), mirroredFacingOffset);
            return mirrorX;
        }

        bool MirroredModulesTooClose(int mirrorX, int activeModuleX, int effectiveWidth)
        {
            if (mirrorX > activeModuleX)
            {
                if (mirrorX + 20 * (2.65 - Camera.Zoom) - (activeModuleX + (effectiveWidth - 1) * 24) < 0)
                    return true;
            }
            else if (activeModuleX + 20 * (2.65 - Camera.Zoom) - (mirrorX + (effectiveWidth + 1) * 24) < 0)
                return true;

            return false;
        }

        void DrawDebug()
        {
            var pos = new Vector2(CenterX - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
            ScreenManager.SpriteBatch.DrawDropShadowText("Debug", pos, Fonts.Arial20Bold);
            pos = new Vector2(CenterX - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
            ScreenManager.SpriteBatch.DrawDropShadowText(Operation.ToString(), pos, Fonts.Arial20Bold);
        }

        // @todo - need to make all these calcs in one place. Right now they are also done in Ship.cs
        void DrawShipInfoPanel()
        {
            float hitPoints                = 0f;
            float powerCapacity            = 0f;
            float ordnanceCap              = 0f;
            float powerFlow                = 0f;
            float shieldPower              = 0f;
            float cargoSpace               = 0f;
            int size                       = 0;
            float warpableMass             = 0f;
            float warpDraw                 = 0f;
            float repairRate               = 0f;
            float sensorRange              = 0f;
            float sensorBonus              = 0f;
            float avgOrdnanceUsed          = 0f;
            float burstOrdnance            = 0f;
            float ordnanceRecovered        = 0f;
            float weaponPowerNeeded        = 0f;
            float empResist                = 0f;
            float offense                  = 0f;
            float defense                  = 0f;
            float targets                  = 0;
            float totalEcm                 = 0f;
            float beamPeakPowerNeeded      = 0f;
            float beamLongestDuration      = 0f;
            float weaponPowerNeededNoBeams = 0f;
            int numCommandModules          = 0;
            bool bEnergyWeapons            = false;
            int troopCount                 = 0;
            int accuracy                   = 0;
            int numTroopBays               = 0;
            int numSlots                   = 0;
            int numWeaponSlots             = 0;
            int numWeapons                 = 0;
            int numOrdnanceWeapons         = 0;
            int numHangarSlots             = 0;
            float totalShieldAmplify       = 0;
            bool unpoweredModules          = false;
            bool canTargetFighters         = false;
            bool canTargetCorvettes        = false;
            bool canTargetCapitals         = false;
            bool hasPowerCells             = false;
            int pointDefenseValue          = 0;
            int totalHangarArea            = 0;
            int maxWeaponRange             = 0;

            Map<ShipModule, float> weaponAccuracyList = new Map<ShipModule, float>();
            Array<float> weaponsPowerFirePerShot      = new Array<float>();
            DesignIssues.Reset();
            var modules = new ModuleCache(ModuleGrid.CopyModulesList());

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                bool wasOffenseDefenseAdded = false;
                size += 1;

                if (slot.Module == null)
                    continue;

                ShipModule module = slot.Module;

                numSlots      += module.Area;
                hitPoints     += module.ActualMaxHealth;
                troopCount    += module.TroopCapacity;
                powerCapacity += module.ActualPowerStoreMax;
                ordnanceCap   += module.OrdinanceCapacity;
                powerFlow     += module.ActualPowerFlowMax;
                cargoSpace    += module.Cargo_Capacity;
                hasPowerCells |= module.ModuleType == ShipModuleType.FuelCell && module.PowerStoreMax > 0;

                if (module.PowerDraw <= 0) // some modules might not need power to operate, we still need their offense
                {
                    offense  += module.CalculateModuleOffense();
                    defense  += module.CalculateModuleDefense(ModuleGrid.SlotsCount);
                    wasOffenseDefenseAdded = true;
                }
                else if (!module.Powered)
                {
                    unpoweredModules = true;
                    continue;
                }

                empResist          += module.EMP_Protection;
                warpableMass       += module.WarpMassCapacity;
                warpDraw           += module.PowerDrawAtWarp;
                shieldPower        += module.shield_power_max;
                totalShieldAmplify += module.AmplifyShields;
                repairRate         += module.ActualBonusRepairRate;
                ordnanceRecovered  += module.OrdnanceAddedPerSecond;
                accuracy            = module.TargetingAccuracy.LowerBound((int)accuracy);
                targets            += module.TargetTracking;
                avgOrdnanceUsed    += module.BayOrdnanceUsagePerSecond;
                totalEcm            = module.ECM.LowerBound(totalEcm);
                sensorRange         = module.SensorRange.LowerBound(sensorRange);
                sensorBonus         = module.SensorBonus.LowerBound(sensorBonus);
                totalHangarArea    += module.MaximumHangarShipSize;


                if (module.IsTroopBay)
                    numTroopBays += 1;

                if (module.IsCommandModule)
                    numCommandModules += 1;

                if (module.IsTroopBay || module.IsSupplyBay || module.MaximumHangarShipSize > 0)
                    numHangarSlots += module.Area;

                if (!wasOffenseDefenseAdded)
                {
                    offense += module.CalculateModuleOffense();
                    defense += module.CalculateModuleDefense(ModuleGrid.SlotsCount);
                }

                Weapon weapon = module.InstalledWeapon;
                if (weapon == null)
                    continue;

                if (weapon.PowerRequiredToFire > 0 || weapon.BeamPowerCostPerSecond > 0)
                    bEnergyWeapons = true;

                numWeaponSlots    += module.Area;
                avgOrdnanceUsed   += weapon.AverageOrdnanceUsagePerSecond;
                burstOrdnance     += weapon.BurstOrdnanceUsagePerSecond;
                weaponPowerNeeded += weapon.PowerFireUsagePerSecond;
                if (!weapon.TruePD)
                {
                    numWeapons += 1;
                    if (weapon.OrdinanceRequiredToFire > 0)
                        numOrdnanceWeapons += 1;
                }

                if (!weapon.Excludes_Fighters)
                    canTargetFighters = true;

                if (!weapon.Excludes_Corvettes)
                    canTargetCorvettes = true;

                if (!weapon.Excludes_Capitals)
                    canTargetCapitals = true;

                if (weapon.TruePD)
                    pointDefenseValue += 4;

                if (weapon.Tag_PD)
                    pointDefenseValue += 1;

                int range = (int)weapon.GetActualRange(EmpireManager.Player);
                if (range > maxWeaponRange)
                    maxWeaponRange = range;

                // added by Fat Bastard for Energy power calcs
                if (weapon.isBeam)
                {
                    beamPeakPowerNeeded += weapon.BeamPowerCostPerSecond;
                    beamLongestDuration  = Math.Max(beamLongestDuration, weapon.BeamDuration);
                }
                else
                {
                    weaponPowerNeededNoBeams += weapon.PowerFireUsagePerSecond; // FB: need non beam weapons power cost to add to the beam peak power cost
                    if (weapon.PowerRequiredToFire > 0)
                        weaponsPowerFirePerShot.Add(weapon.PowerRequiredToFire);
                }

                weaponAccuracyList[module] = weapon.Tag_Guided ? 0 :  weapon.BaseTargetError((int)FireControlLevel).LowerBound(1) / 16;
            }
            FireControlLevel   = accuracy;
            WeaponAccuracyList = weaponAccuracyList;
            FireControlLevel  += 1 + EmpireManager.Player.data.Traits.Militaristic + (ActiveHull.Role == ShipData.RoleName.platform ? 3 : 0);
            targets           += 1 + EmpireManager.Player.data.Traits.Militaristic + (ActiveHull.Role == ShipData.RoleName.platform ? 3 : 0);

            var stats = new ShipStats();
            stats.Update(modules.Modules, ActiveHull, EmpireManager.Player, 0,  size, 1);
            float shieldAmplifyPerShield = ShipUtils.GetShieldAmplification(modules.Amplifiers, modules.Shields);
            shieldPower                  = ShipUtils.UpdateShieldAmplification(modules.Amplifiers, modules.Shields);
            bool mainShieldsPresent      = modules.Shields.Any(s => s.ModuleType == ShipModuleType.Shield);
            Power netPower               = Power.Calculate(modules.Modules, EmpireManager.Player);

            // Other modification to the ship and draw values
            empResist += size; // FB: so the player will know the true EMP Tolerance
            
            float powerRecharge = powerFlow - netPower.NetSubLightPowerDraw;
            
            var cursor = new Vector2(StatsSub.X + 10, ShipStats.Y + 18);
            DrawHullBonuses(ref cursor, stats.Cost, ActiveHull.Bonuses);
            DrawUpkeepSizeMass(ref cursor, stats.Cost, size, stats.Mass, totalHangarArea, troopCount);
            WriteLine(ref cursor);

            DrawPowerConsumedAndRecharge(ref cursor, weaponPowerNeeded, powerRecharge, powerCapacity, out float powerConsumed);
            DrawPowerDrawAtWarp(ref cursor, stats.MaxFTLSpeed, powerFlow, netPower.NetWarpPowerDraw, out float fDrawAtWarp);
            DrawEnergyStats(ref cursor, bEnergyWeapons, powerConsumed, powerCapacity, out float energyDuration);
            DrawPeakPowerStats(ref cursor, beamLongestDuration, beamPeakPowerNeeded, weaponPowerNeededNoBeams, powerRecharge, 
                powerCapacity, out float burstEnergyDuration);

            DrawFtlTime(ref cursor, powerCapacity, fDrawAtWarp, stats.MaxFTLSpeed, out float fWarpTime);
            WriteLine(ref cursor);

            DrawHitPointsAndRepair(ref cursor, hitPoints, repairRate);
            DrawShieldsStats(ref cursor, totalShieldAmplify, shieldPower, shieldAmplifyPerShield, mainShieldsPresent);
            DrawEmpAndEcm(ref cursor, empResist, totalEcm);
            WriteLine(ref cursor);

            DrawWarpPropulsion(ref cursor, stats.MaxFTLSpeed, stats.FTLSpoolTime);
            DrawPropulsion(ref cursor, stats.MaxSTLSpeed, stats.TurnRadsPerSec.ToDegrees(), afterburnerSpd:0f);
            WriteLine(ref cursor);

            DrawOrdnanceAndTroops(ref cursor, ordnanceCap, avgOrdnanceUsed, ordnanceRecovered, troopCount, out float ammoTime);
            WriteLine(ref cursor);

            DrawCargoTargetsAndSensors(ref cursor, cargoSpace, targets, sensorRange, sensorBonus, ActiveHull.Bonuses);
            DrawOffense(ref cursor, offense, defense, size, numWeaponSlots + numHangarSlots);

            var cursorReq = new Vector2(StatsSub.X - 180, ShipStats.Y + Fonts.Arial12Bold.LineSpacing + 5);

            DrawCompletion(ref cursorReq, Localizer.Token(GameText.NoEmptySlots), numSlots, size, GameText.InOrderToCompleteYour);
            CheckDesignIssues();

            void CheckDesignIssues()
            {
                if (PercentComplete(numSlots, size) < 0.75f)
                    return;

                DesignIssues.CheckIssueNoCommand(numCommandModules);
                DesignIssues.CheckIssueBackupCommand(numCommandModules, size);
                DesignIssues.CheckIssueUnpoweredModules(unpoweredModules);
                DesignIssues.CheckIssueOrdnance(avgOrdnanceUsed, ordnanceRecovered, ammoTime);
                DesignIssues.CheckIssuePowerRecharge(bEnergyWeapons, powerRecharge, powerCapacity, powerConsumed);
                DesignIssues.CheckPowerRequiredToFireOnce(weaponsPowerFirePerShot, powerCapacity);
                DesignIssues.CheckIssueOrdnanceBurst(burstOrdnance, ordnanceCap);
                DesignIssues.CheckIssueLowWarpTime(fDrawAtWarp, fWarpTime, stats.MaxFTLSpeed);
                DesignIssues.CheckIssueNoWarp(stats.MaxSTLSpeed, stats.MaxFTLSpeed);
                DesignIssues.CheckIssueSlowWarp(stats.MaxFTLSpeed);
                DesignIssues.CheckIssueNoSpeed(stats.MaxSTLSpeed);
                DesignIssues.CheckTargetExclusions(numWeaponSlots > 0, canTargetFighters, canTargetCorvettes, canTargetCapitals);
                DesignIssues.CheckTruePD(size, pointDefenseValue);
                DesignIssues.CheckWeaponPowerTime(bEnergyWeapons, powerConsumed > 0, energyDuration);
                DesignIssues.CheckCombatEfficiency(powerConsumed, energyDuration, powerRecharge, numWeapons, numOrdnanceWeapons);
                DesignIssues.CheckExcessPowerCells(beamPeakPowerNeeded > 0, burstEnergyDuration, powerConsumed, hasPowerCells);
                DesignIssues.CheckBurstPowerTime(beamPeakPowerNeeded > 0, burstEnergyDuration);
                DesignIssues.CheckOrdnanceVsEnergyWeapons(numWeapons, numOrdnanceWeapons, avgOrdnanceUsed, ordnanceRecovered);
                DesignIssues.CheckTroopsVsBays(troopCount, numTroopBays);
                DesignIssues.CheckTroops(troopCount, size);
                DesignIssues.CheckAccuracy(WeaponAccuracyList);
                DesignIssues.CheckTargets(WeaponAccuracyList, targets);
                DesignIssues.CheckSecondaryCarrier(totalHangarArea > 0, Role, maxWeaponRange);
                DesignIssues.CheckDedicatedCarrier(totalHangarArea > 0, Role, maxWeaponRange, (int)sensorRange,
                    ActiveHull.CombatState == CombatState.ShortRange || ActiveHull.CombatState == CombatState.AttackRuns);

                UpdateDesignButton();
            }
        }

        bool Stationary => ActiveHull.HullRole == ShipData.RoleName.station || ActiveHull.HullRole == ShipData.RoleName.platform;

        float PercentComplete(int numSlots, int size) => DesignComplete(numSlots, size) ? 1f : numSlots / (float)size;
        bool DesignComplete(int numSlots, int size)   => numSlots == size;

        void DrawCompletion(ref Vector2 cursor, string words, int numSlots, int size, ToolTipText tooltip, float lineSpacing = 2)
        {
            float amount    = 165f;
            SpriteFont font = Fonts.Arial12Bold;
            cursor.Y += lineSpacing > 0 ? font.LineSpacing + lineSpacing : 0;
            ScreenManager.SpriteBatch.DrawString(font, words, cursor, Color.White);
            float percentComplete = PercentComplete(numSlots, size);
            string stats          = (percentComplete * 100).String(0) + "%";
            cursor.X += amount - font.MeasureString(stats).X;
            ScreenManager.SpriteBatch.DrawString(font, stats, cursor, TextColor(percentComplete));
            cursor.X -= amount - font.MeasureString(stats).X;
            if (tooltip.IsValid) 
                CheckToolTip(tooltip, cursor, words, stats, font, MousePos);

            Color TextColor(float percent)
            {
                Color color;
                if (percent.AlmostEqual(1)) color = Color.LightGreen;
                else if (percent < 0.33f)     color = Color.Red;
                else if (percent < 0.66f)     color = Color.Orange;
                else                          color = Color.Yellow;

                return color;
            }
        }

        void DrawOffense(ref Vector2 cursor, float offense, float defense, int size, int numWeaponSlots)
        {
            bool isOrbital = ActiveHull.Role == ShipData.RoleName.platform || ActiveHull.Role == ShipData.RoleName.station;
            if (isOrbital) 
                offense /= 2;

            float strength = ShipBuilder.GetModifiedStrength(size, numWeaponSlots, offense, defense);
            if (strength > 0)
            {
                DrawStatColor(ref cursor, TintedValue(GameText.ShipOffense, strength, GameText.EstimatedOffensiveStrengthOfThe, Color.White));
                float relativeStrength = (float)Math.Round(strength / ActiveHull.ModuleSlots.Length, 2);
                DrawStatColor(ref cursor, TintedValue(GameText.RelativeStrength, relativeStrength, GameText.ThisIsTheStrengthOf, Color.White));
            }
        }

        void DrawPeakPowerStats(ref Vector2 cursor, float beamLongestDuration, float beamPeakPowerNeeded, 
            float weaponPowerNeededNoBeams, float powerRecharge, float powerCapacity, out float burstEnergyDuration)
        {
            // FB: @todo  using Beam Longest Duration for peak power calculation in case of variable beam durations in the ship will show the player he needs
            // more power than actually needed. Need to find a better way to show accurate numbers to the player in such case
            float powerConsumedWithBeams = beamPeakPowerNeeded + weaponPowerNeededNoBeams - powerRecharge;
            burstEnergyDuration          = powerCapacity / powerConsumedWithBeams;
            if (!(beamLongestDuration > 0))
                return;

            if (!(powerConsumedWithBeams > 0))
                return;

            DrawStatColor(ref cursor, NormalValue("Burst Wpn Pwr Drain", -powerConsumedWithBeams, GameText.ThisIndicatesThatThereIs, Color.LightSkyBlue));
            if (burstEnergyDuration < beamLongestDuration)
                DrawStatColor(ref cursor, BadValue("Burst Wpn Pwr Time", burstEnergyDuration, GameText.TheTotalTimeTheShip, Color.LightSkyBlue));
            else
                DrawStatEnergy(ref cursor, "Burst Wpn Pwr Time:", "INF", GameText.TheTotalTimeTheShip);
        }

        void DrawEnergyStats(ref Vector2 cursor, bool bEnergyWeapons, float powerConsumed, float powerCapacity, 
            out float weaponFirePowerTime)
        {
            weaponFirePowerTime = 0;
            if (!bEnergyWeapons)
                return;

            if (powerConsumed > 0) // There is power drain from ship's reserves when firing its energy weapons after taking into account recharge
            {
                DrawStatColor(ref cursor, NormalValue("Excess Wpn Pwr Drain", -powerConsumed, GameText.ThisIndicatesThatThereIs2, Color.LightSkyBlue));
                weaponFirePowerTime = powerCapacity / powerConsumed;
                DrawStatColor(ref cursor, LowBadValue("Wpn Fire Power Time", weaponFirePowerTime, GameText.IndicatesTheMaximumTimeIn, Color.LightSkyBlue));
            }
            else
                DrawStatEnergy(ref cursor, "Wpn Fire Power Time:", "INF", GameText.IndicatesTheMaximumTimeIn);
        }

        void DrawOrdnanceAndTroops(ref Vector2 cursor, float ordnanceCap, float ordnanceUsed, float ordnanceRecovered, 
            float troopCount, out float ammoTime)
        {
            ammoTime = ordnanceCap / (ordnanceUsed - ordnanceRecovered);
            if (ordnanceRecovered > 0) DrawStatColor(ref cursor, TintedValue("Ordnance Created / s", ordnanceRecovered, GameText.IndicatesTheAmountOfOrdnance3, Color.IndianRed));
            if (!(ordnanceCap > 0))
                return;

            DrawStatColor(ref cursor, TintedValue(GameText.OrdnanceCapacity, ordnanceCap, GameText.IndicatesTheMaximumAmountOf3, Color.IndianRed));
            if (ordnanceUsed - ordnanceRecovered > 0)
                DrawStatColor(ref cursor, TintedValue("Ammo Time", ammoTime, GameText.IndicatesTheMaximumTimeIn2, Color.IndianRed));
            else
                DrawStatOrdnance(ref cursor, "Ammo Time", "INF", GameText.IndicatesTheMaximumTimeIn2);

            if (troopCount > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.TroopCapacity, troopCount, GameText.IndicatesTheTotalComplementOf, Color.IndianRed));
        }

        void DrawPropulsion(ref Vector2 cursor, float modifiedSpeed, float turn, float afterburnerSpd)
        {
            if (Stationary)
                return;

            DrawStatColor(ref cursor, TintedValue(GameText.SublightSpeed, modifiedSpeed, GameText.IndicatesTheDistanceThisShip, Color.DarkSeaGreen));
            DrawStatColor(ref cursor, TintedValue(GameText.TurnRate, turn, GameText.IndicatesTheNumberOfDegrees, Color.DarkSeaGreen));
            if (afterburnerSpd > 0) 
                DrawStatColor(ref cursor, TintedValue("Afterburner Speed", afterburnerSpd, GameText.IndicatesTheDistanceThisShip, Color.DarkSeaGreen));
        }

        void DrawWarpPropulsion(ref Vector2 cursor, float warpSpeed, float warpSpoolTimer)
        {
            if (Stationary)
                return;

            string warpString = warpSpeed.GetNumberString();
            DrawStatPropulsion(ref cursor, Localizer.Token(GameText.FtlSpeed) + ":", warpString, GameText.IndicatesTheDistanceThisShip3);
        }

        void DrawEmpAndEcm(ref Vector2 cursor, float empResist, float totalEcm)
        {
            if (empResist > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.EmpProtection, empResist, GameText.TheTotalEmpProtectionOf, Color.Goldenrod));

            if (totalEcm > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.Ecm3, totalEcm, GameText.ThisIsTheTotalElectronic, Color.Goldenrod));
        }

        void DrawShieldsStats(ref Vector2 cursor, float totalShieldAmplify, float shieldPower, float shieldAmplifyPerShield, bool mainShieldsPresent)
        {
            Color shieldMaxColor = totalShieldAmplify > 0 && mainShieldsPresent ? Color.Gold : Color.Goldenrod;
            if (shieldPower > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.ShieldPower, shieldPower, GameText.IndicatesTheTotalHitpointsOf2, shieldMaxColor));

            if (shieldAmplifyPerShield > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.ShieldAmplify, (int)shieldAmplifyPerShield, GameText.EachOfTheShipShields, Color.Goldenrod));
        }

        void DrawHitPointsAndRepair(ref Vector2 cursor, float hitPoints, float repairRate)
        {
            DrawStatColor(ref cursor, TintedValue(GameText.TotalHitpoints, hitPoints, GameText.IndicatesTheTotalHitpointsOf, Color.Goldenrod));
            // Added by McShooterz: draw total repair
            if (repairRate > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.RepairRate, repairRate, GameText.ThisIsThisShipsSelfrepair, Color.Goldenrod)); 
        }

        void DrawFtlTime(ref Vector2 cursor, float powerCapacity, float fDrawAtWarp, float warpSpeed, out float fWarpTime)
        {
            fWarpTime = -powerCapacity / fDrawAtWarp;
            if (warpSpeed.AlmostZero() || Stationary)
                return;

            if (fDrawAtWarp < 0)
                DrawStatColor(ref cursor, TintedValue("FTL Time", fWarpTime, GameText.IndicatesThisShipsMaximumSustained, Color.LightSkyBlue));
            else if (fWarpTime > 900)
                DrawStatEnergy(ref cursor, "FTL Time:", "INF", GameText.IndicatesThisShipsMaximumSustained);
            else
                DrawStatEnergy(ref cursor, "FTL Time:", "INF", GameText.IndicatesThisShipsMaximumSustained);
        }

        //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
        void DrawPowerDrawAtWarp(ref Vector2 cursor, float warpSpeed, float powerFlow, float netWarpPowerDraw, out float fDrawAtWarp)
        {
            fDrawAtWarp = powerFlow - netWarpPowerDraw;
            if (Stationary)
                return;

            if (warpSpeed > 0)
                DrawStatColor(ref cursor, TintedValue(GameText.RechargeAtWarp, fDrawAtWarp, GameText.IndicatesTheNetPowerFlow2, Color.LightSkyBlue));
        }

        void DrawPowerConsumedAndRecharge(ref Vector2 cursor, float weaponPowerNeeded, float powerRecharge, 
            float powerCapacity, out float powerConsumed)
        {
            powerConsumed = weaponPowerNeeded - powerRecharge;
            DrawStatColor(ref cursor, CompareValues(GameText.PowerCapacity, powerCapacity, powerConsumed, GameText.IndicatesTheMaximumAmountOf2, Color.LightSkyBlue));
            DrawStatColor(ref cursor, TintedValue(GameText.PowerRecharge, powerRecharge, GameText.IndicatesTheNetPowerFlow, Color.LightSkyBlue));
        }

        void DrawUpkeepSizeMass(ref Vector2 cursor, float cost, int size, float mass, int totalHangarArea, int troopCount)
        {
            float upkeep = GetMaintenanceCost(ActiveHull, (int)cost, totalHangarArea, troopCount);

            DrawStatColor(ref cursor, TintedValue("Upkeep Cost", upkeep, GameText.IndicatesTheCreditsPerTick, Color.White));
            DrawStatColor(ref cursor, TintedValue("Total Module Slots", size, GameText.TheTotalNumberOfModule, Color.White));
            DrawStatColor(ref cursor, TintedValue(GameText.Mass, (int)mass, GameText.AShipsTotalMassDetermines, Color.White));
        }

        void DrawCargoTargetsAndSensors(ref Vector2 cursor, float cargoSpace, float targets, float sensorRange, 
            float sensorBonus, HullBonus bonus)
        {
            if (cargoSpace > 0) 
                DrawStatColor(ref cursor, TintedValue(GameText.CargoSpace, cargoSpace* bonus.CargoModifier, GameText.IndicatesTheTotalCargoSpace, Color.White));

            if (FireControlLevel > 0)
                DrawStatColor(ref cursor, TintedValue(GameText.FireControl, FireControlLevel, GameText.FireControlSystemsOrFcs, Color.White));

            if (targets > 0)
                DrawStatColor(ref cursor, TintedValue(GameText.FcsPower, targets, GameText.ThisIsTheTotalNumber, Color.White));

            if (sensorRange > 0)
            {
                float modifiedSensorRange = (sensorRange + sensorBonus) * bonus.SensorModifier;
                DrawStatColor(ref cursor, TintedValue(GameText.SensorRange3, modifiedSensorRange, GameText.ThisIsTheMaximumSensor, Color.White));
            }

            
        }

        void DrawHullBonuses(ref Vector2 cursor, float cost, HullBonus bonus)
        {
            if (bonus.Hull.NotEmpty()) //Added by McShooterz: Draw Hull Bonuses
            {
                if (bonus.ArmoredBonus != 0 || bonus.ShieldBonus != 0
                    || bonus.SensorBonus != 0 || bonus.SpeedBonus != 0
                    || bonus.CargoBonus != 0 || bonus.DamageBonus != 0
                    || bonus.FireRateBonus != 0 || bonus.RepairBonus != 0
                    || bonus.CostBonus != 0)
                {
                    DrawString(cursor, Color.Orange, Localizer.Token(GameText.HullBonus), Fonts.Verdana14Bold);
                    cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                }

                void HullBonus(ref Vector2 bCursor, float stat, string text)
                {
                    if (stat > 0 || stat < 0)
                        return;
                    DrawString(bCursor, Color.Orange, $"{stat * 100f}%  {text}", Fonts.Verdana12);
                    bCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                }
                HullBonus(ref cursor, bonus.ArmoredBonus, Localizer.HullArmorBonus);
                HullBonus(ref cursor, bonus.ShieldBonus, Localizer.HullShieldBonus);
                HullBonus(ref cursor, bonus.SensorBonus, Localizer.HullSensorBonus);
                HullBonus(ref cursor, bonus.SpeedBonus, Localizer.HullSpeedBonus);
                HullBonus(ref cursor, bonus.CargoBonus, Localizer.HullCargoBonus);
                HullBonus(ref cursor, bonus.DamageBonus, Localizer.HullDamageBonus);
                HullBonus(ref cursor, bonus.FireRateBonus, Localizer.HullFireRateBonus);
                HullBonus(ref cursor, bonus.RepairBonus, Localizer.HullRepairBonus);
                HullBonus(ref cursor, bonus.CostBonus, Localizer.HullCostBonus);
            }
            DrawStatColor(ref cursor, TintedValue(GameText.ProductionCost, cost, GameText.IndicatesTheTotalProductionValue, Color.White));
        }

        public void DrawStat(ref Vector2 cursor, string words, string stat, ToolTipText tooltip, Color nameColor, Color statColor, float spacing = 165f, float lineSpacing = 2)
        {
            SpriteFont font = Fonts.Arial12Bold;
            cursor.Y += lineSpacing > 0 ? font.LineSpacing + lineSpacing : 0;

            var statCursor = new Vector2(cursor.X + spacing, cursor.Y);
            Vector2 statNameCursor = FontSpace(statCursor, -20, words, font);

            DrawString(statNameCursor, nameColor, words, font);
            DrawString(statCursor, statColor, stat, font);
            CheckToolTip(tooltip, cursor, words, stat, font, MousePos);
        }

        public void DrawStat(ref Vector2 cursor, string words, float stat, Color color, ToolTipText tooltipId, bool doGoodBadTint = true, bool isPercent = false, float spacing = 165)
        {
            StatValue sv = isPercent ? TintedPercent(words, stat, tooltipId, color, spacing, 0)
                                     : TintedValue(words, stat, tooltipId, color, spacing, 0);
            DrawStatColor(ref cursor, sv);
        }

        public void DrawStat(ref Vector2 cursor, string words, float stat, Color color, ToolTipText tooltipId, float spacing = 165)
        {
            StatValue sv = PercentBadLower1(words, stat, tooltipId, color, spacing, 0);
            DrawStatColor(ref cursor, sv);
        }

        public void DrawStatBadPercentLower1(ref Vector2 cursor, string words, float stat, Color color, ToolTipText tooltipId, float spacing = 165)
        {
            StatValue sv = PercentBadLower1(words, stat, tooltipId, color, spacing, 0);
            DrawStatColor(ref cursor, sv);
        }

        void DrawStatEnergy(ref Vector2 cursor, string words, string stat, ToolTipText tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.LightSkyBlue, Color.LightGreen);
        }

        void DrawStatPropulsion(ref Vector2 cursor, string words, string stat, ToolTipText tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.DarkSeaGreen, Color.LightGreen);
        }

        void DrawStatOrdnance(ref Vector2 cursor, string words, string stat, ToolTipText tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.IndianRed, Color.LightGreen);
        }

        void DrawStatBad(ref Vector2 cursor, string words, string stat, ToolTipText tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightPink);
        }

        void DrawStat(ref Vector2 cursor, string words, string stat, ToolTipText tooltipId)
        {
            DrawStat(ref cursor, words, stat, tooltipId, Color.White, Color.LightGreen);
        }

        static void WriteLine(ref Vector2 cursor, int lines = 1)
        {
            cursor.Y += Fonts.Arial12Bold.LineSpacing * lines;
        }
        
        static void DrawTitle(SpriteBatch batch, float x, string title)
        {
            int buttonHeight = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height + 10;
            var pos = new Vector2(x, buttonHeight);
            batch.DrawString(Fonts.Arial14Bold, title, pos, Color.Orange);
        }

        void DrawUi(SpriteBatch batch, DrawTimes elapsed)
        {
            EmpireUI.Draw(batch);
            DrawShipInfoPanel();

            CategoryList.Draw(batch, elapsed);

            DrawTitle(batch, ScreenWidth * 0.375f, "Repair Options");
            DrawTitle(batch, ScreenWidth * 0.65f, "Hangar Designation");
            HangarOptionsList.Draw(batch, elapsed);

            float transitionOffset = (float) Math.Pow(TransitionPosition, 2);

            Rectangle r = BlackBar;
            if (IsTransitioning)
                r.Y += (int)(transitionOffset * 50f);
            batch.FillRectangle(r, Color.Black);


            r = BottomSep;
            if (IsTransitioning)
                r.Y += (int) (transitionOffset * 50f);
            batch.FillRectangle(r, new Color(77, 55, 25));


            r = SearchBar;
            if (IsTransitioning)
                r.Y += (int)(transitionOffset * 50f);
            batch.FillRectangle(r, new Color(54, 54, 54));


            SpriteFont font = Fonts.Arial20Bold.MeasureString(ActiveHull.Name).X <= (SearchBar.Width - 5)
                            ? Fonts.Arial20Bold : Fonts.Arial12Bold;
            var cursor1 = new Vector2(SearchBar.X + 3, r.Y + 14 - font.LineSpacing / 2);
            batch.DrawString(font, ActiveHull.Name, cursor1, Color.White);


            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            batch.FillRectangle(r, new Color(54, 54, 54));

            var cursor = new Vector2(r.X + 3, r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
        }

        enum ValueTint
        {
            None,
            Bad,
            GoodBad,
            BadLowerThan2,
            BadPercentLowerThan1,
            CompareValue
        }

        struct StatValue
        {
            public LocalizedText Title;
            public Color TitleColor;
            public float Value;
            public float CompareValue;
            public ToolTipText Tooltip;
            public ValueTint Tint;
            public bool IsPercent;
            public float Spacing;
            public int LineSpacing;

            public Color ValueColor
            {
                get
                {
                    switch (Tint)
                    {
                        case ValueTint.GoodBad:              return Value > 0f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.Bad:                  return Color.LightPink;
                        case ValueTint.BadLowerThan2:        return Value > 2f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.BadPercentLowerThan1: return Value > 1f ? Color.LightGreen : Color.LightPink;
                        case ValueTint.CompareValue:         return CompareValue < Value ? Color.LightGreen : Color.LightPink;
                        case ValueTint.None:
                        default: return Color.White;
                    }
                }
            }


            public string ValueText => IsPercent ? Value.ToString("P0") : Value.GetNumberString();
        }

        static StatValue NormalValue(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.None, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue BadValue(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.Bad, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue TintedValue(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title.Text+":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.GoodBad, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue LowBadValue(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title + ":", Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.BadLowerThan2, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue CompareValues(LocalizedText title, float value, float compareValue, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title.Text+":", Value = value, CompareValue = compareValue, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.CompareValue, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue TintedPercent(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor,  Tint = ValueTint.GoodBad, IsPercent = true, Spacing = spacing, LineSpacing = lineSpacing };

        static StatValue PercentBadLower1(LocalizedText title, float value, ToolTipText tooltip, Color titleColor, float spacing = 165, int lineSpacing = 1)
            => new StatValue { Title = title, Value = value, Tooltip = tooltip, TitleColor = titleColor, Tint = ValueTint.BadPercentLowerThan1, IsPercent = true, Spacing = spacing, LineSpacing = lineSpacing };

        void DrawStatColor(ref Vector2 cursor, StatValue stat)
        {
            SpriteFont font = Fonts.Arial12Bold;
            //const float spacing = 165f;

            WriteLine(ref cursor);
            cursor.Y += stat.LineSpacing;

            Vector2 statCursor = new Vector2(cursor.X + stat.Spacing, cursor.Y);
            string title = stat.Title.Text;
            DrawString(FontSpace(statCursor, -20, title, font), stat.TitleColor, title, font); // @todo Replace with DrawTitle?

            string valueText = stat.ValueText;
            DrawString(statCursor, stat.ValueColor, valueText, font);
            CheckToolTip(stat.Tooltip, cursor, title, valueText, font, MousePos);
        }
    }
}
