using Ship_Game.Gameplay;

namespace Ship_Game
{
    public partial class Empire
    {
        public PersonalityType Personality => data.DiplomaticPersonality.TraitName;

        public bool IsCunning    => Personality == PersonalityType.Cunning;
        public bool IsXenophobic => Personality == PersonalityType.Xenophobic;
        public bool IsRuthless   => Personality == PersonalityType.Ruthless;
        public bool IsAggressive => Personality == PersonalityType.Aggressive;
        public bool IsHonorable  => Personality == PersonalityType.Honorable;
        public bool IsPacifist   => Personality == PersonalityType.Pacifist;

        void SetTreatyWith(Empire them, TreatyType type, bool value)
        {
            if (TryGetRelations(them, out Relationship usToThem))
            {
                usToThem.SetTreaty(this, type, value);
                if (them.TryGetRelations(this, out Relationship themToUs))
                    themToUs.SetTreaty(them, type, value);
            }
        }

        public void SignTreatyWith(Empire them, TreatyType type)
        {
            SetTreatyWith(them, type, true);
        }

        public void BreakTreatyWith(Empire them, TreatyType type)
        {
            SetTreatyWith(them, type, false);
        }

        public void BreakAllTreatiesWith(Empire them, bool includingPeace = false)
        {
            BreakTreatyWith(them, TreatyType.Alliance);
            BreakTreatyWith(them, TreatyType.OpenBorders);
            BreakTreatyWith(them, TreatyType.NonAggression);
            BreakTreatyWith(them, TreatyType.Trade);

            if (includingPeace)
                BreakTreatyWith(them, TreatyType.Peace);
        }

        public void BreakAllianceWith(Empire them)
        {
            BreakTreatyWith(them, TreatyType.Alliance);
            BreakTreatyWith(them, TreatyType.OpenBorders);
            BreakTreatyWith(them, TreatyType.NonAggression);
        }

        public void SignAllianceWith(Empire them)
        {
            SignTreatyWith(them, TreatyType.Alliance);
            SignTreatyWith(them, TreatyType.OpenBorders);
            BreakTreatyWith(them, TreatyType.NonAggression);
        }

        public void EndPeaceWith(Empire them)
        {
            BreakTreatyWith(them, TreatyType.Peace);
        }

        public void LaunchTroopsAfterPeaceSigned(Empire them)
        {
            var theirPlanets = them.GetPlanets();
            foreach (Planet planet in theirPlanets)
                planet.ForceLaunchInvadingTroops(this);
        }
    }
}
