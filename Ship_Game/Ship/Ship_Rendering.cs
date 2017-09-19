using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
    #if DEBUG
        //private class TimedDebugLine
        //{
        //    public float Time;
        //    public Vector2 Start;
        //    public Vector2 End;
        //}
        //private readonly Array<TimedDebugLine> SparseGridDebug = new Array<TimedDebugLine>();
        //private void AddGridLocalDebugLine(float time, Vector2 localStart, Vector2 localEnd)
        //{
        //    SparseGridDebug.Add(new TimedDebugLine {
        //        Time = time, Start = localStart, End = localEnd
        //    });
        //}
        //private void AddGridLocalDebugCircle(float time, Vector2 localPoint)
        //{
        //    SparseGridDebug.Add(new TimedDebugLine {
        //        Time = time, Start = localPoint, End = localPoint
        //    });
        //}

        //private class TimedModuleDebug
        //{
        //    public float Time;
        //    public int X, Y;
        //}
        //private readonly Array<TimedModuleDebug> HitModuleDebug = new Array<TimedModuleDebug>();
        //private void AddGridLocalHitIndicator(float time, int x, int y)
        //{
        //    HitModuleDebug.Add(new TimedModuleDebug {
        //        Time = time, X = x, Y = y
        //    });
        //}

        //private readonly Array<TimedModuleDebug> GridRayTraceDebug = new Array<TimedModuleDebug>();
        //private void AddGridRayTraceDebug(float time, int x, int y)
        //{
        //    GridRayTraceDebug.Add(new TimedModuleDebug {
        //        Time = time, X = x, Y = y
        //    });
        //}

        //private class TimedDebugPoint
        //{
        //    public float Time;
        //    public Point GridPoint;  // grid local point
        //    public Vector2 LocalPos; // shiplocal position
        //}
        //private readonly Array<TimedDebugPoint> GridLocalDebugPoints = new Array<TimedDebugPoint>();
        //public void ShowGridLocalDebugPoint(Vector2 worldPos)
        //{
        //    Point gridPoint = WorldToGridLocalPoint(worldPos);
        //    Vector2 localPos = WorldToGridLocal(worldPos);
        //    GridLocalDebugPoints.Add(new TimedDebugPoint {
        //        Time = 5f, GridPoint = gridPoint, LocalPos = localPos
        //    });
        //}
    #endif

        private void DrawSparseModuleGrid(UniverseScreen us)
        {
            if (false && SparseModuleGrid.Length != 0)
            {
                for (int y = 0; y < GridHeight; ++y)
                {
                    for (int x = 0; x < GridWidth; ++x)
                    {
                        Color color = Color.DarkGray;
                        if    (ExternalModuleGrid[x + y * GridWidth] != null) color = Color.Blue;
                        else if (SparseModuleGrid[x + y * GridWidth] != null) color = Color.Yellow;

                        us.DrawRectangleProjected(GridSquareToWorld(x, y), new Vector2(16f, 16f), Rotation, color);
                    }
                }
            }
        #if DEBUG
            //for (int i = 0; i < SparseGridDebug.Count; ++i)
            //{
            //    TimedDebugLine tdp = SparseGridDebug[i];
            //    Vector2 endPos = GridLocalToWorld(tdp.End);
            //    if (tdp.End != tdp.Start) {
            //        Vector2 startPos = GridLocalToWorld(tdp.Start);
            //        us.DrawLineProjected(startPos, endPos, Color.Magenta);
            //        us.DrawCircleProjected(endPos, 4f, 24, Color.Orange, 1.5f);
            //    } else {
            //        us.DrawCircleProjected(endPos, 4f, 24, Color.Green, 1.5f);
            //    }
            //    if ((tdp.Time -= 1f/60f) <= 0f) SparseGridDebug.RemoveAtSwapLast(i--);
            //}

            //for (int i = 0; i < HitModuleDebug.Count; ++i)
            //{
            //    TimedModuleDebug tmd = HitModuleDebug[i];
            //    us.DrawCircleProjected(GridSquareToWorld(tmd.X, tmd.Y), 10.0f, 20, Color.Cyan, 4f);
            //    if ((tmd.Time -= 1f / 60f) <= 0f) HitModuleDebug.RemoveAtSwapLast(i--);
            //}

            //for (int i = 0; i < GridRayTraceDebug.Count; ++i)
            //{
            //    TimedModuleDebug tmd = GridRayTraceDebug[i];
            //    us.DrawRectangleProjected(GridSquareToWorld(tmd.X, tmd.Y), new Vector2(16f, 16f), Rotation, Color.Gold);
            //    if ((tmd.Time -= 1f / 60f) <= 0f) GridRayTraceDebug.RemoveAtSwapLast(i--);
            //}

            //for (int i = 0; i < GridLocalDebugPoints.Count; ++i)
            //{
            //    TimedDebugPoint tdp = GridLocalDebugPoints[i];

            //    Vector2 worldPos = GridSquareToWorld(tdp.GridPoint);
            //    us.DrawRectangleProjected(worldPos, new Vector2(16f, 16f), Rotation, Color.Maroon);
            //    us.DrawCircleProjected(GridLocalToWorld(tdp.LocalPos), 4.0f, 24, Color.DarkRed, 4f);
            //    us.DrawStringProjected(worldPos, Rotation, 350f / us.camHeight, Color.Magenta, $"X {tdp.GridPoint.X}, Y {tdp.GridPoint.Y}");

            //    if ((tdp.Time -= 1f / 60f) <= 0f) GridLocalDebugPoints.RemoveAtSwapLast(i--);
            //}
        #endif
        }


        public void DrawModulesOverlay(UniverseScreen us)
        {
            Texture2D symbolFighter = ResourceManager.Texture("TacticalIcons/symbol_fighter");
            Texture2D concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1"); // 1x1 gray ship module background tile, 16x16px in size
            Texture2D lightningBolt = ResourceManager.Texture("UI/lightningBolt");
                        
            if (us.DebugWin != null)
            {
                for (int i = 0; i < Projectiles.Count; i++)
                {
                    Projectile projectile = Projectiles[i];
                    if (projectile == null) continue;
                    us.DebugWin.DrawCircle(Debug.DebugModes.Targeting, projectile.Center, projectile.Radius);
                    //us.DrawCircleProjected(projectile.Center, projectile.Radius, 50, Color.Red, 3f);
                }
            }

            float shipDegrees = (float)Math.Round(Rotation.ToDegrees());
            float shipRotation = shipDegrees.ToRadians();

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot = ModuleSlotList[i];

                float moduleWidth  = slot.XSIZE * 16.5f; // using 16.5f instead of 16 to reduce pixel error flickering
                float moduleHeight = slot.YSIZE * 16.5f;
                us.ProjectToScreenCoords(slot.Center, moduleWidth, moduleHeight,
                                         out Vector2 posOnScreen, out float widthOnScreen, out float heightOnScreen);

                // round all the values to TRY prevent module flickering on screen (well, it only helps a tiny bit)
                posOnScreen.X = (float)Math.Round(posOnScreen.X);
                posOnScreen.Y = (float)Math.Round(posOnScreen.Y);

                float slotFacing = (int)((slot.Facing + 45) / 90) * 90f; // align the facing to 0, 90, 180, 270...
                float slotRotation = (shipDegrees + slotFacing).ToRadians();

                us.DrawTextureSized(concreteGlass, posOnScreen, shipRotation, widthOnScreen, heightOnScreen, Color.White);

                if (us.CamHeight > 6000.0f) // long distance view, draw the modules as colored icons
                {
                    us.DrawTextureSized(symbolFighter, posOnScreen, shipRotation, widthOnScreen, heightOnScreen, slot.GetHealthStatusColor());
                }
                else
                {
                    Texture2D moduleTex = ResourceManager.Texture(slot.IconTexturePath);
                    //@HACK the dimensions are already rotated so that rotating again puts it in the wrong orientation. 
                    //so to fix that i am switching the height and width if the module is facing left or right. 
                    if (slot.Facing == 270f || slot.Facing == 90)                    
                        us.DrawTextureSized(moduleTex, posOnScreen, slotRotation, heightOnScreen, widthOnScreen, slot.GetHealthStatusColorWhite());                    
                    else
                        us.DrawTextureSized(moduleTex, posOnScreen, slotRotation, widthOnScreen, heightOnScreen, slot.GetHealthStatusColorWhite());

                    //if (enableModuleDebug)
                    //{
                    //    us.DrawCircleProjected(slot.Center, slot.Radius, 20, Color.Red, 2f);
                    //}
                    if (slot.ModuleType == ShipModuleType.PowerConduit)
                    {
                        if (slot.Powered)
                        {
                            Texture2D poweredTex = ResourceManager.Texture(slot.IconTexturePath + "_power");
                            us.DrawTextureSized(poweredTex, posOnScreen, slotRotation, widthOnScreen, heightOnScreen, Color.White);
                        }
                    }
                    else if (slot.Active && !slot.Powered && slot.PowerDraw > 0.0f)
                    {
                        float smallerSize = Math.Min(widthOnScreen, heightOnScreen);
                        us.DrawTextureSized(lightningBolt, posOnScreen, slotRotation, smallerSize, smallerSize, Color.White);
                    }
                    if (us.Debug && slot.isExternal && slot.Active)
                    {
                        float smallerSize = Math.Min(widthOnScreen, heightOnScreen);
                        us.DrawTextureSized(symbolFighter, posOnScreen, slotRotation, smallerSize, smallerSize, new Color(0, 0, 255, 120));
                    }
                    if (us.Debug)
                    {
                        ModulePosToGridPoint(slot.Position, out int x, out int y);
                        us.DrawString(posOnScreen, shipRotation, 350f / us.CamHeight, Color.Magenta, $"X {x}, Y {y}");
                    }
                }

                // finally, draw firing arcs for the player ship
                if (PlayerShip && slot.FieldOfFire > 0.0f && slot.InstalledWeapon != null)
                    us.DrawWeaponArc(slot, posOnScreen, slotRotation);
            }

            if (us.Debug)
            {
                DrawSparseModuleGrid(us);
            }
        }


        private void DrawTactical(UniverseScreen us, Vector2 screenPos, float screenRadius, float minSize, float maxSize = 0f)
        {
            // try to scale the icon so its size remains consistent when zooming in/out
            float size = KeepConstantSize(screenRadius, minSize, maxSize);

            if (StrategicIconPath.IsEmpty())
                StrategicIconPath = "TacticalIcons/symbol_" + (isConstructor ? "construction" : shipData.GetRole());
            Texture2D icon = ResourceManager.Texture(StrategicIconPath) 
                          ?? ResourceManager.Texture("TacticalIcons/symbol_fighter"); // default symbol
            us.DrawTextureSized(icon, screenPos, Rotation, size, size, loyalty.EmpireColor);

            if (isColonyShip)
            {
                us.DrawTexture(ResourceManager.Texture("UI/flagicon"), 
                    screenPos + new Vector2(-7f, -17f), loyalty.EmpireColor);
            }
        }

        private float KeepConstantSize(float screenRadius, float minSize, float maxSize = 0)
        {
            float size = screenRadius * 2;
            if (size < minSize)
                size = minSize;
            else if (maxSize > 0f && size > maxSize)
                size = maxSize;
            return size;
        }

        public void DrawTacticalIcon(UniverseScreen us, UniverseScreen.UnivScreenState viewState)
        {
            float shipWorldRadius = ShipSO.WorldBoundingSphere.Radius;
            us.ProjectToScreenCoords(Position, shipWorldRadius, out Vector2 screenPos, out float screenRadius);

            if (viewState == UniverseScreen.UnivScreenState.GalaxyView)
            {
                if (!us.IsShipUnderFleetIcon(this, screenPos, 20f))
                    DrawTactical(us, screenPos, screenRadius, 16f, 32f);
            }
            // ShowTacticalCloseup => when you hold down LALT key
            else if (us.ShowTacticalCloseup || viewState > UniverseScreen.UnivScreenState.ShipView)
            {
                if (!us.IsShipUnderFleetIcon(this, screenPos, screenRadius + 3.0f))
                    DrawTactical(us, screenPos, screenRadius, 16f, 32f);
            }
            else if (viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                DrawTactical(us, screenPos, screenRadius, 16f, 32f);

                // display low ammo
                if (OrdinanceMax > 0.0f && Ordinance < 0.5f * OrdinanceMax)
                {
                    Texture2D ammoIcon = ResourceManager.Texture("NewUI/icon_ammo");
                    Color   color    = (Ordinance <= 0.2f * OrdinanceMax) ? Color.Red : Color.Yellow;
                    float size = KeepConstantSize(screenRadius, ammoIcon.Width ) / 3;
                    Vector2 iconPos = screenPos + new Vector2(15) + new Vector2(size);
                    us.DrawTextureSized(ammoIcon, iconPos, 0f, size, size, color);
                }
            }
        }


        public void RenderOverlay(SpriteBatch spriteBatch, Rectangle drawRect, bool showModules)
        {
            //if (!ResourceManager.TryGetHull(shipData.Hull, out ShipData hullData))
              //  return; //try load hull data, cancel if failed
            if (this.GetShipData().HullData == null) return;
            ShipData hullData = shipData.HullData;
            if (hullData.SelectionGraphic.NotEmpty() && !showModules)// draw ship icon plus shields
            {
                Rectangle destinationRectangle = drawRect;
                destinationRectangle.X += 2;

                spriteBatch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic), destinationRectangle, Color.White);
                if (shield_power > 0.0)
                {
                    byte alpha = (byte)(shield_percent * 255.0f);
                    spriteBatch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic + "_shields"), destinationRectangle, new Color(Color.White, alpha));
                }
            }
            if (!showModules && hullData.SelectionGraphic.IsEmpty() || ModuleSlotList.Length == 0)
                return; //ship has no icon, module display is disabled, or there are no modules

            int maxSpan = Math.Max(GridWidth, GridHeight);
            Vector2 halfSpan = new Vector2(GridWidth / 2, GridHeight / 2); //half-span vectors for centering
            Vector2 halfRect = new Vector2(drawRect.Width / 2, drawRect.Height / 2);

            float moduleSize = (drawRect.Width / (maxSpan + 1));
            if (moduleSize < 2.0)
                moduleSize = drawRect.Width / (float)(maxSpan + 1);
            if (moduleSize > 10.0)
                moduleSize = 10f;
            Rectangle shipDrawRect = new Rectangle(
                    drawRect.X + (int)(halfRect.X - (halfSpan.X * moduleSize)),
                    drawRect.Y + (int)(halfRect.Y - (halfSpan.Y * moduleSize)),
                    (int)(GridWidth * moduleSize), (int)(GridHeight * moduleSize));
            foreach (ShipModule m in ModuleSlotList)
            {
                Vector2 modulePos = (m.Position - GridOrigin) / 16f * moduleSize;
                var rect = new Rectangle(shipDrawRect.X + (int)modulePos.X, shipDrawRect.Y + (int)modulePos.Y, (int)moduleSize * m.XSIZE, (int)moduleSize * m.YSIZE);
                spriteBatch.FillRectangle(rect, m.GetHealthStatusColor());
            }
        }

        public void RenderThrusters(ref Matrix view, ref Matrix projection)
        {
            for (int i = 0; i < ThrusterList.Count; ++i)
            {
                Thruster thruster = ThrusterList[i];
                Log.Assert(thruster.technique != null, "Thruster technique not initialized");
                thruster.Draw(ref view, ref projection);
                thruster.Draw(ref view, ref projection);
            }
        }

        public void DrawShieldBubble(UniverseScreen screen)
        {
            var uiNode = ResourceManager.Texture("UI/node");

            screen.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
            foreach (ShipModule slot in ModuleSlotList)
            {
                if (slot.Active && slot.ModuleType == ShipModuleType.Shield && slot.ShieldPower >= 1f)
                {
                    screen.ProjectToScreenCoords(slot.Center, slot.shield_radius * 2.75f, 
                        out Vector2 posOnScreen, out float radiusOnScreen);

                    float shieldRate = .001f + slot.ShieldPower / (slot.shield_power_max +
                                                           (loyalty?.data.ShieldPowerMod * slot.shield_power_max ?? 0));                    
                    screen.DrawTextureSized(uiNode, posOnScreen, 0f, radiusOnScreen, radiusOnScreen,
                        new Color(0f, 1f, 0f, shieldRate * 0.8f));
                }
            }
            screen.ScreenManager.SpriteBatch.End();
        }
    }
}