﻿using System.Linq;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI.ExpansionAI
{
    [StarDataType]
    public struct PlanetRanker
    {
        [StarData] public Planet Planet;
        [StarData] public float Value;
        [StarData] public bool CanColonize;
        [StarData] public bool PoorPlanet;
        [StarData] readonly float DistanceMod;
        [StarData] readonly float EnemyStrMod;

        public override string ToString()
        {
            return $"{Planet.Name} Value={Value} DistanceMod={DistanceMod} EnemyStrMod={EnemyStrMod}";
        }

        public static bool IsGoodValueForUs(Planet p, Empire us)
        {
            if (IsColonizeBlockedByMorals(p.System, us))
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

            bool moralityBlock = IsColonizeBlockedByMorals(Planet.System, empire);
            CanColonize = !moralityBlock;
            Value = rawValue;
            if (!Planet.System.HasPlanetsOwnedBy(empire))
            {
                DistanceMod = (planet.Position.Distance(empireCenter) / longestDistance * 10).Clamped(1, 10);
                EnemyStrMod = ((empire.KnownEnemyStrengthIn(planet.System) / (empire.OffensiveStrength+10)) * 10).Clamped(1, 10);
                CanColonize = !moralityBlock && (rawValue > 20 || empire.IsCybernetic && planet.MineralRichness > 1.5f);
                Value = rawValue / DistanceMod / EnemyStrMod;
            }
        }

        public static bool IsColonizeBlockedByMorals(SolarSystem s, Empire ownerEmpire)
        {
            if (s.OwnerList.Count == 0
                || s.HasPlanetsOwnedBy(ownerEmpire)
                || ownerEmpire.IsFaction)
            {
                return false;
            }

            if (s.OwnerList.Any(e => s.HasPlanetsOwnedBy(e)
                                     && (ownerEmpire.WarnedThemAboutThisSystem(s, e)
                                         || ownerEmpire.IsOpenBordersTreaty(e) 
                                         || ownerEmpire.IsAtWarWith(e))))
            {
                return false;
            }

            return s.OwnerList.Count > 0;
        }
    }
}