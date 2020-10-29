using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace UnitTests.Universe
{
    class SpatialVisualization : GameScreen
    {
        Array<GameplayObject> AllObjects;
        ISpatial Spat;
        bool MoveShips;
        Vector3 Camera;
        float CamHeight;

        AABoundingBox2D SearchArea;

        float UpdateTime;
        float CollideTime;
        float SearchTime;
        float LinearTime;
        GameplayObject[] Found = Empty<GameplayObject>.Array;

        VisualizerOptions VisOpt = new VisualizerOptions();

        public SpatialVisualization(Array<GameplayObject> allObjects, ISpatial spat, bool moveShips) : base(null)
        {
            AllObjects = allObjects;
            Spat = spat;
            MoveShips = moveShips;
            CamHeight = spat.FullSize * (float)Math.Sqrt(2);

            if (moveShips)
            {
                var rand = new Random();
                foreach (GameplayObject obj in allObjects)
                {
                    obj.Velocity.X = (float)(rand.NextDouble() - 0.5) * 2.0f * 5000.0f;
                    obj.Velocity.Y = (float)(rand.NextDouble() - 0.5) * 2.0f * 5000.0f;
                }
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            CamHeight = CamHeight.Clamped(80f, Spat.FullSize*2f);
            Camera.Z = -Math.Abs(CamHeight);
            var down = new Vector3(Camera.X, Camera.Y, 0f);
            View = Matrix.CreateLookAt(Camera, down, Vector3.Down);
            Projection = Matrix.CreatePerspectiveFieldOfView(0.785f, Viewport.AspectRatio, 10f, 35000f);

            if (MoveShips)
            {
                float universeLo = Spat.WorldSize * -0.5f;
                float universeHi = Spat.WorldSize * +0.5f;
                var simTime = new FixedSimTime(fixedDeltaTime);
                foreach (GameplayObject go in AllObjects)
                {
                    if (!(go is Ship ship))
                        continue;

                    if (ship.Position.X < universeLo || ship.Position.X > universeHi)
                        ship.Position.X = -ship.Position.X;

                    if (ship.Position.Y < universeLo || ship.Position.Y > universeHi)
                        ship.Position.Y = -ship.Position.Y;

                    ship.IntegratePosVelocityVerlet(fixedDeltaTime, Vector2.Zero);
                    ship.UpdateModulePositions(simTime, true);
                }
                var timer1 = new PerfTimer();
                Spat.UpdateAll(AllObjects);
                UpdateTime = timer1.Elapsed;

                var timer2 = new PerfTimer();
                Spat.CollideAll(simTime);
                CollideTime = timer2.Elapsed;
            }

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Spat.DebugVisualize(this, VisOpt);
            DrawRectangleProjected(Vector2.Zero, new Vector2(Spat.WorldSize), 0f, Color.Red);

            foreach (GameplayObject go in AllObjects)
            {
                if (!(go is Ship ship))
                    continue;

                ProjectToScreenCoords(ship.Position, ship.Radius,
                                      out Vector2 screenPos, out float screenRadius);
                if (!HitTest(screenPos))
                    continue;

                bool found = Found.Contains(ship);
                if (found)
                {
                    float size = screenRadius*1.5f*2f;
                    if (size < 1)
                        size = 1;
                    DrawRectangle(screenPos, new Vector2(size), 0, Color.Yellow);
                }

                if (CamHeight <= 7000f)
                {
                    ship.DrawModulesOverlay(this, CamHeight, showDebugSelect: found, showDebugStats: false);
                }
            }

            if (SearchArea.Width > 0f)
            {
                DrawRectProjected(SearchArea, Color.GreenYellow, 2);
            }

            var cursor = new Vector2(20, 20);
            DrawText(ref cursor, "Press ESC to quit");
            DrawText(ref cursor, $"Camera: {Camera}");
            DrawText(ref cursor, $"UpdateTime:  {(UpdateTime*1000).String(4)}ms");
            DrawText(ref cursor, $"CollideTime: {(CollideTime*1000).String(4)}ms");
            DrawText(ref cursor, $"FindNearby: {Found.Length}");
            DrawText(ref cursor, $"SearchArea: {SearchArea.Width}x{SearchArea.Height}");
            DrawText(ref cursor, $"SearchTime:   {(SearchTime*1000).String(4)}ms");
            DrawText(ref cursor, $"LinearTime:   {(LinearTime*1000).String(4)}ms");

            base.Draw(batch, elapsed);
        }

        void DrawText(ref Vector2 cursor, string text)
        {
            DrawString(cursor, Color.White, text, Fonts.Arial11Bold);
            cursor.Y += 20;
        }

        float MoveStep(float multiplier) => multiplier * Camera.Z * -0.1f; 

        public override bool HandleInput(InputState input)
        {
            if (input.ScrollIn)  { CamHeight -= MoveStep(2.5f); return true; }
            if (input.ScrollOut) { CamHeight += MoveStep(2.5f); return true; }

            if (input.LeftMouseHeldDown)
            {
                Vector2 delta = input.CursorVelocity;
                Camera.X += MoveStep(0.01f) * delta.X;
                Camera.Y += MoveStep(0.01f) * delta.Y;
            }

            if (input.RightMouseHeldDown)
            {
                SearchArea = AABoundingBox2D.FromIrregularPoints(input.StartRightHold, input.EndRightHold);

                var opt = new SearchOptions(SearchArea)
                {
                    MaxResults = 1000
                };

                var timer2 = new PerfTimer();
                {
                    Spat.FindLinear(opt);
                }
                LinearTime = timer2.Elapsed;

                var timer = new PerfTimer();
                {
                    Found = Spat.FindNearby(opt);
                }
                SearchTime = timer.Elapsed;
            }

            return base.HandleInput(input);
        }
    }
}