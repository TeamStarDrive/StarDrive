using System;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public SavedGame Save(string saveName, bool async = true)
        {
            try
            {
                var savedGame = new SavedGame(this);
                savedGame.Save(saveName, async);
                return savedGame; // used in unit testing
            }
            catch (Exception e)
            {
                Log.Error(e, $"Universe.Save('{saveName}') failed");
            }
            return null;
        }

        void AutoSaveCurrentGame()
        {
            Save("Autosave" + Auto);
            if (++Auto > 3) Auto = 1;
        }
    }
}