using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Graphics.Particles;

namespace UnitTests.Graphics;

[TestClass]
public class ParticleVertexBufferTests : StarDriveTest
{
    [TestMethod]
    public void AParticleStampedLaterThanItsPoolsTimeIsRetiredAndHoldsUpNoneBehindIt()
    {
        using var shared = new ParticleVertexBufferShared(Content.Device);
        using var buffer = new ParticleVertexBuffer(shared);
        buffer.Add(new ParticleVertex { Time = 900f }); // stamped by the ship design screen's clock, ahead of the universe's
        buffer.Add(new ParticleVertex { Time = 100f });
        buffer.Update(totalSimulationTime: 100.1f, particleDuration: 0.3f, isStatic: false);
        Assert.AreEqual(1, buffer.ActiveParticles, "the one from the later clock retired, the one in its life kept");
        buffer.Update(totalSimulationTime: 100.5f, particleDuration: 0.3f, isStatic: false);
        Assert.AreEqual(0, buffer.ActiveParticles, "and that one too once its life is over");
    }

    [TestMethod]
    public void AParticleAddedWhileItsPoolIsEmptyLivesItsLife()
    {
        ParticleSettings settings = null;
        foreach (ParticleSettings s in Ship_Game.Data.Yaml.YamlParser.DeserializeArray<ParticleSettings>(new System.IO.FileInfo("Content/3DParticles/Particles.yaml")))
            if (s.Name == "Flash")
                settings = s;
        Assert.IsNotNull(settings, "setup: the Flash particle's settings");
        settings.MaxParticles = 10; // the particle manager switches every pool off in unit tests
        using var manager = new ParticleManager(Content);
        using var flash = new Particle(manager, Content, settings, id: 0);

        flash.Update(1000f); // a game long under way, the pool empty
        flash.AddParticle(SDGraphics.Vector3.Zero); // as the sim thread adds one before the pool's next update
        flash.Update(1000.1f);
        flash.Update(1000.2f);
        Assert.AreEqual(1, flash.ActiveParticles, "it takes the pool's time, so 0.2 s into its 0.3 s life it still shows");
    }
}