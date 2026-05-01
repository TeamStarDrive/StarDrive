using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace SDGraphics.Shaders;

// TODO Phase 2: restore runtime HLSL compilation. XNA 3.1's CompilerIncludeHandler /
// CompiledEffect / Effect.CompileEffectFromSource / TargetPlatform / CompilerOptions APIs
// are all removed in MonoGame; effects must be precompiled to MGFX (.xnb) via the Content
// Pipeline. Phase 1 §1.8.11 keeps the public surface and stubs runtime compilation.
public class Shader : IDisposable
{
    Effect Fx;
    readonly Map<string, EffectParameter> FxParameters;

    Shader(Effect fx)
    {
        Fx = fx;
        FxParameters = new();
        if (fx != null)
        {
            foreach (EffectParameter parameter in Fx.Parameters)
                FxParameters[parameter.Name] = parameter;
        }
    }

    ~Shader() { Destroy(); }

    public bool IsDisposed => Fx == null;

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }

    void Destroy()
    {
        FxParameters.Clear();
        Mem.Dispose(ref Fx);
    }

    public EffectParameter this[string name] =>
        FxParameters.TryGetValue(name, out EffectParameter p) ? p : null;

    public EffectTechnique CurrentTechnique => Fx?.CurrentTechnique;

    public class IncludeHandler
    {
        public string LocalDir { get; set; }
        public IncludeHandler(string rootDir)
        {
            LocalDir = rootDir;
        }
    }

    public static IncludeHandler CreateIncludeHandler(string pathToShader)
    {
        string rootDir = Path.GetDirectoryName(pathToShader);
        return new(rootDir);
    }

    public static Shader FromFile(GraphicsDevice device, string pathToShader)
    {
        // TODO Phase 2: load precompiled MGFX effect (e.g. via ContentManager.Load<Effect>).
        // For Phase 1 the runtime HLSL compilation path is gone; return null so callers
        // that can degrade gracefully (e.g. SpriteRenderer) keep the game loop alive.
        System.Diagnostics.Debug.WriteLine($"Shader.FromFile({pathToShader}): runtime HLSL compilation removed in MonoGame; returning null. Restore via MGFX in Phase 2");
        return null;
    }

    public void Begin()
    {
        // TODO Phase 2: replace XNA Effect.Begin/End with EffectPass.Apply()
    }

    public void End()
    {
        // TODO Phase 2: replace XNA Effect.Begin/End with EffectPass.Apply()
    }
}
