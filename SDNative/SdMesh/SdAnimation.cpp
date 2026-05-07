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

        // Phase 3.10.B.8: Nano::BonePose is { Translation, Rotation, Scale } — the
        // original positional initializer had Scale/RotationAngles swapped, which
        // turned every bone's stored "Rotation" into a unit-scale-shaped (1,1,1)
        // and every "Scale" into the actual Euler angles. The bone-node LclTRS
        // emitted by the FBX writer was therefore garbage — clusters had
        // degenerate TransformLinkMatrix and migration-side runtimes had to
        // bypass the bind data via frame-0 keyframe heuristics.
        mesh->TheMesh.Bones.emplace_back(Nano::MeshBone {
            boneIndex, parentBone, toString(name),
            Nano::BonePose {
                transform.getTranslation(),
                transform.getRotationAngles(),
                transform.getScale()
            }
        });
    }

    // Phase 3.10.B.8 follow-up: T/R/S-direct model-bone entry point. See header
    // comment above SDMeshAddBoneTRS for the convention-mismatch this fixes.
    DLLAPI(void) SDMeshAddBoneTRS(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                  const Nano::BonePose& bindPose)
    {
        assert(mesh != nullptr && "SDMeshAddBoneTRS mesh cannot be null");
        assert(name != nullptr && "SDMeshAddBoneTRS name cannot be null");

        mesh->TheMesh.Bones.emplace_back(Nano::MeshBone {
            boneIndex, parentBone, toString(name),
            bindPose
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
    // Phase 3.10.B.2: read-side getters for SkinnedBones + AnimationClips that
    // NanoMesh's LoadFBX (B.0/B.1) populated. Caller bounds-checks via the
    // count fields exposed on the SDMesh struct (NumSkinnedBones / NumAnimClips).

    DLLAPI(SDSkinnedBone) SDMeshGetSkinnedBone(SDMesh* mesh, int index)
    {
        assert(mesh != nullptr && "SDMeshGetSkinnedBone mesh cannot be null");
        const Nano::SkinnedBone& sb = mesh->TheMesh.SkinnedBones[index];
        return SDSkinnedBone {
            strview { sb.Name.data(), (int)sb.Name.size() },
            sb.BoneIndex,
            sb.ParentIndex,
            sb.Pose,
            sb.InverseBindPoseTransform
        };
    }

    DLLAPI(SDAnimationClipInfo) SDMeshGetAnimationClip(SDMesh* mesh, int clipIndex)
    {
        assert(mesh != nullptr && "SDMeshGetAnimationClip mesh cannot be null");
        const Nano::AnimationClip& clip = mesh->TheMesh.AnimationClips[clipIndex];
        return SDAnimationClipInfo {
            strview { clip.Name.data(), (int)clip.Name.size() },
            clip.Duration,
            (int)clip.Animations.size()
        };
    }

    DLLAPI(SDBoneAnimationInfo) SDMeshGetBoneAnimation(SDMesh* mesh, int clipIndex, int animIndex)
    {
        assert(mesh != nullptr && "SDMeshGetBoneAnimation mesh cannot be null");
        const Nano::BoneAnimation& anim = mesh->TheMesh.AnimationClips[clipIndex].Animations[animIndex];
        return SDBoneAnimationInfo {
            anim.SkinnedBoneIndex,
            (int)anim.Frames.size()
        };
    }

    DLLAPI(SDAnimationKeyFrameInfo) SDMeshGetAnimationKeyFrame(SDMesh* mesh, int clipIndex, int animIndex, int frameIndex)
    {
        assert(mesh != nullptr && "SDMeshGetAnimationKeyFrame mesh cannot be null");
        const Nano::AnimationKeyFrame& kf = mesh->TheMesh.AnimationClips[clipIndex].Animations[animIndex].Frames[frameIndex];
        return SDAnimationKeyFrameInfo {
            kf.Time,
            kf.Pose
        };
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
