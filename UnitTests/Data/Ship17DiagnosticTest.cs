using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.B.8 diagnostic: dump the SDNative-visible skin/anim state
    /// for ship17a so we can see whether the legacy exporter actually wrote
    /// FbxSkin clusters + animation curves, or whether the FBX is static-
    /// looking from the runtime's POV. Runs as an MSTest so it shows up in
    /// `dotnet test` output — not asserting anything; this is purely a
    /// "what is the runtime seeing" probe.
    /// </summary>
    [TestClass]
    public class Ship17DiagnosticTest : MeshInterface
    {
        public Ship17DiagnosticTest() : base(null) { }

        [TestMethod]
        public unsafe void Probe_Ship17a_SkinAndAnimState()
        {
            string path = "Content/Model/Ships/Ralyeh/ship17a.fbx";
            Assert.IsTrue(File.Exists(path), $"ship17a.fbx not at '{Path.GetFullPath(path)}'");

            SdMesh* mesh = SDMeshOpen(path);
            Assert.IsTrue(mesh != null, "SDMeshOpen returned null for ship17a.fbx");
            try
            {
                Console.WriteLine("");
                Console.WriteLine("=== ship17a.fbx runtime probe ===");
                Console.WriteLine($"Name             : {mesh->Name.AsString}");
                Console.WriteLine($"NumGroups        : {mesh->NumGroups}");
                Console.WriteLine($"NumFaces         : {mesh->NumFaces}");
                Console.WriteLine($"NumModelBones    : {mesh->NumModelBones}");
                Console.WriteLine($"NumSkinnedBones  : {mesh->NumSkinnedBones}");
                Console.WriteLine($"NumAnimClips     : {mesh->NumAnimClips}");

                for (int gi = 0; gi < mesh->NumGroups; gi++)
                {
                    SdMeshGroup* g = SDMeshGetGroup(mesh, gi);
                    SdVertexData data = SDMeshGroupGetData(g);
                    Console.WriteLine("");
                    Console.WriteLine($"  -- group {gi} '{g->Name.AsString}' --");
                    Console.WriteLine($"  vertices: {data.VertexCount}, stride: {data.VertexStride}, indices: {data.IndexCount}");
                    Console.WriteLine($"  layout count: {data.LayoutCount}");
                    for (int li = 0; li < data.LayoutCount; li++)
                    {
                        SdVertexElement e = data.Layout[li];
                        Console.WriteLine($"    [{li}] offset={e.Offset} size={e.Size} fmt={e.NativeFormat} usage={e.NativeUsage}");
                    }
                }

                if (mesh->NumSkinnedBones > 0)
                {
                    for (int bi = 0; bi < mesh->NumSkinnedBones; bi++)
                    {
                        SdSkinnedBoneInfo sb = SDMeshGetSkinnedBone(mesh, bi);
                        Console.WriteLine($"  bone[{bi}] '{sb.Name.AsString}' boneIndex={sb.BoneIndex} parent={sb.ParentBone}");
                    }
                }
                if (mesh->NumAnimClips > 0)
                {
                    for (int ci = 0; ci < mesh->NumAnimClips; ci++)
                    {
                        SdAnimationClipInfo info = SDMeshGetAnimationClip(mesh, ci);
                        Console.WriteLine($"  clip[{ci}] '{info.Name.AsString}' duration={info.Duration:F2}s tracks={info.NumAnimations}");
                    }
                }
                Console.WriteLine("=== end probe ===");
                Console.WriteLine("");
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }
    }
}
