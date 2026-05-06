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
                StaticMesh sm = LoadMeshGroups(mesh, meshName);
                LoadSkinnedAndAnimData(mesh, sm);
                return sm;
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
            // Phase 3.10.B.6: detect skinning at the mesh level so every group's
            // material effect lands as SkinnedLightingEffect (skin VS) rather
            // than the static LightingEffect. The renderer then pushes the
            // bone palette per-frame via SceneObject.AnimationPlayer.
            bool isSkinned = mesh->NumSkinnedBones > 0;
            Map<long, LightingEffect> materials = GetMaterials(mesh, modelName, isSkinned);

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
                    // Per-group transform from FBX/OBJ root. Without this the renderer
                    // composes fx.World = so.World * Identity and drops the FBX root
                    // orientation (and any nested-bone offsets) — modded ships and
                    // stations rendered "on their backs" until this got wired up.
                    MeshToObject = g->Transform,
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

        // Phase 3.10.B.3: pulls SkinnedBones + AnimationClips out of the loaded
        // SDMesh via the B.2 read-side getters and onto the StaticMesh. No-op
        // for static meshes (NumSkinnedBones / NumAnimClips both 0).
        unsafe void LoadSkinnedAndAnimData(SdMesh* mesh, StaticMesh staticMesh)
        {
            int numBones = mesh->NumSkinnedBones;
            if (numBones > 0)
            {
                staticMesh.SkinnedBones = new SkinnedBoneData[numBones];
                for (int i = 0; i < numBones; i++)
                {
                    SdSkinnedBoneInfo info = SDMeshGetSkinnedBone(mesh, i);
                    staticMesh.SkinnedBones[i] = new SkinnedBoneData
                    {
                        Name                     = info.Name.AsString,
                        BoneIndex                = info.BoneIndex,
                        ParentIndex              = info.ParentBone,
                        BindPoseTranslation      = info.BindPose.Translation,
                        BindPoseRotation         = info.BindPose.Rotation,
                        BindPoseScale            = info.BindPose.Scale,
                        InverseBindPoseTransform = info.InverseBindPoseTransform,
                    };
                }
            }

            int numClips = mesh->NumAnimClips;
            if (numClips > 0)
            {
                staticMesh.AnimationClips = new AnimationClipData[numClips];
                for (int c = 0; c < numClips; c++)
                {
                    SdAnimationClipInfo ci = SDMeshGetAnimationClip(mesh, c);
                    var clip = new AnimationClipData
                    {
                        Name       = ci.Name.AsString,
                        Duration   = ci.Duration,
                        Animations = new BoneAnimationData[ci.NumAnimations],
                    };
                    for (int a = 0; a < ci.NumAnimations; a++)
                    {
                        SdBoneAnimationInfo bai = SDMeshGetBoneAnimation(mesh, c, a);
                        var ba = new BoneAnimationData
                        {
                            SkinnedBoneIndex = bai.SkinnedBoneIndex,
                            Frames           = new KeyFrameData[bai.NumFrames],
                        };
                        for (int f = 0; f < bai.NumFrames; f++)
                        {
                            SdAnimationKeyFrameInfo kf = SDMeshGetAnimationKeyFrame(mesh, c, a, f);
                            ba.Frames[f] = new KeyFrameData
                            {
                                Time        = kf.Time,
                                Translation = kf.Pose.Translation,
                                Rotation    = kf.Pose.Rotation,
                                Scale       = kf.Pose.Scale,
                            };
                        }
                        clip.Animations[a] = ba;
                    }
                    staticMesh.AnimationClips[c] = clip;
                }
            }
        }

        unsafe Map<long, LightingEffect> GetMaterials(SdMesh* mesh, string modelName, bool isSkinned)
        {
            var materials = new Map<long, LightingEffect>();
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                long ptr = (long)g->Mat;
                if (!materials.ContainsKey(ptr))
                {
                    if (ptr == 0)
                    {
                        materials[ptr] = isSkinned
                            ? new SkinnedLightingEffect(Device)
                            : new LightingEffect(Device);
                    }
                    else
                    {
                        materials[ptr] = CreateMaterialEffect(g->Mat, Device, Content, modelName, isSkinned);
                    }
                }
            }
            return materials;
        }
    }
}
