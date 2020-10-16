using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class DimensionalPrison : Anomaly, IDisposable
    {
        public string PlatformName = "Mysterious Platform";
        public Vector2 PlaformCenter;
        public string PrisonerId;
        private BackgroundItem Prison;
        private readonly Ship Platform1;
        private readonly Ship Platform2;
        private readonly Ship Platform3;

        private int NumSpawnedRemnants;
        private int NumRemnantsToSpawn = 9;
        private float SpawnCountdown;

        public DimensionalPrison(Vector2 plaformCenter)
        {
            PlaformCenter = plaformCenter;
            CreateDimensionalPrison(plaformCenter, 400);
            Platform1 = SpawnAncientRepulsor(plaformCenter + new Vector2(0f, -400f));
            Platform2 = SpawnAncientRepulsor(plaformCenter + new Vector2(-400f, 400f));
            Platform3 = SpawnAncientRepulsor(plaformCenter + new Vector2(400f, -400f));
        }

        private Ship SpawnAncientRepulsor(Vector2 repulsorPos)
        {
            Ship repulsor = Ship.CreateShipAtPoint(PlatformName, EmpireManager.Unknown, repulsorPos);
            var beam = new Beam(repulsor, PlaformCenter, 75)
            {
                Weapon = ResourceManager.GetWeaponTemplate("AncientRepulsor")
            };
            beam.Infinite     = true;
            beam.Range        = 2500f;
            beam.PowerCost    = 0f;
            beam.DamageAmount = 0f;
            return repulsor;
        }

        private void CreateDimensionalPrison(Vector2 center, int radius)
        {
            var screen = Empire.Universe;
            var r = new Rectangle((int)center.X - radius, (int)center.Y - radius, radius*2, radius*2);
            Prison = new BackgroundItem();
            Prison.LoadContent(screen.ScreenManager);
            Prison.UpperLeft  = new Vector3(r.X, r.Y, 0f);
            Prison.LowerLeft  = Prison.UpperLeft + new Vector3(0f, r.Height, 0f);
            Prison.UpperRight = Prison.UpperLeft + new Vector3(r.Width, 0f, 0f);
            Prison.LowerRight = Prison.UpperLeft + new Vector3(r.Width, r.Height, 0f);
            Prison.Texture = ResourceManager.Texture("star_neutron");
            Prison.FillVertices();
        }

        public override void Draw()
        {
            var screen = Empire.Universe;
            var manager = screen.ScreenManager;
            manager.GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            manager.GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            manager.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            manager.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            manager.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            manager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            manager.GraphicsDevice.RenderState.DepthBufferWriteEnable = false;
            manager.GraphicsDevice.RenderState.CullMode = CullMode.None;
            for (int i = 0; i < 20; i++)
            {
                screen.sparks.AddParticleThreadA(new Vector3(PlaformCenter, 0f) + GenerateRandomWithin(100f), GenerateRandomWithin(25f));
            }
            if (RandomMath.RandomBetween(0f, 100f) > 97f)
            {
                screen.flash.AddParticleThreadA(new Vector3(PlaformCenter, 0f), Vector3.Zero);
            }
            Prison.Draw(manager, screen.View, screen.Projection, 1f);
        }

        private Vector2 GenerateRandomV2(float radius)
        {
            return new Vector2(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
        }

        private Vector3 GenerateRandomWithin(float radius)
        {
            return new Vector3(RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius), RandomMath.RandomBetween(-radius, radius));
        }

        public override void Update(FixedSimTime timeStep)
        {
            // spawn a bunch of drones when the player has killed all platforms :)))
            if (!Platform1.Active && !Platform2.Active && !Platform3.Active)
            {
                SpawnCountdown -= timeStep.FixedTime;
                if (SpawnCountdown <= 0f)
                {
                    Ship enemy = Ship.CreateShipAtPoint("Heavy Drone", EmpireManager.Remnants, PlaformCenter);
                    enemy.Velocity = GenerateRandomV2(100f);
                    enemy.AI.State = AIState.AwaitingOrders;
                    SpawnCountdown = 2f;
                    ++NumSpawnedRemnants;
                }
                if (NumSpawnedRemnants == NumRemnantsToSpawn)
                {
                    Empire.Universe.anomalyManager.AnomaliesList.QueuePendingRemoval(this);
                }
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~DimensionalPrison() { Destroy(); }

        private void Destroy()
        {
            Prison?.Dispose(ref Prison);
        }
    }
}