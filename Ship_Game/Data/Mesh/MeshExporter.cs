using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SynapseGaming.LightingSystem.Effects;

namespace Ship_Game.Data.Mesh
{
    public class MeshExporter : MeshInterface
    {
        public MeshExporter(GameContentManager content) : base(content)
        {
        }

        public bool Export(Model model, string name, string modelFilePath)
        {
            return Export(model, null, null, name, modelFilePath);
        }

        public bool Export(SkinnedModel model, string name, string modelFilePath)
        {
            return Export(model.Model, model.SkeletonBones, model.AnimationClips, name, modelFilePath);
        }

        public unsafe bool Export(Model model,
                                  SkinnedModelBoneCollection animBones, // animated bones
                                  AnimationClipDictionary animClips, // animation clips, each clip channel maps to 1 bone
                                  string name, string modelFilePath)
        {
            if (model.Meshes.Count == 0)
                return false;

            string exportDir = Path.GetDirectoryName(modelFilePath) ?? "";
            Directory.CreateDirectory(exportDir);

            SdMesh* mesh = SDMeshCreateEmpty(name);
            try
            {
                CreateMeshGroups(mesh, exportDir, model.Meshes);
                CreateBones(mesh, model, animBones, animClips);
                return SDMeshSave(mesh, modelFilePath);
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        unsafe void CreateBones(SdMesh* mesh, Model model, 
                                SkinnedModelBoneCollection animBones,
                                AnimationClipDictionary animClips)
        {
            
        }

        unsafe void CreateMeshGroups(SdMesh* mesh, string modelExportDir, ModelMeshCollection meshes)
        {
            Map<Effect, long> materials = ExportMaterials(mesh, modelExportDir, meshes);
            foreach (ModelMesh modelMesh in meshes)
            {
                Matrix transform = modelMesh.ParentBone.Transform;

                for (int i = 0; i < modelMesh.MeshParts.Count; ++i)
                {
                    ModelMeshPart part = modelMesh.MeshParts[i];

                    string groupName = (modelMesh.MeshParts.Count > 1) ? modelMesh.Name + i : modelMesh.Name;
                    SdMeshGroup* group = SDMeshNewGroup(mesh, groupName, &transform);
                    VertexBuffer vb = modelMesh.VertexBuffer;
                    IndexBuffer  ib = modelMesh.IndexBuffer;

                    Vector3[] vertices = vb.GetArray<Vector3>(part, VertexElementUsage.Position);
                    Vector3[] normals  = vb.GetArray<Vector3>(part, VertexElementUsage.Normal);
                    Vector2[] coords   = vb.GetArray<Vector2>(part, VertexElementUsage.TextureCoordinate);
                    Vector4[] weights  = vb.GetArray<Vector4>(part, VertexElementUsage.BlendWeight);
                    SdBlendIndices[] blendIndices = vb.GetArray<SdBlendIndices>(part, VertexElementUsage.BlendIndices);

                    SdVertexData data;
                    data.NumIndices = part.PrimitiveCount * 3;
                    data.NumVertices = part.NumVertices;

                    var indexData = new ushort[data.NumIndices];
                    ib.GetData(part.StartIndex*sizeof(ushort), indexData, 0, data.NumIndices);

                    fixed(ushort* pIndices = indexData)
                    fixed(Vector3* pVertices = vertices)
                    fixed(Vector3* pNormals = normals)
                    fixed(Vector2* pCoords  = coords)
                    fixed(Vector4* pWeights = weights)
                    fixed(SdBlendIndices* pBlends = blendIndices)
                    {
                        data.Indices = pIndices;
                        data.Vertices = pVertices;
                        data.Normals = pNormals;
                        data.Coords = pCoords;
                        data.Weights = pWeights;
                        data.BlendIndices = pBlends;
                        SDMeshGroupSetData(group, data);
                    }

                    if (modelMesh.Effects[0] != null)
                    {
                        var material = (SdMaterial*)materials[modelMesh.Effects[0]];
                        if (material != null)
                            SDMeshGroupSetMaterial(group, material);
                    }
                }
            }
        }
        
        unsafe Map<Effect, long> ExportMaterials(SdMesh* mesh, string exportDir, ModelMeshCollection meshes)
        {
            var exported = new Map<Effect, long>();
            string name = mesh->Name.AsString;
            foreach (ModelMesh modelMesh in meshes)
            {
                for (int i = 0; i < modelMesh.Effects.Count; ++i)
                {
                    Effect effect = modelMesh.Effects[i];
                    if (!exported.ContainsKey(effect))
                    {
                        if (effect is BaseMaterialEffect sunburn)
                        {
                            string matName = sunburn.MaterialName.NotEmpty() ? sunburn.MaterialName : name+i;
                            exported[effect] = (long)ExportMaterial(mesh, sunburn, matName, exportDir);
                        }
                        else if (effect is BasicEffect basic)
                        {
                            string matName = basic.Texture != null && basic.Texture.Name.NotEmpty()
                                ? basic.Texture.Name : name+i;
                            exported[effect] = (long)ExportMaterial(mesh, basic, matName, exportDir);
                        }
                        else
                        {
                            exported[effect] = 0;
                        }
                    }
                }
            }
            return exported;
        }

        string TrySaveTexture(string modelExportDir, string textureName, Texture2D texture)
        {
            if (textureName.IsEmpty() || texture == null)
                return "";

            string name = Path.ChangeExtension(Path.GetFileName(textureName), "dds");
            string writeTo = Path.Combine(modelExportDir, name);

            lock (texture) // Texture2D.Save will crash if 2 threads try to save the same texture
            {
                if (!File.Exists(writeTo))
                {
                    Log.Warning($"  ExportTexture: {writeTo}");
                    texture.Save(writeTo, ImageFileFormat.Dds);
                }
            }
            return name;
        }

        unsafe SdMaterial* ExportMaterial(SdMesh* mesh, BaseMaterialEffect fx, string name, string modelExportDir)
        {
            string diffusePath  = TrySaveTexture(modelExportDir, fx.DiffuseMapFile,       fx.DiffuseMapTexture);
            string specularPath = TrySaveTexture(modelExportDir, fx.SpecularColorMapFile, fx.SpecularColorMapTexture);
            string normalPath   = TrySaveTexture(modelExportDir, fx.NormalMapFile,        fx.NormalMapTexture);
            string emissivePath = TrySaveTexture(modelExportDir, fx.EmissiveMapFile,      fx.EmissiveMapTexture);

            return SDMeshCreateMaterial(mesh, name, 
                diffusePath, alphaPath:"",  specularPath, normalPath, emissivePath, 
                ambientColor:Vector3.One, fx.DiffuseColor, specularColor:Vector3.One, Vector3.Zero, 
                fx.SpecularAmount / 16f, fx.Transparency);
        }

        unsafe SdMaterial* ExportMaterial(SdMesh* mesh, BasicEffect fx, string name, string modelExportDir)
        {
            string diffusePath, specularPath = "", normalPath = "", emissivePath = "";
            if (fx.Texture == null)
            {
                string baseName = name.NotEmpty() && char.IsLetter(name[name.Length - 1]) 
                                ? name.Substring(0, name.Length-1) : name;

                diffusePath  = baseName + "_d.png";
                specularPath = baseName + "_s.png";
                normalPath   = baseName + "_n.png";
                emissivePath = baseName + "_g.png";
            }
            else
            {
                diffusePath  = TrySaveTexture(modelExportDir, name+".png", fx.Texture);
            }

            return SDMeshCreateMaterial(mesh, name, 
                diffusePath, alphaPath:"", specularPath, normalPath, emissivePath, 
                fx.AmbientLightColor, fx.DiffuseColor, fx.SpecularColor, fx.EmissiveColor, 
                fx.SpecularPower, fx.Alpha);
        }
    }
}
