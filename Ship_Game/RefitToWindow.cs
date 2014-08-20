using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class RefitToWindow : GameScreen, IDisposable
	{
		private Vector2 Cursor = Vector2.Zero;

		private ShipListScreen screen;

		private Ship shiptorefit;

		private List<UIButton> Buttons = new List<UIButton>();

		//private UIButton Exit;

		private Submenu sub_ships;

		private ScrollList ShipSL;

		private UIButton RefitOne;

		private UIButton RefitAll;

		private string RefitTo;

		private DanButton ConfirmRefit;

		private Selector selector;

		public RefitToWindow(ShipListScreenEntry entry, ShipListScreen screen)
		{
			this.screen = screen;
			this.shiptorefit = entry.ship;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
		}

		public RefitToWindow(Ship ship)
		{
			this.shiptorefit = ship;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
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
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.ScreenManager.SpriteBatch.Begin();
			this.sub_ships.Draw();
			Rectangle r = this.sub_ships.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			if (this.selector != null)
			{
				this.selector.Draw();
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
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ship.Role, tCursor, Color.Orange);
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
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.parseText(Fonts.Arial12Bold, string.Concat("Refit ", this.shiptorefit.Name, " to ", this.RefitTo), 270f), Cursor, Color.White);
			}
			if (base.IsActive)
			{
				ToolTip.Draw(base.ScreenManager);
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
        ~RefitToWindow() {
            //should implicitly do the same thing as the original bad finalize
        }

		public override void HandleInput(InputState input)
		{
			this.ShipSL.HandleInput(input);
			if (input.Escaped || input.CurrentMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			this.selector = null;
			for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Copied.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.ShipSL.Copied[i];
				if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
				{
					this.selector = new Selector(base.ScreenManager, e.clickRect);
					if (input.InGameSelect)
					{
						AudioManager.PlayCue("sd_ui_accept_alt3");
						this.RefitTo = e.item as string;
					}
				}
			}
			if (this.RefitTo != null)
			{
				if (HelperFunctions.CheckIntersection(this.RefitOne.Rect, input.CursorPosition))
				{
					ToolTip.CreateTooltip(Localizer.Token(2267), base.ScreenManager);
					if (input.InGameSelect)
					{
						this.shiptorefit.GetAI().OrderRefitTo(this.RefitTo);
						AudioManager.PlayCue("echo_affirm");
						this.ExitScreen();
					}
				}
				if (HelperFunctions.CheckIntersection(this.RefitAll.Rect, input.CursorPosition))
				{
					ToolTip.CreateTooltip(Localizer.Token(2268), base.ScreenManager);
					if (input.InGameSelect)
					{
						foreach (Ship ship in EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetShips())
						{
							if (ship.Name != this.shiptorefit.Name)
							{
								continue;
							}
							ship.GetAI().OrderRefitTo(this.RefitTo);
						}
						AudioManager.PlayCue("echo_affirm");
						this.ExitScreen();
					}
				}
			}
		}

		public override void LoadContent()
		{
			Rectangle shipDesignsRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 140, 100, 280, 500);
			this.sub_ships = new Submenu(base.ScreenManager, shipDesignsRect);
			this.ShipSL = new ScrollList(this.sub_ships, 40);
			this.sub_ships.AddTab("Refit to...");
			foreach (string shipname in this.shiptorefit.loyalty.ShipsWeCanBuild)
			{
				if (!(ResourceManager.ShipsDict[shipname].GetShipData().Hull == this.shiptorefit.GetShipData().Hull) || !(shipname != this.shiptorefit.Name) || ResourceManager.ShipRoles[ResourceManager.ShipsDict[shipname].Role].Protected)
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