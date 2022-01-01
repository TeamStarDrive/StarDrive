using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles
{
    public class ParticleManager : IDisposable
    {
        public IParticle BeamFlash;
        public IParticle Explosion;
        public IParticle PhotonExplosion;
        public IParticle ExplosionSmoke;
        public IParticle ProjectileTrail;
        public IParticle JunkSmoke;
        public IParticle FireTrail;
        public IParticle MissileSmokeTrail;
        public IParticle SmokePlume;
        public IParticle Fire;
        public IParticle ThrustEffect;
        public IParticle EngineTrail;
        public IParticle Flame;
        public IParticle SmallFire;
        public IParticle Sparks;
        public IParticle Lightning;
        public IParticle Flash;
        public IParticle StarParticles;
        public IParticle Galaxy;
        public IParticle AsteroidParticles;
        public IParticle MissileThrust;
        public IParticle IonTrail;
        public IParticle BlueSparks;
        public IParticle ModuleSmoke;

        readonly Data.GameContentManager Content;
        readonly GraphicsDevice Device;
        readonly Array<IParticle> Tracked = new Array<IParticle>();

        public IReadOnlyList<IParticle> ParticleSystems => Tracked;

        public ParticleManager(Data.GameContentManager content, GraphicsDevice device)
        {
            Content = content;
            Device = device;
            Reload();
        }

        // You can call this to Reload content
        public void Reload()
        {
            UnloadContent();
            BeamFlash         = Add("BeamFlash");
            ThrustEffect      = Add("ThrustEffect");
            EngineTrail       = Add("EngineTrail");
            Explosion         = Add("Explosion");
            PhotonExplosion   = Add("PhotonExplosion");
            ExplosionSmoke    = Add("ExplosionSmoke");
            ProjectileTrail   = Add("ProjectileTrail");
            JunkSmoke         = Add("JunkSmoke");
            MissileSmokeTrail = Add("MissileSmokeTrail");
            FireTrail         = Add("FireTrail");
            SmokePlume        = Add("SmokePlume");
            Fire              = Add("Fire");
            Flame             = Add("Flame");
            SmallFire         = Add("Fire", 0.35f, (int)(4000 * GlobalStats.DamageIntensity));
            Sparks            = Add("Sparks");
            Lightning         = Add("Lightning");
            Flash             = Add("Flash");
            StarParticles     = Add("StarParticles");
            Galaxy            = Add("Galaxy");
            AsteroidParticles = Add("AsteroidParticles");
            MissileThrust     = Add("MissileThrust");
            IonTrail          = Add("IonTrail");
            BlueSparks        = Add("BlueSparks");
            ModuleSmoke       = Add("ModuleSmoke");
        }

        IParticle Add(string name, float scale = 1, int particleCount = -1)
        {
            var settings = ParticleSettings.Get(name);
            var ps = new Particle(Content, settings, Device, scale, particleCount);
            if (!scale.AlmostEqual(1f))
                ps.Name = $"{name} {scale.String(2)}x";
            Tracked.Add(ps);
            return ps;
        }

        public void UnloadContent()
        {
            foreach (IParticle sys in Tracked)
                sys.Dispose();
            Tracked.Clear();
        }

        public void Dispose()
        {
            UnloadContent();
        }

        public void Update(DrawTimes elapsed)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                IParticle ps = Tracked[i];
                ps.Update(elapsed);
            }
        }

        public void Draw(in Matrix view, in Matrix projection, bool nearView)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                IParticle ps = Tracked[i];
                ps.Draw(view, projection, nearView);
            }
        }
    }
}
