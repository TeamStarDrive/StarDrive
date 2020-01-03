using System;

namespace Ship_Game.AI.ExpansionAI
{
    public struct PlanetRanker
    {
        public Planet Planet;
        public float RawValue;
        public float Value;
        public float Distance;
        public bool OutOfRange;
        public bool CantColonize;

        public override string ToString()
        {
            return $"{Planet.Name} Value={Value} Distance={Distance}";
        }

        public PlanetRanker(Empire empire, Planet planet, bool canColonizeBarren, AO closestAO, float enemyStr)
        {
            Planet             = planet;
            Distance           = planet.Center.Distance(closestAO.Center);
            OutOfRange         = planet.Center.OutsideRadius(closestAO.Center, closestAO.Radius);
            CantColonize       = planet.Owner != null || IsBadWorld(planet, canColonizeBarren);
            int rangeReduction = (int)Math.Ceiling(Distance / closestAO.Radius);
            RawValue           = planet.ColonyRawValue(empire);
            Value              = RawValue / rangeReduction;

            if (enemyStr > 0)
                Value *= (closestAO.OffensiveForcePoolStrength / enemyStr).Clamped(0.1f, 1f);
        }

        static bool IsBadWorld(Planet planet, bool canColonizeBarren)
            => planet.IsBarrenType && !canColonizeBarren && planet.SpecialCommodities == 0;
    }
}