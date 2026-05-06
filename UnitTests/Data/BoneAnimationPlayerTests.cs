using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Mesh;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.B.4 close-out: pin the BoneAnimationPlayer contract.
    /// Bind-pose pre-Start, palette length = NumBones, linear translation
    /// lerp at midpoint, looping wrap, and parent-chain composition all
    /// verified against synthetic two-bone skeleton + 2-key clip data.
    /// </summary>
    [TestClass]
    public class BoneAnimationPlayerTests
    {
        static SkinnedBoneData[] MakeRootChildSkeleton()
        {
            return new[]
            {
                new SkinnedBoneData
                {
                    Name = "Root", BoneIndex = 0, ParentIndex = -1,
                    BindPoseTranslation = Vector3.Zero,
                    BindPoseRotation = Vector3.Zero,
                    BindPoseScale = Vector3.One,
                    InverseBindPoseTransform = Matrix.Identity,
                },
                new SkinnedBoneData
                {
                    Name = "Child", BoneIndex = 1, ParentIndex = 0,
                    BindPoseTranslation = new Vector3(5f, 0f, 0f),
                    BindPoseRotation = Vector3.Zero,
                    BindPoseScale = Vector3.One,
                    InverseBindPoseTransform = Matrix.CreateTranslation(-5f, 0f, 0f),
                },
            };
        }

        static AnimationClipData MakeRootSlideClip(float duration = 1f)
        {
            // One track on the root that slides from x=0 to x=10.
            return new AnimationClipData
            {
                Name = "Slide",
                Duration = duration,
                Animations = new[]
                {
                    new BoneAnimationData
                    {
                        SkinnedBoneIndex = 0,
                        Frames = new[]
                        {
                            new KeyFrameData { Time = 0f,        Translation = Vector3.Zero,             Rotation = Vector3.Zero, Scale = Vector3.One },
                            new KeyFrameData { Time = duration,  Translation = new Vector3(10f, 0f, 0f), Rotation = Vector3.Zero, Scale = Vector3.One },
                        },
                    },
                },
            };
        }

        [TestMethod]
        public void Player_PreStart_HoldsBindPose()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var player = new BoneAnimationPlayer(bones, new[] { MakeRootSlideClip() });

            Assert.AreEqual(2, player.NumBones, "palette length should mirror bone count");
            Assert.AreEqual(2, player.SkinningPalette.Length);
            Assert.IsNull(player.CurrentClip, "no clip until StartClip is called");

            // Root bind = identity; identity * identity = identity
            Assert.AreEqual(Matrix.Identity, player.SkinningPalette[0],
                "root bind-pose skin matrix should be identity");

            // Child: world = T(5,0,0); inverseBind = T(-5,0,0); skin = T(-5)*T(5) = identity
            // (skinning palette in bind pose should always reduce to identity by definition)
            Matrix childSkin = player.SkinningPalette[1];
            Vector3 testPoint = new(5f, 0f, 0f);
            Vector3 transformed = Vector3.Transform(testPoint, childSkin);
            Assert.AreEqual(testPoint.X, transformed.X, 0.0001f,
                "bind-pose skin must leave a vertex at its bind position unchanged");
        }

        [TestMethod]
        public void Player_AtMidpoint_LerpsTranslation()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var player = new BoneAnimationPlayer(bones, new[] { MakeRootSlideClip(duration: 1f) });

            player.StartClip(0);
            Assert.IsNotNull(player.CurrentClip);
            Assert.AreEqual(0f, player.CurrentTime, 1e-5f);

            player.Update(0.5f);
            Assert.AreEqual(0.5f, player.CurrentTime, 1e-5f);

            // Root track lerps x: 0 -> 10 over [0,1], so at t=0.5 root pose translation = (5,0,0)
            // Root bind inverse = identity, so skin[0].Translation should read (5,0,0).
            Matrix rootSkin = player.SkinningPalette[0];
            Assert.AreEqual(5f, rootSkin.Translation.X, 0.001f, "root translation lerp at midpoint");
            Assert.AreEqual(0f, rootSkin.Translation.Y, 0.001f);
            Assert.AreEqual(0f, rootSkin.Translation.Z, 0.001f);
        }

        [TestMethod]
        public void Player_ParentChainPropagates()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var player = new BoneAnimationPlayer(bones, new[] { MakeRootSlideClip(duration: 1f) })
            {
                Looping = false, // a looping Update(1) would wrap to t=0, sampling the start frame
            };

            player.StartClip(0);
            player.Update(1f);   // root translated to (10,0,0)

            // Child bind-world = T(5,0,0), so child current-world = parent.current * child.local
            //                  = T(10,0,0) * T(5,0,0) = T(15,0,0)
            // skin[child] = inverseBind(child) * world(child) = T(-5,0,0) * T(15,0,0) = T(10,0,0)
            // Vertex at child's bind position (5,0,0) ends up at (5+10,0,0) = (15,0,0).
            Matrix childSkin = player.SkinningPalette[1];
            Vector3 transformed = Vector3.Transform(new Vector3(5f, 0f, 0f), childSkin);
            Assert.AreEqual(15f, transformed.X, 0.001f,
                "child should follow parent translation through hierarchy");
        }

        [TestMethod]
        public void Player_LoopingWrapsTime()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var player = new BoneAnimationPlayer(bones, new[] { MakeRootSlideClip(duration: 1f) })
            {
                Looping = true,
            };
            player.StartClip(0);
            player.Update(2.25f); // 2.25 % 1 = 0.25
            Assert.AreEqual(0.25f, player.CurrentTime, 1e-4f);
        }

        [TestMethod]
        public void Player_NonLoopingClampsAtEnd()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var player = new BoneAnimationPlayer(bones, new[] { MakeRootSlideClip(duration: 1f) })
            {
                Looping = false,
            };
            player.StartClip(0);
            player.Update(5f);
            Assert.AreEqual(1f, player.CurrentTime, 1e-4f, "non-looping clip should clamp at duration");
        }

        [TestMethod]
        public void Player_StartClipByName_FindsClip()
        {
            SkinnedBoneData[] bones = MakeRootChildSkeleton();
            var clipA = new AnimationClipData { Name = "A", Duration = 1f, Animations = new BoneAnimationData[0] };
            var clipB = new AnimationClipData { Name = "B", Duration = 2f, Animations = new BoneAnimationData[0] };
            var player = new BoneAnimationPlayer(bones, new[] { clipA, clipB });

            player.StartClip("B");
            Assert.AreSame(clipB, player.CurrentClip);
        }

        [TestMethod]
        public void Player_OutOfOrderParents_TopologicallySorts()
        {
            // Phase 3.10.B.8 regression: ship17a's exporter writes bones in
            // arbitrary order — bone[0] has parentIndex=6, etc. A naive forward
            // sweep mis-roots them and produces garbage skin matrices, making
            // the entire ship clip out of frustum (the "invisible ships" bug).
            // This skeleton mimics the failure shape: bone 0 is a deep child
            // and bone 1 is the root.
            //
            //  bone[0]  child of bone[2]    bind T = (10,0,0)  inverseBind = T(-10,0,0)
            //  bone[1]  root                bind T = (0,0,0)
            //  bone[2]  child of bone[1]    bind T = (5,0,0)   inverseBind = T(-5,0,0)
            var bones = new[]
            {
                new SkinnedBoneData
                {
                    Name = "Tip", BoneIndex = 0, ParentIndex = 2,
                    BindPoseTranslation = new Vector3(5f, 0f, 0f), // local offset from parent
                    BindPoseRotation = Vector3.Zero,
                    BindPoseScale = Vector3.One,
                    // Bind world for tip = bone1.world * bone2.local * bone0.local = T(10,0,0)
                    InverseBindPoseTransform = Matrix.CreateTranslation(-10f, 0f, 0f),
                },
                new SkinnedBoneData
                {
                    Name = "Root", BoneIndex = 1, ParentIndex = -1,
                    BindPoseTranslation = Vector3.Zero,
                    BindPoseRotation = Vector3.Zero,
                    BindPoseScale = Vector3.One,
                    InverseBindPoseTransform = Matrix.Identity,
                },
                new SkinnedBoneData
                {
                    Name = "Mid", BoneIndex = 2, ParentIndex = 1,
                    BindPoseTranslation = new Vector3(5f, 0f, 0f),
                    BindPoseRotation = Vector3.Zero,
                    BindPoseScale = Vector3.One,
                    InverseBindPoseTransform = Matrix.CreateTranslation(-5f, 0f, 0f),
                },
            };

            var player = new BoneAnimationPlayer(bones, null);

            // In bind pose every skin matrix must reduce to identity. Specifically
            // for bone[0]: a vertex at bind position (10,0,0) must transform to (10,0,0).
            Vector3 bindPos = new(10f, 0f, 0f);
            Vector3 transformed = Vector3.Transform(bindPos, player.SkinningPalette[0]);
            Assert.AreEqual(bindPos.X, transformed.X, 1e-3f,
                "out-of-order bone must still produce identity skin in bind pose");
            Assert.AreEqual(bindPos.Y, transformed.Y, 1e-3f);
            Assert.AreEqual(bindPos.Z, transformed.Z, 1e-3f);
        }

        [TestMethod]
        public void Player_NoBones_ProducesEmptyPaletteSafely()
        {
            var player = new BoneAnimationPlayer(null, null);
            Assert.IsFalse(player.HasBones);
            Assert.IsFalse(player.HasClips);
            Assert.AreEqual(0, player.SkinningPalette.Length);
            player.Update(1f); // must be a safe no-op
        }
    }
}
