using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game.Data.Mesh
{
    // TODO Phase 2: SDNative mesh export disabled in Phase 1.
    // Phase 1 excludes SDSunBurn from the solution and stubs SunBurn types;
    // XNAnimation was removed in 1.9 so SkinnedModel paths are gone too.
    public class MeshExporter : MeshInterface
    {
        public MeshExporter(GameContentManager content) : base(content)
        {
        }

        public void Reset()
        {
        }

        public bool Export(Model model, string name, string modelFilePath)
        {
            Log.Warning($"Phase 1: MeshExporter.Export(Model) disabled for '{name}'");
            return false;
        }

        public bool IsAlreadySavedTexture(Texture2D tex) => false;

        public void AddAlreadySavedTexture(Texture2D tex, string texSavePath)
        {
        }
    }
}
