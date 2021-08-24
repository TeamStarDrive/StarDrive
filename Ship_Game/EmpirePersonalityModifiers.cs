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
        public readonly float GoToWarTolerance; // How stronger then them should we be in order to go to war when preparing
        public readonly float DoomFleetThreshold; // If over this threshold, the AI will send a fleet to glass the target planet
        public readonly float WarTasksLifeTime; // How many turns to keep trying fleet requisition before timeout, 1f is 10 turns
        public readonly int WarSneakiness; // modifier to prepare for war detection by the player (player will need better spy network to detect war plot)
        public readonly float HullTechMultiplier; // used in ship progression to modify hull tech cost if needed
        public readonly float PopRatioBeforeMerge; // If enemy has pop bigger then this ratio, consider merge with other empires or surrender
        public readonly float CloserToUsClaimWarn; // Multiplier for distance of new system colonized from empire center, for warning

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
                    CloserToUsClaimWarn   = 0.3f;
                    DefenseTaskWeight     = 1;
                    FleetStrMultiplier    = 1;
                    FederationPopRatioWar = 2f;
                    PopRatioBeforeMerge   = 0.15f;
                    DoomFleetThreshold    = 2;
                    AssaultBomberRatio    = 0.5f;
                    AllyCallToWarRatio    = 1.2f;
                    HullTechMultiplier    = 1f;
                    TrustCostTradePact    = 0;
                    TechValueModifier     = 1;
                    PostInvasionTurns     = 50;
                    GoToWarTolerance      = 1.5f;
                    WarTasksLifeTime      = 1;
                    TrustCostNaPact       = 0;
                    WarSneakiness         = 0;
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
                    CloserToUsClaimWarn   = 0.5f;
                    DefenseTaskWeight     = 1.2f;
                    FleetStrMultiplier    = 1.15f;
                    FederationPopRatioWar = 1.6f;
                    PopRatioBeforeMerge   = 0.15f;
                    DoomFleetThreshold    = 1.5f;
                    AssaultBomberRatio    = 0.75f;
                    AllyCallToWarRatio    = 1.15f;
                    HullTechMultiplier    = 0.85f;
                    TrustCostTradePact    = 20;
                    TrustCostNaPact       = 35;
                    PostInvasionTurns     = 25;
                    TechValueModifier     = 1.05f;
                    WarTasksLifeTime      = 3;
                    GoToWarTolerance      = 1.1f;
                    WarSneakiness         = 5;
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
                    CloserToUsClaimWarn   = 0.4f;
                    DefenseTaskWeight     = 1;
                    FleetStrMultiplier    = 1.1f;
                    FederationPopRatioWar = 1.5f;
                    PopRatioBeforeMerge   = 0.125f;
                    DoomFleetThreshold    = 1f;
                    AssaultBomberRatio    = 1;
                    AllyCallToWarRatio    = 1.2f;
                    HullTechMultiplier    = 0.8f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 45f;
                    PostInvasionTurns     = 25;
                    TechValueModifier     = 1.1f;
                    WarTasksLifeTime      = 2.5f;
                    GoToWarTolerance      = 1.15f;
                    WarSneakiness         = 0;
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
                    CloserToUsClaimWarn   = 0.6f;
                    DefenseTaskWeight     = 1.2f;
                    FleetStrMultiplier    = 1.05f;
                    FederationPopRatioWar = 1.7f;
                    PopRatioBeforeMerge   = 0.03f;
                    DoomFleetThreshold    = 1.75f;
                    AllyCallToWarRatio    = 1.1f;
                    HullTechMultiplier    = 0.7f;
                    AssaultBomberRatio    = 0.5f;
                    TrustCostTradePact    = 15;
                    TrustCostNaPact       = 15;
                    PostInvasionTurns     = 50;
                    TechValueModifier     = 1.2f;
                    WarTasksLifeTime      = 2.5f;
                    GoToWarTolerance      = 1.5f;
                    WarSneakiness         = 0;
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
                    CloserToUsClaimWarn   = 0.3f;
                    DefenseTaskWeight     = 1.3f;
                    FleetStrMultiplier    = 0.95f;
                    FederationPopRatioWar = 1.3f;
                    PopRatioBeforeMerge   = 0.2f;
                    DoomFleetThreshold    = 2;
                    AssaultBomberRatio    = 0.8f;
                    AllyCallToWarRatio    = 1.25f;
                    HullTechMultiplier    = 1f;
                    TrustCostTradePact    = 5;
                    TrustCostNaPact       = 5;
                    PostInvasionTurns     = 60;
                    TechValueModifier     = 1.1f;
                    WarTasksLifeTime      = 2;
                    GoToWarTolerance      = 1.5f;
                    WarSneakiness         = 10;
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
                    CloserToUsClaimWarn   = 0.4f;
                    DefenseTaskWeight     = 1.5f;
                    FleetStrMultiplier    = 1f;
                    FederationPopRatioWar = 1.45f;
                    PopRatioBeforeMerge   = 0.15f;
                    HullTechMultiplier    = 0.9f;
                    DoomFleetThreshold    = 2.5f;
                    AssaultBomberRatio    = 0.6f;
                    AllyCallToWarRatio    = 1f;
                    TrustCostTradePact    = 10;
                    TrustCostNaPact       = 10;
                    PostInvasionTurns     = 70;
                    TechValueModifier     = 1;
                    WarTasksLifeTime      = 3;
                    GoToWarTolerance      = 1.1f;
                    WarSneakiness         = -10;
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
                    CloserToUsClaimWarn   = 0.2f;
                    DefenseTaskWeight     = 2;
                    FleetStrMultiplier    = 0.9f;
                    FederationPopRatioWar = 1.4f;
                    PopRatioBeforeMerge   = 0.2f;
                    HullTechMultiplier    = 1f;
                    DoomFleetThreshold    = 3;
                    AssaultBomberRatio    = 0.5f;
                    AllyCallToWarRatio    = 1.35f;
                    TrustCostTradePact    = 12;
                    TrustCostNaPact       = 3;
                    PostInvasionTurns     = 75;
                    TechValueModifier     = 1;
                    WarTasksLifeTime      = 1.5f;
                    GoToWarTolerance      = 2f;
                    WarSneakiness         = -5;
                    break;
            }
        }
    }
}