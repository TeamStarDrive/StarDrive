using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class ProgressBar
    {
        public Rectangle pBar;
        public float Progress;
        public float Max;
        public string color = "brown";
        public bool DrawPercentage   = false;
        public bool Faction10Values  = false;
        private Rectangle Left;
        private Rectangle Right;
        private Rectangle Middle;
        private Rectangle gLeft;
        private Rectangle gRight;
        private Rectangle gMiddle;
        private bool Vertical;
        private Rectangle Top;
        private Rectangle Bot;
        public ProgressBar(float x, float y, float w, float h) : this(new Rectangle((int)x, (int)y, (int)w, (int)h))
        {
        }
        
        public ProgressBar(Rectangle r)
        {
            pBar = r;
            Left = new Rectangle(r.X, r.Y, 7, 18);
            gLeft = new Rectangle(Left.X + 3, Left.Y + 3, 4, 12);
            Right = new Rectangle(r.X + r.Width - 7, r.Y, 7, 18);
            gRight = new Rectangle(Right.X - 3, Right.Y + 3, 4, 12);
            Middle = new Rectangle(r.X + 7, r.Y, r.Width - 14, 18);
            gMiddle = new Rectangle(Middle.X, Middle.Y + 3, Middle.Width, 12);
        }

        public ProgressBar(Rectangle r, float max, float progress) : this(r)
        {
            Max = max;
            Progress = progress;
        }

        private float Percent => Progress / Max;

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Vertical)
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_top"), Top, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid_vert"), Middle, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_bot"), Bot, Color.White);
                return;
            }
            if (Max > 0f)
            {
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_left"), gLeft, Color.White);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_mid"), gMiddle, Color.White);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_right"), gRight, Color.White);
                int maskX = (int)(Percent * pBar.Width + pBar.X);
                int maskW = pBar.Width - (int)(Percent * pBar.Width);
                var mask = new Rectangle(maskX, pBar.Y, maskW, 18);
                spriteBatch.FillRectangle(mask, Color.Black);
            }
            if (color != "brown")
            {
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_left_{color}"), Left, Color.White);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_mid_{color}"), Middle, Color.White);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_right_{color}"), Right, Color.White);
            }
            else
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_left"), Left, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid"), Middle, Color.White);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_right"), Right, Color.White);
            }
            var textPos = new Vector2(Left.X + 7, Left.Y + Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2);
            spriteBatch.DrawString(Fonts.TahomaBold9, Faction10Values ? Values10 : Values, textPos, Colors.Cream);
        }

        public void DrawGrayed(SpriteBatch spriteBatch)
        {
            if (Vertical)
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_top"), Top, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid_vert"), Middle, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_bot"), Bot, Color.DarkGray);
                return;
            }
            if (Max > 0f)
            {
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_left"), gLeft, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_mid"), gMiddle, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_grd_{color}_right"), gRight, Color.DarkGray);
                int maskX = (int)(Progress / Max * pBar.Width + pBar.X);
                int maskW = pBar.Width - (int)(Progress / Max * pBar.Width);
                var mask = new Rectangle(maskX, pBar.Y, maskW, 18);
                spriteBatch.FillRectangle(mask, Color.Black);
            }
            if (color != "brown")
            {
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_left_{color}"), Left, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_mid_{color}"), Middle, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture($"NewUI/progressbar_container_right_{color}"), Right, Color.DarkGray);
            }
            else
            {
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_left"), Left, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_mid"), Middle, Color.DarkGray);
                spriteBatch.Draw(ResourceManager.Texture("NewUI/progressbar_container_right"), Right, Color.DarkGray);
            }
            var textPos = new Vector2(Left.X + 7, Left.Y + Left.Height / 2 - Fonts.TahomaBold9.LineSpacing / 2);
            spriteBatch.DrawString(Fonts.TahomaBold9, Faction10Values ? Values10 : Values, textPos, Color.DarkGray);
        }

        string Values10 => DrawPercentage ? $"{Progress.String(1)}%" : $"{Progress.String(1)}/{Max.String(1)}";
        string Values    => DrawPercentage ? $"{(int)Progress}%" : $"{(int)Progress}/{(int)Max}";
    }
}