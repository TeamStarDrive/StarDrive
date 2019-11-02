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

        public void DrawAt(SpriteBatch batch, Vector2 at)
        {
            var r = new Rectangle((int)at.X, (int)at.Y, 29, 30);
            var tCursor = new Vector2(at.X + 40f, at.Y);
            var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing, 150, 18);
            var pb = new ProgressBar(pbRect, Cost, ProductionSpent);

            if (isBuilding)
            {
                batch.Draw(Building.IconTex, r);
                batch.DrawString(Fonts.Arial12Bold, Building.Name, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isShip)
            {
                batch.Draw(sData.Icon, r);
                batch.DrawString(Fonts.Arial12Bold, sData.Name, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isTroop)
            {
                Troop template = ResourceManager.GetTroopTemplate(TroopType);
                template.Draw(batch, r);
                batch.DrawString(Fonts.Arial12Bold, TroopType, tCursor, Color.White);
                pb.Draw(batch);
            }

            //foreach (ScrollList.Entry entry in CQueue.VisibleExpandedEntries)
            //{
            //    entry.CheckHoverNoSound(Input.CursorPosition);

            //    var qi = entry.Get<QueueItem>();
            //    var position = new Vector2(entry.X + 40f, entry.Y);
            //    DrawText(ref position, qi.DisplayText);
            //    var r = new Rectangle((int)position.X, (int)position.Y, 150, 18);
            //    var progress = new ProgressBar(r, qi.Cost, qi.ProductionSpent);

            //    if (qi.isBuilding)
            //    {
            //        SubTexture icon = ResourceManager.Texture($"Buildings/icon_{qi.Building.Icon}_48x48");
            //        batch.Draw(icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
            //        progress.Draw(batch);
            //    }
            //    else if (qi.isShip)
            //    {
            //        batch.Draw(qi.sData.Icon, new Rectangle(entry.X, entry.Y, 29, 30), Color.White);
            //        progress.Draw(batch);
            //    }
            //    else if (qi.isTroop)
            //    {
            //        Troop template = ResourceManager.GetTroopTemplate(qi.TroopType);
            //        template.Draw(batch, new Rectangle(entry.X, entry.Y, 29, 30));
            //        progress.Draw(batch);
            //    }
            //}
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

        public override string ToString() => $"QueueItem DisplayText={DisplayText}";

        public SavedGame.QueueItemSave Serialize()
        {
            var qi = new SavedGame.QueueItemSave
            {
                isBuilding  = isBuilding,
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