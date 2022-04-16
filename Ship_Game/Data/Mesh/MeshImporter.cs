using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        // Sunburn.MeshData wrapper StaticMesh
        public unsafe StaticMesh ImportStaticMesh(string meshPath, string meshName)
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

                Log.Info(ConsoleColor.Green, $"StaticMesh {mesh->Name} | faces:{mesh->NumFaces} | groups:{mesh->NumGroups}");
                return LoadMeshGroups(mesh, meshName);
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to load mesh '{meshName}'", 0);
                throw;
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        // XNA.Model
        public unsafe Model ImportModel(string meshPath, string meshName)
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

                //Log.Info(ConsoleColor.Green, $"Model {mesh->Name} | faces:{mesh->NumFaces} | groups:{mesh->NumGroups}");
                return LoadModelGroups(mesh, meshName);
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to load mesh '{meshName}'", 0);
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
        
        unsafe Map<long, LightingEffect> GetSunburnMaterials(SdMesh* mesh, string modelName)
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
            Map<long, LightingEffect> materials = GetSunburnMaterials(mesh, modelName);

            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                SdVertexData data = SDMeshGroupGetData(g);
                if (data.VertexCount == 0 || data.IndexCount == 0)
                    continue;

                //Log.Info(ConsoleColor.Green,
                //    $"  group {g->GroupId}: {g->Name}  verts:{data.VertexCount}  ids:{data.IndexCount}");
                
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

        ///////////////////////////////////////////////////////////////////////////

        unsafe ModelBone GetRootModelBone(SdMesh* mesh)
        {
            if (mesh->NumModelBones == 0)
            {
                return CreateModelBone(mesh->Name.AsString, Matrix.Identity, 0);
            }
            // TODO: load root bone from SdMesh
            return CreateModelBone(mesh->Name.AsString, Matrix.Identity, 0);
        }

        unsafe Model LoadModelGroups(SdMesh* mesh, string modelName)
        {
            var meshes = new Array<ModelMesh>();
            var model = new Model();
            ModelBone rootBone = GetRootModelBone(mesh);
            SetBones(model, new[] { rootBone });

            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                SdVertexData data = SDMeshGroupGetData(g);
                if (data.VertexCount == 0 || data.IndexCount == 0)
                    continue;

                //Log.Info(ConsoleColor.Green,
                //    $"  group {g->GroupId}: {g->Name}  verts:{data.VertexCount}  ids:{data.IndexCount}");

                var vertices = data.CopyVertices(Device);
                var indices = data.CopyIndices(Device);
                var declaration = data.CreateDeclaration(Device);

                ModelMesh meshData = CreateNewModelMesh(
                    g->Name.AsString, rootBone, g->Bounds, vertices, indices,
                    new [] { CreateModelMeshPart(0, 0, data.VertexCount, 0, data.IndexCount/3, vertices, indices, declaration) }
                );
                meshes.Add(meshData);
            }

            SetMeshes(model, meshes);
            return model;
        }

        void SetBones(Model model, ModelBone[] bones)
        {
            var bonesCollection = DynamicCtor<ModelBoneCollection>(new object[]{ bones });

            typeof(Model).GetField("bones", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(model, bonesCollection);
            typeof(Model).GetField("root", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(model, bones[0]);
        }

        void SetMeshes(Model model, Array<ModelMesh> meshes)
        {
            ModelMesh[] meshArray = meshes.ToArray();
            var meshCollection = DynamicCtor<ModelMeshCollection>(new object[]{ meshArray });

            typeof(Model).GetField("meshes", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(model, meshCollection);
        }

        static ModelMesh CreateNewModelMesh(
            string name, ModelBone parentBone, BoundingSphere boundingSphere, 
            VertexBuffer vertexBuffer, IndexBuffer indexBuffer, ModelMeshPart[] meshParts)
        {
            return DynamicCtor<ModelMesh>(name, parentBone, boundingSphere,
                                          vertexBuffer, indexBuffer, meshParts, new object());
        }

        static ModelMeshPart CreateModelMeshPart(
            int streamOffset, int baseVertex, int numVertices, int startIndex, int primitiveCount,
            VertexBuffer vertexBuffer, IndexBuffer indexBuffer, VertexDeclaration vertexDeclaration)
        {
            return DynamicCtor<ModelMeshPart>(streamOffset, baseVertex, numVertices, startIndex, primitiveCount,
                                              vertexBuffer, indexBuffer, vertexDeclaration, new object());
        }

        static ModelBone CreateModelBone(string name, Matrix transform, int index)
        {
            return DynamicCtor<ModelBone>(name, transform, index);
        }

        static T DynamicCtor<T>(params object[] arguments)
        {
            Type[] argTypes = arguments.Select(o => o.GetType());
            var ctor = typeof(T).GetConstructor(BindingFlags.NonPublic|BindingFlags.Instance, null, argTypes, null);
            if (ctor == null)
                throw new InvalidOperationException($"No suitable constructor found: {typeof(T).Name}({string.Join(",", argTypes.Select(t => t.Name))})");
            return (T)ctor.Invoke(arguments);
        }
    }
}
