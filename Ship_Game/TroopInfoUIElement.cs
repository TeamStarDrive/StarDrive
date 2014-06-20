using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class TroopInfoUIElement : UIElement
	{
		private Rectangle SliderRect;

		private Rectangle clickRect;

		private UniverseScreen screen;

		private Rectangle LeftRect;

		private Rectangle RightRect;

		private Rectangle flagRect;

		private Rectangle DefenseRect;

		private Rectangle SoftAttackRect;

		private Rectangle HardAttackRect;

		private Rectangle ItemDisplayRect;

		private DanButton LaunchTroop;

		private Selector sel;

		private ScrollList DescriptionSL;

		public PlanetGridSquare pgs;

		private List<TroopInfoUIElement.TippedItem> ToolTipItems = new List<TroopInfoUIElement.TippedItem>();

		new private Color tColor = new Color(255, 239, 208);

		private string fmt = "0.#";

		//private Rectangle Mark;

		public TroopInfoUIElement(Rectangle r, Ship_Game.ScreenManager sm, UniverseScreen screen)
		{
			this.screen = screen;
			this.ScreenManager = sm;
			this.ElementRect = r;
			this.sel = new Selector(this.ScreenManager, r, Color.Black);
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.SliderRect = new Rectangle(r.X + r.Width - 100, r.Y + r.Height - 40, 500, 40);
			this.clickRect = new Rectangle(this.ElementRect.X + this.ElementRect.Width - 16, this.ElementRect.Y + this.ElementRect.Height / 2 - 11, 11, 22);
			this.LeftRect = new Rectangle(r.X, r.Y + 44, 200, r.Height - 44);
			this.RightRect = new Rectangle(r.X + 200, r.Y + 44, 200, r.Height - 44);
			this.flagRect = new Rectangle(r.X + r.Width - 31, r.Y + 22 - 13, 26, 26);
			this.DefenseRect = new Rectangle(this.LeftRect.X + 12, this.LeftRect.Y + 18, 22, 22);
			this.SoftAttackRect = new Rectangle(this.LeftRect.X + 12, this.DefenseRect.Y + 22 + 5, 16, 16);
			this.HardAttackRect = new Rectangle(this.LeftRect.X + 12, this.SoftAttackRect.Y + 16 + 5, 16, 16);
			this.DefenseRect.X = this.DefenseRect.X - 3;
			this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85, this.LeftRect.Y + 5, 128, 128);
			Rectangle DesRect = new Rectangle(this.HardAttackRect.X, this.HardAttackRect.Y - 10, this.LeftRect.Width + 8, 95);
			Submenu sub = new Submenu(this.ScreenManager, DesRect);
			this.DescriptionSL = new ScrollList(sub, Fonts.Arial12.LineSpacing + 1);
			TroopInfoUIElement.TippedItem def = new TroopInfoUIElement.TippedItem()
			{
				r = this.DefenseRect,
				TIP_ID = 33
			};
			this.ToolTipItems.Add(def);
			def = new TroopInfoUIElement.TippedItem()
			{
				r = this.SoftAttackRect,
				TIP_ID = 34
			};
			this.ToolTipItems.Add(def);
			def = new TroopInfoUIElement.TippedItem()
			{
				r = this.HardAttackRect,
				TIP_ID = 35
			};
			this.ToolTipItems.Add(def);
		}

		public override void Draw(GameTime gameTime)
		{
			string str;
			string str1;
			if (this.pgs == null)
			{
				return;
			}
			if (this.pgs.TroopsHere.Count == 0 && this.pgs.building == null)
			{
				return;
			}
			MathHelper.SmoothStep(0f, 1f, base.TransitionPosition);
			Primitives2D.FillRectangle(this.ScreenManager.SpriteBatch, this.sel.Menu, Color.Black);
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			Header slant = new Header(new Rectangle(this.sel.Menu.X, this.sel.Menu.Y, this.sel.Menu.Width, 41), (this.pgs.TroopsHere.Count > 0 ? this.pgs.TroopsHere[0].Name : Localizer.Token(this.pgs.building.NameTranslationIndex)));
			Body body = new Body(new Rectangle(slant.leftRect.X, this.sel.Menu.Y + 44, this.sel.Menu.Width, this.sel.Menu.Height - 44));
			slant.Draw(this.ScreenManager);
			body.Draw(this.ScreenManager);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_shield"], this.DefenseRect, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/Ground_Attack"], this.SoftAttackRect, Color.White);
			this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/attack_hard"], this.HardAttackRect, Color.White);
			bool Troop = false;
			if (this.pgs.TroopsHere.Count > 0)
			{
				Troop = true;
			}
			Vector2 defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.DefenseRect.Y + 11 - Fonts.Arial12Bold.LineSpacing / 2));
			this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (Troop ? this.pgs.TroopsHere[0].Strength.ToString(this.fmt) : this.pgs.building.Strength.ToString()), defPos, Color.White);
			defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.SoftAttackRect.Y + 8 - Fonts.Arial12Bold.LineSpacing / 2));
			SpriteBatch spriteBatch = this.ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			if (Troop)
			{
				int softAttack = this.pgs.TroopsHere[0].GetSoftAttack();
				str = softAttack.ToString(this.fmt);
			}
			else
			{
				str = this.pgs.building.SoftAttack.ToString();
			}
			spriteBatch.DrawString(arial12Bold, str, defPos, Color.White);
			defPos = new Vector2((float)(this.DefenseRect.X + this.DefenseRect.Width + 2), (float)(this.HardAttackRect.Y + 8 - Fonts.Arial12Bold.LineSpacing / 2));
			SpriteBatch spriteBatch1 = this.ScreenManager.SpriteBatch;
			SpriteFont spriteFont = Fonts.Arial12Bold;
			if (Troop)
			{
				int hardAttack = this.pgs.TroopsHere[0].GetHardAttack();
				str1 = hardAttack.ToString(this.fmt);
			}
			else
			{
				str1 = this.pgs.building.HardAttack.ToString();
			}
			spriteBatch1.DrawString(spriteFont, str1, defPos, Color.White);
			if (!Troop)
			{
				this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85 + 16, this.LeftRect.Y + 5 + 16, 64, 64);
				this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", this.pgs.building.Icon, "_64x64")], this.ItemDisplayRect, Color.White);
			}
			else
			{
				this.ItemDisplayRect = new Rectangle(this.LeftRect.X + 85 + 16, this.LeftRect.Y + 5 + 16, 64, 64);
				this.pgs.TroopsHere[0].Draw(this.ScreenManager.SpriteBatch, this.ItemDisplayRect);
				if (this.pgs.TroopsHere[0].GetOwner() != EmpireManager.GetEmpireByName(this.screen.PlayerLoyalty))
				{
					this.LaunchTroop = null;
				}
				else
				{
					this.LaunchTroop = new DanButton(new Vector2((float)(slant.leftRect.X + 5), (float)(this.ElementRect.Y + this.ElementRect.Height + 15)), string.Concat(Localizer.Token(1435), (this.pgs.TroopsHere[0].AvailableMoveActions >= 1 ? "" : string.Concat(" (", this.pgs.TroopsHere[0].MoveTimer.ToString("0"), ")"))));
					this.LaunchTroop.DrawBlue(this.ScreenManager);
				}
				if (this.pgs.TroopsHere[0].Level > 0)
				{
					for (int i = 0; i < this.pgs.TroopsHere[0].Level; i++)
					{
						Rectangle star = new Rectangle(this.LeftRect.X + this.LeftRect.Width - 20 - 12 * i, this.LeftRect.Y + 12, 12, 11);
						if (HelperFunctions.CheckIntersection(star, MousePos))
						{
							ToolTip.CreateTooltip(127, this.ScreenManager);
						}
						this.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_star"], star, Color.White);
					}
				}
			}
			Vector2 drawCurs = new Vector2((float)this.DefenseRect.X, (float)this.HardAttackRect.Y);
			for (int i = this.DescriptionSL.indexAtTop; i < this.DescriptionSL.Entries.Count && i < this.DescriptionSL.indexAtTop + this.DescriptionSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.DescriptionSL.Entries[i];
				drawCurs.Y = (float)e.clickRect.Y;
				string t1 = e.item as string;
				this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, t1, drawCurs, Color.White);
			}
			this.DescriptionSL.Draw(this.ScreenManager.SpriteBatch);
		}

		public override bool HandleInput(InputState input)
		{
			this.DescriptionSL.HandleInput(input);
			foreach (TroopInfoUIElement.TippedItem ti in this.ToolTipItems)
			{
				if (!HelperFunctions.CheckIntersection(ti.r, input.CursorPosition))
				{
					continue;
				}
				ToolTip.CreateTooltip(ti.TIP_ID, this.ScreenManager);
			}
			if (this.LaunchTroop != null && HelperFunctions.CheckIntersection(this.LaunchTroop.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(67, this.ScreenManager);
				if (this.LaunchTroop.HandleInput(input))
				{
					if ((this.screen.workersPanel as CombatScreen).ActiveTroop.TroopsHere[0].AvailableMoveActions < 1)
					{
						AudioManager.PlayCue("UI_Misc20");
						return true;
					}
					AudioManager.PlayCue("sd_troop_takeoff");
					if (this.pgs.TroopsHere.Count > 0)
					{
						this.pgs.TroopsHere[0].Launch();
					}
					(this.screen.workersPanel as CombatScreen).ActiveTroop = null;
				}
			}
			return false;
		}

		public void SetPGS(PlanetGridSquare pgs)
		{
			this.pgs = pgs;
			if (this.pgs == null)
			{
				return;
			}
			if (pgs.TroopsHere.Count != 0)
			{
				this.DescriptionSL.Entries.Clear();
				this.DescriptionSL.indexAtTop = 0;
				HelperFunctions.parseTextToSL(pgs.TroopsHere[0].Description, (float)(this.LeftRect.Width - 15), Fonts.Arial12, ref this.DescriptionSL);
				return;
			}
			if (pgs.building != null)
			{
				this.DescriptionSL.Entries.Clear();
				this.DescriptionSL.indexAtTop = 0;
				HelperFunctions.parseTextToSL(Localizer.Token(pgs.building.DescriptionIndex), (float)(this.LeftRect.Width - 15), Fonts.Arial12, ref this.DescriptionSL);
			}
		}

		private struct TippedItem
		{
			public Rectangle r;

			public int TIP_ID;
		}
	}
}