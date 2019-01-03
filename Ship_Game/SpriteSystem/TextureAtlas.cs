using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Graphics.Color;

namespace Ship_Game
{
    public class SubTexture
    {
        // name="sprite1" x="461" y="1317" width="28" height="41"
        public string Name;        // name of the sprite for name-based lookup
        public int X, Y;           // position in sprite sheet
        public int Width, Height;  // actual size of sub-texture in sprite sheet
        public Texture2D Atlas;  // MAIN atlas texture
    }

    /// Generic TextureAtlas can be 
    public class TextureAtlas : IDisposable
    {
        public string Name { get; private set; }
        private readonly string CacheName;
        string DescriptorPath => $"{TextureCacheDir}/{CacheName}.atlas";
        string TexturePath    => $"{TextureCacheDir}/{CacheName}.dds";
        private int Hash;
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

        private readonly Map<string, SubTexture> Textures = new Map<string, SubTexture>();

        protected TextureAtlas(string name)
        {
            Name = name;
            CacheName = name.Replace('/', '_');
        }

        ~TextureAtlas()
        {
            Atlas?.Dispose();
        }

        public void Dispose()
        {
            Atlas?.Dispose();
            GC.SuppressFinalize(this);
        }

        public int SpriteCount => Textures.Count;
        public SubTexture this[string name] => Textures[name];

        private static string PrepareTextureCacheDir()
        {
            string dir = Dir.StarDriveAppData + "/TextureCache";
            Directory.CreateDirectory(dir);
            return dir;
        }
        private static readonly string TextureCacheDir = PrepareTextureCacheDir();

        private struct TextureData
        {
            public readonly string Name;
            public int X, Y;
            public readonly int Width;
            public readonly int Height;
            readonly Color[] ColorData;

            public override string ToString() => $"{{{X},{Y}}} {Width}x{Height}";
            public int Area => Width * Height;

            public TextureData(string texName, Texture2D tex)
            {
                Name = texName;
                X = 0;
                Y = 0;
                Width  = tex.Width;
                Height = tex.Height;

                if (tex.Format == SurfaceFormat.Dxt5)
                {
                    ColorData = ImageUtils.DecompressDXT5(tex);
                }
                else if (tex.Format == SurfaceFormat.Dxt1)
                {
                    ColorData = ImageUtils.DecompressDXT1(tex);
                }
                else if (tex.Format == SurfaceFormat.Color)
                {
                    ColorData = new Color[tex.Width * tex.Height];
                    tex.GetData(ColorData);
                }
                else
                {
                    ColorData = new Color[0];
                    Log.Error($"Unsupported atlas texture format: {tex.Format}");
                }
                // This is purely for debugging:
                //ImageUtils.SaveAsPng($"{TextureCacheDir}/{texName}.png", Width, Height, ColorData);
            }

            public void CopyPixelsTo(Color[] atlas, int atlasWidth)
            {
                int sourceIndex = 0;
                int destinationIndex = atlasWidth * Y + X; // initial offset
                for (int iy = 0; iy < Height; ++iy)
                {
                    // copy row
                    Array.Copy(ColorData, sourceIndex, atlas, destinationIndex, Width);
                    sourceIndex += Width; // advance to next row
                    destinationIndex += atlasWidth;
                }
            }
        }

        static FileInfo[] GatherUniqueTextures(string folder)
        {
            FileInfo[] textureFiles = ResourceManager.GatherTextureFiles(folder);
            var uniqueTextures = new Map<string, FileInfo>();
            foreach (FileInfo info in textureFiles)
            {
                string texName = info.NameNoExt();
                if (uniqueTextures.TryGetValue(texName, out FileInfo existing))
                {
                    if (existing.Extension == "xnb") // only replace if old was xnb
                        uniqueTextures[texName] = info;
                }
                else
                {
                    uniqueTextures.Add(texName, info);
                }
            }
            return uniqueTextures.Values.ToArray();
        }

        static int CreateHash(FileInfo[] textures)
        {
            int hash = 5381;
            void Combine(int hashCode) => hash = ((hash << 5) + hash) ^ hashCode;
            Combine(textures.Length.GetHashCode());
            foreach (FileInfo info in textures)
            {
                Combine(info.LastWriteTimeUtc.GetHashCode());
                Combine(info.Name.GetHashCode());
            }
            return hash;
        }

        const int Padding = 2; // Atlas texture padding
        const int MinFreeSpotSize = 16; // Minimum width/height for recycled free spots

        static bool FillFreeSpot(Array<Rectangle> freeSpots, ref TextureData td)
        {
            for (int j = 0; j < freeSpots.Count; ++j)
            {
                Rectangle r = freeSpots[j];
                if (td.Width > r.Width || td.Height > r.Height)
                    continue;
                td.X = r.X;
                td.Y = r.Y;
                freeSpots.RemoveAt(j);
                int fillX = td.Width  + Padding;
                int fillY = td.Height + Padding;
                int remX = r.Width  - fillX;
                int remY = r.Height - fillY;
                // We have remaining sections A, B, C that could be recycled
                // So split it up if >= MinFreeSpotSize and push to freeSpots
                // _____________
                // |fill |  A  |
                // |_____|_____|
                // |  B  |  C  |
                // |_____|_____|
                if (remX >= MinFreeSpotSize) // A
                    freeSpots.Add(new Rectangle(r.X+fillX, r.Y, remX, fillY));
                if (remY >= MinFreeSpotSize) // B
                    freeSpots.Add(new Rectangle(r.X, r.Y+fillY, fillX, remY));
                if (remX >= MinFreeSpotSize && remY >= MinFreeSpotSize) // C
                    freeSpots.Add(new Rectangle(r.X+fillX, r.Y+fillY, remX, remY));
                return true; // success! we filled the free spot
            }
            return false;
        }

