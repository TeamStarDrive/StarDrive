#pragma once
#include "SdMesh.h"

namespace SdMesh
{
    ////////////////////////////////////////////////////////////////////////////////////

    struct SDBonePose
    {
        Vector3 Translation;
        Vector4 Orientation; // Quaternion
        Vector3 Scale;
    };

    struct SDModelBone
    {
        const char* Name;
        int BoneIndex;
        int ParentBone;
        SDBonePose Pose;
    };

    struct SDSkinnedBone
    {
        const char* Name;
        int BoneIndex;
        int ParentBone;
        SDBonePose BindPose;
        Matrix4 InverseBindPoseTransform;
    };

    struct SDBoneAnimation
    {
        const char* BoneName;
        int FrameCount;
    };

    struct SDAnimationClip
    {
        // publicly visible to C#
        strview Name;
        float Duration;
        int NumAnimations;

        // hidden
        string TheName;
    };

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshAddBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                               const Matrix4& transform);

    DLLAPI(void) SDMeshAddSkinnedBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                      const SDBonePose& bindPose, const Matrix4& inverseBindPoseTransform);

    /**
     * Creates a new animation clip inside SDMesh
     * This clip is automatically freed once SDMesh is closed
     */
    DLLAPI(SDAnimationClip*) SDMeshCreateAnimationClip(SDMesh* mesh, const wchar_t* name, float duration);

    ////////////////////////////////////////////////////////////////////////////////////
}
