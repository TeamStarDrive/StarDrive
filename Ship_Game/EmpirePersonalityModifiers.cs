using Ship_Game.AI.StrategyAI.WarGoals;

namespace Ship_Game
{
    public struct PersonalityModifiers
    {
        public float ColonizationClaimRatioWarningThreshold; // warn the player if we have mutual a colonization target based on value
        public float TrustCostNaPact;
        public float TrustCostTradePact;
        public float AddAngerAlliedWithEnemy;
        public float AddAngerAlliedWithEnemies3RdParty;
        public float AllianceValueAlliedWithEnemy;
        public float WantedAgentMissionMultiplier;
        public int TurnsAbove95FederationNeeded;
        public float FederationPopRatioWar;
        public float PlanetStoleTrustMultiplier;
        public float WarGradeThresholdForPeace; // How bad should our total wars grade be to request peace
        public readonly float FleetStrMultiplier; // Add or decrease str addition to fleets after win / lose vs. another empire.

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
                    WarGradeThresholdForPeace    = 0.5f * War.MaxWarGrade;
                    AddAngerAlliedWithEnemy      = 0;
                    FleetStrMultiplier    = 1;
                    FederationPopRatioWar = 1.5f;
                    TrustCostTradePact    = 0;
                    TrustCostNaPact       = 0;
                    break;
                case PersonalityType.Aggressive:
                    ColonizationClaimRatioWarningThreshold = 0.7f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    TurnsAbove95FederationNeeded = 350;
                    AllianceValueAlliedWithEnemy = 0.4f;
                    WantedAgentMissionMultiplier = 0.115f;
                    WarGradeThresholdForPeace    = 0.4f * War.MaxWarGrade;
                    PlanetStoleTrustMultiplier   = 0.5f;
                    AddAngerAlliedWithEnemy      = 50;
                    FleetStrMultiplier    = 1.4f;
                    FederationPopRatioWar = 1.25f;
                    TrustCostTradePact    = 20;
                    TrustCostNaPact       = 35;
                    break;
                case PersonalityType.Ruthless:
                    ColonizationClaimRatioWarningThreshold = 0.6f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    TurnsAbove95FederationNeeded = 420;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.115f;
                    WarGradeThresholdForPeace    = 0.4f * War.MaxWarGrade;
                    PlanetStoleTrustMultiplier   = 0.6f;
                    AddAngerAlliedWithEnemy      = 25;
                    FleetStrMultiplier    = 1.3f;
                    FederationPopRatioWar = 1.2f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 45f;
                    break;
                case PersonalityType.Xenophobic:
                    ColonizationClaimRatioWarningThreshold = 0;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    TurnsAbove95FederationNeeded = 600;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.1f;
                    WarGradeThresholdForPeace    = 0.3f * War.MaxWarGrade;
                    AddAngerAlliedWithEnemy      = 100;
                    FleetStrMultiplier    = 1.05f;
                    FederationPopRatioWar = 1.45f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 15;
                    break;
                case PersonalityType.Cunning:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 50;
                    TurnsAbove95FederationNeeded = 320;
                    AllianceValueAlliedWithEnemy = 0.6f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.7f;
                    WarGradeThresholdForPeace    = 0.7f * War.MaxWarGrade;
                    AddAngerAlliedWithEnemy      = 0;
                    FleetStrMultiplier    = 0.95f;
                    FederationPopRatioWar = 1.2f;
                    TrustCostTradePact    = 5;
                    TrustCostNaPact       = 5;
                    break;
                case PersonalityType.Honorable:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    TurnsAbove95FederationNeeded = 250;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.4f;
                    WarGradeThresholdForPeace    = 0.5f * War.MaxWarGrade;
                    AddAngerAlliedWithEnemy      = 75;
                    FleetStrMultiplier    = 1f;
                    FederationPopRatioWar = 1.25f;
                    TrustCostTradePact    = 10;
                    TrustCostNaPact       = 10;
                    break;
                case PersonalityType.Pacifist:
                    ColonizationClaimRatioWarningThreshold = 1.25f;
                    AddAngerAlliedWithEnemies3RdParty      = 25;
                    TurnsAbove95FederationNeeded = 300;
                    AllianceValueAlliedWithEnemy = 0.8f;
                    WantedAgentMissionMultiplier = 0.1f;
                    WarGradeThresholdForPeace    = 0.85f * War.MaxWarGrade;
                    PlanetStoleTrustMultiplier   = 0.8f;
                    AddAngerAlliedWithEnemy      = 0;
                    FleetStrMultiplier    = 0.9f;
                    FederationPopRatioWar = 1.1f;
                    TrustCostTradePact    = 12;
                    TrustCostNaPact       = 3;
                    break;
            }
        }
    }
}