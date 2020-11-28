using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class Menu2 : UIElementV2
    {
        public Rectangle Menu;

        private Rectangle corner_TL;
        private Rectangle corner_TR;
        private Rectangle corner_BL;
        private Rectangle corner_BR;
        private Rectangle vertLeft;
        private Rectangle vertRight;
        private Rectangle horizBot;
        private Array<Rectangle> RepeatTops = new Array<Rectangle>();
        private Rectangle topExtender;

        public bool Hollow;
        public Color Background;
        public Color Border = Color.TransparentBlack; // mostly for debugging

        public Menu2(in Rectangle theMenu) : this(theMenu, new Color(0, 0, 0, 240))
        {
        }
        public Menu2(int x, int y, int width, int height) : this(new Rectangle(x, y, width, height))
        {
        }
        public Menu2(in Rectangle theMenu, Color color) : base(theMenu)
        {
            Menu = theMenu;
            Background = color; // transparent black

            corner_TL = new Rectangle(theMenu.X, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TL").Width, ResourceManager.Texture("NewUI/menu_2_corner_TL").Height);
            corner_TR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, theMenu.Y, ResourceManager.Texture("NewUI/menu_2_corner_TR").Width, ResourceManager.Texture("NewUI/menu_2_corner_TR").Height);
            corner_BL = new Rectangle(theMenu.X, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BL").Height, ResourceManager.Texture("NewUI/menu_2_corner_BL").Width, ResourceManager.Texture("NewUI/menu_2_corner_BL").Height);
            corner_BR = new Rectangle(theMenu.X + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_corner_BR").Height, ResourceManager.Texture("NewUI/menu_2_corner_BR").Width, ResourceManager.Texture("NewUI/menu_2_corner_BR").Height);
            int topDistance = theMenu.Width - corner_TL.Width - corner_TR.Width;
            int numberRepeats = topDistance / ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width;
            int remainder = numberRepeats * ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width - topDistance;
            var extendTopLeft = new Rectangle(corner_TL.X + corner_TL.Width, corner_TL.Y + 3, remainder / 2, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
            topExtender = new Rectangle(theMenu.X + 8, corner_TL.Y + 3, theMenu.Width - 16, ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender").Height);
            for (int i = 0; i < numberRepeats + 1; i++)
            {
                Rectangle repeat = new Rectangle(corner_TL.X + corner_TL.Width + remainder + ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width * i, extendTopLeft.Y, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Width, ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat").Height);
                RepeatTops.Add(repeat);
            }
            horizBot = new Rectangle(corner_BL.X + corner_BL.Width, theMenu.Y + theMenu.Height - ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height, theMenu.Width - corner_BL.Width - corner_BR.Width, ResourceManager.Texture("NewUI/menu_2_horiz_lower").Height);
            vertLeft = new Rectangle(corner_TL.X + 1, corner_TL.Y + corner_TL.Height, ResourceManager.Texture("NewUI/menu_2_vert_left").Width, theMenu.Height - corner_TL.Height - corner_BL.Height);
            vertRight = new Rectangle(theMenu.X - 1 + theMenu.Width - ResourceManager.Texture("NewUI/menu_2_vert_right").Width, corner_TR.Y + corner_TR.Height, ResourceManager.Texture("NewUI/menu_2_vert_right").Width, theMenu.Height - corner_TR.Height - corner_BR.Height);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Hollow)
            {
                batch.FillRectangle(new Rectangle(Menu.X + 8, Menu.Y + 8, Menu.Width - 8, Menu.Height - 8), Background);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), horizBot, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), topExtender, Color.White);
                foreach (Rectangle r in RepeatTops)
                {
                    batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
                }
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), vertLeft, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), vertRight, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), corner_TL, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), corner_TR, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), corner_BL, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), corner_BR, Color.White);
            }
            else
            {
                batch.FillRectangle(new Rectangle(0, 0, Menu.Width, 10), Color.Black);
                batch.FillRectangle(new Rectangle(0, 0, 10, Menu.Height), Color.Black);
                batch.FillRectangle(new Rectangle(0, Menu.Height - 10, Menu.Width, 10), Color.Black);
                batch.FillRectangle(new Rectangle(Menu.Width - 10, 0, 10, Menu.Height), Color.Black);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_lower"), horizBot, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_extender"), topExtender, Color.White);
                foreach (Rectangle r in RepeatTops)
                {
                    batch.Draw(ResourceManager.Texture("NewUI/menu_2_horiz_upper_repeat"), r, Color.White);
                }
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_left"), vertLeft, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_vert_right"), vertRight, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TL"), corner_TL, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_TR"), corner_TR, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BL"), corner_BL, Color.White);
                batch.Draw(ResourceManager.Texture("NewUI/menu_2_corner_BR"), corner_BR, Color.White);
            }

            if (Border.A > 0)
            {
                batch.DrawRectangle(Rect, Border);
            }
        }

        public override bool HandleInput(InputState input)
        {
            return false; // nothing to handle here
        }
    }
}