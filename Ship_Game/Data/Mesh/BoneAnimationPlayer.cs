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
// Hierarchy: parent indices are guaranteed less than child indices by
// FBX's depth-first writer, so a single forward sweep computes worldPose.
public sealed class BoneAnimationPlayer
{
    readonly SkinnedBoneData[] Bones;
    readonly AnimationClipData[] Clips;

    public Matrix[] SkinningPalette { get; }
    readonly Matrix[] WorldPose;

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
        ResetToBindPose();
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
        for (int i = 0; i < Bones.Length; i++)
        {
            SkinnedBoneData bone = Bones[i];
            SamplePose(bone, CurrentTime, out Vector3 t, out Quaternion r, out Vector3 s);
            Matrix local = ComposeTRS(t, r, s);
            WorldPose[i] = (bone.ParentIndex >= 0 && bone.ParentIndex < i)
                ? local * WorldPose[bone.ParentIndex]
                : local;
            SkinningPalette[i] = bone.InverseBindPoseTransform * WorldPose[i];
        }
    }

    public void ResetToBindPose()
    {
        for (int i = 0; i < Bones.Length; i++)
        {
            SkinnedBoneData b = Bones[i];
            Matrix local = ComposeTRS(b.BindPoseTranslation, EulerToQuat(b.BindPoseRotation), b.BindPoseScale);
            WorldPose[i] = (b.ParentIndex >= 0 && b.ParentIndex < i)
                ? local * WorldPose[b.ParentIndex]
                : local;
            SkinningPalette[i] = b.InverseBindPoseTransform * WorldPose[i];
        }
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
