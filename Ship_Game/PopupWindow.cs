using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public class PopupWindow : GameScreen
    {
        private Rectangle TL;
        private Rectangle TR;
        private Rectangle BL;
        private Rectangle BR;
        private Rectangle TLc;
        private Rectangle TRc;
        private Rectangle BLc;
        private Rectangle BRc;
        private Rectangle TopHoriz;
        private Rectangle TopSep;
        private Rectangle BotHoriz;
        private Rectangle BotSep;
        private Rectangle LeftVert;
        private Rectangle RightVert;
        private Rectangle BottomFill;
        private Rectangle BottomBigFill;
        public Rectangle TitleRect;
        public Rectangle TitleLeft;
        public Rectangle TitleRight;
        public Rectangle EmpireFlagRect;
        public Rectangle MidContainer;
        protected Rectangle MidSepTop;
        protected Rectangle MidSepBot;
        public string TitleText;
        public string MiddleText;

        public UILabel TitleLabel;
        public UILabel MiddleLabel;
        public CloseButton Close;

        public Vector2 BodyTextStart;

        private static Rectangle CenterScreen(int width, int height)
        {
            return new Rectangle(GameBase.ScreenWidth  / 2 - width  / 2, 
                                 GameBase.ScreenHeight / 2 - height / 2, width, height);
        }

        protected PopupWindow(GameScreen parent, int width, int height)
            : base(parent, CenterScreen(width, height))
        {
            IsPopup = true;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Close.Visible = CanEscapeFromScreen;
            batch.Begin();

            // 4 corners
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_TL"), TL, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_TR"), TR, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_BL"), BL, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_BR"), BR, Color.White);

            batch.Draw(ResourceManager.Texture("Popup/popup_horiz_T"), TopHoriz, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_horiz_T_gradient"), TopSep, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_vert_L"), LeftVert, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_vert_R"), RightVert, Color.White);

            batch.Draw(ResourceManager.Texture("Popup/popup_horiz_B"), BotHoriz, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_horiz_B_gradient"), BotSep, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_filler_lower"), BottomFill, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_filler_lower"), BottomBigFill, Color.White);

            if (MidContainer.Height != 0)
                batch.Draw(ResourceManager.Texture("Popup/popup_filler_lower"), MidContainer, Color.White);
            if (MidSepTop.Height != 0)
                batch.Draw(ResourceManager.Texture("Popup/popup_separator"), MidSepTop, Color.White);
            if (MidSepBot.Height != 0)
                batch.Draw(ResourceManager.Texture("Popup/popup_separator"), MidSepBot, Color.White);

            batch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleRect, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleLeft, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleRight, Color.White);

            // stroke the corners
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_TL_stroke"), TLc, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_TR_stroke"), TRc, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_BL_stroke"), BLc, Color.White);
            batch.Draw(ResourceManager.Texture("Popup/popup_corner_BR_stroke"), BRc, Color.White);

            base.Draw(batch, elapsed);

            batch.End();
        }

        public override void LoadContent()
        {
            RemoveAll();

            Rect = CenterScreen(Rect.Width, Rect.Height);
            TL    = new Rectangle(Rect.X, Rect.Y, 28, 30);
            TLc        = TL;
            TLc.X      = TLc.X - 2;
            TLc.Y      = TLc.Y + 3;
            TLc.Width  = 30;
            TLc.Height = 27;
            TR = new Rectangle(Rect.Right - 28, Rect.Y, 28, 30);
            TRc        = TR;
            TRc.Y      = TRc.Y + 3;
            TRc.Width  = 28;
            TRc.Height = 27;
            int distance = Rect.Width - 60 - 433;
            TopSep     = new Rectangle(TL.Right + distance / 2, TL.Y + 3, 433, 4);
            TopHoriz   = new Rectangle(TL.Right - 2, TopSep.Y, Rect.Width - 54, 4);
            BL         = new Rectangle(Rect.X, Rect.Bottom - 30, 28, 30);
            BR         = new Rectangle(Rect.Right - 28, Rect.Bottom - 30, 28, 30);
            BotSep     = new Rectangle(BL.Right + distance / 2, BL.Y + 18, 433, 12);
            BotHoriz   = new Rectangle(BL.Right - 2, BotSep.Y, Rect.Width - 54, 12);
            TitleRect  = new Rectangle(Rect.X + 28, Rect.Y + 7, Rect.Width - 56, 46);
            TitleLeft  = new Rectangle(TitleRect.X - 25, TitleRect.Y + 23, 25, TitleRect.Height - 23);
            TitleRight = new Rectangle(TitleRect.Right, TitleRect.Y + 23, 17, TitleRect.Height - 23);
            LeftVert   = new Rectangle(TL.X + 1, TL.Bottom, 2, Rect.Height - 60);
            RightVert  = new Rectangle(Rect.Right - 11, TL.Bottom, 11, Rect.Height - 60);
            BLc        = new Rectangle(Rect.X - 2, Rect.Bottom - 30, 28, 30);
            BRc        = new Rectangle(BR.X, Rect.Bottom - 30, 28, 30);
            BottomFill = new Rectangle(BL.Right, BL.Y, Rect.Width - BL.Width - BR.Width, BL.Height - 12);

            EmpireFlagRect = new Rectangle(TitleRight.X-75, TitleRight.Y-22, 45, 45);

            Close = CloseButton(Rect.Right - 44, Rect.Y + 19);

            if (TitleText != null)
            {
                var pos = new Vector2(TitleRect.X, TitleRect.CenterY() - Fonts.Arial20Bold.LineSpacing / 2);
                TitleLabel = Label(pos.Rounded(), TitleText, Fonts.Arial20Bold);
            }

            if (MiddleText != null)
            {
                MidContainer = new Rectangle(TitleLeft.X, TitleRect.Bottom, TitleRect.Width + TitleLeft.Width + TitleRight.Width, 88);
                MiddleText = Fonts.Arial12Bold.ParseText(MiddleText, MidContainer.Width - 50);
                var textSize = Fonts.Arial12Bold.MeasureString(MiddleText);
                var pos = new Vector2(MidContainer.CenterX() - textSize.X / 2f, 
                                      MidContainer.CenterY() - textSize.Y / 2f);
                MiddleLabel = Label(pos.Rounded(), MiddleText, Fonts.Arial12Bold);
            }
            else
            {
                MidContainer = new Rectangle(TitleLeft.X, TitleRect.Bottom, TitleRect.Width + TitleLeft.Width + TitleRight.Width, 0);
            }

            MidSepTop = new Rectangle(MidContainer.X, MidContainer.Y, MidContainer.Width, 2);
            MidSepBot = new Rectangle(MidContainer.X, MidContainer.Bottom - 2, MidContainer.Width, 2);
            BottomBigFill = new Rectangle(MidContainer.X, MidContainer.Bottom, MidContainer.Width, BottomFill.Y - MidContainer.Bottom);

            BodyTextStart = new Vector2(BottomBigFill.Left +12, BottomBigFill.Top + 12);

            base.LoadContent();
        }
    }
}