namespace Ship_Game
{
    public struct PersonalityModifiers
    {
        public float ColonizationClaimRatioWarningThreshold; // warn the player if we have mutual a colonization target based on vaule
        public float TrustCostNaPact;
        public float TrustCostTradePact;
        public float AddAngerAlliedWithEnemy;
        public float AddAngerAlliedWithEnemies3RdParty;
        public float AllianceValueAlliedWithEnemy;
        public float WantedAgentMissionMultiplier;
        public int TurnsAbove95FederationNeeded;
        public float FederationPopRatioWar;
        public float PlanetStoleTrustMultiplier;

        public PersonalityModifiers(PersonalityType type)
        {
            switch (type)
            {
                default:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 25;
                    TurnsAbove95FederationNeeded = 250;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.75f;
                    AddAngerAlliedWithEnemy      = 0;
                    FederationPopRatioWar = 1;
                    TrustCostNaPact       = 0;
                    TrustCostTradePact    = 0;
                    break;
                case PersonalityType.Aggressive:
                    ColonizationClaimRatioWarningThreshold = 0.7f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    TurnsAbove95FederationNeeded = 350;
                    AllianceValueAlliedWithEnemy = 0.4f;
                    WantedAgentMissionMultiplier = 0.115f;
                    PlanetStoleTrustMultiplier   = 0.5f;
                    AddAngerAlliedWithEnemy      = 50;
                    FederationPopRatioWar = 1.1f;
                    TrustCostNaPact       = 35;
                    TrustCostTradePact    = 20;
                    break;
                case PersonalityType.Ruthless:
                    ColonizationClaimRatioWarningThreshold = 0.6f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    TurnsAbove95FederationNeeded = 420;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.115f;
                    PlanetStoleTrustMultiplier   = 0.6f;
                    AddAngerAlliedWithEnemy      = 25;
                    FederationPopRatioWar = 1.2f;
                    TrustCostNaPact       = 45f;
                    TrustCostTradePact    = 15;
                    break;
                case PersonalityType.Xenophobic:
                    ColonizationClaimRatioWarningThreshold = 0;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    TurnsAbove95FederationNeeded = 600;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.1f;
                    AddAngerAlliedWithEnemy      = 100;
                    FederationPopRatioWar = 1.25f;
                    TrustCostNaPact       = 15;
                    TrustCostTradePact    = 15;
                    break;
                case PersonalityType.Cunning:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 50;
                    TurnsAbove95FederationNeeded = 320;
                    AllianceValueAlliedWithEnemy = 0.6f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.7f;
                    AddAngerAlliedWithEnemy      = 0;
                    FederationPopRatioWar = 0.5f;
                    TrustCostNaPact       = 5;
                    TrustCostTradePact    = 5;
                    break;
                case PersonalityType.Honorable:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    TurnsAbove95FederationNeeded = 250;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.4f;
                    AddAngerAlliedWithEnemy      = 75;
                    FederationPopRatioWar = 1;
                    TrustCostNaPact       = 10;
                    TrustCostTradePact    = 10;
                    break;
                case PersonalityType.Pacifist:
                    ColonizationClaimRatioWarningThreshold = 1.25f;
                    AddAngerAlliedWithEnemies3RdParty      = 25;
                    TurnsAbove95FederationNeeded = 300;
                    AllianceValueAlliedWithEnemy = 0.8f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.8f;
                    AddAngerAlliedWithEnemy      = 0;
                    FederationPopRatioWar = 0.8f;
                    TrustCostNaPact       = 3;
                    TrustCostTradePact    = 12;
                    break;
            }
        }
    }
}