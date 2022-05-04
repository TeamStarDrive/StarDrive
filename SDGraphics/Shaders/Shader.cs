using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace SDGraphics.Shaders
{
    public class Shader : IDisposable
    {
        Effect Fx;

        Shader(Effect fx)
        {
            Fx = fx;
        }

        ~Shader()
        {
            Memory.Dispose(ref Fx);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Memory.Dispose(ref Fx);
        }

        public EffectParameter this[string name] => Fx.Parameters[name];
        public EffectTechnique CurrentTechnique => Fx.CurrentTechnique;

        public static Shader FromFile(GraphicsDevice device, string pathToShader)
        {
            string sourceCode = File.ReadAllText(pathToShader);

            CompiledEffect compiled = Effect.CompileEffectFromSource(sourceCode, new CompilerMacro[0], null,
                                                                     CompilerOptions.None, TargetPlatform.Windows);
            if (!compiled.Success)
            {
                throw new Exception($"Shader.FromFile {pathToShader} failed: {compiled.ErrorsAndWarnings}");
            }

            var fx = new Effect(device, compiled.GetEffectCode(), CompilerOptions.None, null);
            return new Shader(fx);
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
}
