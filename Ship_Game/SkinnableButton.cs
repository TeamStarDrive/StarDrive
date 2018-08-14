using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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
        private readonly Texture2D Texture;

        public SkinnableButton(Rectangle r, string TexturePath)
        {
            this.tPath = TexturePath;
            Texture = ResourceManager.Texture(tPath);
            this.r = r;
        }
        public SkinnableButton(Rectangle r, Texture2D texture)
        {
            Texture = texture;
            this.r = r;
        }

        public void Draw(ScreenManager screenManager)
        {
            if (this.Toggled)
            {
                screenManager.SpriteBatch.FillRectangle(this.r, this.ToggleColor);
            }
         
            screenManager.SpriteBatch.Draw(Texture, this.r, (this.Hover ? this.HoverColor : this.BaseColor));
            if (this.SecondSkin != null)
            {
                if (this.Toggled)
                {
                    Rectangle secondRect = new Rectangle(this.r.X + this.r.Width / 2 - ResourceManager.Texture(this.SecondSkin).Width / 2, this.r.Y + this.r.Height / 2 - ResourceManager.Texture(this.SecondSkin).Height / 2, ResourceManager.Texture(this.SecondSkin).Width, ResourceManager.Texture(this.SecondSkin).Height);
                    screenManager.SpriteBatch.Draw(ResourceManager.Texture(this.SecondSkin), secondRect, Color.White);
                    return;
                }
                Rectangle secondRect0 = new Rectangle(this.r.X + this.r.Width / 2 - ResourceManager.Texture(this.SecondSkin).Width / 2, this.r.Y + this.r.Height / 2 - ResourceManager.Texture(this.SecondSkin).Height / 2, ResourceManager.Texture(this.SecondSkin).Width, ResourceManager.Texture(this.SecondSkin).Height);
                screenManager.SpriteBatch.Draw(ResourceManager.Texture(this.SecondSkin), secondRect0, (this.Hover ? Color.LightGray : Color.Black));
            }
        }

        public bool HandleInput(InputState input)
        {
            if (!this.r.HitTest(input.CursorPosition))
            {
                this.Hover = false;
            }
            else
            {
                this.Hover = true;
                if (input.InGameSelect)
                {
                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                    if (this.IsToggle)
                    {
                        this.Toggled = !this.Toggled;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}