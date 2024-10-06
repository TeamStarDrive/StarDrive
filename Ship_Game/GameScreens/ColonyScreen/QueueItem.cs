using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Commands.Goals;

namespace Ship_Game
{
    public delegate void QueueItemCompleted(bool success);

    [StarDataType]
    public class QueueItem
    {
        [StarData] public Planet Planet;
        [StarData] public bool isBuilding;
        [StarData] public bool IsMilitary; // Military building
        [StarData] public bool IsTerraformer; 
        [StarData] public bool isShip;
        [StarData] public bool isOrbital;
        [StarData] public bool isTroop;
        [StarData] public IShipDesign ShipData;
        [StarData] public Building Building;
        [StarData] public string TroopType;
        [StarData] public Array<int> TradeRoutes = new();
        [StarData] public Array<Rectangle> AreaOfOperation = new();
        [StarData] public PlanetGridSquare pgs;
        [StarData] public string DisplayName;
        [StarData] public float Cost;
        [StarData] public float ProductionSpent;
        [StarData] public Goal Goal;
        [StarData] public int Priority;
        [StarData] public QueueItemType QType;
        [StarData] public float PriorityBonus { get; private set; } // Gets bigger as the queue is prioritized
        [StarData] public bool Rush;
        [StarData] public bool NotifyOnEmpty = true;
        [StarData] public bool IsPlayerAdded = false;
        [StarData] public bool TransportingColonists  = true;
        [StarData] public bool TransportingFood       = true;
        [StarData] public bool TransportingProduction = true;
        [StarData] public bool AllowInterEmpireTrade  = true;

        public bool IsCivilianBuilding => isBuilding && !IsMilitary;
        public Rectangle rect;
        public Rectangle removeRect;

        // production still needed until this item is finished
        public float ProductionNeeded => ActualCost - ProductionSpent;

        // is this item finished constructing?
        public bool IsComplete => ProductionSpent.GreaterOrEqual(ActualCost); // float imprecision

        // if TRUE, this QueueItem will be cancelled during next production queue update
        public bool IsCancelled;

        public QueueItem() { }

        public QueueItem(Planet planet)
        {
            Planet = planet;
        }

        public void SetCanceled(bool state = true) => IsCancelled = state;

        public void DrawAt(UniverseState us, SpriteBatch batch, Vector2 at, bool lowRes)
        {
            var r = new Rectangle((int)at.X, (int)at.Y, 29, 30);
            var tCursor = new Vector2(at.X + 40f, at.Y);
            var pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y + Fonts.Arial12Bold.LineSpacing + 4, 150, 18);
            var pb = new ProgressBar(pbRect, ActualCost, ProductionSpent);
            Graphics.Font font = lowRes ? Fonts.Arial8Bold : Fonts.Arial10;

            if (isBuilding)
            {
                batch.Draw(Building.IconTex, r);
                batch.DrawString(Fonts.Arial12Bold, Building.TranslatedName, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isShip)
            {
                batch.Draw(ShipData.Icon, r);
                string name = DisplayName.IsEmpty() ? ShipData.Name : DisplayName;
                if (Goal is FleetGoal fg && fg.Fleet != null)
                    name = $"{name} ({fg.Fleet.Name})";

                batch.DrawString(Fonts.Arial12Bold, name, tCursor, Color.White);
                pb.Draw(batch);
            }
            else if (isTroop)
            {
                Troop template = ResourceManager.GetTroopTemplate(TroopType);
                template.Draw(us, batch, r);
                batch.DrawString(Fonts.Arial12Bold, TroopType, tCursor, Color.White);
                pb.Draw(batch);
            }

            if (Rush)
            {
                var rushCursor = new Vector2(at.X + 200f, at.Y + 22);
                batch.DrawString(font, "Continuous Rush", rushCursor, Color.IndianRed);
            }
        }

        public float ActualCost
        {
            get
            {
                float cost = Cost;
                if (isShip && !ShipData.IsSingleTroopShip)
                    cost *= Planet.ShipCostModifier; // single troop ships do not get shipyard bonus

                return (int)cost; // FB - int to avoid float issues in release which prevent items from being complete
            }
        }

        public string DisplayText
        {
            get
            {
                if (isBuilding)
                    return Building.TranslatedName.Text;
                if (isShip || isOrbital)
                    return DisplayName ?? ShipData.Name;
                if (isTroop)
                    return TroopType;
                return "";
            }
        }

        // This also increases the priority bonus of the item. So it will be bumped up in the list a little next time
        public float GetAndUpdatePriorityForAI(Planet planet,int totalFreighters)
        {
            float priority = 5000;
            Empire owner = planet.Owner;
            switch (QType)
            {
                case QueueItemType.OrbitalUrgent:
                case QueueItemType.ColonyShipClaim: priority = 0;                                                                               break;
                case QueueItemType.Building:        priority = planet.PrioritizeColonyBuilding(Building);                                       break;
                case QueueItemType.Troop:           priority = 0.2f + owner.AI.DefensiveCoordinator.TroopsToTroopsWantedRatio * 5;              break;
                case QueueItemType.Scout:           priority = owner.GetPlanets().Count * 0.02f;                                                break;
                case QueueItemType.Orbital:         priority = 1 + (owner.TotalOrbitalMaintenance / owner.AI.DefenseBudget.LowerBound(1) * 10); break;
                case QueueItemType.RoadNode:        priority = 0.5f + owner.AI.SpaceRoadsManager.NumOnlineSpaceRoads * 0.1f;                    break;
                case QueueItemType.ColonyShip: 
                    priority = (owner.GetPlanets().Count * (owner.IsExpansionists ? 0.005f : 0.01f));
                    if (Goal != null && !Goal.TargetPlanet.System.HasPlanetsOwnedBy(owner))
                        priority -= 0.5f;
                    
                    break;
                case QueueItemType.Freighter: 
                    priority =  totalFreighters < owner.GetPlanets().Count*1.5f ? 0 : 0.5f * totalFreighters / owner.FreighterCap;
                    break;
                case QueueItemType.CombatShip:      
                    priority = (owner.TotalWarShipMaintenance / owner.AI.BuildCapacity.LowerBound(1));
                    if (owner.IsMilitarists)
                        priority *= 0.5f;
                    break;
            }

            if (owner.IsAtWarWithMajorEmpire)
            {
                switch (QType)
                {
                    case QueueItemType.Troop:
                    case QueueItemType.CombatShip: priority *= 0.25f; PriorityBonus += 0.2f;  break;
                    case QueueItemType.Orbital:    priority *= 0.66f; break;
                }
            }

            switch (QType)
            {
                case QueueItemType.Scout:
                case QueueItemType.ColonyShip: PriorityBonus += 0.1f;  break;
                case QueueItemType.Building:   PriorityBonus += 0.15f; break;
                case QueueItemType.Freighter:  PriorityBonus += 0.2f;  break;
                default:                       PriorityBonus += 0.05f; break;
            }

            if (DisplayText.Contains("Subspace Projector") || Rush)
                PriorityBonus += 1f;

            PriorityBonus += ProductionSpent / Cost.LowerBound(1);
            return (priority - PriorityBonus);
        }

        public override string ToString() => $"QueueItem DisplayText={DisplayText}";
    }

    public enum QueueItemType
    {
        ColonyShip,
        ColonyShipClaim, // change to ColonyShipPriority when spinning a savegame version
        Freighter,
        Scout,
        Troop,
        CombatShip,
        Building,
        Orbital,
        OrbitalUrgent,
        RoadNode,
        SwarmController
    }

}
