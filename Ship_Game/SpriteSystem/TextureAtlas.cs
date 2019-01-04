using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SubTexture
    {
        // name="sprite1" x="461" y="1317" width="28" height="41"
        public readonly string Name;        // name of the sprite for name-based lookup
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly Texture2D Texture;  // texture

        public Rectangle Rect => new Rectangle(X, Y, Width, Height);
        public int Right  => X + Width;
        public int Bottom => Y + Height;

        public SubTexture(string name, int x, int y, int w, int h, Texture2D texture)
        {
            Name = name;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            Texture = texture;
        }

        // special case: SubTexture is a container for a full texture
        public SubTexture(Texture2D fullTexture)
        {
            Name   = "";
            Width  = fullTexture.Width;
            Height = fullTexture.Height;
            Texture  = fullTexture;
        }

        // UV-coordinates
        public float CoordLeft   => X / (float)Texture.Width;
        public float CoordTop    => Y / (float)Texture.Height;
        public float CoordRight  => (X + (Width  - 1)) / (float)Texture.Width;
        public float CoordBottom => (Y + (Height - 1)) / (float)Texture.Height;

        public Vector2 CoordUpperLeft  => new Vector2(CoordLeft, CoordTop);
        public Vector2 CoordLowerLeft  => new Vector2(CoordLeft, CoordBottom);
        public Vector2 CoordLowerRight => new Vector2(CoordRight, CoordBottom);
        public Vector2 CoordUpperRight => new Vector2(CoordRight, CoordTop);

        public Vector2 Center() => new Vector2(Width / 2f, Height / 2f);
        public Vector2 Size()   => new Vector2(Width, Height);

        public override string ToString()
            => $"sub-tex  {Name} {Rect.X},{Rect.Y} {Rect.Width}x{Rect.Height}  atlas:{Texture.Width}x{Texture.Height}";
    }

    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public class TextureAtlas : IDisposable
    {
        const int Version = 1; // changing this will force all caches to regenerate
        const int Padding = 2; // Atlas texture padding
        const int MinFreeSpotSize = 16; // Minimum width/height for recycled free spots

        public string Name { get; private set; }
        readonly string CacheName;
        string DescriptorPath => $"{TextureCacheDir}/{CacheName}.atlas";
        string TexturePath    => $"{TextureCacheDir}/{CacheName}.dds";
        int Hash;
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }
        Array<Rectangle> FreeSpots; // non-local for debugging purposes

        readonly Map<string, SubTexture> Lookup = new Map<string, SubTexture>();
        SubTexture[] Sorted = Empty<SubTexture>.Array;
        readonly Array<Texture2D> Owned = new Array<Texture2D>();

        protected TextureAtlas(string name)
        {
            Name = name;

            CacheName = name;
            if (CacheName.StartsWith("Textures/"))
                CacheName = CacheName.Substring("Textures/".Length);
            CacheName = CacheName.Replace('/', '_');
        }

        ~TextureAtlas()
        {
            Destroy();
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        void Destroy()
        {
            Atlas?.Dispose();
            for (int i = 0; i < Owned.Count; ++i)
                Owned[i].Dispose();
            Owned.Clear();
        }

        public string SizeString => $"{$"{Width}x{Height}",9}";
        public override string ToString() => $"atlas {Count} {SizeString} {Name}";

        public IReadOnlyList<SubTexture> Textures => Sorted;
        public int Count => Sorted.Length;
        public SubTexture this[int index] => Sorted[index];
        public SubTexture this[string name] => Lookup[name];

        public bool TryGetTexture(string name, out SubTexture texture)
        {
            return Lookup.TryGetValue(name, out texture);
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
            public bool NoPack; // This texture should not be packed

            public void CopyPixelsTo(Color[] atlas, int atlasWidth)
            {
                if (Texture == null)
                    throw new ObjectDisposedException("TextureData Texture2D ref already disposed");

                Color[] colorData;
                if (Texture.Format == SurfaceFormat.Dxt5)
                    colorData = ImageUtils.DecompressDXT5(Texture);
                else if (Texture.Format == SurfaceFormat.Dxt1)
                    colorData = ImageUtils.DecompressDXT1(Texture);
                else if (Texture.Format == SurfaceFormat.Color) {
                    colorData = new Color[Texture.Width * Texture.Height];
                    Texture.GetData(colorData);
                } else {
                    colorData = new Color[0];
                    Log.Error($"Unsupported atlas texture format: {Texture.Format}");
                }
                Texture.Dispose(); // save some memory
                Texture = null;

                //ImageUtils.SaveAsPng($"{TextureCacheDir}/{texName}.png", Width, Height, ColorData);
                ImageUtils.CopyPixelsTo(atlas, atlasWidth, X, Y, colorData, Width, Height);
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
            int initialWidth  = 128;
            int initialHeight = 128;
            Width  = initialWidth;
            Height = initialHeight;
            int cursorX = 0;
            int cursorY = 0;
            int bottomY = 0;

            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                if (t.Width >= 512 || t.Height >= 512)
                {
                    t.NoPack = true;
                    //Log.Info($"NoPack {t.Name} {t.Width}x{t.Height}");
                    //Directory.CreateDirectory($"{TextureCacheDir}/NoPack");
                    //t.Texture.Save($"{TextureCacheDir}/NoPack/{t.Name}.dds", ImageFileFormat.Dds);
                    continue;
                }

                // check if we can fit anything into existing free spots:
                if (FillFreeSpot(t))
                    continue;

                int remainingX = Width - cursorX;
                if (remainingX < t.Width)
                {
                    int remainingY = bottomY - cursorY;
                    if (remainingX >= MinFreeSpotSize && remainingY >= MinFreeSpotSize)
                        FreeSpots.Add(new Rectangle(cursorX, cursorY, remainingX, remainingY));
                    cursorX = 0;
                    cursorY = bottomY + Padding;
                }
                int newBottomY = cursorY + (t.Height);
                if (newBottomY > bottomY) bottomY = newBottomY;
                while (bottomY > Height) { Height += 64; }
                
                if (Height >= Width*2) // reset everything if Height is double of Width
                {
                    i = -1;
                    Width *= 2;
                    Height = initialHeight;
                    cursorX = cursorY = bottomY = 0;
                    FreeSpots.Clear();
                    continue;
                }

                t.X = cursorX;
                t.Y = cursorY;
                int fillX = (t.Width + Padding);
                cursorX += fillX;

                // After filling our spot, there is a potential free spot.
                // We know this because we fill our objects in descending order.
                // ____________
                // |  tdfill  |
                // |__________|
                // | freespot |
                if (fillX >= MinFreeSpotSize)
                {
                    int freeSpotY = (t.Y + t.Height + Padding);
                    int freeSpotH = (bottomY - freeSpotY);
                    if (freeSpotH >= MinFreeSpotSize)
                    {
                        FreeSpots.Add(new Rectangle(t.X, freeSpotY, fillX, freeSpotH));
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

            bool hasAlpha = false;
            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                if (t.NoPack)
                {
                    Owned.Add(t.Texture);
                }
                else
                {
                    if (t.Texture.Format == SurfaceFormat.Color ||
                        t.Texture.Format == SurfaceFormat.Dxt5)
                        hasAlpha = true;
                    textures[i].CopyPixelsTo(atlasColorData, Width);
                }
            }

            // DEBUG only!
            //foreach (Rectangle r in FreeSpots)
            //    DrawRectangle(atlasColorData, r, Color.AliceBlue);
            FreeSpots = null;

            bool compress = Width > 1024 && Height > 1024;
            if (compress)
            {
                // We compress the DDS color into DXT5 and then reload it through XNA
                ImageUtils.ConvertToRGBA(Width, Height, atlasColorData);
                ImageUtils.SaveAsDDS(TexturePath, Width, Height, atlasColorData);
                //ImageUtils.SaveAsPNG(TexturePathPNG, Width, Height, atlasColorData); // DEBUG!

                // DXT5 Compressed size in memory is good, but quality sucks!
                Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);
            }
            else
            {
                // Uncompressed DDS, lossless quality, fast loading, big size in memory :(
                Atlas = new Texture2D(content.Manager.GraphicsDevice, Width, Height, 1, TextureUsage.Linear, SurfaceFormat.Color);
                Atlas.SetData(atlasColorData);
                Atlas.Save(TexturePath, ImageFileFormat.Dds);
            }

            for (int i = 0; i < textures.Length; ++i)
            {
                TextureInfo t = textures[i];
                Texture2D tex = t.NoPack ? t.Texture : Atlas;
                Lookup[t.Name] = new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, tex);
            }

            Log.Info($"CreateTexture    {SizeString} {Name} alpha:{hasAlpha} elapsed:{s.Elapsed.TotalMilliseconds}ms");
            SaveAtlasDescriptor(textures);
        }

        void SaveAtlasDescriptor(TextureInfo[] textures)
        {
            using (var fs = new StreamWriter(DescriptorPath))
            {
                fs.WriteLine(Hash);
                fs.WriteLine(Name);
                for (int i = 0; i < textures.Length; ++i)
                {
                    TextureInfo t = textures[i];
                    string pack = t.NoPack ? "nopack" : "atlas";
                    fs.WriteLine($"{pack} {t.X} {t.Y} {t.Width} {t.Height} {t.Name}");
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

                Lookup.Clear();
                Name = fs.ReadLine();
                Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);
                Width = Atlas.Width;
                Height = Atlas.Height;

                var separator = new[] { ' ' };

                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    string[] entry = line.Split(separator, 6);
                    string what = entry[0];
                    int.TryParse(entry[1], out int x);
                    int.TryParse(entry[2], out int y);
                    int.TryParse(entry[3], out int w);
                    int.TryParse(entry[4], out int h);
                    string sprite = entry[5];

                    Texture2D tex = Atlas;
                    if (what == "nopack")
                    {
                        tex = content.LoadUncached<Texture2D>($"{Name}/{sprite}");
                        Owned.Add(tex);
                    }
                    Lookup.Add(sprite, new SubTexture(sprite, x, y, w, h, tex));
                }
                CreateSortedList();
            }
            Log.Info($"LoadAtlas    {Lookup.Count,3} {SizeString} {Name}");
            return true; // we loaded everything
        }

        static TextureInfo[] LoadTextureInfo(GameContentManager content, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];
            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetName = info.CleanResPath(false);
                string texName = info.NameNoExt();
                var tex = content.LoadUncached<Texture2D>(assetName);
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
            Sorted = Lookup.Values.ToArray();
            Array.Sort(Sorted, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useCache = true)
        {
            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            if (textureFiles.Length == 0)
                return null; // no textures!!

            var atlas = new TextureAtlas(folder) { Hash = CreateHash(textureFiles) };
            if (useCache && atlas.TryLoadCache(content))
                return atlas;

            TextureInfo[] textures = LoadTextureInfo(content, textureFiles);
            atlas.PackTextures(textures);
            atlas.CreateAtlasTexture(content, textures);
            atlas.CreateSortedList();
            HelperFunctions.CollectMemorySilent();
            return atlas;
        }
    }
}
