namespace Ship_Game
{
    public class DifficultyModifiers
    {
        public readonly int SysComModifier;
        public readonly int DiploWeightVsPlayer;
        public readonly float BaseColonyGoals;
        public readonly float ColonyGoalMultiplier; // Multiplier to the colony goals by rank
        public readonly float ShipBuildStrMin;
        public readonly float ShipBuildStrMax;
        public readonly int ColonyRankModifier;
        public readonly float TaskForceStrength;
        public readonly bool DataVisibleToPlayer;
        public readonly float Anger;
        public readonly int ShipLevel;
        public readonly bool HideTacticalData;
        public readonly float MaxDesiredPlanets;
        public readonly float CreditsMultiplier;
        public readonly float EnemyTroopStrength;
        public readonly int MineralDecayDivider;
        public readonly float PiratePayModifier;
        public readonly int MinStartingColonies; // Starting colonies what we want
        public readonly int ExpandSearchTurns; // For Expansion
        public readonly int RemnantTurnsLevelUp; // How many turns should pass before Remnants level up
        public readonly float RemnantResourceMod; // Multiplier to Remnant Prod generation
        public readonly float RemnantNumBombers; // Multiplier to Remnant bombers wanted
        public readonly int RemnantPortalCreationMod; // Determines portal creation on a new remnant level
        public readonly int StandByColonyShips;
        public readonly int TrustLostStoleColony; // Vs players
        public readonly float FleetStrModifier; // AI increase/decrease str of fleets needs, when they win or lose vs another empire
        public readonly int NumSystemsToSniff; // Number of system the AI will try to re-scout until it is fully explored
        public readonly int PlayerWarPriorityLimit; // Priority of wars vs player (war priority is 0 to 10 where 0 means more priority)
        public readonly int NumWarTasksPerWar;
        public readonly int WarTaskPriorityMod; // higher priority vs player if at war with them
        public readonly int WarSneakiness; // modifier to prepare for war detection by the player (player will need better spy network to detect war plot)
        public readonly float HullTechMultiplier; // used in ship progression to modify hull tech cost if needed
        public readonly int CombatShipGoalsPerPlanet;
        public readonly int MiningOpsTurnsPerRun; // Mining Ops planner turn timer

        // A mod can set the general str of remnant designs. Default is 2 an this is a multiplier for starting fleet multiplier
        public readonly float RemnantStrModifier; 

        // AI Buffs/Nerfs
        public readonly float FlatMoneyBonus;
        public readonly float ProductionMod;
        public readonly float ResearchMod;
        public readonly float TaxMod;
        public readonly float ShipCostMod;
        public readonly float TroopCostMod;
        public readonly float ResearchTaxMultiplier;
        public readonly float ModHpModifier;

        /// <summary>
        /// Higher than 1 will decrease colonization pace and vice versa
        /// </summary>
        public readonly float ExpansionMultiplier;

        /// <summary>
        /// For lower difficulties, this interval will be higher
        /// For high difficulty, the AI keeps checking expansion more frequently
        /// </summary>
        public readonly int ExpansionCheckInterval;

