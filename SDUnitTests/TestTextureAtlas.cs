using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;
using Ship_Game;

namespace SDUnitTests
{
    public class DummyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            Console.WriteLine($"Dummy::GetService {serviceType.Name}");
            return null;
        }
    }

    [TestFixture]
    public class TestTextureAtlas
    {
        public TestTextureAtlas()
        {
        }

        [Test]
        public void CreateAtlasFromFolder()
        {
            string path = Path.GetDirectoryName(GetType().Assembly.Location);
            Directory.SetCurrentDirectory(path);
            var content = new GameContentManager(new DummyServiceProvider(), "UnitTest");

            FileInfo[] files = ResourceManager.GatherFilesUnified("Textures/Buildings", "xnb");
            foreach (FileInfo file in files)
                TestContext.Out.WriteLine($"file: {file.RelPath()}");

            Texture2D test = content.Load<Texture2D>(files[0].RelPathNoExt());

            TextureAtlas atlas = TextureAtlas.FromFolder(content, "Textures/Buildings");

            TestContext.Out.WriteLine("What!");

            content.Dispose();
        }
    }
}
