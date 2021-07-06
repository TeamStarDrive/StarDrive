﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Graphics.Particles
{
    public class ParticleManager : IDisposable
    {
        public IParticleSystem BeamFlash;
        public IParticleSystem Explosion;
        public IParticleSystem PhotonExplosion;
        public IParticleSystem ExplosionSmoke;
        public IParticleSystem ProjectileTrail;
        public IParticleSystem FireTrail;
        public IParticleSystem SmokePlume;
        public IParticleSystem Fire;
        public IParticleSystem ThrustEffect;
        public IParticleSystem EngineTrail;
        public IParticleSystem Flame;
        public IParticleSystem SmallFlame;
        public IParticleSystem Sparks;
        public IParticleSystem Lightning;
        public IParticleSystem Flash;
        public IParticleSystem StarParticles;
        public IParticleSystem Galaxy;

        readonly Data.GameContentManager Content;
        readonly GraphicsDevice Device;
        readonly Array<IParticleSystem> Tracked = new Array<IParticleSystem>();

        public IReadOnlyList<IParticleSystem> ParticleSystems => Tracked;

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
            ThrustEffect    = Add("ThrustEffect");
            EngineTrail     = Add("EngineTrail");
            Explosion       = Add("Explosion");
            PhotonExplosion = Add("PhotonExplosion");
            ExplosionSmoke  = Add("ExplosionSmoke");
            ProjectileTrail = Add("ProjectileTrail");
            FireTrail       = Add("FireTrail");
            SmokePlume      = Add("SmokePlume");
            Fire            = Add("Fire");
            Flame           = Add("Flame");
            SmallFlame      = Add("Flame", 0.25f, (int)(4000 * GlobalStats.DamageIntensity));
            Sparks          = Add("Sparks");
            Lightning       = Add("Lightning");
            Flash           = Add("Flash");
            StarParticles   = Add("StarParticles");
            Galaxy          = Add("Galaxy");
        }

        IParticleSystem Add(string name, float scale = 1, int particleCount = -1)
        {
            var settings = ParticleSettings.Get(name);
            var ps = new ParticleSystem(Content, settings, Device, scale, particleCount);
            Tracked.Add(ps);
            return ps;
        }

        public void UnloadContent()
        {
            foreach (IParticleSystem sys in Tracked)
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
                IParticleSystem ps = Tracked[i];
                ps.Update(elapsed);
            }
        }

        public void Draw(in Matrix view, in Matrix projection, bool nearView)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                IParticleSystem ps = Tracked[i];
                ps.Draw(view, projection, nearView);
            }
        }
    }
}
