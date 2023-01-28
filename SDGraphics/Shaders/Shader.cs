using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace SDGraphics.Shaders;

public class Shader : IDisposable
{
    Effect Fx;
    readonly Map<string, EffectParameter> FxParameters;

    Shader(Effect fx)
    {
        Fx = fx;
        FxParameters = new();
        foreach (EffectParameter parameter in Fx.Parameters)
        {
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

    public EffectParameter this[string name] => FxParameters[name];
    public EffectTechnique CurrentTechnique => Fx.CurrentTechnique;

    public class IncludeHandler : CompilerIncludeHandler
    {
        public string LocalDir { get; set; }
        public IncludeHandler(string rootDir)
        {
            LocalDir = rootDir;
        }
        public override Stream Open(CompilerIncludeHandlerType includeType, string filename)
        {
            // TODO: this isn't fully mod compatible
            string path = includeType == CompilerIncludeHandlerType.Local
                ? Path.Combine(LocalDir, filename) // for local includes: #include "Simple.fxh"
                : Path.Combine("Content", filename); // for game-global includes #include <Effects/Simple.fxh>
            return new FileInfo(path).OpenRead();
        }
    }

    /// <summary>
    /// Creates a new include handler, using `pathToShader` as the directory reference
    /// </summary>
    public static IncludeHandler CreateIncludeHandler(string pathToShader)
    {
        string rootDir = Path.GetDirectoryName(pathToShader);
        return new(rootDir);
    }

    public static Shader FromFile(GraphicsDevice device, string pathToShader)
    {
        string sourceCode = File.ReadAllText(pathToShader);
        IncludeHandler handler = CreateIncludeHandler(pathToShader);
        CompiledEffect compiled = Effect.CompileEffectFromSource(sourceCode, Empty<CompilerMacro>.Array, handler,
                                                                 CompilerOptions.None, TargetPlatform.Windows);
        if (!compiled.Success)
        {
            throw new($"Shader.FromFile {pathToShader} failed: {compiled.ErrorsAndWarnings}");
        }

        var fx = new Effect(device, compiled.GetEffectCode(), CompilerOptions.None, null);
        return new(fx);
    }

    public void Begin()
    {
        Fx.Begin();
    }

    public void End()
    {
        Fx.End();
    }
}