using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    public sealed class RoadNode
    {
        public Vector2 Position;
        public Ship Platform;
    }

	public sealed class SpaceRoad
	{
		public Array<RoadNode> RoadNodesList = new Array<RoadNode>();

        public SolarSystem Origin;
		public SolarSystem Destination;
		public readonly int NumberOfProjectors;

		public SpaceRoad()
		{
		}

        public SpaceRoad(SolarSystem origin, SolarSystem destination, Empire empire, float roadBudget, float nodeMaintenance)
		{
			Origin = origin;
			Destination = destination;

            float projectorRadius = empire.ProjectorRadius * 1.75f;
            float distance = origin.Position.Distance(destination.Position);
            NumberOfProjectors = (int)(Math.Ceiling(distance / projectorRadius));

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