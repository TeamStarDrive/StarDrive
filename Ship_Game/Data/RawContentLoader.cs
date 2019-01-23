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

        Texture2D LoadImageAsTexture(string fileNameWithExt)
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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SDMesh
        {
            public readonly CStrView Name;
            public readonly int NumGroups;
            public readonly int NumFaces;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct SDMaterial
        {
            public readonly CStrView Name; // name of the material instance
            public readonly CStrView MaterialFile;
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
            public readonly SDMaterial Mat;
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

            public LightingEffect CreateMaterialEffect(GraphicsDevice device, GameContentManager content)
            {
                var fx = new LightingEffect(device);
                fx.MaterialName             = Mat.Name.AsString;
                fx.MaterialFile             = Mat.MaterialFile.AsString;
                fx.ProjectFile              = "Ship_Game/Data/RawContentLoader.cs";
                fx.DiffuseMapFile           = Mat.DiffusePath.AsString;
                fx.EmissiveMapFile          = Mat.EmissivePath.AsString;
                fx.NormalMapFile            = Mat.NormalPath.AsString;
                fx.SpecularColorMapFile     = Mat.SpecularPath.AsString;
                fx.DiffuseAmbientMapFile    = "";
                fx.ParallaxMapFile          = "";
                if (fx.DiffuseMapFile.NotEmpty())        fx.DiffuseMapTexture        = content.Load<Texture2D>(fx.DiffuseMapFile);
                if (fx.EmissiveMapFile.NotEmpty())       fx.EmissiveMapTexture       = content.Load<Texture2D>(fx.EmissiveMapFile);
                if (fx.NormalMapFile.NotEmpty())         fx.NormalMapTexture         = content.Load<Texture2D>(fx.NormalMapFile);
                if (fx.SpecularColorMapFile.NotEmpty())  fx.SpecularColorMapTexture  = content.Load<Texture2D>(fx.SpecularColorMapFile);
                //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
                //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, content.Load<Texture2D>(fx.ParallaxMapFile));
                fx.Skinned         = false;
                fx.DoubleSided     = false;

                Texture2D alphaMap = Mat.AlphaPath.NotEmpty
                    ? content.Load<Texture2D>(Mat.AlphaPath.AsString)
                    : fx.DiffuseMapTexture;

                fx.SetTransparencyModeAndMap(TransparencyMode.None, Mat.Alpha, alphaMap);
                fx.SpecularPower                 = 14.0f * Mat.Specular;
                fx.SpecularAmount                = 6.0f * Mat.Specular;
                fx.FresnelReflectBias            = 0.0f;
                fx.FresnelReflectOffset          = 0.0f;
                fx.FresnelMicrofacetDistribution = 0.0f;
                fx.ParallaxScale                 = 0.0f;
                fx.ParallaxOffset                = 0.0f;
                fx.DiffuseColor  = Mat.DiffuseColor;
                fx.EmissiveColor = Mat.EmissiveColor;
                fx.AddressModeU  = TextureAddressMode.Wrap;
                fx.AddressModeV  = TextureAddressMode.Wrap;
                fx.AddressModeW  = TextureAddressMode.Wrap;
                return fx;
            }
        }

        [DllImport("SDNative.dll")] static extern unsafe SDMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string filename);
        [DllImport("SDNative.dll")] static extern unsafe void SDMeshClose(SDMesh* mesh);
        [DllImport("SDNative.dll")] static extern unsafe SDMeshGroup* SDMeshGetGroup(SDMesh* mesh, int groupId);

        [DllImport("SDNative.dll")] static extern unsafe SDMesh* SDMeshCreateEmpty([MarshalAs(UnmanagedType.LPWStr)] string meshname);
        [DllImport("SDNative.dll")] static extern unsafe bool SDMeshSave(SDMesh* mesh, [MarshalAs(UnmanagedType.LPWStr)] string filename);
        [DllImport("SDNative.dll")] static extern unsafe SDMeshGroup* SDMeshNewGroup(SDMesh* mesh,
                                                            [MarshalAs(UnmanagedType.LPWStr)] string groupname,
                                                            Matrix* transform);

        [DllImport("SDNative.dll")] static extern unsafe void SDMeshGroupSetData(SDMeshGroup* group, 
                                                            Vector3* verts, Vector3* normals, Vector2* coords, int numVertices,
                                                            ushort* indices, int numIndices);

        [DllImport("SDNative.dll")] static extern unsafe void SDMeshGroupSetMaterial(SDMeshGroup* group, 
                                                            [MarshalAs(UnmanagedType.LPWStr)] string name,
                                                            [MarshalAs(UnmanagedType.LPWStr)] string materialFile,
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
            SDMesh* sdmesh = null;
            try
            {
                string meshPath = GetContentPath(modelName);
                sdmesh = SDMeshOpen(meshPath);
                if (sdmesh == null)
                {
                    if (!File.Exists(meshPath))
                        throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the file does not exist!");
                    throw new InvalidDataException($"Failed to load mesh '{meshPath}' because the data format is invalid!");
                }

                Log.Info(ConsoleColor.Green, $"SDStaticMesh {sdmesh->Name} | faces:{sdmesh->NumFaces} | groups:{sdmesh->NumGroups}");

                GraphicsDevice device = Device;
                var staticMesh = new StaticMesh { Name = sdmesh->Name.AsString };

                for (int i = 0; i < sdmesh->NumGroups; ++i)
                {
                    SDMeshGroup* g = SDMeshGetGroup(sdmesh, i);
                    if (g->NumVertices == 0 || g->NumIndices == 0)
                        continue;

                    Log.Info(ConsoleColor.Green, $"  group {g->GroupId}: {g->Name}  verts:{g->NumVertices}  ids:{g->NumIndices}");

                    var meshData = new MeshData
                    {
                        Name                      = g->Name.AsString,
                        Effect                    = g->CreateMaterialEffect(device, Content),
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
            catch (Exception e)
            {
                Log.ErrorDialog(e, $"Failed to load mesh '{modelName}'");
                throw;
            }
            finally
            {
                SDMeshClose(sdmesh);
            }
        }

        static T[] VertexData<T>(VertexBuffer vbo, VertexElement[] vde, int numVerts, int stride, VertexElementUsage usage) where T : struct
        {
            for (int i = 0; i < vde.Length; ++i)
            {
                if (vde[i].VertexElementUsage == usage)
                {
                    var data = new T[numVerts];
                    vbo.GetData(vde[i].Offset, data, 0, numVerts, stride);
                    return data;
                }
            }
            return null;
        }

        public static unsafe bool SaveModel(Model model, string name, string modelFilePath)
        {
            if (model.Meshes.Count == 0)
                return false;

            string modelExportDir = Path.GetDirectoryName(modelFilePath) ?? "";
            Directory.CreateDirectory(modelExportDir);
            SDMesh* sdmesh = SDMeshCreateEmpty(name);

            foreach (ModelMesh modelMesh in model.Meshes)
            {
                Matrix transform = modelMesh.ParentBone.Transform;
                SDMeshGroup* group = SDMeshNewGroup(sdmesh, modelMesh.Name, &transform);
                VertexBuffer vbo = modelMesh.VertexBuffer;
                IndexBuffer  ibo = modelMesh.IndexBuffer;

                ModelMeshPart part = modelMesh.MeshParts[0];
                int stride = part.VertexStride;
                VertexElement[] vde = part.VertexDeclaration.GetVertexElements();
                int numVertices = vbo.SizeInBytes / stride;
                int numIndices  = ibo.SizeInBytes / sizeof(ushort);


                Vector3[] verts   = VertexData<Vector3>(vbo, vde, numVertices, stride, VertexElementUsage.Position);
                Vector3[] normals = VertexData<Vector3>(vbo, vde, numVertices, stride, VertexElementUsage.Normal);
                Vector2[] coords  = VertexData<Vector2>(vbo, vde, numVertices, stride, VertexElementUsage.TextureCoordinate);
                var indexData = new ushort[numIndices];
                ibo.GetData(0, indexData, 0, numIndices);

                fixed(Vector3* pVerts   = verts)
                fixed(Vector3* pNormals = normals)
                fixed(Vector2* pCoords  = coords)
                fixed(ushort* pIndices = indexData)
                {
                    SDMeshGroupSetData(group, pVerts, pNormals, pCoords, numVertices, pIndices, numIndices);
                }

                for (int i = 0; i < modelMesh.Effects.Count; ++i)
                {
                    string materialName = i == 0 ? name : name + i;
                    Effect effect = modelMesh.Effects[i];

                    if (effect is BaseMaterialEffect sunburnMaterial)
                    {
                        SaveMaterial(group, sunburnMaterial, materialName, modelExportDir);
                    }
                    else if (effect is BasicEffect basicEffect)
                    {
                        SaveMaterial(group, basicEffect, materialName, modelExportDir);
                    }
                }
            }

            bool success = SDMeshSave(sdmesh, modelFilePath);
            SDMeshClose(sdmesh);
            return success;
        }

        static string TrySaveTexture(string modelExportDir, string textureName, Texture2D texture)
        {
            if (textureName.IsEmpty() || texture == null)
                return "";

            string name = Path.ChangeExtension(Path.GetFileName(textureName), "png");
            string writeTo = Path.Combine(modelExportDir, name);

            lock (texture) // Texture2D.Save will crash if 2 threads try to save the same texture
            {
                if (!File.Exists(writeTo))
                {
                    Log.Warning($"  ExportTexture: {writeTo}");
                    texture.Save(writeTo, ImageFileFormat.Png);
                }
            }
            return name;
        }

        static unsafe void SaveMaterial(SDMeshGroup* group, BaseMaterialEffect fx, string name, string modelExportDir)
        {
            string diffusePath  = TrySaveTexture(modelExportDir, fx.DiffuseMapFile,       fx.DiffuseMapTexture);
            string specularPath = TrySaveTexture(modelExportDir, fx.SpecularColorMapFile, fx.SpecularColorMapTexture);
            string normalPath   = TrySaveTexture(modelExportDir, fx.NormalMapFile,        fx.NormalMapTexture);
            string emissivePath = TrySaveTexture(modelExportDir, fx.EmissiveMapFile,      fx.EmissiveMapTexture);

            SDMeshGroupSetMaterial(
                group, name, 
                name+".mtl", 
                diffusePath, 
                "", 
                specularPath, 
                normalPath, 
                emissivePath, 
                Vector3.One, 
                fx.DiffuseColor, 
                Vector3.One, 
                fx.EmissiveColor, 
                fx.SpecularAmount / 16f, 
                fx.Transparency);
        }

        static unsafe void SaveMaterial(SDMeshGroup* group, BasicEffect fx, string name, string modelExportDir)
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

            SDMeshGroupSetMaterial(
                group, name, 
                name+".mtl", 
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
