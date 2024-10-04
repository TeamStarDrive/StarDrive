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
        public readonly int TurnsAbove95AllianceTreshold; // how many turns we need above trust 95 to offer alliance
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
        public readonly bool ClearNeutralExoticSystems; // Will try to clear neutral exotic systems and deploy research stations and the likes
        public readonly float PlayerWarContributionRatioThreshold; // How much player sub-contribution ratio the AI can tolerate for allied wars
        public readonly int PlayerWarContributionMaxWarnings; // How many warning of lesser player requested war is needed by AI to do something about it
        public readonly bool CanWeSurrenderToPlayerAfterBetrayal; // Will the AI be able to surrender to player after s/he betrayed them in allied war
        public readonly float DistanceToDefendAllyThreshold; // Defend Allies' systems if they are closer to us, basaed on this threshold lower is closer
        public readonly float ImperialistWarPlanetsToTakeMult; // multiplier for how many planets to take from available enemy planets if we have better score
        public readonly float PiratePayChanceModifier; // How inclined to pay pirates
        public readonly float SpyDamageRelationsMultiplier; // Multipler for base rations damage when caught spying. Works for new Espionage logic
        public readonly float WantedMoleCovreage; // Coverage of mole threshold of the victim total planets
        public readonly float AlliancOfferStrThreshold; // Dont offer alliance normally if they are too weak
        public readonly float TrustChangeThreshold; // used to change some trust when this threshold is passed, depending on E personality as well and scores.
        public readonly float AngerMultiplierRelDamage;

        // Espionage AI Operation activation thresholds
        public readonly float EspionageTechScoreOpsMultiplier;
        public readonly float EspionageExpansionScoreOpsMultiplier;
        public readonly float EspionageIndustryScoreOpsMultiplier;
        public readonly float EspionageMilitaryScoreOpsMultiplier;
        public readonly float EspionageTotalScoreOpsMultiplier;


        public PersonalityModifiers(PersonalityType type)
        {
            switch (type)
            {
                default:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 25;
                    PlayerWarContributionRatioThreshold    = 1f;
                    PlayerWarContributionMaxWarnings       = 2;
                    ImperialistWarPlanetsToTakeMult = 0.4f;
                    DistanceToDefendAllyThreshold   = 1f;
                    SpyDamageRelationsMultiplier = 1;
                    TurnsAbove95FederationNeeded = 250;
                    TurnsAbove95AllianceTreshold = 100;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.75f;
                    WarGradeThresholdForPeace    = 0.4f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = false; 
                    AddAngerAlliedWithEnemy      = 0;
                    PiratePayChanceModifier      = 1;
                    TrustChangeThreshold         = 1.5f;
                    AlliancOfferStrThreshold     = 1.5f;
                    AngerMultiplierRelDamage     = 1f;
                    CloserToUsClaimWarn   = 0.3f;
                    WantedMoleCovreage    = 0.25f;
                    DefenseTaskWeight     = 1;
                    FleetStrMultiplier    = 1;
                    FederationPopRatioWar = 4f;
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

                    EspionageTechScoreOpsMultiplier      = 2;
                    EspionageExpansionScoreOpsMultiplier = 2;
                    EspionageIndustryScoreOpsMultiplier  = 2;
                    EspionageMilitaryScoreOpsMultiplier  = 2;
                    EspionageTotalScoreOpsMultiplier     = 2;
                    break;
                case PersonalityType.Aggressive:
                    ColonizationClaimRatioWarningThreshold = 0.9f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    PlayerWarContributionRatioThreshold    = 1f;
                    PlayerWarContributionMaxWarnings       = 1;
                    ImperialistWarPlanetsToTakeMult = 0.6f;
                    DistanceToDefendAllyThreshold   = 0.8f;
                    SpyDamageRelationsMultiplier = 1;
                    TurnsAbove95FederationNeeded = 250;
                    TurnsAbove95AllianceTreshold = 300;
                    AllianceValueAlliedWithEnemy = 0.4f;
                    WantedAgentMissionMultiplier = 0.115f;
                    WarGradeThresholdForPeace    = 0.3f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = true;
                    PlanetStoleTrustMultiplier   = 0.5f;
                    AddAngerAlliedWithEnemy      = 50;
                    PiratePayChanceModifier      = 0.4f;
                    TrustChangeThreshold         = 1.25f;
                    AlliancOfferStrThreshold     = 0.75f;
                    AngerMultiplierRelDamage     = 1f;
                    CloserToUsClaimWarn   = 0.5f;
                    WantedMoleCovreage    = 0.25f;
                    DefenseTaskWeight     = 1.2f;
                    FleetStrMultiplier    = 1.15f;
                    FederationPopRatioWar = 2.5f;
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

                    EspionageTechScoreOpsMultiplier      = 1.2f;
                    EspionageExpansionScoreOpsMultiplier = 1.3f;
                    EspionageIndustryScoreOpsMultiplier  = 1.25f;
                    EspionageMilitaryScoreOpsMultiplier  = 1;
                    EspionageTotalScoreOpsMultiplier     = 1.5f;
                    break;
                case PersonalityType.Ruthless:
                    ColonizationClaimRatioWarningThreshold = 0.8f;
                    AddAngerAlliedWithEnemies3RdParty      = 75;
                    PlayerWarContributionRatioThreshold    = 1.1f;
                    PlayerWarContributionMaxWarnings       = 1;
                    ImperialistWarPlanetsToTakeMult = 0.8f;
                    DistanceToDefendAllyThreshold   = 0.6f;
                    SpyDamageRelationsMultiplier = 1.25f;
                    TurnsAbove95FederationNeeded = 320;
                    TurnsAbove95AllianceTreshold = 250;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.115f;
                    WarGradeThresholdForPeace    = 0.3f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = true;
                    PlanetStoleTrustMultiplier   = 0.6f;
                    AddAngerAlliedWithEnemy      = 25;
                    PiratePayChanceModifier      = 0.5f;
                    TrustChangeThreshold         = 1.2f;
                    AlliancOfferStrThreshold     = 0.7f;
                    AngerMultiplierRelDamage     = 1f;
                    CloserToUsClaimWarn   = 0.4f;
                    WantedMoleCovreage    = 0.2f;
                    DefenseTaskWeight     = 1;
                    FleetStrMultiplier    = 1.1f;
                    FederationPopRatioWar = 2.2f;
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

                    EspionageTechScoreOpsMultiplier      = 1.25f;
                    EspionageExpansionScoreOpsMultiplier = 1.5f;
                    EspionageIndustryScoreOpsMultiplier  = 1.2f;
                    EspionageMilitaryScoreOpsMultiplier  = 1.2f;
                    EspionageTotalScoreOpsMultiplier     = 1.25f;
                    break;
                case PersonalityType.Xenophobic:
                    ColonizationClaimRatioWarningThreshold = 0.6f;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    PlayerWarContributionRatioThreshold    = 1.15f;
                    PlayerWarContributionMaxWarnings       = 1;
                    ImperialistWarPlanetsToTakeMult = 0.5f;
                    DistanceToDefendAllyThreshold   = 0.7f;
                    SpyDamageRelationsMultiplier = 2.5f;
                    TurnsAbove95FederationNeeded = 400;
                    TurnsAbove95AllianceTreshold = 200;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.1f;
                    WarGradeThresholdForPeace    = 0.25f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = true;
                    AddAngerAlliedWithEnemy      = 100;
                    PiratePayChanceModifier      = 0.6f;
                    TrustChangeThreshold         = 1.1f;
                    AlliancOfferStrThreshold     = 0.7f;
                    AngerMultiplierRelDamage     = 2f;
                    CloserToUsClaimWarn   = 0.6f;
                    WantedMoleCovreage    = 0.15f;
                    DefenseTaskWeight     = 1.2f;
                    FleetStrMultiplier    = 1.05f;
                    FederationPopRatioWar = 3f;
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

                    EspionageTechScoreOpsMultiplier      = 1.2f;
                    EspionageExpansionScoreOpsMultiplier = 1.2f;
                    EspionageIndustryScoreOpsMultiplier  = 1.2f;
                    EspionageMilitaryScoreOpsMultiplier  = 1.2f;
                    EspionageTotalScoreOpsMultiplier     = 1.4f;

                    break;
                case PersonalityType.Cunning:
                    ColonizationClaimRatioWarningThreshold = 1;
                    CanWeSurrenderToPlayerAfterBetrayal    = true;
                    AddAngerAlliedWithEnemies3RdParty      = 50;
                    PlayerWarContributionRatioThreshold    = 1.15f;
                    PlayerWarContributionMaxWarnings       = 2;
                    ImperialistWarPlanetsToTakeMult = 0.7f;
                    DistanceToDefendAllyThreshold   = 0.9f;
                    SpyDamageRelationsMultiplier = 0.5f;
                    TurnsAbove95FederationNeeded = 220;
                    TurnsAbove95AllianceTreshold = 150;
                    AllianceValueAlliedWithEnemy = 0.6f;
                    WantedAgentMissionMultiplier = 0.13f;
                    PlanetStoleTrustMultiplier   = 0.7f;
                    WarGradeThresholdForPeace    = 0.5f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = false;
                    AddAngerAlliedWithEnemy      = 0;
                    PiratePayChanceModifier      = 0.7f;
                    AlliancOfferStrThreshold     = 0.8f;
                    TrustChangeThreshold         = 1.4f;
                    AngerMultiplierRelDamage     = 0.8f;
                    CloserToUsClaimWarn   = 0.3f;
                    WantedMoleCovreage    = 0.5f;
                    DefenseTaskWeight     = 1.3f;
                    FleetStrMultiplier    = 0.95f;
                    FederationPopRatioWar = 2f;
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

                    EspionageTechScoreOpsMultiplier      = 1.1f;
                    EspionageExpansionScoreOpsMultiplier = 1.1f;
                    EspionageIndustryScoreOpsMultiplier  = 1.1f;
                    EspionageMilitaryScoreOpsMultiplier  = 1.1f;
                    EspionageTotalScoreOpsMultiplier     = 1.15f;
                    break;
                case PersonalityType.Honorable:
                    ColonizationClaimRatioWarningThreshold = 1;
                    AddAngerAlliedWithEnemies3RdParty      = 100;
                    PlayerWarContributionRatioThreshold    = 1.1f;
                    PlayerWarContributionMaxWarnings       = 1;
                    ImperialistWarPlanetsToTakeMult = 0.5f;
                    DistanceToDefendAllyThreshold   = 1.25f;
                    SpyDamageRelationsMultiplier = 2f;
                    TurnsAbove95FederationNeeded = 200;
                    TurnsAbove95AllianceTreshold = 125;
                    AllianceValueAlliedWithEnemy = 0.5f;
                    WantedAgentMissionMultiplier = 0.1f;
                    PlanetStoleTrustMultiplier   = 0.4f;
                    WarGradeThresholdForPeace    = 0.55f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = false;
                    TrustChangeThreshold         = 1.25f;
                    AddAngerAlliedWithEnemy      = 75;
                    PiratePayChanceModifier      = 0.25f;
                    AlliancOfferStrThreshold     = 0.5f;
                    AngerMultiplierRelDamage     = 2f;
                    CloserToUsClaimWarn   = 0.4f;
                    WantedMoleCovreage    = 0.15f;
                    DefenseTaskWeight     = 1.5f;
                    FleetStrMultiplier    = 1f;
                    FederationPopRatioWar = 1.5f;
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

                    EspionageTechScoreOpsMultiplier      = 2.6f;
                    EspionageExpansionScoreOpsMultiplier = 2.5f;
                    EspionageIndustryScoreOpsMultiplier  = 2.5f;
                    EspionageMilitaryScoreOpsMultiplier  = 2.75f;
                    EspionageTotalScoreOpsMultiplier     = 3f;
                    break;
                case PersonalityType.Pacifist:
                    ColonizationClaimRatioWarningThreshold = 1.25f;
                    AddAngerAlliedWithEnemies3RdParty      = 25;
                    PlayerWarContributionRatioThreshold    = 1.1f;
                    PlayerWarContributionMaxWarnings       = 2;
                    ImperialistWarPlanetsToTakeMult = 0.4f;
                    DistanceToDefendAllyThreshold   = 1.4f;
                    SpyDamageRelationsMultiplier = 1.5f;
                    TurnsAbove95FederationNeeded = 200;
                    TurnsAbove95AllianceTreshold = 100;
                    AllianceValueAlliedWithEnemy = 0.8f;
                    WantedAgentMissionMultiplier = 0.1f;
                    WarGradeThresholdForPeace    = 0.75f * War.MaxWarGrade;
                    ClearNeutralExoticSystems    = false;
                    PlanetStoleTrustMultiplier   = 0.8f;
                    TrustChangeThreshold         = 1.5f;
                    AddAngerAlliedWithEnemy      = 0;
                    PiratePayChanceModifier      = 0.75f;
                    AlliancOfferStrThreshold     = 0.2f;
                    AngerMultiplierRelDamage     = 1;
                    CloserToUsClaimWarn   = 0.2f;
                    WantedMoleCovreage    = 0.25f;
                    DefenseTaskWeight     = 2;
                    FleetStrMultiplier    = 0.9f;
                    FederationPopRatioWar = 1.2f;
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

                    EspionageTechScoreOpsMultiplier      = 2f;
                    EspionageExpansionScoreOpsMultiplier = 2.5f;
                    EspionageIndustryScoreOpsMultiplier  = 2.75f;
                    EspionageMilitaryScoreOpsMultiplier  = 2.75f;
                    EspionageTotalScoreOpsMultiplier     = 3f;
                    break;
            }
        }
    }
}