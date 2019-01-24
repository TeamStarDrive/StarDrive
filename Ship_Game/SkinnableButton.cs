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

        private string tPath;

        public int WhichToolTip;

        public bool HasToolTip;

        public string SecondSkin;

        public Color HoverColor = Color.White;

        public Color BaseColor = Color.White;

        private readonly Color ToggleColor = new Color(33, 26, 18);
        private readonly SubTexture Texture;

        public SkinnableButton(Rectangle r, string texturePath)
        {
            tPath = texturePath;
            Texture = ResourceManager.Texture(tPath);
            this.r = r;
        }
        public SkinnableButton(Rectangle r, SubTexture texture)
        {
            Texture = texture;
            this.r = r;
        }

        public void Draw(ScreenManager screenManager)
        {
            if (Toggled)
            {
                screenManager.SpriteBatch.FillRectangle(r, ToggleColor);
            }
         
            screenManager.SpriteBatch.Draw(Texture, r, (Hover ? HoverColor : BaseColor));
            if (SecondSkin != null)
            {
                if (Toggled)
                {
                    Rectangle secondRect = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture(SecondSkin).Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture(SecondSkin).Height / 2, ResourceManager.Texture(SecondSkin).Width, ResourceManager.Texture(SecondSkin).Height);
                    screenManager.SpriteBatch.Draw(ResourceManager.Texture(SecondSkin), secondRect, Color.White);
                    return;
                }
                Rectangle secondRect0 = new Rectangle(r.X + r.Width / 2 - ResourceManager.Texture(SecondSkin).Width / 2, r.Y + r.Height / 2 - ResourceManager.Texture(SecondSkin).Height / 2, ResourceManager.Texture(SecondSkin).Width, ResourceManager.Texture(SecondSkin).Height);
                screenManager.SpriteBatch.Draw(ResourceManager.Texture(SecondSkin), secondRect0, (Hover ? Color.LightGray : Color.Black));
            }
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