using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public abstract class MenuScreen : GameScreen
	{
		private Array<string> menuEntries = new Array<string>();

		private int selectedEntry;

		protected IList<string> MenuEntries => menuEntries;

	    protected MenuScreen(GameScreen parent) : base(parent, new Rectangle(0,0, 400, 400), false)
		{
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			base.TransitionOffTime = TimeSpan.FromSeconds(1);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			Viewport viewport = base.Viewport;
			Vector2 viewportSize = new Vector2((float)viewport.Width, (float)viewport.Height);
			Vector2 position = new Vector2(0f, viewportSize.Y * 0.65f);
			float transitionOffset = (float)Math.Pow((double)base.TransitionPosition, 2);
			if (base.ScreenState != Ship_Game.ScreenState.TransitionOn)
			{
				position.Y = position.Y + transitionOffset * 512f;
			}
			else
			{
				position.Y = position.Y + transitionOffset * 256f;
			}
			base.ScreenManager.SpriteBatch.Begin();
			for (int i = 0; i < this.menuEntries.Count; i++)
			{
				Color color = Color.White;
				float scale = 1f;
				if (base.IsActive && i == this.selectedEntry)
				{
					double time = Game1.Instance.GameTime.TotalGameTime.TotalSeconds;
					float pulsate = (float)Math.Sin(time * 6) + 1f;
					color = Color.Orange;
					scale = scale + pulsate * 0.05f;
				}
				color = new Color(color.R, color.G, color.B, base.TransitionAlpha);
				Vector2 origin = new Vector2(0f, (float)(Fonts.Arial12Bold.LineSpacing / 2));
				Vector2 size = Fonts.Arial12Bold.MeasureString(this.menuEntries[i]);
				position.X = viewportSize.X / 2f - size.X / 2f * scale;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.menuEntries[i], position, color, 0f, origin, scale, SpriteEffects.None, 0f);
				position.Y = position.Y + (float)Fonts.Arial12Bold.LineSpacing;
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override bool HandleInput(InputState input)
		{
			if (input.MenuUp)
			{
                GameAudio.PlaySfxAsync("blip_click");
				if (--selectedEntry < 0)
					selectedEntry = menuEntries.Count - 1;
			}
			if (input.MenuDown)
			{
			    GameAudio.PlaySfxAsync("blip_click");
				if (++selectedEntry >= menuEntries.Count)
					selectedEntry = 0;
			}
			if (input.MenuSelect)
			{
				this.OnSelectEntry(this.selectedEntry);
				return true;
			}
			if (input.MenuCancel)
			{
				this.OnCancel();
			}
            return base.HandleInput(input);
		}

		protected abstract void OnCancel();

		protected abstract void OnSelectEntry(int entryIndex);
	}
}