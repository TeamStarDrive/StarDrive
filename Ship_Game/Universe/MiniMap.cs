using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class MiniMap
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
			this.zIn = new ToggleButton(this.R, "Minimap/button_C_normal", "Minimap/button_C_normal", "Minimap/button_C_hover", "Minimap/button_C_normal", "Minimap/icons_zoomctrl");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 25, 22, 22);
			this.zOut = new ToggleButton(this.R, "Minimap/button_C_normal", "Minimap/button_C_normal", "Minimap/button_C_hover", "Minimap/button_C_normal", "Minimap/icons_zoomout");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 50, 22, 22);
			this.pList = new ToggleButton(this.R, "Minimap/button_B_normal", "Minimap/button_B_normal", "Minimap/button_B_hover", "Minimap/button_B_normal", "UI/icon_planetslist");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 75, 22, 22);
			this.sList = new ToggleButton(this.R, "Minimap/button_active", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_ftloverlay");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 100, 22, 22);
			this.Fleets = new ToggleButton(this.R, "Minimap/button_active", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_rangeoverlay");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 125, 22, 22);
			this.DSB = new ToggleButton(this.R, "Minimap/button_active", "Minimap/button_normal", "Minimap/button_hover", "Minimap/button_normal", "UI/icon_dsbw");
			this.R = new Rectangle(this.Housing.X + 14, this.Housing.Y + 70 + 150, 22, 26);
			this.Auto = new ToggleButton(this.R, "Minimap/button_down_active", "Minimap/button_down_inactive", "Minimap/button_down_hover", "Minimap/button_down_inactive", "AI");
		}

		public void Draw(ScreenManager screenManager, UniverseScreen screen)
		{
			screenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Minimap/radar_over"], this.Housing, Color.White);
			float scale = (float)this.ActualMap.Width / (screen.Size.X * 2);        //Updated to play nice with the new negative map values
			Vector2 minimapZero = new Vector2((float)ActualMap.X + 100, (float)ActualMap.Y + 100);
            var uiNode  = ResourceManager.TextureDict["UI/node"];
            var uiNode1 = ResourceManager.TextureDict["UI/node1"];

			foreach (Empire e in EmpireManager.Empires)
			{
				if (e != EmpireManager.Player && !EmpireManager.Player.GetRelations(e).Known)
					continue;

                using (e.BorderNodes.AcquireReadLock())
				{
					foreach (Empire.InfluenceNode node in e.BorderNodes)
					{
						float radius = node.Radius * scale;
						Vector2 nodepos = new Vector2(minimapZero.X + node.Position.X * scale, minimapZero.Y + node.Position.Y * scale);
						Color ec = new Color(e.EmpireColor.R, e.EmpireColor.G, e.EmpireColor.B, 30);
						float rscale = radius * 0.005f;
						if (rscale < 0.006f) rscale = 0.006f;
						screenManager.SpriteBatch.Draw(uiNode1, nodepos, null, ec, 0f, uiNode.Center(), rscale, SpriteEffects.None, 1f);
					}
				}
			}
			foreach (SolarSystem system in UniverseScreen.SolarSystemList)
			{
				Rectangle star = new Rectangle((int)(minimapZero.X + system.Position.X * scale), (int)(minimapZero.Y + system.Position.Y * scale), 2, 2);
				if (system.OwnerList.Count <= 0 || !system.ExploredDict[EmpireManager.Player])
				{
					Primitives2D.FillRectangle(screenManager.SpriteBatch, star, Color.Gray);
				}
				else
				{
                    Primitives2D.FillRectangle(screenManager.SpriteBatch, star, system.OwnerList.ToList()[0].EmpireColor);
				}
			}
			Vector2 upperLeftView = screen.UnprojectToWorldPosition(new Vector2(0f, 0f));
			upperLeftView = new Vector2((float)HelperFunctions.RoundTo(upperLeftView.X, 20000), (float)HelperFunctions.RoundTo(upperLeftView.Y, 20000));
			Vector2 right = screen.UnprojectToWorldPosition(new Vector2((float)screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, 0f));
			right = new Vector2((float)HelperFunctions.RoundTo(right.X, 20000), 0f);
			float xdist = (right.X - upperLeftView.X) * scale;
			xdist = (float)HelperFunctions.RoundTo(xdist, 1);
			float ydist = xdist * (float)screenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / (float)screenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
			Rectangle LookingAt = new Rectangle((int)minimapZero.X + (int)(upperLeftView.X * scale), (int)minimapZero.Y + (int)(upperLeftView.Y * scale), (int)xdist, (int)ydist);
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
			Primitives2D.FillRectangle(screenManager.SpriteBatch, LookingAt, new Color(255, 255, 255, 30));
			Primitives2D.DrawRectangle(screenManager.SpriteBatch, LookingAt, Color.White);
			Vector2 topMiddleView = new Vector2((float)(LookingAt.X + LookingAt.Width / 2), (float)LookingAt.Y);
			Vector2 botMiddleView = new Vector2(topMiddleView.X - 1f, (float)(LookingAt.Y + LookingAt.Height));
			Vector2 leftMiddleView = new Vector2((float)LookingAt.X, (float)(LookingAt.Y + LookingAt.Height / 2));
			Vector2 rightMiddleView = new Vector2((float)(LookingAt.X + LookingAt.Width), leftMiddleView.Y + 1f);
			Primitives2D.DrawLine(screenManager.SpriteBatch, new Vector2(topMiddleView.X, minimapZero.Y - 100), topMiddleView, Color.White);
			Primitives2D.DrawLine(screenManager.SpriteBatch, new Vector2(botMiddleView.X, (float)(this.ActualMap.Y + this.ActualMap.Height)), botMiddleView, Color.White);
			Primitives2D.DrawLine(screenManager.SpriteBatch, new Vector2((float)this.ActualMap.X, leftMiddleView.Y), leftMiddleView, Color.White);
			Primitives2D.DrawLine(screenManager.SpriteBatch, new Vector2((float)(this.ActualMap.X + this.ActualMap.Width), rightMiddleView.Y), rightMiddleView, Color.White);
			//ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Minimap/radar_over"], this.Housing, Color.White);
            if (screen.showingFTLOverlay)
            {
                this.sList.Active = true;
            }
            else
            {
                this.sList.Active = false;
            }

            if (screen.showingDSBW)
            {
                this.DSB.Active = true;
            }
            else
            {
                this.DSB.Active = false;
            }

            if (screen.aw.isOpen)
            {
                this.Auto.Active = true;
            }
            else
            {
                this.Auto.Active = false;
            }

            if (screen.showingRangeOverlay)
            {
                this.Fleets.Active = true;
            }
            else
            {
                this.Fleets.Active = false;
            }

			this.zOut.DrawIconResized(screenManager);
			this.zIn.DrawIconResized(screenManager);
			this.DSB.DrawIconResized(screenManager);
			this.pList.DrawIconResized(screenManager);
			this.sList.DrawIconResized(screenManager);
			this.Fleets.DrawIconResized(screenManager);
			this.Auto.DrawIconResized(screenManager);
		}

		public bool HandleInput(InputState input, UniverseScreen screen)
		{
			if (HelperFunctions.CheckIntersection(this.zIn.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(57, screen.ScreenManager, "Page Up");
			}
			if (this.zIn.HandleInput(input))
			{
				GameAudio.PlaySfx("sd_ui_accept_alt3");
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
				GameAudio.PlaySfx("sd_ui_accept_alt3");
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
				GameAudio.PlaySfx("sd_ui_accept_alt3");
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
				ToolTip.CreateTooltip(56, screen.ScreenManager);
			}
			if (this.pList.HandleInput(input))
			{
                GameAudio.PlaySfx("sd_ui_accept_alt3");
                screen.ScreenManager.AddScreen(new PlanetListScreen(screen, screen.EmpireUI));
                return true;
			}
			if (HelperFunctions.CheckIntersection(this.sList.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(223, screen.ScreenManager, "F1");
			}
			if (this.sList.HandleInput(input))
			{                
				GameAudio.PlaySfx("sd_ui_accept_alt3");
                if (screen.showingFTLOverlay)
                {
                    screen.showingFTLOverlay = false;
                }
                else
                {
                    screen.showingFTLOverlay = true;
                }
				return true;
			}
			if (HelperFunctions.CheckIntersection(this.Fleets.r, input.CursorPosition))
			{
				ToolTip.CreateTooltip(224, screen.ScreenManager, "F2");
			}
			if (this.Fleets.HandleInput(input))
			{
				GameAudio.PlaySfx("sd_ui_accept_alt3");
                if (screen.showingRangeOverlay)
                {
                    screen.showingRangeOverlay = false;
                }
                else
                {
                    screen.showingRangeOverlay = true;
                }
				//screen.ScreenManager.AddScreen(new FleetDesignScreen(screen.EmpireUI));
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
			GameAudio.PlaySfx("sd_ui_accept_alt3");
			screen.aw.isOpen = !screen.aw.isOpen;
			return true;
		}
	}
}