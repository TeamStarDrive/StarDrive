using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;
using XnaBoundingBox = Microsoft.Xna.Framework.BoundingBox;

namespace Ship_Game.Data.Mesh
{
    // Phase 2.8.C: restored OBJ/FBX runtime path. SDNative SDMeshOpen handles both
    // (NanoMesh underneath). FBX is still gated by the SDK 2018→2020 ABI fix in x64
    // (project_phase2_backlog_fbx.md); .obj works today and unblocks planet/asteroid
    // rendering. Hull/ship XNB stubs remain — see Phase 2.8 close-out doc.
    public class MeshImporter : MeshInterface
    {
        // Synthetic unit-sphere bounding box used when SDMeshOpen returns null
        // (file missing or unrecognized format) so callers reading Radius/Bounds
        // don't NRE while the geometry path stays a graceful no-op.
        static readonly XnaBoundingBox StubBounds = new(-Vector3.One, Vector3.One);

        public MeshImporter(GameContentManager content) : base(content)
        {
        }

        public unsafe StaticMesh ImportStaticMesh(string meshPath, string meshName)
        {
            SdMesh* mesh = null;
            try
            {
                mesh = SDMeshOpen(meshPath);
                if (mesh == null)
                {
                    if (!File.Exists(meshPath))
                        Log.Warning($"ImportStaticMesh '{meshName}': file not found at '{meshPath}'");
                    else
                        Log.Warning($"ImportStaticMesh '{meshName}': SDMeshOpen returned null (unsupported format?)");
                    return new StaticMesh(meshName, StubBounds);
                }

                Log.Info(ConsoleColor.Green,
                    $"StaticMesh {mesh->Name.AsString} | faces:{mesh->NumFaces} | groups:{mesh->NumGroups}");
                return LoadMeshGroups(mesh, meshName);
            }
            catch (Exception e)
            {
                Log.Error(e, $"ImportStaticMesh '{meshName}' failed; returning empty mesh");
                return new StaticMesh(meshName, StubBounds);
            }
            finally
            {
                if (mesh != null) SDMeshClose(mesh);
            }
        }

        public Model ImportModel(string meshPath, string meshName)
        {
            // Phase 2.8.C: ImportModel restoration deferred. The XNA Model API requires
            // ModelMesh/ModelBone construction via reflection (private ctors); the legacy
            // path also pulled in SgMotion/SkinnedModel. Callers that need StaticMesh
            // already use ImportStaticMesh; nothing in §2.8.C scope hits this path.
            Log.Warning($"Phase 2 stub: ImportModel disabled, returning null for '{meshName}'");
            return null;
        }

        unsafe StaticMesh LoadMeshGroups(SdMesh* mesh, string modelName)
        {
            Map<long, LightingEffect> materials = GetMaterials(mesh, modelName);

            var rawMeshes = new Array<MeshData>();
            XnaBoundingBox bounds = default;

            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                SdVertexData data = SDMeshGroupGetData(g);
                if (data.VertexCount == 0 || data.IndexCount == 0)
                    continue;

                VertexDeclaration declaration = data.CreateDeclaration();
                rawMeshes.Add(new MeshData
                {
                    Name              = g->Name.AsString,
                    Effect            = materials[(long)g->Mat],
                    IndexBuffer       = data.CopyIndices(Device),
                    VertexBuffer      = data.CopyVertices(Device, declaration),
                    VertexDeclaration = declaration,
                    PrimitiveCount    = data.IndexCount / 3,
                    VertexCount       = data.VertexCount,
                    VertexStride      = data.VertexStride,
                    ObjectSpaceBoundingSphere = g->Bounds,
                });

                var bb = XnaBoundingBox.CreateFromSphere(g->Bounds);
                if (g->Transform != Matrix.Identity)
                {
                    bb.Min = Vector3.Transform(bb.Min, g->Transform);
                    bb.Max = Vector3.Transform(bb.Max, g->Transform);
                }
                bounds = bounds == default ? bb : XnaBoundingBox.CreateMerged(bounds, bb);
            }

            return new StaticMesh(mesh->Name.AsString, bounds)
            {
                RawMeshes = rawMeshes
            };
        }

        unsafe Map<long, LightingEffect> GetMaterials(SdMesh* mesh, string modelName)
        {
            var materials = new Map<long, LightingEffect>();
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                long ptr = (long)g->Mat;
                if (!materials.ContainsKey(ptr))
                {
                    materials[ptr] = (ptr == 0)
                        ? new LightingEffect(Device)
                        : CreateMaterialEffect(g->Mat, Device, Content, modelName);
                }
            }
            return materials;
        }
    }
}
