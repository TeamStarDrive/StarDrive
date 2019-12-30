using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public delegate void QueueItemCompleted(bool success);

    public class QueueItem
    {
        public Planet Planet;
        public bool isBuilding;
        public bool IsMilitary; // Military building
        public bool isShip;
        public bool isOrbital;
        public bool isTroop;
        public ShipData sData;
        public Building Building;
        public string TroopType;
        public Array<Guid> TradeRoutes         = new Array<Guid>();
        public Array<Rectangle> AreaOfOperation = new Array<Rectangle>();
        public Rectangle rect;
        public Rectangle removeRect;
        public int QueueNumber;
        public int ShipLevel;
        public PlanetGridSquare pgs;
        public string DisplayName;
        public float Cost;
        public float ProductionSpent;
        public Goal Goal;
        public bool NotifyOnEmpty = true;
        public bool IsPlayerAdded = false;
        public bool TransportingColonists;
        public bool TransportingFood;
        public bool TransportingProduction;
        public bool AllowInterEmpireTrade;

        // Event action for when this QueueItem is finished
        public QueueItemCompleted OnComplete;

        // production still needed until this item is finished
        public float ProductionNeeded => ActualCost - ProductionSpent;

        // is this item finished constructing?
        public bool IsComplete => ProductionSpent.GreaterOrEqual(ActualCost); // float imprecision

        public QueueItem(Planet planet)
        {
            Planet = planet;
        }

        public int TurnsUntilComplete
        {
            get
            {
                float production = Planet.Prod.NetIncome;
                if (production <= 0f)
                    return 999;
                float turns = ProductionNeeded / production;
                return (int)Math.Ceiling(turns);
            }
        }

        public float ActualCost
        {
            get
            {
                float cost = Cost;
                if (isShip) cost *= Planet.ShipBuildingModifier;
                return (int)cost; // FB - int to avoid float issues in release which prevent items from being complete
            }
        }

        public string DisplayText
        {
            get
            {
                if (isBuilding)
                    return Localizer.Token(Building.NameTranslationIndex);
                if (isShip || isOrbital)
                    return DisplayName ?? sData.Name;
                if (isTroop)
                    return TroopType;
                return "";
            }
        }

        public override string ToString() => DisplayText;

        public SavedGame.QueueItemSave Serialize()
        {
            var qi = new SavedGame.QueueItemSave
            {
                isBuilding  = isBuilding,
                IsMilitary  = IsMilitary,
                Cost        = Cost,
                isShip      = isShip,
                DisplayName = DisplayName,
                isTroop     = isTroop,
                ProgressTowards = ProductionSpent,
                isPlayerAdded   = IsPlayerAdded,
                TradeRoutes     = TradeRoutes,
                AreaOfOperation = AreaOfOperation.Select(r => new RectangleData(r)),
                TransportingColonists  = TransportingColonists,
                TransportingFood       = TransportingFood,
                TransportingProduction = TransportingProduction,
                AllowInterEmpireTrade  = AllowInterEmpireTrade
        };
            if (qi.isBuilding) qi.UID = Building.Name;
            if (qi.isShip)     qi.UID = sData.Name;
            if (qi.isTroop)    qi.UID = TroopType;
            if (Goal != null) qi.GoalGUID  = Goal.guid;
            if (pgs != null)  qi.pgsVector = new Vector2(pgs.x, pgs.y);
            return qi;
        }
    }
}