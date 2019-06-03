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

        mesh->TheMesh.SkinnedBones.emplace_back(Nano::SkinnedBone {
            boneIndex, parentBone, toString(name), 
            bindPose, inverseBindPoseTransform
        });
        mesh->NumSkinnedBones = (int)mesh->TheMesh.SkinnedBones.size();
    }

    DLLAPI(SDAnimationClip*) SDMeshCreateAnimationClip(SDMesh* mesh, const wchar_t* name, float duration)
    {
        assert(mesh != nullptr && "SDMeshCreateAnimationClip mesh cannot be null");

        string cName = toString(name);
        Nano::AnimationClip& theClip = mesh->TheMesh.AnimationClips[cName];
        theClip.Name = cName;
        theClip.Duration = duration;

        SDAnimationClip& clip = *mesh->Clips.emplace_back(new SDAnimationClip{ theClip });
        clip.Name = theClip.Name;
        clip.Duration = duration;

        mesh->NumAnimClips = (int)mesh->Clips.size();

        return &clip;
    }

    DLLAPI(SDBoneAnimation*) SDMeshAddBoneAnimation(SDAnimationClip* clip, int skinnedBoneIndex)
    {
        assert(clip != nullptr && "SDMeshAddBoneAnimation clip cannot be null");

        Nano::BoneAnimation& theAnim = clip->TheClip.Animations.emplace_back();
        theAnim.SkinnedBoneIndex = skinnedBoneIndex;

        SDBoneAnimation& anim = *clip->TheAnims.emplace_back(new SDBoneAnimation{ theAnim });
        anim.BoneIndex = theAnim.SkinnedBoneIndex;

        return &anim;
    }

    DLLAPI(void) SDMeshAddAnimationKeyFrame(SDBoneAnimation* anim, const Nano::AnimationKeyFrame& keyFrame)
    {
        assert(anim != nullptr && "SDMeshAddAnimationKeyFrame anim cannot be null");

        anim->TheAnim.Frames.push_back(keyFrame);
        anim->NumKeyFrames = (int)anim->TheAnim.Frames.size();
        anim->KeyFrames = anim->TheAnim.Frames.data();
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
