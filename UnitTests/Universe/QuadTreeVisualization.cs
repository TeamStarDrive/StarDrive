using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game;
using Ship_Game.Ships;

namespace UnitTests.Universe
{
    class QuadTreeVisualization : GameScreen
    {
        TestQuadTree Test;
        Quadtree Tree;
        Vector3 Camera;
        float CamHeight;

        Vector2 SearchStart;
        float SearchRadius;
        float SearchTime;
        float LinearTime;
        GameplayObject[] Found = Empty<GameplayObject>.Array;


        public QuadTreeVisualization(TestQuadTree test, Quadtree tree) : base(null)
        {
            Tree = tree;
            Test = test;
            CamHeight = tree.FullSize * (float)Math.Sqrt(2);
        }

        public override void Update(float fixedDeltaTime)
        {
            CamHeight = CamHeight.Clamped(80f, Tree.FullSize*2f);
            Camera.Z = -Math.Abs(CamHeight);
            var down = new Vector3(Camera.X, Camera.Y, 0f);
            View = Matrix.CreateLookAt(Camera, down, Vector3.Down);
            Projection = Matrix.CreatePerspectiveFieldOfView(0.785f, Viewport.AspectRatio, 10f, 35000f);

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Tree.DebugVisualize(this);
            DrawRectangleProjected(Vector2.Zero, new Vector2(Test.UniverseSize), 0f, Color.Red);

            foreach (Ship ship in Test.AllShips)
            {
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

            if (SearchStart != Vector2.Zero && SearchRadius != 0f)
            {
                DrawCircleProjected(SearchStart, SearchRadius, Color.GreenYellow, 2);
            }

            DrawString(new Vector2(20, 20), Color.White, "Press ESC to quit", Fonts.Arial11Bold);
            DrawString(new Vector2(20, 40), Color.White, $"Camera: {Camera}", Fonts.Arial11Bold);
            DrawString(new Vector2(20, 60), Color.White, $"FindNearby: {Found.Length}", Fonts.Arial11Bold);
            DrawString(new Vector2(20, 80), Color.White, $"SearchRadius: {SearchRadius}", Fonts.Arial11Bold);
            DrawString(new Vector2(20,100), Color.White, $"SearchTime:   {(SearchTime*1000).String(4)}ms  Linear:{Tree.WasLinearSearch}", Fonts.Arial11Bold);
            DrawString(new Vector2(20,120), Color.White, $"LinearTime:   {(LinearTime*1000).String(4)}ms", Fonts.Arial11Bold);

            base.Draw(batch, elapsed);
        }

        float MoveStep(float multiplier) => multiplier * Camera.Z * -0.1f; 

        public override bool HandleInput(InputState input)
        {
            if (input.IsKeyDown(Keys.W)) { Camera.Y -= MoveStep(0.1f); return true; }
            if (input.IsKeyDown(Keys.S)) { Camera.Y += MoveStep(0.1f); return true; }
            if (input.IsKeyDown(Keys.A)) { Camera.X -= MoveStep(0.1f); return true; }
            if (input.IsKeyDown(Keys.D)) { Camera.X += MoveStep(0.1f); return true; }
            if (input.ScrollIn)          { CamHeight -= MoveStep(2.5f); return true; }
            if (input.ScrollOut)         { CamHeight += MoveStep(2.5f); return true; }

            if (input.LeftMouseHeldDown)
            {
                var timer2 = new PerfTimer();
                {
                    QuadtreePerfTests.FindLinearOpt(Test.AllShips, null, SearchStart, SearchRadius);
                }
                LinearTime = timer2.Elapsed;

                var timer = new PerfTimer();
                {
                    SearchStart = UnprojectToWorldPosition(input.StartLeftHold);
                    SearchRadius = SearchStart.Distance(UnprojectToWorldPosition(input.EndLeftHold));
                    Found = Tree.FindNearby(SearchStart, SearchRadius);
                }
                SearchTime = timer.Elapsed;
            }

            return base.HandleInput(input);
        }
    }
}