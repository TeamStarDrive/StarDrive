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

        public override string ToString()
            => $"sub-tex  {Name} {X},{Y} {Width}x{Height}  atlas:{Atlas.Width}x{Atlas.Height}";
    }

    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public class TextureAtlas : IDisposable
    {
        const int Version = 1; // changing this will force all caches to regenerate

        public string Name { get; private set; }
        readonly string CacheName;
        string DescriptorPath => $"{TextureCacheDir}/{CacheName}.atlas";
        string TexturePath    => $"{TextureCacheDir}/{CacheName}.dds";
        int Hash;
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }
        Array<Rectangle> FreeSpots; // non-local for debugging purposes

        readonly Map<string, SubTexture> Textures = new Map<string, SubTexture>();
        SubTexture[] SortedTextures = Empty<SubTexture>.Array;

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

        public string SizeString => $"{$"{Width}x{Height}",9}";
        public override string ToString() => $"atlas {SpriteCount} {SizeString} {Name}";

        public int SpriteCount => SortedTextures.Length;
        public SubTexture this[int index] => SortedTextures[index];
        public SubTexture this[string name] => Textures[name];

        public bool TryGetTexture(string name, out SubTexture texture)
        {
            return Textures.TryGetValue(name, out texture);
        }

        static string PrepareTextureCacheDir()
        {
            string dir = Dir.StarDriveAppData + "/TextureCache";
            Directory.CreateDirectory(dir);
            return dir;
        }
        static readonly string TextureCacheDir = PrepareTextureCacheDir();

        class TextureInfo
        {
            public string Name;
            public int X, Y;
            public int Width;
            public int Height;
            public Texture2D Texture;

            public void CopyPixelsTo(Color[] atlas, int atlasWidth)
            {
                if (Texture == null)
                    throw new ObjectDisposedException("TextureData Texture2D ref already disposed");

                Color[] colorData;
                if (Texture.Format == SurfaceFormat.Dxt5)
                {
                    colorData = ImageUtils.DecompressDXT5(Texture);
                }
                else if (Texture.Format == SurfaceFormat.Dxt1)
                {
                    colorData = ImageUtils.DecompressDXT1(Texture);
                }
                else if (Texture.Format == SurfaceFormat.Color)
                {
                    colorData = new Color[Texture.Width * Texture.Height];
                    Texture.GetData(colorData);
                }
                else
                {
                    colorData = new Color[0];
                    Log.Error($"Unsupported atlas texture format: {Texture.Format}");
                }

                Texture.Dispose(); // save some memory
                Texture = null;

                // This is purely for debugging:
                //ImageUtils.SaveAsPng($"{TextureCacheDir}/{texName}.png", Width, Height, ColorData);

                int sourceIndex = 0;
                int destinationIndex = atlasWidth * Y + X; // initial offset
                for (int iy = 0; iy < Height; ++iy)
                {
                    // copy row
                    Array.Copy(colorData, sourceIndex, atlas, destinationIndex, Width);
                    sourceIndex += Width; // advance to next row
                    destinationIndex += atlasWidth;
                }
            }
        }

        static FileInfo[] GatherUniqueTextures(string folder)
        {
            FileInfo[] textureFiles = ResourceManager.GatherTextureFiles(folder, recursive:false);
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
            // @note This is very fast, looks like FileInfo is cached somehow.
            int hash = 5381;
            void Combine(int hashCode) => hash = ((hash << 5) + hash) ^ hashCode;
            Combine(textures.Length.GetHashCode());
            Combine(Version);
            foreach (FileInfo info in textures)
            {
                Combine(info.LastWriteTimeUtc.GetHashCode());
                Combine(info.Name.GetHashCode());
            }
            return hash;
        }

        const int Padding = 0; // Atlas texture padding
        const int MinFreeSpotSize = 16; // Minimum width/height for recycled free spots

        bool FillFreeSpot(TextureInfo td)
        {
            for (int j = 0; j < FreeSpots.Count; ++j)
            {
                Rectangle r = FreeSpots[j];
                if (td.Width > r.Width || td.Height > r.Height)
                    continue;
                td.X = r.X;
                td.Y = r.Y;
                FreeSpots.RemoveAt(j);
                int fillX = td.Width  + Padding;
                int fillY = td.Height + Padding;
                int remX = r.Width  - fillX;
                int remY = r.Height - fillY;
                // We have remaining sections A, B that could be recycled
                // So split it up if >= MinFreeSpotSize and insert to freeSpots
                // _____________
                // |fill |  A  |
                // |_____|  A  |
                // |__B__|__A__|
                if (remX >= MinFreeSpotSize) // A
                {
                    FreeSpots.Insert(j, new Rectangle(r.X+fillX, r.Y, remX, r.Height));
                    if (remY >= MinFreeSpotSize) // B?
                        FreeSpots.Insert(j, new Rectangle(r.X, r.Y+fillY, fillX, remY));
                }
                // _________
                // |fill | |
                // |_____|_|
                // |B B B B|
                // |B_B_B_B|
                else if (remY >= MinFreeSpotSize && (fillX+remX) >= MinFreeSpotSize)
                {
                    FreeSpots.Insert(j, new Rectangle(r.X, r.Y+fillY, fillX+remX, remY));
                }
                return true; // success! we filled the free spot
            }
            return false;
        }


        // @note This is fast enough
        void PackTextures(TextureInfo[] textures)
        {
            // Sort textures by AREA, DESCENDING
            Array.Sort(textures, (a, b) => (b.Width*b.Height) - (a.Width*a.Height));

            FreeSpots = new Array<Rectangle>();
            int initialWidth  = Math.Max(textures[0].Width,  128).RoundPowerOf2();
            int initialHeight = Math.Max(textures[0].Height, 128).RoundPowerOf2();
            Width  = initialWidth;
            Height = initialHeight;
            int cursorX = 0;
            int cursorY = 0;
            int bottomY = 0;

            // an 48x96 free spot filled by a 48x48 tex, will leave
            // a 48x47 slot, which can no longer be filled
            // so we add additional padding to cursor movement
            const int cursorPadding = Padding * 2;

            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo td = textures[i];

                // check if we can fit anything into existing free spots:
                if (FillFreeSpot(td))
                    continue;

                int remainingX = Width - cursorX;
                if (remainingX < td.Width)
                {
                    int remainingY = bottomY - cursorY;
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                        FreeSpots.Add(new Rectangle(cursorX, cursorY, remainingX, remainingY));
                    cursorX = 0;
                    cursorY = bottomY + Padding + cursorPadding;
                }
                int newBottomY = cursorY + td.Height;
                if (newBottomY > bottomY) bottomY = newBottomY;
                while (bottomY > Height) { Height *= 2; }
                
                if (Height >= Width*2) // reset everything if Height is double of Width
                {
                    i = -1;
                    Width *= 2;
                    Height = initialHeight;
                    cursorX = cursorY = bottomY = 0;
                    FreeSpots.Clear();
                    continue;
                }

                td.X = cursorX;
                td.Y = cursorY;
                int fillX = (td.Width + Padding + cursorPadding);
                cursorX += fillX;

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in descending order.
                // ____________
                // |  tdfill  |
                // |__________|
                // | freespot |
                if (fillX >= MinFreeSpotSize)
                {
                    int freeSpotY = (td.Y + td.Height + Padding);
                    int freeSpotH = (bottomY - freeSpotY) + cursorPadding;
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new Rectangle(td.X, freeSpotY, fillX, freeSpotH));
                    }
                }
            }
        }

        void DrawRectangle(Color[] image, Rectangle r, Color color)
        {
            if (r.Height == 0) {Log.Error("DrawRectangle Height cannot be 0");return;}
            if (r.Width == 0) {Log.Error("DrawRectangle Height cannot be 0");return;}

            int x = r.X;
            int y = r.Y;
            int endX = x + (r.Width - 1);
            if (endX >= Width) endX = Width - 1;
            int endY = y + (r.Height - 1);
            if (endY >= Height) endY = Height - 1;

            for (int ix = x; ix <= endX; ++ix) // top and bottom ----
            {
                image[(y*Width) + ix] = color;
                image[(endY*Width) + ix] = color;
            }
            for (int iy = y; iy <= endY; ++iy) // | left and right |
            {
                image[(iy*Width) + x] = color;
                image[(iy*Width) + endX] = color;
            }
        }

        void CreateAtlasTexture(GameContentManager content, TextureInfo[] textures)
        {
            Stopwatch s = Stopwatch.StartNew();
            var atlasColorData = new Color[Width * Height];
            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo td = textures[i];
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

            foreach (Rectangle r in FreeSpots)
                DrawRectangle(atlasColorData, r, Color.AliceBlue);
            FreeSpots = null;

            //HelperFunctions.CollectMemorySilent();
            // We compress the DDS color into DXT5 and then reload it through XNA
            ImageUtils.SaveAsDDS(TexturePath, Width, Height, atlasColorData);
            atlasColorData = null;
            
            //HelperFunctions.CollectMemorySilent();
            Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);
            //Atlas.Save($"{TextureCacheDir}/{CacheName}.png", ImageFileFormat.Png); // DEBUG, Slooooooowwww

            // Initialize atlas reference
            foreach (SubTexture t in Textures.Values)
                t.Atlas = Atlas;

            Log.Info($"CreateTexture    {SizeString} {Name} elapsed:{s.Elapsed.TotalMilliseconds}ms");
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
                Width = Atlas.Width;
                Height = Atlas.Height;

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
                    t.Atlas = Atlas;
                    Textures.Add(t.Name, t);
                }
                CreateSortedList();
            }
            Log.Info($"LoadAtlas    {Textures.Count,3} {SizeString} {Name}");
            return true; // we loaded everything
        }

        static TextureInfo[] LoadTextureInfo(GameContentManager content, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];
            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetPath = info.CleanResPath(false);
                string texName = info.NameNoExt();
                var tex = content.LoadUncached<Texture2D>(assetPath);
                textures[i] = new TextureInfo
                {
                    Name = texName,
                    Width = tex.Width,
                    Height = tex.Height,
                    Texture = tex,
                };
            }
            return textures;
        }

        void CreateSortedList()
        {
            SortedTextures = Textures.Values.ToArray();
            Array.Sort(SortedTextures, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useCache = true)
        {
            var atlas = new TextureAtlas(folder);

            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            if (textureFiles.Length == 0)
                return atlas;

            atlas.Hash = CreateHash(textureFiles);
            if (useCache && atlas.TryLoadCache(content))
                return atlas;

            TextureInfo[] textures = LoadTextureInfo(content, textureFiles);
            atlas.PackTextures(textures);
            atlas.CreateAtlasTexture(content, textures);
            atlas.SaveAtlasDescriptor();
            atlas.CreateSortedList();
            return atlas;
        }
    }
}
