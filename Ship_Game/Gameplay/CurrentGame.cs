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

        public static void StartNew(UniverseData data, float pace, float starsMod, int extraPlanets, int numEmpires)
        {
            Difficulty      = data.difficulty;
            GalaxySize      = data.GalaxySize;
            Pace            = pace;
            StarsModifier   = starsMod;
            ExtraPlanets    = extraPlanets;

            SettingsResearchModifier = GetResearchMultiplier(GalaxySize, StarsModifier, ExtraPlanets, numEmpires);

            RandomEventManager.ActiveEvent = null; // This is a bug that will reset ongoing event upon game load (like hyperspace flux)
        }

        static float GetResearchMultiplier(GalSize galaxySize, float starsModifier, int extraPlanets, int numMajorEmpires)
        {
            if (!GlobalStats.ModChangeResearchCost)
                return 1f;

            int idealNumPlayers   = (int)galaxySize + 3;
            float galSizeModifier = ((int)galaxySize / 2f).LowerBound(0.25f);
            float extraPlanetsMod = 1 + extraPlanets * 0.25f;
            float playerRatio     = (float)idealNumPlayers / numMajorEmpires;
            float settingsRatio   = galSizeModifier * extraPlanetsMod * playerRatio * starsModifier;

            return settingsRatio;
        }

        public static float ProductionPace => 1 + (Pace - 1) * 0.5f;
    }
}
