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
     * Adds a model bone to the mesh by passing pre-decomposed T/R/S directly.
     *
     * Why this exists alongside SDMeshAddBone(Matrix4): SDMeshAddBone calls
     * rpp::Matrix4::getRotationAngles(), which extracts Euler angles assuming
     * an rpp-built (column-vector intrinsic-XYZ) rotation matrix. XNA's
     * `Matrix` is row-vector with the same byte layout, so the bytes are
     * interpreted as the transposed (= inverse) rotation, and the extracted
     * Eulers come out NEGATED relative to the rotation the caller actually
     * authored. Bind poses written via SDMeshAddBone therefore disagreed with
     * keyframes (which the legacy MeshExporter converts via the standard
     * intrinsic-XYZ formula directly from XnaQuaternion), causing skinned
     * ships to articulate wrong (180° root flip + visibly broken limbs once
     * the runtime bind walk uses LclR rather than the historical frame-0
     * heuristic). This entry point lets the caller compute T/R/S in the same
     * convention the keyframe path uses, eliminating the drift at the source.
     */
    DLLAPI(void) SDMeshAddBoneTRS(SDMesh* mesh, const wchar_t* name, int boneIndex, int parentBone,
                                  const Nano::BonePose& bindPose);

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