        public DifficultyModifiers(Empire empire, GameDifficulty difficulty)
        {
            float remnantGeneralStr = 2; // Vanilla will be 2
            if (GlobalStats.Defaults.RemnantDesignStrMultiplier > 0.01f)
                remnantGeneralStr = GlobalStats.Defaults.RemnantDesignStrMultiplier;

            DataVisibleToPlayer    = false;
            FlatMoneyBonus         = 0;
            ProductionMod          = 0;
            ResearchMod            = 0;
            TaxMod                 = 0;
            ShipCostMod            = 0;
            TroopCostMod           = 0;
            ModHpModifier          = 0;
            ResearchTaxMultiplier  = 1;
            PlayerWarPriorityLimit = 10;
            switch (difficulty)
            {
                default:
                case GameDifficulty.Normal:
                    ShipBuildStrMin      = 0.7f;
                    ShipBuildStrMax      = 0.9f;
                    ColonyRankModifier   = 0;
                    TaskForceStrength    = 1.25f;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.5f;
                    CreditsMultiplier    = 0.2f;
                    EnemyTroopStrength   = 1.4f;
                    MineralDecayDivider  = 80;
                    PiratePayModifier    = 0.75f;
                    ExpansionMultiplier  = 1.0f;
                    ExpansionCheckInterval = 7; // every 7 turns
                    MinStartingColonies  = 3;
                    ExpandSearchTurns    = 50;
                    RemnantTurnsLevelUp  = 350;
                    RemnantResourceMod   = 0.5f;
                    RemnantNumBombers    = 0.75f;
                    BaseColonyGoals      = 2;
                    ColonyGoalMultiplier = 0.5f;
                    StandByColonyShips   = 0;
                    TrustLostStoleColony = 5;
                    FleetStrModifier     = 0.2f;
                    NumSystemsToSniff    = 2;
                    NumWarTasksPerWar    = 2;
                    WarTaskPriorityMod   = 1;
                    RemnantStrModifier   = remnantGeneralStr + 0.5f;
                    WarSneakiness        = 0;
                    HullTechMultiplier   = 1f;
                    MiningOpsTurnsPerRun = 10;
                    RemnantPortalCreationMod = 10;
                    CombatShipGoalsPerPlanet = 3;
                    break;
                case GameDifficulty.Hard:
                    ShipBuildStrMin      = 0.8f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 1;
                    TaskForceStrength    = 1.5f;
                    ShipLevel            = 2;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 0.75f;
                    CreditsMultiplier    = empire.isPlayer ? 0.3f : 0.1f;
                    EnemyTroopStrength   = 1.6f;
                    MineralDecayDivider  = 60;
                    PiratePayModifier    = 1f;
                    ExpansionMultiplier  = 0.5f;
                    ExpansionCheckInterval = 5; // every 5 turns
                    MinStartingColonies  = 5;
                    ExpandSearchTurns    = 30;
                    RemnantTurnsLevelUp  = 325;
                    RemnantResourceMod   = 0.6f;
                    RemnantNumBombers    = 1f;
                    BaseColonyGoals      = 4;
                    ColonyGoalMultiplier = 0.75f;
                    StandByColonyShips   = 3;
                    TrustLostStoleColony = 10;
                    FleetStrModifier     = 0.3f;
                    NumSystemsToSniff    = 3;
                    NumWarTasksPerWar    = 3;
                    WarTaskPriorityMod   = 1;
                    RemnantStrModifier   = remnantGeneralStr + 1;
                    WarSneakiness        = 10;
                    HullTechMultiplier   = 0.85f;
                    MiningOpsTurnsPerRun = 7;
                    RemnantPortalCreationMod = 6;
                    CombatShipGoalsPerPlanet = 4;
                    if (!empire.isPlayer)
                    {
                        FlatMoneyBonus = 10;
                        ProductionMod  = 0.3f;
                        ResearchMod    = 0.5f;
                        TaxMod         = 0.5f;
                        ShipCostMod    = -0.2f;
                        TroopCostMod   = -0.2f;
                        ResearchTaxMultiplier  = 0.7f;
                        PlayerWarPriorityLimit = 5;
                    }

                    break;
                case GameDifficulty.Brutal:
                    ShipBuildStrMin      = 0.9f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 2;
                    TaskForceStrength    = 1.75f;
                    ShipLevel            = 3;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 1f;
                    CreditsMultiplier    = empire.isPlayer ? 0.4f : 0.075f;
                    EnemyTroopStrength   = 1.8f;
                    MineralDecayDivider  = 40;
                    PiratePayModifier    = 1.5f;
                    ExpansionMultiplier  = 0.25f; 
                    ExpansionCheckInterval = 3; // every 3 turns
                    MinStartingColonies  = 6;
                    ExpandSearchTurns    = 20;
                    RemnantTurnsLevelUp  = 300;
                    RemnantResourceMod   = 0.75f;
                    RemnantNumBombers    = 1.25f;
                    BaseColonyGoals      = 5;
                    ColonyGoalMultiplier = 1;
                    StandByColonyShips   = 4;
                    TrustLostStoleColony = 15;
                    FleetStrModifier     = 0.4f;
                    NumSystemsToSniff    = 4;
                    NumWarTasksPerWar    = 4;
                    WarTaskPriorityMod   = 2;
                    RemnantStrModifier   = remnantGeneralStr + 1.5f;
                    WarSneakiness        = 25;
                    HullTechMultiplier   = 0.7f;
                    MiningOpsTurnsPerRun = 5;
                    RemnantPortalCreationMod = 5;
                    CombatShipGoalsPerPlanet = 5;
                    if (!empire.isPlayer)
                    {
                        FlatMoneyBonus = 20;
                        ProductionMod  = 0.6f;
                        ResearchMod    = 1.2f;
                        TaxMod         = 1f;
                        ShipCostMod    = -0.4f;
                        ModHpModifier  = 0.1f;
                        TroopCostMod   = -0.4f;
                        ResearchTaxMultiplier  = 0.5f;
                        PlayerWarPriorityLimit = 3;
                    }

                    break;
                case GameDifficulty.Insane:
                    ShipBuildStrMin      = 0.9f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 2;
                    TaskForceStrength    = 1.75f;
                    ShipLevel            = 5;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 1f;
                    CreditsMultiplier    = empire.isPlayer ? 0.5f : 0.05f;
                    EnemyTroopStrength   = 1.8f;
                    MineralDecayDivider  = 40;
                    PiratePayModifier    = 1.5f;
                    ExpansionMultiplier  = 0.1f; // 10x lower threshold, Insane AI can expand as much as it wants!
                    ExpansionCheckInterval = 1; // every turn, there is no limit!
                    MinStartingColonies  = 6;
                    ExpandSearchTurns    = 20;
                    RemnantTurnsLevelUp  = 275;
                    RemnantResourceMod   = 1f;
                    RemnantNumBombers    = 1.5f;
                    BaseColonyGoals      = 6;
                    ColonyGoalMultiplier = 1;
                    StandByColonyShips   = 5;
                    TrustLostStoleColony = 20;
                    FleetStrModifier     = 0.5f;
                    NumSystemsToSniff    = 5;
                    NumWarTasksPerWar    = 4;
                    WarTaskPriorityMod   = 2;
                    RemnantStrModifier   = remnantGeneralStr + 2f;
                    WarSneakiness        = 40;
                    HullTechMultiplier   = 0.6f;
                    MiningOpsTurnsPerRun = 3;
                    RemnantPortalCreationMod = 4;
                    CombatShipGoalsPerPlanet = 6;
                    if (!empire.isPlayer)
                    {
                        FlatMoneyBonus         = 30;
                        ProductionMod          = 1.2f;
                        ResearchMod            = 2f;
                        TaxMod                 = 1.5f;
                        ShipCostMod            = -0.6f;
                        TroopCostMod           = -0.6f;
                        ModHpModifier          = 0.25f;
                        ResearchTaxMultiplier  = 0.3f;
                        PlayerWarPriorityLimit = 2;
                    }

                    break;
            }

            SysComModifier      = (int)(((int)difficulty + 1) * 0.5f + 0.5f);
            DiploWeightVsPlayer = (int)difficulty + 1;
            Anger               = 1 + ((int)difficulty + 1) * 0.2f;

            if (empire.isPlayer)
            {
                BaseColonyGoals    = 5;
                ShipBuildStrMin    = 1f;
                ShipBuildStrMax    = 1f;
                ColonyRankModifier = 0;
                TaskForceStrength  = 1f;
                MiningOpsTurnsPerRun = 3;
                ExpansionCheckInterval = 2;
                if (empire.Universe.P.FixedPlayerCreditCharge && difficulty > GameDifficulty.Normal)
                    CreditsMultiplier = 0.2f;
            }

            if (empire.WeAreRemnants)
            {
                TaskForceStrength = 1f;
            }
        }
    }
}
