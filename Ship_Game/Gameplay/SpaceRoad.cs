using System;
using SDUtils;
using Ship_Game.AI;
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
        [StarData] public Array<RoadNode> RoadNodesList = new();
        [StarData] public SolarSystem Origin;
        [StarData] public SolarSystem Destination;
        [StarData] public readonly int NumberOfProjectors;

        public SpaceRoad()
        {
        }

        public SpaceRoad(SolarSystem origin, SolarSystem destination, Empire empire, float roadBudget, float nodeMaintenance)
        {
            Origin = origin;
            Destination = destination;

            float projectorRadius = empire.GetProjectorRadius() * 1.75f;
            float distance = origin.Position.Distance(destination.Position);
            NumberOfProjectors = 1 + (int)(Math.Ceiling(distance / projectorRadius));

            // can we afford the whole road?
            if (roadBudget - (nodeMaintenance * NumberOfProjectors) <= 0f)
            {
                NumberOfProjectors = 0;
                return;
            }

            float projectorSpacing = distance / NumberOfProjectors;
            float baseOffset = projectorSpacing * 0.5f;

            for (int i = 0; i < NumberOfProjectors; i++)
            {
                float nodeOffset = baseOffset + projectorSpacing*i;
                Vector2 roadDirection = origin.Position.DirectionToTarget(destination.Position);

                var node = new RoadNode
                {
                    Position = origin.Position + roadDirection * nodeOffset
                };

                if (!EmpireAI.InfluenceNodeExistsAt(node.Position, empire))
                    RoadNodesList.Add(node);
            }
        }
    }
}