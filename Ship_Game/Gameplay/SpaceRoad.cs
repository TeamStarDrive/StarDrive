using System;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class RoadNode
    {
        [StarData] public Vector2 Position;
        [StarData] public Ship Platform;
    }

    [StarDataType]
    public sealed class SpaceRoad
    {
        [StarData] readonly Empire Owner;
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

        public bool Hot => Heat > NumProjectors;
        public bool Frozen => Heat < -NumProjectors && NumProjectors > 2;

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

                var node = new RoadNode
                {
                    Position = sys1.Position + roadDirection * nodeOffset
                };

                //if (i > 0 && i < NumProjectors && !EmpireAI.InfluenceNodeExistsAt(node.Position, owner))
                RoadNodesList.Add(node);
            }

            UpdateMaintenance();
            AddHeat();
        }

        public void AddHeat()
        {
            Heat += 1f;
        }

        public void CoolDown()
        {
            Heat -= 1f;
        }

        void UpdateMaintenance()
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
            string[] names = { sys1.Name, sys2.Name };
            names.Sort(s => s);
            return $"{names[0]}-{names[1]}";
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
                if (node.Platform?.Active != true && !Owner.AI.NodeAlreadyExistsAt(node.Position))
                {
                    Log.Info($"BuildProjector - {Owner.Name} - fill gap at {node.Position}");
                    Owner.AI.AddGoal(new BuildConstructionShip(node.Position, "Subspace Projector", Owner));
                }
            }
        }

        public void Scrap()
        {

        }

        public bool IsValid()
        {
            return false;
        }

        public enum SpaceRoadStatus
        {
            Down,
            InProgress,
            Online,
        }

    }
}