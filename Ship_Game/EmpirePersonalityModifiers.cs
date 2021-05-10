using Ship_Game.AI.StrategyAI.WarGoals;

namespace Ship_Game
{
    public class PersonalityModifiers
    {
        public readonly float ColonizationClaimRatioWarningThreshold; // warn the player if we have mutual a colonization target based on value
        public readonly float TrustCostNaPact;
        public readonly float TrustCostTradePact;
        public readonly float AddAngerAlliedWithEnemy;
        public readonly float AddAngerAlliedWithEnemies3RdParty;
        public readonly float AllianceValueAlliedWithEnemy;
        public readonly float WantedAgentMissionMultiplier;
        public readonly int TurnsAbove95FederationNeeded;
        public readonly float FederationPopRatioWar;
        public readonly float PlanetStoleTrustMultiplier;
        public readonly float WarGradeThresholdForPeace; // How bad should our total wars grade be to request peace
        public readonly float FleetStrMultiplier; // Add or decrease str addition to fleets after win / lose vs. another empire.
        public readonly float DefenseTaskWeight; // How much the AI values defense task over other (it will cancel other tasks for defense), bigger is more value
        public readonly float TechValueModifier; // Some personalities value techs more vs player
        public readonly float AssaultBomberRatio; // Percent of existing troops to launch in order to board attacking fleets when planet is bombed
        public readonly float AllyCallToWarRatio; // The tolerance the AI has to join war with an ally vs 3rd party
        public readonly int PostInvasionTurns; // How many turns a post invasion defense fleet will stay

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
                    DefenseTaskWeight     = 2;
                    FleetStrMultiplier    = 1;
                    FederationPopRatioWar = 1.5f;
                    AssaultBomberRatio    = 0.5f;
                    AllyCallToWarRatio    = 1.2f;
                    TrustCostTradePact    = 0;
                    TechValueModifier     = 1;
                    PostInvasionTurns     = 50;
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
                    DefenseTaskWeight     = 4f;
                    FleetStrMultiplier    = 1.4f;
                    FederationPopRatioWar = 1.25f;
                    AssaultBomberRatio    = 0.75f;
                    AllyCallToWarRatio    = 1.15f;
                    TrustCostTradePact    = 20;
                    TrustCostNaPact       = 35;
                    PostInvasionTurns     = 25;
                    TechValueModifier     = 1.05f;
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
                    DefenseTaskWeight     = 6;
                    FleetStrMultiplier    = 1.3f;
                    FederationPopRatioWar = 1.2f;
                    AssaultBomberRatio    = 1;
                    AllyCallToWarRatio    = 1.2f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 45f;
                    PostInvasionTurns     = 25;
                    TechValueModifier     = 1.1f;
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
                    DefenseTaskWeight     = 7;
                    FleetStrMultiplier    = 1.05f;
                    FederationPopRatioWar = 1.45f;
                    AllyCallToWarRatio    = 1.1f;
                    AssaultBomberRatio    = 0.5f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 15;
                    PostInvasionTurns     = 50;
                    TechValueModifier     = 1.2f;
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
                    DefenseTaskWeight     = 8;
                    FleetStrMultiplier    = 0.95f;
                    FederationPopRatioWar = 1.2f;
                    AssaultBomberRatio    = 0.8f;
                    AllyCallToWarRatio    = 1.25f;
                    TrustCostTradePact    = 5;
                    TrustCostNaPact       = 5;
                    PostInvasionTurns     = 60;
                    TechValueModifier     = 1.1f;
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
                    DefenseTaskWeight     = 9;
                    FleetStrMultiplier    = 1f;
                    FederationPopRatioWar = 1.25f;
                    AssaultBomberRatio    = 0.6f;
                    AllyCallToWarRatio    = 1f;
                    TrustCostTradePact    = 10;
                    TrustCostNaPact       = 10;
                    PostInvasionTurns     = 70;
                    TechValueModifier     = 1;
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
                    DefenseTaskWeight     = 10;
                    FleetStrMultiplier    = 0.9f;
                    FederationPopRatioWar = 1.1f;
                    AssaultBomberRatio    = 0.5f;
                    AllyCallToWarRatio    = 1.35f;
                    TrustCostTradePact    = 12;
                    TrustCostNaPact       = 3;
                    PostInvasionTurns     = 75;
                    TechValueModifier     = 1;
                    break;
            }
        }
    }
}