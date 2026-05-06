#pragma once
#include <Nano/Mesh.h>
#include "SdMaterial.h"

namespace SdMesh
{
    using rpp::Vector3;
    using rpp::Vector4;
    using rpp::Matrix4;
    using std::string;
    using std::vector;
    using std::unique_ptr;
    ////////////////////////////////////////////////////////////////////////////////////

    struct SDMesh;

    struct SDModelBone
    {
        strview Name;
        int BoneIndex;
        int ParentBone;
        Matrix4 Transform;
    };

    struct SDSkinnedBone
    {
        strview Name;
        int BoneIndex;
        int ParentBone;
        Nano::BonePose BindPose;
        Matrix4 InverseBindPoseTransform;
    };

    // Contains BoneAnimations
    struct SDAnimationClip { int Id; };

    // Contains AnimationKeyFrames
    struct SDBoneAnimation { int Id; };

    ////////////////////////////////////////////////////////////////////////////////////
    // Phase 3.10.B.2: read-side info structs returned by the new getters below.
    // These describe data already loaded into Nano::Mesh by NanoMesh's LoadFBX
    // (B.0/B.1) so the C# side can construct SkinnedMesh + animation runtime
    // state without owning native memory. Names are returned as strview slices
    // pointing into the std::string buffers held by Nano::Mesh; valid as long
    // as the SDMesh wrapper is alive (don't access after SDMeshClose).

    struct SDAnimationClipInfo
    {
        strview Name;
        float Duration;
        int NumAnimations;
    };

    struct SDBoneAnimationInfo
    {
        int SkinnedBoneIndex;
        int NumFrames;
    };

    struct SDAnimationKeyFrameInfo
    {
        float Time;
        Nano::BonePose Pose;
    };

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshAddBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                               const Matrix4& transform);

    /**
     * Adds a new skinned bone to the mesh' list of bones
     */
    DLLAPI(void) SDMeshAddSkinnedBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                      const Nano::BonePose& bindPose, const Matrix4& inverseBindPoseTransform);

    /**
     * Creates a new animation clip inside SDMesh
     * This clip is automatically freed once SDMesh is closed
     */
    DLLAPI(SDAnimationClip) SDMeshCreateAnimationClip(SDMesh* mesh, const wchar_t* name, float duration);

    /**
     * Creates a new animation channel inside the animation clip for a specific bone
     */
    DLLAPI(SDBoneAnimation) SDMeshAddBoneAnimation(SDMesh* mesh, SDAnimationClip clip, int skinnedBoneIndex);

    /**
     * Adds a bone transformation keyframe to the bone animation channel
     */
    DLLAPI(void) SDMeshAddAnimationKeyFrame(SDMesh* mesh, SDAnimationClip clip, SDBoneAnimation anim, const Nano::AnimationKeyFrame& keyFrame);

    ////////////////////////////////////////////////////////////////////////////////////
    // Phase 3.10.B.2 read-side getters. Counts come from the SDMesh struct's
    // NumSkinnedBones / NumAnimClips fields (already mirrored on the C# side).

    DLLAPI(SDSkinnedBone) SDMeshGetSkinnedBone(SDMesh* mesh, int index);
    DLLAPI(SDAnimationClipInfo) SDMeshGetAnimationClip(SDMesh* mesh, int clipIndex);
    DLLAPI(SDBoneAnimationInfo) SDMeshGetBoneAnimation(SDMesh* mesh, int clipIndex, int animIndex);
    DLLAPI(SDAnimationKeyFrameInfo) SDMeshGetAnimationKeyFrame(SDMesh* mesh, int clipIndex, int animIndex, int frameIndex);

    ////////////////////////////////////////////////////////////////////////////////////
}
