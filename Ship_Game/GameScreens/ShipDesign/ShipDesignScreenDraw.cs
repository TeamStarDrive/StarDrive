using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Point = SDGraphics.Point;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen // refactored by Fat Bastard
    {
        public Point GridPosUnderCursor;
        public SlotStruct SlotUnderCursor;

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.BeginFrameRendering(elapsed, ref View, ref Projection);
            ParentUniverse.DrawStarField(batch);
            ParentUniverse.Particles.Draw(View, Projection, nearView:true);
            ParentUniverse.Particles.Update(elapsed.CurrentGameTime);

            ScreenManager.RenderSceneObjects();

            if (ToggleOverlay)
            {
                batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);

                DrawEmptySlots(batch);
                DrawModules(batch);
                DrawUnpoweredTex(batch);
                DrawTacticalOverlays(batch);
                DrawModuleSelections();
                DrawProjectedModuleRect(batch);

                if (EnableDebugFeatures)
                {
                    DrawDebugDetails(batch);
                }

                batch.End();
            }

            batch.Begin();
            if (ActiveModule != null && !ModuleSelectComponent.HitTest(Input))
            {
                DrawActiveModule(batch);
            }

            DrawUi(batch, elapsed);
            ArcsButton.DrawWithShadowCaps(batch);

            base.Draw(batch, elapsed);
            batch.End();
            ScreenManager.EndFrameRendering();
        }

        void DrawDebugDetails(SpriteBatch batch)
        {
            // Draw Internal and External modules for the design
            foreach (ShipModule m in DesignedShip.Modules)
            {
                var rect = new RectF(ModuleGrid.GridPosToWorld(m.Pos), m.WorldSize);
                if (m.HasInternalRestrictions)
                    DrawRectangleProjected(rect, Color.Green);
                else if (m.IsExternal)
                    DrawRectangleProjected(rect, Color.Blue);
            }

            if (ShowAllArcs)
            {
                var shieldGridColor = Color.Cyan.Alpha(0.5f);
                var gridColor = Color.Gray.Alpha(0.5f);

                for (int y = 0; y < DesignedShip.Grid.Height; ++y)
                {
                    for (int x = 0; x < DesignedShip.Grid.Width; ++x)
                    {
                        var worldPos = ModuleGrid.GridPosToWorld(new Point(x,y));
                        var rect = new RectF(worldPos, new Vector2(16));

                        int numShields = DesignedShip.Grid.GetNumShieldsAt(x, y);
                        if (numShields > 0)
                        {
                            DrawRectangleProjected(rect, shieldGridColor);
                            DrawStringProjected(worldPos+new Vector2(1,1), 6, shieldGridColor, $"S:{numShields}");

                            ShipModule shield = DesignedShip.HitTestShields(rect.Center, 10f);
                            if (shield == null)
                            {
                                DrawCircleProjected(rect.Center, 10f, Color.Red);
                                DrawStringProjected(worldPos+new Vector2(1,6), 6, Color.Red, "Hit:false");
                            }
                        }
                        else
                        {
                            DrawRectangleProjected(rect, gridColor);
                        }
                    }
                }
            }

            DrawCrossHairProjected(ModuleGrid.GridPosToWorld(CurrentHull.GridCenter), 16f, Color.Red, 2f);
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
        }

        void DrawProjectedModuleRect(SpriteBatch batch)
        {
            if (ProjectedSlot == null || ActiveModule == null)
                return;

            bool fits = ModuleGrid.ModuleFitsAtSlot(ProjectedSlot, ActiveModule);
            DrawRectangleProjected(ProjectedSlot.GetWorldRectFor(ActiveModule), fits ? Color.LightGreen : Color.Red, 1.5f);

            if (IsSymmetricDesignMode && GetMirrorSlot(ProjectedSlot, ActiveModule, out MirrorSlot mirrored))
            {
                bool mirrorFits = ModuleGrid.ModuleFitsAtSlot(mirrored.Slot, ActiveModule);
                DrawRectangleProjected(mirrored.Slot.GetWorldRectFor(ActiveModule), mirrorFits 
                    ? Color.LightGreen.Alpha(0.66f) : Color.Red.Alpha(0.66f), 1.5f);
            }
        }

        void DrawEmptySlots(SpriteBatch batch)
        {
            SubTexture concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1");

            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                RectF rect = ProjectToScreenRectF(slot.WorldRect);

                if (slot.Module != null)
                {
                    batch.Draw(concreteGlass, rect, Color.Gray);
                }
                else if (slot.Root.Module == null)
                {
                    bool valid = ActiveModule == null || slot.CanSlotSupportModule(ActiveModule);
                    Color activeColor = valid ? Color.LightGreen : Color.Red;
                    batch.Draw(concreteGlass, rect, activeColor);

                    if (!HullEditMode && DesignedShip.PwrGrid.IsPowered(slot.Pos))
                    {
                        Color yellow = ActiveModule != null ? new Color(Color.Yellow, 150) : Color.Yellow;
                        batch.Draw(concreteGlass, rect, yellow);
                    }

                    string r = slot.HullRestrict.ToString();
                    DrawStringProjected(slot.WorldPos + new Vector2(8), 8f, Color.Navy, r, Fonts.Arial20Bold);
                }
            }
        }

        void DrawModules(SpriteBatch batch)
        {
            if (HullEditMode)
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
                        if (m != null)
                        {
                            slot.Tex = m.Powered ? ResourceManager.Texture(m.IconTexturePath + "_power") : m.ModuleTexture;
                        }
                    }
                    DrawModuleTex(slot.Module.ModuleRot, batch, slot, slot.WorldRect);
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
                    rotation = -RadMath.HalfPI;
                    break;
                case ModuleOrientation.Right when !rotatedTexture:
                    moduleWorldRect.W = ySize; // swap width & height
                    moduleWorldRect.H = xSize;
                    moduleWorldRect.X += xSize;
                    rotation = RadMath.HalfPI;
                    break;
                case ModuleOrientation.Rear when !rotatedTexture:
                    effects = SpriteEffects.FlipVertically;
                    break;
                case ModuleOrientation.Normal:
                    if (slot?.WorldPos.X > 0f && slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        effects = SpriteEffects.FlipHorizontally;
                    break;
            }

            RectF screenRect = ProjectToScreenRectF(moduleWorldRect);
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

                if (s.Module.ShieldPowerMax > 0f)
                    DrawCircleProjected(s.Center, s.Module.ShieldHitRadius, Color.LightGreen);

                if (s.Module.ModuleType == ShipModuleType.Turret && Input.LeftMouseHeld())
                {
                    DrawFireArcText(s);
                    if (IsSymmetricDesignMode)
                        ToolTip.ShipYardArcTip();
                }

                DrawHangarShipText(s.Module, s.WorldRect);
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
            if (HullEditMode)
                return;

            var unPowered = ResourceManager.Texture("UI/lightningBolt");
            foreach (SlotStruct slot in ModuleGrid.SlotsList)
            {
                ShipModule m = slot.Module;
                if (m == null || m == HighlightedModule && Input.LeftMouseHeld() && m.ModuleType == ShipModuleType.Turret)
                    continue;
                
                if (m.PowerDraw > 0f
                    && m.ModuleType != ShipModuleType.PowerConduit
                    && !DesignedShip.PwrGrid.IsPowered(slot.Pos))
                {
                    ProjectToScreenCoordsF(slot.Center, new Vector2(10), out Vector2 pos, out Vector2 size);
                    var screenRect = new RectF(pos - size/2, size);
                    batch.DrawDropShadowImage(screenRect, unPowered, Color.White);
                    batch.Draw(unPowered, screenRect, ApplyCurrentAlphaToColor(Color.Red));
                }
            }
        }

        void DrawFireArcText(SlotStruct slot)
        {
            Color fill = Color.Black.Alpha(0.33f);
            Color edge = (slot.Module == HighlightedModule) ? Color.DarkOrange : fill;
            DrawRectangleProjected(slot.WorldRect, edge, fill);
            DrawStringProjected(slot.Center, 0, 1, Color.Orange, slot.Module.TurretAngle.ToString());
        }

        void DrawHangarShipText(ShipModule m, in RectF worldRect)
        {
            if (m.ModuleType != ShipModuleType.Hangar)
                return;

            Color color = Color.Black.Alpha(0.33f);
            Color textC = ShipBuilder.GetHangarTextColor(m.HangarShipUID);
            DrawRectangleProjected(worldRect, textC, color);

            if (ResourceManager.GetShipTemplate(m.HangarShipUID, out Ship hangarShip))
            {
                DrawStringProjected(worldRect.Center, 6, textC, hangarShip.Name, Fonts.Arial20Bold, shadow:true, center:true);
            }
        }

        public void DrawWeaponArcs(SpriteBatch batch, SlotStruct slot)
        {
            Weapon w = slot.Module.InstalledWeapon;
            if (w != null)
            {
                DrawWeaponArcs(batch, this, w, slot.Module, slot.Center, 500f, 0f, slot.Module.TurretAngle);
            }
        }

        void DrawWeaponArcs(SpriteBatch batch, ShipModule module, Vector2 moduleWorldPos, float shipFacing, int turretAngle)
        {
            Weapon w = module.InstalledWeapon;
            if (w != null)
            {
                Vector2 moduleWorldCenter = moduleWorldPos + module.WorldSize * 0.5f;
                DrawWeaponArcs(batch, this, module.InstalledWeapon, ActiveModule, 
                               moduleWorldCenter, 500f, shipFacing, turretAngle);
            }
        }

        // @note This is reused in DebugInfoScreen as well
        public static void DrawWeaponArcs(SpriteBatch batch, GameScreen screen, Weapon w, ShipModule m,
                                          Vector2 moduleWorldCenter, float worldSize, float shipFacing, int turretAngle)
        {
            Color color;
            if (w.Tag_Cannon && !w.Tag_Energy)        color = new Color(255, 255, 0, 255);
            else if (w.Tag_Cannon)                    color = new Color(0, 255, 0, 255);
            else if (!w.IsBeam)                       color = new Color(255, 0, 0, 255);
            else                                      color = new Color(0, 0, 255, 255);

            screen.ProjectToScreenCoords(moduleWorldCenter, 0f, worldSize,
                                         out Vector2d posOnScreen, out double sizeOnScreen);
            Vector2 pos = posOnScreen.ToVec2f();
            float size = (float)sizeOnScreen;

            SubTexture arcTexture = ResourceManager.GetArcTexture(m.FieldOfFire.ToDegrees());

            var texOrigin = new Vector2(250f, 250f);
            var rect = new RectF(pos, new Vector2(size));

            float facing = shipFacing + ((float)turretAngle).ToRadians();
            batch.Draw(arcTexture, rect, color.Alpha(0.75f), facing, texOrigin, SpriteEffects.None, 1f);

            Vector2 direction = facing.RadiansToDirection();
            Vector2 start     = pos;
            Vector2 end = start + direction * size;
            batch.DrawLine(start, start.LerpTo(end, 0.45f), color.Alpha(0.25f), 3);

            end = start + direction * size;
            
            Vector2 textPos = start.LerpTo(end, 0.16f);
            float textRot   = facing + RadMath.HalfPI;
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

            Vector2 moduleWorldPos = CursorWorldPosition2D.Rounded();
            Vector2 moduleWorldSize = ActiveModule.WorldSize;
            var worldRect = new RectF(moduleWorldPos, moduleWorldSize);

            DrawModuleTex(ActiveModule.ModuleRot, batch, null, worldRect, template);
            DrawWeaponArcs(batch, ActiveModule, moduleWorldPos, 0, ActiveModule.TurretAngle);
            DrawHangarShipText(ActiveModule, worldRect);

            if (IsSymmetricDesignMode)
            {
                Vector2 mirrorWorldPos = GetMirrorWorldPos(moduleWorldPos, moduleWorldSize);
                if (!MirroredModulesTooClose(mirrorWorldPos, moduleWorldPos, moduleWorldSize))
                {
                    ModuleOrientation orientation = GetMirroredOrientation(ActiveModule.ModuleRot);

                    var mirrorWorldRect = new RectF(mirrorWorldPos, moduleWorldSize);
                    DrawModuleTex(orientation, batch, null, mirrorWorldRect, template, 0.5f);

                    int turretAngle = GetMirroredTurretAngle(ActiveModule.TurretAngle);
                    DrawWeaponArcs(batch, ActiveModule, mirrorWorldPos, 0f, turretAngle);
                    DrawHangarShipText(ActiveModule, mirrorWorldRect);
                }
            }

            if (ActiveModule.ShieldPowerMax > 0f)
            {
                DrawShieldCircle(template, moduleWorldPos, moduleWorldSize);
            }
        }

        void DrawShieldCircle(ShipModule moduleTemplate, Vector2 moduleWorldPos, Vector2 moduleWorldSize)
        {
            Vector2 moduleCenter = moduleWorldPos + moduleWorldSize*0.5f;
            DrawCircleProjected(moduleCenter, moduleTemplate.ShieldHitRadius, Color.LightGreen);

            if (IsSymmetricDesignMode)
            {
                Vector2 mirrorCenter = GetMirrorWorldPos(moduleWorldPos, moduleWorldSize) + moduleWorldSize*0.5f;
                DrawCircleProjected(mirrorCenter, moduleTemplate.ShieldHitRadius, Color.LightGreen.Alpha(0.5f));
            }
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

            // TODO: these should be split into separate parts
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
            batch.DrawString(font, name, cursor1, ShipSaved || IsEmptyHull ? Color.White : Color.OrangeRed);

            r = new Rectangle(r.X - r.Width - 12, r.Y, r.Width, r.Height);
            DesignRoleRect = new Rectangle(r.X , r.Y, r.Width, r.Height);
            batch.FillRectangle(r, new Color(54, 54, 54));

            var cursor = new Vector2(r.X + 3, r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2);
            batch.DrawString(Fonts.Arial20Bold, Localizer.GetRole(Role, EmpireManager.Player), cursor, Color.White);
        }
    }
}
