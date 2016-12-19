using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class SubTexture
    {
        // name="sprite1" x="461" y="1317" rotated="true" width="28" height="41" frameX="-237" frameY="-116" frameWidth="512" frameHeight="264"
        public string Name;        // name of the sprite for name-based lookup
        public int X, Y;           // position in sprite sheet
        public int Width, Height;  // actual size in sprite sheet
        public int FrameX, FrameY; // trimmed offset from the original frame
        public int FrameWidth, FrameHeight; // original size of the frame before trimming
        public bool Rotated;       // rotated -90 ?
    }

    /// Generic TextureAtlas can be 
    public class TextureAtlas
    {
        public string Name { get; private set; }

        private Texture[] Textures;
        private List<SubTexture> Sprites = new List<SubTexture>();

        public TextureAtlas()
        {
        }

        public static TextureAtlas Load(ContentManager content, string path)
        {

            return null;
        }
    }
}
