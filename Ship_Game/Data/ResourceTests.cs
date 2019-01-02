using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    public class ResourceTests
    {
        public ResourceTests()
        {

        }

        public void RunAll()
        {
            Log.Info("=== Running Resource Tests ===");
            TestTextureAtlas();
            Log.Info("=== Finished running Resource Tests ===");
        }

        void TestTextureAtlas()
        {
            TextureAtlas atlas = TextureAtlas.CreateFromFolder(
                ResourceManager.RootContent, "Textures/Modules");


        }
    }
}
