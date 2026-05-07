using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.Data.Mesh;

// Phase 3.10.B.4: minimal runtime animation player. Owns a SkinnedBoneData[]
// + AnimationClipData[] payload (B.3 data types), advances time inside the
// active clip, and produces a matrix palette ready for the GPU's skinning
// vertex shader (B.5).
//
// Skinning convention (XNA row-vector × row-major):
//   skinMatrix[i] = inverseBindPose[i] * worldPoseCurrent[i]
//   vertex_skinned = sum_j weight_j * (vertex * skinMatrix[boneIndex_j])
//
// Hierarchy: B.4 originally assumed parent indices come before child indices
// (FBX *typically* depth-first-writes), but real export pipelines (Maya/Max
// DCC tools driving the legacy StarDrive XNB exporter) ship bones in
// arbitrary order — ship17a has bone 0 with parentIndex=6 etc. A naive
// forward sweep silently mis-roots out-of-order bones and produces garbage
// skin matrices for the affected verts. We instead pre-compute a topological
// traversal at ctor time so every parent is evaluated before its child.
public sealed class BoneAnimationPlayer
{
    readonly SkinnedBoneData[] Bones;
    readonly AnimationClipData[] Clips;
    readonly int[] TraversalOrder; // parent-before-child indices into Bones

    public Matrix[] SkinningPalette { get; }
    readonly Matrix[] WorldPose;
    // Phase 3.10.B.8: per-bone inverse-bind-world matrix used for skinning.
    // Sourced from frame 0 of the bone's animation track in clip 0 when
    // available (the legacy XNA exporter writes its bind pose into frame 0
    // of the take, not into the cluster's TransformLinkMatrix). Falls back
    // to the bone's stored InverseBindPoseTransform for non-animated bones.
    readonly Matrix[] BindWorldInverse;

    public AnimationClipData CurrentClip { get; private set; }
    public float CurrentTime { get; private set; }
    public bool Looping { get; set; } = true;

    public bool HasBones => Bones.Length > 0;
    public bool HasClips => Clips.Length > 0;
    public int  NumBones => Bones.Length;

    public BoneAnimationPlayer(SkinnedBoneData[] bones, AnimationClipData[] clips)
    {
        Bones = bones ?? Array.Empty<SkinnedBoneData>();
        Clips = clips ?? Array.Empty<AnimationClipData>();
        SkinningPalette = new Matrix[Bones.Length];
        WorldPose = new Matrix[Bones.Length];
        BindWorldInverse = new Matrix[Bones.Length];
        TraversalOrder = ComputeTopologicalOrder(Bones);
        ComputeBindWorldInverse();
        ResetToBindPose();
    }

    // Phase 3.10.B.8: derive each bone's inverse-bind-world matrix.
    //
    // Skinning math is `skin = inverseBindWorld * currentWorld`. The
    // inverseBindWorld must reflect the SAME hierarchy chain that
    // Sample() walks at runtime, but anchored at the bind pose the
    // geometry was authored against — NOT frame 0 of the animation,
    // which can be different. Using frame 0 as bind makes vertices look
    // correct at t=0 but produces "broken limb" articulation through
    // the rest of the animation when bind ≠ frame 0 (Ralyeh ship17b
    // root bone: bind R = -90° around Z, frame 0 R = +90° around Z).
    //
    // Walks the bone hierarchy through BindPoseTranslation/Rotation/Scale
    // (which the cleaned-up exporter now writes correctly). Bones without
    // a usable inverse fall back to their stored InverseBindPoseTransform.
    void ComputeBindWorldInverse()
    {
        var bindWorld = new Matrix[Bones.Length];
        for (int idx = 0; idx < TraversalOrder.Length; idx++)
        {
            int i = TraversalOrder[idx];
            SkinnedBoneData bone = Bones[i];

            Matrix local = ComposeTRS(
                bone.BindPoseTranslation,
                EulerToQuat(bone.BindPoseRotation),
                bone.BindPoseScale);
            bindWorld[i] = bone.ParentIndex >= 0
                ? local * bindWorld[bone.ParentIndex]
                : local;

            BindWorldInverse[i] = TryInvert(bindWorld[i], out Matrix inv)
                ? inv
                : bone.InverseBindPoseTransform; // last-resort fallback
            if (HasNaN(BindWorldInverse[i]))
                BindWorldInverse[i] = Matrix.Identity;
        }
    }

