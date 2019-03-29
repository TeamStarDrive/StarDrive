using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.Texture
{
    public class TextureInterface
    {
        readonly GameContentManager Content;

        // This must be lazy init, because content manager is instantiated before
        // graphics device is initialized
        protected GraphicsDevice Device => Content.Manager.GraphicsDevice;

        protected TextureInterface(GameContentManager content)
        {
            Content = content;
        }
    }
}
