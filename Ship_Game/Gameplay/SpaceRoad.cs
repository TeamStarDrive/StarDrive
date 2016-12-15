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
           
            float Distance = Vector2.Distance(Origin.Position, Destination.Position) ;

            
            float offset = (Empire.ProjectorRadius * 1.75f);//fbedard: increased from 1.5f
            offset = offset == 0 ? 1 : offset;
            this.NumberOfProjectors = (int)(Math.Ceiling(Distance / offset));
            offset = Distance / NumberOfProjectors;
            offset *= .5f;
            if (SSPBudget - nodeMaintenance * this.NumberOfProjectors <= 0)
            {
                this.NumberOfProjectors = 0;
                return ;
            }
			for (int i = 0; i < NumberOfProjectors; i++)
			{
				RoadNode node = new RoadNode();
				float angle = Origin.Position.AngleToTarget(Destination.Position);
                node.Position = Origin.Position.PointOnCircle(angle, offset + (i * (float)( Distance / this.NumberOfProjectors) ));
				bool reallyAdd = true;
                float extrad = Empire.ProjectorRadius;

                using (empire.BorderNodes.AcquireReadLock())
                foreach (Empire.InfluenceNode bordernode in empire.BorderNodes)
                {
                    extrad = !(bordernode.KeyedObject is Ship) ? Empire.ProjectorRadius : 0;
                  
                    if (Vector2.Distance(node.Position, bordernode.Position) + extrad >= bordernode.Radius)
                        continue;
                    reallyAdd = false;
                }

                if (reallyAdd)
					this.RoadNodesList.Add(node);
			}
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