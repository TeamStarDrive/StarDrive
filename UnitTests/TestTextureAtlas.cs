using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.SpriteSystem;

namespace UnitTests
{
    [TestClass]
    public class TestTextureAtlas : StarDriveTest
    {
        public TestTextureAtlas()
        {
            CreateGameInstance();
        }

        [TestMethod]
        public void TextureAtlasFromFolder()
        {
            TextureAtlas.FromFolder("Textures", false);
            TextureAtlas.FromFolder("Textures/Modules", false);
            TextureAtlas.FromFolder("Textures/Conduits", false);
        }

        [TestMethod]
        public void ContentManager()
        {
            var nonExistent = Content.Load<SubTexture>("TestForAtlasDoesNotExist");
            Assert.AreEqual(nonExistent, null, "nonExistent SubTexture must be null");

            var reactor = Content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Assert.AreNotEqual(reactor, null, "reactor != null");

            reactor = Content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Assert.AreNotEqual(reactor, null, "reactor != null");
        }
    }
}
