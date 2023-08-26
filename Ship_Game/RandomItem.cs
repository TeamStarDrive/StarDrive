using System;

namespace Ship_Game
{
	public sealed class RandomItem
	{
		public bool HardCoreOnly;
		public string BuildingID;
		public float TerranChance;
        public int TerranInstanceMin;
        public int TerranInstanceMax;
		public float OceanicChance;
		public int OceanicInstanceMin;
        public int OceanicInstanceMax;
		public float DesertChance;
        public int DesertInstanceMin;
        public int DesertInstanceMax;
		public float SwampChance;
        public int SwampInstanceMin;
        public int SwampInstanceMax;
		public float TundraChance;
        public int TundraInstanceMin;
        public int TundraInstanceMax;
		public float BarrenChance;
        public int BarrenInstanceMin;
        public int BarrenInstanceMax;
		public float IceChance;
        public int IceInstanceMin;
        public int IceInstanceMax;
		public float SteppeChance;
        public int SteppeInstanceMin;
        public int SteppeInstanceMax;
		public float GasChance;
        public int GasInstanceMin;
        public int GasInstanceMax;
		public float VolcanicChance;
        public int VolcanicInstanceMin;
        public int VolcanicInstanceMax;

        public (float,int, int) ChanceMinMaxInstance(PlanetCategory category)
        {
            switch (category)
            {
                default:
                case PlanetCategory.Terran:   return (TerranChance,   TerranInstanceMin,   TerranInstanceMax);
                case PlanetCategory.Barren:   return (BarrenChance,   BarrenInstanceMin,   BarrenInstanceMax);
                case PlanetCategory.Desert:   return (DesertChance,   DesertInstanceMin,   DesertInstanceMax);
                case PlanetCategory.Steppe:   return (SteppeChance,   SteppeInstanceMin,   SteppeInstanceMax);
                case PlanetCategory.Tundra:   return (TundraChance,   TundraInstanceMin,   TundraInstanceMax);
                case PlanetCategory.Volcanic: return (VolcanicChance, VolcanicInstanceMin, VolcanicInstanceMax);
                case PlanetCategory.Ice:      return (IceChance,      IceInstanceMin,      IceInstanceMax);
                case PlanetCategory.Swamp:    return (SwampChance,    SwampInstanceMin,    SwampInstanceMax);
                case PlanetCategory.Oceanic:  return (OceanicChance,  OceanicInstanceMin,  OceanicInstanceMax);
                case PlanetCategory.GasGiant: return (GasChance,      GasInstanceMin,      GasInstanceMax);
            }
        }
	}
}