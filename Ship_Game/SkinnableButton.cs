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
        public Color HoverColor            = Color.White;
        public Color BaseColor             = Color.White;
        private readonly Color ToggleColor = new Color(33, 26, 18);
        private readonly SubTexture Texture;
        private readonly SubTexture SecondTex;
        private readonly SubTexture BackGroundTex; // Can draw a background tex under this button
        public Color BackGroundTexColor { get; private set; } = Color.Black;
        private string tPath;

        public SkinnableButton(Rectangle r, string texturePath)
        {
            tPath     = texturePath;
            Texture   = ResourceManager.Texture(tPath);
            this.r    = r;
        }
        public SkinnableButton(Rectangle r, SubTexture texture, SubTexture secondary, SubTexture backgroundTex = null)
        {
            Texture       = texture;
            this.r        = r;
            SecondTex     = secondary;
            BackGroundTex = backgroundTex;
        }

        public void Draw(SpriteBatch batch)
        {
            if (Toggled)
                batch.FillRectangle(r, ToggleColor);

            if (BackGroundTex != null && BackGroundTexColor != Color.Black)
                batch.Draw(BackGroundTex, r, BackGroundTexColor);

            batch.Draw(Texture, r, Hover ? HoverColor : BaseColor);
            if (SecondTex != null)
                batch.Draw(SecondTex, r, Hover ? HoverColor : BaseColor);
        }

        /// <summary>
        /// Updates the background texture color, if applicable. Can be used for blinking textures.
        /// </summary>
        public void UpdateBackGroundTexColor(Color color)
        {
            BackGroundTexColor = color;
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