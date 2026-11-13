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

        public unsafe StaticMesh ImportStaticMesh(string meshPath, string meshName, bool extractVertexPositions = false)
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
                StaticMesh sm = LoadMeshGroups(mesh, meshName, extractVertexPositions);
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

        unsafe StaticMesh LoadMeshGroups(SdMesh* mesh, string modelName, bool extractVertexPositions)
        {
            // Phase 3.10.B.6: detect skinning at the mesh level so every group's
            // material effect lands as SkinnedLightingEffect (skin VS) rather
            // than the static LightingEffect. The renderer then pushes the
            // bone palette per-frame via SceneObject.AnimationPlayer.
            bool isSkinned = mesh->NumSkinnedBones > 0;
            Map<long, LightingEffect> materials = GetMaterials(mesh, modelName, isSkinned);
            if (HasParts(mesh))
                return LoadParts(mesh, materials, extractVertexPositions);

            var rawMeshes = new Array<MeshData>();
            var positions = extractVertexPositions ? new Array<Vector3>() : null;
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

                if (positions != null)
                    ExtractObjectSpacePositions(data, g->Transform, positions);

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
                RawMeshes = rawMeshes,
                VertexPositions = positions?.ToArray(),
            };
        }

        static unsafe bool HasParts(SdMesh* mesh)
        {
            for (int i = 0; i < mesh->NumGroups; ++i)
                if (StaticMesh.PartIdOf(SDMeshGetGroup(mesh, i)->Name.AsString) > 0)
                    return true;
            return false;
        }

        // a model cut into numbered parts loads as one mesh per material with its parts in order, so an intact ship draws each at once
        unsafe StaticMesh LoadParts(SdMesh* mesh, Map<long, LightingEffect> materials, bool extractVertexPositions)
        {
            // each group's data is fetched once, as every fetch rebuilds it, and stays valid while nothing fetches the group again
            var data = new SdVertexData[mesh->NumGroups];
            var members = new Array<(int Part, int Group)>();
            int numParts = 0;
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                data[i] = SDMeshGroupGetData(g);
                if (data[i].VertexCount == 0 || data[i].IndexCount == 0)
                    continue;
                int part = Math.Max(StaticMesh.PartIdOf(g->Name.AsString), 0); // an object with no part number stays with the core
                members.Add((part, i));
                numParts = Math.Max(numParts, part + 1);
            }
            members.Sort((a, b) => a.Part != b.Part ? a.Part.CompareTo(b.Part) : a.Group.CompareTo(b.Group));

            var positions = extractVertexPositions ? new Array<Vector3>() : null;
            var partTriangles = new Array<Vector3>[numParts]; // always, as a cached model serves every later load
            var rawMeshes = new Array<MeshData>();
            var merged = new bool[members.Count];
            XnaBoundingBox bounds = default;
            for (int first = 0; first < members.Count; ++first)
            {
                if (merged[first])
                    continue;
                SdMeshGroup* g0 = SDMeshGetGroup(mesh, members[first].Group);
                SdVertexData d0 = data[members[first].Group];
                VertexDeclaration declaration = d0.CreateDeclaration();

                // the groups drawn with this one: same material, transform and vertex layout
                var batch = new Array<int>();
                for (int k = first; k < members.Count; ++k)
                {
                    if (merged[k])
                        continue;
                    SdMeshGroup* g = SDMeshGetGroup(mesh, members[k].Group);
                    SdVertexData d = data[members[k].Group];
                    if (g->Mat != g0->Mat || g->Transform != g0->Transform || !SameLayout(d, d0))
                        continue;
                    merged[k] = true;
                    batch.Add(k);
                }

                int vertexCount = 0, indexCount = 0;
                foreach (int k in batch)
                {
                    SdVertexData d = data[members[k].Group];
                    vertexCount += d.VertexCount;
                    indexCount += d.IndexCount;
                }

                var vertices = new byte[vertexCount * d0.VertexStride];
                var indices = new int[indexCount];
                var partStart = new int[numParts];
                var partPrimitives = new int[numParts];
                BoundingSphere sphere = default;
                int vertexBase = 0, indexBase = 0;
                foreach (int k in batch)
                {
                    int part = members[k].Part;
                    SdMeshGroup* g = SDMeshGetGroup(mesh, members[k].Group);
                    SdVertexData d = data[members[k].Group];
                    new ReadOnlySpan<byte>(d.VertexData, d.VertexCount * d.VertexStride).CopyTo(vertices.AsSpan(vertexBase * d0.VertexStride));
                    if (partPrimitives[part] == 0)
                        partStart[part] = indexBase;
                    for (int i = 0; i < d.IndexCount; ++i)
                        indices[indexBase + i] = d.IndexData[i] + vertexBase;
                    partPrimitives[part] += d.IndexCount / 3;
                    sphere = sphere.Radius > 0f ? BoundingSphere.CreateMerged(sphere, g->Bounds) : g->Bounds;

                    var bb = XnaBoundingBox.CreateFromSphere(g->Bounds); // per object, as a model in one piece
                    if (g->Transform != Matrix.Identity)
                    {
                        bb.Min = Vector3.Transform(bb.Min, g->Transform);
                        bb.Max = Vector3.Transform(bb.Max, g->Transform);
                    }
                    bounds = bounds == default ? bb : XnaBoundingBox.CreateMerged(bounds, bb);

                    if (positions != null)
                        ExtractObjectSpacePositions(d, g->Transform, positions);
                    AddTriangles(d, g->Transform, partTriangles[part] ??= new());
                    vertexBase += d.VertexCount;
                    indexBase += d.IndexCount;
                }

                rawMeshes.Add(new MeshData
                {
                    Name              = SDMeshGetGroup(mesh, members[batch[0]].Group)->Name.AsString,
                    Effect            = materials[(long)g0->Mat],
                    IndexBuffer       = CreateIndexBuffer(indices, vertexCount),
                    VertexBuffer      = CreateVertexBuffer(vertices, declaration, vertexCount),
                    VertexDeclaration = declaration,
                    PrimitiveCount    = indexCount / 3,
                    VertexCount       = vertexCount,
                    VertexStride      = d0.VertexStride,
                    ObjectSpaceBoundingSphere = sphere,
                    MeshToObject      = g0->Transform,
                    PartStart         = partStart,
                    PartPrimitives    = partPrimitives,
                });
            }

            var triangles = new Vector3[numParts][];
            for (int part = 0; part < numParts; ++part)
                triangles[part] = partTriangles[part]?.ToArray() ?? Empty<Vector3>.Array;
            return new StaticMesh(mesh->Name.AsString, bounds)
            {
                RawMeshes = rawMeshes,
                VertexPositions = positions?.ToArray(),
                NumParts = numParts,
                PartTriangles = triangles,
            };
        }

        static unsafe bool SameLayout(in SdVertexData a, in SdVertexData b)
        {
            if (a.VertexStride != b.VertexStride || a.LayoutCount != b.LayoutCount)
                return false;
            for (int i = 0; i < a.LayoutCount; ++i)
                if (a.Layout[i].Offset != b.Layout[i].Offset || a.Layout[i].NativeFormat != b.Layout[i].NativeFormat
                    || a.Layout[i].NativeUsage != b.Layout[i].NativeUsage)
                    return false;
            return true;
        }

        IndexBuffer CreateIndexBuffer(int[] indices, int vertexCount)
        {
            if (vertexCount <= ushort.MaxValue + 1)
            {
                ushort[] shorts = indices.Select(i => (ushort)i);
                var buf16 = new IndexBuffer(Device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
                buf16.SetData(shorts);
                return buf16;
            }
            var buf = new IndexBuffer(Device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            buf.SetData(indices);
            return buf;
        }

        VertexBuffer CreateVertexBuffer(byte[] vertices, VertexDeclaration declaration, int vertexCount)
        {
            var buf = new VertexBuffer(Device, declaration, vertexCount, BufferUsage.WriteOnly);
            buf.SetData(vertices);
            return buf;
        }

        // each triangle's three corners in object space, as the renderer places them
        static unsafe void AddTriangles(SdVertexData data, in Matrix transform, Array<Vector3> dst)
        {
            int posOffset = PositionOffset(data);
            if (posOffset < 0)
                return;
            bool identity = transform == Matrix.Identity;
            byte* basePtr = data.VertexData + posOffset;
            for (int i = 0; i < data.IndexCount; ++i)
            {
                var pos = *(Vector3*)(basePtr + data.IndexData[i] * data.VertexStride);
                dst.Add(identity ? pos : Vector3.Transform(pos, transform));
            }
        }

        // the byte offset of the position in a vertex, -1 when it has none the importer can read
        static unsafe int PositionOffset(in SdVertexData data)
        {
            for (int i = 0; i < data.LayoutCount; ++i)
            {
                if (data.Layout[i].NativeUsage == 0) // SDElementUsage::Position
                {
                    byte format = data.Layout[i].NativeFormat;
                    return format is 2 or 3 ? data.Layout[i].Offset : -1; // Vector3 / Vector4
                }
            }
            return -1;
        }

        // Same MeshToObject transform the renderer applies to vertices, so
        // collected positions live in the same space as SceneObject bounds.
        static unsafe void ExtractObjectSpacePositions(SdVertexData data, in Matrix transform, Array<Vector3> dst)
        {
            int posOffset = PositionOffset(data);
            if (posOffset < 0)
                return;

            bool identity = transform == Matrix.Identity;
            byte* basePtr = data.VertexData + posOffset;
            int stride = data.VertexStride;
            for (int i = 0; i < data.VertexCount; ++i)
            {
                var pos = *(Vector3*)(basePtr + i * stride);
                if (!identity)
                    pos = Vector3.Transform(pos, transform);
                dst.Add(pos);
            }
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
