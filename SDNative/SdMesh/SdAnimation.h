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
}
