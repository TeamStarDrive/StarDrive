using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class FleetDesign
	{
		public List<FleetDataNode> Data = new List<FleetDataNode>();

		public int FleetIconIndex;

		public string Name;

		public FleetDesign()
		{
		}

		public void Rotate(float facing)
		{
			foreach (FleetDataNode node in this.Data)
			{
				float angle = Math.Abs(Vector2.Zero.AngleToTarget(node.FleetOffset)) + MathHelper.ToDegrees(facing);
				angle = angle.ToRadians();
				float distance = node.FleetOffset.Length();
				node.FleetOffset = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, angle, distance);
			}
		}
	}
}