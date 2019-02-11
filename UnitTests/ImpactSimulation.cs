using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;

namespace UnitTests
{
    class SimulationObject
    {
        public string Name;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Radius;
        public SimParameters Sim;

        public void Update(float deltaTime)
        {
            Position += Velocity * deltaTime;
        }

        public void Draw(SpriteBatch batch, Vector2 worldCenter)
        {
            float scale = Sim.Scale;
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

    class ImpactSimulation : GameDummy
    {
        readonly SimulationObject OurShip;
        readonly SimulationObject Target;
        readonly SimulationObject Projectile;
        readonly Vector2 ScreenCenter;

        SimResult Result;
        bool SimulationFinished;
        float ExitAfterFinished = 10f;

        readonly SimParameters Sim;
        float SimTime;
        Vector2 SimCenter;

        public ImpactSimulation(TestImpactPredictor.Scenario s, SimParameters sim)
            : base(1024,1024, show:true)
        {
            Sim = sim;
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            OurShip = new SimulationObject
            {
                Name = "Us",
                Position = s.Us,
                Velocity = s.UsVel,
                Color = Color.Green,
                Radius = 32,
                Sim = sim,
            };
            Target = new SimulationObject
            {
                Name = "Target",
                Position = s.Tgt,
                Velocity = s.TgtVel,
                Color = Color.Red,
                Radius = 32,
                Sim = sim,
            };
            Projectile = new SimulationObject
            {
                Name = "Projectile",
                Position = s.Us,
                Velocity = Sim.ProjectileVelocity,
                Color = Color.Orange,
                Radius = 8f,
                Sim = sim,
            };
            GlobalStats.XRES = (int)ScreenSize.X; // Required for DrawLine...
            GlobalStats.YRES = (int)ScreenSize.Y;
            ScreenCenter = ScreenSize * 0.5f;
        }

        protected override void BeginRun()
        {
            base.BeginRun();
            Fonts.LoadContent(Content);
        }

        public SimResult RunIntersectionSimulation()
        {
            IsFixedTimeStep = false;
            Run();
            return Result;
        }

        public (Vector2 Min, Vector2 Max) GetSimulationBounds()
        {
            Vector2 min = default, max = default;
            foreach (SimulationObject o in new[]{ OurShip, Target, Projectile })
            {
                Vector2 p = o.Position;
                if (p.X < min.X) min.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y;

                if (p.X > max.X) max.X = p.X;
                if (p.Y > max.Y) max.Y = p.Y;
            }
            return (min, max);
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            if (!SimulationFinished)
            {
                if (Sim.DelayBetweenSteps > 0f)
                {
                    Thread.Sleep((int)(Sim.DelayBetweenSteps*1000));
                }

                SimTime += Sim.Step;

                OurShip.Update(Sim.Step);
                Target.Update(Sim.Step);
                Projectile.Update(Sim.Step);

                if (Target.Position.InRadius(Projectile.Position, Projectile.Radius))
                {
                    SimulationFinished = true;

                    // final simulation correction towards Target
                    float speed = Projectile.Velocity.Length();
                    float distance = Target.Position.Distance(Projectile.Position);
                    float timeAdjust = distance / speed;
                    timeAdjust *= 1.09f; // additional heuristic precision adjustment

                    Result.Intersect = Projectile.Position + Projectile.Velocity*timeAdjust;
                    Result.Time = SimTime + timeAdjust;
                }

                if (SimTime >= Sim.Duration)
                {
                    SimulationFinished = true;
                }

                (Vector2 min, Vector2 max) = GetSimulationBounds();
                float width = min.Distance(max);
                SimCenter = (min + max)/2f;
                Sim.Scale = (ScreenSize.X - 200f) / (width * 2.0f);
            }
            else
            {
                ExitAfterFinished -= (float)time.ElapsedRealTime.TotalSeconds;
                if (ExitAfterFinished < 0f)
                    Exit();
            }
        }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(time);
            Batch.Begin();

            Vector2 center = -SimCenter*Sim.Scale + ScreenCenter;
            OurShip.Draw(Batch, center);
            Target.Draw(Batch, center);
            Projectile.Draw(Batch, center);

            Batch.DrawString(Fonts.Arial14Bold, $"Simulation Time {SimTime.String(2)}s / {Sim.Duration.String(2)}s",
                             new Vector2(5,5), Color.White);

            Batch.DrawString(Fonts.Arial14Bold, $"  Scale    {Sim.Scale}",
                new Vector2(5,25), Color.White);

            Batch.DrawString(Fonts.Arial14Bold, $"  OurShip    {OurShip.Velocity.Length().String()}m/s {OurShip.Position}",
                new Vector2(5,45), Color.White);

            Batch.DrawString(Fonts.Arial14Bold, $"  Target     {Target.Velocity.Length().String()}m/s {Target.Position}",
                new Vector2(5,65), Color.White);
            
            Batch.DrawString(Fonts.Arial14Bold, $"  Projectile {Projectile.Velocity.Length().String()}m/s {Projectile.Position}",
                new Vector2(5,85), Color.White);

            Batch.DrawString(Fonts.Arial14Bold, $"  {Result}",
                new Vector2(5,105), Color.White);

            if (SimulationFinished && Result.Intersect.NotZero())
            {
                Vector2 pos = center + Result.Intersect*Sim.Scale;
                Batch.DrawCircle(pos, 10f*Sim.Scale, Color.Yellow, 2);
            }
            if (SimulationFinished)
            {
                Batch.DrawString(Fonts.Arial14Bold, $"Exit in {ExitAfterFinished.String(1)}s",
                    new Vector2(300,5), Color.White);
            }
            Batch.End();
        }
    }
}
