using System;

namespace Ship_Game
{
	public sealed class RandomItem
	{
		public bool HardCoreOnly;
		public string BuildingID;
		public float TerranChance;
		public int TerranInstanceMax;
		public float OceanicChance;
		public int OceanicInstanceMax;
		public float DesertChance;
		public int DesertInstanceMax;
		public float SwampChance;
		public int SwampInstanceMax;
		public float TundraChance;
		public int TundraInstanceMax;
		public float BarrenChance;
		public int BarrenInstanceMax;
		public float IceChance;
		public int IceInstanceMax;
		public float SteppeChance;
		public int SteppeInstanceMax;
		public float GasChance;
		public int GasInstanceMax;
		public float VolcanicChance;
		public int VolcanicInstanceMax;

        public (float,int) ChanceAndMaxInstance(PlanetCategory category)
        {
            switch (category)
            {
                default:
                case PlanetCategory.Other:
                case PlanetCategory.Barren: return (BarrenChance, BarrenInstanceMax);
                case PlanetCategory.Desert: return (DesertChance, DesertInstanceMax);
                case PlanetCategory.Steppe: return (SteppeChance, SteppeInstanceMax);
                case PlanetCategory.Tundra: return (TundraChance, TundraInstanceMax);
                case PlanetCategory.Terran: return (TerranChance, TerranInstanceMax);
                case PlanetCategory.Volcanic: return (VolcanicChance, VolcanicInstanceMax);
                case PlanetCategory.Ice:      return (IceChance,      IceInstanceMax);
                case PlanetCategory.Swamp:    return (SwampChance,    SwampInstanceMax);
                case PlanetCategory.Oceanic:  return (OceanicChance,  OceanicInstanceMax);
                case PlanetCategory.GasGiant: return (GasChance,      GasInstanceMax);
            }
        }
	}
}