        void PackTextures(TextureData[] textures)
        {
            // Sort textures by AREA, DESCENDING
            Array.Sort(textures, (a, b) => b.Area - a.Area);

            var freeSpots = new Array<Rectangle>();

            Width  = 2048;
            Height = 512;
            int cursorX = 0;
            int cursorY = 0;
            int bottomY = 0;

            for (int i = 0; i < textures.Length; ++i)
            {
                ref TextureData td = ref textures[i];

                // check if we can fit anything into existing free spots:
                if (FillFreeSpot(freeSpots, ref td))
                    continue;

                int remainingX = Width - cursorX;
                if (remainingX < td.Width)
                {
                    freeSpots.Add(new Rectangle(cursorX, cursorY, remainingX, bottomY-cursorY));
                    cursorX = 0;
                    cursorY = bottomY + Padding;
                }

                int thisBottomY = cursorY + td.Height;
                if (thisBottomY >= bottomY) bottomY = thisBottomY;
                while (bottomY >= Height) Height += 512;

                td.X = cursorX;
                td.Y = cursorY;
                cursorX += td.Width + Padding;

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in descending order.
                // ____________
                // |  tdfill  |
                // |__________|
                // | freespot |
                if (td.Width >= MinFreeSpotSize)
                {
                    int freeSpotY = cursorY + td.Height + Padding;
                    int remainingY = bottomY - freeSpotY;
                    if (remainingY >= MinFreeSpotSize)
                        freeSpots.Add(new Rectangle(td.X, freeSpotY, td.Width, remainingY));
                }
            }

            Console.WriteLine($"Packed Atlas {Name}: {Width}x{Height}px  {textures.Length} sub-textures");
        }

        void CreateAtlasTexture(GameContentManager content, TextureData[] textures)
        {
            var atlasColorData = new Color[Width * Height];
            for (int i = 0; i < textures.Length; ++i)
            {
                ref TextureData td = ref textures[i];
                td.CopyPixelsTo(atlasColorData, Width);

                Textures[td.Name] = new SubTexture
                {
                    Name = td.Name,
                    X = td.X,
                    Y = td.Y,
                    Width = td.Width,
                    Height = td.Height,
                };
            }

            // We compress the DDS color into DXT5 and then reload it through XNA
            ImageUtils.SaveAsDDS(TexturePath, Width, Height, atlasColorData);
            Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);

            // Initialize atlas reference
            foreach (SubTexture t in Textures.Values)
                t.Atlas = Atlas;

            Console.WriteLine($"CreateAtlasTexture {Name} {Width}x{Height}");
        }


        void SaveAtlasDescriptor()
        {
            using (var fs = new StreamWriter(DescriptorPath))
            {
                fs.WriteLine(Hash);
                fs.WriteLine(Name);
                foreach (KeyValuePair<string, SubTexture> kv in Textures)
                {
                    SubTexture t = kv.Value;
                    fs.WriteLine($"{t.X} {t.Y} {t.Width} {t.Height} {t.Name}");
                }
            }
            Console.WriteLine($"SaveAtlas {Name}");
        }

        bool TryLoadCache(GameContentManager content)
        {
            if (!File.Exists(DescriptorPath) || !File.Exists(TexturePath))
                return false; // neither atlas cache file exists, regenerate
            using (var fs = new StreamReader(DescriptorPath))
            {
                int.TryParse(fs.ReadLine(), out int hash);
                if (hash != Hash)
                    return false; // hash mismatch, we need to regenerate cache

                Textures.Clear();
                Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);

                Name = fs.ReadLine();
                var separator = new[] { ' ' };

                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    string[] entry = line.Split(separator, 5);
                    var t = new SubTexture();
                    int.TryParse(entry[0], out t.X);
                    int.TryParse(entry[1], out t.Y);
                    int.TryParse(entry[2], out t.Width);
                    int.TryParse(entry[3], out t.Height);
                    t.Name = entry[4];
                    Textures.Add(t.Name, t);
                }
            }
            Console.WriteLine($"LoadAtlas {Name} {Atlas.Width}x{Atlas.Height} {Textures.Count} sub-textures");
            return true; // we loaded everything
        }

        public static TextureAtlas CreateFromFolder(GameContentManager content, string folder)
        {
            var atlas = new TextureAtlas(folder);

            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            atlas.Hash = CreateHash(textureFiles);
            if (atlas.TryLoadCache(content))
                return atlas;

            var textures = new TextureData[textureFiles.Length];
            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetPath = info.CleanResPath(false);
                string texName = info.NameNoExt();
                using (var tex = content.LoadUncached<Texture2D>(assetPath))
                {
                    //Console.WriteLine($"texture: {texName}  {tex.Width}x{tex.Height}  {tex.Format}");
                    textures[i] = new TextureData(texName, tex);
                }
            }

            atlas.PackTextures(textures);
            atlas.CreateAtlasTexture(content, textures);
            atlas.SaveAtlasDescriptor();
            return atlas;
        }
    }
}
