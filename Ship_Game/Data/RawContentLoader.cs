using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Texture;

namespace Ship_Game.Data
{
    /// <summary>
    /// Helper class for GameContentManager
    /// Allows loading FBX, OBJ and PNG files instead of .XNB content
    /// </summary>
    public class RawContentLoader
    {
        readonly GameContentManager Content;
        readonly TextureImporter TexImport;
        readonly TextureExporter TexExport;
        readonly MeshImporter MeshImport;
        readonly MeshExporter MeshExport;

        public RawContentLoader(GameContentManager content)
        {
            Content = content;
            TexImport = new TextureImporter(content);
            TexExport = new TextureExporter(content);
            MeshImport = new MeshImporter(content);
            MeshExport = new MeshExporter(content);
        }

        public static bool IsSupportedMesh(string modelNameWithExtension)
        {
            return IsSupportedMeshExtension(Path.GetExtension(modelNameWithExtension));
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
        
        public static string GetContentPath(string contentName)
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
            {
                return contentName;
            }
            return "Content/" + contentName;
        }

        public object LoadAsset(string fileNameWithExt, string ext)
        {
            if (IsSupportedMeshExtension(ext))
            {
                Log.Info(ConsoleColor.Magenta, $"Raw LoadMesh: {fileNameWithExt}");
                string meshPath = GetContentPath(fileNameWithExt);
                return MeshImport.Import(meshPath, fileNameWithExt);
            }

            //Log.Info(ConsoleColor.Magenta, $"Raw LoadTexture: {fileNameWithExt}");
            return LoadImageAsTexture(fileNameWithExt);
        }

        ///////////////////////////////////////////////////

        public Texture2D LoadImageAsTexture(string fileNameWithExt)
        {
            string contentPath = GetContentPath(fileNameWithExt);
            return TexImport.Load(contentPath);
        }

        public Texture2D LoadImageAsTexture(FileInfo file)
        {
            return TexImport.Load(file);
        }

        ///////////////////////////////////////////////////

        public StaticMesh LoadStaticMesh(string meshName)
        {
            string meshPath = GetContentPath(meshName);
            return MeshImport.Import(meshPath, meshName);
        }

        public FileInfo[] GetAllXnbModelFiles(string folder)
        {
            var files = new Array<FileInfo>();
            files.AddRange(Dir.GetFiles("Content/", "*.xnb", SearchOption.AllDirectories));
            if (GlobalStats.HasMod)
                files.AddRange(Dir.GetFiles(GlobalStats.ModPath, "*.xnb", SearchOption.AllDirectories));

            var modelFiles = new Array<FileInfo>();
            for (int i = 0; i < files.Count; ++i)
            {
                FileInfo file = files[i];
                string name = file.Name;
                if (name.EndsWith("_d.xnb") || name.EndsWith("_g.xnb") ||
                    name.EndsWith("_n.xnb") || name.EndsWith("_s.xnb") ||
                    name.EndsWith("_d_0.xnb") || name.EndsWith("_g_0.xnb") ||
                    name.EndsWith("_n_0.xnb") || name.EndsWith("_s_0.xnb"))
                {
                    continue;
                }
                modelFiles.Add(file);
            }
            return modelFiles.ToArray();
        }

        public void ExportXnbMesh(FileInfo file, bool alwaysOverwrite = false)
        {
            try
            {
                string relativePath = file.RelPath();
                Log.Info(relativePath);

                if (relativePath.StartsWith("Content\\"))
                    relativePath = relativePath.Substring(8);

                string savePath = "MeshExport\\" + Path.ChangeExtension(relativePath, "fbx");

                if (alwaysOverwrite || !File.Exists(savePath))
                {
                    var model = Content.LoadModel(relativePath); // @note This may throw if it's not a mesh
                    Log.Info($"ExportMesh: {savePath}");

                    string nameNoExt = Path.GetFileNameWithoutExtension(file.Name);
                    MeshExport.Export(model, nameNoExt, savePath);
                }
            }
            catch (Exception)
            {
                // just ignore resources that are not static models
            }
        }

        public void ExportAllXnbMeshes()
        {
            FileInfo[] files = GetAllXnbModelFiles("Model");

            void ExportMeshes(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    ExportXnbMesh(files[i]);
                }
            }
            Parallel.For(files.Length, ExportMeshes, Parallel.NumPhysicalCores * 2);
            //ExportMeshes(0, files.Length);
        }
    }
}
