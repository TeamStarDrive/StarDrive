using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace Ship_Game
{
	public sealed class TutorialScreen : GameScreen
	{
		private Map<string, Texture2D> TexDict = new Map<string, Texture2D>();

		private Rectangle BridgeRect;

		private int Index;

		private CloseButton close;

		public TutorialScreen(GameScreen parent) : base(parent)
		{
			IsPopup = true;
			TransitionOnTime = TimeSpan.FromSeconds(0.25);
			TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

	

		public override void Draw(SpriteBatch batch)
		{
			ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
		    batch.Begin();
		    batch.Draw(TexDict[string.Concat("Slide_", Index.ToString("00"))], BridgeRect, Color.White);
			close.Draw(batch);
		    batch.End();
		}

		public override bool HandleInput(InputState input)
		{
			if (close.HandleInput(input))
			{
				ExitScreen();
				return true;
			}
			if (input.Escaped)
			{
				ExitScreen();
                return true;
			}
			if (input.Right || input.InGameSelect)
			{
				TutorialScreen index = this;
				index.Index = index.Index + 1;
				if (Index > TexDict.Count - 1)
				{
					Index = 0;
				}
			}
			if (input.Left || input.RightMouseClick)
			{
				TutorialScreen tutorialScreen = this;
				tutorialScreen.Index = tutorialScreen.Index - 1;
				if (Index < 0)
				{
					Index = TexDict.Count - 1;
				}
			}
            return base.HandleInput(input);
		}

		public override void LoadContent()
		{
            FileInfo[] textList;
            try
            {
                 textList = Dir.GetFiles("Content/Tutorials/" + GlobalStats.Language + "/", "xnb");
            }
            catch
            {
                 textList = Dir.GetFiles("Content/Tutorials/English/", "xnb");
            }
			foreach (FileInfo info in textList)
			{
			    string name = Path.GetFileNameWithoutExtension(info.Name);
			    Texture2D tex = Game1.Instance.Content.Load<Texture2D>("Tutorials/"+ GlobalStats.Language+"/"+name);
			    TexDict[name] = tex;
			}
            var center = ScreenManager.Center();
			BridgeRect = new Rectangle((int)center.X - 640, (int)center.Y - 360, 1280, 720);
			close = new CloseButton(this, new Rectangle(BridgeRect.X + BridgeRect.Width - 38, BridgeRect.Y + 15, 20, 20));
			base.LoadContent();
		}
	}
}