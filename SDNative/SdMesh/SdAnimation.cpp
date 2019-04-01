#include "SdAnimation.h"

namespace SdMesh
{
    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshAddBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone, 
                               const Matrix4& transform)
    {

    }

    DLLAPI(void) SDMeshAddSkinnedBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                      const SDBonePose& bindPose, const Matrix4& inverseBindPoseTransform)
    {

    }

    DLLAPI(SDAnimationClip*) SDMeshCreateAnimationClip(SDMesh* mesh, const wchar_t* name, float duration)
    {
        return nullptr;
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
