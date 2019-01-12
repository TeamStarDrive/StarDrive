using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests
{
    [TestClass]
    public class TestTextureAtlas : IDisposable
    {
        readonly GameDummy Game;

        public TestTextureAtlas()
        {
            Directory.SetCurrentDirectory("/Projects/BlackBox/StarDrive");
            Game = new GameDummy();
            Game.Create();
        }

        public void Dispose()
        {
            Game?.Dispose();
        }

        [TestMethod]
        public void TextureAtlasFromFolder()
        {
            TextureAtlas.FromFolder(Game.Content, "Textures/sd_shockwave_01", false);
            TextureAtlas.FromFolder(Game.Content, "Textures", false);
            TextureAtlas.FromFolder(Game.Content, "Textures/Modules", false);
            TextureAtlas.FromFolder(Game.Content, "Textures/Conduits", false);
        }

        [TestMethod]
        public void ContentManager()
        {
            var nonExistent = Game.Content.Load<SubTexture>("TestForAtlasDoesNotExist");
            Assert.AreEqual(nonExistent, null, "nonExistent SubTexture must be null");

            var reactor = Game.Content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Assert.AreNotEqual(reactor, null, "reactor != null");

            reactor = Game.Content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Assert.AreNotEqual(reactor, null, "reactor != null");
        }
    }
}
