using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace UnitTests
{
    class SimObject
    {
        public string Name;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Radius;

        public SimObject(string name, Vector2 p, Vector2 v, Color c, float r)
        {
            Name = name;
            Position = p;
            Velocity = v;
            Color = c;
            Radius = r;
        }

        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
        }

        public void Draw(SpriteBatch batch, Vector2 worldCenter, float scale)
        {
            Vector2 pos = worldCenter + Position*scale;
            batch.DrawCircle(pos, Radius*scale, Color, 1f);
            batch.DrawLine(pos, pos + Velocity*scale, Color, 1f);
            batch.DrawString(Fonts.Arial11Bold, Name, pos + new Vector2(5,5), Color);
        }
    }

    public class SimParameters
    {
        public float Step = 1.0f / 60.0f;
        public float DelayBetweenSteps = 0f;
        public float Duration = 60.0f;
        public float Scale = 1.0f;
        public Vector2 ProjectileVelocity;
        public bool EnablePauses = true;
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

    internal class ImpactSimulation : IGameComponent, IDrawable, IUpdateable
    {
        readonly Array<SimObject> Objects = new Array<SimObject>();
        readonly SimObject Projectile;
        readonly SimObject Target;

        SimState State = SimState.Starting;
        float StartCounter = 1f;
        float ExitCounter  = 1f;
        readonly AutoResetEvent Exit = new AutoResetEvent(false);
        SimResult Result;

        readonly TestGameDummy Owner;
        readonly SimParameters Sim;

        float Time;
        Vector2 Center;
        float PrevDistance;
        
        public bool Visible     { get; } = true;
        public bool Enabled     { get; } = true;
        public int  DrawOrder   { get; } = 0;
        public int  UpdateOrder { get; } = 0;

        public ImpactSimulation(TestImpactPredictor.Scenario s, SimParameters sim)
        {
            Owner = TestGameDummy.GetOrStartInstance(1024, 1024);
            Sim   = sim;
            var us = new SimObject("Us", s.Us, s.UsVel, Color.Green, 32);
            Target = new SimObject("Target", s.Tgt, s.TgtVel, Color.Red, 32);
            Projectile = new SimObject("Projectile", s.Us, Sim.ProjectileVelocity, Color.Orange, 8);

            Objects.AddRange(new []{ us, Target, Projectile });
            PrevDistance = float.MaxValue;
            UpdateSimScaleAndBounds();
        }
        
        public void Initialize()
        {
        }

        public SimResult RunAndWaitForResult()
        {
            Owner.Components.Add(this);
            Owner.Visible = true;

            Exit.WaitOne();

            Owner.Components.Remove(this);
            Owner.Visible = false;
            return Result;
        }

        public void Update(float deltaTime)
        {
            switch (State)
            {
                case SimState.Starting: WaitingToStart(deltaTime); break;
                case SimState.Running:  UpdateSimulation();   break;
                case SimState.Exiting:  WaitingToExit(deltaTime);  break;
            }
        }

        void WaitingToExit(float deltaTime)
        {
            if (!Sim.EnablePauses || Owner.Input.IsKeyDown(Keys.Space))
                ExitCounter = 0f;

            ExitCounter -= deltaTime;
            if (ExitCounter <= 0f)
                Exit.Set();
        }

        void WaitingToStart(float deltaTime)
        {
            if (!Sim.EnablePauses || Owner.Input.IsKeyDown(Keys.Space))
                State = SimState.Running;

            if (State == SimState.Running)
                return;

            StartCounter -= deltaTime;
            if (StartCounter > 0f)
                return;

            State = SimState.Running;
        }

        void UpdateSimulation()
        {
            if (Sim.DelayBetweenSteps > 0f)
            {
                Thread.Sleep((int)(Sim.DelayBetweenSteps * 1000));
            }

            Time += Sim.Step;

            foreach (SimObject o in Objects)
                o.Update(Sim.Step);

            float distance = Projectile.Position.Distance(Target.Position);
            if (distance <= (Projectile.Radius + Target.Radius))
            {
                State = SimState.Exiting;

                // final simulation correction towards Target
                float speed = Projectile.Velocity.Length();
                float timeAdjust = distance / speed;
                timeAdjust *= 1.09f; // additional heuristic precision adjustment

                Result.Intersect = Projectile.Position + Projectile.Velocity * timeAdjust;
                Result.Time = Time + timeAdjust;
                return;
            }

            if (distance > PrevDistance)
                Projectile.Name = "Projectile MISS";
            PrevDistance = distance;

            if (Time >= Sim.Duration)
            {
                State = SimState.Exiting;
                return;
            }

            UpdateSimScaleAndBounds();
        }

        public void Draw(float deltaTime)
        {
            SpriteBatch batch = Owner.Batch;

            Vector2 center = -Center*Sim.Scale + GameBase.ScreenCenter;
            foreach (SimObject o in Objects)
                o.Draw(batch, center, Sim.Scale);

            DrawText(5,  5, $"Simulation Time {Time.String(2)}s / {Sim.Duration.String(2)}s");
            DrawText(5, 25, $"  Scale      {Sim.Scale.String(2)}");
            for (int i = 0; i < Objects.Count; ++i)
            {
                SimObject o = Objects[i];
                DrawText(5, 45 + i*20, $"  {o.Name,-16}  {o.Velocity.Length().String(),-3}m/s  {o.Position}");
            }
            DrawText(5,105, $"  {Result}");

            if (State == SimState.Exiting && Result.Intersect.NotZero())
            {
                Vector2 pos = center + Result.Intersect*Sim.Scale;
                batch.DrawCircle(pos, 10f*Sim.Scale, Color.Yellow, 2);
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

        void DrawText(float x, float y, string text) => Owner.DrawText(x, y, text);
        
        void UpdateSimScaleAndBounds()
        {
            (Vector2 min, Vector2 max) = GetSimulationBounds();
            float width = min.Distance(max);
            Center = (min + max) / 2f;
            Sim.Scale = (GameBase.ScreenWidth - 200f) / (width * 2.0f);
            Sim.Scale = Sim.Scale.Clamped(0.01f, 2.0f);
        }

        (Vector2 Min, Vector2 Max) GetSimulationBounds()
        {
            Vector2 min = default, max = default;
            foreach (SimObject o in Objects)
            {
                Vector2 p = o.Position;
                if (p.X < min.X) min.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y;

                if (p.X > max.X) max.X = p.X;
                if (p.Y > max.Y) max.Y = p.Y;
            }
            return (min, max);
        }
    }
}
