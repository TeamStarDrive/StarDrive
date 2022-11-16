using System.Collections.Generic;
using System.Linq;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        [StarData] public readonly Map<string, SpaceRoad> ProjectorHeatMap = new();


        public void AddSpaceRoadHeat(SolarSystem origin, SolarSystem destination)
        {
            if (origin == destination)
                return;

            string name = SpaceRoad.GetSpaceRoadName(origin, destination);
            if (ProjectorHeatMap.TryGetValue(name, out SpaceRoad spaceRoad))
            {
                spaceRoad.AddHeat();
            }
            else
            {
                int numProjectors = SpaceRoad.GetNeededNumProjectors(origin, destination, OwnerEmpire);
                if (numProjectors > 0)
                    ProjectorHeatMap.Add(name, new SpaceRoad(origin, destination, OwnerEmpire, numProjectors, name));
            }
        }

        public static bool InfluenceNodeExistsAt(Vector2 pos, Empire empire)
        {
            return empire.Universe.Influence.IsInInfluenceOf(empire, pos);
        }

        public bool NodeAlreadyExistsAt(Vector2 pos)
        {
            for (int gi = 0; gi < GoalsList.Count; gi++)
            {
                Goal g = GoalsList[gi];
                if (g is BuildConstructionShip && g.BuildPosition.InRadius(pos, 100))
                    return true;
            }

            return false;
        }

        void RunInfrastructurePlanner()
        {
            if (!OwnerEmpire.CanBuildPlatforms || OwnerEmpire.isPlayer && !OwnerEmpire.AutoBuild)
                return;

            if (OwnerEmpire.Universe.StarDate % 10 == 0)
                CoolDownRoads();

            float roadMaintenance = ProjectorHeatMap.Values.Sum(r => r.Maintenance);
            float availableRoadBudget = SSPBudget - roadMaintenance;

            if (availableRoadBudget > 0)
                CreateNewRoads(ref availableRoadBudget);

            ScrapSpaceRoads(ref availableRoadBudget);
        }

        void CoolDownRoads()
        {
            var ships = OwnerEmpire.OwnedShips;
            if (ships.Any(s => s.IsIdleFreighter))
            {
                foreach (SpaceRoad road in ProjectorHeatMap.Values)
                    road.CoolDown();
            }
        }

        void ScrapSpaceRoads(ref float availableBudget)
        {
            var sortedByHeat = ProjectorHeatMap.Values.Sorted(r => r.Heat);
            for (int i = 0; i < sortedByHeat.Length; i++)
            {
                SpaceRoad road = sortedByHeat[i];
                if (!road.IsValid() || availableBudget < 0)
                {
                    string name = road.Name;
                    availableBudget += road.Maintenance;
                    road.Scrap();
                    ProjectorHeatMap.Remove(name);
                }
            }


            /*
            for (int i = 0; i < toRemove.Count; i++)
            {
                SpaceRoad road = toRemove[i];
                OwnerEmpire.SpaceRoadsList.Remove(road);
                foreach (RoadNode node in road.RoadNodesList)
                {
                    if (node.Platform != null && node.Platform.Active)
                    {
                        node.Platform.Die(null, true);
                        continue;
                    }

                    for (int x = 0; x < GoalsList.Count; x++)
                    {
                        Goal g = GoalsList[x];
                        if (g.Type != GoalType.DeepSpaceConstruction || !g.BuildPosition.AlmostEqual(node.Position))
                            continue;

                        RemoveGoal(g);
                        IReadOnlyList<Planet> ps = OwnerEmpire.GetPlanets();
                        for (int pi = 0; pi < ps.Count; pi++)
                        { 
                            if (ps[pi].Construction.Cancel(g))
                                break;
                        }

                        var ships = OwnerEmpire.OwnedShips;
                        for (int si = 0; si < ships.Count; si++)
                        {
                            Ship ship = ships[si];
                            ShipAI.ShipGoal goal = ship.AI.OrderQueue.PeekLast;
                            if (goal?.Goal != null &&
                                goal.Goal.Type == GoalType.DeepSpaceConstruction &&
                                goal.Goal.BuildPosition == node.Position)
                            {
                                ship.AI.OrderScrapShip();
                                break;
                            }
                        }
                    }
                }

                OwnerEmpire.SpaceRoadsList.Remove(road);
            }*/
        }

        void CreateNewRoads(ref float roadBudget)
        {
            var sortedByHeat = ProjectorHeatMap.Values.SortedDescending(r => r.Heat);
            for (int i = 0; i < sortedByHeat.Length; i++)
            {
                SpaceRoad road = sortedByHeat[i];
                switch (road.Status)
                {
                    case SpaceRoad.SpaceRoadStatus.Down when road.Hot:
                        if (road.Maintenance < roadBudget)
                        {
                            road.DeployAllProjectors();
                            roadBudget -= road.Maintenance;
                        }
                        break;
                    case SpaceRoad.SpaceRoadStatus.InProgress: 
                        road.FillGaps();
                        break;
                }
            }
        }
    }
}