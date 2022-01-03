using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;

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
        public IParticle IonRing;
        public IParticle IonRingReversed;
        public IParticle Bubble;

        readonly GameContentManager Content;
        readonly Array<IParticle> Tracked = new Array<IParticle>();
        readonly Map<string, IParticle> ByName = new Map<string, IParticle>();
        readonly Map<string, ParticleEffect> Effects = new Map<string, ParticleEffect>();
        readonly Map<string, ParticleSettings> Settings = new Map<string, ParticleSettings>();

        public IReadOnlyList<IParticle> ParticleSystems => Tracked;

        public ParticleManager(GameContentManager content)
        {
            Content = content;
            Reload();
        }

        public void Unload()
        {
            GameBase.ScreenManager.RemoveHotLoadTarget("3DParticles/Particles.yaml");

            var effects = Effects.Values.ToArray();
            lock (Effects)
            {
                Effects.Clear();
                foreach (var fx in effects)
                    fx.Dispose();
            }

            foreach (IParticle sys in Tracked)
            {
                sys.Dispose();
            }

            Tracked.Clear();
            ByName.Clear();
            Settings.Clear();
        }

        public void Dispose()
        {
            Unload();
        }

        // You can call this to Reload content
        public void Reload()
        {
            GameLoadingScreen.SetStatus("LoadParticles");
            Unload();

            FileInfo pSettings = GameBase.ScreenManager.AddHotLoadTarget(null, "3DParticles/Particles.yaml", f => Reload());
            LoadParticleSettings(pSettings);

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
            SmallFire         = Add("SmallFire");
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
            IonRing           = Add("IonRing");
            IonRingReversed   = Add("IonRingReversed");
            Bubble            = Add("Bubble");

            FileInfo pEffects = GameBase.ScreenManager.AddHotLoadTarget(null, "3DParticles/ParticleEffects.yaml", f => Reload());
            LoadParticleEffects(pEffects);
        }

        void LoadParticleSettings(FileInfo file)
        {
            Array<ParticleSettings> list = YamlParser.DeserializeArray<ParticleSettings>(file);
            foreach (ParticleSettings ps in list)
            {
                if (Settings.ContainsKey(ps.Name))
                {
                    Log.Error($"ParticleSetting duplicate definition='{ps.Name}' in Particles.yaml. Ignoring.");
                }
                else
                {
                    Settings.Add(ps.Name, ps);
                    ps.GetEffect(ResourceManager.RootContent); // compile
                }
            }
        }

        void LoadParticleEffects(FileInfo file)
        {
            var effectsData = YamlParser.DeserializeArray<ParticleEffect.ParticleEffectData>(file);
            lock (Effects)
            {
                // create the effect templates
                foreach (ParticleEffect.ParticleEffectData effectData in effectsData)
                {
                    Effects[effectData.Name] = new ParticleEffect(effectData, this);
                }
            }
        }

        ParticleSettings GetSettings(string name)
        {
            if (Settings.Count == 0)
                throw new InvalidOperationException("ParticleSettings have not been loaded!");
            if (!Settings.TryGetValue(name, out ParticleSettings ps))
                throw new InvalidDataException($"Unknown ParticleSettings Name: {name}");
            return ps;
        }

        IParticle Add(string name)
        {
            var settings = GetSettings(name);
            var ps = new Particle(Content, settings, id: Tracked.Count);
            Tracked.Add(ps);
            ByName.Add(name, ps);
            return ps;
        }

        public IParticle GetParticleOrNull(string particleName)
        {
            return ByName.Get(particleName, out IParticle p) ? p : null;
        }

        public ParticleEffect CreateEffect(string effectName, in Vector3 initialPos, GameplayObject context)
        {
            ParticleEffect template = GetEffectTemplate(effectName);
            if (template == null)
                return null;
            return new ParticleEffect(template, initialPos, context);
        }

        // Get a ParticleEffect Template
        public ParticleEffect GetEffectTemplate(string effectName)
        {
            // lock because this might be called from a background thread in Ship.Update()
            lock (Effects)
            {
                if (Effects.TryGetValue(effectName, out ParticleEffect effect))
                    return effect;
            }
            Log.Error($"ParticleEffect '{effectName}' not found!");
            return null;
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
