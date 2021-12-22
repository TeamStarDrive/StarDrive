namespace Ship_Game
{
    /// <summary>
    /// This is a proxy workaround for the current buggy implementation
    /// of `Empire.Universe?.Property` where Universe can be null.
    ///
    /// At some point this can be redesigned as non-global
    /// </summary>
    public static class CurrentGame
    {
        // @note GamePace is used all over the place while UniverseData is still being constructed
        //       So we need to pull it out of UniverseScreen
        public static float Pace { get; private set; } = 1f;
        public static UniverseData.GameDifficulty Difficulty { get; private set; }
        public static GalSize GalaxySize = GalSize.Medium;
        public static int ExtraPlanets;
        public static float StarsModifier = 1f;
        public static float SettingsResearchModifier = 1f;
        public static float RemnantPaceModifier = 20;

        public static void StartNew(UniverseData data, float pace, float starsMod, int extraPlanets, int numEmpires)
        {
            Difficulty      = data.Difficulty;
            GalaxySize      = data.GalaxySize;
            Pace            = pace;
            StarsModifier   = starsMod;
            ExtraPlanets    = extraPlanets;

            SettingsResearchModifier = GetResearchMultiplier(GalaxySize, StarsModifier, ExtraPlanets, numEmpires);
            RemnantPaceModifier      = CalcRemnantPace(GalaxySize, StarsModifier, ExtraPlanets, numEmpires);

            RandomEventManager.ActiveEvent = null; // This is a bug that will reset ongoing event upon game load (like hyperspace flux)
        }

        static float CalcRemnantPace(GalSize galaxySize, float starsModifier, int extraPlanets, int numMajorEmpires)
        {
            float stars      = starsModifier * 4; // 1-8
            float size       = (int)galaxySize + 1; // 1-7
            int extra        = extraPlanets; // 1-3
            float numEmpires = numMajorEmpires / 2f; // 1-4.5

            float pace = 20 - stars - size - extra - numEmpires;
            return pace.LowerBound(1);
        }

        static float GetResearchMultiplier(GalSize galaxySize, float starsModifier, int extraPlanets, int numMajorEmpires)
        {
            if (!GlobalStats.ModChangeResearchCost)
                return 1f;

            int idealNumPlayers   = (int)galaxySize + 3;
            float galSizeModifier = galaxySize <= GalSize.Medium 
                ? ((int)galaxySize / 2f).LowerBound(0.25f) // 0.25, 0.5 or 1
                : 1 + ((int)galaxySize - (int)GalSize.Medium) * 0.25f; // 1.25, 1.5, 1.75, 2

            float extraPlanetsMod = 1 + extraPlanets * 0.25f;
            float playerRatio     = (float)idealNumPlayers / numMajorEmpires;
            float settingsRatio   = galSizeModifier * extraPlanetsMod * playerRatio * starsModifier;

            return settingsRatio;
        }

        public static float ProductionPace => 1 + (Pace - 1) * 0.5f;
    }
}
