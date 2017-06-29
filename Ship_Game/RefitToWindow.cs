using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class RefitToWindow : GameScreen
	{
		private Vector2 Cursor = Vector2.Zero;

		private ShipListScreen screen;

		private Ship shiptorefit;

		//private UIButton Exit;

		private Submenu sub_ships;

		private ScrollList ShipSL;

		private UIButton RefitOne;

		private UIButton RefitAll;

		private string RefitTo;

		private DanButton ConfirmRefit;

		private Selector selector;

		public RefitToWindow(ShipListScreen screen, ShipListScreenEntry entry) : base(screen)
		{
			this.screen = screen;
			this.shiptorefit = entry.ship;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public RefitToWindow(GameScreen parent, Ship ship) : base(parent)
		{
			this.shiptorefit = ship;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

        protected override void Dispose(bool disposing)
        {
            ShipSL?.Dispose(ref ShipSL);
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.sub_ships.Draw();
			Rectangle r = this.sub_ships.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(r, new Color(0, 0, 0, 210));
			sel.Draw(ScreenManager.SpriteBatch);
			if (this.selector != null)
			{
				this.selector.Draw(ScreenManager.SpriteBatch);
			}
			this.ShipSL.Draw(base.ScreenManager.SpriteBatch);
			Vector2 bCursor = new Vector2((float)(this.sub_ships.Menu.X + 5), (float)(this.sub_ships.Menu.Y + 25));
			for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Copied.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ShipSL.Copied[i];
				Ship ship = ResourceManager.ShipsDict[e.item as string];
				bCursor.Y = (float)e.clickRect.Y;
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[ship.GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
				Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Name, tCursor, Color.White);
				tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
				if (this.sub_ships.Tabs[0].Selected)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.shipData.GetRole(), tCursor, Color.Orange);
				}
				Rectangle MoneyRect = new Rectangle(e.clickRect.X + 165, e.clickRect.Y, 21, 20);
				Vector2 moneyText = new Vector2((float)(MoneyRect.X + 25), (float)(MoneyRect.Y - 2));
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["NewUI/icon_production"], MoneyRect, Color.White);
				int refitCost = (int)(ship.GetCost(ship.loyalty) - this.shiptorefit.GetCost(ship.loyalty));
				if (refitCost < 0)
				{
					refitCost = 0;
				}
				refitCost = refitCost + 10;
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, refitCost.ToString(), moneyText, Color.White);
			}
			if (this.RefitTo != null)
			{
				this.RefitOne.Draw(base.ScreenManager.SpriteBatch);
				this.RefitAll.Draw(base.ScreenManager.SpriteBatch);
				Vector2 Cursor = new Vector2((float)this.ConfirmRefit.r.X, (float)(this.ConfirmRefit.r.Y + 30));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.ParseText(Fonts.Arial12Bold, string.Concat("Refit ", this.shiptorefit.Name, " to ", this.RefitTo), 270f), Cursor, Color.White);
			}
			if (base.IsActive)
			{
				ToolTip.Draw(ScreenManager.SpriteBatch);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			if (this.screen != null)
			{
				this.screen.ResetStatus();
			}
			base.ExitScreen();
		}

	

		public override bool HandleInput(InputState input)
		{
			this.ShipSL.HandleInput(input);
			if (input.Escaped || input.MouseCurr.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
                return true;
			}
			this.selector = null;
			for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Copied.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ShipSL.Copied[i];
				if (e.clickRect.HitTest(input.CursorPosition))
				{
					this.selector = new Selector(e.clickRect);
					if (input.InGameSelect)
					{
						GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
						this.RefitTo = e.item as string;
					}
				}
			}
			if (this.RefitTo != null)
			{
				if (this.RefitOne.Rect.HitTest(input.CursorPosition))
				{
					ToolTip.CreateTooltip(Localizer.Token(2267));
					if (input.InGameSelect)
					{
						this.shiptorefit.AI.OrderRefitTo(this.RefitTo);
						GameAudio.PlaySfxAsync("echo_affirm");
						this.ExitScreen();
                        return true;
					}
				}
				if (this.RefitAll.Rect.HitTest(input.CursorPosition))
				{
					ToolTip.CreateTooltip(Localizer.Token(2268));
					if (input.InGameSelect)
					{
						foreach (Ship ship in EmpireManager.Player.GetShips())
						{
							if (ship.Name != this.shiptorefit.Name)
							{
								continue;
							}
							ship.AI.OrderRefitTo(this.RefitTo);
						}
						GameAudio.PlaySfxAsync("echo_affirm");
						this.ExitScreen();
                        return true;
					}
				}
			}
            return base.HandleInput(input);
		}

		public override void LoadContent()
		{
			Rectangle shipDesignsRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 140, 100, 280, 500);
			this.sub_ships = new Submenu(base.ScreenManager, shipDesignsRect);
			this.ShipSL = new ScrollList(this.sub_ships, 40);
			this.sub_ships.AddTab("Refit to...");
			foreach (string shipname in this.shiptorefit.loyalty.ShipsWeCanBuild)
			{
                if (!(ResourceManager.ShipsDict[shipname].GetShipData().Hull == this.shiptorefit.GetShipData().Hull) || !(shipname != this.shiptorefit.Name) || ResourceManager.ShipRoles[ResourceManager.ShipsDict[shipname].shipData.Role].Protected)
				{
					continue;
				}
				this.ShipSL.AddItem(shipname);
			}
			this.ConfirmRefit = new DanButton(new Vector2((float)shipDesignsRect.X, (float)(shipDesignsRect.Y + 505)), "Do Refit");
			this.RefitOne = new UIButton()
			{
				Rect = new Rectangle(shipDesignsRect.X + 25, shipDesignsRect.Y + 505, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_pressed"],
				Text = Localizer.Token(2265)
			};
			this.RefitAll = new UIButton()
			{
				Rect = new Rectangle(shipDesignsRect.X + 140, shipDesignsRect.Y + 505, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_low_btn_100px_pressed"],
				Text = Localizer.Token(2266)
			};
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}