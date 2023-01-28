using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics.Shaders;

namespace SDGraphics.Sprites;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

/// <summary>
/// Wrapper around a SpriteRenderer compatible Shader
/// </summary>
public class SpriteShader : IDisposable
{
    public readonly Shader Shader;
    public readonly EffectPass ShaderPass;

    readonly EffectParameter ViewProjectionParam;
    readonly EffectParameter TextureParam;
    readonly EffectParameter UseTextureParam;
    readonly EffectParameter ColorParam;

    /// <summary>
    /// Wraps around an existing Shader, taking ownership of it,
    /// and Disposing it when this object is destroyed.
    /// </summary>
    /// <param name="shader">The SpriteRenderer compatible Shader to take ownership of</param>
    public SpriteShader(Shader shader)
    {
        Shader = shader ?? throw new NullReferenceException(nameof(shader));
        ViewProjectionParam = shader["ViewProjection"];
        TextureParam = shader["Texture"];
        UseTextureParam = shader["UseTexture"];
        ColorParam = shader["Color"];
        ShaderPass = shader.CurrentTechnique.Passes[0];

        // set the defaults
        SetColor(Color.White);
    }

    public bool IsDisposed => Shader.IsDisposed;
    
    public void Dispose()
    {
        Shader.Dispose();
        TextureParamValue = null;
    }
    
    [Conditional("DEBUG")]
    static void CheckTextureDisposed(Texture2D texture)
    {
        if (texture is { IsDisposed: true })
            throw new ObjectDisposedException($"Texture2D '{texture.Name}'");
    }

    bool UseTextureParamValue;

    public void SetUseTexture(bool useTexture)
    {
        if (UseTextureParamValue != useTexture)
        {
            UseTextureParamValue = useTexture;
            UseTextureParam.SetValue(useTexture);
        }
    }

    Color ColorParamValue;

    public void SetColor(Color color)
    {
        if (ColorParamValue != color)
        {
            ColorParamValue = color;
            ColorParam.SetValue(color.ToVector4());
        }
    }

    Texture2D TextureParamValue;

    public void SetTexture(Texture2D texture)
    {
        if (TextureParamValue != texture)
        {
            CheckTextureDisposed(texture);
            TextureParamValue = texture;
            TextureParam.SetValue(texture);
        }
    }

    Matrix ViewProjectionParamValue;

    public unsafe void SetViewProjection(in Matrix viewProjection)
    {
        if (ViewProjectionParamValue != viewProjection)
        {
            ViewProjectionParamValue = viewProjection;
            fixed (Matrix* pViewProjection = &ViewProjectionParamValue)
            {
                ViewProjectionParam.SetValue(*(XnaMatrix*)pViewProjection);
            }
        }
    }
}
