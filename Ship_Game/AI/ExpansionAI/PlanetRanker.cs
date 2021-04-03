using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.ExpansionAI
{
    public struct PlanetRanker
    {
        public Planet Planet;
        public float Value;
        public bool CanColonize;
        public bool PoorPlanet;
        private readonly float DistanceMod;
        private readonly float EnemyStrMod;

        public override string ToString()
        {
            return $"{Planet.Name} Value={Value} DistanceMod={DistanceMod} EnemyStrMod={EnemyStrMod}";
        }

        public static bool IsGoodValueForUs(Planet p, Empire us)
        {
            if (IsColonizeBlockedByMorals(p.ParentSystem, us))
                return false;

            float value = p.ColonyPotentialValue(us);
            return value > 20 || us.IsCybernetic && p.MineralRichness > 0.9f;
        }

        public PlanetRanker(Empire empire, Planet planet, float longestDistance, Vector2 empireCenter)
        {
            Planet                = planet;
            DistanceMod           = 1;
            EnemyStrMod           = 1;
            CanColonize           = true;
            PoorPlanet            = false;
            float rawValue        = planet.ColonyPotentialValue(empire);

            if (!Planet.ParentSystem.HasPlanetsOwnedBy(empire))
            {
                DistanceMod = (planet.Center.Distance(empireCenter) / longestDistance * 10).Clamped(1, 10);
                EnemyStrMod = (empire.KnownEnemyStrengthIn(planet.ParentSystem) / empire.OffensiveStrength * 10).Clamped(1, 10);
            }

            Value              = rawValue / DistanceMod / EnemyStrMod;
            bool moralityBlock = IsColonizeBlockedByMorals(Planet.ParentSystem, empire);

            // We can colonize if we are not morally blocked and value is good
            CanColonize =  !moralityBlock && (rawValue > 30 || empire.IsCybernetic && planet.MineralRichness > 1.5f);
        }

        public static bool IsColonizeBlockedByMorals(SolarSystem s, Empire ownerEmpire)
        {
            if (s.OwnerList.Count == 0
                || s.HasPlanetsOwnedBy(ownerEmpire)
                || ownerEmpire.isFaction)
            {
                return false;
            }

            if (s.OwnerList.Any(e => s.HasPlanetsOwnedBy(e)
                                     && (ownerEmpire.IsOpenBordersTreaty(e) || ownerEmpire.IsAtWarWith(e))))
            {
                return false;
            }

            return s.OwnerList.Count > 0;
        }
    }
}