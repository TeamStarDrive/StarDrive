using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
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

    [TestMethod]
    public void ImportStaticMesh_PlanetSphereObj_VertexDeclarationUsagesAreMonoGameValues()
    {
        // Regression net for the SDNative-byte → MonoGame-enum translation in
        // SdVertexData.CreateDeclaration. SDNative writes XNA-3.1-ordinal bytes
        // (Position=0, Normal=3, Coordinate=5) into NativeUsage; passing those
        // unchanged into MG's VertexElement crashed the universe-screen draw
        // with "Unknown vertex element usage!" from PlatformApplyState (because
        // SDElementUsage::Sample=13 is out of MG's enum range entirely, and
        // even valid bytes mean the wrong semantic — byte 5 is Tangent in MG,
        // not TextureCoordinate).
        StaticMesh mesh = Content.LoadStaticMesh("Model/SpaceObjects/planet_sphere.obj");
        Assert.IsFalse(mesh.RawMeshes.IsEmpty);

        var seenUsages = new HashSet<VertexElementUsage>();
        foreach (MeshData md in mesh.RawMeshes)
        {
            foreach (VertexElement e in md.VertexDeclaration.GetVertexElements())
            {
                seenUsages.Add(e.VertexElementUsage);
            }
        }

        // OBJ-imported geometry must carry at least Position; planet_sphere.obj
        // ships with Normal + TextureCoordinate too.
        Assert.IsTrue(seenUsages.Contains(VertexElementUsage.Position),
            $"Missing Position usage. Saw: {string.Join(",", seenUsages)}");
        Assert.IsTrue(seenUsages.Contains(VertexElementUsage.Normal),
            $"Missing Normal usage. Saw: {string.Join(",", seenUsages)}");
        Assert.IsTrue(seenUsages.Contains(VertexElementUsage.TextureCoordinate),
            $"Missing TextureCoordinate usage — translation may have left native byte 5 " +
            $"as Tangent. Saw: {string.Join(",", seenUsages)}");
    }
}
