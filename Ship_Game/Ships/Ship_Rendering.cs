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
            if (ModuleSlotList.Length != 0)
            {
                var gs = GetGridState();
                for (int y = 0; y < Grid.Height; ++y)
                {
                    for (int x = 0; x < Grid.Width; ++x)
                    {
                        Color color = Color.DarkGray;
                        if    (Externals.Get(gs, x, y) != null) color = Color.Blue;
                        else if (GetModuleAt(x, y) != null) color = Color.Yellow;

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

        public void DrawModulesOverlay(GameScreen sc, double camHeight,
                                       bool showDebugSelect, bool showDebugStats)
        {
            SubTexture symbolFighter = ResourceManager.Texture("TacticalIcons/symbol_fighter");
            SubTexture concreteGlass = ResourceManager.Texture("Modules/tile_concreteglass_1x1"); // 1x1 gray ship module background tile, 16x16px in size
            SubTexture lightningBolt = ResourceManager.Texture("UI/lightningBolt");

            float shipDegrees = (float)Math.Round(Rotation.ToDegrees());
            float shipRotation = shipDegrees.ToRadians();

            // this size calculation is quite delicate because of float coordinate imprecision issues
            double moduleSize = sc.ProjectToScreenSize(16.5f);
            double oneUnit = moduleSize/16.5;

            for (int i = 0; i < ModuleSlotList.Length; ++i)
            {
                ShipModule slot = ModuleSlotList[i];

                bool square = slot.XSize == slot.YSize;
                double w = (slot.XSize * moduleSize);
                double h = (square ? w : slot.YSize * moduleSize);

                Vector2d posOnScreen = sc.ProjectToScreenPosition(slot.Position);

                // round all the values to TRY prevent module flickering on screen
                // it helps by a noticeable amount
                //posOnScreen.X = Math.Round(posOnScreen.X);
                //posOnScreen.Y = Math.Round(posOnScreen.Y);

                int slotFacing = 0;
                switch (slot.ModuleRot)
                {
                    case ModuleOrientation.Right: slotFacing += 90; break;
                    case ModuleOrientation.Left:  slotFacing += 270; break;
                    case ModuleOrientation.Rear:  slotFacing += 180; break;
                }

                float slotRotation = (shipDegrees + slotFacing).ToRadians();
                Color healthColor = slot.GetHealthStatusColorWhite();
                if (camHeight > 6000f)
                {
                    // only draw the slot backgrounds
                    sc.DrawTextureSized(concreteGlass, posOnScreen, shipRotation, w, h, healthColor);
                }
                else
                {
                    // these kind of modules are known to be 100% opaque, so we can discard the background
                    bool opaque = slot.ModuleType == ShipModuleType.Armor
                               || slot.ModuleType == ShipModuleType.PowerConduit
                               || slot.ModuleType == ShipModuleType.Hangar;
                    if (!opaque)
                    {
                        sc.DrawTextureSized(concreteGlass, posOnScreen, shipRotation, w, h, healthColor);
                    }

                    if (square)
                    {
                        sc.DrawTextureSized(slot.ModuleTexture, posOnScreen, slotRotation, w, h, healthColor);
                        if (showDebugSelect)
                            sc.DrawCircle(posOnScreen, slot.Radius*oneUnit, Color.Orange, 2f);
                    }
                    else
                    {
                        // @TODO HACK the dimensions are already rotated so that rotating again puts it in the wrong orientation. 
                        // so to fix that i am switching the height and width if the module is facing left or right. 
                        if (slotFacing == 270 || slotFacing == 90)
                        {
                            double oldW = w; w = h; h = oldW; // swap(w, h)
                        }

                        sc.DrawTextureSized(slot.ModuleTexture, posOnScreen, slotRotation, w, h, healthColor);
                        if (showDebugSelect)
                            sc.DrawCapsuleProjected(slot.GetModuleCollisionCapsule(), Color.Orange, 2f);
                    }

                    if (slot.ModuleType == ShipModuleType.PowerConduit)
                    {
                        if (slot.Active && slot.Powered)
                        {
                            SubTexture poweredTex = ResourceManager.Texture(slot.IconTexturePath + "_power");
                            sc.DrawTextureSized(poweredTex, posOnScreen, slotRotation, w, h, Color.White);
                        }
                    }
                    else if (slot.Active && !slot.Powered && slot.PowerDraw > 0.0f)
                    {
                        double smallerSize = Math.Min(w, h);
                        sc.DrawTextureSized(lightningBolt, posOnScreen, slotRotation, smallerSize, smallerSize, Color.White);
                    }

                    if (showDebugStats)
                    {
                        // draw blue marker on all active external modules
                        if (slot.IsExternal && slot.Active)
                        {
                            double smallerSize = Math.Min(w, h);
                            sc.DrawTextureSized(symbolFighter, posOnScreen, slotRotation, smallerSize, smallerSize, new Color(0, 0, 255, 120));
                        }

                        // draw the debug x/y pos
                        sc.DrawString(posOnScreen.ToVec2f(), shipRotation, (float)(600.0 / camHeight), Color.Red,
                                      $"X{slot.Pos.X} Y{slot.Pos.Y}\nF{slotFacing}");
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
            (SubTexture icon, SubTexture secondary) = TacticalIcon();
            us.DrawTextureSized(icon, screenPos, Rotation, size, size, Loyalty.EmpireColor);
            if (secondary != null)
                us.DrawTextureSized(secondary, screenPos, Rotation, size, size, Loyalty.EmpireColor);
        }

        void DrawFlagIcons(UniverseScreen us, Vector2 screenPos, float screenRadius)
        {
            if (ShipData.IsColonyShip)
            {
                float size = ScaleIconSize(screenRadius, 16f, 16f);
                Vector2 offSet = new Vector2(-screenRadius *.75f, -screenRadius * .75f);
                us.DrawTextureSized(ResourceManager.Texture("UI/flagicon"),
                    screenPos +  offSet, 0, size, size, Loyalty.EmpireColor);
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
            us.ProjectToScreenCoords(Position, shipWorldRadius,
                                     out Vector2d screenPos, out double screenRadius);
            Vector2 pos = screenPos.ToVec2f();
            float radius = (float)screenRadius;

            DrawFlagIcons(us, pos, radius);

            if (viewState == UniverseScreen.UnivScreenState.GalaxyView)
            {
                if (!us.IsShipUnderFleetIcon(this, pos, 20f))
                    DrawTactical(us, pos, radius, 16f, 8f);
            }
            // ShowTacticalCloseup => when you hold down LALT key
            else if (us.ShowTacticalCloseup || viewState > UniverseScreen.UnivScreenState.ShipView)
            {
                if (!us.IsShipUnderFleetIcon(this, pos, radius + 3.0f))
                    DrawTactical(us, pos, radius, 16f, 8f);
            }
            else if (viewState <= UniverseScreen.UnivScreenState.ShipView)
            {
                DrawTactical(us, pos, radius, 16f, 8f);
                DrawStatusIcons(us, radius, pos);
            }
        }

        void DrawStatusIcons(UniverseScreen us, float screenRadius, Vector2 screenPos)
        {
            if (!HelperFunctions.DataVisibleToPlayer(Loyalty))
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

        public void RenderOverlay(SpriteBatch batch, Rectangle drawRect, 
                                  bool showModules, bool moduleHealthColor = true)
        {
            bool drawIconOnly = !showModules || ModuleSlotList.Length == 0;
            if (drawIconOnly && ShipData.SelectionGraphic.NotEmpty()) // draw ship icon plus shields
            {
                Rectangle destRect = drawRect;
                destRect.X += 2;
                string icon = "SelectionBox Ships/" + ShipData.SelectionGraphic;
                batch.Draw(ResourceManager.Texture(icon), destRect, Color.White);
                if (ShieldPower > 0.0)
                {
                    batch.Draw(ResourceManager.Texture(icon + "_shields"), destRect,
                               new Color(Color.White, (float)ShieldPercent));
                }
                return;
            }
            
            int maxSpan = Math.Max(Grid.Width, Grid.Height);
            Vector2 gridCenter = new Vector2(Grid.Width, Grid.Height) / 2f;
            Vector2 rectCenter = new Vector2(drawRect.Width, drawRect.Height) / 2f;

            float moduleSize = (drawRect.Width / (maxSpan + 1f)).Clamped(2f, 24f);
            var shipDrawRect = new Rectangle(
                    drawRect.X + (int)(rectCenter.X - (gridCenter.X * moduleSize)),
                    drawRect.Y + (int)(rectCenter.Y - (gridCenter.Y * moduleSize)),
                    (int)(Grid.Width * moduleSize), (int)(Grid.Height * moduleSize));

            for (int i = 0; i < ModuleSlotList.Length; i++)
            {
                ShipModule m = ModuleSlotList[i];
                int x = m.Pos.X;
                int y = m.Pos.Y;
                Vector2 modulePos = new Vector2(x, y) * moduleSize;
                Color healthColor = moduleHealthColor ? m.GetHealthStatusColor() : new Color(40, 40, 40);
                Color moduleColorMultiply = healthColor.AddRgb(moduleHealthColor ? 0.66f : 1);
                var rect = new Rectangle(shipDrawRect.X + (int)modulePos.X,
                                         shipDrawRect.Y + (int)modulePos.Y,
                                         (int)moduleSize * m.XSize,
                                         (int)moduleSize * m.YSize);

                SubTexture tex = m.ModuleTexture;
                m.GetOrientedModuleTexture(ref tex, m.ModuleRot);

                batch.FillRectangle(rect, healthColor);
                batch.Draw(tex, rect, moduleColorMultiply);
            }
        }

        public void RenderThrusters(ref Matrix view, ref Matrix projection)
        {
            for (int i = 0; i < ThrusterList.Length; ++i)
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
            foreach (ShipModule m in GetShields())
            {
                if (m.Active && m.ShieldsAreActive)
                {
                    screen.ProjectToScreenCoords(m.Position, m.ShieldRadius * 2.75f, 
                        out Vector2d posOnScreen, out double radiusOnScreen);

                    float shieldRate = 0.001f + m.ShieldPower / m.ActualShieldPowerMax;
                    screen.DrawTextureSized(uiNode, posOnScreen, 0f, radiusOnScreen, radiusOnScreen, 
                        Shield.GetBubbleColor(shieldRate, m.ShieldBubbleColor));
                }
            }

            screen.ScreenManager.SpriteBatch.End();
        }

        public void DrawWeaponRanges(GameScreen screen, CombatState state)
        {
            float radius;
            if (state == CombatState.GuardMode)
                radius = GuardModeRange;
            else if (state == CombatState.HoldPosition)
                radius = HoldPositionRange;
            else
                radius = GetDesiredCombatRangeForState(state);

            screen.Renderer.DrawCircleDeferred(Position, radius, Colors.CombatOrders());
        }
    }
}