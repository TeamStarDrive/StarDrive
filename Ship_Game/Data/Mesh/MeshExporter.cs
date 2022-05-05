using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using Ship_Game.Data.Texture;
using SynapseGaming.LightingSystem.Effects;
using SDGraphics;

namespace Ship_Game.Data.Mesh
{
    public class MeshExporter : MeshInterface
    {
        readonly TextureExporter TexExport;

        public MeshExporter(GameContentManager content) : base(content)
        {
            TexExport = new TextureExporter(Content);
        }

        public void Reset()
        {
            AlreadySavedTextures.Clear();
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
                if (animBones != null)
                {
                    CreateBones(mesh, model, animBones, animClips);
                }
                return SDMeshSave(mesh, modelFilePath);
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        static unsafe void CreateBones(SdMesh* mesh, Model model,
                                       SkinnedModelBoneCollection animBones,
                                       AnimationClipDictionary animClips)
        {
            int allBones = model.Bones.Count;
            for (int i = 0; i < allBones; ++i)
            {
                ModelBone b = model.Bones[i];
                SDMeshAddBone(mesh, b.Name, b.Index, b.Parent?.Index ?? -1, new Matrix(b.Transform));
            }

            int animatedBones = animBones.Count;
            for (int i = 0; i < animatedBones; ++i)
            {
                SkinnedModelBone bone = animBones[i];
                Pose pose = bone.BindPose;
                var sdPose = new SdBonePose
                {
                    Translation = new Vector3(pose.Translation),
                    Orientation = pose.Orientation,
                    Scale = new Vector3(pose.Scale)
                };
                SDMeshAddSkinnedBone(mesh, bone.Name, bone.Index, bone.Parent?.Index ?? -1,
                                     sdPose, new Matrix(bone.InverseBindPoseTransform));
            }

            AnimationClip[] clips = animClips.Values.Sorted(clip => clip.Name);
            foreach (AnimationClip animClip in clips)
            {
                SdAnimationClip clip = SDMeshCreateAnimationClip(mesh, 
                    animClip.Name, (float)animClip.Duration.TotalSeconds);

                foreach (KeyValuePair<string, AnimationChannel> ch in animClip.Channels)
                {
                    int skinnedIndex = animBones.IndexOf(b => b.Name == ch.Key);
                    if (skinnedIndex == -1)
                    {
                        Log.Error($"Invalid AnimationChannel {ch.Key} does not reference a valid SkinnedBone");
                        continue;
                    }

                    SdBoneAnimation anim = SDMeshAddBoneAnimation(mesh, clip, skinnedIndex);
                    foreach (AnimationChannelKeyframe kf in ch.Value)
                    {
                        Pose pose = kf.Pose;
                        var keyFrame = new SdAnimationKeyFrame
                        {
                            Time = (float)kf.Time.TotalSeconds,
                            Pose = new SdBonePose
                            {
                                Translation = new Vector3(pose.Translation),
                                Orientation = pose.Orientation,
                                Scale = new Vector3(pose.Scale)
                            }
                        };
                        SDMeshAddAnimationKeyFrame(mesh, clip, anim, keyFrame);
                    }
                }
            }
        }

        unsafe void CreateMeshGroups(SdMesh* mesh, string modelExportDir, ModelMeshCollection meshes)
        {
            Map<Effect, long> materials = ExportMaterials(mesh, modelExportDir, meshes);
            foreach (ModelMesh modelMesh in meshes)
            {
                Matrix transform = new Matrix(modelMesh.ParentBone.Transform);

                for (int i = 0; i < modelMesh.MeshParts.Count; ++i)
                {
                    ModelMeshPart part = modelMesh.MeshParts[i];

                    string groupName = (modelMesh.MeshParts.Count > 1) ? modelMesh.Name + i : modelMesh.Name;
                    SdMeshGroup* group = SDMeshNewGroup(mesh, groupName, &transform);
                    VertexBuffer vb = modelMesh.VertexBuffer;
                    IndexBuffer  ib = modelMesh.IndexBuffer;

                    SdVertexElement[] layout = CreateVertexElements(part.VertexDeclaration);

                    SdVertexData data;
                    data.VertexStride = part.VertexStride;
                    data.LayoutCount  = layout.Length;
                    data.IndexCount   = part.PrimitiveCount * 3;
                    data.VertexCount  = part.NumVertices;

                    var indexData = new ushort[data.IndexCount];
                    ib.GetData(part.StartIndex*sizeof(ushort), indexData, 0, data.IndexCount);

                    var vertexData = new byte[data.VertexCount * data.VertexStride];
                    vb.GetData(part.BaseVertex * part.VertexStride, vertexData, 0, vertexData.Length, 0);

                    fixed(ushort* pIndexData = indexData)
                    fixed(byte* pVertexData = vertexData)
                    fixed(SdVertexElement* pLayout = layout)
                    {
                        data.IndexData = pIndexData;
                        data.VertexData = pVertexData;
                        data.Layout = pLayout;
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
                            string matName = sunburn.MaterialName;
                            if (matName.IsEmpty())
                                matName = name+i;
                            exported[effect] = (long)ExportMaterial(mesh, sunburn, matName, exportDir);
                        }
                        else if (effect is BasicEffect basic && basic.Texture != null)
                        {
                            // ex: "Model\\SpaceObjects\\arazius3night_0.xnb"
                            string matName = Path.GetFileNameWithoutExtension(basic.Texture.Name);
                            if (matName.IsEmpty())
                                matName = name + i;
                            exported[effect] = (long)ExportMaterial(mesh, basic, matName, exportDir);
                        }
                        else
                        {
                            Log.Warning($"No texture for mesh {exportDir}/{name} effect {i}");
                            exported[effect] = 0;
                        }
                    }
                }
            }
            return exported;
        }

