using System;
using System.Linq;

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
        public float EnemyStrength;

        public override string ToString()
        {
            return $"{Planet.Name} Value={Value} Distance={Distance} EnemyStr={EnemyStrength}";
        }

        public PlanetRanker(Empire empire, Planet planet, bool canColonizeBarren, AO closestAO, float enemyStr)
        {
            Planet             = planet;
            Distance           = planet.Center.Distance(closestAO.Center).ClampMin(1);
            OutOfRange         = planet.Center.OutsideRadius(closestAO.Center, closestAO.Radius);
            CantColonize       = false;
            int rangeReduction = (int)Math.Ceiling(Distance / closestAO.Radius);
            RawValue           = planet.ColonyPotentialValue(empire);
            Value              = RawValue / rangeReduction;
            EnemyStrength      = enemyStr;
            if (enemyStr > 0)
                Value *= (closestAO.OffensiveForcePoolStrength / enemyStr).Clamped(0.1f, 1f);

            bool moralityBlock = IsColonizeBlockedByMorals(planet.ParentSystem, empire);
            CantColonize       = planet.Owner != null || IsBadWorld(planet, canColonizeBarren) || moralityBlock;
        }

        static bool IsBadWorld(Planet planet, bool canColonizeBarren)
            => planet.IsBarrenType && !canColonizeBarren && planet.SpecialCommodities == 0;
        private bool IsColonizeBlockedByMorals(SolarSystem s, Empire ownerEmpire)
        {
            if (s.OwnerList.Count == 0)
                return false;
            if (s.OwnerList.Contains(ownerEmpire))
                return false;
            if (ownerEmpire.isFaction)
                return false;
            if (ownerEmpire.data?.DiplomaticPersonality == null)
                return false;
            bool atWar = ownerEmpire.AllRelations.Any(war => war.Value.AtWar);
            bool trusting = ownerEmpire.data.DiplomaticPersonality.IsTrusting;
            bool careless = ownerEmpire.data.DiplomaticPersonality.Careless;

            if (atWar && careless) return false;

            foreach (Empire enemy in s.OwnerList)
                if (ownerEmpire.IsEmpireAttackable(enemy) && !trusting)
                    return false;

            return true;

        }
    }
}