    static BoneAnimationData FindTrackInClip(AnimationClipData clip, int boneIndex)
    {
        BoneAnimationData[] tracks = clip?.Animations;
        if (tracks == null) return null;
        for (int i = 0; i < tracks.Length; i++)
            if (tracks[i].SkinnedBoneIndex == boneIndex)
                return tracks[i];
        return null;
    }

    static bool TryInvert(Matrix m, out Matrix inv)
    {
        inv = Matrix.Invert(m);
        return !HasNaN(inv);
    }

    static bool HasNaN(Matrix m)
        => float.IsNaN(m.M11) || float.IsNaN(m.M22) || float.IsNaN(m.M33) || float.IsNaN(m.M44)
        || float.IsInfinity(m.M11) || float.IsInfinity(m.M22) || float.IsInfinity(m.M33) || float.IsInfinity(m.M44);

    // Kahn-style topological sort: emit roots first, then any bone whose
    // parent has already been emitted, until everyone's placed. A cycle
    // (which shouldn't exist in a real skeleton) drops the survivors in
    // input order to guarantee termination.
    static int[] ComputeTopologicalOrder(SkinnedBoneData[] bones)
    {
        int n = bones.Length;
        var order = new int[n];
        if (n == 0) return order;
        var emitted = new bool[n];
        int head = 0;
        while (head < n)
        {
            bool progress = false;
            for (int i = 0; i < n; i++)
            {
                if (emitted[i]) continue;
                int parent = bones[i].ParentIndex;
                if (parent < 0 || (parent < n && emitted[parent]))
                {
                    order[head++] = i;
                    emitted[i] = true;
                    progress = true;
                }
            }
            if (!progress)
            {
                for (int i = 0; i < n; i++)
                    if (!emitted[i]) { order[head++] = i; emitted[i] = true; }
                break;
            }
        }
        return order;
    }

    public void StartClip(int index)
    {
        if (index < 0 || index >= Clips.Length) return;
        CurrentClip = Clips[index];
        CurrentTime = 0f;
        Sample();
    }

    public void StartClip(string name)
    {
        for (int i = 0; i < Clips.Length; i++)
            if (Clips[i].Name == name) { StartClip(i); return; }
    }

    public void Update(float deltaTime)
    {
        if (CurrentClip == null) return;
        CurrentTime += deltaTime;
        float duration = CurrentClip.Duration;
        if (Looping && duration > 0f && CurrentTime >= duration)
            CurrentTime %= duration;
        else if (!Looping && duration > 0f && CurrentTime > duration)
            CurrentTime = duration;
        Sample();
    }

    void Sample()
    {
        // Iterate in topological order so every bone's parent transform is
        // already populated before we read it. Direct array-index iteration
        // would mis-root any bone whose parent has a higher index.
        for (int idx = 0; idx < TraversalOrder.Length; idx++)
        {
            int i = TraversalOrder[idx];
            SkinnedBoneData bone = Bones[i];
            SamplePose(bone, CurrentTime, out Vector3 t, out Quaternion r, out Vector3 s);
            Matrix local = ComposeTRS(t, r, s);
            WorldPose[i] = bone.ParentIndex >= 0
                ? local * WorldPose[bone.ParentIndex]
                : local;
            SkinningPalette[i] = SafeSkin(BindWorldInverse[i] * WorldPose[i]);
        }
    }

    public void ResetToBindPose()
    {
        // Compose worldPose from each bone's stored BindPose T/R/S (the same
        // values ComputeBindWorldInverse uses to build BindWorldInverse), so
        // skin = BindWorldInverse * worldPose reduces to identity for every
        // bone — vertices land at their authored bind position.
        CurrentTime = 0f;
        for (int idx = 0; idx < TraversalOrder.Length; idx++)
        {
            int i = TraversalOrder[idx];
            SkinnedBoneData bone = Bones[i];
            Matrix local = ComposeTRS(
                bone.BindPoseTranslation,
                EulerToQuat(bone.BindPoseRotation),
                bone.BindPoseScale);
            WorldPose[i] = bone.ParentIndex >= 0
                ? local * WorldPose[bone.ParentIndex]
                : local;
            SkinningPalette[i] = SafeSkin(BindWorldInverse[i] * WorldPose[i]);
        }
    }

