using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Rendering;

namespace UnitTests.Data;

/// <summary>
/// Phase 2.8.C smoke signal: prove the un-stubbed SDNative-backed OBJ load path
/// produces a non-empty StaticMesh with valid VertexBuffer / IndexBuffer / declaration.
/// Uses planet_sphere.obj — one of the four .obj assets shipped in Content. Catches
/// regressions in:
///   - SdVertexData.CopyIndices/CopyVertices/CreateDeclaration (un-stubbed in this sub-phase)
///   - SDNative SDMeshOpen for OBJ in x64
///   - MeshImporter.LoadMeshGroups assembly path (material map, bounds merge, group iter)
/// FBX import is still gated by the SDK 2018→2020 ABI fix; covered by a separate
/// test once §2.10 re-enables it.
/// </summary>
[TestClass]
public class MeshImporterTests : StarDriveTest
{
    [TestMethod]
    public void ImportStaticMesh_PlanetSphereObj_HasNonZeroGeometry()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/SpaceObjects/planet_sphere.obj");

        Assert.IsNotNull(mesh, "LoadStaticMesh returned null");
        Assert.IsFalse(mesh.RawMeshes.IsEmpty,
            "Expected at least one RawMesh group; got empty — SDMeshOpen likely failed or LoadMeshGroups skipped all groups.");

        int totalVerts = 0, totalPrims = 0;
        foreach (MeshData md in mesh.RawMeshes)
        {
            Assert.IsNotNull(md.VertexBuffer, $"group '{md.Name}': VertexBuffer is null");
            Assert.IsNotNull(md.IndexBuffer, $"group '{md.Name}': IndexBuffer is null");
            Assert.IsNotNull(md.VertexDeclaration, $"group '{md.Name}': VertexDeclaration is null");
            Assert.IsTrue(md.VertexCount > 0, $"group '{md.Name}': VertexCount={md.VertexCount}");
            Assert.IsTrue(md.PrimitiveCount > 0, $"group '{md.Name}': PrimitiveCount={md.PrimitiveCount}");
            totalVerts += md.VertexCount;
            totalPrims += md.PrimitiveCount;
        }

        // A UV sphere typically has hundreds of verts and a few hundred triangles.
        // 50/50 is a generous floor — anything less suggests the OBJ parser saw a
        // broken file or the buffer-copy methods produced empty buffers.
        Assert.IsTrue(totalVerts >= 50, $"Total verts={totalVerts}, expected >=50");
        Assert.IsTrue(totalPrims >= 50, $"Total prims={totalPrims}, expected >=50");
    }
}
