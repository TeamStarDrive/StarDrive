﻿using System.Linq;
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

        public PlanetRanker(Empire empire, Planet planet, float longestDistance, Vector2 empireCenter)
        {
            Planet                = planet;
            DistanceMod           = 1;
            EnemyStrMod           = 1;
            CanColonize           = true;
            PoorPlanet            = false;
            float rawValue        = planet.ColonyPotentialValue(empire);

            if (!Planet.ParentSystem.IsOwnedBy(empire))
            {
                DistanceMod = (planet.Center.Distance(empireCenter) / longestDistance * 10).Clamped(1, 10);
                EnemyStrMod = (empire.KnownEnemyStrengthIn(planet.ParentSystem) / empire.OffensiveStrength * 10).Clamped(1, 10);
            }

            Value              = rawValue / DistanceMod / EnemyStrMod;
            bool moralityBlock = IsColonizeBlockedByMorals(Planet.ParentSystem, empire);

            // We can colonize if we are not morally blocked and any planet better than 10
            CanColonize =  !moralityBlock && (rawValue > 20 || empire.IsCybernetic && planet.MineralRichness > 0.9);
        }

        private bool IsColonizeBlockedByMorals(SolarSystem s, Empire ownerEmpire)
        {
            if (s.OwnerList.Count == 0
                || s.IsOnlyOwnedBy(ownerEmpire)
                || ownerEmpire.isFaction
                || ownerEmpire.data?.DiplomaticPersonality == null)
            {
                return false;
            }

            bool atWar    = ownerEmpire.AllRelations.Any(war => war.Rel.AtWar);
            bool trusting = ownerEmpire.data.DiplomaticPersonality.IsTrusting;
            bool careless = ownerEmpire.data.DiplomaticPersonality.Careless;

            if (atWar && careless) 
                return false;

            foreach (Empire enemy in s.OwnerList)
                if (ownerEmpire.IsEmpireAttackable(enemy) && !trusting)
                    return false;

            return true;
        }
    }
}