using System;
using System.Collections.Generic;
using System.IO;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game.Graphics.Particles;

public sealed class ParticleManager : IDisposable
{
    // these are disposed via `Tracked` array
    #pragma warning disable CA2213
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
    #pragma warning restore CA2213

    readonly GameContentManager Content;
    readonly Array<IParticle> Tracked = new();
    readonly Map<string, IParticle> ByName = new();
    readonly Map<string, ParticleEffect> Effects = new();
    readonly Map<string, ParticleSettings> Settings = new();

    public IReadOnlyList<IParticle> ParticleSystems => Tracked;

    readonly Array<ParticleVertexBuffer> AllBuffers = new();
    readonly Array<ParticleVertexBuffer> FreeBuffers = new();
    ParticleVertexBufferShared SharedBufferData;

    public ParticleManager(GameContentManager content)
    {
        Content = content;
        Reload();
    }

    ~ParticleManager()
    {
        if (SharedBufferData.VertexDeclaration != null)
            Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (!GlobalStats.IsUnitTest)
            Log.Write(ConsoleColor.Cyan, "ParticleManager.Unload");

        GameBase.ScreenManager?.RemoveHotLoadTarget("3DParticles/Particles.yaml");

        lock (Effects)
        {
            foreach (var fx in Effects.Values)
                fx.Dispose();
            Effects.Clear();
        }

        foreach (IParticle sys in Tracked)
        {
            sys.Dispose();
        }

        Tracked.Clear();
        ByName.Clear();
        Settings.Clear();

        lock (AllBuffers)
        {
            AllBuffers.ClearAndDispose();
            FreeBuffers.Clear();
        }

        SharedBufferData?.Dispose();
    }

    // You can call this to Reload content
    public void Reload()
    {
        GameLoadingScreen.SetStatus("LoadParticles");
        Dispose(true);

        SharedBufferData = new(Content.Device);

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
            var ps = new Particle(this, Content, setting.Value, id: Tracked.Count);
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

    // Gets a new or reused vertex buffer
    public ParticleVertexBuffer GetReusableBuffer()
    {
        lock (AllBuffers)
        {
            if (FreeBuffers.NotEmpty)
            {
                var b = FreeBuffers.PopLast();
                b.Reset();
                return b;
            }
            else
            {
                ParticleVertexBuffer b = new(SharedBufferData);
                AllBuffers.Add(b);
                return b;
            }
        }
    }

    // Free the vertex buffer so it can be reused
    public void FreeVertexBuffer(ParticleVertexBuffer b)
    {
        lock (AllBuffers)
        {
            FreeBuffers.Add(b);

            // if we have too many free buffers, dispose half of them
            if (FreeBuffers.Count < (AllBuffers.Count / 2))
                return;

            int toDispose = FreeBuffers.Count / 2;
            if (toDispose < 4) // but only if there's enough of them
                return;

            float mem = toDispose * ParticleVertexBuffer.Size * 4 * ParticleVertex.SizeInBytes;
            Log.Info($"Disposing {toDispose} particle buffers, totaling {mem/(1024f*1024f):0.0}MB");
            for (int i = 0; i < toDispose; ++i)
            {
                var buffer = FreeBuffers.PopLast();
                buffer.Dispose();
                AllBuffers.Remove(buffer);
            }
        }
    }

    public float GetUsedGPUMemory()
    {
        int n = AllBuffers.Count;
        float vboMem = n * ParticleVertexBuffer.Size * 4 * ParticleVertex.SizeInBytes;
        float iboMem = 1 * ParticleVertexBuffer.Size * 6 * sizeof(ushort);
        float vdMem = 1 * ParticleVertex.VertexElements.Length * 8;
        return vboMem + iboMem + vdMem;
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