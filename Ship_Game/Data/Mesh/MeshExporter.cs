using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Texture;
using SDGraphics;
using SDUtils;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace Ship_Game.Data.Mesh
{
    // Phase 3.4 step 1: static-mesh export path restored from pre-Phase-1
    // (commit b893360a6^) and adapted to MonoGame 3.8.1.303's ModelMeshPart shape.
    // Skinned + animated paths (XNAnimation surface) are deferred to §3.5.
    // SunBurn `BaseMaterialEffect` material handling is deferred to §3.4 step 2
    // (LightingMaterialReader_Pro stub) — non-BasicEffect Effects emit a warn
    // and produce no SdMaterial for now.
    //
    // The end of the export path calls SDMeshSave which routes through NanoMesh's
    // FBX writer. NanoMesh is currently built with NANOMESH_NO_FBX=1 (Phase 1
    // carryover; FBX SDK 2018→2020 ABI swap is §3.2). Until §3.2 lands, SDMeshSave
    // is expected to return false; the C# walks above it still execute and unit
    // tests can pin the structural correctness.
    public class MeshExporter : MeshInterface
    {
        readonly TextureExporter TexExport;
        readonly Dictionary<Texture2D, string> AlreadySavedTextures = new();

        public MeshExporter(GameContentManager content) : base(content)
        {
            TexExport = new TextureExporter(Content);
        }

        public void Reset()
        {
            AlreadySavedTextures.Clear();
        }

        public bool IsAlreadySavedTexture(Texture2D tex) => tex != null && AlreadySavedTextures.ContainsKey(tex);

        public void AddAlreadySavedTexture(Texture2D tex, string texSavePath)
        {
            if (tex != null) AlreadySavedTextures[tex] = texSavePath;
        }

        public unsafe bool Export(Model model, string name, string modelFilePath)
        {
            if (model == null || model.Meshes.Count == 0)
                return false;

            string exportDir = Path.GetDirectoryName(modelFilePath) ?? "";
            if (exportDir.Length > 0)
                Directory.CreateDirectory(exportDir);

            SdMesh* mesh = SDMeshCreateEmpty(name);
            if (mesh == null)
            {
                Log.Warning($"MeshExporter.Export: SDMeshCreateEmpty('{name}') returned null");
                return false;
            }

            try
            {
                AddBones(mesh, model);
                Dictionary<Effect, IntPtr> materials = ExportMaterials(mesh, exportDir, model.Meshes);
                AddMeshGroups(mesh, model.Meshes, materials);
                return SDMeshSave(mesh, modelFilePath);
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        static unsafe void AddBones(SdMesh* mesh, Model model)
        {
            int count = model.Bones.Count;
            for (int i = 0; i < count; ++i)
            {
                ModelBone b = model.Bones[i];
                int parentIndex = b.Parent != null ? b.Parent.Index : -1;
                Matrix transform = new Matrix(b.Transform);
                SDMeshAddBone(mesh, b.Name ?? "", b.Index, parentIndex, in transform);
            }
        }

        unsafe void AddMeshGroups(SdMesh* mesh, ModelMeshCollection meshes, Dictionary<Effect, IntPtr> materials)
        {
            foreach (ModelMesh modelMesh in meshes)
            {
                XnaMatrix parentXform = modelMesh.ParentBone != null ? modelMesh.ParentBone.Transform : XnaMatrix.Identity;
                Matrix transform = new Matrix(parentXform);
                int partCount = modelMesh.MeshParts.Count;

                for (int i = 0; i < partCount; ++i)
                {
                    ModelMeshPart part = modelMesh.MeshParts[i];
                    if (part.VertexBuffer == null || part.IndexBuffer == null || part.NumVertices <= 0 || part.PrimitiveCount <= 0)
                        continue;

                    string groupName = (partCount > 1) ? modelMesh.Name + i : modelMesh.Name;
                    SdMeshGroup* group = SDMeshNewGroup(mesh, groupName ?? "", &transform);
                    if (group == null)
                        continue;

                    int stride = part.VertexBuffer.VertexDeclaration.VertexStride;
                    SdVertexElement[] layout = CreateVertexElements(part.VertexBuffer.VertexDeclaration);

                    SdVertexData data;
                    data.VertexStride = stride;
                    data.LayoutCount  = layout.Length;
                    data.IndexCount   = part.PrimitiveCount * 3;
                    data.VertexCount  = part.NumVertices;

                    // 16-bit index path matches XNA 3.1 baked content. 32-bit indices on a
                    // ship XNB would be unusual; warn and skip rather than risk mis-sized reads.
                    if (part.IndexBuffer.IndexElementSize != IndexElementSize.SixteenBits)
                    {
                        Log.Warning($"MeshExporter: skipping mesh part '{groupName}' — 32-bit index buffer not supported by SDNative ushort index path");
                        continue;
                    }

                    var indexData  = new ushort[data.IndexCount];
                    part.IndexBuffer.GetData(part.StartIndex * sizeof(ushort), indexData, 0, data.IndexCount);

                    var vertexData = new byte[data.VertexCount * stride];
                    part.VertexBuffer.GetData(part.VertexOffset * stride, vertexData, 0, vertexData.Length, 0);

                    fixed (ushort* pIndex = indexData)
                    fixed (byte* pVertex = vertexData)
                    fixed (SdVertexElement* pLayout = layout)
                    {
                        data.IndexData  = pIndex;
                        data.VertexData = pVertex;
                        data.Layout     = pLayout;
                        SDMeshGroupSetData(group, data);
                    }

                    Effect partEffect = part.Effect ?? (modelMesh.Effects.Count > 0 ? modelMesh.Effects[0] : null);
                    if (partEffect != null && materials.TryGetValue(partEffect, out IntPtr matPtr) && matPtr != IntPtr.Zero)
                    {
                        SDMeshGroupSetMaterial(group, (SdMaterial*)matPtr);
                    }
                }
            }
        }

        unsafe Dictionary<Effect, IntPtr> ExportMaterials(SdMesh* mesh, string exportDir, ModelMeshCollection meshes)
        {
            var exported = new Dictionary<Effect, IntPtr>();
            string meshName = mesh->Name.AsString;
            int dedupeIndex = 0;

            foreach (ModelMesh modelMesh in meshes)
            {
                int effectsCount = modelMesh.Effects.Count;
                for (int i = 0; i < effectsCount; ++i)
                {
                    Effect effect = modelMesh.Effects[i];
                    if (effect == null || exported.ContainsKey(effect))
                        continue;

                    if (effect is BasicEffect basic)
                    {
                        string matName = !string.IsNullOrEmpty(basic.Texture?.Name)
                            ? Path.GetFileNameWithoutExtension(basic.Texture.Name)
                            : meshName + dedupeIndex;
                        exported[effect] = (IntPtr)ExportMaterial(mesh, basic, matName, exportDir);
                    }
                    else
                    {
                        // Phase 3.4 step 2 (SunBurn LightingMaterialReader_Pro stub) lights up
                        // BaseMaterialEffect handling. Until then, non-BasicEffect content is
                        // exported geometry-only; the .fbx will lack texture references for
                        // those parts. Mod-authored Effects that aren't BasicEffect also fall
                        // through here.
                        Log.Info($"MeshExporter: skipping material for non-BasicEffect '{effect.GetType().Name}' (mesh '{meshName}', effect #{i}); §3.4 step 2 will restore SunBurn material handling");
                        exported[effect] = IntPtr.Zero;
                    }
                    ++dedupeIndex;
                }
            }
            return exported;
        }

        unsafe SdMaterial* ExportMaterial(SdMesh* mesh, BasicEffect fx, string matName, string exportDir)
        {
            string diffusePath = "";
            string specularPath = "";
            string normalPath = "";
            string emissivePath = "";

            if (fx.Texture == null)
            {
                // Mirror pre-Phase-1 convention: when an authored material had no runtime
                // texture bound (artist provided sidecar PNGs), reference standard
                // `_d/_s/_n/_g` siblings.
                string baseName = matName.NotEmpty() && char.IsLetter(matName[matName.Length - 1])
                                  ? matName.Substring(0, matName.Length - 1) : matName;
                diffusePath  = baseName + "_d.png";
                specularPath = baseName + "_s.png";
                normalPath   = baseName + "_n.png";
                emissivePath = baseName + "_g.png";
            }
            else
            {
                diffusePath = TrySaveTexture(exportDir, matName, matName + ".png", fx.Texture);
            }

            return SDMeshCreateMaterial(mesh, matName,
                diffusePath, alphaPath: "", specularPath, normalPath, emissivePath,
                new Vector3(fx.AmbientLightColor),
                new Vector3(fx.DiffuseColor),
                new Vector3(fx.SpecularColor),
                new Vector3(fx.EmissiveColor),
                fx.SpecularPower, fx.Alpha);
        }

        string TrySaveTexture(string exportDir, string matName, string textureName, Texture2D texture)
        {
            if (string.IsNullOrEmpty(textureName) || texture == null || string.IsNullOrEmpty(exportDir))
                return "";

            string writeTo = Path.Combine(exportDir, Path.GetFileName(textureName));
            writeTo = TexExport.GetSaveAutoFormatPath(texture, writeTo);

            // Texture2D.Save isn't thread-safe per-instance; lock on the texture so concurrent
            // export threads (if any) don't collide. Single-threaded today.
            lock (texture)
            {
                if (AlreadySavedTextures.TryGetValue(texture, out string already))
                    return Path.GetFileName(already);

                AlreadySavedTextures[texture] = writeTo;
                if (!File.Exists(writeTo))
                {
                    Log.Write(ConsoleColor.Green, $"  Export Mesh MaterialTex: {matName} {writeTo}");
                    TexExport.SaveAutoFormat(texture, writeTo);
                }
                return Path.GetFileName(writeTo);
            }
        }
    }
}
