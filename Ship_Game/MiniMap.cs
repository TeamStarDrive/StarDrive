using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class MiniMap
	{
		private Rectangle Housing;

		private Rectangle ActualMap;

		private Rectangle R;

		private ToggleButton zOut;

		private ToggleButton zIn;

		private ToggleButton pList;

		private ToggleButton sList;

		private ToggleButton Auto;

		private ToggleButton DSB;

		private ToggleButton Fleets;

		public MiniMap(Rectangle housing)
		{
			this.Housing = housing;
			this.ActualMap = new Rectangle(housing.X + 61, housing.Y + 43, 200, 200);
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70, 22, 22);
			this.zIn = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "Minimap/icons_zoomctrl");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 25, 22, 22);
			this.zOut = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "Minimap/icons_zoomout");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 50, 22, 22);
			this.DSB = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_dsbw");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 75, 22, 22);
			this.pList = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_planetslist");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 100, 22, 22);
			this.sList = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_shipslist");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 125, 22, 22);
			this.Fleets = new ToggleButton(this.R, "Minimap/button_normal", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_fleets");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 150, 22, 26);
			this.Auto = new ToggleButton(this.R, "Minimap/button_down_inactive", "Minimap/button_down_inactive", "Minimap/button_down_active", "Minimap/button_down_inactive", "AI");
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager, UniverseScreen screen)
		{
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Minimap/radar"], this.Housing, Color.White);
			float scale = (float)this.ActualMap.Width / screen.Size.X;
			Vector2 MinimapZero = new Vector2((float)this.ActualMap.X, (float)this.ActualMap.Y);
			foreach (Empire e in EmpireManager.EmpireList)
			{
				if (e != EmpireManager.GetEmpireByName(screen.PlayerLoyalty) && !EmpireManager.GetEmpireByName(screen.PlayerLoyalty).GetRelations()[e].Known)
				{
					continue;
				}
				List<Circle> circles = new List<Circle>();
				lock (GlobalStats.BorderNodeLocker)
				{
					foreach (Empire.InfluenceNode node in e.BorderNodes)
					{
						float radius = node.Radius * scale;
						Vector2 nodepos = new Vector2(MinimapZero.X + node.Position.X * scale, MinimapZero.Y + node.Position.Y * scale);
						Vector2 Origin = new Vector2((float)(ResourceManager.TextureDict["UI/node"].Width / 2), (float)(ResourceManager.TextureDict["UI/node"].Height / 2));
						Color ec = new Color(e.EmpireColor.R, e.EmpireColor.G, e.EmpireColor.B, 30);
						float rscale = radius * 0.005f;
						if ((double)rscale < 0.006)
						{
							rscale = 0.006f;
						}
						Rectangle? nullable = null;
						ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/node1"], nodepos, nullable, ec, 0f, Origin, rscale, SpriteEffects.None, 1f);
					}
				}
			}
			foreach (SolarSystem system in UniverseScreen.SolarSystemList)
			{
				Rectangle star = new Rectangle((int)(MinimapZero.X + system.Position.X * scale), (int)(MinimapZero.Y + system.Position.Y * scale), 2, 2);
				if (system.OwnerList.Count <= 0 || !system.ExploredDict[EmpireManager.GetEmpireByName(screen.PlayerLoyalty)])
				{
					Primitives2D.FillRectangle(ScreenManager.SpriteBatch, star, Color.Gray);
				}
				else
				{
					Primitives2D.FillRectangle(ScreenManager.SpriteBatch, star, system.OwnerList[0].EmpireColor);
				}
			}
			Vector2 upperLeftView = screen.GetWorldSpaceFromScreenSpace(new Vector2(0f, 0f));
			upperLeftView = new Vector2((float)HelperFunctions.RoundTo(upperLeftView.X, 20000), (float)HelperFunctions.RoundTo(upperLeftView.Y, 20000));
			Vector2 right = screen.GetWorldSpaceFromScreenSpace(new Vector2((float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, 0f));
			right = new Vector2((float)HelperFunctions.RoundTo(right.X, 20000), 0f);
			float xdist = (right.X - upperLeftView.X) * scale;
			xdist = (float)HelperFunctions.RoundTo(xdist, 1);
			float ydist = xdist * (float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / (float)ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			Rectangle LookingAt = new Rectangle((int)MinimapZero.X + (int)(upperLeftView.X * scale), (int)MinimapZero.Y + (int)(upperLeftView.Y * scale), (int)xdist, (int)ydist);
			if (LookingAt.Width < 2)
			{
				LookingAt.Width = 2;
				LookingAt.Height = 2;
			}
			if (LookingAt.X < this.ActualMap.X)
			{
				LookingAt.X = this.ActualMap.X;
			}
			if (LookingAt.Y < this.ActualMap.Y)
			{
				LookingAt.Y = this.ActualMap.Y;
			}
			Primitives2D.FillRectangle(ScreenManager.SpriteBatch, LookingAt, new Color(255, 255, 255, 30));
			Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, LookingAt, Color.White);
			Vector2 topMiddleView = new Vector2((float)(LookingAt.X + LookingAt.Width / 2), (float)LookingAt.Y);
			Vector2 botMiddleView = new Vector2(topMiddleView.X - 1f, (float)(LookingAt.Y + LookingAt.Height));
			Vector2 leftMiddleView = new Vector2((float)LookingAt.X, (float)(LookingAt.Y + LookingAt.Height / 2));
			Vector2 rightMiddleView = new Vector2((float)(LookingAt.X + LookingAt.Width), leftMiddleView.Y + 1f);
			Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2(topMiddleView.X, MinimapZero.Y), topMiddleView, Color.White);
			Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2(botMiddleView.X, (float)(this.ActualMap.Y + this.ActualMap.Height)), botMiddleView, Color.White);
			Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2((float)this.ActualMap.X, leftMiddleView.Y), leftMiddleView, Color.White);
			Primitives2D.DrawLine(ScreenManager.SpriteBatch, new Vector2((float)(this.ActualMap.X + this.ActualMap.Width), rightMiddleView.Y), rightMiddleView, Color.White);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Minimap/radar_over"], this.Housing, Color.White);
			this.zOut.DrawIconResized(ScreenManager);
			this.zIn.DrawIconResized(ScreenManager);
			this.DSB.DrawIconResized(ScreenManager);
			this.pList.DrawIconResized(ScreenManager);
			this.sList.DrawIconResized(ScreenManager);
			this.Fleets.DrawIconResized(ScreenManager);
			this.Auto.DrawIconResized(ScreenManager);
		}

		public bool HandleInput(InputState input, UniverseScreen screen)
		{
			if (HelperFunctions.CheckIntersection(this.zIn.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(57, screen.ScreenManager, "Page Up");
			}
			if (this.zIn.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				screen.AdjustCamTimer = 1f;
				screen.transitionElapsedTime = 0f;
				screen.transitionDestination.Z = 4500f;
				screen.snappingToShip = true;
				screen.ViewingShip = true;
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.zOut.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(58, screen.ScreenManager, "Page Down");
			}
			if (this.zOut.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				screen.AdjustCamTimer = 1f;
				screen.transitionElapsedTime = 0f;
				screen.transitionDestination.X = screen.camPos.X;
				screen.transitionDestination.Y = screen.camPos.Y;
				screen.transitionDestination.Z = 4200000f * UniverseScreen.GameScaleStatic;
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.DSB.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(54, screen.ScreenManager, "B");
			}
			if (this.DSB.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				if (screen.showingDSBW)
				{
					screen.showingDSBW = false;
				}
				else
				{
					screen.dsbw = new DeepSpaceBuildingWindow(screen.ScreenManager, screen);
					screen.showingDSBW = true;
				}
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.pList.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(56, screen.ScreenManager, "L");
			}
			if (this.pList.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				screen.ScreenManager.AddScreen(new PlanetListScreen(screen.ScreenManager, screen.EmpireUI));
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.sList.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(55, screen.ScreenManager, "K");
			}
			if (this.sList.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				screen.ScreenManager.AddScreen(new ShipListScreen(screen.ScreenManager, screen.EmpireUI));
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.Fleets.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(60, screen.ScreenManager, "J");
			}
			if (this.Fleets.HandleInput(input))
			{
				AudioManager.PlayCue("sd_ui_accept_alt3");
				screen.ScreenManager.AddScreen(new FleetDesignScreen(screen.EmpireUI));
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.Auto.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(59, screen.ScreenManager, "H");
			}
			if (!this.Auto.HandleInput(input))
			{
				return false;
			}
			AudioManager.PlayCue("sd_ui_accept_alt3");
			screen.aw.isOpen = !screen.aw.isOpen;
			return true;
		}
	}
}