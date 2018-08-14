using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public class StaticMesh
    {
        public string Name { get; set; }
        public Array<MeshData> Meshes { get; set; } = new Array<MeshData>();
        public int Count => Meshes.Count;
    }
}
