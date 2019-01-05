using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;

namespace Ship_Game
{
    public sealed class Bomb
    {
        public static UniverseScreen Screen;
        public Vector3 Position;
        public Vector3 Velocity;
        private Planet TargetPlanet;
        public Matrix World { get; private set; }

        public string WeaponName;
        private const string TextureName = "projBall_02_orange";
        private const string ModelName   = "projBall";

        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FiretrailEmitter;

        public Empire Owner;
        //public float Facing;
        private float PlanetRadius;

        public SubTexture Texture { get; }
        public Model      Model   { get; }

        public Bomb(Vector3 position, Empire empire)
        {
            Owner = empire;
            Texture     = ResourceManager.ProjTexture(TextureName);
            Model       = ResourceManager.ProjectileModelDict[ModelName];
            WeaponName  = "NuclearBomb";
            Position    = position;
        }

        public void DoImpact()
        {
            TargetPlanet.DropBomb(this);
            Screen.BombList.QueuePendingRemoval(this);
        }

        public void SetTarget(Planet p)
        {
            TargetPlanet = p;
            PlanetRadius = TargetPlanet.SO.WorldBoundingSphere.Radius;
            Vector3 vtt = new Vector3(TargetPlanet.Center, 2500f) + 
                new Vector3(RandomMath2.RandomBetween(-500f, 500f) * p.Scale, 
                            RandomMath2.RandomBetween(-500f, 500f) * p.Scale, 0f) - Position;
            vtt = Vector3.Normalize(vtt);
            Velocity = vtt * 1350f;
        }

        public void Update(float deltaTime)
        {
            Position += Velocity*deltaTime;
            World    = Matrix.CreateTranslation(Position);
                        //* Matrix.CreateRotationZ(Facing);

            Vector3 planetPos = TargetPlanet.Center.ToVec3(z:2500f);

            float impactRadius = TargetPlanet.ShieldStrengthCurrent > 0f ? 100f : 30f;
            if (Position.InRadius(planetPos, PlanetRadius + impactRadius))
                DoImpact();


            // fiery trail radius:
            if (!Position.InRadius(planetPos, PlanetRadius + 1000f))
                return;

            if (TrailEmitter == null)
            {
                Velocity *= 0.65f;
                TrailEmitter     = Screen.projectileTrailParticles.NewEmitter(500f, Position);
                FiretrailEmitter = Screen.fireTrailParticles.NewEmitter(500f, Position);
            }
            TrailEmitter.Update(deltaTime, Position);
            FiretrailEmitter.Update(deltaTime, Position);
        }
    }
}