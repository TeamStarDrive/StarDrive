using System;
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
    public class TextureAtlas
    {
        public string Name { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

        public  readonly Array<Texture2D> Textures = new Array<Texture2D>();
        private readonly Array<SubTexture> Sprites = new Array<SubTexture>();

        protected TextureAtlas(string name)
        {
            Name = name;
        }

        protected TextureAtlas(GraphicsDevice device, string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
            Atlas = new Texture2D(device, width, height, 1, TextureUsage.AutoGenerateMipMap, SurfaceFormat.Color);
        }

        public int SpriteCount => Sprites.Count;
        public SubTexture this[int spriteIndex] => Sprites[spriteIndex];

        public bool Good => Textures.Count > 0 && Sprites.Count > 0;

        // Performs a binary search on the sprites
        public SubTexture this[string name]
        {
            get
            {
                int imin = 0, imax = Sprites.Count - 1;
                if (imax >= 0) // since names are sorted, do binary search:
                {
                    while (imin < imax)
                    {
                        int imid = (imin + imax) >> 1;
                        if (string.CompareOrdinal(Sprites[imid].Name, name) < 0)
                            imin = imid + 1;
                        else
                            imax = imid;
                    }
                    if (imin <= imax && Sprites[imin].Name == name)
                        return Sprites[imin];
                }
                return null; // not found
            }
        }

        // @todo Make this fit into ResourceManager?
        private static bool GetAtlasDescr(string textureName, out FileInfo outInfo)
        {
            outInfo = ResourceManager.GetModOrVanillaFile("Textures/" + textureName + ".xml");
            return outInfo != null;
        }

        // Since we do binary search by Sprite name, we need to sort them after loading
        private void SortSprites()
        {
            Sprites.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private static string PrepareTextureCacheDir()
        {
            string dir = Dir.StarDriveAppData + "/TextureCache";
            Directory.CreateDirectory(dir);
            return dir;
        }
        private static readonly string TextureCacheDir = PrepareTextureCacheDir();

        private struct TextureData
        {
            public string Name;
            public int X, Y;
            public int Width, Height;
            public Color[] ColorData;
            public override string ToString() => $"{{{X},{Y}}} {Width}x{Height}";

            public void SaveAsPng(string filename)
            {
                ImageUtils.SaveAsPng(filename, Width, Height, ColorData);
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

        private static FileInfo[] GatherUniqueTextures(string folder)
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

        private static TextureData GetTextureData(string texName, Texture2D tex)
        {
            var td = new TextureData
            {
                Name   = texName,
                Width  = tex.Width,
                Height = tex.Height,
            };

            if (tex.Format == SurfaceFormat.Dxt5)
            {
                td.ColorData = ImageUtils.DecompressDXT5(tex);
            }
            else if (tex.Format == SurfaceFormat.Dxt1)
            {
                td.ColorData = ImageUtils.DecompressDXT1(tex);
            }
            else if (tex.Format == SurfaceFormat.Color)
            {
                td.ColorData = new Color[tex.Width * tex.Height];
                tex.GetData(td.ColorData);
            }
            else
            {
                Log.Error($"Unsupported atlas texture format: {tex.Format}");
            }
            td.SaveAsPng($"{TextureCacheDir}/{texName}.png");
            return td;
        }

        static void PackTextures(TextureData[] textures, out int atlasWidth, out int atlasHeight)
        {
            var freeSpots = new Array<Rectangle>();

            atlasWidth = 2048;
            atlasHeight = 1024;
            int cursorX = 0;
            int cursorY = 0;
            int bottomY = 0;
            const int padding = 2;

            for (int i = 0; i < textures.Length; ++i)
            {
                ref TextureData td = ref textures[i];
                int remainingX = atlasWidth - cursorX;
                if (remainingX < td.Width)
                {
                    freeSpots.Add(new Rectangle(cursorX, cursorY, remainingX, bottomY-cursorY));
                    cursorX = 0;
                    cursorY = bottomY + padding;
                }

                int thisBottomY = cursorY + td.Height;
                if (thisBottomY >= bottomY) bottomY = thisBottomY;
                while (bottomY >= atlasHeight) atlasHeight += 1024;

                td.X = cursorX;
                td.Y = cursorY;
                cursorX += td.Width + padding;
            }

            Console.WriteLine($"Atlas: {atlasWidth}x{atlasHeight}px");
        }

        void CopyTextureData(TextureData[] textures)
        {
            var atlasColorData = new Color[Width * Height];
            var subTextures = new SubTexture[textures.Length];
            for (int i = 0; i < textures.Length; ++i)
            {
                ref TextureData td = ref textures[i];
                td.CopyPixelsTo(atlasColorData, Width);
                subTextures[i] = new SubTexture
                {
                    Name = td.Name,
                    X = td.X,
                    Y = td.Y,
                    Width = td.Width,
                    Height = td.Height,
                    Atlas = Atlas
                };
            }

            Atlas.SetData(atlasColorData);
            Atlas.Save($"{TextureCacheDir}/atlas.png", ImageFileFormat.Png);
        }

        public static TextureAtlas CreateFromFolder(GameContentManager content, string folder)
        {
            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            var textures = new TextureData[textureFiles.Length];

            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetPath = info.CleanResPath(false);
                string texName = info.NameNoExt();
                using (var tex = content.LoadUncached<Texture2D>(assetPath))
                {
                    Console.WriteLine($"texture: {texName}  {tex.Width}x{tex.Height}  {tex.Format}");
                    textures[i] = GetTextureData(texName, tex);
                }
            }

            Array.Sort(textures, (a, b) => // Sort textures in DESCENDING order
            {
                if (a.Width > b.Width) return -1;
                if (a.Width < b.Width) return +1;
                if (a.Height > b.Height) return -1;
                if (a.Height < b.Height) return +1;
                return 0;
            });

            PackTextures(textures, out int atlasWidth, out int atlasHeight);

            var atlas = new TextureAtlas(content.Manager.GraphicsDevice, folder, atlasWidth, atlasHeight);
            atlas.CopyTextureData(textures);
            return atlas;
        }

        public static TextureAtlas LoadExisting(GameContentManager content, string atlasTexture)
        {
            if (GetAtlasDescr(atlasTexture, out FileInfo info))
            {
                var atlas = new TextureAtlas(atlasTexture);
                atlas.LoadDescriptor(info, atlasTexture);
                atlas.SortSprites();
                return atlas;
            }

            // try multipack atlasses
            TextureAtlas multiAtlas = null;
            for (int i = 0; ; ++i)
            {
                string multiName = atlasTexture + '-' + i;
                if (!GetAtlasDescr(multiName, out info))
                    break;

                if (multiAtlas == null)
                    multiAtlas = new TextureAtlas(atlasTexture);

                multiAtlas.LoadDescriptor(info, multiName);
            }
            multiAtlas?.SortSprites();
            return multiAtlas;
        }

        private static XmlNode ChildNode(XmlNode node, string childTagName)
        {
            foreach (XmlNode child in node.ChildNodes)
                if (child.Name == childTagName) return child;
            return null;
        }

        private static int GetInt(XmlNamedNodeMap attr, string name)
        {
            var node = attr.GetNamedItem(name);
            return node != null ? int.Parse(node.Value) : 0;
        }

        private void LoadDescriptor(FileInfo descriptorInfo, string textureName)
        {
            var xml = new XmlDocument();
            using (var reader = descriptorInfo.OpenRead())
                xml.Load(reader);

            var root = ChildNode(xml, "TextureAtlas");
            if (root == null)
            {
                Log.Error("Invalid TextureAtlas XML: {0}.xml", textureName);
                return;
            }

            Texture2D tex = ResourceManager.LoadTexture(textureName);
            if (tex == null)
            {
                Log.Error("Invalid TextureAtlas xnb: {0}.xnb", textureName);
                return;
            }
            Textures.Add(tex);

            foreach (XmlNode xSubTex in root.ChildNodes)
            {
                var attr = xSubTex.Attributes;
                string name = attr?["name"]?.Value;
                if (name == null) continue;

                var subTex = new SubTexture
                {
                    Name        = name,
                    X           = GetInt(attr, "x"),
                    Y           = GetInt(attr, "y"),
                    Width       = GetInt(attr, "width"),
                    Height      = GetInt(attr, "height"),
                };
                Sprites.Add(subTex);
            }
        }
    }
}
