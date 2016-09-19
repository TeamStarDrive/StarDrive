using Microsoft.Xna.Framework;
using Ship_Game;
using System;
using System.Collections.Generic;

namespace Ship_Game.Gameplay
{//subspaceprojector
	public sealed class SpaceRoad
	{
		public List<RoadNode> RoadNodesList = new List<RoadNode>();

		private SolarSystem Origin;

		private SolarSystem Destination;

		public int NumberOfProjectors;

		public SpaceRoad()
		{
		}

        public SpaceRoad(SolarSystem Origin, SolarSystem Destination, Empire empire, float SSPBudget, float nodeMaintenance)
		{
			this.Origin = Origin;
			this.Destination = Destination;
			float Distance = Vector2.Distance(Origin.Position, Destination.Position);

            
            float offset = (Empire.ProjectorRadius * 1.75f);//fbedard: increased from 1.5f
            offset = offset == 0 ? 1 : offset;
            this.NumberOfProjectors = (int)(Math.Ceiling(Distance / offset));
            if (SSPBudget - nodeMaintenance * this.NumberOfProjectors <= 0)
            {
                this.NumberOfProjectors = 0;
                return ;
            }
			for (int i = 0; i < this.NumberOfProjectors; i++)
			{
				RoadNode node = new RoadNode();
				float angle = HelperFunctions.findAngleToTarget(Origin.Position, Destination.Position);
                node.Position = HelperFunctions.GeneratePointOnCircle(angle, Origin.Position,  (float)i * (float)( Distance / this.NumberOfProjectors));
				bool reallyAdd = true;
                empire.BorderNodeLocker.EnterReadLock();
				{
					foreach (Empire.InfluenceNode bordernode in empire.BorderNodes)
					{
						if (Vector2.Distance(node.Position, bordernode.Position) >= bordernode.Radius*.75f)
						{
							continue;
						}
						reallyAdd = false;
					}
				}
                empire.BorderNodeLocker.ExitReadLock();
				if (reallyAdd)
				{
					this.RoadNodesList.Add(node);
				}
			}
            return ;
		}

		public SolarSystem GetDestination()
		{
			return this.Destination;
		}

		public SolarSystem GetOrigin
            ()
		{
			return this.Origin;
		}

		public void SetDestination(SolarSystem s)
		{
			this.Destination = s;
		}

		public void SetOrigin(SolarSystem s)
		{
			this.Origin = s;
		}
	}
}