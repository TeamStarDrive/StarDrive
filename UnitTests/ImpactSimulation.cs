using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game;
using Ship_Game.Gameplay;
using Keys = SDGraphics.Input.Keys;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace UnitTests;

class SimObject : GameObject
{
    public string Name;
    public float VelocityMax; // terminal velocity
    public Color Color;
    public bool CanMove = true;

    public SimObject(string name, Vector2 p, Vector2 v, Color c, float r)
        : base(0, GameObjectType.Ship)
    {
        Name = name;
        Position = p;
        Velocity = v;
        Color = c;
        Radius = r;
    }

    public void Update(float dt)
    {
        if (CanMove)
        {
            Vector2 newAcc = Acceleration; // constant acceleration
            bool isZeroAcc = newAcc.X == 0f && newAcc.Y == 0f;
            UpdateVelocityAndPosition(dt, newAcc, isZeroAcc);

            if (VelocityMax > 0f && Velocity.Length() > VelocityMax)
            {
                Velocity = Velocity.Normalized() * VelocityMax;
            }
        }
    }

    public void Draw(SpriteBatch batch, GameScreen screen)
    {
        Vector2 posOnScreen = screen.ProjectToScreenPosition(Position).ToVec2f();

        float radiusOnScreen = (float)screen.ProjectToScreenSize(Radius);
        batch.DrawCircle(posOnScreen, radiusOnScreen, Color, 1f);
        if (CanMove)
        {
            Vector2 velEndOnScreen = screen.ProjectToScreenPosition(Position + Velocity).ToVec2f();
            batch.DrawLine(posOnScreen, velEndOnScreen, Color, 1f);
        }
        batch.DrawString(Fonts.Arial11Bold, Name, posOnScreen + new Vector2(5,5), Color);
    }
}

public class SimParameters
{
    public float Step = 1.0f / 60.0f;
    public float SimSpeed = 1f;
    public float Duration = 10f;
    public float MaxCameraHeight = 100_000f;
    public bool EnablePauses = true;

    public Vector2 Prediction; // prediction where it will impact
    public Vector2 ProjVelStart; // initial projectile velocity

    public Vector2 ProjAcc;
    public float ProjVelMax;

    public float UsRadius = 32;
    public float ThemRadius = 32;
    public float ProjectileRadius = 8f;

    // alternative approach
    public bool SimBombDrop;

    public AABoundingBox2D? Bounds = null;
}

public struct SimResult
{
    public Vector2 Intersect;
    public float Time;

    public override string ToString()
    {
        return $"SimResult:  {Intersect}  Time:{Time.String(3)}s";
    }
}

internal enum SimState
{
    Starting, Running, Exiting
}

internal class ImpactSimulation : GameScreen
{
    readonly Array<SimObject> Objects = new();
    readonly SimObject Us;
    readonly SimObject Tgt;
    readonly SimObject Proj;

    SimState State = SimState.Starting;
    float StartCounter = 1f;
    float ExitCounter  = 1f;

    public SimResult Result = new();

    readonly TestGameDummy Game;
    readonly SimParameters Sim;

    public Vector3 Camera = new(0, 0, 1024);

    bool IsPaused;
    float Time;
    float PrevDistance;

    float ReleaseTime = 0f;
    float TimeToTarget = 0f;

    public ImpactSimulation(TestGameDummy game, TestImpactPredictor.Scenario s, SimParameters sim)
        : base(null, toPause: null)
    {
        Game = game;
        Sim = sim;
        Us = new SimObject("Us", s.Us, s.UsVel, Color.Green, Sim.UsRadius);
        Tgt = new SimObject("Target", s.Tgt, s.TgtVel, Color.Red, Sim.ThemRadius);
        Proj = new SimObject("Projectile", s.Us, Sim.ProjVelStart, Color.Orange, Sim.ProjectileRadius)
        {
            Acceleration = sim.ProjAcc,
            VelocityMax = sim.ProjVelMax
        };

        Objects.AddRange(new []{ Us, Tgt, Proj });
        IsPaused = Sim.EnablePauses;
        Time = 0f;
        PrevDistance = float.MaxValue;

        Proj.CanMove = !sim.SimBombDrop;

        UpdateSimScaleAndBounds();
    }

    public override bool HandleInput(InputState input)
    {
        if (Sim.EnablePauses && Game.Input.KeyPressed(Keys.Space))
        {
            IsPaused = !IsPaused;
            return true;
        }
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        UpdateSimScaleAndBounds();

        switch (State)
        {
            case SimState.Starting: WaitingToStart(fixedDeltaTime); break;
            case SimState.Running:  UpdateSimulation();   break;
            case SimState.Exiting:  WaitingToExit(fixedDeltaTime);  break;
        }
    }

    void WaitingToExit(float deltaTime)
    {
        if (Sim.EnablePauses && IsPaused)
            return;

        ExitCounter -= deltaTime;
        if (!Sim.EnablePauses || ExitCounter <= 0f)
            ExitScreen();
    }

    void WaitingToStart(float deltaTime)
    {
        if (!IsPaused)
        {
            State = SimState.Running;
        }
        else if (State != SimState.Running)
        {
            StartCounter -= deltaTime;
            if (StartCounter <= 0f)
            {
                State = SimState.Running;
                IsPaused = false;
            }
        }
    }

