using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class Node
    {
        public TechEntry tech;

        public Rectangle NodeRect;

        public Vector2 NodePosition;
        
        public bool isResearched;

        public DanProgressBar pb;

        public void Draw(ScreenManager ScreenManager, Empire e)
        {
            ScreenManager.SpriteBatch.FillRectangle(NodeRect, new Color(54, 54, 54));
            Rectangle underRect = new Rectangle(NodeRect.X, NodeRect.Y + 28, NodeRect.Width, 22);
            ScreenManager.SpriteBatch.FillRectangle(underRect, new Color(37, 37, 37));
            if (!isResearched)
            {
                ScreenManager.SpriteBatch.DrawRectangle(NodeRect, Color.Black);
            }
            else
            {
                ScreenManager.SpriteBatch.DrawRectangle(NodeRect, new Color(243, 134, 53));
            }
            if (e.ResearchTopic == tech.UID)
            {
                ScreenManager.SpriteBatch.DrawRectangle(NodeRect, new Color(24, 81, 91), 3f);
            }
            else if (e.data.ResearchQueue.Contains(tech.UID))
            {
                ScreenManager.SpriteBatch.DrawRectangle(NodeRect, Color.White, 3f);
            }
            Vector2 cursor = new Vector2(NodeRect.X + 10, NodeRect.Y + 4);
            HelperFunctions.DrawDropShadowText(ScreenManager, Localizer.Token(ResourceManager.TechTree[tech.UID].NameIndex), cursor, Fonts.Arial12Bold);
            Rectangle BlackBar = new Rectangle(NodeRect.X + 215, NodeRect.Y + 1, 1, NodeRect.Height - 2);
            ScreenManager.SpriteBatch.FillRectangle(BlackBar, Color.Black);
            Rectangle rIconRect = new Rectangle(BlackBar.X + 4, BlackBar.Y + 4, 19, 20);
            ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("NewUI/icon_science"), rIconRect, Color.White);
            cursor.X = rIconRect.X + 24;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont arial12Bold = Fonts.Arial12Bold;
            int cost = (int)ResourceManager.TechTree[tech.UID].Cost;
            spriteBatch.DrawString(arial12Bold, cost.ToString(), cursor, Color.White);
            float progress = tech.Progress / ResourceManager.TechTree[tech.UID].Cost;
            pb.Draw(ScreenManager, progress);
            cursor.X = pb.rect.X + pb.rect.Width + 33;
            cursor.Y = pb.rect.Y - 2;
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