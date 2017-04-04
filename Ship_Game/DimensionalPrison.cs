using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game
{
    public sealed class DimensionalPrison : Anomaly, IDisposable
    {
        public Vector2 p1;
        public Vector2 p2;
        public Vector2 p3;
        public string PlatformName = "Mysterious Platform";
        public Vector2 Pos;
        public string PrisonerID;
        private BackgroundItem Prison;
        private Beam b1;
        private Beam b2;
        private Beam b3;
        private Ship s1;
        private Ship s2;
        private Ship s3;

        private int numCreated;
        private int numToCreate = 9;
        private float timer;

        public DimensionalPrison(Vector2 pos)
        {
            this.p1 = pos + new Vector2(0f, -400f);
            this.p2 = pos + new Vector2(-400f, 400f);
            this.p3 = pos + new Vector2(400f, 400f);
            this.s1 = Ship.CreateShipAtPoint(this.PlatformName, EmpireManager.Unknown, this.p1);
            this.s2 = Ship.CreateShipAtPoint(this.PlatformName, EmpireManager.Unknown, this.p2);
            this.s3 = Ship.CreateShipAtPoint(this.PlatformName, EmpireManager.Unknown, this.p3);
            this.Pos = pos;
            var r = new Rectangle((int)pos.X - 200, (int)pos.Y - 200, 400, 400);
            this.Prison = new BackgroundItem();
            this.Prison.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
            this.Prison.UpperLeft = new Vector3((float)r.X, (float)r.Y, 0f);
            this.Prison.LowerLeft = this.Prison.UpperLeft + new Vector3(0f, (float)r.Height, 0f);
            this.Prison.UpperRight = this.Prison.UpperLeft + new Vector3((float)r.Width, 0f, 0f);
            this.Prison.LowerRight = this.Prison.UpperLeft + new Vector3((float)r.Width, (float)r.Height, 0f);
            this.Prison.Texture = ResourceManager.TextureDict["star_neutron"];
            this.Prison.FillVertices();
            this.b1 = new Beam(this.p1, pos, 75, this.s1)
            {
                Weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
            };
            this.b1.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
            this.s1.AddBeam(this.b1);
            this.b1.Infinite = true;
            this.b1.Range = 2500f;
            this.b1.PowerCost = 0f;
            this.b1.DamageAmount = 0f;
            this.b2 = new Beam(this.p2, pos, 75, this.s2)
            {
                Weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
            };
            this.b2.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
            this.b2.Infinite = true;
            this.s2.AddBeam(this.b2);
            this.b2.Range = 2500f;
            this.b2.PowerCost = 0f;
            this.b2.DamageAmount = 0f;
            this.b3 = new Beam(this.p3, pos, 75, this.s3)
            {
                Weapon = ResourceManager.WeaponsDict["AncientRepulsor"]
            };
            this.b3.LoadContent(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection);
            this.b3.Infinite = true;
            this.s3.AddBeam(this.b3);
            this.b3.Range = 2500f;
            this.b3.PowerCost = 0f;
            this.b3.DamageAmount = 0f;
        }

        public override void Draw()
        {
            Anomaly.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Anomaly.screen.ScreenManager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            Anomaly.screen.ScreenManager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            for (int i = 0; i < 20; i++)
            {
                Anomaly.screen.sparks.AddParticleThreadA(new Vector3(this.Pos, 0f) + this.GenerateRandomWithin(100f), this.GenerateRandomWithin(25f));
            }
            if (RandomMath.RandomBetween(0f, 100f) > 97f)
            {
                Anomaly.screen.flash.AddParticleThreadA(new Vector3(this.Pos, 0f), Vector3.Zero);
            }
            this.Prison.Draw(Anomaly.screen.ScreenManager, Anomaly.screen.view, Anomaly.screen.projection, 1f);
        }

        private Vector2 GenerateRandomV2(float radius)
        {
            return new Vector2(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
        }

        private Vector3 GenerateRandomWithin(float radius)
        {
            return new Vector3(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
        }

        public override void Update(float elapsedTime)
        {
            if (!this.s1.Active && !this.s2.Active && !this.s3.Active)
            {
                DimensionalPrison dimensionalPrison = this;
                dimensionalPrison.timer = dimensionalPrison.timer - elapsedTime;
                if (this.timer <= 0f)
                {
                    Ship enemy = Ship.CreateShipAtPoint("Heavy Drone", EmpireManager.Remnants, this.Pos);
                    enemy.Velocity = this.GenerateRandomV2(100f);
                    enemy.AI.State = AIState.AwaitingOrders;
                    this.timer = 2f;
                    DimensionalPrison dimensionalPrison1 = this;
                    dimensionalPrison1.numCreated = dimensionalPrison1.numCreated + 1;
                }
                if (this.numCreated == this.numToCreate)
                {
                    Anomaly.screen.anomalyManager.AnomaliesList.QueuePendingRemoval(this);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DimensionalPrison() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            Prison?.Dispose(ref Prison);
        }
    }
}