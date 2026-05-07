using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 3.10.B.5 close-out: verify SkinnedEffect.mgfxo loaded, exposes
/// the matrix-palette parameter the renderer (B.6) will push, AND retains
/// the full LightingEffect parameter surface so the binder can target it
/// the same way it targets the static MeshLighting effect.
/// </summary>
[TestClass]
public class SkinnedLightingEffectTests : StarDriveTest
{
    [TestMethod]
    public void SkinnedLightingEffect_ExposesBonesParameter()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        Assert.IsNotNull(fx.Parameters["Bones"], "SkinnedEffect.mgfxo missing the Bones[] palette parameter");
    }

    [TestMethod]
    public void SkinnedLightingEffect_PreservesLightingEffectSurface()
    {
        // Renderer (B.6) wires SkinnedLightingEffect through the same
        // LightingEffectBinder.Apply path it uses for LightingEffect, so the
        // material + light + shadow uniforms must survive. Mirrors the
        // ShadowMapTests guard for MeshLighting.mgfxo.
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        Assert.IsNotNull(fx.Parameters["World"]);
        Assert.IsNotNull(fx.Parameters["View"]);
        Assert.IsNotNull(fx.Parameters["Projection"]);
        Assert.IsNotNull(fx.Parameters["DiffuseColor"]);
        Assert.IsNotNull(fx.Parameters["AmbientLightColor"]);
        Assert.IsNotNull(fx.Parameters["DirLight0Direction"]);
        Assert.IsNotNull(fx.Parameters["PointLight0PositionAndRadius"]);
        Assert.IsNotNull(fx.Parameters["DynamicLight0PositionAndRadius"]);
        Assert.IsNotNull(fx.Parameters["ShadowParams"], "ShadowParams missing — receiver shadow path won't apply on skinned hulls");
        Assert.IsNotNull(fx.Parameters["ShadowMap"]);
        Assert.IsNotNull(fx.Parameters["LightViewProjection"]);
    }

    [TestMethod]
    public void SkinnedLightingEffect_SetBoneTransforms_PushesPalette()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);

        var palette = new Matrix[3];
        palette[0] = Matrix.Identity;
        palette[1] = Matrix.CreateTranslation(1f, 2f, 3f);
        palette[2] = Matrix.CreateScale(2f);

        fx.SetBoneTransforms(palette); // must not throw
    }

    [TestMethod]
    public void SkinnedLightingEffect_SetBoneTransforms_TruncatesOversizedPalette()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);

        // A future mod with 200 deformer bones would overflow the shader's
        // Bones[64] array; SetBoneTransforms silently clamps to the cap
        // rather than throwing inside MonoGame's SetValue.
        var palette = new Matrix[200];
        for (int i = 0; i < palette.Length; i++) palette[i] = Matrix.Identity;
        fx.SetBoneTransforms(palette); // must not throw
    }

    [TestMethod]
    public void SkinnedLightingEffect_NullOrEmptyPalette_IsSafeNoOp()
    {
        GraphicsDevice device = Game.GraphicsDevice;
        using var fx = new SkinnedLightingEffect(device);
        fx.SetBoneTransforms(null);
        fx.SetBoneTransforms(new Matrix[0]);
    }
}
