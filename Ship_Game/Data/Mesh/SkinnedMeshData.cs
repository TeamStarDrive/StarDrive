using Microsoft.Xna.Framework;

namespace Ship_Game.Data.Mesh;

// Phase 3.10.B.3: portable C# data structures for the skin + animation
// payload that NanoMesh's LoadFBX (B.0/B.1) populates and SDNative's
// read-side getters (B.2) surface. These are owned by StaticMesh; static
// meshes leave them null. The runtime animation player (B.4+) consumes
// them per-frame.
//
// All rotations are Euler XYZ degrees (matching Nano::BonePose). The
// player converts to a quaternion at sample time for slerp blending.

public sealed class SkinnedBoneData
{
    public string Name;
    public int BoneIndex;
    public int ParentIndex;          // -1 for root bones
    public Vector3 BindPoseTranslation;
    public Vector3 BindPoseRotation; // Euler XYZ DEGREES
    public Vector3 BindPoseScale;
    public Matrix InverseBindPoseTransform;
}

public sealed class KeyFrameData
{
    public float Time;               // seconds
    public Vector3 Translation;
    public Vector3 Rotation;         // Euler XYZ DEGREES
    public Vector3 Scale;
}

public sealed class BoneAnimationData
{
    public int SkinnedBoneIndex;
    public KeyFrameData[] Frames;    // sorted by Time ascending
}

public sealed class AnimationClipData
{
    public string Name;
    public float Duration;           // seconds
    public BoneAnimationData[] Animations;
}
