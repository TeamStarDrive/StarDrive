using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PlanetListScreenEntry
	{
		public Planet planet;

		public Rectangle TotalEntrySize;

		public Rectangle SysNameRect;

		public Rectangle PlanetNameRect;

		public Rectangle FertRect;

		public Rectangle RichRect;

		public Rectangle PopRect;

		public Rectangle OwnerRect;

		public Rectangle OrdersRect;

		private Rectangle ShipIconRect;

		private UITextEntry ShipNameEntry = new UITextEntry();

		private UIButton Colonize;

		public PlanetListScreen screen;

		private bool marked;

		//private string Status_Text;

		public PlanetListScreenEntry(Planet p, int x, int y, int width1, int height, PlanetListScreen caller)
		{
			this.screen = caller;
			this.planet = p;
			this.TotalEntrySize = new Rectangle(x, y, width1 - 60, height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.12f), height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.25f), height);
			this.FertRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 100, height);
			this.RichRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width, y, 120, height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width, y, 200, height);
			this.OwnerRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width, y, 100, height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width + this.OwnerRect.Width, y, 100, height);
			//this.Status_Text = "";
			this.ShipIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, 50, 50);
			string shipName = this.planet.Name;
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			this.ShipNameEntry.Text = shipName;
			float width = (float)((int)((float)this.FertRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
			this.Colonize = new UIButton()
			{
				Rect = new Rectangle(this.OrdersRect.X + 10, this.OrdersRect.Y + this.OrdersRect.Height / 2 - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height / 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"]
			};
			Goal goal = new Goal();
			foreach (Goal g in Ship.universeScreen.player.GetGSAI().Goals)
			{
				if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != p)
				{
					continue;
				}
				this.marked = true;
			}
			if (!this.marked)
			{
				this.Colonize.Text = Localizer.Token(1425);
			}
			else
			{
				this.Colonize.Text = Localizer.Token(1426);
			}
			this.Colonize.Launches = Localizer.Token(1425);
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, GameTime gameTime)
		{
			string singular;
			Color TextColor = new Color(255, 239, 208);
			string sysname = this.planet.system.Name;
			if (Fonts.Arial20Bold.MeasureString(sysname).X <= (float)this.SysNameRect.Width)
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial20Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, sysname, SysNameCursor, TextColor);
			}
			else
			{
				Vector2 SysNameCursor = new Vector2((float)(this.SysNameRect.X + this.SysNameRect.Width / 2) - Fonts.Arial12Bold.MeasureString(sysname).X / 2f, (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
				ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, sysname, SysNameCursor, TextColor);
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			if (this.planet.system.DangerTimer > 0f)
			{
				TimeSpan totalGameTime = gameTime.TotalGameTime;
				float f = (float)Math.Sin((double)totalGameTime.TotalSeconds);
				f = Math.Abs(f) * 255f;
				Color flashColor = new Color(255, 255, 255, (byte)f);
				Rectangle flashRect = new Rectangle(this.SysNameRect.X + this.SysNameRect.Width - 40, this.SysNameRect.Y + 5, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Width, ResourceManager.TextureDict["Ground_UI/Ground_Attack"].Height);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Ground_UI/EnemyHere"], flashRect, flashColor);
				if (HelperFunctions.CheckIntersection(flashRect, MousePos))
				{
					ToolTip.CreateTooltip(123, ScreenManager);
				}
			}
			Rectangle planetIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, this.PlanetNameRect.Height - 10, this.PlanetNameRect.Height - 10);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Planets/", this.planet.planetType)], planetIconRect, Color.White);
			if (this.planet.Owner != null)
			{
				SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
				KeyValuePair<string, Texture2D> item = ResourceManager.FlagTextures[this.planet.Owner.data.Traits.FlagIndex];
				spriteBatch.Draw(item.Value, planetIconRect, this.planet.Owner.EmpireColor);
			}
			int i = 0;
			Vector2 StatusIcons = new Vector2((float)(this.PlanetNameRect.X + this.PlanetNameRect.Width - 50), (float)(planetIconRect.Y + 10));
			if (this.planet.RecentCombat)
			{
				Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 14, 14);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_fighting_small"], statusRect, Color.White);
				if (HelperFunctions.CheckIntersection(statusRect, MousePos))
				{
					ToolTip.CreateTooltip(119, ScreenManager);
				}
				i++;
			}
			if (EmpireManager.GetEmpireByName(this.screen.empUI.screen.PlayerLoyalty).data.MoleList.Count > 0)
			{
				foreach (Mole m in EmpireManager.GetEmpireByName(this.screen.empUI.screen.PlayerLoyalty).data.MoleList)
				{
					if (m.PlanetGuid != this.planet.guid)
					{
						continue;
					}
					StatusIcons.X = StatusIcons.X + (float)(18 * i);
					Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 14, 14);
					ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_spy_small"], statusRect, Color.White);
					i++;
					if (!HelperFunctions.CheckIntersection(statusRect, MousePos))
					{
						break;
					}
					ToolTip.CreateTooltip(120, ScreenManager);
					break;
				}
			}
			foreach (Building b in this.planet.BuildingList)
			{
				if (b.EventTriggerUID == "")
				{
					continue;
				}
				StatusIcons.X = StatusIcons.X + (float)(18 * i);
				Rectangle statusRect = new Rectangle((int)StatusIcons.X, (int)StatusIcons.Y, 14, 14);
				ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/icon_anomaly_small"], statusRect, Color.White);
				if (!HelperFunctions.CheckIntersection(statusRect, MousePos))
				{
					break;
				}
				ToolTip.CreateTooltip(121, ScreenManager);
				break;
			}
			Vector2 rpos = new Vector2()
			{
				X = (float)this.ShipNameEntry.ClickableArea.X,
				Y = (float)(this.ShipNameEntry.ClickableArea.Y - 10)
			};
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.planet.Name, rpos, TextColor);
			rpos.Y = rpos.Y + (float)(Fonts.Arial20Bold.LineSpacing - 3);
			Vector2 FertilityCursor = new Vector2((float)(this.FertRect.X + 35), (float)(this.FertRect.Y + this.FertRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.planet.Fertility.ToString("#.0"), FertilityCursor, (this.planet.habitable ? Color.White : Color.LightPink));
			Vector2 RichCursor = new Vector2((float)(this.RichRect.X + 35), (float)(this.RichRect.Y + this.RichRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.planet.MineralRichness.ToString("#.0"), RichCursor, (this.planet.habitable ? Color.White : Color.LightPink));
			Vector2 PopCursor = new Vector2((float)(this.PopRect.X + 60), (float)(this.PopRect.Y + this.PopRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			SpriteBatch spriteBatch1 = ScreenManager.SpriteBatch;
			SpriteFont arial12Bold = Fonts.Arial12Bold;
			float population = this.planet.Population / 1000f;
			string str = population.ToString("#.0");
			float maxPopulation = (this.planet.MaxPopulation + this.planet.MaxPopBonus) / 1000f;
			spriteBatch1.DrawString(arial12Bold, string.Concat(str, " / ", maxPopulation.ToString("#.0")), PopCursor, (this.planet.habitable ? Color.White : Color.LightPink));
			Vector2 OwnerCursor = new Vector2((float)(this.OwnerRect.X + 20), (float)(this.OwnerRect.Y + this.OwnerRect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2));
			SpriteBatch spriteBatch2 = ScreenManager.SpriteBatch;
			SpriteFont spriteFont = Fonts.Arial12Bold;
			if (this.planet.Owner != null)
			{
				singular = this.planet.Owner.data.Traits.Singular;
			}
			else
			{
				singular = (this.planet.habitable ? Localizer.Token(2263) : Localizer.Token(2264));
			}
			spriteBatch2.DrawString(spriteFont, singular, OwnerCursor, (this.planet.Owner != null ? this.planet.Owner.EmpireColor : Color.Gray));
			string PlanetText = string.Concat(this.planet.GetTypeTranslation(), " ", this.planet.GetRichness());
			Vector2 vector2 = new Vector2((float)(this.FertRect.X + 10), (float)(2 + this.SysNameRect.Y + this.SysNameRect.Height / 2) - Fonts.Arial12Bold.MeasureString(PlanetText).Y / 2f);
			ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, PlanetText, rpos, TextColor);
			if (this.planet.habitable && this.planet.Owner == null)
			{
				this.Colonize.Draw(ScreenManager.SpriteBatch);
			}
		}

		public void HandleInput(InputState input)
		{
			if (!HelperFunctions.CheckIntersection(this.Colonize.Rect, input.CursorPosition))
			{
				this.Colonize.State = UIButton.PressState.Normal;
			}
			else
			{
				this.Colonize.State = UIButton.PressState.Hover;
				if (input.InGameSelect)
				{
					if (!this.marked)
					{
						AudioManager.PlayCue("echo_affirm");
						Goal g = new Goal(this.planet, Ship.universeScreen.player);
						Ship.universeScreen.player.GetGSAI().Goals.Add(g);
						this.Colonize.Text = "Cancel Colonize";
						this.marked = true;
						return;
					}
					foreach (Goal g in Ship.universeScreen.player.GetGSAI().Goals)
					{
						if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != this.planet)
						{
							continue;
						}
						AudioManager.PlayCue("echo_affirm");
						if (g.GetColonyShip() != null)
						{
							g.GetColonyShip().GetAI().OrderOrbitNearest(true);
						}
						Ship.universeScreen.player.GetGSAI().Goals.QueuePendingRemoval(g);
						this.marked = false;
						this.Colonize.Text = "Colonize";
						break;
					}
					Ship.universeScreen.player.GetGSAI().Goals.ApplyPendingRemovals();
					return;
				}
			}
		}

		public void SetNewPos(int x, int y)
		{
			this.TotalEntrySize = new Rectangle(x, y, this.TotalEntrySize.Width, this.TotalEntrySize.Height);
			this.SysNameRect = new Rectangle(x, y, (int)((float)this.TotalEntrySize.Width * 0.12f), this.TotalEntrySize.Height);
			this.PlanetNameRect = new Rectangle(x + this.SysNameRect.Width, y, (int)((float)this.TotalEntrySize.Width * 0.25f), this.TotalEntrySize.Height);
			this.FertRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width, y, 100, this.TotalEntrySize.Height);
			this.RichRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width, y, 120, this.TotalEntrySize.Height);
			this.PopRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width, y, 200, this.TotalEntrySize.Height);
			this.OwnerRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width, y, 100, this.TotalEntrySize.Height);
			this.OrdersRect = new Rectangle(x + this.SysNameRect.Width + this.PlanetNameRect.Width + this.FertRect.Width + this.RichRect.Width + this.PopRect.Width + this.OwnerRect.Width, y, 100, this.TotalEntrySize.Height);
			this.ShipIconRect = new Rectangle(this.PlanetNameRect.X + 5, this.PlanetNameRect.Y + 5, 50, 50);
			string shipName = this.planet.Name;
			this.ShipNameEntry.ClickableArea = new Rectangle(this.ShipIconRect.X + this.ShipIconRect.Width + 10, 2 + this.SysNameRect.Y + this.SysNameRect.Height / 2 - Fonts.Arial20Bold.LineSpacing / 2, (int)Fonts.Arial20Bold.MeasureString(shipName).X, Fonts.Arial20Bold.LineSpacing);
			this.Colonize.Rect = new Rectangle(this.OrdersRect.X + 10, this.OrdersRect.Y + this.OrdersRect.Height / 2 - ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height / 2, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height);
			float width = (float)((int)((float)this.FertRect.Width * 0.8f));
			while (width % 10f != 0f)
			{
				width = width + 1f;
			}
		}
	}
}