    // Phase 3.10.B.8: defensive guard. The Ralyeh ship17a-f FBX corpus came
    // out of the legacy XNA exporter with degenerate bind-pose data — clusters
    // have zero TransformLinkMatrix, bone nodes have near-zero LclScaling,
    // and FBX SDK's own matrix evaluation returns NaN. Combining those with
    // skinning produced NaN clip-space → invisible ships. This fallback turns
    // any NaN-producing skin matrix into identity so the affected vertices
    // render at their bind position. Animation is silently disabled for those
    // bones; the ship is visible (static at bind pose) instead of gone.
    static Matrix SafeSkin(Matrix m)
    {
        if (float.IsNaN(m.M11) || float.IsNaN(m.M22) || float.IsNaN(m.M33) || float.IsNaN(m.M44)
         || float.IsInfinity(m.M11) || float.IsInfinity(m.M22) || float.IsInfinity(m.M33) || float.IsInfinity(m.M44))
            return Matrix.Identity;
        return m;
    }

    void SamplePose(SkinnedBoneData bone, float time,
                    out Vector3 translation, out Quaternion rotation, out Vector3 scale)
    {
        BoneAnimationData track = FindTrack(bone.BoneIndex);
        if (track == null || track.Frames == null || track.Frames.Length == 0)
        {
            translation = bone.BindPoseTranslation;
            rotation = EulerToQuat(bone.BindPoseRotation);
            scale = bone.BindPoseScale;
            return;
        }

        KeyFrameData[] frames = track.Frames;
        if (frames.Length == 1 || time <= frames[0].Time)
        {
            translation = frames[0].Translation;
            rotation = EulerToQuat(frames[0].Rotation);
            scale = frames[0].Scale;
            return;
        }
        if (time >= frames[frames.Length - 1].Time)
        {
            KeyFrameData last = frames[frames.Length - 1];
            translation = last.Translation;
            rotation = EulerToQuat(last.Rotation);
            scale = last.Scale;
            return;
        }

        // Linear scan; clip key counts are small (typically <60). If a future
        // mod ships hundreds of keys per bone, swap to binary search.
        int j = 0;
        while (j < frames.Length - 1 && frames[j + 1].Time <= time) j++;
        KeyFrameData f0 = frames[j];
        KeyFrameData f1 = frames[j + 1];
        float span = f1.Time - f0.Time;
        float u = span > 1e-6f ? (time - f0.Time) / span : 0f;
        translation = Vector3.Lerp(f0.Translation, f1.Translation, u);
        rotation = Quaternion.Slerp(EulerToQuat(f0.Rotation), EulerToQuat(f1.Rotation), u);
        scale = Vector3.Lerp(f0.Scale, f1.Scale, u);
    }

    BoneAnimationData FindTrack(int boneIndex)
    {
        BoneAnimationData[] tracks = CurrentClip?.Animations;
        if (tracks == null) return null;
        for (int i = 0; i < tracks.Length; i++)
            if (tracks[i].SkinnedBoneIndex == boneIndex)
                return tracks[i];
        return null;
    }

    static Matrix ComposeTRS(Vector3 t, Quaternion r, Vector3 s)
        => Matrix.CreateScale(s) * Matrix.CreateFromQuaternion(r) * Matrix.CreateTranslation(t);

    // FBX EulerXYZ DEGREES (NanoMesh's chosen storage): rotation order is
    // intrinsic X -> Y -> Z, equivalent to matrix M = Rz * Ry * Rx. XNA's
    // q1 * q2 means "apply q2 first, then q1", so qz * qy * qx composes the
    // same rotation. CreateFromYawPitchRoll uses YXZ order and would silently
    // mis-rotate any bone with non-zero combined Euler angles.
    static Quaternion EulerToQuat(Vector3 eulerDegrees)
    {
        float rx = MathHelper.ToRadians(eulerDegrees.X);
        float ry = MathHelper.ToRadians(eulerDegrees.Y);
        float rz = MathHelper.ToRadians(eulerDegrees.Z);
        Quaternion qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rx);
        Quaternion qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, ry);
        Quaternion qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rz);
        return qz * qy * qx;
    }
}
