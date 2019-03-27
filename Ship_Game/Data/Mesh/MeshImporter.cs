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
                SdVertexData data = SDMeshGroupGetData(g);
                if (data.VertexCount == 0 || data.IndexCount == 0)
                    continue;

                Log.Info(ConsoleColor.Green,
                    $"  group {g->GroupId}: {g->Name}  verts:{data.VertexCount}  ids:{data.IndexCount}");
                
                var meshData = new MeshData
                {
                    Name              = g->Name.AsString,
                    Effect            = materials[(long)g->Mat],
                    IndexBuffer       = data.CopyIndices(Device),
                    VertexBuffer      = data.CopyVertices(Device),
                    VertexDeclaration = data.CreateDeclaration(Device),
                    PrimitiveCount    = data.IndexCount/3,
                    VertexCount       = data.VertexCount,
                    VertexStride      = data.VertexStride,
                    ObjectSpaceBoundingSphere = g->Bounds,
                };
                staticMesh.Meshes.Add(meshData);
            }
            return staticMesh;
        }
    }
}
