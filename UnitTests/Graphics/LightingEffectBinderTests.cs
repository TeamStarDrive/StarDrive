using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Mesh;
using SunBurnLights = SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 2.8 sub-phase A5: assert that lights submitted to the SunBurn-stub
/// LightManager actually propagate onto the underlying BasicEffect's
/// DirectionalLight0/1/2 slots when LightingEffectBinder.Apply runs. This is
/// the data-flow assertion for the highest-risk part of the new renderer —
/// the new light → effect parameter binding.
/// </summary>
[TestClass]
public class LightingEffectBinderTests : StarDriveTest
{
    [TestMethod]
    public void SubmitDirectionalLight_PropagatesToBasicEffect()
    {
        var lightManager = new SunBurnLights.LightManager();
        var dir = new SunBurnLights.DirectionalLight
        {
            Name = "TestKey",
            Direction = new Vector3(0.5f, -0.5f, -0.5f),
            DiffuseColor = new Vector3(1f, 0.9f, 0.8f),
            Intensity = 1.5f,
            Enabled = true,
        };
        lightManager.Submit(dir);

        var env = new SceneEnvironment
        {
            AmbientLightColor = new Vector3(0.2f, 0.2f, 0.25f),
        };

        using var fx = new LightingEffect(Game.GraphicsDevice);
        LightingEffectBinder.Apply(fx, lightManager.ActiveLights, env, Vector3.Zero);

        Assert.IsTrue(fx.LightingEnabled, "LightingEnabled should be true once a directional light is submitted.");
        Assert.IsTrue(fx.DirectionalLight0.Enabled, "DirectionalLight0 should be enabled.");
        Vector3 expected = Vector3.Normalize(new Vector3(0.5f, -0.5f, -0.5f));
        Vector3 actual = fx.DirectionalLight0.Direction;
        Assert.AreEqual(expected.X, actual.X, 0.001f, "Direction.X mismatch");
        Assert.AreEqual(expected.Y, actual.Y, 0.001f, "Direction.Y mismatch");
        Assert.AreEqual(expected.Z, actual.Z, 0.001f, "Direction.Z mismatch");

        Vector3 expectedDiffuse = new Vector3(1f, 0.9f, 0.8f) * 1.5f;
        Assert.AreEqual(expectedDiffuse.X, fx.DirectionalLight0.DiffuseColor.X, 0.001f, "DiffuseColor.X mismatch");
        Assert.AreEqual(expectedDiffuse.Y, fx.DirectionalLight0.DiffuseColor.Y, 0.001f, "DiffuseColor.Y mismatch");
        Assert.AreEqual(expectedDiffuse.Z, fx.DirectionalLight0.DiffuseColor.Z, 0.001f, "DiffuseColor.Z mismatch");

        Assert.IsFalse(fx.DirectionalLight1.Enabled, "DirectionalLight1 should be disabled (only 1 light submitted).");
        Assert.IsFalse(fx.DirectionalLight2.Enabled, "DirectionalLight2 should be disabled (only 1 light submitted).");

        Assert.AreEqual(env.AmbientLightColor.X, fx.AmbientLightColor.X, 0.001f, "Ambient should match SceneEnvironment.AmbientLightColor.");
    }

    [TestMethod]
    public void NoLightsSubmitted_LightingDisabled()
    {
        var lightManager = new SunBurnLights.LightManager();
        var env = new SceneEnvironment { AmbientLightColor = Vector3.Zero };
        using var fx = new LightingEffect(Game.GraphicsDevice);
        LightingEffectBinder.Apply(fx, lightManager.ActiveLights, env, Vector3.Zero);

        Assert.IsFalse(fx.LightingEnabled, "LightingEnabled should be false when no lights and zero ambient.");
        Assert.IsFalse(fx.DirectionalLight0.Enabled);
        Assert.IsFalse(fx.DirectionalLight1.Enabled);
        Assert.IsFalse(fx.DirectionalLight2.Enabled);
    }
}
