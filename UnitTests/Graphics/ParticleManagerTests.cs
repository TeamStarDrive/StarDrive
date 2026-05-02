using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Graphics.Particles;

namespace UnitTests.Graphics;

/// <summary>
/// Phase 2.9.A regression net: prove ParticleManager.Reload() exercises the full
/// load path successfully — yaml parse (Particles.yaml + ParticleEffects.yaml),
/// MGFX compile (3DParticles/ParticleEffect.mgfx via ps.GetEffect), and all 27
/// named-template lookups in ByName.
///
/// IsUnitTest=true forces ParticleSettings.MaxParticles=0 inside
/// LoadParticleSettings, which causes Particle's ctor to early-return before
/// LoadParticleEffect (Particle.cs:73). But ps.GetEffect runs *before* that
/// short-circuit, so the MGFX compile is still on the load path. Catches:
///   - Particles.yaml schema drift (a name removed)
///   - ParticleManager.Reload property assignments drifting from yaml names
///   - mgfxc-compiled ParticleEffect.mgfx failing to load (e.g. version mismatch
///     after a MonoGame upgrade)
///   - 3DParticles/ texture or shader assets missing from Content
/// </summary>
[TestClass]
public class ParticleManagerTests : StarDriveTest
{
    [TestMethod]
    public void Reload_PopulatesAllNamedTemplates()
    {
        using var manager = new ParticleManager(Content);

        // The 27 named IParticle properties on ParticleManager must all resolve.
        Assert.IsNotNull(manager.BeamFlash, nameof(manager.BeamFlash));
        Assert.IsNotNull(manager.Explosion, nameof(manager.Explosion));
        Assert.IsNotNull(manager.PhotonExplosion, nameof(manager.PhotonExplosion));
        Assert.IsNotNull(manager.ExplosionSmoke, nameof(manager.ExplosionSmoke));
        Assert.IsNotNull(manager.ProjectileTrail, nameof(manager.ProjectileTrail));
        Assert.IsNotNull(manager.JunkSmoke, nameof(manager.JunkSmoke));
        Assert.IsNotNull(manager.FireTrail, nameof(manager.FireTrail));
        Assert.IsNotNull(manager.MissileSmokeTrail, nameof(manager.MissileSmokeTrail));
        Assert.IsNotNull(manager.SmokePlume, nameof(manager.SmokePlume));
        Assert.IsNotNull(manager.Fire, nameof(manager.Fire));
        Assert.IsNotNull(manager.ThrustEffect, nameof(manager.ThrustEffect));
        Assert.IsNotNull(manager.EngineTrail, nameof(manager.EngineTrail));
        Assert.IsNotNull(manager.Flame, nameof(manager.Flame));
        Assert.IsNotNull(manager.SmallFire, nameof(manager.SmallFire));
        Assert.IsNotNull(manager.Sparks, nameof(manager.Sparks));
        Assert.IsNotNull(manager.Lightning, nameof(manager.Lightning));
        Assert.IsNotNull(manager.Flash, nameof(manager.Flash));
        Assert.IsNotNull(manager.StarParticles, nameof(manager.StarParticles));
        Assert.IsNotNull(manager.Galaxy, nameof(manager.Galaxy));
        Assert.IsNotNull(manager.AsteroidParticles, nameof(manager.AsteroidParticles));
        Assert.IsNotNull(manager.MissileThrustFlare, nameof(manager.MissileThrustFlare));
        Assert.IsNotNull(manager.IonTrail, nameof(manager.IonTrail));
        Assert.IsNotNull(manager.BlueSparks, nameof(manager.BlueSparks));
        Assert.IsNotNull(manager.ModuleSmoke, nameof(manager.ModuleSmoke));
        Assert.IsNotNull(manager.IonRing, nameof(manager.IonRing));
        Assert.IsNotNull(manager.IonRingReversed, nameof(manager.IonRingReversed));
        Assert.IsNotNull(manager.Bubble, nameof(manager.Bubble));

        // No particle system should be missing from the tracked list.
        Assert.IsTrue(manager.ParticleSystems.Count >= 27,
            $"Expected at least 27 tracked systems, got {manager.ParticleSystems.Count}. " +
            "Audit Particles.yaml against ParticleManager.Reload() property assignments.");
    }
}
