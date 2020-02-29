namespace Ship_Game
{
    public struct DifficultyModifiers
    {
        public readonly int SysComModifier;
        public readonly int DiploWeightVsPlayer;
        public readonly float BaseColonyGoals;
        public readonly float ShipBuildStrMin;
        public readonly float ShipBuildStrMax;
        public readonly int ColonyRankModifier;
        public readonly float TaskForceStrength;
        public readonly bool DataVisibleToPlayer;
        public readonly float Anger;
        public readonly int RemnantStory;
        public readonly int ShipLevel;
        public readonly bool HideTacticalData;
        public readonly float MaxDesiredPlanets;
        public readonly float FleetCompletenessMin;
        public readonly int AssetBombThreshold;
        public readonly float EnemyTroopStrength;

        public DifficultyModifiers(Empire empire, UniverseData.GameDifficulty difficulty)
        {
            DataVisibleToPlayer = false;
            switch (difficulty)
            {
                case UniverseData.GameDifficulty.Easy:
                    ShipBuildStrMin      = 0.3f;
                    ShipBuildStrMax      = 0.8f;
                    ColonyRankModifier   = -2;
                    TaskForceStrength    = 0.8f;
                    DataVisibleToPlayer  = true;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.25f;
                    FleetCompletenessMin = 0.25f;
                    AssetBombThreshold   = 20;
                    EnemyTroopStrength   = 1f;
                    break;
                default:
                case UniverseData.GameDifficulty.Normal:
                    ShipBuildStrMin      = 0.7f;
                    ShipBuildStrMax      = 1;
                    ColonyRankModifier   = 0;
                    TaskForceStrength    = 1f;
                    ShipLevel            = 0;
                    HideTacticalData     = false;
                    MaxDesiredPlanets    = 0.5f;
                    FleetCompletenessMin = 0.25f;
                    AssetBombThreshold   = 10;
                    EnemyTroopStrength   = 1.1f;
                    break;
                case UniverseData.GameDifficulty.Hard:
                    ShipBuildStrMin      = 0.8f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 1;
                    TaskForceStrength    = 1.1f;
                    ShipLevel            = 2;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 0.75f;
                    FleetCompletenessMin = 0.5f;
                    AssetBombThreshold   = 7;
                    EnemyTroopStrength   = 1.2f;
                    break;
                case UniverseData.GameDifficulty.Brutal:
                    ShipBuildStrMin      = 0.9f;
                    ShipBuildStrMax      = 1f;
                    ColonyRankModifier   = 2;
                    TaskForceStrength    = 1.2f;
                    ShipLevel            = 3;
                    HideTacticalData     = true;
                    MaxDesiredPlanets    = 1f;
                    FleetCompletenessMin = 1f;
                    AssetBombThreshold   = 5;
                    EnemyTroopStrength   = 1.3f;
                    break;
            }

            if (empire.isPlayer)
            {
                BaseColonyGoals = 10;
            }
            else
            {
                EconomicResearchStrategy strategy = ResourceManager.GetEconomicStrategy(empire.data.EconomicPersonality.Name);
                BaseColonyGoals                   = (float)difficulty * 2.5f * strategy.ExpansionRatio;
            }

            SysComModifier      = (int)(((int)difficulty + 1) * 0.75f + 0.5f);
            DiploWeightVsPlayer = (int)difficulty + 1;
            Anger               = 1 + ((int)difficulty + 1) * 0.2f;
            RemnantStory        = (int)difficulty * 3;

            if (empire.isPlayer)
            {
                ShipBuildStrMin    = 0.9f;
                ShipBuildStrMax    = 1f;
                ColonyRankModifier = 0;
                TaskForceStrength  = 1f;
            }
        }
    }
}
