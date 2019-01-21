using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace Ship_Game.Universe.SolarBodies
{
    public class SunType
    {
        [StarDataKey] public string Id;
        [StarData] public string IconPath;
        [StarData] public float Intensity = 1.5f;
        [StarData] public float Radius = 150000f;
        [StarData] public Color Color = Color.White;

        public Texture2D Texture; // small texture
        public SubTexture LoResIcon; // lo-res icon used in background star fields
        public SubTexture HiRes; // hi-res texture applied on a 3D object

        public void DrawIcon(SpriteBatch batch, Rectangle rect)
        {
            batch.Draw(LoResIcon, rect, Color.White);
        }
    }
}
