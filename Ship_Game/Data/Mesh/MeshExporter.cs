using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using Ship_Game.Data.Texture;
using SynapseGaming.LightingSystem.Effects;
using SDGraphics;
using SDUtils;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;

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

        // Phase 3.10.B.8: convert XNA Quaternion to Euler XYZ DEGREES matching
        // FBX's intrinsic XYZ rotation order (rotation matrix M = Rz * Ry * Rx,
        // i.e., apply X first, then Y, then Z).
        //
        // Why: SdBonePose.Rotation is byte-for-byte ABI-aligned with NanoMesh's
        // Nano::BonePose::Rotation (Vector3, Euler degrees). The previous shape
        // had `XnaQuaternion Orientation` which silently dropped qw and treated
        // (qx,qy,qz) as Euler degrees during P/Invoke marshaling — every
        // exported skinned mesh ended up with garbage cluster matrices and
        // bizarre keyframe rotations. Converting at the boundary keeps the
        // in-memory model XNA-native (Quaternion) and the wire format FBX-
        // standard (Euler degrees).
        //
        // Round-trip with Nano::BonePose's writer (GLToFbxDouble3 + LclRotation)
        // and the migration-side reader (FbxToOpenGL on the same field) is
        // lossless except at gimbal-lock poses, which character animation rigs
        // don't normally hit.
        static Vector3 QuatToEulerXYZDegrees(XnaQuaternion q)
        {
            // Standard inverse of intrinsic XYZ Euler → quaternion.
            //   ry = asin(2*(qw*qy - qx*qz))
            //   rx = atan2(2*(qw*qx + qy*qz), 1 - 2*(qx² + qy²))
            //   rz = atan2(2*(qw*qz + qx*qy), 1 - 2*(qy² + qz²))
            float qx = q.X, qy = q.Y, qz = q.Z, qw = q.W;
            float pitchSin = 2f * (qw * qy - qx * qz);
            if (pitchSin > 1f) pitchSin = 1f;
            else if (pitchSin < -1f) pitchSin = -1f;
            float ry = (float)Math.Asin(pitchSin);
            float rx = (float)Math.Atan2(2f * (qw * qx + qy * qz), 1f - 2f * (qx * qx + qy * qy));
            float rz = (float)Math.Atan2(2f * (qw * qz + qx * qy), 1f - 2f * (qy * qy + qz * qz));
            const float radToDeg = (float)(180.0 / Math.PI);
            return new Vector3(rx * radToDeg, ry * radToDeg, rz * radToDeg);
        }

        static unsafe void CreateBones(SdMesh* mesh, Model model,
                                       SkinnedModelBoneCollection animBones,
                                       AnimationClipDictionary animClips)
        {
            int allBones = model.Bones.Count;
            for (int i = 0; i < allBones; ++i)
            {
                ModelBone b = model.Bones[i];
                // Phase 3.10.B.8: decompose ModelBone.Transform into T/R/S in C# and
                // pass through SDMeshAddBoneTRS instead of letting C++ rpp::Matrix4
                // do the extraction. rpp's getRotationAngles assumes a column-vector
                // intrinsic-XYZ matrix; XNA's Matrix is row-vector with the same byte
                // layout, so the bytes look like the transposed (= inverse) rotation
                // and the extracted Eulers come back negated. The keyframe path below
                // already converts via QuatToEulerXYZDegrees from XnaQuaternion, so
                // routing the bind pose through the same helper aligns the two
                // conventions and stops bind/keyframe Eulers from disagreeing.
                b.Transform.Decompose(out Microsoft.Xna.Framework.Vector3 xnaScale,
                                      out XnaQuaternion xnaQuat,
                                      out Microsoft.Xna.Framework.Vector3 xnaTrans);
                var bindPose = new SdBonePose
                {
                    Translation = new Vector3(xnaTrans),
                    Rotation = QuatToEulerXYZDegrees(xnaQuat),
                    Scale = new Vector3(xnaScale),
                };
                SDMeshAddBoneTRS(mesh, b.Name, b.Index, b.Parent?.Index ?? -1, bindPose);
            }

            int animatedBones = animBones.Count;
            for (int i = 0; i < animatedBones; ++i)
            {
                SkinnedModelBone bone = animBones[i];
                Pose pose = bone.BindPose;
                var sdPose = new SdBonePose
                {
                    Translation = new Vector3(pose.Translation),
                    Rotation = QuatToEulerXYZDegrees(pose.Orientation),
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
                                Rotation = QuatToEulerXYZDegrees(pose.Orientation),
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
                // Compose absolute world transform by walking up the parent-bone chain.
                // ParentBone.Transform alone misses intermediate bones in deeper hierarchies.
                Matrix transform = Matrix.Identity;
                for (ModelBone b = modelMesh.ParentBone; b != null; b = b.Parent)
                    transform = new Matrix(b.Transform) * transform;

                for (int i = 0; i < modelMesh.MeshParts.Count; ++i)
                {
                    ModelMeshPart part = modelMesh.MeshParts[i];
                    string baseGroupName = (modelMesh.MeshParts.Count > 1) ? modelMesh.Name + i : modelMesh.Name;
                    VertexBuffer vb = modelMesh.VertexBuffer;
                    IndexBuffer  ib = modelMesh.IndexBuffer;
                    SdVertexElement[] layout = CreateVertexElements(part.VertexDeclaration);

                    // Read indices honoring the source IndexElementSize. XNA models with
                    // > 65535 vertices per part (Star Trek Excalibur 76890, Omaga 84802)
                    // use 32-bit indices. The previous unconditional 16-bit read truncated
                    // the high word → spike-fan deformation.
                    bool is32Bit = ib.IndexElementSize == IndexElementSize.ThirtyTwoBits;
                    int indexCount = part.PrimitiveCount * 3;
                    int[] partIndices = new int[indexCount];
                    if (is32Bit)
                    {
                        var raw = new uint[indexCount];
                        ib.GetData(part.StartIndex * 4, raw, 0, indexCount);
                        for (int k = 0; k < indexCount; ++k) partIndices[k] = (int)raw[k];
                    }
                    else
                    {
                        var raw = new ushort[indexCount];
                        ib.GetData(part.StartIndex * 2, raw, 0, indexCount);
                        for (int k = 0; k < indexCount; ++k) partIndices[k] = raw[k];
                    }

                    int vertexStride = part.VertexStride;
                    var partVertexData = new byte[part.NumVertices * vertexStride];
                    vb.GetData(part.BaseVertex * vertexStride, partVertexData, 0, partVertexData.Length, 0);

                    Effect partEffect = part.Effect;
                    long matAddr = (partEffect != null && materials.TryGetValue(partEffect, out long m) && m != 0) ? m : 0;

                    // FBX writer (and NanoMesh's Triangle indices) can carry 32-bit values
                    // internally, but the C++ SDVertexData ABI surface is `ushort* IndexData`.
                    // Rather than churn the submodule, chunk parts > 65535 verts into
                    // <= 65530-vert sub-groups (small headroom) and write each as its own
                    // FBX group. Sub-groups share the same per-part material binding.
                    const int VERT_CAP = 65530;

                    if (part.NumVertices <= VERT_CAP)
                    {
                        var indexData = new ushort[indexCount];
                        for (int k = 0; k < indexCount; ++k) indexData[k] = (ushort)partIndices[k];
                        WriteChunk(mesh, transform, baseGroupName, layout, vertexStride, partVertexData, indexData, part.NumVertices, matAddr);
                    }
                    else
                    {
                        Log.Write(ConsoleColor.Cyan, $"  Splitting {baseGroupName}: {part.NumVertices} verts > {VERT_CAP}, chunking by triangle...");
                        EmitChunkedPart(mesh, transform, baseGroupName, layout, vertexStride,
                            partVertexData, partIndices, part.NumVertices, VERT_CAP, matAddr);
                    }
                }
            }
        }

        static unsafe void EmitChunkedPart(SdMesh* mesh, Matrix transform, string baseGroupName,
                                           SdVertexElement[] layout, int vertexStride,
                                           byte[] partVertexData, int[] partIndices, int partVertexCount,
                                           int vertCap, long matAddr)
        {
            var localOf = new int[partVertexCount];
            for (int k = 0; k < localOf.Length; ++k) localOf[k] = -1;
            var chunkIndices = new List<ushort>(vertCap);
            var chunkVerts = new List<byte>(vertCap * vertexStride);
            var dirty = new List<int>(vertCap);
            int chunkVertCount = 0;
            int chunkIndex = 0;
            int triCount = partIndices.Length / 3;

            for (int t = 0; t < triCount; ++t)
            {
                int a = partIndices[t * 3];
                int b = partIndices[t * 3 + 1];
                int c = partIndices[t * 3 + 2];
                int newVerts = (localOf[a] < 0 ? 1 : 0) + (localOf[b] < 0 ? 1 : 0) + (localOf[c] < 0 ? 1 : 0);
                if (chunkVertCount + newVerts > vertCap)
                {
                    WriteChunk(mesh, transform, baseGroupName + "_chunk" + chunkIndex++,
                        layout, vertexStride, chunkVerts.ToArray(), chunkIndices.ToArray(), chunkVertCount, matAddr);
                    for (int k = 0; k < dirty.Count; ++k) localOf[dirty[k]] = -1;
                    dirty.Clear();
                    chunkIndices.Clear();
                    chunkVerts.Clear();
                    chunkVertCount = 0;
                }
                chunkIndices.Add(MapIdx(a, localOf, chunkVerts, partVertexData, vertexStride, ref chunkVertCount, dirty));
                chunkIndices.Add(MapIdx(b, localOf, chunkVerts, partVertexData, vertexStride, ref chunkVertCount, dirty));
                chunkIndices.Add(MapIdx(c, localOf, chunkVerts, partVertexData, vertexStride, ref chunkVertCount, dirty));
            }
            if (chunkIndices.Count > 0)
                WriteChunk(mesh, transform, baseGroupName + "_chunk" + chunkIndex,
                    layout, vertexStride, chunkVerts.ToArray(), chunkIndices.ToArray(), chunkVertCount, matAddr);
        }

        static ushort MapIdx(int origIdx, int[] localOf, List<byte> chunkVerts,
                             byte[] partVertexData, int vertexStride, ref int chunkVertCount,
                             List<int> dirty)
        {
            int local = localOf[origIdx];
            if (local < 0)
            {
                local = chunkVertCount++;
                localOf[origIdx] = local;
                dirty.Add(origIdx);
                int off = origIdx * vertexStride;
                for (int b = 0; b < vertexStride; ++b)
                    chunkVerts.Add(partVertexData[off + b]);
            }
            return (ushort)local;
        }

        static unsafe void WriteChunk(SdMesh* mesh, Matrix transform, string groupName,
                                      SdVertexElement[] layout, int vertexStride,
                                      byte[] vertexBytes, ushort[] indexData, int vertexCount,
                                      long matAddr)
        {
            SdMeshGroup* group = SDMeshNewGroup(mesh, groupName, &transform);
            SdVertexData data;
            data.VertexStride = vertexStride;
            data.LayoutCount  = layout.Length;
            data.IndexCount   = indexData.Length;
            data.VertexCount  = vertexCount;
            fixed (ushort* pIndexData = indexData)
            fixed (byte* pVertexData = vertexBytes)
            fixed (SdVertexElement* pLayout = layout)
            {
                data.IndexData = pIndexData;
                data.VertexData = pVertexData;
                data.Layout = pLayout;
                SDMeshGroupSetData(group, data);
            }
            if (matAddr != 0)
                SDMeshGroupSetMaterial(group, (SdMaterial*)matAddr);
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
                            // Log the actual runtime type so the next re-export pass tells
                            // us which Effect class needs a new branch above (likely a
                            // SunBurn variant or a BasicEffect without Texture).
                            Log.Warning($"No texture for mesh {exportDir}/{name} effect {i} (type: {effect?.GetType().FullName ?? "<null>"})");
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
                    // Texture was already saved (possibly in a different model's folder).
                    // Return a relative path so the .mtl reference resolves cross-folder
                    // (e.g. "../ship09_d.dds"). Same-folder case yields just the filename.
                    return MakeRelativePath(modelExportDir, alreadySavedPath);
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

        // Computes a forward-slash relative path from `fromDir` to `toFile` using URI logic
        // (works on .NET Framework 4.8 — Path.GetRelativePath is .NET Core+ only).
        static string MakeRelativePath(string fromDir, string toFile)
        {
            string fromFull = Path.GetFullPath(fromDir);
            if (!fromFull.EndsWith(Path.DirectorySeparatorChar.ToString()))
                fromFull += Path.DirectorySeparatorChar;
            var fromUri = new Uri(fromFull);
            var toUri = new Uri(Path.GetFullPath(toFile));
            return Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
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

            // BasicEffect.SpecularPower is in the XNA range [16, ~128]. The C-API
            // expects Specular in [0, 1] — the BaseMaterialEffect overload above
            // already normalises (SpecularAmount/16). Mirror that here; without it,
            // values like 50 round-tripped to the FBX and the runtime computed
            // SpecularPower = 16 + 48*50 ≈ 2400 + SpecularAmount = 300, blowing the
            // hull out to a silver-white highlight. Star Trek Excalibur hit this.
            float specular = fx.SpecularPower / 64.0f;
            return SDMeshCreateMaterial(mesh, matName,
                diffusePath, alphaPath:"", specularPath, normalPath, emissivePath,
                new Vector3(fx.AmbientLightColor),
                new Vector3(fx.DiffuseColor),
                new Vector3(fx.SpecularColor),
                new Vector3(fx.EmissiveColor),
                specular, fx.Alpha);
        }
    }
}
