using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Ship_Game
{
	public class QueueItem
	{
		public bool isBuilding;

		public bool isShip;

		public bool isTroop;

		public ShipData sData;

		public Ship_Game.Building Building;

		public Troop troop;

		public Rectangle rect;

		public Rectangle ProgressBarRect;

		public float productionTowards;

		public Rectangle promoteRect;

		public Rectangle demoteRect;

		public Rectangle removeRect;

		public int QueueNumber;

		public bool isRefit;

		public PlanetGridSquare pgs;

		public string DisplayName;

		public float Cost;

		public Ship_Game.Goal Goal;

		public Color PromoteColor = Color.White;

		public Color DemoteColor = Color.White;
        public bool NotifyOnEmpty =true;
        public bool notifyWhenBuilt =false;

		public QueueItem()
		{
		}
	}
}