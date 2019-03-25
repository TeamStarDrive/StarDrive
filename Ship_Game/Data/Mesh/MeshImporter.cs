using System;
using System.IO;
using SgMotion;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh
{
    public class MeshImporter : MeshInterface
    {
        public MeshImporter(GameContentManager content) : base(content)
        {
        }
        
        public unsafe StaticMesh Import(string meshPath, string meshName)
        {
            SdMesh* mesh = null;
            try
            {
                mesh = SDMeshOpen(meshPath);
                if (mesh == null)
                {
                    if (!File.Exists(meshPath))
                        throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the file does not exist!");
                    throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the data format is invalid!");
                }

                Log.Info(ConsoleColor.Green, $"SDStaticMesh {mesh->Name} | faces:{mesh->NumFaces} | groups:{mesh->NumGroups}");

                return LoadMeshGroups(mesh, meshName);
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to load mesh '{meshName}'");
                throw;
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        SkinnedModel SkinnedMeshFromFile(string modelName)
        {
            return null;
        }
        
        unsafe Map<long, LightingEffect> LoadMaterials(SdMesh* mesh, string modelName)
        {
            var materials = new Map<long, LightingEffect>();
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;
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

        unsafe StaticMesh LoadMeshGroups(SdMesh* mesh, string modelName)
        {
            var staticMesh = new StaticMesh { Name = mesh->Name.AsString };
            Map<long, LightingEffect> materials = LoadMaterials(mesh, modelName);

            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;

                Log.Info(ConsoleColor.Green, $"  group {g->GroupId}: {g->Name}  verts:{g->NumVertices}  ids:{g->NumIndices}");

                var meshData = new MeshData
                {
                    Name                      = g->Name.AsString,
                    Effect                    = materials[(long)g->Mat],
                    ObjectSpaceBoundingSphere = g->Bounds,
                    IndexBuffer               = g->CopyIndices(Device),
                    VertexBuffer              = g->CopyVertices(Device),
                    VertexDeclaration         = VertexLayout(Device),
                    PrimitiveCount            = g->NumTriangles,
                    VertexCount               = g->NumVertices,
                    VertexStride              = sizeof(SdVertex)
                };
                staticMesh.Meshes.Add(meshData);
            }
            return staticMesh;
        }
    }
}
