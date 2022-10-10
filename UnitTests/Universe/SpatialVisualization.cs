using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;

namespace UnitTests.Universe
{
    class SpatialVisualization : GameScreen
    {
        SpatialObjectBase[] AllObjects;
        ISpatial Spat;
        public bool MoveShips;
        Vector3 Camera;
        float CamHeight;

        AABoundingBox2D SearchArea;

        float UpdateTime;
        float CollideTime;
        float SearchTime;
        float LinearTime;
        int Collisions;
        SpatialObjectBase[] Found = Empty<SpatialObjectBase>.Array;

        readonly VisualizerOptions VisOpt = new VisualizerOptions()
        {
        };

        public SpatialVisualization(GameObject[] allObjects, ISpatial spat, bool moveShips)
            : base(null, toPause: null)
        {
            AllObjects = allObjects;
            Spat = spat;
            MoveShips = moveShips;
            CamHeight = spat.FullSize * (float)Math.Sqrt(2);

            if (moveShips)
            {
                var rand = new Random();
                foreach (GameObject obj in allObjects)
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
            SetPerspectiveProjection();
            SetViewMatrix(Matrix.CreateLookAt(Camera, down, Vector3.Down));

            if (MoveShips)
            {
                float universeLo = Spat.WorldSize * -0.5f;
                float universeHi = Spat.WorldSize * +0.5f;
                var simTime = new FixedSimTime(fixedDeltaTime);
                foreach (GameObject go in AllObjects)
                {
                    if (go.Position.X < universeLo || go.Position.X > universeHi)
                        go.Velocity.X = -go.Velocity.X;

                    if (go.Position.Y < universeLo || go.Position.Y > universeHi)
                        go.Velocity.Y = -go.Velocity.Y;

                    if (go is Ship ship)
                    {
                        ship.IntegratePosVelocityVerlet(fixedDeltaTime, Vector2.Zero);
                        ship.UpdateModulePositions(simTime, true);
                    }
                    else if (go is Projectile p && p.Active)
                    {
                        p.TestUpdatePhysics(simTime);
                    }
                }

                var timer1 = new PerfTimer();
                Spat.UpdateAll(AllObjects);
                UpdateTime = timer1.Elapsed;

                var timer2 = new PerfTimer();
                Collisions += Spat.CollideAll(simTime, showCollisions: true);
                CollideTime = timer2.Elapsed;
            }

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Spat.DebugVisualize(this, VisOpt);
            DrawRectangleProjected(Vector2.Zero, new Vector2(Spat.WorldSize), 0f, Color.Red);

            int numShips = 0;
            int numProjectiles = 0;
            AABoundingBox2D visibleWorldRect = VisibleWorldRect;

            foreach (GameObject go in AllObjects)
            {
                if (go is Ship) ++numShips;
                else if (go is Projectile p && p.Active) ++numProjectiles;

                if (CamHeight <= 10_000f && visibleWorldRect.Overlaps(go.Position.X, go.Position.Y, go.Radius))
                {
                    if (go is Ship s)
                    {
                        bool found = Found.Contains(go);
                        s.DrawModulesOverlay(this, CamHeight, showDebugSelect: found, showDebugStats: false);
                    }
                    else if (go is Projectile)
                    {
                        Vector2 screenPos = ProjectToScreenPosition(go.Position).ToVec2f();
                        DrawLine(screenPos, screenPos+go.Direction*10, Color.Red);
                    }
                }
            }

            var cursor = new Vector2(20, 20);
            DrawText(ref cursor, "Press ESC to quit");
            DrawText(ref cursor, $"Camera: {Camera}");
            DrawText(ref cursor, $"Ships: {numShips} Projectiles: {numProjectiles}");
            DrawText(ref cursor, $"UpdateTime:  {(UpdateTime*1000).String(4)}ms");
            DrawText(ref cursor, $"CollideTime: {(CollideTime*1000).String(4)}ms {Collisions}");
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
                Vector2 a = UnprojectToWorldPosition(input.StartRightHold);
                Vector2 b = UnprojectToWorldPosition(input.EndRightHold);
                SearchArea = AABoundingBox2D.FromIrregularPoints(a, b);

                var opt = new SearchOptions(SearchArea)
                {
                    MaxResults = 1000,
                    DebugId = 1,
                };

                var timer2 = new PerfTimer();
                {
                    Spat.FindLinear(ref opt);
                }
                LinearTime = timer2.Elapsed;

                var timer = new PerfTimer();
                {
                    Found = Spat.FindNearby(ref opt);
                }
                SearchTime = timer.Elapsed;
            }

            return base.HandleInput(input);
        }
    }
}