using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI.ExpansionAI
{
    public struct PlanetRanker
    {
        public Planet Planet;
        public float Value;
        public bool CantColonize;
        public bool PoorPlanet;
        private readonly float DistanceMod;
        private readonly Empire Empire;

        public override string ToString()
        {
            return $"{Planet.Name} Value={Value} DistanceMod={DistanceMod}"; // EnemyStr={EnemyStrength}";
        }

        public PlanetRanker(Empire empire, Planet planet, bool canColonizeBarren, float longestDistance, Vector2 empireCenter)
        {
            Planet                = planet;
            Empire                = empire;
            DistanceMod           = 1;
            CantColonize          = false;
            PoorPlanet            = false;
            float rawValue        = planet.ColonyPotentialValue(empire);

            if (!planet.ParentSystem.IsOwnedBy(empire))
                DistanceMod= (planet.Center.Distance(empireCenter)/longestDistance * 10).Clamped(1,10);

            Value              = rawValue / DistanceMod;
            bool moralityBlock = IsColonizeBlockedByMorals(planet.ParentSystem, empire);
            CantColonize       = IsBadWorld(planet, canColonizeBarren) || moralityBlock;
        }

        public void EvaluatePoorness(float avgValue) => PoorPlanet = Value < avgValue;

        public bool CanColonize    => !CantColonize;
        //public bool NeedClaimFleet => EnemyStrength.Greater(0) || Planet.GetGroundStrengthOther(Empire).Greater(0);

        static bool IsBadWorld(Planet planet, bool canColonizeBarren)
            => planet.IsBarrenType && !canColonizeBarren && planet.SpecialCommodities == 0;

        private bool IsColonizeBlockedByMorals(SolarSystem s, Empire ownerEmpire)
        {
            if (s.OwnerList.Count == 0
                || s.IsOwnedBy(ownerEmpire)
                || ownerEmpire.isFaction
                || ownerEmpire.data?.DiplomaticPersonality == null)
            {
                return false;
            }

            bool atWar    = ownerEmpire.AllRelations.Any(war => war.Value.AtWar);
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