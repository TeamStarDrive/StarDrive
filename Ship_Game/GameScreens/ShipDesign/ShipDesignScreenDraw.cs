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
            Empire.Universe.DrawStarField();
            
            ScreenManager.RenderSceneObjects();

            if (ToggleOverlay)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
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

            DrawUi(batch, elapsed);
            ArcsButton.DrawWithShadowCaps(batch);

            if (HullEditMode)
                DrawDebug(batch);

            base.Draw(batch, elapsed);
            batch.End();
            ScreenManager.EndFrameRendering();
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
                DrawRectangleProjected(highlighted.WorldRect, Color.DarkOrange, 1.25f);
                if (IsSymmetricDesignMode && GetMirrorSlotStruct(highlighted, out SlotStruct mirrored))
                    DrawRectangleProjected(mirrored.WorldRect, Color.DarkOrange.Alpha(0.66f), 1.25f);
            }
            else if (HullEditMode)
            {
                Vector2 cursor = CursorWorldPosition.ToVec2();
                // round to 16
                var rounded = new Vector2((float)Math.Round(cursor.X / 16f) * 16f,
                                          (float)Math.Round(cursor.Y / 16f) * 16f);
                DrawRectangleProjected(rounded, new Vector2(16), 0f, Color.DarkOrange, thickness:1.25f);
            }
        }

        void DrawProjectedModuleRect()
        {
            if (ProjectedSlot == null || ActiveModule == null)
                return;

            bool fits = ModuleGrid.ModuleFitsAtSlot(ProjectedSlot, ActiveModule);
            DrawRectangle(ProjectedSlot.GetWorldRectFor(ActiveModule), fits ? Color.LightGreen : Color.Red, 1.5f);

            if (IsSymmetricDesignMode 
                && GetMirrorProjectedSlot(ProjectedSlot, ActiveModule.XSIZE, ActiveModule.ModuleRot, out SlotStruct mirrored))
            {
                bool mirrorFits = ModuleGrid.ModuleFitsAtSlot(mirrored, ActiveModule);
                DrawRectangle(mirrored.GetWorldRectFor(ActiveModule), mirrorFits 
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
                    slot.Draw(batch, this, concreteGlass, Color.Gray);
                }
                else if (slot.Root.Module == null)
                {
                    bool valid = ActiveModule == null || slot.CanSlotSupportModule(ActiveModule);
                    Color activeColor = valid ? Color.LightGreen : Color.Red;
                    slot.Draw(batch, this, concreteGlass, activeColor);

                    if (DesignedShip != null && DesignedShip.PwrGrid.IsPowered(slot.Pos))
                    {
                        Color yellow = ActiveModule != null ? new Color(Color.Yellow, 150) : Color.Yellow;
                        slot.Draw(batch, this, concreteGlass, yellow);
                    }

                    DrawStringProjected(slot.WorldPos, 0f, 0.4f, Color.Navy, " " + slot.Restrictions, Fonts.Arial20Bold);
                }
            }
        }

        void DrawModules(SpriteBatch batch)
        {
            if (DesignedShip == null)
                return;
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                if (slot.Module != null && slot.Tex != null)
                {
                    if (slot.Module.ModuleType == ShipModuleType.PowerConduit)
                    {
                        // get the module from the design ship, this is not the same as
                        // ModuleGrid.SlotsList modules :(
                        ShipModule m = DesignedShip.GetModuleAt(slot.Pos);
                        slot.Tex = m.Powered ? ResourceManager.Texture(m.IconTexturePath + "_power") : m.ModuleTexture;
                    }
                    DrawModuleTex(slot.ModuleRot, batch, slot, slot.WorldRect);
                }
            }
        }

        void DrawModuleTex(ModuleOrientation orientation, SpriteBatch batch, SlotStruct slot,
                           RectF moduleWorldRect, ShipModule template = null, float alpha = 1)
        {
            SpriteEffects effects = SpriteEffects.None;
            SubTexture texture = slot != null ? slot.Tex : ResourceManager.Texture(template.IconTexturePath);
            float xSize = moduleWorldRect.W;
            float ySize = moduleWorldRect.H;
            float rotation = 0f;

            bool rotatedTexture = (template ?? slot.Module).GetOrientedModuleTexture(ref texture, orientation);

            switch (orientation)
            {
                case ModuleOrientation.Left when !rotatedTexture:
                    moduleWorldRect.W = ySize; // swap width & height
                    moduleWorldRect.H = xSize;
                    moduleWorldRect.Y += ySize;
                    rotation = -1.57079637f;
                    break;
                case ModuleOrientation.Right when !rotatedTexture:
                    moduleWorldRect.W = ySize; // swap width & height
                    moduleWorldRect.H = xSize;
                    moduleWorldRect.X += xSize;
                    rotation = 1.57079637f;
                    break;
                case ModuleOrientation.Rear when !rotatedTexture:
                    effects = SpriteEffects.FlipVertically;
                    break;
                case ModuleOrientation.Normal:
                    if (slot?.WorldPos.X > 0f && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        effects = SpriteEffects.FlipHorizontally;
                    break;
            }

            Rectangle screenRect = ProjectToScreenRect(moduleWorldRect);
            batch.Draw(texture, screenRect, Color.White.Alpha(alpha), rotation, Vector2.Zero, effects, 1f);
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

        void DrawUnpoweredTex(SpriteBatch batch)
        {
            if (DesignedShip == null)
                return;

            var unpowered = ResourceManager.Texture("UI/lightningBolt");
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                ShipModule m = slot.Module;
                if (m == null || m == HighlightedModule && Input.LeftMouseHeld() && m.ModuleType == ShipModuleType.Turret)
                    continue;
                
                if (m.PowerDraw > 0f
                    && m.ModuleType != ShipModuleType.PowerConduit
                    && !DesignedShip.PwrGrid.IsPowered(slot.Pos))
                {
                    batch.Draw(unpowered,
                        slot.Center, Color.White, 0f, new Vector2(8f, 8f), 1f, SpriteEffects.None, 1f);
                }
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
            DrawRectangleProjected(slot.WorldRect, edge, fill);
            DrawStringProjected(slot.Center, 0, 1, Color.Orange, slot.Module.TurretAngle.ToString());
        }

        void DrawHangarShipText(SlotStruct s)
        {
            string hangarShipUID = s.Module.HangarShipUID;
            Color color = Color.Black.Alpha(0.33f);
            Color textC = ShipBuilder.GetHangarTextColor(hangarShipUID);
            DrawRectangleProjected(s.WorldRect, textC, color);

            if (ResourceManager.GetShipTemplate(hangarShipUID, out Ship hangarShip))
            {
                DrawStringProjected(s.Center, 0, 0.4f, textC, hangarShip.Name);
            }
        }

        public void DrawWeaponArcs(SpriteBatch batch, SlotStruct slot)
        {
            Weapon w = slot.Module.InstalledWeapon;
            if (w != null)
            {
                DrawWeaponArcs(batch, this, 0f, w, slot.Module, slot.Center, 500f);
            }
        }

        void DrawWeaponArcs(SpriteBatch batch, ShipModule module, Vector2 moduleWorldPos, float shipFacing = 0)
        {
            Weapon w = module.InstalledWeapon;
            if (w != null)
            {
                Vector2 moduleWorldCenter = moduleWorldPos + module.WorldSize * 0.5f;
                DrawWeaponArcs(batch, this, shipFacing, module.InstalledWeapon, ActiveModule, moduleWorldCenter, 500f);
            }
        }

        // @note This is reused in DebugInfoScreen as well
        public static void DrawWeaponArcs(SpriteBatch batch, GameScreen screen, float shipFacing,
                                          Weapon w, ShipModule m, Vector2 moduleWorldCenter, float worldSize)
        {
            Color color;
            if (w.Tag_Cannon && !w.Tag_Energy)        color = new Color(255, 255, 0, 255);
            else if (w.Tag_Railgun || w.Tag_Subspace) color = new Color(255, 0, 255, 255);
            else if (w.Tag_Cannon)                    color = new Color(0, 255, 0, 255);
            else if (!w.isBeam)                       color = new Color(255, 0, 0, 255);
            else                                      color = new Color(0, 0, 255, 255);

            screen.ProjectToScreenCoords(moduleWorldCenter, 0f, worldSize,
                                        out Vector2 posOnScreen, out float sizeOnScreen);

            SubTexture arcTexture = Empire.Universe.GetArcTexture(m.FieldOfFire.ToDegrees());

            var texOrigin = new Vector2(250f, 250f);
            
            Rectangle rect = posOnScreen.ToRect((int)sizeOnScreen, (int)sizeOnScreen);

            float radians = (shipFacing + m.TurretAngleRads);
            batch.Draw(arcTexture, rect, color.Alpha(0.75f), radians, texOrigin, SpriteEffects.None, 1f);

            Vector2 direction = radians.RadiansToDirection();
            Vector2 start     = posOnScreen;
            Vector2 end = start + direction * sizeOnScreen;
            batch.DrawLine(start, start.LerpTo(end, 0.45f), color.Alpha(0.25f), 3);

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

        void DrawActiveModule(SpriteBatch batch)
        {
            ShipModule template = ResourceManager.GetModuleTemplate(ActiveModule.UID);

            Vector2 moduleWorldPos = CursorWorldPosition.ToVec2().Rounded();
            RectF worldRect = new RectF(moduleWorldPos, ActiveModule.WorldSize);
            
            DrawModuleTex(ActiveModState, batch, null, worldRect, template);
            DrawWeaponArcs(batch, ActiveModule, moduleWorldPos);

            if (IsSymmetricDesignMode)
            {
                Vector2 mirrorWorldPos = GetMirrorWorldPos(moduleWorldPos);
                if (!MirroredModulesTooClose(mirrorWorldPos, moduleWorldPos, ActiveModule.WorldSize))
                {
                    ModuleOrientation orientation = GetMirroredOrientation(ActiveModState);

                    RectF mirrorWorldRect = new RectF(mirrorWorldPos, ActiveModule.WorldSize);
                    DrawModuleTex(orientation, batch, null, mirrorWorldRect, template, 0.5f);
                    
                    int turretAngle = GetMirroredTurretAngle(orientation, ActiveModule.TurretAngle);
                    float shipFacing = ((float)turretAngle).ToRadians(); // HACK
                    DrawWeaponArcs(batch, ActiveModule, mirrorWorldPos, shipFacing);
                }
            }

            if (ActiveModule.shield_power_max > 0f)
            {
                DrawShieldCircle(template, moduleWorldPos);
            }
        }

        void DrawShieldCircle(ShipModule moduleTemplate, Vector2 moduleWorldPos)
        {
            Vector2 moduleCenter = moduleWorldPos + ActiveModule.WorldSize*0.5f;
            DrawCircleProjected(moduleCenter, ActiveModule.ShieldHitRadius, Color.LightGreen);

            if (IsSymmetricDesignMode)
            {
                Vector2 mirrorCenter = GetMirrorWorldPos(moduleWorldPos) + ActiveModule.WorldSize*0.5f;
                DrawCircleProjected(mirrorCenter, ActiveModule.ShieldHitRadius, Color.LightGreen.Alpha(0.5f));
            }
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
            HullBonus bonus = CurrentDesign.Bonuses;
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


            string name = DesignOrHullName;
            Graphics.Font font = Fonts.Arial20Bold.TextWidth(name) <= (SearchBar.Width - 5)
                               ? Fonts.Arial20Bold : Fonts.Arial12Bold;
            var cursor1 = new Vector2(SearchBar.X + 3, r.Y + 14 - font.LineSpacing / 2);
            batch.DrawString(font, name, cursor1, Color.White);

            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            batch.FillRectangle(r, new Color(54, 54, 54));

            var cursor = new Vector2(r.X + 3, r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
        }
    }
}
