using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class SkinnableButton
    {
        public Rectangle r;
        public object ReferenceObject;
        public string Action = "";
        public bool IsToggle = true;
        public bool Toggled;
        public bool Hover;
        public int WhichToolTip;
        public bool HasToolTip;
        public Color HoverColor = Color.White;
        public Color BaseColor = Color.White;
        private readonly Color ToggleColor = new Color(33, 26, 18);
        private readonly SubTexture Texture;
        private readonly SubTexture SecondTex;
        private string tPath;

        public SkinnableButton(Rectangle r, string texturePath)
        {
            tPath   = texturePath;
            Texture = ResourceManager.Texture(tPath);
            this.r  = r;
        }
        public SkinnableButton(Rectangle r, SubTexture texture, SubTexture secondary)
        {
            Texture   = texture;
            this.r    = r;
            SecondTex = secondary;
        }

        public void Draw(SpriteBatch batch)
        {
            if (Toggled)
                batch.FillRectangle(r, ToggleColor);
         
            batch.Draw(Texture, r, Hover ? HoverColor : BaseColor);
            if (SecondTex != null)
                batch.Draw(SecondTex, r, Hover ? HoverColor : BaseColor);
        }

        public bool HandleInput(InputState input)
        {
            if (!r.HitTest(input.CursorPosition))
            {
                Hover = false;
            }
            else
            {
                Hover = true;
                if (input.InGameSelect)
                {
                    GameAudio.AcceptClick();
                    if (IsToggle)
                    {
                        Toggled = !Toggled;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}