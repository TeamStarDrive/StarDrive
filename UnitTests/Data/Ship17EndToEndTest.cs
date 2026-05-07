using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.B.8 second-pass: walk the actual production load chain on
    /// ship17a (RawContent.LoadStaticMesh → MeshImporter → CreateSceneObject)
    /// and assert each handoff in the chain is intact. If any of these fails
    /// in CI but the game still shows invisible ships, the failure is in the
    /// renderer / shader path; if any of these fails, we know the data side
    /// is broken before the GPU even sees it.
    /// </summary>
    [TestClass]
    public class Ship17EndToEndTest : StarDriveTest
    {
        [TestMethod]
        public void Ship17a_FullLoadChain_LandsAsSkinnedSO()
        {
            const string meshPath = "Model/Ships/Ralyeh/ship17a";

            StaticMesh mesh = StaticMesh.LoadMesh(Content, meshPath, animated: true);
            Assert.IsNotNull(mesh, "LoadMesh returned null");

            Assert.IsTrue(mesh.IsSkinned,
                $"StaticMesh.IsSkinned=false for ship17a — SkinnedBones={(mesh.SkinnedBones?.Length ?? 0)}, " +
                $"AnimationClips={(mesh.AnimationClips?.Length ?? 0)}, RawMeshes={mesh.RawMeshes.Count}");
            Assert.IsTrue(mesh.SkinnedBones.Length > 0, "SkinnedBones empty");
            Assert.IsTrue(mesh.AnimationClips != null && mesh.AnimationClips.Length > 0, "AnimationClips empty");
            Assert.IsFalse(mesh.RawMeshes.IsEmpty, "RawMeshes empty — MeshImporter likely fell into the catch path");

            // Material effect should be SkinnedLightingEffect for skinned hulls.
            // If it's plain LightingEffect, MeshImporter didn't propagate isSkinned.
            foreach (MeshData md in mesh.RawMeshes)
            {
                Assert.IsNotNull(md.Effect, $"RawMesh '{md.Name}' has no Effect");
                Assert.IsInstanceOfType(md.Effect, typeof(SkinnedLightingEffect),
                    $"RawMesh '{md.Name}' Effect is {md.Effect.GetType().Name}, expected SkinnedLightingEffect");
            }

            // Vertex declaration must include BlendIndices + BlendWeight.
            MeshData first = mesh.RawMeshes.First();
            var elements = first.VertexDeclaration.GetVertexElements();
            Assert.IsTrue(elements.Any(e => e.VertexElementUsage == Microsoft.Xna.Framework.Graphics.VertexElementUsage.BlendIndices),
                "Vertex declaration missing BlendIndices");
            Assert.IsTrue(elements.Any(e => e.VertexElementUsage == Microsoft.Xna.Framework.Graphics.VertexElementUsage.BlendWeight),
                "Vertex declaration missing BlendWeight");

            // SceneObject creation should auto-attach a player.
            SceneObject so = mesh.CreateSceneObject();
            Assert.IsNotNull(so, "CreateSceneObject returned null");
            Assert.IsNotNull(so.AnimationPlayer, "AnimationPlayer not attached");
            Assert.IsTrue(so.IsSkinned);
            Assert.IsNotNull(so.AnimationPlayer.CurrentClip, "CurrentClip not auto-started");

            // Phase 3.10.B.8 follow-up: dump bind-pose vs frame-0 for every
            // bone so we can see whether they actually agree (clean exporter
            // should make them match — XNAnimation's BindPose is what the
            // geometry is skinned against; frame 0 is just the start of the
            // animation, which may or may not match).
            var clip0 = mesh.AnimationClips[0];
            System.Console.WriteLine($"  -- clip[0] '{clip0.Name}' duration={clip0.Duration:F2}s tracks={clip0.Animations.Length} --");
            for (int b = 0; b < mesh.SkinnedBones.Length; b++)
            {
                SkinnedBoneData sb = mesh.SkinnedBones[b];
                var track = clip0.Animations.FirstOrDefault(a => a.SkinnedBoneIndex == sb.BoneIndex);
                var f0 = track?.Frames != null && track.Frames.Length > 0 ? track.Frames[0] : null;
                System.Console.WriteLine($"  bone[{b}] '{sb.Name}'");
                System.Console.WriteLine($"    bind  T={sb.BindPoseTranslation} R={sb.BindPoseRotation} S={sb.BindPoseScale}");
                if (f0 != null)
                    System.Console.WriteLine($"    frame0 T={f0.Translation} R={f0.Rotation} S={f0.Scale}");
            }

            // No skin matrix in the palette may contain NaN/Inf — vertices
            // weighted to a NaN bone clip the entire triangle out of frustum.
            so.AnimationPlayer.ResetToBindPose();
            for (int i = 0; i < so.AnimationPlayer.SkinningPalette.Length; i++)
            {
                Matrix m = so.AnimationPlayer.SkinningPalette[i];
                Assert.IsFalse(float.IsNaN(m.M11) || float.IsNaN(m.M22) || float.IsNaN(m.M33) || float.IsNaN(m.M44),
                    $"Skin matrix [{i}] contains NaN — vertices weighted to this bone will clip out");
                Assert.IsFalse(float.IsInfinity(m.M11) || float.IsInfinity(m.M22) || float.IsInfinity(m.M33) || float.IsInfinity(m.M44),
                    $"Skin matrix [{i}] contains Inf");
            }

            // Math invariant: BindWorldInverse is derived from the bone's
            // stored BindPose T/R/S, so ResetToBindPose (which composes the
            // SAME bind T/R/S into WorldPose) must produce identity skin
            // matrices for every bone. If this fires, the bind data flow
            // is desynchronized between ComputeBindWorldInverse and
            // ResetToBindPose.
            so.AnimationPlayer.ResetToBindPose();
            for (int i = 0; i < so.AnimationPlayer.SkinningPalette.Length; i++)
            {
                Matrix m = so.AnimationPlayer.SkinningPalette[i];
                Vector3 transformed = Vector3.Transform(new Vector3(1f, 2f, 3f), m);
                Assert.AreEqual(1f, transformed.X, 0.01f, $"bone[{i}] bind-pose skin should be ~identity (X)");
                Assert.AreEqual(2f, transformed.Y, 0.01f, $"bone[{i}] bind-pose skin should be ~identity (Y)");
                Assert.AreEqual(3f, transformed.Z, 0.01f, $"bone[{i}] bind-pose skin should be ~identity (Z)");
            }
        }
    }
}
