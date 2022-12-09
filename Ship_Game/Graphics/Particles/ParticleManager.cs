using System;
using System.Collections.Generic;
using System.IO;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;

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
        public IParticle MissileThrustFlare;
        public IParticle IonTrail;
        public IParticle BlueSparks;
        public IParticle ModuleSmoke;
        public IParticle IonRing;
        public IParticle IonRingReversed;
        public IParticle Bubble;

        readonly GameContentManager Content;
        readonly Array<IParticle> Tracked = new();
        readonly Map<string, IParticle> ByName = new();
        readonly Map<string, ParticleEffect> Effects = new();
        readonly Map<string, ParticleSettings> Settings = new();

        public IReadOnlyList<IParticle> ParticleSystems => Tracked;

        public ParticleManager(GameContentManager content)
        {
            Content = content;
            Reload();
        }

        public void Unload()
        {
            GameBase.ScreenManager?.RemoveHotLoadTarget("3DParticles/Particles.yaml");

            var effects = Effects.Values.ToArr();
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
            CreateParticles();

            BeamFlash         = Get("BeamFlash");
            ThrustEffect      = Get("ThrustEffect");
            EngineTrail       = Get("EngineTrail");
            Explosion         = Get("Explosion");
            PhotonExplosion   = Get("PhotonExplosion");
            ExplosionSmoke    = Get("ExplosionSmoke");
            ProjectileTrail   = Get("ProjectileTrail");
            JunkSmoke         = Get("JunkSmoke");
            MissileSmokeTrail = Get("MissileSmokeTrail");
            FireTrail         = Get("FireTrail");
            SmokePlume        = Get("SmokePlume");
            Fire              = Get("Fire");
            Flame             = Get("Flame");
            SmallFire         = Get("SmallFire");
            Sparks            = Get("Sparks");
            Lightning         = Get("Lightning");
            Flash             = Get("Flash");
            StarParticles     = Get("StarParticles");
            Galaxy            = Get("Galaxy");
            AsteroidParticles = Get("AsteroidParticles");
            MissileThrustFlare= Get("MissileThrustFlare");
            IonTrail          = Get("IonTrail");
            BlueSparks        = Get("BlueSparks");
            ModuleSmoke       = Get("ModuleSmoke");
            IonRing           = Get("IonRing");
            IonRingReversed   = Get("IonRingReversed");
            Bubble            = Get("Bubble");

            FileInfo pEffects = GameBase.ScreenManager.AddHotLoadTarget(null, "3DParticles/ParticleEffects.yaml", f => Reload());
            LoadParticleEffects(pEffects);
        }

        void LoadParticleSettings(FileInfo file)
        {
            Array<ParticleSettings> list = YamlParser.DeserializeArray<ParticleSettings>(file);
            foreach (ParticleSettings ps in list)
            {
                if (GlobalStats.IsUnitTest)
                    ps.MaxParticles = 0; // force-disable the PS in unit tests

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

        void CreateParticles()
        {
            foreach (var setting in Settings)
            {
                var ps = new Particle(Content, setting.Value, id: Tracked.Count);
                Tracked.Add(ps);
                ByName.Add(setting.Key, ps);
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

        IParticle Get(string name)
        {
            return ByName[name];
        }

        public IParticle GetParticleOrNull(string particleName)
        {
            return ByName.Get(particleName, out IParticle p) ? p : null;
        }

        // Creates a new effect instance, OR returns null if effect does not exist
        public ParticleEffect CreateEffect(string effectName, in Vector3 initialPos, GameObject context)
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

        // @param totalSimulationTime Total seconds elapsed since simulation started
        public void Update(float totalSimulationTime)
        {
            for (int i = 0; i < Tracked.Count; ++i)
            {
                IParticle ps = Tracked[i];
                ps.Update(totalSimulationTime);
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
