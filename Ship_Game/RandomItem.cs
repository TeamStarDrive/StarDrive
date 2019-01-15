using System;

namespace Ship_Game
{
	public sealed class RandomItem
	{
		public bool HardCoreOnly;
		public string BuildingID;
		public float TerranChance;
		public float TerranInstanceMax;
		public float OceanicChance;
		public float OceanicInstanceMax;
		public float DesertChance;
		public float DesertInstanceMax;
		public float SwampChance;
		public float SwampInstanceMax;
		public float TundraChance;
		public float TundraInstanceMax;
		public float BarrenChance;
		public float BarrenInstanceMax;
		public float IceChance;
		public float IceInstanceMax;
		public float SteppeChance;
		public float SteppeInstanceMax;
		public float GasChance;
		public float GasInstanceMax;
		public float VolcanicChance;
		public float VolcanicInstanceMax;

        public (float,float) ChanceAndMaxInstance(PlanetCategory category)
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