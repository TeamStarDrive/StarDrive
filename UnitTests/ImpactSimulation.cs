using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        public bool EnablePauses = true;
        public string Name = "";
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

    class ImpactSimWindow : GameDummy
    {
        public readonly Vector2 ScreenCenter;
        public KeyboardState Keys;
        public ImpactSimWindow() : base(1024,1024,true)
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            GlobalStats.XRES = (int)ScreenSize.X; // Required for DrawLine...
            GlobalStats.YRES = (int)ScreenSize.Y;
            ScreenCenter = ScreenSize * 0.5f;
            IsFixedTimeStep = false;
        }
        
        protected override void BeginRun()
        {
            base.BeginRun();
            Fonts.LoadContent(Content);
            //Keys = Keyboard.GetState();
        }

        protected override void Update(GameTime time)
        {
            //Keys = Keyboard.GetState();
            base.Update(time);
        }

        protected override void Draw(GameTime time)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(time);
        }
    }

    class ImpactSimulation : IGameComponent, IDrawable, IUpdateable
    {
        SimulationObject OurShip;
        SimulationObject Target;
        SimulationObject Projectile;

        SimResult Result;
        bool SimulationStarted;
        bool SimulationFinished;
        float StartIn = 1f;
        float ExitIn = 1f;

        SimParameters Sim;
        float SimTime;
        Vector2 SimCenter;

        bool HasMissed;
        float PrevDistance;

        ImpactSimWindow Owner;
        
        public bool Visible { get; } = true;
        public int DrawOrder { get; } = 0;
        public event EventHandler VisibleChanged;
        public event EventHandler DrawOrderChanged;

        public bool Enabled { get; } = true;
        public int UpdateOrder { get; } = 0;
        public event EventHandler EnabledChanged;
        public event EventHandler UpdateOrderChanged;

        public ImpactSimulation(ImpactSimWindow win, TestImpactPredictor.Scenario s, SimParameters sim)
        {
            Sim = sim;
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
            PrevDistance = Projectile.Position.Distance(Target.Position);
            Owner = win;
            Owner.Components.Add(this);
        }
                
        public void Initialize()
        {
            
        }

        public SimResult WaitForResult()
        {
            Owner.Show();
            Owner.RunOne();
            System.Windows.Forms.Application.Run(Owner.Form);
            Owner.Hide();
            //Owner.CreateWindow();
            Owner.Components.Remove(this);
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

        public void Update(GameTime time)
        {
            if (!Sim.EnablePauses)
                SimulationStarted = true;

            if (!SimulationStarted)
            {
                if (Owner.Keys.IsKeyDown(Keys.Space))
                    SimulationStarted = true;
                StartIn -= (float)time.ElapsedRealTime.TotalSeconds;
                if (StartIn > 0f)
                    return;
                SimulationStarted = true;
            }

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

                float distance = Projectile.Position.Distance(Target.Position);
                if (distance <= (Projectile.Radius+Target.Radius))
                {
                    SimulationFinished = true;
                    HasMissed = false;

                    // final simulation correction towards Target
                    float speed = Projectile.Velocity.Length();
                    float timeAdjust = distance / speed;
                    timeAdjust *= 1.09f; // additional heuristic precision adjustment

                    Result.Intersect = Projectile.Position + Projectile.Velocity*timeAdjust;
                    Result.Time = SimTime + timeAdjust;
                }
                else
                {
                    if (distance > PrevDistance)
                    {
                        HasMissed = true;
                        Projectile.Name = "Projectile MISSED";
                    }
                    PrevDistance = distance;
                }


                if (SimTime >= Sim.Duration)
                {
                    SimulationFinished = true;
                }

                (Vector2 min, Vector2 max) = GetSimulationBounds();
                float width = min.Distance(max);
                SimCenter = (min + max)/2f;
                Sim.Scale = (Owner.ScreenSize.X - 200f) / (width * 2.0f);
                Sim.Scale = Sim.Scale.Clamped(0.01f, 2.0f);
            }
            else // finished
            {
                if (!Sim.EnablePauses || Owner.Keys.IsKeyDown(Keys.Space))
                    ExitIn = 0f;

                ExitIn -= (float)time.ElapsedRealTime.TotalSeconds;
                if (ExitIn <= 0f)
                    System.Windows.Forms.Application.();
            }
        }

        void DrawText(float x, float y, string text)
        {
            Owner.Batch.DrawString(Fonts.Arial14Bold, text, new Vector2(x,y), Color.White);
        }

        public void Draw(GameTime time)
        {
            SpriteBatch batch = Owner.Batch;
            batch.Begin();

            Vector2 center = -SimCenter*Sim.Scale + Owner.ScreenCenter;
            OurShip.Draw(batch, center);
            Target.Draw(batch, center);
            Projectile.Draw(batch, center);

            DrawText(5,  5, $"Sim {Sim.Name} Time {SimTime.String(2)}s / {Sim.Duration.String(2)}s");
            DrawText(5, 25, $"  Scale      {Sim.Scale.String(2)}");
            DrawText(5, 45, $"  OurShip    {OurShip.Velocity.Length().String(),-3}m/s {OurShip.Position}");
            DrawText(5, 65, $"  Target     {Target.Velocity.Length().String(),-3}m/s {Target.Position}");
            DrawText(5, 85, $"  Projectile {Projectile.Velocity.Length().String(),-3}m/s {Projectile.Position} {(HasMissed?"MISSED":"")}");
            DrawText(5,105, $"  {Result}");

            if (SimulationFinished && Result.Intersect.NotZero())
            {
                Vector2 pos = center + Result.Intersect*Sim.Scale;
                batch.DrawCircle(pos, 10f*Sim.Scale, Color.Yellow, 2);
            }
            if (SimulationFinished)
            {
                DrawText(300,5, $"Exit in {ExitIn.String(1)}s");
            }
            if (!SimulationStarted)
            {
                DrawText(300,5, $"Start in {StartIn.String(1)}s");
            }
            batch.End();
        }
    }
}