    void UpdateSimulation()
    {
        if (Sim.SimSpeed > 0f)
        {
            float delay = Sim.Step / Sim.SimSpeed;
            Thread.Sleep((int)(delay * 1000));
        }

        if (IsPaused)
            return; // it's paused

        Time += Sim.Step;

        foreach (SimObject o in Objects)
            o.Update(Sim.Step);

        if (Sim.SimBombDrop && !Proj.CanMove)
        {
            Proj.Position = Us.Position; // keep it attached to Us

            (Vector2 prediction, float timeToTarget) = ImpactPredictor.PredictBombingPos(
                Proj.Position,
                Proj.Velocity,
                Proj.Acceleration,
                Proj.VelocityMax,
                groundY: 0f
            );

            Sim.Prediction = prediction;
            if (Sim.Prediction.Distance(Tgt.Position) < Tgt.Radius)
            {
                Proj.CanMove = true; // release!
                ReleaseTime = Time;
                TimeToTarget = timeToTarget;
            }
        }

        float distance = Proj.Position.Distance(Tgt.Position);
        if (distance <= (Proj.Radius + Tgt.Radius))
        {
            State = SimState.Exiting;

            // final simulation correction towards Target
            float speed = Proj.Velocity.Length();
            float timeAdjust = distance / speed;
            //timeAdjust *= 1.09f; // additional heuristic precision adjustment

            Result.Intersect = Proj.Position + Proj.Velocity * timeAdjust;
            Result.Time = Time + timeAdjust;
        }
        else
        {
            if (distance > PrevDistance)
                Proj.Name = "Projectile MISS";

            PrevDistance = distance;

            if (Time >= Sim.Duration)
            {
                State = SimState.Exiting;
            }
        }
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        // draw a background grid to help with object reference
        const int gridSize = 64;
        const float cellSize = 128;
        float fullSize = gridSize*cellSize;
        Color color = new(Color.Green, 20);

        for (int x = -gridSize/2; x < gridSize/2; ++x)
        {
            DrawLineProjected(new(x*cellSize, -fullSize/2), new(x*cellSize,fullSize/2), color);
        }
        for (int y = -gridSize/2; y < gridSize/2; ++y)
        {
            DrawLineProjected(new(-fullSize/2, y*cellSize), new(fullSize/2, y*cellSize), color);
        }

        foreach (SimObject o in Objects)
            o.Draw(batch, this);

        string status = (IsPaused ? "PAUSED" : "RUNNING");
        DrawText(5,  5, $"Simulation {status} {Time.String(1)}s / {Sim.Duration.String(2)}s");
        DrawText(5, 25, $"  Camera {(int)Camera.X} {(int)Camera.Y} {(int)Camera.Z}");
        for (int i = 0; i < Objects.Count; ++i)
        {
            SimObject o = Objects[i];
            DrawText(5, 45 + i*20, $"  {o.Name,-16}  {o.Velocity.Length().String(),-3}m/s  {o.Position.ToString(2)}");
        }
        DrawText(5, 105, $"  {Result}");
        if (ReleaseTime > 0f)
        {
            DrawText(5, 125, $"  ReleaseTime {ReleaseTime.String(2)}");
        }
        if (TimeToTarget > 0f)
        {
            DrawText(5, 145, $"  TimeToTarget {TimeToTarget.String(2)}s");
        }

        DrawCircleProjected(Sim.Prediction, Tgt.Radius*0.2f, Color.GreenYellow, 1f);
        DrawStringProjected(Sim.Prediction+Proj.Radius, 0f, Proj.Radius*2, Color.GreenYellow, "PIP");

        if (State == SimState.Exiting && Result.Intersect.NotZero())
        {
            DrawCircleProjected(Result.Intersect, Tgt.Radius*0.4f, Color.Yellow, 2f);
        }
        if (State == SimState.Exiting)
        {
            DrawText(300,5, $"Exit in {ExitCounter.String(1)}s");
        }
        if (State == SimState.Starting)
        {
            DrawText(300,5, $"Start in {StartCounter.String(1)}s");
        }
    }

    void DrawText(float x, float y, string text) => Game.DrawText(x, y, text);
    
    void UpdateCameraMatrix()
    {
        Camera.Z = Camera.Z.Clamped(80f, Sim.MaxCameraHeight);
        SetViewPerspective(Matrices.CreateLookAtDown(Camera.X, Camera.Y, -Camera.Z));
    }

    void UpdateSimScaleAndBounds()
    {
        AABoundingBox2D bounds = Sim.Bounds ?? GetSimulationBounds();
        Camera.X = bounds.CenterX;
        Camera.Y = bounds.CenterY;
        UpdateCameraMatrix();
        
        float simBoundsSize = bounds.Height;
        float currentSize = (float)ProjectToScreenSize(simBoundsSize);
        float wantedSize = ScreenHeight * 0.75f;
        float diff = wantedSize - currentSize;

        while (Math.Abs(diff) > 20)
        {
            Camera.Z += diff < 0 ? 10 : -10;
            UpdateCameraMatrix();
            currentSize = (float)ProjectToScreenSize(simBoundsSize);

            float newDiff = wantedSize - currentSize;
            if (diff < 0 && newDiff > 0 || diff > 0 && newDiff < 0)
                break; // overshoot, quit the loop
            diff = newDiff;
        }
    }

    AABoundingBox2D GetSimulationBounds()
    {
        AABoundingBox2D bounds = default;
        foreach (SimObject o in Objects)
        {
            bounds = bounds.Merge(new(o.Position, o.Radius));
        }
        
        // now make it symmetrical so the camera is stable
        float maxX = Math.Max(Math.Abs(bounds.X1), Math.Abs(bounds.X2));
        float maxY = Math.Max(Math.Abs(bounds.Y1), Math.Abs(bounds.Y2));
        return new(-maxX, -maxY, maxX, maxY);
    }
}
