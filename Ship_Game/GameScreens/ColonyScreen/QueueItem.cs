using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Audio;
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
        public Array<Guid> TradeRoutes = new Array<Guid>();
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
        public bool Rush;
        public bool NotifyOnEmpty = true;
        public bool IsPlayerAdded = false;
        public bool TransportingColonists  = true;
        public bool TransportingFood       = true;
        public bool TransportingProduction = true;
        public bool AllowInterEmpireTrade  = true;

        // Event action for when this QueueItem is finished
        public QueueItemCompleted OnComplete;

        // production still needed until this item is finished
        public float ProductionNeeded => ActualCost - ProductionSpent;

        // is this item finished constructing?
        public bool IsComplete => ProductionSpent.GreaterOrEqual(ActualCost); // float imprecision

        // if TRUE, this QueueItem will be cancelled during next production queue update
        public bool IsCancelled;

        public QueueItem(Planet planet)
        {
            Planet = planet;
        }

        public void SetCanceled(bool state = true) => IsCancelled = state;

        public void DrawAt(SpriteBatch batch, Vector2 at, bool lowRes)
        {
            var r = new Rectangle((int)at.X, (int)at.Y, 29, 30);
            var tCursor = new Vector2(at.X + 40f, at.Y);
            var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing + 4, 150, 18);
            var pb = new ProgressBar(pbRect, ActualCost, ProductionSpent);
            var rushCursor = new Vector2(at.X + 200f, at.Y + 18);

            if (isBuilding)
            {
                batch.Draw(Building.IconTex, r);
                batch.DrawString(Fonts.Arial12Bold, Building.TranslatedName, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isShip)
            {
                batch.Draw(sData.Icon, r);
                string name = DisplayName.IsEmpty() ? sData.Name : DisplayName;
                batch.DrawString(Fonts.Arial12Bold, name, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isTroop)
            {
                Troop template = ResourceManager.GetTroopTemplate(TroopType);
                template.Draw(batch, r);
                batch.DrawString(Fonts.Arial12Bold, TroopType, tCursor, Color.White);
                pb.Draw(batch);
            }

            if (Rush)
            {
                Graphics.Font font = lowRes ? Fonts.Arial8Bold : Fonts.Arial10;
                batch.DrawString(font, "Continuous Rush", rushCursor, Color.IndianRed);
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

        public override string ToString() => $"QueueItem DisplayText={DisplayText}";

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
                Rush        = Rush,
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
            if (pgs != null)  qi.pgsVector = new Vector2(pgs.X, pgs.Y);
            return qi;
        }
    }
}
