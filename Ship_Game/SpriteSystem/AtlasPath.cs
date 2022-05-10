using System.IO;
using SDUtils;

namespace Ship_Game.SpriteSystem
{
    public class AtlasPath
    {
        public readonly string OriginalName;

        public readonly string CacheAtlasTex;
        public readonly string CacheAtlasFile;

        public readonly string PrePackedTex;
        public readonly string PrePackedFile;

        readonly string AtlasName;
        readonly string CacheDir;

        public AtlasPath(string name)
        {
            OriginalName = Path.GetFileName(name);
            AtlasName = name.Replace('/', '_');

            // put mod atlases into a separate folder to reduce invalidations
            string cache = GlobalStats.HasMod ? $"/TC-{GlobalStats.ModName}" : "/TextureCache";
            CacheDir = Dir.StarDriveAppData + cache;
            Directory.CreateDirectory(CacheDir);
            CacheAtlasTex  = $"{CacheDir}/{AtlasName}.dds";
            CacheAtlasFile = $"{CacheDir}/{AtlasName}.atlas";
            
            FileInfo atlasTex  = ResourceManager.GetModOrVanillaFile($"{name}/{OriginalName}.dds");
            FileInfo prePacked = ResourceManager.GetModOrVanillaFile($"{name}/{OriginalName}.atlas");
            PrePackedTex = atlasTex?.FullName;
            PrePackedFile = prePacked?.FullName;
        }
        public string GetExportPath(TextureInfo t)
        {
            string prefix = t.NoPack ? "NoPack/" : "";
            string folder = $"{CacheDir}/{AtlasName}/{prefix}";
            Directory.CreateDirectory(folder);
            return $"{folder}{t.Name}";
        }
        public string GetCompressedCacheDir()
        {
            string folder = $"{CacheDir}/{AtlasName}/";
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}