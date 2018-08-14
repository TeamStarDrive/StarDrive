using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public class Node
    {
        public TechEntry tech;

        public Rectangle NodeRect;

        public Vector2 NodePosition;
        
        public bool isResearched;

        public DanProgressBar pb;

        public Node()
        {
        }

        public void Draw(Ship_Game.ScreenManager ScreenManager, Empire e)
        {
            ScreenManager.SpriteBatch.FillRectangle(this.NodeRect, new Color(54, 54, 54));
            Rectangle underRect = new Rectangle(this.NodeRect.X, this.NodeRect.Y + 28, this.NodeRect.Width, 22);
            ScreenManager.SpriteBatch.FillRectangle(underRect, new Color(37, 37, 37));
            if (!this.isResearched)
            {
                ScreenManager.SpriteBatch.DrawRectangle(this.NodeRect, Color.Black);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawRectangle(this.NodeRect, new Color(243, 134, 53));
            }
            if (e.ResearchTopic == this.tech.UID)
            {
                ScreenManager.SpriteBatch.DrawRectangle(this.NodeRect, new Color(24, 81, 91), 3f);
            }
            else if (e.data.ResearchQueue.Contains(this.tech.UID))
            {
                ScreenManager.SpriteBatch.DrawRectangle(this.NodeRect, Color.White, 3f);
            }
            Vector2 cursor = new Vector2((float)(this.NodeRect.X + 10), (float)(this.NodeRect.Y + 4));
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(ResourceManager.TechTree[this.tech.UID].NameIndex), cursor, Fonts.Arial12Bold);
            Rectangle BlackBar = new Rectangle(this.NodeRect.X + 215, this.NodeRect.Y + 1, 1, this.NodeRect.Height - 2);
            ScreenManager.SpriteBatch.FillRectangle(BlackBar, Color.Black);
            Rectangle rIconRect = new Rectangle(BlackBar.X + 4, BlackBar.Y + 4, 19, 20);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIconRect, Color.White);
            cursor.X = (float)(rIconRect.X + 24);
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            int cost = (int)ResourceManager.TechTree[this.tech.UID].Cost;
            spriteBatch.DrawString(arial12Bold, cost.ToString(), cursor, Color.White);
            float progress = this.tech.Progress / ResourceManager.TechTree[this.tech.UID].Cost;
            this.pb.Draw(ScreenManager, progress);
            cursor.X = (float)(this.pb.rect.X + this.pb.rect.Width + 33);
            cursor.Y = (float)(this.pb.rect.Y - 2);
            int num = (int)(progress * 100f);
            string s = string.Concat(num.ToString(), "%");
            cursor.X = cursor.X - Fonts.Arial12Bold.MeasureString(s).X;
            ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, s, cursor, new Color(24, 81, 91));
        }

        public virtual bool HandleInput(InputState input)
        {
            return false;
        }
    }
}