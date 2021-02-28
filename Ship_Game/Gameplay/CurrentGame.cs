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
        public static int NumMajorEmpires; // Including the player
        public static float StarsModifier = 1f;

        public static void StartNew(UniverseData data, float pace, float starsMod, int extraPlanets, int numEmpires)
        {
            Difficulty      = data.difficulty;
            GalaxySize      = data.GalaxySize;
            Pace            = pace;
            StarsModifier   = starsMod;
            ExtraPlanets    = extraPlanets;
            NumMajorEmpires = numEmpires;

            RandomEventManager.ActiveEvent = null; // This is a bug that will reset ongoing event upon game load (like hyperspace flux)
        }

        public static float ProductionPace => 1 + (Pace - 1) * 0.5f;
    }
}
