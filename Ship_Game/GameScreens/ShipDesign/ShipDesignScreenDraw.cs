using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
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

            DrawTargetReference(DesignedShip.DesignStats.WeaponAccuracies);
            
            DrawUi(batch, elapsed);
            ArcsButton.DrawWithShadowCaps(batch);

            if (HullEditMode)
                DrawDebug(batch);

            base.Draw(batch, elapsed);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        void DrawTargetReference(Map<ShipModule, float> weaponAccuracies)
        {
            if (Camera.Zoom > 0.4f) return;
            float x = GlobalStats.XRES / 2f - CameraPosition.X * Camera.Zoom;
            float y = GlobalStats.YRES / 2f - CameraPosition.Y * Camera.Zoom;

            float focalPoint = Ship.TargetErrorFocalPoint;
            float radius = (focalPoint + shipSO.WorldBoundingSphere.Radius) * Camera.Zoom;
            float topEdge = y - radius;

            if (shipSO.WorldBoundingSphere.Radius < Ship.TargetErrorFocalPoint * 0.3f)
            {
                DrawRangeCircle(x, y, 0.25f, weaponAccuracies);
            }

            float[] rangeCircles = new float[5] { 0.5f, 1, 2, 4, 6 };
            foreach(float range in rangeCircles)
                if (!DrawRangeCircle(x, y, range, weaponAccuracies))
                    break;
        }

        bool DrawRangeCircle(float x, float y, float multiplier, Map<ShipModule, float> weaponAccuracies)
        {
            float focalPoint = Ship.TargetErrorFocalPoint * multiplier;
            float radius     = (focalPoint) * Camera.Zoom;
            float topEdge    = y - radius;
            float rightEdge  = x + radius;
            float leftEdge   = x - radius;
            float bottomEdge = y + radius;
            float thickness  = 0.5f + (multiplier / 7);

            Weapon weapon = ActiveModule?.InstalledWeapon ?? HighlightedModule?.InstalledWeapon;
            if (weapon == null && weaponAccuracies.Count > 0)
            {
                weapon = weaponAccuracies.FindMax((k, v) => v).Key.InstalledWeapon;
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
            float error = weapon?.BaseTargetError(DesignedShip.TargetingAccuracy, focalPoint, EmpireManager.Player) ?? 0;
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
            else if (HullEditMode)
            {
                Vector2 cursor = Camera.GetWorldSpaceFromScreenSpace(Input.CursorPosition);
                cursor = WorldToDesignCoords(cursor);
                DrawRectangle(new RectF(cursor, new Vector2(16f,16f)), Color.DarkOrange, 1.25f);
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
                batch.DrawString(Fonts.Arial20Bold, " " + slot.Restrictions, slot.PosVec2,
                                 Color.Navy, 0f, Vector2.Zero, 0.4f);
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
                    if (slot?.XMLPos.X > 256f && slot.Module.ModuleType != ShipModuleType.PowerConduit)
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

                DrawWeaponArcs(batch, s, DesignedShip.TargetingAccuracy);

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
            DrawString(slot.Center, 0, 1, Color.Orange, slot.Module.FacingDegrees.String(0));
        }

        void DrawHangarShipText(SlotStruct s)
        {
            string hangarShipUID = s.Module.hangarShipUID;
            Color color = Color.Black.Alpha(0.33f);
            Color textC = ShipBuilder.GetHangarTextColor(hangarShipUID);
            DrawRectangle(s.ModuleRect, textC, color);

            if (ResourceManager.GetShipTemplate(hangarShipUID, out Ship hangarShip))
            {
                DrawString(s.Center, 0, 0.4f, textC, hangarShip.Name);
            }
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
            batch.DrawString(Fonts.Arial8Bold, rangeText, textPos + offset, color.Alpha(0.3f),
                             textRot, new Vector2(textWidth / 2, 10f));
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
            DrawWeaponArcs(batch, facing, module.InstalledWeapon, ActiveModule, screenPos + new Vector2(cx, cy), 500f, DesignedShip.TargetingAccuracy);
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

        void DrawDebug(SpriteBatch batch)
        {
            string title = "HULL EDIT MODE";
            var pos = new Vector2(CenterX - Fonts.Arial20Bold.TextWidth(title) * 0.5f, 120f);
            batch.DrawDropShadowText(title, pos, Fonts.Arial20Bold);
            pos = new Vector2(CenterX - Fonts.Arial20Bold.TextWidth(Operation.ToString()) * 0.5f, 140f);
            batch.DrawDropShadowText(Operation.ToString(), pos, Fonts.Arial20Bold);
        }

        // TODO: Is this used anywhere?
        void DrawHullBonuses(ref Vector2 cursor, float cost)
        {
            HullBonus bonus = ActiveHull.Bonuses;
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

                void HullBonus(ref Vector2 bCursor, float stat, in LocalizedText text)
                {
                    if (stat > 0 || stat < 0)
                        return;
                    DrawString(bCursor, Color.Orange, $"{stat * 100f}%  {text.Text}", Fonts.Verdana12);
                    bCursor.Y += Fonts.Arial12Bold.LineSpacing + 2;
                }
                HullBonus(ref cursor, bonus.ArmoredBonus, GameText.ArmorProtection);
                HullBonus(ref cursor, bonus.ShieldBonus, "Shield Strength");
                HullBonus(ref cursor, bonus.SensorBonus, GameText.ArmorProtection);
                HullBonus(ref cursor, bonus.SpeedBonus, GameText.MaxSpeed);
                HullBonus(ref cursor, bonus.CargoBonus, GameText.CargoSpace2);
                HullBonus(ref cursor, bonus.DamageBonus, "Weapon Damage");
                HullBonus(ref cursor, bonus.FireRateBonus, GameText.FireRate);
                HullBonus(ref cursor, bonus.RepairBonus, GameText.RepairRate);
                HullBonus(ref cursor, bonus.CostBonus, GameText.CostReduction);
            }
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


            Graphics.Font font = Fonts.Arial20Bold.MeasureString(ActiveHull.Name).X <= (SearchBar.Width - 5)
                            ? Fonts.Arial20Bold : Fonts.Arial12Bold;
            var cursor1 = new Vector2(SearchBar.X + 3, r.Y + 14 - font.LineSpacing / 2);
            batch.DrawString(font, ActiveHull.Name, cursor1, Color.White);


            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            batch.FillRectangle(r, new Color(54, 54, 54));

            var cursor = new Vector2(r.X + 3, r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
        }
    }
}
