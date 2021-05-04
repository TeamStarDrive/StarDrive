using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Debug;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public partial class Ship
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

        void DrawSparseModuleGrid(GameScreen us)
        {
            if (SparseModuleGrid.Length != 0)
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


        public void DrawModulesOverlay(GameScreen sc, float camHeight,
                                       bool showDebugSelect, bool showDebugStats)
        {
            SubTexture symbolFighter = ResourceManager.Texture("TacticalIcons/symbol_fighter");
            SubTexture concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1"); // 1x1 gray ship module background tile, 16x16px in size
            SubTexture lightningBolt = ResourceManager.Texture("UI/lightningBolt");

            float shipDegrees = (float)Math.Round(Rotation.ToDegrees());
            float shipRotation = shipDegrees.ToRadians();

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot = ModuleSlotList[i];

                float moduleWidth  = slot.XSIZE * 16.5f; // using 16.5f instead of 16 to reduce pixel error flickering
                float moduleHeight = slot.YSIZE * 16.5f;

                float w = sc.ProjectToScreenSize(moduleWidth);
                float h = sc.ProjectToScreenSize(moduleHeight);
                Vector2 posOnScreen = sc.ProjectToScreenPosition(slot.Center);

                // round all the values to TRY prevent module flickering on screen
                // it helps by a noticeable amount
                posOnScreen.X = (float)Math.Round(posOnScreen.X);
                posOnScreen.Y = (float)Math.Round(posOnScreen.Y);
                if (w.AlmostEqual(h, 0.001f)) w = h;

                float slotFacing = 0;
                switch (slot.Orientation)
                {
                    case ModuleOrientation.Right: slotFacing += 90; break;
                    case ModuleOrientation.Left:  slotFacing += 270; break;
                    case ModuleOrientation.Rear:  slotFacing += 180; break;
                }

                float slotRotation = (shipDegrees + slotFacing).ToRadians();
                sc.DrawTextureSized(concreteGlass, posOnScreen, shipRotation, w, h, Color.White);
                if (camHeight > 6000.0f) // long distance view, draw the modules as colored icons
                {
                    sc.DrawTextureSized(symbolFighter, posOnScreen, shipRotation, w, h, slot.GetHealthStatusColor());
                }
                else
                {
                    Color healthColor = slot.GetHealthStatusColorWhite();
                    if (slot.XSIZE == slot.YSIZE)
                    {
                        sc.DrawTextureSized(slot.ModuleTexture, posOnScreen, slotRotation, w, h, healthColor);
                        if (showDebugSelect)
                            sc.DrawCircleProjected(slot.Center, slot.Radius, Color.Orange, 2f);
                    }
                    else
                    {
                        // @TODO HACK the dimensions are already rotated so that rotating again puts it in the wrong orientation. 
                        // so to fix that i am switching the height and width if the module is facing left or right. 
                        if (slotFacing.AlmostEqual(270f) || slotFacing.AlmostEqual(90f))
                        {
                            float oldW = w; w = h; h = oldW; // swap(w, h)
                        }

                        sc.DrawTextureSized(slot.ModuleTexture, posOnScreen, slotRotation, w, h, healthColor);
                        if (showDebugSelect)
                            sc.DrawCapsuleProjected(slot.GetModuleCollisionCapsule(), Color.Orange, 2f);
                    }

                    if (slot.ModuleType == ShipModuleType.PowerConduit)
                    {
                        if (slot.Powered)
                        {
                            SubTexture poweredTex = ResourceManager.Texture(slot.IconTexturePath + "_power");
                            sc.DrawTextureSized(poweredTex, posOnScreen, slotRotation, w, h, Color.White);
                        }
                    }
                    else if (slot.Active && !slot.Powered && slot.PowerDraw > 0.0f)
                    {
                        float smallerSize = Math.Min(w, h);
                        sc.DrawTextureSized(lightningBolt, posOnScreen, slotRotation, smallerSize, smallerSize, Color.White);
                    }

                    if (showDebugStats)
                    {
                        // draw blue marker on all active external modules
                        if (slot.isExternal && slot.Active)
                        {
                            float smallerSize = Math.Min(w, h);
                            sc.DrawTextureSized(symbolFighter, posOnScreen, slotRotation, smallerSize, smallerSize, new Color(0, 0, 255, 120));
                        }

                        // draw the debug x/y pos
                        ModulePosToGridPoint(slot.Position, out int x, out int y);
                        sc.DrawString(posOnScreen, shipRotation, 600f / camHeight, Color.Red, $"X{x} Y{y}\nF{slotFacing}");
                    }
                }
            }

            if (false && showDebugSelect)
                DrawSparseModuleGrid(sc);
        }


        void DrawTactical(UniverseScreen us, Vector2 screenPos, float screenRadius, float minSize, float maxSize = 0f)
        {
            // try to scale the icon so its size remains consistent when zooming in/out
            float size = ScaleIconSize(screenRadius, minSize, maxSize);
            us.DrawTextureSized(GetTacticalIcon(out SubTexture secondary, out _), screenPos, Rotation, size, size, loyalty.EmpireColor);
            if (secondary != null)
                us.DrawTextureSized(secondary, screenPos, Rotation, size, size, loyalty.EmpireColor);
        }

        void DrawFlagIcons(UniverseScreen us, Vector2 screenPos, float screenRadius)
        {            
            if (isColonyShip)
            {
                float size = ScaleIconSize(screenRadius, 16f, 16f);
                Vector2 offSet = new Vector2(-screenRadius *.75f, -screenRadius * .75f);
                us.DrawTextureSized(ResourceManager.Texture("UI/flagicon"),
                    screenPos +  offSet, 0, size, size, loyalty.EmpireColor);
            }
        }

        float ScaleIconSize(float screenRadius, float minSize = 0, float maxSize = 0)
        {            
            float size = screenRadius * 2 ;
            if (size < minSize && minSize != 0)
                size = minSize;
            else if (maxSize > 0f && size > maxSize)
                size = maxSize ;
            return size + GlobalStats.IconSize;
        }

        public void DrawTacticalIcon(UniverseScreen us, UniverseScreen.UnivScreenState viewState)
        {
            float shipWorldRadius = Radius;
            us.ProjectToScreenCoords(Position, shipWorldRadius, out Vector2 screenPos, out float screenRadius);

            DrawFlagIcons(us, screenPos, screenRadius);

            if (viewState == UniverseScreen.UnivScreenState.GalaxyView)
            {
                if (!us.IsShipUnderFleetIcon(this, screenPos, 20f))
                    DrawTactical(us, screenPos, screenRadius, 16f, 8f);
            }
            // ShowTacticalCloseup => when you hold down LALT key
            else if (us.ShowTacticalCloseup || viewState > UniverseScreen.UnivScreenState.ShipView)
            {
                if (!us.IsShipUnderFleetIcon(this, screenPos, screenRadius + 3.0f))
                    DrawTactical(us, screenPos, screenRadius, 16f, 8f);
            }
            else if (viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                DrawTactical(us, screenPos, screenRadius, 16f, 8f);
                DrawStatusIcons(us, screenRadius, screenPos);
            }
        }

        void DrawStatusIcons(UniverseScreen us, float screenRadius, Vector2 screenPos)
        {
            if (!HelperFunctions.DataVisibleToPlayer(loyalty))
                return;

            Vector2 offSet = new Vector2(screenRadius * .75f, screenRadius * .75f);

            // display low ammo
            if (OrdnancePercent < 0.5f)
            {
                float criticalThreshold = InCombat ? ShipResupply.OrdnanceThresholdCombat : ShipResupply.OrdnanceThresholdNonCombat;
                Color color             = OrdnancePercent <= criticalThreshold ? Color.Red : Color.Yellow;
                DrawSingleStatusIcon(us, screenRadius, screenPos, ref offSet, "NewUI/icon_ammo", color);
            }
            // FB: display resupply icons
            switch (AI.State)
            {
                case Ship_Game.AI.AIState.Resupply:
                case Ship_Game.AI.AIState.ResupplyEscort:
                    DrawSingleStatusIcon(us, screenRadius, screenPos, ref offSet, "NewUI/icon_resupply", Color.White);
                    break;
                case Ship_Game.AI.AIState.ReturnToHangar:
                    DrawSingleStatusIcon(us, screenRadius, screenPos, ref offSet, "UI/icon_hangar", Color.Yellow);
                    break;
            }
        }

        void DrawSingleStatusIcon(UniverseScreen us, float screenRadius, Vector2 screenPos, ref Vector2 offSet, string texture, Color color)
        {
            SubTexture statusIcon = ResourceManager.Texture(texture);
            float size = ScaleIconSize(screenRadius, 16f, 16f);
            us.DrawTextureSized(statusIcon, screenPos + offSet, 0f, size, size, color);
            offSet.X += size * 1.2f;
        }

        public void RenderOverlay(SpriteBatch batch, Rectangle drawRect, bool showModules, bool moduleHealthColor = true)
        {
            ShipData hullData = shipData.BaseHull;
            bool drawIcon = !showModules || ModuleSlotList.Length == 0;
            if (drawIcon && hullData.SelectionGraphic.NotEmpty())// draw ship icon plus shields
            {
                Rectangle destRect = drawRect;
                destRect.X += 2;
                batch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic), destRect, Color.White);
                if (shield_power > 0.0)
                {
                    byte alpha = (byte)(shield_percent * 255.0f);
                    batch.Draw(ResourceManager.Texture("SelectionBox Ships/" + hullData.SelectionGraphic + "_shields"), destRect, new Color(Color.White, alpha));
                }
                return;
            }
            
            int maxSpan = Math.Max(GridWidth, GridHeight);
            Vector2 gridCenter = new Vector2(GridWidth, GridHeight) / 2f;
            Vector2 rectCenter = new Vector2(drawRect.Width, drawRect.Height) / 2f;

            float moduleSize = (drawRect.Width / (maxSpan + 1f)).Clamped(2f, 24f);
            var shipDrawRect = new Rectangle(
                    drawRect.X + (int)(rectCenter.X - (gridCenter.X * moduleSize)),
                    drawRect.Y + (int)(rectCenter.Y - (gridCenter.Y * moduleSize)),
                    (int)(GridWidth * moduleSize), (int)(GridHeight * moduleSize));

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule m              = ModuleSlotList[i];
                Vector2 modulePos         = (m.Position - GridOrigin) / 16f * moduleSize;
                Color healthColor         = moduleHealthColor ? m.GetHealthStatusColor() : new Color(40, 40, 40);
                Color moduleColorMultiply = healthColor.AddRgb(moduleHealthColor ? 0.66f : 1);
                var rect = new Rectangle(shipDrawRect.X + (int)modulePos.X, shipDrawRect.Y + (int)modulePos.Y,
                                   (int)moduleSize * m.XSIZE,(int)moduleSize * m.YSIZE);

                SubTexture tex = m.ModuleTexture;
                HelperFunctions.GetOrientedModuleTexture(m, ref tex, m.Orientation);

                batch.FillRectangle(rect, healthColor);
                batch.Draw(tex, rect, moduleColorMultiply);
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
            for (int i = 0; i < Shields.Length; ++i)
            {
                ShipModule m = Shields[i];
                if (m.Active && m.ShieldsAreActive)
                {
                    screen.ProjectToScreenCoords(m.Center, m.shield_radius * 2.75f, 
                        out Vector2 posOnScreen, out float radiusOnScreen);

                    float shieldRate = 0.001f + m.ShieldPower / m.ActualShieldPowerMax;                    
                    screen.DrawTextureSized(uiNode, posOnScreen, 0f, radiusOnScreen, radiusOnScreen, 
                        Shield.GetBubbleColor(shieldRate, m.ShieldBubbleColor));
                }
            }

            screen.ScreenManager.SpriteBatch.End();
        }

        public void DrawWeaponRanges(CombatState state)
        {
            // create the variables to add to the draw so that they are not created during draw. 
            Vector2 center = Center;
            float radius = GetDesiredCombatRangeForState(state);
            Empire.Universe.RenderQueue.DrawCircleDeferred(center, radius, Colors.CombatOrders());
        }
    }
}