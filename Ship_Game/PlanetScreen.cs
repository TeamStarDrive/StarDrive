using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PlanetScreen
	{
		public static UniverseScreen screen;

		public Texture2D Panel;

		//private Rectangle PanelRectangle;

		public Texture2D SliderBar;

		//private Planet p;

		//private Texture2D SliderButton;

		//private Texture2D SliderFillOrange;

		//private Texture2D SliderFillBlue;

		/*private Texture2D SliderFillGreen;

		private Texture2D foodIcon;

		private Texture2D prodIcon;

		private Texture2D assignWorkers;

		private Rectangle AssignWorkers = new Rectangle();

		private Rectangle sliderBar1 = new Rectangle();

		private Rectangle sliderBar2 = new Rectangle();

		private Rectangle sliderBar3 = new Rectangle();

		private Rectangle sliderButton1 = new Rectangle();

		private Rectangle sliderButton2 = new Rectangle();

		private Rectangle sliderButton3 = new Rectangle();

		private Rectangle sliderOrange = new Rectangle();

		private Rectangle sliderBlue = new Rectangle();

		private Rectangle sliderGreen = new Rectangle();

		private Rectangle GrowthInfoPanel = new Rectangle();

		private Rectangle topSepSource;

		private Rectangle topSepBar;

		private Rectangle BuildingsButton;

		private Rectangle ShipsButton;

		private Rectangle BuildingsPanel;

		private Rectangle buildingBottomSepBar;

		private Rectangle buildingRightSepBar;

		private Rectangle BuildingRightGradient;

		private Rectangle BottomGradient;

		private Rectangle ShipyardPanel;

		private Rectangle ShipMenu;

		private Rectangle ShipLeftGradient; */

		public List<ToolTip> tips = new List<ToolTip>();

		//private Ship_Game.ScreenManager ScreenManager;

		private List<QueueItem> DisplayedConstructionQ = new List<QueueItem>();

		private List<ShipQueueItem> ShipsList = new List<ShipQueueItem>();

		//private Rectangle foodIconRect;

		//private bool FirstRun = true;

		public PlanetScreen()
		{
		}

		public PlanetScreen(Planet p, Ship_Game.ScreenManager ScreenManager)
		{
		}

		public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
		{
		}

		/*private void DrawShipyardMenu(SpriteBatch spriteBatch)
		{
			spriteBatch.Draw(this.Panel, this.ShipyardPanel, Color.Black);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/RightLeftGradient"], this.ShipLeftGradient, Color.White);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/SepBarVertical"], new Rectangle(this.ShipyardPanel.X, this.ShipyardPanel.Y, 3, this.ShipyardPanel.Height + 3), new Rectangle?(new Rectangle(0, 0, 3, this.ShipyardPanel.Height + 3)), Color.White);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/SepBar"], new Rectangle(this.ShipyardPanel.X, this.ShipyardPanel.Y + this.ShipyardPanel.Height, this.buildingBottomSepBar.Width + 1, this.buildingBottomSepBar.Height), new Rectangle?(new Rectangle(0, 0, this.buildingBottomSepBar.Width + 1, this.buildingBottomSepBar.Height)), Color.White);
			foreach (ShipQueueItem item in this.ShipsList)
			{
				item.Draw(spriteBatch);
			}
		}*/

		private void GetBuildingsWeCanBuildHere()
		{
		}

		public virtual void HandleInput(InputState input)
		{
		}

		private void HandleSlider()
		{
		}

		public virtual void Update(float elapsedTime)
		{
		}

		/*public void UpdateShipyardList()
		{
			this.ShipsList.Clear();
			int numShips = 0;
			foreach (KeyValuePair<string, Ship> hull in ResourceManager.ShipsDict)
			{
				Rectangle qRect = new Rectangle(this.ShipyardPanel.X + 23, this.ShipyardPanel.Y + numShips * 50, 300, 50);
				ShipQueueItem qI = new ShipQueueItem(hull.Value.GetShipData(), qRect, this.p.Owner.EmpireColor);
				this.ShipsList.Add(qI);
				numShips++;
			}
		}*/
	}
}