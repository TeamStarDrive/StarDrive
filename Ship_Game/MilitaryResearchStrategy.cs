namespace Ship_Game
{
    //Unused in game that i can see --gremlin
	public sealed class MilitaryResearchStrategy
	{
		public string Name;

		public Array<Tech> HullPath = new Array<Tech>();

		public Array<Tech> DefensePath = new Array<Tech>();

		public Array<Tech> KineticPath = new Array<Tech>();

		public Array<Tech> EnergyPath = new Array<Tech>();

		public Array<Tech> BeamPath = new Array<Tech>();

		public Array<Tech> MissilePath = new Array<Tech>();

	    public struct Tech
		{
			public string id;
		}
	}
}