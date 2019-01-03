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
        public void RunAll()
        {
            Log.Info("=== Running Resource Tests ===");
            TestTextureAtlas();
            TestLoadAllAtlases();
            TestContentManager();
            Log.Info("=== Finished running Resource Tests ===");
        }

        void TestTextureAtlas()
        {
            TextureAtlas.FromFolder(ResourceManager.RootContent, "Textures", false);
            TextureAtlas.FromFolder(ResourceManager.RootContent, "Textures/Modules", false);
            TextureAtlas.FromFolder(ResourceManager.RootContent, "Textures/Conduits", false);
        }

        void TestLoadAllAtlases()
        {
            ResourceManager.LoadTextureAtlases();
        }

        void TestContentManager()
        {
            GameContentManager content = ResourceManager.RootContent;

            var nonExistent = content.Load<SubTexture>("TestForAtlasDoesNotExist");
            Log.Assert(nonExistent == null, "nonExistent SubTexture must be null");

            var reactor = content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Log.Assert(reactor != null, "reactor != null");

            reactor = content.Load<SubTexture>("Textures/Modules/AncientReactorMed");
            Log.Assert(reactor != null, "reactor != null");
        }
    }
}
