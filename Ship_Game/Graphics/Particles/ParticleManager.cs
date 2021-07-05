using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles
{
    public class ParticleManager : IDisposable
    {
        public ParticleSystem BeamFlash;
        public ParticleSystem Explosion;
        public ParticleSystem PhotonExplosion;
        public ParticleSystem ExplosionSmoke;
        public ParticleSystem ProjectileTrail;
        public ParticleSystem FireTrail;
        public ParticleSystem SmokePlume;
        public ParticleSystem Fire;
        public ParticleSystem EngineTrail;
        public ParticleSystem Flame;
        public ParticleSystem SmallFlame;
        public ParticleSystem Sparks;
        public ParticleSystem Lightning;
        public ParticleSystem Flash;
        public ParticleSystem StarParticles;
        public ParticleSystem Galaxy;

        readonly Data.GameContentManager Content;
        readonly GraphicsDevice Device;
        readonly Array<ParticleSystem> Tracked = new Array<ParticleSystem>();

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
            BeamFlash       = Add("BeamFlash");
            Explosion       = Add("Explosion");
            PhotonExplosion = Add("PhotonExplosion");
            ExplosionSmoke  = Add("ExplosionSmoke");
            ProjectileTrail = Add("ProjectileTrail");
            FireTrail       = Add("FireTrail");
            SmokePlume      = Add("SmokePlume");
            Fire            = Add("Fire");
            EngineTrail     = Add("EngineTrail");
            Flame           = Add("Flame");
            SmallFlame      = Add("Flame", 0.25f, (int)(4000 * GlobalStats.DamageIntensity));
            Sparks          = Add("Sparks");
            Lightning       = Add("Lightning");
            Flash           = Add("Flash");
            StarParticles   = Add("StarParticles");
            Galaxy          = Add("Galaxy");
        }

        ParticleSystem Add(string name, float scale = 1, int particleCount = -1)
        {
            var ps = new ParticleSystem(Content, ParticleSettings.Get(name), Device, scale, particleCount);
            Tracked.Add(ps);
            return ps;
        }

        public void UnloadContent()
        {
            foreach (ParticleSystem sys in Tracked)
                sys.Dispose();
            Tracked.Clear();
        }

        public void Dispose()
        {
            UnloadContent();
        }

        public void Draw(in Matrix view, in Matrix projection)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                ParticleSystem ps = Tracked[i];
                ps.SetCamera(view, projection);
                ps.Draw();
            }
        }

        public void Update(DrawTimes elapsed)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                ParticleSystem ps = Tracked[i];
                ps.Update(elapsed);
            }
        }
    }
}
