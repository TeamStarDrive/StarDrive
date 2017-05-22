using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        public override void Draw(GameTime gameTime)
        {
            ScreenManager.BeginFrameRendering(gameTime, ref View, ref Projection);

            Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.starfield);
            ScreenManager.RenderSceneObjects();

            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate,
                SaveStateMode.None, Camera.get_transformation(ScreenManager.GraphicsDevice));
            if (ToggleOverlay)
            {
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.Module != null)
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                            , new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                                , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.Gray);
                    }
                    else
                    {
                        if (this.ActiveModule != null)
                        {
                            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
                            Texture2D item = ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Color activeColor = slot.ShowValid ? Color.LightGreen : Color.Red;

                            spriteBatch.Draw(item, slot.PQ.enclosingRect, activeColor);
                            if (slot.Powered)
                            {
                                ScreenManager.SpriteBatch.Draw(
                                    ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                    , slot.PQ.enclosingRect, new Color(255, 255, 0, 150));
                            }
                        }
                        else if (slot.Powered)
                        {
                            ScreenManager.SpriteBatch.Draw(
                                ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                , slot.PQ.enclosingRect, Color.Yellow);
                        }
                        else
                        {
                            SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
                            Texture2D texture2D = ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Rectangle rectangle1 = slot.PQ.enclosingRect;
                            Color unpoweredColored;
                            if (slot.ShowValid)
                            {
                                unpoweredColored = Color.LightGreen;
                            }
                            else
                            {
                                unpoweredColored = (slot.ShowValid ? Color.White : Color.Red);
                            }
                            spriteBatch1.Draw(texture2D, rectangle1, unpoweredColored);
                        }
                    }
                    if (slot.Module != null)
                        continue;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions)
                        , new Vector2(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y)
                        , Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null)
                    {
                        continue;
                    }
                    if (slot.State != ActiveModuleState.Normal)
                    {
                        Rectangle r = new Rectangle(
                            slot.PQ.enclosingRect.X,
                            slot.PQ.enclosingRect.Y,
                            16 * slot.Module.XSIZE,
                            16 * slot.Module.YSIZE);

                        // @todo Simplify this
                        switch (slot.State)
                        {
                            case ActiveModuleState.Left:
                            {
                                int h = slot.Module.YSIZE * 16;
                                int w = slot.Module.XSIZE * 16;
                                r.Width = h; // swap width & height
                                r.Height = w;
                                r.Y += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, -1.57079637f,
                                    Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Right:
                            {
                                int w = slot.Module.YSIZE * 16;
                                int h = slot.Module.XSIZE * 16;
                                r.Width = w;
                                r.Height = h;
                                r.X += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 1.57079637f, Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Rear:
                            {
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 0f, Vector2.Zero
                                    , SpriteEffects.FlipVertically, 1f);
                                break;
                            }
                        }
                    }
                    else if (slot.Module.XSIZE <= 1 && slot.Module.YSIZE <= 1)
                    {
                        if (slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        {
                            ScreenManager.SpriteBatch.Draw(slot.Tex, slot.PQ.enclosingRect, Color.White);
                        }
                        else
                        {
                            string graphic = GetConduitGraphic(slot);
                            var conduitTex = ResourceManager.Texture("Conduits/" + graphic);
                            ScreenManager.SpriteBatch.Draw(conduitTex, slot.PQ.enclosingRect, Color.White);
                            if (slot.Module.Powered)
                            {
                                var poweredTex = ResourceManager.Texture("Conduits/" + graphic + "_power");
                                ScreenManager.SpriteBatch.Draw(poweredTex, slot.PQ.enclosingRect, Color.White);
                            }
                        }
                    }
                    else if (slot.SlotReference.Position.X <= 256f)
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X,
                            slot.PQ.enclosingRect.Y
                            , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X,
                                slot.PQ.enclosingRect.Y
                                , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), null, Color.White, 0f, Vector2.Zero
                            , SpriteEffects.FlipHorizontally, 1f);
                    }
                    if (slot.Module != HoveredModule)
                    {
                        continue;
                    }
                    ScreenManager.SpriteBatch.DrawRectangle(new Rectangle(slot.PQ.enclosingRect.X,
                        slot.PQ.enclosingRect.Y
                        , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White, 2f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null ||
                        slot.Module != this.HighlightedModule && !this.ShowAllArcs)
                    {
                        continue;
                    }
                    if (slot.Module.shield_power_max > 0f)
                    {
                        Vector2 center = new Vector2(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2
                            , slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2);
                        DrawCircle(center, slot.Module.shield_radius, 50, Color.LightGreen);
                    }


                    // @todo Use this to fix the 'original' code below :)))
                    var arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);

                    void DrawArc(Color drawcolor)
                    {
                        var center = new Vector2(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2
                            , slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2);
                        var origin = new Vector2(250f, 250f);

                        var toDraw = new Rectangle((int)center.X, (int)center.Y, 500, 500);
                        ScreenManager.SpriteBatch.Draw(arcTexture, toDraw, null, drawcolor
                            , slot.Module.Facing.ToRadians(), origin, SpriteEffects.None, 1f);

                    }

                    if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)                    
                        DrawArc(new Color(255, 255, 0, 255));                    
                    else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)                    
                        DrawArc(new Color(255, 0, 255, 255));                    
                    else if (slot.Module.InstalledWeapon.Tag_Cannon)                    
                        DrawArc(new Color(0, 255, 0, 255));                    
                    else if (!slot.Module.InstalledWeapon.isBeam)                                         
                        DrawArc(new Color(255, 0, 0, 255));                    
                    else                    
                        DrawArc(new Color(0, 0, 255, 255));                
                }

                foreach (SlotStruct ss in this.Slots)
                {
                    if (ss.Module == null)
                    {
                        continue;
                    }
                    Vector2 Center = new Vector2(ss.PQ.X + 16 * ss.Module.XSIZE / 2,
                        ss.PQ.Y + 16 * ss.Module.YSIZE / 2);
                    Vector2 lightOrigin = new Vector2(8f, 8f);
                    if (ss.Module.PowerDraw <= 0f || ss.Module.Powered ||
                        ss.Module.ModuleType == ShipModuleType.PowerConduit)
                    {
                        continue;
                    }
                    Rectangle? nullable8 = null;
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/lightningBolt"],
                        Center, nullable8, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
                }
            }
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            foreach (ModuleButton mb in this.ModuleButtons)
            {
                if (!this.ModuleSelectionArea.HitTest(
                    new Vector2((float) (mb.moduleRect.X + 30), (float) (mb.moduleRect.Y + 30))))
                {
                    continue;
                }
                if (mb.isHighlighted)
                {
                    ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/blueHighlight"],
                        mb.moduleRect, Color.White);
                }
                ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mb.ModuleUID);
                Rectangle modRect = new Rectangle(0, 0, moduleTemplate.XSIZE * 16, moduleTemplate.YSIZE * 16);
                //{
                modRect.X = mb.moduleRect.X + 64 - modRect.Width / 2;
                modRect.Y = mb.moduleRect.Y + 64 - modRect.Height / 2;
                //};
                ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(moduleTemplate.IconTexturePath), modRect,
                    Color.White);
                float nWidth = Fonts.Arial12.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X;
                Vector2 nameCursor = new Vector2((float) (mb.moduleRect.X + 64) - nWidth / 2f,
                    (float) (mb.moduleRect.Y + 128 - Fonts.Arial12.LineSpacing - 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Localizer.Token(moduleTemplate.NameIndex),
                    nameCursor, Color.White);
            }
            float single = (float) Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(single, (float) state.Y);
            if (this.ActiveModule != null && !this.ActiveModSubMenu.Menu.HitTest(MousePos) &&
                !this.ModSel.Menu.HitTest(MousePos) && (!this.Choosefighterrect.HitTest(MousePos) ||
                                                        this.ActiveModule.ModuleType != ShipModuleType.Hangar ||
                                                        this.ActiveModule.IsSupplyBay || this.ActiveModule.IsTroopBay))
            {
                ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);

                Rectangle r = new Rectangle(this.MouseStateCurrent.X, this.MouseStateCurrent.Y,
                    (int) ((float) (16 * this.ActiveModule.XSIZE) * this.Camera.Zoom),
                    (int) ((float) (16 * this.ActiveModule.YSIZE) * this.Camera.Zoom));
                switch (this.ActiveModState)
                {
                    case ActiveModuleState.Normal:
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, Color.White);
                        break;
                    }
                    case ActiveModuleState.Left:
                    {
                        r.Y = r.Y + (int) ((16 * moduleTemplate.XSIZE) * Camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width = h;
                        r.Height = w;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Right:
                    {
                        r.X = r.X + (int) ((16 * moduleTemplate.YSIZE) * Camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width = h;
                        r.Height = w;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Rear:
                    {
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White,
                            0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
                        break;
                    }
                }
                if (this.ActiveModule.shield_power_max > 0f)
                {
                    Vector2 center = new Vector2((float) this.MouseStateCurrent.X, (float) this.MouseStateCurrent.Y) +
                                     new Vector2((float) (moduleTemplate.XSIZE * 16 / 2),
                                         (float) (moduleTemplate.YSIZE * 16 / 2));
                    DrawCircle(center, this.ActiveModule.shield_radius * this.Camera.Zoom, 50, Color.LightGreen);
                }
            }
            this.DrawUI(gameTime);
            selector?.Draw();
            ArcsButton.DrawWithShadowCaps(ScreenManager);
            if (Debug)
            {
                float width2 = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f;

                var pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
                HelperFunctions.DrawDropShadowText(ScreenManager, "Debug", pos, Fonts.Arial20Bold);
                pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Operation.ToString()).X / 2, 140f);
                HelperFunctions.DrawDropShadowText(ScreenManager, Operation.ToString(), pos, Fonts.Arial20Bold);
#if SHIPYARD
                string ratios = $"I: {TotalI}       O: {TotalO}      E: {TotalE}      IO: {TotalIO}      " +
                                $"IE: {TotalIE}      OE: {TotalOE}      IOE: {TotalIOE}";
                pos = new Vector2(width2 - Fonts.Arial20Bold.MeasureString(Ratios).X / 2, 180f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, Ratios, pos, Fonts.Arial20Bold);
#endif
            }
            Close.Draw(ScreenManager);
            ScreenManager.SpriteBatch.End();
            ScreenManager.EndFrameRendering();
        }

        private void DrawString(ref Vector2 cursorPos, string text, SpriteFont font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            ScreenManager.SpriteBatch.DrawString(font, text, cursorPos, Color.SpringGreen);
            cursorPos.X = cursorPos.X + Fonts.Arial8Bold.MeasureString(text).X;
        }

        private void DrawActiveModuleData()
        {
            float powerDraw;
            this.ActiveModSubMenu.Draw();
            Rectangle r = this.ActiveModSubMenu.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
            sel.Draw();
            ShipModule mod = this.ActiveModule;

            if (this.ActiveModule == null && this.HighlightedModule != null)
            {
                mod = this.HighlightedModule;
            }
            else if (this.ActiveModule != null)
            {
                mod = this.ActiveModule;
            }

            if (mod != null)
            {
                mod.HealthMax = ResourceManager.GetModuleTemplate(mod.UID).HealthMax;
            }
            if (!ActiveModSubMenu.Tabs[0].Selected || mod == null)
                return;

            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);

            //Added by McShooterz: Changed how modules names are displayed for allowing longer names
            Vector2 modTitlePos = new Vector2((float) (this.ActiveModSubMenu.Menu.X + 10),
                (float) (this.ActiveModSubMenu.Menu.Y + 35));
            if (Fonts.Arial20Bold.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X + 16 <
                this.ActiveModSubMenu.Menu.Width)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float) (Fonts.Arial20Bold.LineSpacing + 6);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(moduleTemplate.NameIndex),
                    modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float) (Fonts.Arial14Bold.LineSpacing + 4);
            }
            string rest = "";
            if (moduleTemplate.Restrictions == Restrictions.IO)
            {
                rest = "Any Slot except E";
            }
            else if (moduleTemplate.Restrictions == Restrictions.I)
            {
                rest = "I, IO, IE or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.O)
            {
                rest = "O, IO, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.E)
            {
                rest = "E, IE, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IOE)
            {
                rest = "Any Slot";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IE)
            {
                rest = "Any Slot except O";
            }
            else if (moduleTemplate.Restrictions == Restrictions.OE)
            {
                rest = "Any Slot except I";
            }

            // Concat ship class restrictions
            string shipRest = "";
            bool specialString = false;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones &&
                GlobalStats.ActiveModInfo.useDestroyers)
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
            }
            if (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.useDrones &&
                GlobalStats.ActiveModInfo.useDestroyers)
            {
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
            }
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones &&
                !GlobalStats.ActiveModInfo.useDestroyers)
            {
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
            if (GlobalStats.ActiveModInfo == null || (!GlobalStats.ActiveModInfo.useDrones &&
                                                      !GlobalStats.ActiveModInfo.useDestroyers))
            {
                if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule &&
                    mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule &&
                    mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
                else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule &&
                         !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule &&
                         !mod.StationModule && !mod.FreighterModule)
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
                         !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule &&
                         !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                {
                    shipRest = "All Hulls";
                    specialString = true;
                }
            }

            else if (!specialString && (!mod.DroneModule && GlobalStats.ActiveModInfo != null &&
                                        GlobalStats.ActiveModInfo.useDrones) || !mod.FighterModule ||
                     !mod.CorvetteModule || !mod.FrigateModule ||
                     (!mod.DestroyerModule && GlobalStats.ActiveModInfo != null &&
                      GlobalStats.ActiveModInfo.useDestroyers) || !mod.CruiserModule || !mod.CruiserModule ||
                     !mod.CarrierModule || !mod.CapitalModule || !mod.PlatformModule || !mod.StationModule ||
                     !mod.FreighterModule)
            {
                if (mod.DroneModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                    shipRest += "Dr ";
                if (mod.FighterModule)
                    shipRest += "F ";
                if (mod.CorvetteModule)
                    shipRest += "CO ";
                if (mod.FrigateModule)
                    shipRest += "FF ";
                if (mod.DestroyerModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDestroyers)
                    shipRest += "DD ";
                if (mod.CruiserModule)
                    shipRest += "CC ";
                if (mod.CarrierModule)
                    shipRest += "CV ";
                if (mod.CapitalModule)
                    shipRest += "CA ";
                if (mod.FreighterModule)
                    shipRest += "Frt ";
                if (mod.PlatformModule || mod.StationModule)
                    shipRest += "Stat ";
            }

            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(Localizer.Token(122), ": ", rest),
                modTitlePos, Color.Orange);
            modTitlePos.Y = modTitlePos.Y + (float) (Fonts.Arial8Bold.LineSpacing);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Hulls: ", shipRest), modTitlePos,
                Color.LightSteelBlue);
            modTitlePos.Y = modTitlePos.Y + (float) (Fonts.Arial8Bold.LineSpacing + 11);
            int startx = (int) modTitlePos.X;
            if (moduleTemplate.IsWeapon && moduleTemplate.BombType == null)
            {
                var weaponTemplate = ResourceManager.GetWeaponTemplate(moduleTemplate.WeaponType);

                var sb = new StringBuilder();
                if (weaponTemplate.Tag_Guided) sb.Append("GUIDED ");
                if (weaponTemplate.Tag_Intercept) sb.Append("INTERCEPTABLE ");
                if (weaponTemplate.Tag_Energy) sb.Append("ENERGY ");
                if (weaponTemplate.Tag_Hybrid) sb.Append("HYBRID ");
                if (weaponTemplate.Tag_Kinetic) sb.Append("KINETIC ");
                if (weaponTemplate.Tag_Explosive && !weaponTemplate.Tag_Flak) sb.Append("EXPLOSIVE ");
                if (weaponTemplate.Tag_Subspace) sb.Append("SUBSPACE ");
                if (weaponTemplate.Tag_Warp) sb.Append("WARP ");
                if (weaponTemplate.Tag_PD) sb.Append("POINT DEFENSE ");
                if (weaponTemplate.Tag_Flak) sb.Append("FLAK ");

                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats &&
                    (weaponTemplate.Tag_Missile && !weaponTemplate.Tag_Guided))
                    sb.Append("ROCKET ");
                else if (weaponTemplate.Tag_Missile)
                    sb.Append("MISSILE ");

                if (weaponTemplate.Tag_Tractor) sb.Append("TRACTOR ");
                if (weaponTemplate.Tag_Beam) sb.Append("BEAM ");
                if (weaponTemplate.Tag_Array) sb.Append("ARRAY ");
                if (weaponTemplate.Tag_Railgun) sb.Append("RAILGUN ");
                if (weaponTemplate.Tag_Torpedo) sb.Append("TORPEDO ");
                if (weaponTemplate.Tag_Bomb) sb.Append("BOMB ");
                if (weaponTemplate.Tag_BioWeapon) sb.Append("BIOWEAPON ");
                if (weaponTemplate.Tag_SpaceBomb) sb.Append("SPACEBOMB ");
                if (weaponTemplate.Tag_Drone) sb.Append("DRONE ");
                if (weaponTemplate.Tag_Cannon) sb.Append("CANNON ");
                DrawString(ref modTitlePos, sb.ToString(), Fonts.Arial8Bold);

                modTitlePos.Y = modTitlePos.Y + (Fonts.Arial8Bold.LineSpacing + 5);
                modTitlePos.X = startx;
            }

            string txt = this.parseText(Localizer.Token(moduleTemplate.DescriptionIndex),
                (float) (this.ActiveModSubMenu.Menu.Width - 20), Fonts.Arial12);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
            modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
            float starty = modTitlePos.Y;
            float strength = ResourceManager.CalculateModuleOffenseDefense(mod, ActiveHull.ModuleSlots.Length);
            if (strength > 0)
            {
                this.DrawStat(ref modTitlePos, "Offense", (float) strength, 227);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
            }
            if (!mod.isWeapon || mod.InstalledWeapon == null)
            {
                if (mod.Cost != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(128),
                        (float) mod.Cost * UniverseScreen.GamePaceStatic, 84);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.Mass != 0)
                {
                    float MassMod = (float) EmpireManager.Player.data.MassModifier;
                    float ArmourMassMod = (float) EmpireManager.Player.data.ArmourMassModifier;

                    if (mod.ModuleType == ShipModuleType.Armor)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(123), (ArmourMassMod * mod.Mass) * MassMod, 79);
                    }
                    else
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(123), MassMod * mod.Mass, 79);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HealthMax != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(124),
                        (float) mod.HealthMax + mod.HealthMax * (float) EmpireManager.Player.data.Traits.ModHpModifier,
                        80);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ModuleType != ShipModuleType.PowerPlant)
                {
                    powerDraw = -(float) mod.PowerDraw;
                }
                else
                {
                    powerDraw = (mod.PowerDraw > 0f
                        ? (float) (-mod.PowerDraw)
                        : mod.PowerFlowMax + mod.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod);
                }
                if (powerDraw != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(125), powerDraw, 81);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.MechanicalBoardingDefense != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2231), (float) mod.MechanicalBoardingDefense, 143);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BonusRepairRate != 0f)
                {
                    this.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"),
                        (float) (
                            (mod.BonusRepairRate + mod.BonusRepairRate * EmpireManager.Player.data.Traits.RepairMod) *
                            (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                             ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                                ? 1f + ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus
                                : 1)), 97);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                //Shift to next Column
                float MaxDepth = modTitlePos.Y;
                modTitlePos.X = modTitlePos.X + 152f;
                modTitlePos.Y = starty;
                if (mod.thrust != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(131), (float) mod.thrust, 91);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.WarpThrust != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2064), (float) mod.WarpThrust, 92);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TurnThrust != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2260), (float) mod.TurnThrust, 148);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_power_max != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(132),
                        mod.shield_power_max *
                        (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                         ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                            ? 1f + ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus
                            : 1f) + EmpireManager.Player.data.ShieldPowerMod * mod.shield_power_max, 93);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_radius != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(133), (float) mod.shield_radius, 94);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_recharge_rate != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(134), (float) mod.shield_recharge_rate, 95);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }

                // Doc: new shield resistances, UI info.

                if (mod.shield_kinetic_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6162), (float) mod.shield_kinetic_resist, 209
                        , Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_energy_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6163), (float) mod.shield_energy_resist, 210,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_explosive_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6164), (float) mod.shield_explosive_resist, 211,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_missile_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6165), (float) mod.shield_missile_resist, 212,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_flak_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6166), (float) mod.shield_flak_resist, 213,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_hybrid_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6167), (float) mod.shield_hybrid_resist, 214,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_railgun_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6168), (float) mod.shield_railgun_resist, 215,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_subspace_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6169), (float) mod.shield_subspace_resist, 216,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_warp_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6170), (float) mod.shield_warp_resist, 217,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_beam_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6171), (float) mod.shield_beam_resist, 218,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_threshold != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6176), (float) mod.shield_threshold, 222,
                        Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.SensorRange != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(126), (float) mod.SensorRange, 96);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SensorBonus != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6121), (float) mod.SensorBonus, 167);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HealPerTurn != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6131), mod.HealPerTurn, 174);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterRange != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(126), (float) mod.TransporterRange, 168);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterPower != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6123), (float) mod.TransporterPower, 169);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTimerConstant != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6122), (float) mod.TransporterTimerConstant, 170);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterOrdnance != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6124), (float) mod.TransporterOrdnance, 171);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTroopAssault != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6135), (float) mod.TransporterTroopAssault, 187);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TransporterTroopLanding != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6128), (float) mod.TransporterTroopLanding, 172);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdinanceCapacity != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2129), (float) mod.OrdinanceCapacity, 124);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.Cargo_Capacity != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(119), (float) mod.Cargo_Capacity, 109);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdnanceAddedPerSecond != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6120), (float) mod.OrdnanceAddedPerSecond, 162);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InhibitionRadius != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2233), (float) mod.InhibitionRadius, 144);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TroopCapacity != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(336), (float) mod.TroopCapacity, 173);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.PowerStoreMax != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2235),
                        (float) (mod.PowerStoreMax + mod.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier),
                        145);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                //added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
                if (mod.PowerDrawAtWarp != 0f)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6011), (float) (-mod.PowerDrawAtWarp), 178);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM && mod.ECM != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6004), (float) mod.ECM, 154, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ModuleType == ShipModuleType.Hangar && mod.hangarTimerConstant != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(136), (float) mod.hangarTimerConstant, 98);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.explodes)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Explodes", modTitlePos,
                        Color.OrangeRed);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.KineticResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6142), (float) mod.KineticResist, 189,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.EnergyResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6143), (float) mod.EnergyResist, 190,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.GuidedResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6144), (float) mod.GuidedResist, 191,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.MissileResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6145), (float) mod.MissileResist, 192,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HybridResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6146), (float) mod.HybridResist, 193,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BeamResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6147), (float) mod.BeamResist, 194, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ExplosiveResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6148), (float) mod.ExplosiveResist, 195,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InterceptResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6149), (float) mod.InterceptResist, 196,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.RailgunResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6150), (float) mod.RailgunResist, 197,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SpaceBombResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6151), (float) mod.SpaceBombResist, 198,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BombResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6152), (float) mod.BombResist, 199, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BioWeaponResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6153), (float) mod.BioWeaponResist, 200,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.DroneResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6154), (float) mod.DroneResist, 201,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.WarpResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6155), (float) mod.WarpResist, 202, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TorpedoResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6156), (float) mod.TorpedoResist, 203,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.CannonResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6157), (float) mod.CannonResist, 204,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SubspaceResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6158), (float) mod.SubspaceResist, 205,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.PDResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6159), (float) mod.PDResist, 206, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.FlakResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6160), (float) mod.FlakResist, 207, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.APResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6161), (float) mod.APResist, 208, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.DamageThreshold != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6175), (float) mod.DamageThreshold, 221);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.EMP_Protection != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6174), (float) mod.EMP_Protection, 219);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.FixedTracking > 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6187), (float) mod.FixedTracking, 231);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TargetTracking > 0)
                {
                    this.DrawStat(ref modTitlePos, "+" + Localizer.Token(6186), (float) mod.TargetTracking, 226);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.PermittedHangarRoles.Length > 0)
                {
                    modTitlePos.Y = Math.Max(modTitlePos.Y, MaxDepth) + (float) Fonts.Arial12Bold.LineSpacing;
                    Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold,
                        string.Concat(Localizer.Token(137), " : ", mod.hangarShipUID), shipSelectionPos, Color.Orange);
                    r = this.ChooseFighterSub.Menu;
                    r.Y = r.Y + 25;
                    r.Height = r.Height - 25;
                    sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
                    sel.Draw();
                    this.UpdateHangarOptions(mod);
                    this.ChooseFighterSub.Draw();
                    this.ChooseFighterSL.Draw(ScreenManager.SpriteBatch);
                    Vector2 bCursor = new Vector2((float) (this.ChooseFighterSub.Menu.X + 15),
                        (float) (this.ChooseFighterSub.Menu.Y + 25));
                    for (int i = this.ChooseFighterSL.indexAtTop;
                        i < this.ChooseFighterSL.Entries.Count && i < this.ChooseFighterSL.indexAtTop +
                        this.ChooseFighterSL.entriesToDisplay;
                        i++)
                    {
                        ScrollList.Entry e = this.ChooseFighterSL.Entries[i];
                        bCursor.Y = (float) e.clickRect.Y;
                        ScreenManager.SpriteBatch.Draw(
                            ResourceManager.TextureDict[
                                ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath],
                            new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                        Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            (!string.IsNullOrEmpty((e.item as Ship).VanityName)
                                ? (e.item as Ship).VanityName
                                : (e.item as Ship).Name), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    if (this.selector != null)
                    {
                        this.selector.Draw();
                        return;
                    }
                }
                return;
            }
            else
            {
                this.DrawStat(ref modTitlePos, Localizer.Token(128), (float) mod.Cost * UniverseScreen.GamePaceStatic,
                    84);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                this.DrawStat(ref modTitlePos, Localizer.Token(123),
                    (float) EmpireManager.Player.data.MassModifier * mod.Mass, 79);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                this.DrawStat(ref modTitlePos, Localizer.Token(124),
                    (float) mod.HealthMax + EmpireManager.Player.data.Traits.ModHpModifier * mod.HealthMax, 80);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                this.DrawStat(ref modTitlePos, Localizer.Token(125),
                    (mod.ModuleType != ShipModuleType.PowerPlant ? -(float) mod.PowerDraw : mod.PowerFlowMax), 81);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                this.DrawStat(ref modTitlePos, Localizer.Token(126),
                    (float) ModifiedWeaponStat(mod.InstalledWeapon, "range"), 82);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                if (!mod.InstalledWeapon.explodes || mod.InstalledWeapon.OrdinanceRequiredToFire <= 0f)
                {
                    if (mod.InstalledWeapon.isRepairBeam)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(135),
                            (float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * -90f *
                            mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 166);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                        this.DrawStat(ref modTitlePos, "Duration", (float) mod.InstalledWeapon.BeamDuration, 188);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    else if (mod.InstalledWeapon.isBeam)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(127),
                            (float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * 90f *
                            mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 83);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                        this.DrawStat(ref modTitlePos, "Duration", (float) mod.InstalledWeapon.BeamDuration, 188);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(127),
                            (float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus(), 83);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                }
                else
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(127),
                        (float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                        EmpireManager.Player.data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount, 83);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                modTitlePos.X = modTitlePos.X + 152f;
                modTitlePos.Y = starty;
                if (!mod.InstalledWeapon.isBeam && !mod.InstalledWeapon.isRepairBeam)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(129),
                        (float) ModifiedWeaponStat(mod.InstalledWeapon, "speed"), 85);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.DamageAmount > 0f)
                {
                    if (mod.InstalledWeapon.isBeam)
                    {
                        float dps = (float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() *
                                    90f * mod.InstalledWeapon.BeamDuration /
                                    (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus());
                        this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    else if (mod.InstalledWeapon.explodes && mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
                    {
                        if (mod.InstalledWeapon.SalvoCount <= 1)
                        {
                            float dps =
                                1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                                ((float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                                 EmpireManager.Player.data.OrdnanceEffectivenessBonus *
                                 mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float) mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                        }
                        else
                        {
                            float dps = (float) mod.InstalledWeapon.SalvoCount /
                                        (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") *
                                         GetHullFireRateBonus()) *
                                        ((float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") *
                                         GetHullDamageBonus() +
                                         EmpireManager.Player.data.OrdnanceEffectivenessBonus *
                                         mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float) mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                            this.DrawStat(ref modTitlePos, "Salvo", (float) mod.InstalledWeapon.SalvoCount, 182);
                            modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                        }
                    }
                    else if (mod.InstalledWeapon.SalvoCount <= 1)
                    {
                        float dps =
                            1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                            ((float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                             (float) mod.InstalledWeapon.DamageAmount *
                             EmpireManager.Player.data.Traits.EnergyDamageMod);
                        dps = dps * (float) mod.InstalledWeapon.ProjectileCount;
                        this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        float dps = (float) mod.InstalledWeapon.SalvoCount /
                                    (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                                    ((float) ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() +
                                     (float) mod.InstalledWeapon.DamageAmount *
                                     EmpireManager.Player.data.Traits.EnergyDamageMod);
                        dps = dps * (float) mod.InstalledWeapon.ProjectileCount;
                        this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                        this.DrawStat(ref modTitlePos, "Salvo", (float) mod.InstalledWeapon.SalvoCount, 182);
                        modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                    }
                }
                if (mod.InstalledWeapon.BeamPowerCostPerSecond > 0f)
                {
                    this.DrawStat(ref modTitlePos, "Pwr/s", (float) mod.InstalledWeapon.BeamPowerCostPerSecond, 87);
                    modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                }
                this.DrawStat(ref modTitlePos, "Delay", mod.InstalledWeapon.fireDelay, 183);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                if (mod.InstalledWeapon.EMPDamage > 0f)
                {
                    this.DrawStat(ref modTitlePos, "EMP",
                        1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) *
                        (float) mod.InstalledWeapon.EMPDamage, 110);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.SiphonDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.SiphonDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.SiphonDamage;
                    this.DrawStat(ref modTitlePos, "Siphon", damage, 184);
                    modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.MassDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.MassDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.MassDamage;
                    this.DrawStat(ref modTitlePos, "Tractor", damage, 185);
                    modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.PowerDamage > 0f)
                {
                    float damage;
                    if (mod.InstalledWeapon.isBeam)
                        damage = mod.InstalledWeapon.PowerDamage * 90f * mod.InstalledWeapon.BeamDuration;
                    else
                        damage = mod.InstalledWeapon.PowerDamage;
                    this.DrawStat(ref modTitlePos, "Pwr Dmg", damage, 186);
                    modTitlePos.Y += (float) Fonts.Arial12Bold.LineSpacing;
                }
                this.DrawStat(ref modTitlePos, Localizer.Token(130), (float) mod.FieldOfFire, 88);
                modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                if (mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
                {
                    this.DrawStat(ref modTitlePos, "Ord / Shot", (float) mod.InstalledWeapon.OrdinanceRequiredToFire,
                        89);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.PowerRequiredToFire > 0f)
                {
                    this.DrawStat(ref modTitlePos, "Pwr / Shot", (float) mod.InstalledWeapon.PowerRequiredToFire, 90);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.Tag_Guided && GlobalStats.ActiveModInfo != null &&
                    GlobalStats.ActiveModInfo.enableECM)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6005), (float) mod.InstalledWeapon.ECMResist, 155,
                        isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.EffectVsArmor != 1f)
                {
                    if (mod.InstalledWeapon.EffectVsArmor <= 1f)
                    {
                        float effectVsArmor = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
                        this.DrawVSResistBad(ref modTitlePos, "VS Armor",
                            string.Concat(effectVsArmor.ToString("#"), "%"), 147);
                    }
                    else
                    {
                        float single = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
                        this.DrawVSResist(ref modTitlePos, "VS Armor", string.Concat(single.ToString("#"), "%"), 147);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.EffectVSShields != 1f)
                {
                    if (mod.InstalledWeapon.EffectVSShields <= 1f)
                    {
                        float effectVSShields = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                        this.DrawVSResistBad(ref modTitlePos, "VS Shield",
                            string.Concat(effectVSShields.ToString("#"), "%"), 147);
                    }
                    else
                    {
                        float effectVSShields1 = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                        this.DrawVSResist(ref modTitlePos, "VS Shield",
                            string.Concat(effectVSShields1.ToString("#"), "%"), 147);
                    }
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InstalledWeapon.ShieldPenChance > 0)
                {
                    this.DrawStat(ref modTitlePos, "Shield Pen", mod.InstalledWeapon.ShieldPenChance, 181);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.OrdinanceCapacity != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(2129), (float) mod.OrdinanceCapacity, 124);
                    modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.InstalledWeapon.TruePD)
                {
                    string fireRest = "Cannot Target Ships";
                    modTitlePos.Y = modTitlePos.Y + 2 * ((float) Fonts.Arial12Bold.LineSpacing);
                    modTitlePos.X = modTitlePos.X - 152f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos,
                        Color.LightCoral);
                    return;
                }
                if (!mod.InstalledWeapon.TruePD && mod.InstalledWeapon.Excludes_Fighters ||
                    mod.InstalledWeapon.Excludes_Corvettes || mod.InstalledWeapon.Excludes_Capitals ||
                    mod.InstalledWeapon.Excludes_Stations)
                {
                    string fireRest = "Cannot Target:";
                    modTitlePos.Y = modTitlePos.Y + 2 * ((float) Fonts.Arial12Bold.LineSpacing);
                    modTitlePos.X = modTitlePos.X - 152f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos,
                        Color.LightCoral);
                    modTitlePos.X = modTitlePos.X + 120f;

                    if (mod.InstalledWeapon.Excludes_Fighters)
                    {
                        if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                        {
                            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Drones", modTitlePos,
                                Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                        }
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Fighters", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Corvettes)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Corvettes", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Capitals)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Capitals", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.Excludes_Stations)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Stations", modTitlePos,
                            Color.LightCoral);
                        modTitlePos.Y = modTitlePos.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }

                    return;
                }
                else
                    return;
            }
        }

        private void DrawHullSelection()
        {
            Rectangle r = this.HullSelectionSub.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
            sel.Draw();
            this.HullSL.Draw(ScreenManager.SpriteBatch);
            float x = (float) Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float) state.Y);
            this.HullSelectionSub.Draw();
            Vector2 bCursor = new Vector2((float) (this.HullSelectionSub.Menu.X + 10),
                (float) (this.HullSelectionSub.Menu.Y + 45));
            for (int i = this.HullSL.indexAtTop;
                i < this.HullSL.Copied.Count && i < this.HullSL.indexAtTop + this.HullSL.entriesToDisplay;
                i++)
            {
                bCursor = new Vector2((float) (this.HullSelectionSub.Menu.X + 10),
                    (float) (this.HullSelectionSub.Menu.Y + 45));
                ScrollList.Entry e = this.HullSL.Copied[i];
                bCursor.Y = (float) e.clickRect.Y;
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).Draw(ScreenManager, bCursor);
                }
                else if (e.item is ShipData)
                {
                    bCursor.X = bCursor.X + 10f;
                    ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict[(e.item as ShipData).IconPath],
                        new Rectangle((int) bCursor.X, (int) bCursor.Y, 29, 30), Color.White);
                    Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as ShipData).Name, tCursor,
                        Color.White);
                    tCursor.Y = tCursor.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold,
                        Localizer.GetRole((e.item as ShipData).Role, EmpireManager.Player), tCursor, Color.Orange);
                    if (e.clickRect.HitTest(MousePos))
                    {
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
                }
            }
        }

        private void DrawList()
        {
            float h;
            float x = (float) Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float) state.Y);
            Vector2 bCursor = new Vector2((float) (this.ModSel.Menu.X + 10), (float) (this.ModSel.Menu.Y + 45));
            for (int i = this.WeaponSl.indexAtTop;
                i < this.WeaponSl.Copied.Count && i < this.WeaponSl.indexAtTop + this.WeaponSl.entriesToDisplay;
                i++)
            {
                bCursor = new Vector2((float) (this.ModSel.Menu.X + 10), (float) (this.ModSel.Menu.Y + 45));
                ScrollList.Entry e = this.WeaponSl.Copied[i];
                bCursor.Y = (float) e.clickRect.Y;
                if (e.item is ModuleHeader)
                {
                    (e.item as ModuleHeader).Draw(ScreenManager, bCursor);
                }
                else if (e.item is ShipModule mod)
                {
                    bCursor.X += 5f;
                    ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);
                    Rectangle modRect = new Rectangle((int) bCursor.X, (int) bCursor.Y,
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width,
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height);
                    Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
                    Vector2 vector21 =
                        new Vector2(
                            (float) (ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width / 2),
                            (float) (ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height / 2));
                    float aspectRatio =
                        (float) ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width /
                        (float) ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height;
                    float w = (float) modRect.Width;
                    for (h = (float) modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
                    {
                        w = w - aspectRatio * 1.6f;
                    }
                    modRect.Width = (int) w;
                    modRect.Height = (int) h;
                    ScreenManager.SpriteBatch.Draw(
                        ResourceManager.TextureDict[moduleTemplate.IconTexturePath], modRect, Color.White);
                    //Added by McShooterz: allow longer modules names
                    Vector2 tCursor = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);
                    if (Fonts.Arial12Bold.MeasureString(Localizer.Token((e.item as ShipModule).NameIndex)).X + 90 <
                        this.ModSel.Menu.Width)
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                            Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float) Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold,
                            Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float) Fonts.Arial11Bold.LineSpacing;
                    }
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, moduleTemplate.Restrictions.ToString(),
                        tCursor, Color.Orange);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(moduleTemplate.Restrictions.ToString()).X;
                    if (moduleTemplate.InstalledWeapon != null && moduleTemplate.ModuleType != ShipModuleType.Turret ||
                        moduleTemplate.XSIZE != moduleTemplate.YSIZE)
                    {
                        Rectangle rotateRect = new Rectangle((int) bCursor.X + 240, (int) bCursor.Y + 3, 20, 22);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_can_rotate"],
                            rotateRect, Color.White);
                        if (rotateRect.HitTest(MousePos))
                        {
                            ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys",
                                ScreenManager);
                        }
                    }
                    if (e.clickRect.HitTest(MousePos))
                    {
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
                }
            }
        }

        private bool RestrictedModCheck(ShipData.RoleName Role, ShipModule Mod)
        {
            if (Mod.FighterModule || Mod.CorvetteModule || Mod.FrigateModule || Mod.StationModule ||
                Mod.DestroyerModule || Mod.CruiserModule
                || Mod.CarrierModule || Mod.CapitalModule || Mod.FreighterModule || Mod.PlatformModule ||
                Mod.DroneModule)
            {
                if (Role == ShipData.RoleName.drone && Mod.DroneModule == false) return true;
                if (Role == ShipData.RoleName.scout && Mod.FighterModule == false) return true;
                if (Role == ShipData.RoleName.fighter && Mod.FighterModule == false) return true;
                if (Role == ShipData.RoleName.corvette && Mod.CorvetteModule == false) return true;
                if (Role == ShipData.RoleName.gunboat && Mod.CorvetteModule == false) return true;
                if (Role == ShipData.RoleName.frigate && Mod.FrigateModule == false) return true;
                if (Role == ShipData.RoleName.destroyer && Mod.DestroyerModule == false) return true;
                if (Role == ShipData.RoleName.cruiser && Mod.CruiserModule == false) return true;
                if (Role == ShipData.RoleName.carrier && Mod.CarrierModule == false) return true;
                if (Role == ShipData.RoleName.capital && Mod.CapitalModule == false) return true;
                if (Role == ShipData.RoleName.freighter && Mod.FreighterModule == false) return true;
                if (Role == ShipData.RoleName.platform && Mod.PlatformModule == false) return true;
                if (Role == ShipData.RoleName.station && Mod.StationModule == false) return true;
            }
            else if (Mod.FightersOnly)
            {
                if (Role == ShipData.RoleName.fighter) return true;
                if (Role == ShipData.RoleName.scout) return true;
                if (Role == ShipData.RoleName.corvette) return true;
                if (Role == ShipData.RoleName.gunboat) return true;
            }

            return false;
        }

        private void DrawModuleSelection()
        {
            Rectangle r = this.ModSel.Menu;
            r.Y = r.Y + 25;
            r.Height = r.Height - 25;
            Selector sel = new Selector(ScreenManager, r, new Color(0, 0, 0, 210));
            sel.Draw();
            this.ModSel.Draw();
            this.WeaponSl.Draw(ScreenManager.SpriteBatch);
            float x = (float) Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 vector2 = new Vector2(x, (float) state.Y);
            if (this.ModSel.Tabs[0].Selected)
            {
                if (this.Reset)
                {
                    this.WeaponSl.Entries.Clear();
                    Array<string> WeaponCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                        if (tmp.isWeapon)
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats)
                            {
                                if (tmp.InstalledWeapon.Tag_Flak && !WeaponCategories.Contains("Flak Cannon"))
                                {
                                    WeaponCategories.Add("Flak Cannon");
                                    ModuleHeader type = new ModuleHeader("Flak Cannon", 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Railgun && !WeaponCategories.Contains("Magnetic Cannon"))
                                {
                                    WeaponCategories.Add("Magnetic Cannon");
                                    ModuleHeader type = new ModuleHeader("Magnetic Cannon", 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Array && !WeaponCategories.Contains("Beam Array"))
                                {
                                    WeaponCategories.Add("Beam Array");
                                    ModuleHeader type = new ModuleHeader("Beam Array", 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Tractor && !WeaponCategories.Contains("Tractor Beam"))
                                {
                                    WeaponCategories.Add("Tractor Beam");
                                    ModuleHeader type = new ModuleHeader("Tractor Beam", 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided &&
                                    !WeaponCategories.Contains("Unguided Rocket"))
                                {
                                    WeaponCategories.Add("Unguided Rocket");
                                    ModuleHeader type = new ModuleHeader("Unguided Rocket", 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                                else if (!WeaponCategories.Contains(tmp.InstalledWeapon.WeaponType))
                                {
                                    WeaponCategories.Add(tmp.InstalledWeapon.WeaponType);
                                    ModuleHeader type = new ModuleHeader(tmp.InstalledWeapon.WeaponType, 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                            }
                            else
                            {
                                if (!WeaponCategories.Contains(tmp.InstalledWeapon.WeaponType))
                                {
                                    WeaponCategories.Add(tmp.InstalledWeapon.WeaponType);
                                    ModuleHeader type = new ModuleHeader(tmp.InstalledWeapon.WeaponType, 240f);
                                    this.WeaponSl.AddItem(type);
                                }
                            }
                        }
                        else if (tmp.ModuleType == ShipModuleType.Bomb && !WeaponCategories.Contains("Bomb"))
                        {
                            WeaponCategories.Add("Bomb");
                            ModuleHeader type = new ModuleHeader("Bomb", 240f);
                            this.WeaponSl.AddItem(type);
                        }
                        tmp = null;
                    }
                    foreach (ScrollList.Entry e in this.WeaponSl.Entries)
                    {
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();
                            bool restricted =
                                tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule ||
                                tmp.DestroyerModule || tmp.CruiserModule
                                || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule ||
                                tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                //mer
                            }
                            // if not using new tags, ensure original <FightersOnly> still functions as in vanilla.
                            else if (!restricted && tmp.FightersOnly &&
                                     this.ActiveHull.Role != ShipData.RoleName.fighter &&
                                     this.ActiveHull.Role != ShipData.RoleName.scout &&
                                     this.ActiveHull.Role != ShipData.RoleName.corvette &&
                                     this.ActiveHull.Role != ShipData.RoleName.gunboat)
                                continue;
                            if (tmp.isWeapon)
                            {
                                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats)
                                {
                                    if (tmp.InstalledWeapon.Tag_Flak || tmp.InstalledWeapon.Tag_Array ||
                                        tmp.InstalledWeapon.Tag_Railgun || tmp.InstalledWeapon.Tag_Tractor ||
                                        (tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided))
                                    {
                                        if ((e.item as ModuleHeader).Text == "Flak Cannon" &&
                                            tmp.InstalledWeapon.Tag_Flak)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Magnetic Cannon" &&
                                            tmp.InstalledWeapon.Tag_Railgun)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Beam Array" &&
                                            tmp.InstalledWeapon.Tag_Array)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Tractor Beam" &&
                                            tmp.InstalledWeapon.Tag_Tractor)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Unguided Rocket" &&
                                            tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided)
                                            e.AddItem(module.Value);
                                    }
                                    else if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                    {
                                        e.AddItem(module.Value);
                                    }
                                }
                                else
                                {
                                    if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                    {
                                        e.AddItem(module.Value);
                                    }
                                }
                            }
                            else if (tmp.ModuleType == ShipModuleType.Bomb && (e.item as ModuleHeader).Text == "Bomb")
                            {
                                e.AddItem(module.Value);
                            }
                            tmp = null;
                        }
                    }
                    this.Reset = false;
                }
                this.DrawList();
            }
            if (this.ModSel.Tabs[2].Selected)
            {
                if (this.Reset)
                {
                    this.WeaponSl.Entries.Clear();
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                        if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield ||
                             tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead &&
                            !tmp.isPowerArmour && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
                        {
                            ModuleCategories.Add(tmp.ModuleType.ToString());
                            ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                            this.WeaponSl.AddItem(type);
                        }

                        // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                        if (tmp.isPowerArmour && tmp.ModuleType == ShipModuleType.Armor &&
                            !ModuleCategories.Contains(Localizer.Token(6172)))
                        {
                            ModuleCategories.Add(Localizer.Token(6172));
                            ModuleHeader type = new ModuleHeader(Localizer.Token(6172), 240f);
                            this.WeaponSl.AddItem(type);
                        }
                        if (tmp.isBulkhead && tmp.ModuleType == ShipModuleType.Armor &&
                            !ModuleCategories.Contains(Localizer.Token(6173)))
                        {
                            ModuleCategories.Add(Localizer.Token(6173));
                            ModuleHeader type = new ModuleHeader(Localizer.Token(6173), 240f);
                            this.WeaponSl.AddItem(type);
                        }

                        tmp = null;
                    }
                    foreach (ScrollList.Entry e in this.WeaponSl.Entries)
                    {
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                            if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield ||
                                 tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead &&
                                !tmp.isPowerArmour && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
                            {
                                e.AddItem(module.Value);
                            }
                            if (tmp.isPowerArmour && (e.item as ModuleHeader).Text == Localizer.Token(6172))
                            {
                                e.AddItem(module.Value);
                            }
                            if (tmp.isBulkhead && (e.item as ModuleHeader).Text == Localizer.Token(6173))
                            {
                                e.AddItem(module.Value);
                            }
                            tmp = null;
                        }
                    }
                    this.Reset = false;
                }
                this.DrawList();
            }
            if (this.ModSel.Tabs[1].Selected)
            {
                if (this.Reset)
                {
                    this.WeaponSl.Entries.Clear();
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                        if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell ||
                             tmp.ModuleType == ShipModuleType.PowerPlant ||
                             tmp.ModuleType == ShipModuleType.PowerConduit) &&
                            !ModuleCategories.Contains(tmp.ModuleType.ToString()))
                        {
                            ModuleCategories.Add(tmp.ModuleType.ToString());
                            ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                            this.WeaponSl.AddItem(type);
                        }
                        tmp = null;
                    }
                    foreach (ScrollList.Entry e in this.WeaponSl.Entries)
                    {
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                            if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell ||
                                 tmp.ModuleType == ShipModuleType.PowerPlant ||
                                 tmp.ModuleType == ShipModuleType.PowerConduit) &&
                                (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
                            {
                                e.AddItem(module.Value);
                            }
                            tmp = null;
                        }
                    }
                    this.Reset = false;
                }
                this.DrawList();
            }
            WeaponSl.DrawModules();
        }

        private void DrawRequirement(ref Vector2 Cursor, string words, bool met)
        {
            float amount = 165f;
            if (GlobalStats.IsGermanFrenchOrPolish)
            {
                amount = amount + 35f;
            }
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor,
                (met ? Color.LightGreen : Color.LightPink));
            string stats = (met ? "OK" : "X");
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stats).X);
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stats, Cursor,
                (met ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stats).X);
        }

        private void DrawShipInfoPanel()
        {
            float HitPoints = 0f;
            float Mass = 0f;
            float PowerDraw = 0f;
            float PowerCapacity = 0f;
            float OrdnanceCap = 0f;
            float PowerFlow = 0f;
            float ShieldPower = 0f;
            float Thrust = 0f;
            float AfterThrust = 0f;
            float CargoSpace = 0f;
            int TroopCount = 0;
            float Size = 0f;
            float Cost = 0f;
            float WarpThrust = 0f;
            float TurnThrust = 0f;
            float WarpableMass = 0f;
            float WarpDraw = 0f;
            float FTLCount = 0f;
            float FTLSpeed = 0f;
            float RepairRate = 0f;
            float sensorRange = 0f;
            float sensorBonus = 0f;
            float BeamLongestDuration = 0f;
            float OrdnanceUsed = 0f;
            float OrdnanceRecoverd = 0f;
            float WeaponPowerNeeded = 0f;
            float Upkeep = 0f;
            float FTLSpoolTimer = 0f;
            float EMPResist = 0f;
            bool bEnergyWeapons = false;
            float Off = 0f;
            float Def = 0;
            float strength = 0;
            float targets = 0;
            int fixedtargets = 0;
            float TotalECM = 0f;

            // bonuses are only available in mods
            ResourceManager.HullBonuses.TryGetValue(ActiveHull.Hull, out HullBonus bonus);

            foreach (SlotStruct slot in this.Slots)
            {
                Size = Size + 1f;
                if (slot.Module == null)
                {
                    continue;
                }
                HitPoints = HitPoints + (slot.Module.Health +
                                         EmpireManager.Player.data.Traits.ModHpModifier * slot.Module.Health);
                if (slot.Module.Mass < 0f && slot.Powered)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                else if (slot.Module.Mass > 0f)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                TroopCount += slot.Module.TroopCapacity;
                PowerCapacity += slot.Module.PowerStoreMax +
                                 slot.Module.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier;
                OrdnanceCap = OrdnanceCap + (float) slot.Module.OrdinanceCapacity;
                PowerFlow += slot.Module.PowerFlowMax +
                             slot.Module.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod;
                if (slot.Module.Powered)
                {
                    EMPResist += slot.Module.EMP_Protection;
                    WarpableMass = WarpableMass + slot.Module.WarpMassCapacity;
                    PowerDraw = PowerDraw + slot.Module.PowerDraw;
                    WarpDraw = WarpDraw + slot.Module.PowerDrawAtWarp;
                    if (slot.Module.ECM > TotalECM)
                        TotalECM = slot.Module.ECM;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.PowerRequiredToFire > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.BeamPowerCostPerSecond > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.FTLSpeed > 0f)
                    {
                        FTLCount = FTLCount + 1f;
                        FTLSpeed = FTLSpeed + slot.Module.FTLSpeed;
                    }
                    if (slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier > FTLSpoolTimer)
                    {
                        FTLSpoolTimer = slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier;
                    }
                    ShieldPower += slot.Module.shield_power_max +
                                   EmpireManager.Player.data.ShieldPowerMod * slot.Module.shield_power_max;
                    Thrust = Thrust + slot.Module.thrust;
                    WarpThrust = WarpThrust + slot.Module.WarpThrust;
                    TurnThrust = TurnThrust + slot.Module.TurnThrust;

                    RepairRate += ((slot.Module.BonusRepairRate + slot.Module.BonusRepairRate *
                                    EmpireManager.Player.data.Traits.RepairMod) * (1f + bonus?.RepairBonus ?? 0));
                    OrdnanceRecoverd += slot.Module.OrdnanceAddedPerSecond;
                    if (slot.Module.SensorRange > sensorRange)
                    {
                        sensorRange = slot.Module.SensorRange;
                    }
                    if (slot.Module.SensorBonus > sensorBonus)
                        sensorBonus = slot.Module.SensorBonus;

                    //added by gremlin collect weapon stats                  
                    if (slot.Module.isWeapon || slot.Module.BombType != null)
                    {
                        Weapon weapon;
                        if (slot.Module.BombType == null)
                            weapon = slot.Module.InstalledWeapon;
                        else
                            weapon = ResourceManager.WeaponsDict[slot.Module.BombType];
                        OrdnanceUsed += weapon.OrdinanceRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        WeaponPowerNeeded += weapon.PowerRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        if (weapon.isBeam)
                            WeaponPowerNeeded += weapon.BeamPowerCostPerSecond * weapon.BeamDuration / weapon.fireDelay;
                        if (BeamLongestDuration < weapon.BeamDuration)
                            BeamLongestDuration = weapon.BeamDuration;
                    }
                    //end
                    if (slot.Module.FixedTracking > fixedtargets)
                        fixedtargets = slot.Module.FixedTracking;

                    targets += slot.Module.TargetTracking;
                }
                Cost = Cost + slot.Module.Cost * UniverseScreen.GamePaceStatic;
                CargoSpace = CargoSpace + slot.Module.Cargo_Capacity;
            }

            targets += fixedtargets;

            Mass = Mass + (float) (ActiveHull.ModuleSlots.Length / 2);
            Mass = Mass * EmpireManager.Player.data.MassModifier;
            if (Mass < (float) (ActiveHull.ModuleSlots.Length / 2))
            {
                Mass = (float) (ActiveHull.ModuleSlots.Length / 2);
            }
            float Speed = 0f;
            float WarpSpeed = WarpThrust / (Mass + 0.1f);
            //Added by McShooterz: hull bonus speed
            WarpSpeed *= EmpireManager.Player.data.FTLModifier * (1f + bonus?.SpeedBonus ?? 0);
            float single = WarpSpeed / 1000f;
            string WarpString = string.Concat(single.ToString("#.0"), "k");
            float Turn = 0f;
            if (Mass > 0f)
            {
                Speed = Thrust / Mass;
                Turn = TurnThrust / Mass / 700f;
            }
            float AfterSpeed = AfterThrust / (Mass + 0.1f);
            AfterSpeed = AfterSpeed * EmpireManager.Player.data.SubLightModifier;
            Turn = (float) MathHelper.ToDegrees(Turn);
            Vector2 Cursor = new Vector2((float) (this.StatsSub.Menu.X + 10), (float) (this.ShipStats.Menu.Y + 33));

            if (bonus != null) //Added by McShooterz: Draw Hull Bonuses
            {
                Vector2 LCursor = new Vector2(this.HullSelectionRect.X - 145, HullSelectionRect.Y + 31);
                if (bonus.ArmoredBonus != 0 || bonus.ShieldBonus != 0 || bonus.SensorBonus != 0 ||
                    bonus.SpeedBonus != 0 || bonus.CargoBonus != 0 || bonus.DamageBonus != 0 ||
                    bonus.FireRateBonus != 0 || bonus.RepairBonus != 0 || bonus.CostBonus != 0)
                {
                    ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold, Localizer.Token(6015), LCursor,
                        Color.Orange);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Verdana14Bold.LineSpacing + 2);
                }
                if (bonus.ArmoredBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6016), bonus.ArmoredBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.ShieldBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Shield Strength", bonus.ShieldBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SensorBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6017), bonus.SensorBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SpeedBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6018), bonus.SpeedBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CargoBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6019), bonus.CargoBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.DamageBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Weapon Damage", bonus.DamageBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.FireRateBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6020), bonus.FireRateBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.RepairBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6013), bonus.RepairBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CostBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6021), bonus.CostBonus);
                    LCursor.Y = LCursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
            //Added by McShooterz: hull bonus starting cost
            DrawStat(ref Cursor, Localizer.Token(109) + ":",
                ((int) Cost + (bonus?.StartingCost ?? 0)) * (1f - bonus?.CostBonus ?? 0), 99);
            Cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                Upkeep = GetMaintCostShipyardProportional(this.ActiveHull, Cost, EmpireManager.Player);
            }
            else
            {
                Upkeep = GetMaintCostShipyard(this.ActiveHull, Size, EmpireManager.Player);
            }

            this.DrawStat(ref Cursor, "Upkeep Cost:", -Upkeep, 175);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing +
                                           2); //Gretman (so we can see how many total slots are on the ships)
            this.DrawStat(ref Cursor, "Ship UniverseRadius:", (float) ActiveHull.ModuleSlots.Length, 230);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);

            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(110), ":"), PowerCapacity, 100,
                Color.LightSkyBlue);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(111), ":"), (PowerFlow - PowerDraw), 101,
                Color.LightSkyBlue);

            //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            float fDrawAtWarp = 0;
            if (WarpDraw != 0)
            {
                fDrawAtWarp = (PowerFlow - (WarpDraw / 2 * EmpireManager.Player.data.FTLPowerDrainModifier +
                                            (PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier)));
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102,
                        Color.LightSkyBlue);
                }
            }
            else
            {
                fDrawAtWarp = (PowerFlow - PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier);
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102,
                        Color.LightSkyBlue);
                }
            }


            float fWarpTime = ((-PowerCapacity / fDrawAtWarp) * 0.9f);
            string sWarpTime = fWarpTime.ToString("0.#");
            if (WarpSpeed > 0)
            {
                if (fDrawAtWarp < 0)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", sWarpTime, 176);
                }
                else if (fWarpTime > 900)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }
            }


            float powerconsumed = WeaponPowerNeeded - PowerFlow;
            float EnergyDuration = 0f;
            if (powerconsumed > 0)
            {
                EnergyDuration = WeaponPowerNeeded > 0 ? ((PowerCapacity) / powerconsumed) : 0;
                if ((EnergyDuration >= BeamLongestDuration) && bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, "Power Time:", EnergyDuration, 163, Color.LightSkyBlue);
                }
                else if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergyBad(ref Cursor, "Power Time:", EnergyDuration.ToString("N1"), 163);
                }
            }
            else
            {
                if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "Power Time:", "INF", 163);
                }
            }
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(113), ":"), HitPoints, 103, Color.Goldenrod);
            //Added by McShooterz: draw total repair
            if (RepairRate > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6013), ":"), RepairRate, 236,
                    Color.Goldenrod);
            }
            if (ShieldPower > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(114), ":"), ShieldPower, 104,
                    Color.Goldenrod);
            }
            if (EMPResist > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6177), ":"), EMPResist, 220,
                    Color.Goldenrod);
            }
            if (TotalECM > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6189), ":"), TotalECM, 234,
                    Color.Goldenrod, isPercent: true);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);


            // The Doctor: removed the mass display. It's a meaningless value to the player, and it takes up a valuable line in the limited space.
            //this.DrawStat(ref Cursor, string.Concat(Localizer.Token(115), ":"), (int)Mass, 79);
            //Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);

            #region HardcoreRule info

            if (GlobalStats.HardcoreRuleset)
            {
                string massstring = GetNumberString(Mass);
                string wmassstring = GetNumberString(WarpableMass);
                string warpmassstring = string.Concat(massstring, "/", wmassstring);
                if (Mass > WarpableMass)
                {
                    this.DrawStatBad(ref Cursor, "Warpable Mass:", warpmassstring, 153);
                }
                else
                {
                    this.DrawStat(ref Cursor, "Warpable Mass:", warpmassstring, 153);
                }
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawRequirement(ref Cursor, "Warp Capable", Mass <= WarpableMass);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                if (FTLCount > 0f)
                {
                    float speed = FTLSpeed / FTLCount;
                    this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), speed, 135);
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                }
            }

            #endregion

            else if (WarpSpeed <= 0f)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(2170), ":"), 0, 135, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            else
            {
                this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(2170), ":"), WarpString, 135);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (WarpSpeed > 0 && FTLSpoolTimer > 0)
            {
                this.DrawStatColor(ref Cursor, "FTL Spool:", FTLSpoolTimer, 177, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(116), ":"),
                (Speed * EmpireManager.Player.data.SubLightModifier *
                 (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                     ? 1f + bonus.SpeedBonus
                     : 1)), 105, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            //added by McShooterz: afterburn speed
            if (AfterSpeed != 0)
            {
                this.DrawStatColor(ref Cursor, "Afterburner Speed:", AfterSpeed, 105, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(117), ":"), Turn, 107, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);
            if (OrdnanceCap > 0)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(118), ":"), OrdnanceCap, 108,
                    Color.IndianRed);
            }
            if (OrdnanceRecoverd > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, "Ordnance Created / s:", OrdnanceRecoverd, 162, Color.IndianRed);
            }
            if (OrdnanceCap > 0)
            {
                float AmmoTime = 0f;
                if (OrdnanceUsed - OrdnanceRecoverd > 0)
                {
                    AmmoTime = OrdnanceCap / (OrdnanceUsed - OrdnanceRecoverd);
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, "Ammo Time:", AmmoTime, 164, Color.IndianRed);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatOrdnance(ref Cursor, "Ammo Time:", "INF", 164);
                }
            }
            if (TroopCount > 0)
            {
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6132), ":"), (float) TroopCount, 180,
                    Color.IndianRed);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 10);

            if (CargoSpace > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(119), ":"),
                    (CargoSpace +
                     (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                      ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                         ? CargoSpace * bonus.CargoBonus
                         : 0)), 109);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (sensorRange != 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6130), ":"),
                    ((sensorRange + sensorBonus) +
                     (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses &&
                      ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull)
                         ? (sensorRange + sensorBonus) * bonus.SensorBonus
                         : 0)), 235);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (targets > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6188), ":"), ((targets + 1f)), 232);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }

            Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing);
            bool hasBridge = false;
            bool EmptySlots = true;
            foreach (SlotStruct slot in this.Slots)
            {
                if (slot.ModuleUID == null)
                    EmptySlots = false;

                if (slot.Module != null)
                {
                    Off += ResourceManager.CalculateModuleOffense(slot.Module);
                    Def += ResourceManager.CalculateModuleDefense(slot.Module, (int) Size);
                }
                if (slot.ModuleUID == null || !ResourceManager.GetModuleTemplate(slot.ModuleUID).IsCommandModule)
                    continue;

                hasBridge = true;
            }
            strength = (Def > Off ? Off * 2 : Def + Off);
            if (strength > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6190), ":"), strength, 227);
                Cursor.Y = Cursor.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            Vector2 CursorReq = new Vector2((float) (this.StatsSub.Menu.X - 180),
                (float) (this.ShipStats.Menu.Y + (Fonts.Arial12Bold.LineSpacing * 2) + 45));
            if (this.ActiveHull.Role != ShipData.RoleName.platform)
            {
                this.DrawRequirement(ref CursorReq, Localizer.Token(120), hasBridge);
                CursorReq.Y = CursorReq.Y + (float) (Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawRequirement(ref CursorReq, Localizer.Token(121), EmptySlots);
        }

        private void DrawHullBonus(ref Vector2 Cursor, string words, float stat)
        {
            ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12,
                string.Concat((stat * 100f).ToString(), "% ", words), Cursor, Color.Orange);
        }

        private void DrawStatColor(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, Color color
            , bool doGoodBadTint = true, bool isPercent = false)
        {
            SpriteFont font = Fonts.Arial12Bold;
            float amount = Spacing(120f);
            DrawString(Cursor, color, words, font);
            string numbers = "0.0";
            numbers = isPercent ? stat.ToString("P1") : GetNumberString(stat);
            if (stat == 0f) numbers = "0.0";
            Cursor = FontSpace(Cursor, amount, numbers, font);

            color = doGoodBadTint ? (stat > 0f ? Color.LightGreen : Color.LightPink) : Color.White;
            DrawString(Cursor, color, numbers, font);

            Cursor = FontBackSpace(Cursor, amount, numbers, font);

            CheckToolTip(Tooltip_ID, Cursor, words, numbers, font, MousePos);
        }

        private void DrawStat(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, bool doGoodBadTint = true
            , bool isPercent = false)
        {
            DrawStatColor(ref Cursor, words, stat, Tooltip_ID, Color.White, doGoodBadTint, isPercent);
        }

        private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID, Color nameColor,
            Color statColor, float spacing = 165f)
        {
            SpriteFont font = Fonts.Arial12Bold;
            float amount = Spacing(spacing);
            Color color = nameColor;
            DrawString(Cursor, color, words, font);
            Cursor = FontSpace(Cursor, amount, words, font);
            color = statColor;
            DrawString(Cursor, color, stat, font);
            Cursor = FontBackSpace(Cursor, amount, stat, font);
            CheckToolTip(Tooltip_ID, Cursor, words, stat, font, MousePos);
        }

        private void DrawStatEnergy(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.LightSkyBlue, Color.LightGreen);
        }

        private void DrawStatPropulsion(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.DarkSeaGreen, Color.LightGreen);
        }

        private void DrawStatOrdnance(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen);
        }

        private void DrawVSResist(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 105);
        }

        private void DrawVSResistBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 105);
        }

        private void DrawStatBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightPink, 165);
        }

        private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.White, Color.LightGreen, 165);
        }

        private void DrawStatEnergyBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            DrawStat(ref Cursor, words, stat, Tooltip_ID, Color.LightSkyBlue, Color.LightPink, 165);
        }

        private void DrawUI(GameTime gameTime)
        {
            this.EmpireUI.Draw(ScreenManager.SpriteBatch);
            this.DrawShipInfoPanel();

            //Defaults based on hull types
            //Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
            if (this.ActiveHull.Role == ShipData.RoleName.freighter && this.Fml)
            {
                this.CategoryList.ActiveIndex = 1;
                this.Fml = false;
            }
            //Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
            else if (this.ActiveHull.Role == ShipData.RoleName.scout && this.Fml)
            {
                this.CategoryList.ActiveIndex = 2;
                this.Fml = false;
            }
            //All other hulls default to unclassified.
            else if (this.Fml)
            {
                this.CategoryList.ActiveIndex = 0;
                this.Fml = false;
            }

            //Loads the Category from the ShipDesign XML of the ship being loaded, and loads this OVER the hull type default, very importantly.
            foreach (Entry e in this.CategoryList.Options)
            {
                if (e.Name == LoadCategory.ToString() && this.Fmlevenmore)
                {
                    this.CategoryList.ActiveIndex = e.@value - 1;
                    this.Fmlevenmore = false;
                }
            }
            this.CategoryList.Draw(ScreenManager.SpriteBatch);
            this.CarrierOnlyBox.Draw(ScreenManager);
            string classifTitle = "Behaviour Presets";
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, classifTitle, ClassifCursor, Color.Orange);
            float transitionOffset = (float) Math.Pow((double) TransitionPosition, 2);
            Rectangle r = this.BlackBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, Color.Black);
            r = this.BottomSep;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(77, 55, 25));
            r = this.SearchBar;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            ScreenManager.SpriteBatch.FillRectangle(r, new Color(54, 54, 54));
            if (Fonts.Arial20Bold.MeasureString(this.ActiveHull.Name).X <= (float) (this.SearchBar.Width - 5))
            {
                Vector2 Cursor = new Vector2((float) (this.SearchBar.X + 3),
                    (float) (r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            else
            {
                Vector2 Cursor = new Vector2((float) (this.SearchBar.X + 3),
                    (float) (r.Y + 14 - Fonts.Arial12Bold.LineSpacing / 2));
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            r = this.SaveButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.SaveButton.Draw(ScreenManager.SpriteBatch, r);
            r = this.LoadButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.LoadButton.Draw(ScreenManager.SpriteBatch, r);
            r = this.ToggleOverlayButton.Rect;
            if (ScreenState == ScreenState.TransitionOn ||
                ScreenState == ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int) (transitionOffset * 50f);
            }
            this.ToggleOverlayButton.Draw(ScreenManager.SpriteBatch, r);
            this.DrawModuleSelection();
            this.DrawHullSelection();
            if (this.ActiveModule != null || this.HighlightedModule != null)
            {
                this.DrawActiveModuleData();
            }
            foreach (ToggleButton button in this.CombatStatusButtons)
            {
                button.Draw(ScreenManager);
            }
            if (IsActive)
            {
                ToolTip.Draw(ScreenManager);
            }
        }
    }
}