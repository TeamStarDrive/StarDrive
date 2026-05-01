using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game.Data.Mesh
{
    // TODO Phase 2: SDNative mesh import disabled in Phase 1.
    // Phase 1 excludes SDSunBurn from the solution and stubs SunBurn types;
    // XNAnimation was removed in 1.9 so SkinnedModel paths are gone too.
    public class MeshImporter : MeshInterface
    {
        // TODO Phase 2: synthetic unit-sphere bounding box used as a fallback for stubbed
        // mesh import so callers that read Radius/Bounds don't NRE. Geometry and Draw are
        // no-ops via StaticMesh's Phase 1 stubs. Restore real bounds once SDNative mesh
        // import is re-enabled (see project_phase2_backlog_runtime.md priority #6).
        static readonly BoundingBox StubBounds = new(-Vector3.One, Vector3.One);

        public MeshImporter(GameContentManager content) : base(content)
        {
        }

        public StaticMesh ImportStaticMesh(string meshPath, string meshName)
        {
            Log.Warning($"Phase 2 stub: ImportStaticMesh disabled, returning empty mesh for '{meshName}'");
            return new StaticMesh(meshName, StubBounds);
        }

        public Model ImportModel(string meshPath, string meshName)
        {
            Log.Warning($"Phase 2 stub: ImportModel disabled, returning null for '{meshName}'");
            return null;
        }
    }
}