        Map<Texture2D, string> AlreadySavedTextures = new Map<Texture2D, string>();

        public bool IsAlreadySavedTexture(Texture2D tex)
        {
            return AlreadySavedTextures.ContainsKey(tex);
        }

        public void AddAlreadySavedTexture(Texture2D tex, string texSavePath)
        {
            AlreadySavedTextures[tex] = texSavePath;
        }

        string TrySaveTexture(string modelExportDir, string matName, string textureName, Texture2D texture)
        {
            if (textureName.IsEmpty() || texture == null)
                return "";

            string writeTo = Path.Combine(modelExportDir, Path.GetFileName(textureName));
            writeTo = TexExport.GetSaveAutoFormatPath(texture, writeTo);

            lock (texture) // Texture2D.Save will crash if 2 threads try to save the same texture
            {
                // This happens a lot. Many ships share a common base texture.
                if (AlreadySavedTextures.TryGetValue(texture, out string alreadySavedPath))
                {
                    return Path.GetFileName(alreadySavedPath);
                }

                AlreadySavedTextures.Add(texture, writeTo);
                if (!File.Exists(writeTo))
                {
                    Log.Write(ConsoleColor.Green, $"  Export Mesh MaterialTex: {matName} {writeTo}");
                    TexExport.SaveAutoFormat(texture, writeTo);
                }

                return Path.GetFileName(writeTo);
            }
        }

        unsafe SdMaterial* ExportMaterial(SdMesh* mesh, BaseMaterialEffect fx, string matName, string modelExportDir)
        {
            string diffusePath  = TrySaveTexture(modelExportDir, matName, fx.DiffuseMapFile,       fx.DiffuseMapTexture);
            string specularPath = TrySaveTexture(modelExportDir, matName, fx.SpecularColorMapFile, fx.SpecularColorMapTexture);
            string normalPath   = TrySaveTexture(modelExportDir, matName, fx.NormalMapFile,        fx.NormalMapTexture);
            string emissivePath = TrySaveTexture(modelExportDir, matName, fx.EmissiveMapFile,      fx.EmissiveMapTexture);

            return SDMeshCreateMaterial(mesh, matName, 
                diffusePath, alphaPath:"",  specularPath, normalPath, emissivePath, 
                ambientColor:Vector3.One, new Vector3(fx.DiffuseColor), specularColor:Vector3.One, Vector3.Zero, 
                fx.SpecularAmount / 16f, fx.Transparency);
        }

        unsafe SdMaterial* ExportMaterial(SdMesh* mesh, BasicEffect fx, string matName, string modelExportDir)
        {
            string diffusePath, specularPath = "", normalPath = "", emissivePath = "";
            if (fx.Texture == null)
            {
                string baseName = matName.NotEmpty() && char.IsLetter(matName[matName.Length - 1]) 
                                ? matName.Substring(0, matName.Length-1) : matName;

                diffusePath  = baseName + "_d.png";
                specularPath = baseName + "_s.png";
                normalPath   = baseName + "_n.png";
                emissivePath = baseName + "_g.png";
            }
            else
            {
                diffusePath = TrySaveTexture(modelExportDir, matName, matName+".png", fx.Texture);
            }

            return SDMeshCreateMaterial(mesh, matName, 
                diffusePath, alphaPath:"", specularPath, normalPath, emissivePath, 
                new Vector3(fx.AmbientLightColor),
                new Vector3(fx.DiffuseColor),
                new Vector3(fx.SpecularColor),
                new Vector3(fx.EmissiveColor),
                fx.SpecularPower, fx.Alpha);
        }
    }
}
