using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Rendering;
using XnaBoundingBox = Microsoft.Xna.Framework.BoundingBox;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.B.7 close-out: pin the auto-play wiring. A skinned StaticMesh
    /// turned into a SceneObject must come back with a BoneAnimationPlayer
    /// already started on its first clip; a static mesh comes back without one.
    /// Renderer + gameplay code rely on this so no call site has to remember
    /// to construct + StartClip the player explicitly.
    /// </summary>
    [TestClass]
    public class StaticMeshAutoPlayTests
    {
        static StaticMesh MakeSkinnedMesh()
        {
            var mesh = new StaticMesh("autoplay-test", new XnaBoundingBox(-Vector3.One, Vector3.One))
            {
                SkinnedBones = new[]
                {
                    new SkinnedBoneData
                    {
                        Name = "Root", BoneIndex = 0, ParentIndex = -1,
                        BindPoseTranslation = Vector3.Zero, BindPoseRotation = Vector3.Zero, BindPoseScale = Vector3.One,
                        InverseBindPoseTransform = Matrix.Identity,
                    },
                },
                AnimationClips = new[]
                {
                    new AnimationClipData
                    {
                        Name = "Idle", Duration = 1f,
                        Animations = new BoneAnimationData[0],
                    },
                },
            };
            return mesh;
        }

        [TestMethod]
        public void Skinned_CreateSceneObject_AutoStartsFirstClip()
        {
            using StaticMesh mesh = MakeSkinnedMesh();
            Assert.IsTrue(mesh.IsSkinned);

            SceneObject so = mesh.CreateSceneObject();
            Assert.IsNotNull(so);
            Assert.IsNotNull(so.AnimationPlayer, "skinned mesh must auto-attach a BoneAnimationPlayer");
            Assert.IsTrue(so.IsSkinned);
            Assert.IsNotNull(so.AnimationPlayer.CurrentClip,
                "first clip should be auto-started so gameplay code doesn't need to call StartClip");
            Assert.AreEqual("Idle", so.AnimationPlayer.CurrentClip.Name);
        }

        [TestMethod]
        public void Skinned_NoClips_PlayerExistsButNoCurrentClip()
        {
            using StaticMesh mesh = MakeSkinnedMesh();
            mesh.AnimationClips = null; // skin data without animation clips

            SceneObject so = mesh.CreateSceneObject();
            Assert.IsNotNull(so.AnimationPlayer, "bone palette is still useful for bind-pose rendering");
            Assert.IsNull(so.AnimationPlayer.CurrentClip);
        }

        [TestMethod]
        public void Static_CreateSceneObject_LeavesAnimationPlayerNull()
        {
            using var mesh = new StaticMesh("static-test", new XnaBoundingBox(-Vector3.One, Vector3.One));
            Assert.IsFalse(mesh.IsSkinned);

            SceneObject so = mesh.CreateSceneObject();
            Assert.IsNotNull(so);
            Assert.IsNull(so.AnimationPlayer, "static meshes must not allocate a player");
            Assert.IsFalse(so.IsSkinned);
        }

        [TestMethod]
        public void SceneObject_UpdateAnimation_TicksPlayer()
        {
            using StaticMesh mesh = MakeSkinnedMesh();
            SceneObject so = mesh.CreateSceneObject();
            Assert.AreEqual(0f, so.AnimationPlayer.CurrentTime);

            so.UpdateAnimation(0.25f);
            Assert.AreEqual(0.25f, so.AnimationPlayer.CurrentTime, 1e-5f,
                "SceneObject.UpdateAnimation must advance the player so existing per-frame call sites animate skinned hulls automatically");
        }
    }
}
