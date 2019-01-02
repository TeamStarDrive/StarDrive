using System;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;

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

        public  readonly Array<Texture2D> Textures = new Array<Texture2D>();
        private readonly Array<SubTexture> Sprites = new Array<SubTexture>();

        protected TextureAtlas(string name)
        {
            Name = name;
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

        private struct TextureData
        {
            public string Name;
            public int Width, Height;
            public Color[] ColorData;
            public byte[] RgbaData;
        }

        public static TextureAtlas CreateFromFolder(GameContentManager content, string folder)
        {
            FileInfo[] textureFiles = ResourceManager.GatherTextureFiles(folder);
            var textureData = new Array<TextureData>(textureFiles.Length);

            foreach (FileInfo info in textureFiles)
            {
                string relPath = info.CleanResPath(false);
                string relPathNoExt = relPath.Substring(0, relPath.Length - 4);
                string texName = relPathNoExt.Substring("Textures/".Length);

                Texture2D texture = ResourceManager.LoadTexture(texName);
                Console.WriteLine($"texture: {texName}  {texture.Width}x{texture.Height}  {texture.Format}");
                
                var data = new TextureData
                {
                    Name   = texName,
                    Width  = texture.Width,
                    Height = texture.Height,
                };

                if (texture.Format == SurfaceFormat.Dxt5)
                {
                    var rawTexture = new byte[data.Width * data.Height];
                    texture.GetData(rawTexture);
                    data.RgbaData = DDSImage.DecompressData(data.Width, data.Height, rawTexture, DDSImage.PixelFormat.DXT5);
                }
                else if (texture.Format == SurfaceFormat.Color)
                {
                    data.ColorData = new Color[texture.Width * texture.Height];
                    texture.GetData(data.ColorData);
                }
                else
                {
                    Log.Error($"Unsupported atlas texture format: {texture.Format}");
                }

                textureData.Add(data);
            }

            // Sort textures in DESCENDING order
            textureData.Sort((a, b) =>
            {
                if (a.Width > b.Width) return -1;
                if (a.Width < b.Width) return +1;
                if (a.Height > b.Height) return -1;
                if (a.Height < b.Height) return +1;
                return 0;
            });

            foreach (TextureData texture in textureData)
            {

            }
            return null;
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
