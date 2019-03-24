using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    /// <summary>
    /// Helper class for GameContentManager
    /// Allows loading FBX, OBJ and PNG files instead of .XNB content
    /// </summary>
    public class RawContentLoader
    {
        readonly GameContentManager Content;

        // This must be lazy init, because content manager is instantianted before
        // graphics device is initialized
        public GraphicsDevice Device => Content.Manager.GraphicsDevice;

        public RawContentLoader(GameContentManager content)
        {
            Content = content;
        }

        public static bool IsSupportedMeshExtension(string extension)
        {
            if (extension.IsEmpty())
                return false;
            if (extension[0] == '.')
                return extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".obj", StringComparison.OrdinalIgnoreCase);
            return extension.Equals("fbx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("obj", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSupportedMesh(string modelNameWithExtension)
        {
            return IsSupportedMeshExtension(Path.GetExtension(modelNameWithExtension));
        }

        static string GetContentPath(string contentName)
        {
            if (contentName.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(contentName))
                    return contentName;
            }
            else if (GlobalStats.HasMod)
            {
                string modPath = GlobalStats.ModPath + contentName;
                if (File.Exists(modPath)) return modPath;
            }
            else if (contentName.StartsWith("Content/"))
                return contentName;
            return "Content/" + contentName;
        }

        public object LoadAsset(string fileNameWithExt, string ext)
        {
            if (IsSupportedMeshExtension(ext))
            {
                Log.Info(ConsoleColor.Magenta, $"Raw LoadMesh: {fileNameWithExt}");
                return StaticMeshFromFile(fileNameWithExt);
            }

            //Log.Info(ConsoleColor.Magenta, $"Raw LoadTexture: {fileNameWithExt}");
            return LoadImageAsTexture(fileNameWithExt);
        }

        static bool IsPowerOf2(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        public Texture2D LoadImageAsTexture(string fileNameWithExt)
        {
            string contentPath = GetContentPath(fileNameWithExt);
            using (var fs = new FileStream(contentPath, FileMode.Open))
            {
                TextureCreationParameters parameters = Texture.GetCreationParameters(Device, fs);

                // mipmap gen... not really needed
                //parameters.TextureUsage |= TextureUsage.AutoGenerateMipMap;

                // this will add runtime DXT5 compression, which is incredibly slow
                //if (IsPowerOf2(parameters.Width) && IsPowerOf2(parameters.Height))
                //    parameters.Format = SurfaceFormat.Dxt5;

                fs.Seek(0, SeekOrigin.Begin);
                var texture = Texture2D.FromFile(Device, fs, parameters);
                return texture;
            }
        }

        public Texture2D LoadImageAsTexture(FileInfo file)
        {
            using (FileStream fs = file.OpenRead())
            {
                TextureCreationParameters parameters = Texture.GetCreationParameters(Device, fs);

                // mipmap gen... not really needed
                //parameters.TextureUsage |= TextureUsage.AutoGenerateMipMap;

                // this will add runtime DXT5 compression, which is incredibly slow
                //if (IsPowerOf2(parameters.Width) && IsPowerOf2(parameters.Height))
                //    parameters.Format = SurfaceFormat.Dxt5;

                fs.Seek(0, SeekOrigin.Begin);
                var texture = Texture2D.FromFile(Device, fs, parameters);
                return texture;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SDMesh
        {
            public readonly CStrView Name;
            public readonly int NumGroups;
            public readonly int NumFaces;
            public readonly int NumMaterials;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SDMaterial
        {
            public readonly CStrView Name; // name of the material instance
            public readonly CStrView DiffusePath;
            public readonly CStrView AlphaPath;
            public readonly CStrView SpecularPath;
            public readonly CStrView NormalPath;
            public readonly CStrView EmissivePath;
            public readonly Vector3 AmbientColor;
            public readonly Vector3 DiffuseColor;
            public readonly Vector3 SpecularColor;
            public readonly Vector3 EmissiveColor;
            public readonly float Specular;
            public readonly float Alpha;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SDVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Coords;
            public Vector3 Tangent;
            public Vector3 Binormal;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        unsafe struct SDMeshGroup
        {
            public readonly int GroupId;
            public readonly CStrView Name;
            public readonly SDMaterial* Mat;
            public readonly int NumTriangles;
            public readonly int NumVertices;
            public readonly int NumIndices;
            public readonly SDVertex* Vertices;
            public readonly ushort*   Indices;
            public readonly BoundingSphere Bounds;
            public readonly Matrix Transform;

            public IndexBuffer CopyIndices(GraphicsDevice device)
            {
                ushort* src = Indices;
                var dst = new ushort[NumIndices];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new IndexBuffer(device, sizeof(ushort)*NumIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                buf.SetData(dst);
                return buf;
            }

            public VertexBuffer CopyVertices(GraphicsDevice device)
            {
                SDVertex* src = Vertices;
                var dst = new SDVertex[NumVertices];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new VertexBuffer(device, sizeof(SDVertex)*NumVertices, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }
        }

        static unsafe LightingEffect CreateMaterialEffect(
            SDMaterial* mat, GraphicsDevice device, GameContentManager content, string materialFile)
        {
            var fx = new LightingEffect(device);
            fx.MaterialName          = mat->Name.AsString;
            fx.MaterialFile          = materialFile;
            fx.ProjectFile           = "Ship_Game/Data/RawContentLoader.cs";
            fx.DiffuseMapFile        = mat->DiffusePath.AsString;
            fx.EmissiveMapFile       = mat->EmissivePath.AsString;
            fx.NormalMapFile         = mat->NormalPath.AsString;
            fx.SpecularColorMapFile  = mat->SpecularPath.AsString;
            fx.DiffuseAmbientMapFile = "";
            fx.ParallaxMapFile       = "";
            if (fx.DiffuseMapFile.NotEmpty())        fx.DiffuseMapTexture        = content.Load<Texture2D>(fx.DiffuseMapFile);
            if (fx.EmissiveMapFile.NotEmpty())       fx.EmissiveMapTexture       = content.Load<Texture2D>(fx.EmissiveMapFile);
            if (fx.NormalMapFile.NotEmpty())         fx.NormalMapTexture         = content.Load<Texture2D>(fx.NormalMapFile);
            if (fx.SpecularColorMapFile.NotEmpty())  fx.SpecularColorMapTexture  = content.Load<Texture2D>(fx.SpecularColorMapFile);
            //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
            //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, content.Load<Texture2D>(fx.ParallaxMapFile));
            fx.Skinned         = false;
            fx.DoubleSided     = false;

            Texture2D alphaMap = mat->AlphaPath.NotEmpty
                ? content.Load<Texture2D>(mat->AlphaPath.AsString)
                : fx.DiffuseMapTexture;

            fx.SetTransparencyModeAndMap(TransparencyMode.None, mat->Alpha, alphaMap);
            fx.SpecularPower                 = 14.0f * mat->Specular;
            fx.SpecularAmount                = 6.0f * mat->Specular;
            fx.FresnelReflectBias            = 0.0f;
            fx.FresnelReflectOffset          = 0.0f;
            fx.FresnelMicrofacetDistribution = 0.0f;
            fx.ParallaxScale                 = 0.0f;
            fx.ParallaxOffset                = 0.0f;
            fx.DiffuseColor  = mat->DiffuseColor;
            //fx.EmissiveColor = mat->EmissiveColor;
            fx.AddressModeU  = TextureAddressMode.Wrap;
            fx.AddressModeV  = TextureAddressMode.Wrap;
            fx.AddressModeW  = TextureAddressMode.Wrap;
            return fx;
        }


        [DllImport("SDNative.dll")] static extern unsafe
            SDMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [DllImport("SDNative.dll")] static extern unsafe
            void SDMeshClose(SDMesh* mesh);

        [DllImport("SDNative.dll")] static extern unsafe
            SDMeshGroup* SDMeshGetGroup(SDMesh* mesh, int groupId);

        [DllImport("SDNative.dll")] static extern unsafe
            SDMesh* SDMeshCreateEmpty([MarshalAs(UnmanagedType.LPWStr)] string meshName);

        [DllImport("SDNative.dll")] static extern unsafe
            bool SDMeshSave(SDMesh* mesh, [MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [DllImport("SDNative.dll")] static extern unsafe
            SDMeshGroup* SDMeshNewGroup(SDMesh* mesh, 
                [MarshalAs(UnmanagedType.LPWStr)] string groupName,
                Matrix* transform);

        [DllImport("SDNative.dll")] static extern unsafe
            void SDMeshGroupSetData(SDMeshGroup* group, 
                Vector3* verts, Vector3* normals, Vector2* coords, 
                int numVertices, ushort* indices, int numIndices);

        [DllImport("SDNative.dll")] static extern unsafe 
            SDMaterial* SDMeshCreateMaterial(SDMesh* mesh, 
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                [MarshalAs(UnmanagedType.LPWStr)] string diffusePath,
                [MarshalAs(UnmanagedType.LPWStr)] string alphaPath,
                [MarshalAs(UnmanagedType.LPWStr)] string specularPath,
                [MarshalAs(UnmanagedType.LPWStr)] string normalPath,
                [MarshalAs(UnmanagedType.LPWStr)] string emissivePath,
                Vector3 ambientColor,
                Vector3 diffuseColor,
                Vector3 specularColor,
                Vector3 emissiveColor,
                float specular,
                float alpha);

        [DllImport("SDNative.dll")] static extern unsafe
            void SDMeshGroupSetMaterial(SDMeshGroup* group, SDMaterial* material);


        static VertexDeclaration Layout;

        static VertexDeclaration VertexLayout(GraphicsDevice device)
        {
            if (Layout == null)
            {
                Layout = new VertexDeclaration(device, new[]
                {
                    new VertexElement(0, 0,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                    new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                    new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                    new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0)
                });
            }
            return Layout;
        }

        unsafe StaticMesh StaticMeshFromFile(string modelName)
        {
            SDMesh* mesh = null;
            try
            {
                string meshPath = GetContentPath(modelName);
                mesh = SDMeshOpen(meshPath);
                if (mesh == null)
                {
                    if (!File.Exists(meshPath))
                        throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the file does not exist!");
                    throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the data format is invalid!");
                }

                Log.Info(ConsoleColor.Green, $"SDStaticMesh {mesh->Name} | faces:{mesh->NumFaces} | groups:{mesh->NumGroups}");

                return LoadMeshGroups(mesh, modelName);
            }
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to load mesh '{modelName}'");
                throw;
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }
        
        unsafe Map<long, LightingEffect> LoadMaterials(SDMesh* mesh, GraphicsDevice device, string modelName)
        {
            var materials = new Map<long, LightingEffect>();
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SDMeshGroup* g = SDMeshGetGroup(mesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;
                long ptr = (long)g->Mat;
                if (!materials.ContainsKey(ptr))
                {
                    materials[ptr] = (ptr == 0) ? new LightingEffect(device) : CreateMaterialEffect(g->Mat, device, Content, modelName);
                }
            }
            return materials;
        }

        unsafe StaticMesh LoadMeshGroups(SDMesh* mesh, string modelName)
        {
            var staticMesh = new StaticMesh { Name = mesh->Name.AsString };
            GraphicsDevice device = Device;
            Map<long, LightingEffect> materials = LoadMaterials(mesh, device, modelName);

            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SDMeshGroup* g = SDMeshGetGroup(mesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;

                Log.Info(ConsoleColor.Green, $"  group {g->GroupId}: {g->Name}  verts:{g->NumVertices}  ids:{g->NumIndices}");

                var meshData = new MeshData
                {
                    Name                      = g->Name.AsString,
                    Effect                    = materials[(long)g->Mat],
                    ObjectSpaceBoundingSphere = g->Bounds,
                    IndexBuffer               = g->CopyIndices(device),
                    VertexBuffer              = g->CopyVertices(device),
                    VertexDeclaration         = VertexLayout(device),
                    PrimitiveCount            = g->NumTriangles,
                    VertexCount               = g->NumVertices,
                    VertexStride              = sizeof(SDVertex)
                };
                staticMesh.Meshes.Add(meshData);
            }
            return staticMesh;
        }


        public static unsafe bool SaveModel(Model model, string name, string modelFilePath)
        {
            if (model.Meshes.Count == 0)
                return false;

            string exportDir = Path.GetDirectoryName(modelFilePath) ?? "";
            Directory.CreateDirectory(exportDir);
            SDMesh* mesh = SDMeshCreateEmpty(name);
            try
            {
                CreateMeshGroups(mesh, exportDir, model.Meshes);
                return SDMeshSave(mesh, modelFilePath);
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }
        
        static unsafe Map<Effect, long> ExportMaterials(SDMesh* mesh, string exportDir, ModelMeshCollection meshes)
        {
            var exported = new Map<Effect, long>();
            string name = mesh->Name.AsString;
            foreach (ModelMesh modelMesh in meshes)
            {
                for (int i = 0; i < modelMesh.Effects.Count; ++i)
                {
                    Effect effect = modelMesh.Effects[i];
                    if (!exported.ContainsKey(effect))
                    {
                        if (effect is BaseMaterialEffect sunburn)
                        {
                            string matName = sunburn.MaterialName.NotEmpty() ? sunburn.MaterialName : name+i;
                            exported[effect] = (long)ExportMaterial(mesh, sunburn, matName, exportDir);
                        }
                        else if (effect is BasicEffect basic)
                        {
                            string matName = basic.Texture != null && basic.Texture.Name.NotEmpty()
                                ? basic.Texture.Name : name+i;
                            exported[effect] = (long)ExportMaterial(mesh, basic, matName, exportDir);
                        }
                        else
                        {
                            exported[effect] = 0;
                        }
                    }
                }
            }
            return exported;
        }

        static T[] VertexData<T>(VertexBuffer vbo, VertexElement[] vde, int start, int count, int stride, VertexElementUsage usage) where T : struct
        {
            for (int i = 0; i < vde.Length; ++i)
            {
                if (vde[i].VertexElementUsage == usage)
                {
                    var data = new T[count];
                    vbo.GetData(vde[i].Offset + start*stride, data, 0, count, stride);
                    return data;
                }
            }
            return null;
        }

        static unsafe void CreateMeshGroups(SDMesh* mesh, string modelExportDir, ModelMeshCollection meshes)
        {
            Map<Effect, long> materials = ExportMaterials(mesh, modelExportDir, meshes);
            foreach (ModelMesh modelMesh in meshes)
            {
                Matrix transform = modelMesh.ParentBone.Transform;

                for (int i = 0; i < modelMesh.MeshParts.Count; ++i)
                {
                    ModelMeshPart part = modelMesh.MeshParts[i];

                    string groupName = (modelMesh.MeshParts.Count > 1) ? modelMesh.Name + i : modelMesh.Name;
                    SDMeshGroup* group = SDMeshNewGroup(mesh, groupName, &transform);
                    VertexBuffer vb = modelMesh.VertexBuffer;
                    IndexBuffer  ib = modelMesh.IndexBuffer;

                    int stride = part.VertexStride;
                    VertexElement[] ve = part.VertexDeclaration.GetVertexElements();
                    int vertices = part.NumVertices;
                    Vector3[] verts   = VertexData<Vector3>(vb, ve, part.BaseVertex, vertices, stride, VertexElementUsage.Position);
                    Vector3[] normals = VertexData<Vector3>(vb, ve, part.BaseVertex, vertices, stride, VertexElementUsage.Normal);
                    Vector2[] coords  = VertexData<Vector2>(vb, ve, part.BaseVertex, vertices, stride, VertexElementUsage.TextureCoordinate);
                    
                    int numIndices = part.PrimitiveCount * 3;
                    var indexData = new ushort[numIndices];
                    ib.GetData(part.StartIndex*sizeof(ushort), indexData, 0, numIndices);

                    fixed(Vector3* pVerts   = verts)
                    fixed(Vector3* pNormals = normals)
                    fixed(Vector2* pCoords  = coords)
                    fixed(ushort* pIndices = indexData)
                    {
                        SDMeshGroupSetData(group, pVerts, pNormals, pCoords, vertices, pIndices, numIndices);
                    }

                    if (modelMesh.Effects[0] != null)
                    {
                        var material = (SDMaterial*)materials[modelMesh.Effects[0]];
                        if (material != null)
                            SDMeshGroupSetMaterial(group, material);
                    }
                }
            }
        }

        static string TrySaveTexture(string modelExportDir, string textureName, Texture2D texture)
        {
            if (textureName.IsEmpty() || texture == null)
                return "";

            string name = Path.ChangeExtension(Path.GetFileName(textureName), "dds");
            string writeTo = Path.Combine(modelExportDir, name);

            lock (texture) // Texture2D.Save will crash if 2 threads try to save the same texture
            {
                if (!File.Exists(writeTo))
                {
                    Log.Warning($"  ExportTexture: {writeTo}");
                    texture.Save(writeTo, ImageFileFormat.Dds);
                }
            }
            return name;
        }

        static unsafe SDMaterial* ExportMaterial(SDMesh* mesh, BaseMaterialEffect fx, string name, string modelExportDir)
        {
            string diffusePath  = TrySaveTexture(modelExportDir, fx.DiffuseMapFile,       fx.DiffuseMapTexture);
            string specularPath = TrySaveTexture(modelExportDir, fx.SpecularColorMapFile, fx.SpecularColorMapTexture);
            string normalPath   = TrySaveTexture(modelExportDir, fx.NormalMapFile,        fx.NormalMapTexture);
            string emissivePath = TrySaveTexture(modelExportDir, fx.EmissiveMapFile,      fx.EmissiveMapTexture);

            return SDMeshCreateMaterial(
                mesh,
                name, 
                diffusePath, 
                "", // alphaPath
                specularPath, 
                normalPath, 
                emissivePath, 
                Vector3.One, // ambientColor
                fx.DiffuseColor, 
                Vector3.One, // specularColor
                Vector3.Zero, 
                fx.SpecularAmount / 16f, 
                fx.Transparency);
        }

        static unsafe SDMaterial* ExportMaterial(SDMesh* mesh, BasicEffect fx, string name, string modelExportDir)
        {
            string diffusePath, specularPath = "", normalPath = "", emissivePath = "";
            if (fx.Texture == null)
            {
                string matbase = name.NotEmpty() && char.IsLetter(name[name.Length - 1]) 
                    ? name.Substring(0, name.Length-1) : name;

                diffusePath  = matbase + "_d.png";
                specularPath = matbase + "_s.png";
                normalPath   = matbase + "_n.png";
                emissivePath = matbase + "_g.png";
            }
            else
            {
                diffusePath  = TrySaveTexture(modelExportDir, name+".png", fx.Texture);
            }

            return SDMeshCreateMaterial(
                mesh,
                name, 
                diffusePath, 
                "", 
                specularPath, 
                normalPath, 
                emissivePath, 
                fx.AmbientLightColor, 
                fx.DiffuseColor, 
                fx.SpecularColor, 
                fx.EmissiveColor, 
                fx.SpecularPower, 
                fx.Alpha);
        }

        SkinnedModel SkinnedMeshFromFile(string modelName)
        {
            return null;
        }

    }
}
