using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data.Mesh;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.4 step 1: structural pin for the restored static-mesh export path
    /// in <see cref="MeshExporter"/>. Until §3.2 (FBX SDK 2018→2020) un-stubs the
    /// NanoMesh FBX writer, <c>SDMeshSave</c> returns false and no .fbx hits disk;
    /// these tests therefore pin the pieces the C# walks must do correctly:
    /// the export directory is created, no exception escapes, and the materials /
    /// bone walks execute without throwing for a synthetic single-quad model.
    /// </summary>
    [TestClass]
    public class MeshExporterStaticPathTests : StarDriveTest
    {
        const string ExportRoot = "MeshExport/Phase3_Step1";

        [TestMethod]
        public void Export_NullModel_ReturnsFalse()
        {
            var exporter = new MeshExporter(Content);
            Assert.IsFalse(exporter.Export(null, "null-model", ExportRoot + "/null/x.fbx"),
                "Export(null) must short-circuit to false without throwing");
        }

        [TestMethod]
        public void Export_EmptyModel_ReturnsFalse()
        {
            // Model() default ctor leaves Meshes as a populated empty collection only after
            // reflection priming — synthesize via the same helper used for the quad test.
            Model empty = BuildModel(new List<ModelMesh>(), new List<ModelBone>());
            var exporter = new MeshExporter(Content);
            Assert.IsFalse(exporter.Export(empty, "empty-model", ExportRoot + "/empty/x.fbx"),
                "Empty Model.Meshes must short-circuit to false");
        }

        [TestMethod]
        public void Export_SingleQuadBasicEffect_RunsAllWalks()
        {
            string outPath = Path.Combine(ExportRoot, "quad", "quad.fbx");
            // Pre-clean: don't depend on prior runs.
            string outDir = Path.GetDirectoryName(outPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);

            Model quad = BuildSingleQuadModel(Content.Manager.GraphicsDevice);
            var exporter = new MeshExporter(Content);

            // Return value is `false` while NanoMesh's FBX writer is stubbed (§3.2 dependency).
            // What matters here is that the bone walk, vertex pull, material walk, and
            // SDMeshGroupSetData calls all complete without throwing.
            bool result = exporter.Export(quad, "Quad", outPath);

            Assert.IsTrue(Directory.Exists(outDir),
                $"Export should have created '{outDir}' before reaching SDMeshSave");
            // The result is allowed to be false (NanoMesh stubbed) but should NOT throw.
            // No assertion on `result` itself — pinning to `false` would silently fail to
            // light up once §3.2 lands and SDMeshSave starts succeeding.
            _ = result;
        }

        // Synthesize a 1-bone, 1-mesh, 1-part Model containing a 4-vert quad with BasicEffect.
        // MonoGame's Model/ModelMesh internals are mostly settable via property backing fields
        // and simple constructors; this stays closer to the runtime data shape than mocking.
        static Model BuildSingleQuadModel(GraphicsDevice device)
        {
            // 4 verts, position-only stride = 12 bytes
            var vertices = new[]
            {
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3( 1, -1, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3( 1,  1, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-1,  1, 0), new Vector2(0, 0)),
            };
            var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };

            var vb = new VertexBuffer(device, VertexPositionTexture.VertexDeclaration, vertices.Length, BufferUsage.None);
            vb.SetData(vertices);
            var ib = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.None);
            ib.SetData(indices);

            var part = new ModelMeshPart();
            SetBackingField(part, "VertexBuffer", vb);
            SetBackingField(part, "IndexBuffer", ib);
            SetBackingField(part, "VertexOffset", 0);
            SetBackingField(part, "NumVertices", vertices.Length);
            SetBackingField(part, "StartIndex", 0);
            SetBackingField(part, "PrimitiveCount", indices.Length / 3);
            SetField(part, "_effect", new BasicEffect(device) { TextureEnabled = false });

            var bone = new ModelBone();
            SetBackingField(bone, "Index", 0);
            SetBackingField(bone, "Name", "Root");
            SetBackingField(bone, "Children", new ModelBoneCollection(new List<ModelBone>()));
            SetField(bone, "transform", Matrix.Identity);

            var mesh = new ModelMesh(device, new List<ModelMeshPart> { part });
            SetBackingField(mesh, "Name", "QuadMesh");
            SetBackingField(mesh, "ParentBone", bone);

            return BuildModel(new List<ModelMesh> { mesh }, new List<ModelBone> { bone });
        }

        static Model BuildModel(List<ModelMesh> meshes, List<ModelBone> bones)
        {
            // The 3-arg internal constructor is the cleanest route; it wraps Bones/Meshes
            // in their proper collections and wires Root.
            ConstructorInfo ctor = typeof(Model).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(GraphicsDevice), typeof(List<ModelBone>), typeof(List<ModelMesh>) },
                modifiers: null);
            Assert.IsNotNull(ctor, "MonoGame Model 3-arg ctor not found — internal API may have shifted");
            return (Model)ctor.Invoke(new object[] { Content.Manager.GraphicsDevice, bones, meshes });
        }

        static void SetBackingField(object instance, string propName, object value)
        {
            string backing = $"<{propName}>k__BackingField";
            FieldInfo f = instance.GetType().GetField(backing, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"{instance.GetType().Name}.{propName} backing field '{backing}' not found");
            f.SetValue(instance, value);
        }

        static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"{instance.GetType().Name}.{fieldName} not found");
            f.SetValue(instance, value);
        }
    }
}
