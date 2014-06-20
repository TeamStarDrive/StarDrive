using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PopupWindow : GameScreen, IDisposable
	{
		protected Rectangle r;

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

		public PopupWindow()
		{
		}

		public PopupWindow(MainMenuScreen s)
		{
		}

		public PopupWindow(Rectangle r)
		{
			base.IsPopup = true;
			if (GlobalStats.Config.Language != "English")
			{
				r.X = r.X - 20;
				r.Width = r.Width + 40;
			}
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.r = r;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
		}

		public void DrawBase(GameTime gameTime)
		{
			base.ScreenManager.SpriteBatch.Begin();
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_TL"], this.TL, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_TR"], this.TR, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_BL"], this.BL, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_BR"], this.BR, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_TL_stroke"], this.TLc, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_TR_stroke"], this.TRc, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_horiz_T"], this.TopHoriz, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_horiz_T_gradient"], this.TopSep, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_vert_L"], this.LeftVert, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_vert_R"], this.RightVert, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_BL_stroke"], this.BLc, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_corner_BR_stroke"], this.BRc, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_horiz_B"], this.BotHoriz, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_horiz_B_gradient"], this.BotSep, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_lower"], this.BottomFill, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_lower"], this.BottomBigFill, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_mid"], this.MidContainer, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_separator"], this.MidSepTop, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_separator"], this.MidSepBot, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_title"], this.TitleRect, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_title"], this.TitleLeft, Color.White);
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Popup/popup_filler_title"], this.TitleRight, Color.White);
			if (this.TitleText != null)
			{   //draw title text
				HelperFunctions.DrawDropShadowText(base.ScreenManager, this.TitleText, this.TitleTextPos, Fonts.Arial20Bold);
			}
			if (this.MiddleText != null)
			{   //draw description test
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12Bold, this.MiddleText, this.MiddleTextPos, Color.White);
			}
			this.close.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~PopupWindow() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			if (input.Escaped || input.RightMouseClick || this.close.HandleInput(input))
			{
				this.ExitScreen();
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			this.Setup();
		}

		public void Setup()
		{
			this.r = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.r.Width / 2, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.r.Height / 2, this.r.Width, this.r.Height);
			this.close = new CloseButton(new Rectangle(this.r.X + this.r.Width - 38, this.r.Y + 15, 20, 20));
			this.TL = new Rectangle(this.r.X, this.r.Y, 28, 30);
			this.TLc = this.TL;
			this.TLc.X = this.TLc.X - 2;
			this.TLc.Y = this.TLc.Y + 3;
			this.TLc.Width = 30;
			this.TLc.Height = 27;
			this.TR = new Rectangle(this.r.X + this.r.Width - 28, this.r.Y, 28, 30);
			this.TRc = this.TR;
			this.TRc.Y = this.TRc.Y + 3;
			this.TRc.Width = 28;
			this.TRc.Height = 27;
			int Distance = this.r.Width - 60 - 433;
			this.TopSep = new Rectangle(this.TL.X + this.TL.Width + Distance / 2, this.TL.Y + 3, 433, 4);
			this.TopHoriz = new Rectangle(this.TL.X + this.TL.Width - 2, this.TopSep.Y, this.r.Width - 54, 4);
			this.BL = new Rectangle(this.r.X, this.r.Y + this.r.Height - 30, 28, 30);
			this.BR = new Rectangle(this.r.X + this.r.Width - 28, this.r.Y + this.r.Height - 30, 28, 30);
			this.BotSep = new Rectangle(this.BL.X + this.BL.Width + Distance / 2, this.BL.Y + 18, 433, 12);
			this.BotHoriz = new Rectangle(this.BL.X + this.BL.Width - 2, this.BotSep.Y, this.r.Width - 54, 12);
			this.TitleRect = new Rectangle(this.r.X + 28, this.r.Y + 7, this.r.Width - 56, 46);
			this.TitleLeft = new Rectangle(this.TitleRect.X - 25, this.TitleRect.Y + 23, 25, this.TitleRect.Height - 23);
			this.TitleRight = new Rectangle(this.TitleRect.X + this.TitleRect.Width, this.TitleRect.Y + 23, 17, this.TitleRect.Height - 23);
			this.LeftVert = new Rectangle(this.TL.X + 1, this.TL.Y + this.TL.Height, 2, this.r.Height - 60);
			this.RightVert = new Rectangle(this.r.X + this.r.Width - 11, this.TL.Y + this.TL.Height, 11, this.r.Height - 60);
			this.BLc = new Rectangle(this.r.X - 2, this.r.Y + this.r.Height - 30, 28, 30);
			this.BRc = new Rectangle(this.BR.X, this.r.Y + this.r.Height - 30, 28, 30);
			this.BottomFill = new Rectangle(this.BL.X + this.BL.Width, this.BL.Y, this.r.Width - this.BL.Width - this.BR.Width, this.BL.Height - 12);
			this.MidContainer = new Rectangle(this.TitleLeft.X, this.TitleRect.Y + this.TitleRect.Height, this.TitleRect.Width + this.TitleLeft.Width + this.TitleRight.Width, 88);
			this.MidSepTop = new Rectangle(this.MidContainer.X, this.MidContainer.Y, this.MidContainer.Width, 2);
			this.MidSepBot = new Rectangle(this.MidContainer.X, this.MidContainer.Y + this.MidContainer.Height - 2, this.MidContainer.Width, 2);
			this.BottomBigFill = new Rectangle(this.MidContainer.X, this.MidContainer.Y + this.MidContainer.Height, this.MidContainer.Width, this.BottomFill.Y - (this.MidContainer.Y + this.MidContainer.Height));
			if (this.TitleText != null)
			{
                this.TitleTextPos = new Vector2((float)(this.TL.X + this.TL.Width), (float)(this.TitleRect.Y + this.TitleRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2));
				
					this.TitleTextPos.X = (float)((int)this.TitleTextPos.X);
					this.TitleTextPos.Y = (float)((int)this.TitleTextPos.Y);
				
			}
			if (this.MiddleText != null)
			{
				this.MiddleText = HelperFunctions.parseText(Fonts.Verdana12Bold, this.MiddleText, (float)(this.MidContainer.Width - 50));
                this.MiddleTextPos = new Vector2((float)(this.MidContainer.X + this.MidContainer.Width / 2) - Fonts.Verdana12Bold.MeasureString(this.MiddleText).X / 2f, (float)(this.MidContainer.Y + this.MidContainer.Height / 2) - Fonts.Verdana12Bold.MeasureString(this.MiddleText).Y / 2f);
				
					this.MiddleTextPos.X = (float)((int)this.MiddleTextPos.X);
					this.MiddleTextPos.Y = (float)((int)this.MiddleTextPos.Y);
				
			}
		}
	}
}