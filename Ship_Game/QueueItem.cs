using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class QueueItem
    {
        public bool isBuilding;
        public bool isShip;
        public bool isTroop;
        public ShipData sData;
        public Ship_Game.Building Building;
        public string troopType;
        public Rectangle rect;
        public Rectangle ProgressBarRect;
        public float productionTowards;
        public Rectangle promoteRect;
        public Rectangle demoteRect;
        public Rectangle removeRect;
        public int QueueNumber;
        public bool isRefit;
        public string RefitName = "";
        public PlanetGridSquare pgs;
        public string DisplayName;
        public float Cost;
        public Goal Goal;
        public Color PromoteColor = Color.White;
        public Color DemoteColor = Color.White;
        public bool NotifyOnEmpty =true;
        public bool notifyWhenBuilt =false;
        public bool IsPlayerAdded = false;
    }
}