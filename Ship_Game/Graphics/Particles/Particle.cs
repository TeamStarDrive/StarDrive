using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Graphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Utils;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game;

public sealed class Particle : IParticle
{
    // Settings class controls the appearance and animation of this particle system.
    readonly ParticleSettings Settings;

    // Custom effect for drawing particles. This computes the particle
    // animation entirely in the vertex shader: no per-particle CPU work required!
    #pragma warning disable CA2213 // resources managed by Content
    Effect ParticleEffect;
    Texture2D ParticleTexture;
    #pragma warning restore CA2213
    Map<string, EffectParameter> FxParams = new();

    // all currently active particle buffers
    Array<ParticleVertexBuffer> Buffers = new();

    // This is the actual particle count, which is Particles.Length / 4
    public int MaxParticles { get; }

    public int ActiveParticles { get; private set; }
    public bool IsOutOfParticles => ActiveParticles == MaxParticles;

    // Store the current time, in seconds.
    float CurrentTime;

    public string Name { get; set; }

    public int ParticleId { get; }

    /// <summary>
    /// Can be used to disable this particle system (for debugging purposes or otherwise)
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// If true, particle shader will generate debug markers around particles
    /// </summary>
    public bool EnableDebug { get; set; }

    readonly ThreadSafeRandom Random = new();
    readonly GraphicsDevice GraphicsDevice;
    readonly ParticleManager Manager;
    readonly GameContentManager Content; // for loading the fx file and particle tex

    readonly object Sync = new();
    Array<ParticleVertex> PendingParticles = new();
    Array<ParticleVertex> BackBuffer = new();

    public Particle(ParticleManager manager, GameContentManager content, ParticleSettings settings, int id)
    {
        Name = settings.Name;
        GraphicsDevice = content.Device;
        Manager = manager;
        Content = content;
        ParticleId = id;

        MaxParticles = settings.MaxParticles;
        if (MaxParticles == 0) // special case, the particle system is totally disabled
        {
            IsEnabled = false;
            return;
        }

        Settings = settings.Clone();
        Settings.MaxParticles = MaxParticles; 

        LoadParticleEffect();
    }

    void LoadParticleEffect()
    {
        ParticleEffect = Settings.GetEffect(Content);
        ParticleTexture = Settings.GetTexture(Content);

        FxParams = new();
        foreach (var parameter in ParticleEffect.Parameters)
            FxParams[parameter.Name] = parameter;
    }

    void SetParticleParameters(in Matrix view, in Matrix projection, GraphicsDevice device)
    {
        FxParams["View"].SetValue(view);
        FxParams["Projection"].SetValue(projection);
            
        // Set an effect parameter describing the viewport size. This is
        // needed to convert particle sizes into screen space point sizes.
        FxParams["ViewportScale"].SetValue(new Vector2(0.5f / device.Viewport.AspectRatio, -0.5f));

        // Set an effect parameter describing the current time. All the vertex
        // shader particle animation is keyed off this value.
        FxParams["CurrentTime"].SetValue(CurrentTime);

        // Set the values of parameters that do not change.
        FxParams["Duration"].SetValue((float)Settings.Duration.TotalSeconds);
        FxParams["DurationRandomness"].SetValue(Settings.DurationRandomness);
        FxParams["AlignRotationToVelocity"].SetValue(Settings.AlignRotationToVelocity);
        FxParams["EndVelocity"].SetValue(Settings.EndVelocity);

        Color[] startColor = Settings.StartColorRange;
        Color[] endColor = Settings.EndColorRange ?? startColor;
        FxParams["StartMinColor"].SetValue(startColor[0].ToVector4());
        FxParams["StartMaxColor"].SetValue(startColor[startColor.Length-1].ToVector4());
        FxParams["EndMinColor"].SetValue(endColor[0].ToVector4());
        FxParams["EndMaxColor"].SetValue(endColor[endColor.Length-1].ToVector4());

        // To reach endColor at relativeAge=EndColorTime
        // Set EndColorTimeMul multiplier to 1/EndColorTime so it reaches EndColor at that relativeAge
        // Ex: EndColorTime=0.75, EndColorTimeMul=1/0.75=1.33, EndColor is reached at relativeAge*1.33, so faster
        FxParams["EndColorTimeMul"].SetValue(1f / Settings.EndColorTime);

        FxParams["RotateSpeed"].SetValue(new Vector2(Settings.RotateSpeed.Min, Settings.RotateSpeed.Max));

        Range startSize = Settings.StartEndSize[0];
        Range endSize = Settings.StartEndSize[Settings.StartEndSize.Length - 1];
        FxParams["StartSize"].SetValue(new Vector2(startSize.Min, startSize.Max));
        FxParams["EndSize"].SetValue(new Vector2(endSize.Min, endSize.Max));

        FxParams["Texture"].SetValue(ParticleTexture);

        string technique = "FullDynamicParticles";
        switch (Settings.Static)
        {
            case false when Settings.IsAlignRotationToVel:
                technique = "DynamicAlignRotationToVelocityParticles";
                break;
            case false when !Settings.IsRotating:
                technique = "DynamicNonRotatingParticles";
                break;
            case true when Settings.IsRotating:
                technique = "StaticRotatingParticles";
                break;
            case true when !Settings.IsRotating:
                technique = "StaticNonRotatingParticles";
                break;
        }

        ParticleEffect.CurrentTechnique = ParticleEffect.Techniques[technique];
    }

