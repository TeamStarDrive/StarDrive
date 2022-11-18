using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class RoadNode
    {
        [StarData] public readonly Vector2 Position;
        [StarData] public Ship Projector { get; private set; }
        public bool ProjectorExists => Projector is { Active: true };

        public RoadNode(Vector2 pos)
        {
            Position = pos;
        }
        public void SetProjector(Ship projector)
        {
            Projector = projector;
        }
    }

    [StarDataType]
    public sealed class SpaceRoad
    {
        [StarData] public Empire Owner { get; private set; }
        [StarData] public Array<RoadNode> RoadNodesList = new();
        [StarData] public SolarSystem System1;
        [StarData] public SolarSystem System2;
        [StarData] public readonly int NumProjectors;
        [StarData] public readonly string Name;
        [StarData] public SpaceRoadStatus Status { get; private set; }
        [StarData] public float Heat { get; private set; }
        [StarData] public float Maintenance { get; private set; }

        const float ProjectorDensity = 1.75f;
        const float SpacingOffset = 0.5f;

        public bool IsHot => Heat > NumProjectors;
        public bool IsCold => Heat < -NumProjectors;

        public SpaceRoad()
        {
        }

        public SpaceRoad(SolarSystem sys1, SolarSystem sys2, Empire owner, int numProjectors, string name)
        {
            System1 = sys1;
            System2 = sys2;
            NumProjectors = numProjectors;
            Status = SpaceRoadStatus.Down;
            Owner = owner;
            Name = name;

            float distance = sys1.Position.Distance(sys2.Position);
            float projectorSpacing = distance / NumProjectors;
            float baseOffset = projectorSpacing * SpacingOffset;

            for (int i = 0; i <= NumProjectors; i++)
            {
                float nodeOffset = baseOffset + projectorSpacing * i;
                Vector2 roadDirection = sys1.Position.DirectionToTarget(sys2.Position);
                var node = new RoadNode(sys1.Position + roadDirection * nodeOffset);
                RoadNodesList.Add(node);
            }

            UpdateMaintenance();
            AddHeat();
        }

        public void AddHeat()
        {
            Heat = (Heat +1).UpperBound(NumProjectors * 2);
        }

        public void CoolDown()
        {
            Heat = (Heat - 1).LowerBound(NumProjectors * 2);
        }

        public void UpdateMaintenance()
        {
            Maintenance = ResourceManager.GetShipTemplate("Subspace Projector").GetMaintCost(Owner) * NumProjectors;
        }
        public static int GetNeededNumProjectors(SolarSystem origin, SolarSystem destination, Empire owner)
        {
            float projectorRadius = owner.GetProjectorRadius() * ProjectorDensity;
            float distance = origin.Position.Distance(destination.Position);
            return (int)(Math.Ceiling(distance / projectorRadius));
        }

        public static string GetSpaceRoadName(SolarSystem sys1, SolarSystem sys2)
        {
            return sys1.Id < sys2.Id ? $"{sys1.Id}-{sys2.Id}" : $"{sys2.Id}-{sys1.Id}";
        }

        public void DeployAllProjectors()
        {
            Status = SpaceRoadStatus.InProgress;
            foreach (RoadNode node in RoadNodesList)
            {
                Log.Info($"BuildProjector - {Owner.Name} - at {node.Position}");
                Owner.AI.AddGoal(new BuildConstructionShip(node.Position, "Subspace Projector", Owner));
            }
        }

        public void FillGaps()
        {
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (!node.ProjectorExists && !Owner.AI.NodeAlreadyExistsAt(node.Position))
                {
                    Log.Info($"BuildProjector - {Owner.Name} - fill gap at {node.Position}");
                    Owner.AI.AddGoal(new BuildConstructionShip(node.Position, "Subspace Projector", Owner));
                }
            }
        }

        public void Scrap(Array<Goal> goalsList)
        {
            foreach (RoadNode node in RoadNodesList)
            {
                if (node.ProjectorExists)
                    node.Projector.AI.OrderScuttleShip();
                else
                    RemoveProjectorGoal(goalsList, node.Position);
            }
        }

        void RemoveProjectorGoal(Array<Goal>  goalsList, Vector2 nodePos)
        {
            for (int i = goalsList.Count - 1; i >= 0; i--)
            {
                Goal g = goalsList[i];
                if (g.Type == GoalType.DeepSpaceConstruction && g.BuildPosition.AlmostEqual(nodePos))
                {
                    g.PlanetBuildingAt.Construction.Cancel(g);
                    g.FinishedShip?.AI.OrderScrapShip();
                    Owner.AI.RemoveGoal(g);
                    break;
                }
            }
        }

        public void RecalculateStatus()
        {
            Status = RoadNodesList.Any(n => !n.ProjectorExists) ? SpaceRoadStatus.InProgress : SpaceRoadStatus.Online;
        }

        public void SetProjectorInNode(RoadNode node, Ship projector)
        {
            node.SetProjector(projector);
            RecalculateStatus();
        }

        public void TransferOwnerShipTo(Empire empire)
        {
            Owner = empire;
            UpdateMaintenance();
        }

        public enum SpaceRoadStatus
        {
            Down,
            InProgress,
            Online,
        }

    }
}