using Ship_Game;
using System;
using SDGraphics;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Spatial;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace UnitTests.Universe;

/// <summary>
/// Common base class for spatial tree visualizers
/// </summary>
internal abstract class CommonVisualization : GameScreen
{
    public Vector3 Camera;
    public float CamHeight;
    public AABoundingBox2D SearchArea;

    public SpatialObjectBase[] AllObjects;
    public SpatialObjectBase[] Found = Empty<SpatialObjectBase>.Array;
    public VisualizerOptions VisOpt = new();

    protected CommonVisualization(float fullSize)
        : base(null, toPause: null)
    {
        CamHeight = fullSize * (float)Math.Sqrt(2);
    }

    protected abstract float FullSize { get; }
    protected abstract float WorldSize { get; }

    protected abstract void UpdateSim(float fixedDeltaTime);
    protected abstract void DrawTree();
    protected abstract void DrawStats();
    protected abstract void Search(in AABoundingBox2D searchArea);

    public override void Update(float fixedDeltaTime)
    {
        CamHeight = CamHeight.Clamped(80f, FullSize*2f);
        Camera.Z = -Math.Abs(CamHeight);
        var down = new Vector3(Camera.X, Camera.Y, 0f);
        SetPerspectiveProjection();
        SetViewMatrix(Matrix.CreateLookAt(Camera, down, Vector3.Down));

        UpdateSim(fixedDeltaTime);

        base.Update(fixedDeltaTime);
    }

    protected virtual void DrawObjects()
    {
        AABoundingBox2D visibleWorldRect = VisibleWorldRect;
        foreach (SpatialObjectBase go in AllObjects)
        {
            if (visibleWorldRect.Overlaps(go.Position.X, go.Position.Y, go.Radius))
            {
                if (CamHeight <= 10_000f)
                {
                    if (go is Ship s)
                    {
                        bool found = Found.Contains(go);
                        s.DrawModulesOverlay(this, CamHeight, showDebugSelect: found, showDebugStats: false);
                    }
                    else if (go is Projectile p)
                    {
                        Vector2 screenPos = ProjectToScreenPosition(go.Position).ToVec2f();
                        DrawLine(screenPos, screenPos+p.Direction*10, Color.Red);
                    }
                }
                if (go is SolarSystem sys)
                {
                    DrawCircleProjected(sys.Position, sys.Radius, Color.AliceBlue);
                }
                else if (go is Planet p)
                {
                    DrawCircleProjected(p.Position, p.Radius, Color.Green);
                }
            }
        }
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        DrawTree();
        DrawRectangleProjected(Vector2.Zero, new Vector2(WorldSize), 0f, Color.Red);
        DrawObjects();
        DrawStats();
        base.Draw(batch, elapsed);
    }

    protected void DrawText(ref Vector2 cursor, string text)
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
            Search(SearchArea);
        }

        return base.HandleInput(input);
    }
}