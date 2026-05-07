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
///
/// Phase 3.2 extension: asteroid .fbx tests now also exercise the FBX 2020.3.7 SDK path
/// (un-stubbed by dropping NANOMESH_NO_FBX=1). Catches regressions in Mesh_Fbx.cpp's
/// FBX SDK use, the SDNative-side FbxArray ABI alignment, and the .fbx → SDMeshOpen
/// extension routing in RawContentLoader.
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

    // Phase 3.2: ResourceManager.LoadAsteroids() walks the Asteroids folder for
    // .xnb files and calls LoadStaticMesh with the .xnb path. With the XNB Model
    // pipeline still stubbed (§3.4 work), the .xnb→.fbx sibling fallback in
    // GameContentManager.LoadStaticMesh is what lets asteroids render. Pin it.
    [TestMethod]
    public void LoadStaticMesh_AsteroidXnb_FallsBackToFbxSibling()
    {
        StaticMesh mesh = Content.LoadStaticMesh("Model/Asteroids/asteroid1.xnb");
        Assert.IsNotNull(mesh);
        Assert.IsFalse(mesh.RawMeshes.IsEmpty,
            "Asking for asteroid1.xnb should fall back to asteroid1.fbx and load real geometry; " +
            "got an empty mesh, meaning the .xnb→.fbx fallback in LoadStaticMesh is broken.");
    }

    [TestMethod]
    [DataRow("Model/Asteroids/asteroid1.fbx")]
    [DataRow("Model/Asteroids/asteroid2.fbx")]
    [DataRow("Model/Asteroids/asteroid3.fbx")]
    [DataRow("Model/Asteroids/asteroid4.fbx")]
    [DataRow("Model/Asteroids/asteroid5.fbx")]
    [DataRow("Model/Asteroids/asteroid6.fbx")]
    [DataRow("Model/Asteroids/asteroid7.fbx")]
    [DataRow("Model/Asteroids/asteroid8.fbx")]
    [DataRow("Model/Asteroids/asteroid9.fbx")]
    public void ImportStaticMesh_AsteroidFbx_HasNonZeroGeometry(string assetPath)
    {
        StaticMesh mesh = Content.LoadStaticMesh(assetPath);

        Assert.IsNotNull(mesh, $"LoadStaticMesh returned null for '{assetPath}'");
        Assert.IsFalse(mesh.RawMeshes.IsEmpty,
            $"'{assetPath}': RawMeshes empty — SDMeshOpen likely returned null. " +
            $"Indicates FBX SDK 2020 lib/header mismatch or Mesh_Fbx.cpp regression.");

        int totalVerts = 0, totalPrims = 0;
        foreach (MeshData md in mesh.RawMeshes)
        {
            Assert.IsNotNull(md.VertexBuffer, $"'{assetPath}' group '{md.Name}': VertexBuffer is null");
            Assert.IsNotNull(md.IndexBuffer, $"'{assetPath}' group '{md.Name}': IndexBuffer is null");
            Assert.IsTrue(md.VertexCount > 0, $"'{assetPath}' group '{md.Name}': VertexCount={md.VertexCount}");
            Assert.IsTrue(md.PrimitiveCount > 0, $"'{assetPath}' group '{md.Name}': PrimitiveCount={md.PrimitiveCount}");
            totalVerts += md.VertexCount;
            totalPrims += md.PrimitiveCount;
        }

        // Asteroid meshes are very low-poly rocks (smallest are ~44 prims).
        // 30 is a sanity floor against a parser regression that produces a
        // single-tri or degenerate mesh.
        Assert.IsTrue(totalVerts >= 30, $"'{assetPath}': total verts={totalVerts}");
        Assert.IsTrue(totalPrims >= 30, $"'{assetPath}': total prims={totalPrims}");
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
