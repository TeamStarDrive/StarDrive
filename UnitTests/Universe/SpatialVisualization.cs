using System;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Universe;

/// <summary>
/// Debug & Test for Spatial.Qtree and Spatial.NativeSpatial
/// </summary>
class SpatialVisualization : CommonVisualization
{
    readonly ISpatial Spat;

    public bool MoveShips;
    public int NumShips;
    public int NumProjectiles;

    float UpdateTime;
    float CollideTime;
    float SearchTime;
    float LinearTime;
    int Collisions;

    protected override float FullSize => Spat.FullSize;
    protected override float WorldSize => Spat.WorldSize;

    public SpatialVisualization(SpatialObjectBase[] allObjects, ISpatial spat, bool moveShips)
        : base(spat.FullSize)
    {
        AllObjects = allObjects;
        Spat = spat;
        MoveShips = moveShips;

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

    protected override void Search(in AABoundingBox2D searchArea)
    {
        var opt = new SearchOptions(SearchArea) { MaxResults = 1000, DebugId = 1, };
        var t1 = new PerfTimer();
        Spat.FindLinear(in opt);
        LinearTime = t1.Elapsed;
        var t2 = new PerfTimer();
        Found = Spat.FindNearby(in opt);
        SearchTime = t2.Elapsed;
    }

    protected override void UpdateSim(float fixedDeltaTime)
    {
        if (MoveShips)
        {
            float universeLo = Spat.WorldSize * -0.5f;
            float universeHi = Spat.WorldSize * +0.5f;
            var simTime = new FixedSimTime(fixedDeltaTime);
            foreach (PhysicsObject go in AllObjects)
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

        NumShips = 0;
        NumProjectiles = 0;
        foreach (GameObject go in AllObjects)
        {
            if (go is Ship) ++NumShips;
            else if (go is Projectile p && p.Active) ++NumProjectiles;
        }
    }

    protected override void DrawTree()
    {
        Spat.DebugVisualize(this, VisOpt);
    }

    protected override void DrawObjects()
    {
        AABoundingBox2D visibleWorldRect = VisibleWorldRect;
        foreach (GameObject go in AllObjects)
        {
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
    }

    protected override void DrawStats()
    {
        var cursor = new Vector2(20, 20);
        DrawText(ref cursor, "Press ESC to quit");
        DrawText(ref cursor, $"Camera: {Camera}");
        DrawText(ref cursor, $"Ships: {NumShips} Projectiles: {NumProjectiles}");
        DrawText(ref cursor, $"UpdateTime:  {(UpdateTime*1000).String(4)}ms");
        DrawText(ref cursor, $"CollideTime: {(CollideTime*1000).String(4)}ms {Collisions}");
        DrawText(ref cursor, $"FindNearby: {Found.Length}");
        DrawText(ref cursor, $"SearchArea: {SearchArea.Width}x{SearchArea.Height}");
        DrawText(ref cursor, $"SearchTime:   {(SearchTime*1000).String(4)}ms");
        DrawText(ref cursor, $"LinearTime:   {(LinearTime*1000).String(4)}ms");
    }
}