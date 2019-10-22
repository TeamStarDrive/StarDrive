#include "SdAnimation.h"
#include "SdMesh.h"
#include <cassert>

namespace SdMesh
{
    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshAddBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone, 
                               const Matrix4& transform)
    {
        assert(mesh != nullptr && "SDMeshAddBone mesh cannot be null");

        mesh->TheMesh.Bones.emplace_back(Nano::MeshBone {
            boneIndex, parentBone, toString(name),
            Nano::BonePose {
                transform.getTranslation(),
                transform.getScale(),
                transform.getRotationAngles()
            }
        });
    }

    DLLAPI(void) SDMeshAddSkinnedBone(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                      const Nano::BonePose& bindPose, const Matrix4& inverseBindPoseTransform)
    {
        assert(mesh != nullptr && "SDMeshAddSkinnedBone mesh cannot be null");
        assert(name != nullptr && "SDMeshAddSkinnedBone name cannot be null");

        mesh->TheMesh.SkinnedBones.emplace_back(Nano::SkinnedBone{
            boneIndex, parentBone, toString(name), 
            bindPose, inverseBindPoseTransform
        });
        mesh->NumSkinnedBones = (int)mesh->TheMesh.SkinnedBones.size();
    }

    DLLAPI(SDAnimationClip) SDMeshCreateAnimationClip(SDMesh* mesh, const wchar_t* name, float duration)
    {
        assert(mesh != nullptr && "SDMeshCreateAnimationClip mesh cannot be null");
        assert(name != nullptr && "SDMeshCreateAnimationClip name cannot be null");

        int id = mesh->TheMesh.AddAnimClip(toString(name), duration);
        mesh->NumAnimClips = mesh->TheMesh.TotalAnimClips();
        return SDAnimationClip{ id };
    }

    DLLAPI(SDBoneAnimation) SDMeshAddBoneAnimation(SDMesh* mesh, SDAnimationClip clip, int skinnedBoneIndex)
    {
        assert(mesh != nullptr && "SDMeshAddBoneAnimation mesh cannot be null");

        Nano::AnimationClip& theClip = mesh->TheMesh.AnimationClips[clip.Id];
        int animId = (int)theClip.Animations.size();
        Nano::BoneAnimation& theAnim = theClip.Animations.emplace_back();
        theAnim.SkinnedBoneIndex = skinnedBoneIndex;

        return SDBoneAnimation{ animId };
    }

    DLLAPI(void) SDMeshAddAnimationKeyFrame(SDMesh* mesh, SDAnimationClip clip, SDBoneAnimation anim, const Nano::AnimationKeyFrame& keyFrame)
    {
        assert(mesh != nullptr && "SDMeshAddAnimationKeyFrame mesh cannot be null");

        Nano::AnimationClip& theClip = mesh->TheMesh.AnimationClips[clip.Id];
        Nano::BoneAnimation& theAnim = theClip.Animations[anim.Id];
        theAnim.Frames.push_back(keyFrame);
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
