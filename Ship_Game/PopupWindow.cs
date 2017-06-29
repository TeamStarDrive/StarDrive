using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PopupWindow : GameScreen
	{
		protected Rectangle R;
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
		public Rectangle MidContainer;
		private Rectangle MidSepTop;
		private Rectangle MidSepBot;
		public string TitleText;
		public string MiddleText;
		private Vector2 TitleTextPos;
		private Vector2 MiddleTextPos;
		private CloseButton close;

        protected PopupWindow(GameScreen parent) : base(parent)
        {
        }
		public PopupWindow(GameScreen parent, Rectangle r) : base(parent)
		{
			IsPopup = true;
			if (!GlobalStats.IsEnglish)
			{
				r.X = r.X - 20;
				r.Width = r.Width + 40;
			}
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
			R = r;
		}

		public override void Draw(GameTime gameTime)
		{
		}

		public void DrawBase(GameTime gameTime)
		{
			ScreenManager.SpriteBatch.Begin();
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_TL"), TL, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_TR"), TR, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_BL"), BL, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_BR"), BR, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_TL_stroke"), TLc, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_TR_stroke"), TRc, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_horiz_T"), TopHoriz, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_horiz_T_gradient"), TopSep, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_vert_L"), LeftVert, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_vert_R"), RightVert, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_BL_stroke"), BLc, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_corner_BR_stroke"), BRc, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_horiz_B"), BotHoriz, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_horiz_B_gradient"), BotSep, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_lower"), BottomFill, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_lower"), BottomBigFill, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_mid"), MidContainer, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_separator"), MidSepTop, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_separator"), MidSepBot, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleRect, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleLeft, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Popup/popup_filler_title"), TitleRight, Color.White);
			if (TitleText != null)
			{   //draw title text
				HelperFunctions.DrawDropShadowText(ScreenManager, TitleText, TitleTextPos, Fonts.Arial20Bold);
			}
			if (MiddleText != null)
			{   //draw description test
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, MiddleText, MiddleTextPos, Color.White);
			}
			close.Draw(ScreenManager);
			ScreenManager.SpriteBatch.End();
		}

        public Vector2 DrawString(SpriteFont font, string theirText, Vector2 theirTextPos, Color color)
        {
            theirTextPos.Y = theirTextPos.Y + font.LineSpacing;
            ScreenManager.SpriteBatch.DrawString(font, theirText, theirTextPos, color);
            theirTextPos.Y = theirTextPos.Y + font.LineSpacing + 2;
            return theirTextPos;
        }

        public override bool HandleInput(InputState input)
		{
			if (input.Escaped || input.RightMouseClick || close.HandleInput(input))
			{
				ExitScreen();
                return true;
			}
			return base.HandleInput(input);
		}

		public override void LoadContent()
		{
			Setup();
		}

		public void Setup()
		{
			R = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth  / 2 - R.Width  / 2, 
                              ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - R.Height / 2, R.Width, R.Height);
			close = new CloseButton(new Rectangle(R.X + R.Width - 38, R.Y + 15, 20, 20));
			TL    = new Rectangle(R.X, R.Y, 28, 30);
			TLc        = TL;
			TLc.X      = TLc.X - 2;
			TLc.Y      = TLc.Y + 3;
			TLc.Width  = 30;
			TLc.Height = 27;
			TR = new Rectangle(R.X + R.Width - 28, R.Y, 28, 30);
			TRc        = TR;
			TRc.Y      = TRc.Y + 3;
			TRc.Width  = 28;
			TRc.Height = 27;
			int distance = R.Width - 60 - 433;
			TopSep     = new Rectangle(TL.X + TL.Width + distance / 2, TL.Y + 3, 433, 4);
			TopHoriz   = new Rectangle(TL.X + TL.Width - 2, TopSep.Y, R.Width - 54, 4);
			BL         = new Rectangle(R.X, R.Y + R.Height - 30, 28, 30);
			BR         = new Rectangle(R.X + R.Width - 28, R.Y + R.Height - 30, 28, 30);
			BotSep     = new Rectangle(BL.X + BL.Width + distance / 2, BL.Y + 18, 433, 12);
			BotHoriz   = new Rectangle(BL.X + BL.Width - 2, BotSep.Y, R.Width - 54, 12);
			TitleRect  = new Rectangle(R.X + 28, R.Y + 7, R.Width - 56, 46);
			TitleLeft  = new Rectangle(TitleRect.X - 25, TitleRect.Y + 23, 25, TitleRect.Height - 23);
			TitleRight = new Rectangle(TitleRect.X + TitleRect.Width, TitleRect.Y + 23, 17, TitleRect.Height - 23);
			LeftVert   = new Rectangle(TL.X + 1, TL.Y + TL.Height, 2, R.Height - 60);
			RightVert  = new Rectangle(R.X + R.Width - 11, TL.Y + TL.Height, 11, R.Height - 60);
			BLc        = new Rectangle(R.X - 2, R.Y + R.Height - 30, 28, 30);
			BRc        = new Rectangle(BR.X, R.Y + R.Height - 30, 28, 30);
			BottomFill = new Rectangle(BL.X + BL.Width, BL.Y, R.Width - BL.Width - BR.Width, BL.Height - 12);
			MidContainer = new Rectangle(TitleLeft.X, TitleRect.Y + TitleRect.Height, TitleRect.Width + TitleLeft.Width + TitleRight.Width, 88);
			MidSepTop    = new Rectangle(MidContainer.X, MidContainer.Y, MidContainer.Width, 2);
			MidSepBot    = new Rectangle(MidContainer.X, MidContainer.Y + MidContainer.Height - 2, MidContainer.Width, 2);
			BottomBigFill = new Rectangle(MidContainer.X, MidContainer.Y + MidContainer.Height, 
                                          MidContainer.Width, BottomFill.Y - (MidContainer.Y + MidContainer.Height));
			if (TitleText != null)
			{
                TitleTextPos = new Vector2(TL.X + TL.Width, TitleRect.Y + TitleRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2);
				
					TitleTextPos.X = (int)TitleTextPos.X;
					TitleTextPos.Y = (int)TitleTextPos.Y;
				
			}
			if (MiddleText != null)
			{
				MiddleText = HelperFunctions.ParseText(Fonts.Arial12Bold, MiddleText, MidContainer.Width - 50);
                var textSize = Fonts.Arial12Bold.MeasureString(MiddleText);
                MiddleTextPos = new Vector2(MidContainer.X + MidContainer.Width / 2  - textSize.X / 2f, 
                                            MidContainer.Y + MidContainer.Height / 2 - textSize.Y / 2f);
				MiddleTextPos.X = (int)MiddleTextPos.X;
				MiddleTextPos.Y = (int)MiddleTextPos.Y;
			}
		}
	}
}