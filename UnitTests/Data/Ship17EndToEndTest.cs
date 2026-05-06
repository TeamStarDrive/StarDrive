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

            // No skin matrix in the palette may contain NaN/Inf — that's the
            // hard requirement for the ship to render at all (NaN clip-space
            // would clip the entire mesh out of frustum). The B.4 SafeSkin
            // guard converts any NaN/Inf result to identity, so the ship
            // renders at bind pose even if FBX-side bind data is degenerate.
            so.AnimationPlayer.ResetToBindPose();
            for (int i = 0; i < so.AnimationPlayer.SkinningPalette.Length; i++)
            {
                Matrix m = so.AnimationPlayer.SkinningPalette[i];
                Assert.IsFalse(float.IsNaN(m.M11) || float.IsNaN(m.M22) || float.IsNaN(m.M33) || float.IsNaN(m.M44),
                    $"Skin matrix [{i}] contains NaN — vertices weighted to this bone will clip out");
                Assert.IsFalse(float.IsInfinity(m.M11) || float.IsInfinity(m.M22) || float.IsInfinity(m.M33) || float.IsInfinity(m.M44),
                    $"Skin matrix [{i}] contains Inf");
            }
        }
    }
}
