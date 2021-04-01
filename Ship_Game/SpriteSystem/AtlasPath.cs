using System.IO;

namespace Ship_Game.SpriteSystem
{
    public class AtlasPath
    {
        public readonly string OriginalName;
        public readonly string Texture;
        public readonly string Descriptor;
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
            Texture    = $"{CacheDir}/{AtlasName}.dds";
            Descriptor = $"{CacheDir}/{AtlasName}.atlas";
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