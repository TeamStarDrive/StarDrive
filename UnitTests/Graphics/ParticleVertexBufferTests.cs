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
}