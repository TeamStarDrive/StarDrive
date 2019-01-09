using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public class QueueItem
    {
        public Planet Planet;
        public bool isBuilding;
        public bool isShip;
        public bool isTroop;
        public ShipData sData;
        public Building Building;
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

        public int EstimatedTurnsToComplete
        {
            get
            {
                float production = Planet.Prod.NetIncome;
                if (production <= 0f)
                    return 10000;
                float turns = (Cost - productionTowards) / production;
                return (int)Math.Ceiling(turns);
            }
        }
            
        public QueueItem(Planet planet)
        {
            Planet = planet;
        }

        public string DisplayText
        {
            get
            {
                if (isBuilding)
                    return Localizer.Token(Building.NameTranslationIndex);
                if (isShip)
                    return DisplayName ?? sData.Name;
                if (isTroop)
                    return troopType;
                return "";
            }
        }

        public override string ToString() => DisplayText;
    }
}