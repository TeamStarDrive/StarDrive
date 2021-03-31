using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
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

        public Array<FileInfo> GetAllXnbModelFiles(string folder)
        {
            var files = new Array<FileInfo>();
            files.AddRange(Dir.GetFiles($"Content/{folder}", "*.xnb", SearchOption.AllDirectories));
            if (GlobalStats.HasMod)
                files.AddRange(Dir.GetFiles($"{GlobalStats.ModPath}/{folder}", "*.xnb", SearchOption.AllDirectories));

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
            return modelFiles;
        }

        public void ExportXnbMesh(FileInfo file, bool alwaysOverwrite = false)
        {
            string relativePath = file.RelPath();
            Log.Info(relativePath);

            if (relativePath.StartsWith("Content\\"))
                relativePath = relativePath.Substring(8);

            string savePath = "MeshExport\\" + Path.ChangeExtension(relativePath, "fbx");
            if (!alwaysOverwrite && File.Exists(savePath))
                return;

            string nameNoExt = Path.GetFileNameWithoutExtension(file.Name);
            try
            {
                Model model = Content.LoadModel(relativePath);
                Log.Info($"  Export StaticMesh: {savePath}");
                MeshExport.Export(model, nameNoExt, savePath);
                return;
            }
            catch
            {
            }

            try
            {
                SkinnedModel model = Content.LoadSkinnedModel(relativePath);
                Log.Info($"  Export AnimatedMesh: {savePath}");
                MeshExport.Export(model, nameNoExt, savePath);
            }
            catch (ContentLoadException e)
            {
                Log.Warning($"Failed to export {relativePath}: {e.Message}");
            }
        }

        public void ExportAllXnbMeshes()
        {
            var files = new Array<FileInfo>();
            files.AddRange(GetAllXnbModelFiles("Effects"));
            files.AddRange(GetAllXnbModelFiles("Model"));
            files.AddRange(GetAllXnbModelFiles("mod models"));
            files.AddRange(GetAllXnbModelFiles("model"));

            void ExportMeshes(int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    ExportXnbMesh(files[i]);
                }
            }
            //Parallel.For(files.Count, ExportMeshes, Parallel.NumPhysicalCores * 2);
            ExportMeshes(0, files.Count);
        }

        public void ExportAllTextures()
        {
            string outDir = Path.GetFullPath("ExportedTextures");
            Log.Write(ConsoleColor.Blue, $"ExportTextures to: {outDir}");

            Parallel.ForEach(Dir.GetFiles("Content/", "xnb"), f => ExportTexture(f, outDir, TextureFileFormat.DDS));
            Parallel.ForEach(Dir.GetFiles("Content/", "png"), f => ExportTexture(f, outDir, TextureFileFormat.PNG));

            if (GlobalStats.HasMod)
            {
                Parallel.ForEach(Dir.GetFiles(GlobalStats.ModPath, "xnb"), f => ExportTexture(f, outDir, TextureFileFormat.DDS));
                Parallel.ForEach(Dir.GetFiles(GlobalStats.ModPath, "png"), f => ExportTexture(f, outDir, TextureFileFormat.PNG));
            }
        }

        void ExportTexture(FileInfo file, string outDir, TextureFileFormat fmt)
        {
            string relPath = file.RelPath();
            string outExt = (fmt == TextureFileFormat.DDS) ? ".dds" : ".png";
            string outFile = Path.Combine(outDir, Path.ChangeExtension(relPath, outExt));
            bool saved = false;
            try
            {
                GameLoadingScreen.SetStatus("Export", outFile);
                string ext = file.Extension.Remove(0, 1).ToLower(); // '.Xnb' -> 'xnb'
                using (Texture2D tex = Content.LoadUncachedTexture(file, ext))
                    saved = TexExport.Save(tex, outFile, fmt);
            }
            catch // not a texture
            {
            }
            if (saved)
                Log.Info($"Saved {outFile}");
            else
                Log.Warning($"Ignored {relPath}");
        }
    }
}
