using System.IO;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SubTexture
    {
        // name="sprite1" x="461" y="1317" rotated="true" width="28" height="41" frameX="-237" frameY="-116" frameWidth="512" frameHeight="264"
        public string Name;        // name of the sprite for name-based lookup
        public int X, Y;           // position in sprite sheet
        public int Width, Height;  // actual size of subtexture in sprite sheet
        public int FrameX, FrameY; // trimmed offset from the original frame
        public int FrameWidth, FrameHeight; // original size of the frame before trimming
        public bool Rotated;       // rotated -90 ?
        public Texture2D Texture; // associated texture
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

        // Since we do binary search by Sprite name, we need to sort them
        private void SortSprites()
        {
            Sprites.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        public static TextureAtlas Load(GameContentManager content, string textureName)
        {
            if (GetAtlasDescr(textureName, out FileInfo info))
            {
                var atlas = new TextureAtlas(textureName);
                atlas.LoadDescriptor(info, textureName);
                atlas.SortSprites();
                return atlas;
            }

            // try multipack atlasses
            TextureAtlas multiAtlas = null;
            for (int i = 0; ; ++i)
            {
                string multiName = textureName + '-' + i;
                if (!GetAtlasDescr(multiName, out info))
                    break;

                if (multiAtlas == null)
                    multiAtlas = new TextureAtlas(textureName);

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
        private static bool GetBool(XmlNamedNodeMap attr, string name)
        {
            var node = attr.GetNamedItem(name);
            return node != null && bool.Parse(node.Value);
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
                    Rotated     = GetBool(attr,"rotated"),
                    Width       = GetInt(attr, "width"),
                    Height      = GetInt(attr, "height"),
                    FrameX      = GetInt(attr, "frameX"),
                    FrameY      = GetInt(attr, "frameY"),
                    FrameWidth  = GetInt(attr, "frameWidth"),
                    FrameHeight = GetInt(attr, "frameHeight")
                };
                Sprites.Add(subTex);
            }
        }
    }
}
