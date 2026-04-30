using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game.Data.Mesh
{
    // TODO Phase 2: SDNative mesh import disabled in Phase 1.
    // Phase 1 excludes SDSunBurn from the solution and stubs SunBurn types;
    // XNAnimation was removed in 1.9 so SkinnedModel paths are gone too.
    public class MeshImporter : MeshInterface
    {
        public MeshImporter(GameContentManager content) : base(content)
        {
        }

        public StaticMesh ImportStaticMesh(string meshPath, string meshName)
        {
            Log.Warning($"Phase 1: ImportStaticMesh disabled, returning null for '{meshName}'");
            return null;
        }

        public Model ImportModel(string meshPath, string meshName)
        {
            Log.Warning($"Phase 1: ImportModel disabled, returning null for '{meshName}'");
            return null;
        }
    }
}