    public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition)
    {
        return new(this, particlesPerSecond, scale: 1f, initialPosition);
    }

    public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector3 initialPosition, float scale)
    {
        return new(this, particlesPerSecond, scale: scale, initialPosition);
    }

    public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition)
    {
        return new(this, particlesPerSecond, scale: 1f, new Vector3(initialPosition, 0f));
    }

    public ParticleEmitter NewEmitter(float particlesPerSecond, in Vector2 initialPosition, float scale)
    {
        return new(this, particlesPerSecond, scale: scale, new Vector3(initialPosition, 0f));
    }

    /// <summary>
    /// Updates the Particle queue by retiring aged particles and submitting pending particles
    /// </summary>
    public void Update(float totalSimulationTime)
    {
        var buffers = Buffers;
        if (buffers == null || !IsEnabled)
            return;

        CurrentTime = totalSimulationTime;
        int numActive = UpdateBuffers(totalSimulationTime, buffers);

        lock (Sync)
        {
            // swap PendingParticles into BackBuffer
            (BackBuffer, PendingParticles) = (PendingParticles, BackBuffer);
        }

        int count = BackBuffer.Count;
        if (count > 0 && numActive < MaxParticles)
        {
            ParticleVertex[] backBuffer = BackBuffer.GetInternalArrayItems();

            if (Buffers.IsEmpty)
                Buffers.Add(Manager.GetReusableBuffer());

            ParticleVertexBuffer last = Buffers.Last;
            for (int i = 0; i < count && numActive < MaxParticles; ++i)
            {
                ++numActive;
                if (!last.Add(in backBuffer[i]))
                {
                    last = Manager.GetReusableBuffer();
                    last.Add(in backBuffer[i]);
                    Buffers.Add(last);
                }
            }

            ActiveParticles = numActive;
            BackBuffer.Clear();
        }
        // if we have no more particles allocated, reset all counters to mitigate potential overflow
        else if (numActive == 0)
        {
            // If we let our timer go on increasing for ever, it would eventually
            // run out of floating point precision, at which point the particles
            // would render incorrectly. An easy way to prevent this is to notice
            // that the time value doesn't matter when NO particles are being drawn,
            // so we can reset it back to zero any time the active queue is empty.
            CurrentTime = 0f;
            ActiveParticles = 0;

            // if we have 0 active particles, we should give the used buffers
            // back to manager, so it can reuse them or delete them
            foreach (var buffer in buffers)
                Manager.FreeVertexBuffer(buffer);
            buffers.Clear();
        }
    }

    // @return Number of active particles
    int UpdateBuffers(float totalSimulationTime, Array<ParticleVertexBuffer> buffers)
    {
        float particleDuration = (float)Settings.Duration.TotalSeconds;
        int numActive = 0;
        for (int i = 0; i < buffers.Count; ++i)
        {
            var buffer = buffers[i];
            if (buffer.Update(totalSimulationTime, particleDuration, Settings.Static))
            {
                buffers.RemoveAt(i);
                --i;
                Manager.FreeVertexBuffer(buffer);
            }
            else
            {
                numActive += buffer.ActiveParticles;
            }
        }
        return numActive;
    }

    public void Draw(in Matrix view, in Matrix projection, bool nearView)
    {
        var buffers = Buffers;
        if (buffers == null || !IsEnabled || (!nearView && Settings.OnlyNearView))
            return;

        bool hasActiveParticles = false;
        foreach (var buffer in buffers)
        {
            if (buffer.ActiveParticles > 0)
            {
                hasActiveParticles = true;
                break;
            }
        }

        if (hasActiveParticles)
        {
            GraphicsDevice device = GraphicsDevice;
            RenderStates.EnableAlphaBlend(device, Settings.SrcDstBlend[0], Settings.SrcDstBlend[1]);
            RenderStates.EnableAlphaTest(device, CompareFunction.Greater, referenceAlpha:0);
            RenderStates.DisableDepthWrite(device);

            if (EnableDebug)
            {
                RenderStates.DisableAlphaBlend(device);
                RenderStates.DisableAlphaTest(device);
            }
                
            SetParticleParameters(in view, in projection, device);

            foreach (var buffer in buffers)
            {
                buffer.Draw(ParticleEffect);
            }

            RenderStates.DisableAlphaTest(device);
            RenderStates.EnableDepthWrite(device);
        }
    }

    public void AddParticle(in Vector3 position, in Vector3 velocity, float scale, Color color)
    {
        // when Graphics device is reset, this particle system will be disposed
        // and Particles will be set to null
        if (Buffers == null || !IsEnabled)
            return;

        // thread unsafe check: can we even add any particles?
        int maybeFreeParticles = (MaxParticles - ActiveParticles) - PendingParticles.Count;
        if (maybeFreeParticles <= 0)
            return;

        // Adjust the input velocity based on how much
        // this particle system wants to be affected by it.
        Vector3 v = velocity * Settings.InheritOwnerVelocity;

        ThreadSafeRandom random = Random;

        // Add in some random amount of horizontal velocity.
        Range randVelX = Settings.RandomVelocityXY[0];
        Range randVelY = Settings.RandomVelocityXY[1];
        float velX = randVelX.Min.LerpTo(randVelX.Max, random.Float());
        float velY = randVelY.Min.LerpTo(randVelY.Max, random.Float());

        if (Settings.AlignRandomVelocityXY)
        {
            // aligned to global camera in Universe
            Vector3 forward = (Settings.InheritOwnerVelocity == 0 ? velocity : v).Normalized();
            Vector3 right;
            if (forward == Vector3.Zero) // emitter is stationary, follow universe coordinates
            {
                forward = new Vector3(0, -1, 0); // -Y is UP in universe
                right = Vector3.UnitX; // since forward is pointing up, we must point +X = right
            }
            else
            {
                right = forward.RightVector(new Vector3(0, 0, -1));
            }
            v += right * velX;
            v += forward * velY;
        }
        else
        {
            float horizontalAngle = random.Float() * RadMath.TwoPI;
            v.X += velX * RadMath.Cos(horizontalAngle);
            v.Z += velX * RadMath.Sin(horizontalAngle);
            v.Y += velY;
        }

        // Choose four random control values. These will be used by the vertex
        // shader to give each particle a different size, rotation, and color.
        var randomValues = new Color(random.Byte(), random.Byte(), random.Byte(), random.Byte());

        var vertex = new ParticleVertex()
        {
            Position = position,
            Velocity = v,
            Color = color,
            Random = randomValues,
            Scale = scale,
            Time = CurrentTime,
        };

        lock (Sync)
        {
            // check if we can actually draw the particle
            if (PendingParticles.Count < maybeFreeParticles)
                PendingParticles.Add(vertex);
        }
    }

    public void AddParticle(in Vector3 position)
    {
        AddParticle(position, Vector3.Zero, 1f, Color.White);
    }
    public void AddParticle(in Vector3 position, float scale)
    {
        AddParticle(position, Vector3.Zero, scale, Color.White);
    }
    public void AddParticle(in Vector3 position, in Vector3 velocity)
    {
        AddParticle(position, velocity, 1f, Color.White);
    }

    public void AddMultipleParticles(int numParticles, in Vector3 position)
    {
        AddMultipleParticles(numParticles, position, Vector3.Zero, 1f, Color.White);
    }
    public void AddMultipleParticles(int numParticles, in Vector3 position, float scale)
    {
        AddMultipleParticles(numParticles, position, Vector3.Zero, scale, Color.White);
    }
    public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity)
    {
        AddMultipleParticles(numParticles, position, velocity, 1f, Color.White);
    }
    public void AddMultipleParticles(int numParticles, in Vector3 position, in Vector3 velocity, float scale, Color color)
    {
        for (int i = 0; i < numParticles; ++i)
            AddParticle(position, velocity, scale, color);
    }

    void Dispose(bool disposing)
    {
        if (Buffers != null)
        {
            Buffers.ClearAndDispose();
            Buffers = null;
        }
        Random.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Particle()
    {
        Dispose(false);
    }
}
