using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class SpaceRoadsManager
    {
        // Fat Bastard
        /* This will maintain the space roads (Subspace Projector lanes) the empire needs
           When freighters set up a trade plan, a space road will be created between the the 2 planets.
           The more freighter follow that lane, the more heat this lane will get. If the heat is higher 
           then a certain threshold and there is budget, all the related projectors will be deployed.
           Roads also cool down if not used for quite some time and after a certain threshold, the road will
           be scrapped.
        */

        [StarData] public readonly Array<SpaceRoad> SpaceRoads = new();
        [StarData] public readonly Empire Owner;

        [StarDataConstructor]
        SpaceRoadsManager() {}

        public SpaceRoadsManager(Empire owner)
        {
            Owner= owner;
        }

        // Adds heat to an existing road or set ups a new road is it does not exist
        public void AddSpaceRoadHeat(SolarSystem origin, SolarSystem destination, float extraHeat)
        {
            if (origin == destination)
                return;

            string name = SpaceRoad.GetSpaceRoadName(origin, destination);
            SpaceRoad existingRoad = SpaceRoads.Find(r => r.Name == name);
            if (existingRoad != null)
            {
                existingRoad.AddHeat(extraHeat);
            }
            else
            {
                int numProjectors = SpaceRoad.GetNeededNumProjectors(origin, destination, Owner);
                if (numProjectors > 0)
                    SpaceRoads.Add(new SpaceRoad(origin, destination, Owner, numProjectors, name));
            }
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            for (int i = 0; i < Owner.AI.Goals.Count; i++)
            {
                Goal g = Owner.AI.Goals[i];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 1000))
                    return true;
            }

            return false;
        }

        public bool InfluenceNodeExistsAt(Vector2 pos)
        {
            return Owner.Universe.Influence.IsInInfluenceOf(Owner, pos);
        }

        public void Update()
        {
            if (!Owner.CanBuildPlatforms || Owner.isPlayer && !Owner.AutoBuildSpaceRoads)
                return;

            if (Owner.Universe.StarDate % 1 == 0)
                CoolDownRoads();

            float roadMaintenance = SpaceRoads.Sum(r => r.Maintenance);
            float availableRoadBudget = Owner.AI.SSPBudget - roadMaintenance;
            if (!TryScrapSpaceRoad(lowBudgetMustScrap: availableRoadBudget  < 0))
                CreateNewRoad(availableRoadBudget);
        }

        void CoolDownRoads()
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                SpaceRoads[i].CoolDown();
        }

        void RemoveRoad(SpaceRoad road)
        {
            road.Scrap(Owner.AI.Goals);
            SpaceRoads.Remove(road);
        }

        // Scrap one road per turn, if applicable
        bool TryScrapSpaceRoad(bool lowBudgetMustScrap)
        {
            SpaceRoad coldestRoad = lowBudgetMustScrap 
                    ? SpaceRoads.FindMin(r => r.Heat)
                    : SpaceRoads.FindMinFiltered(r => r.IsCold, r => r.Heat);

            if (coldestRoad != null && (coldestRoad.IsCold || lowBudgetMustScrap))
            {
                RemoveRoad(coldestRoad);
                return true;
            }

            return false;
        }

        // Dealing with one road till its completed
        void CreateNewRoad(float roadBudget)
        {
            if (roadBudget <= 0)
                return;

            // Look for the hottest road which is not yet online and deal with it
            SpaceRoad hottestRoad = SpaceRoads.FindMaxFiltered(r => r.Status != SpaceRoad.SpaceRoadStatus.Online, r => r.Heat);
            if (hottestRoad != null) 
            {
                switch (hottestRoad.Status)
                {
                    case SpaceRoad.SpaceRoadStatus.Down when hottestRoad.IsHot && hottestRoad.OperationalMaintenance < roadBudget:
                        hottestRoad.DeployAllProjectors();
                        return;
                    case SpaceRoad.SpaceRoadStatus.InProgress: // This is tagged as some projectors are missing
                        hottestRoad.FillGaps();
                        return;
                }
            }
        }

        public void AddProjectorToRoadList(Ship projector, Vector2 buildPos)
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                if (SpaceRoads[i].AddProjector(projector, buildPos))
                    return; // removed successfully
        }

        public void RemoveProjectorFromRoadList(Ship projector)
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
                if (SpaceRoads[i].RemoveProjector(projector))
                    return; // removed successfully
        }

        public void UpdateAllRoadsMaintenance()
        {
            for (int i = 0; i < SpaceRoads.Count; i++)
            {
                SpaceRoad road = SpaceRoads[i];
                road.UpdateMaintenance();
            }
        }

        public int NumOnlineSpaceRoads => SpaceRoads.Count(r => r.Status == SpaceRoad.SpaceRoadStatus.Online);

        public void AbsorbSpaceRoadOwnershipFrom(Empire them, Array<SpaceRoad> theirRoads)
        {
            for (int i = 0; i < theirRoads.Count; i++)
            {
                SpaceRoad theirSpaceRoad = theirRoads[i];
                SpaceRoad existingRoad = SpaceRoads.Find(r => r.Name == theirSpaceRoad.Name);
                if (existingRoad != null)
                {
                    theirSpaceRoad.Scrap(them.AI.Goals); // we have that road as well
                }
                else
                {
                    theirSpaceRoad.TransferOwnerShipTo(Owner);
                    SpaceRoads.Add(theirSpaceRoad);
                }
            }

            theirRoads.Clear();

            // Iterating all projectors since there might be projectors which are not in roads
            var projectors = them.OwnedProjectors;
            for (int i = projectors.Count - 1; i >= 0; i--)
            {
                Ship ship = projectors[i];
                ship.LoyaltyChangeByGift(Owner, addNotification: false);
            }
        }

        public void SetupProjectorBridgeIfNeeded(Ship ship)
        {
            if (ship.System == null
                || Owner.IsFaction
                || Owner.isPlayer && !Owner.AutoBuildSpaceRoads)
            {
                return;
            }

            if (!CheckBridgeNeededColonyShip())
                CheckBridgeNeededTrade();

            bool CheckBridgeNeededColonyShip()
            {
                if (ship.ShipData.IsColonyShip && ship.AI.State == AIState.Colonize
                                          && !Owner.AI.SpaceRoadsManager.InfluenceNodeExistsAt(ship.Position))
                {
                    // find where the ship was coming from and setup a projector bridge
                    Goal colonizationGoal = Owner.AI.FindGoal(g => g.FinishedShip == ship);
                    if (colonizationGoal?.PlanetBuildingAt != null)
                    {
                        Owner.AI.AddGoal(new ProjectorBridge(ship.System,
                            colonizationGoal.PlanetBuildingAt.ParentSystem.Position, Owner));

                        return true;
                    }
                }

                return false;
            }

            void CheckBridgeNeededTrade()
            {
                if (ship.IsFreighter && !Owner.AI.SpaceRoadsManager.InfluenceNodeExistsAt(ship.Position))
                {
                    // find where the ship was coming from and setup a projector bridge
                    if (ship.AI.State == AIState.SystemTrader)
                    {
                        ship.AI.FindGoal(ShipAI.Plan.Trade, out ShipAI.ShipGoal goal);
                        if (goal?.Trade != null)
                        {
                            Owner.AI.AddGoal(new ProjectorBridge(ship.System,
                                goal.Trade.ExportFrom.ParentSystem.Position, Owner));

                            return;
                        }
                    }
                    // fallback to ship position for bridge direction
                    Owner.AI.AddGoal(new ProjectorBridge(ship.System, Owner.WeightedCenter, Owner));
                }
            }
        }
    }
}