using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class MinimapButtons
	{
		public Rectangle Rect;

		private SkinnableButton bDSBW;

		private SkinnableButton bAutomation;

		private SkinnableButton bPlanetsList;

		private SkinnableButton bFleets;

		private SkinnableButton bShipsList;

		private SkinnableButton home;

		private SkinnableButton zoom;

		public static UniverseScreen screen;

		public MinimapButtons(Rectangle r, EmpireUIOverlay eui)
		{
			this.Rect = r;
			Vector2 Cursor = new Vector2((float)(r.X + 11), (float)(r.Y + 13));
			Color baseColor = new Color(34, 42, 56);
			this.home = new SkinnableButton(new Rectangle((int)Cursor.X + 3, (int)Cursor.Y, 15, 16), "UI/icon_home")
			{
				BaseColor = baseColor,
				HoverColor = new Color(140, 88, 50),
				IsToggle = false
			};
			Cursor.Y = Cursor.Y + 25f;
			this.zoom = new SkinnableButton(new Rectangle((int)Cursor.X, (int)Cursor.Y, 22, 22), "UI/icon_minus")
			{
				BaseColor = baseColor,
				HoverColor = new Color(140, 88, 50),
				IsToggle = false
			};
			Cursor.Y = Cursor.Y + 28f;
			this.bDSBW = new SkinnableButton(new Rectangle((int)Cursor.X, (int)Cursor.Y, 22, 17), "UI/icon_dsbw")
			{
				BaseColor = baseColor,
				HoverColor = new Color(140, 88, 50),
				IsToggle = false
			};
			Cursor.Y = Cursor.Y + 28f;
			this.bPlanetsList = new SkinnableButton(new Rectangle((int)Cursor.X, (int)Cursor.Y, 21, 21), "UI/icon_planetslist")
			{
				BaseColor = baseColor,
				HoverColor = new Color(140, 88, 50),
				IsToggle = false
			};
			Cursor.Y = Cursor.Y + 28f;
			this.bShipsList = new SkinnableButton(new Rectangle((int)Cursor.X + 2, (int)Cursor.Y, 18, 19), "UI/icon_shipslist")
			{
				BaseColor = baseColor,
				IsToggle = false,
				HoverColor = new Color(140, 88, 50)
			};
			Cursor.Y = Cursor.Y + 28f;
			this.bFleets = new SkinnableButton(new Rectangle((int)Cursor.X + 2, (int)Cursor.Y, 18, 16), "UI/icon_fleets")
			{
				BaseColor = baseColor,
				HoverColor = new Color(140, 88, 50),
				IsToggle = false
			};
			Cursor.Y = Cursor.Y + 28f;
			this.bAutomation = new SkinnableButton(new Rectangle((int)Cursor.X + 2, (int)Cursor.Y, 15, 12), "UI/icon_automation")
			{
				BaseColor = baseColor,
				IsToggle = false,
				HoverColor = new Color(140, 88, 50)
			};
		}

		public void Draw()
		{
			this.home.Draw(MinimapButtons.screen.ScreenManager);
			this.zoom.Draw(MinimapButtons.screen.ScreenManager);
			this.bDSBW.Draw(MinimapButtons.screen.ScreenManager);
			this.bPlanetsList.Draw(MinimapButtons.screen.ScreenManager);
			this.bShipsList.Draw(MinimapButtons.screen.ScreenManager);
			this.bFleets.Draw(MinimapButtons.screen.ScreenManager);
			this.bAutomation.Draw(MinimapButtons.screen.ScreenManager);
		}

		public bool HandleInput(InputState input)
		{
			bool clicked = false;
			if (!HelperFunctions.CheckIntersection(this.bAutomation.r, input.CursorPosition))
			{
				this.bAutomation.Hover = false;
			}
			else
			{
				this.bAutomation.Hover = true;
				ToolTip.CreateTooltip(59, MinimapButtons.screen.ScreenManager);
			}
			if (this.bAutomation.HandleInput(input))
			{
				MinimapButtons.screen.aw.isOpen = !MinimapButtons.screen.aw.isOpen;
				clicked = true;
			}
			return clicked;
		}
	}
}