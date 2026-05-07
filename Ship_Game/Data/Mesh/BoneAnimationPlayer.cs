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
    // Sample() walks at runtime — otherwise frame 0 doesn't reduce to
    // identity and the ship renders displaced.
    //
    // The legacy XNA exporter wrote its bind pose into frame 0 of the
    // animation take, not into the FBX cluster's TransformLinkMatrix
    // (those came back as zero/singular). So we walk the bone hierarchy
    // using each bone's frame-0 keyframe T/R/S as its bind-pose local
    // transform, and invert the resulting bind world. Bones without
    // animation tracks fall back to their loaded InverseBindPoseTransform
    // (NanoMesh's reader synthesizes identity for those when the cluster
    // matrix is degenerate).
    void ComputeBindWorldInverse()
    {
        AnimationClipData clip0 = Clips.Length > 0 ? Clips[0] : null;
        var bindWorld = new Matrix[Bones.Length];
        for (int idx = 0; idx < TraversalOrder.Length; idx++)
        {
            int i = TraversalOrder[idx];
            SkinnedBoneData bone = Bones[i];

            Vector3 t; Quaternion r; Vector3 s;
            BoneAnimationData track = FindTrackInClip(clip0, bone.BoneIndex);
            if (track != null && track.Frames != null && track.Frames.Length > 0)
            {
                KeyFrameData f0 = track.Frames[0];
                t = f0.Translation;
                r = EulerToQuat(f0.Rotation);
                s = f0.Scale;
            }
            else
            {
                t = bone.BindPoseTranslation;
                r = EulerToQuat(bone.BindPoseRotation);
                s = bone.BindPoseScale;
            }

            Matrix local = ComposeTRS(t, r, s);
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
        // CurrentTime=0 + Sample() uses frame-0 keyframe values which by
        // definition equal the bind world we computed in ComputeBindWorldInverse.
        // Skin matrices reduce to identity → vertices stay at bind position.
        CurrentTime = 0f;
        if (CurrentClip != null)
        {
            Sample();
        }
        else
        {
            // No clip set yet (pre-StartClip). Re-derive from frame 0 directly.
            AnimationClipData clip0 = Clips.Length > 0 ? Clips[0] : null;
            for (int idx = 0; idx < TraversalOrder.Length; idx++)
            {
                int i = TraversalOrder[idx];
                SkinnedBoneData bone = Bones[i];
                Vector3 t; Quaternion r; Vector3 s;
                BoneAnimationData track = FindTrackInClip(clip0, bone.BoneIndex);
                if (track != null && track.Frames != null && track.Frames.Length > 0)
                {
                    KeyFrameData f0 = track.Frames[0];
                    t = f0.Translation;
                    r = EulerToQuat(f0.Rotation);
                    s = f0.Scale;
                }
                else
                {
                    t = bone.BindPoseTranslation;
                    r = EulerToQuat(bone.BindPoseRotation);
                    s = bone.BindPoseScale;
                }
                Matrix local = ComposeTRS(t, r, s);
                WorldPose[i] = bone.ParentIndex >= 0
                    ? local * WorldPose[bone.ParentIndex]
                    : local;
                SkinningPalette[i] = SafeSkin(BindWorldInverse[i] * WorldPose[i]);
            }
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
