using System;

namespace Ship_Game;

public partial class UniverseScreen
{
    /// <summary>
    /// Saves forcefully Pause the game, until the auto-save is complete
    /// </summary>
    public bool IsSaving { get; private set; }
    string PendingSaveName;
    int Auto = 1;

    public SavedGame Save(string saveName, bool throwOnError = false)
    {
        try
        {
            IsSaving = true;
            var savedGame = new SavedGame(this);
            savedGame.Save(saveName);
            return savedGame; // used in unit testing
        }
        catch (Exception e)
        {
            if (throwOnError)
                throw;
            Log.Error(e, $"Universe.Save('{saveName}') failed");
        }
        finally
        {
            IsSaving = false;
        }
        return null;
    }

    public void SaveAsync(string saveName)
    {
        IsSaving = true;
        var savedGame = new SavedGame(this);
        savedGame.SaveAsync(saveName, (error) =>
        {
            IsSaving = false;
            if (error != null)
            {
                Log.Error(error, $"Universe.SaveAsync('{saveName}') failed");
            }
        });
    }

    // Saves must run on the simulation thread to ensure thread safety
    public void SaveDuringNextUpdate(string saveName)
    {
        PendingSaveName = saveName;
    }

    void CheckForPendingSaves()
    {
        GameBase game = GameBase.Base;

        if (PendingSaveName.NotEmpty())
        {
            SaveAsync(PendingSaveName);
            PendingSaveName = null;

            // reset auto-save timer since we just saved the game
            LastAutosaveTime = game.TotalElapsed;
        }
        else
        {
            if (LastAutosaveTime == 0f)
                LastAutosaveTime = game.TotalElapsed;

            float timeSinceLastAutoSave = (game.TotalElapsed - LastAutosaveTime);
            if (timeSinceLastAutoSave >= GlobalStats.AutoSaveFreq)
            {
                LastAutosaveTime = game.TotalElapsed;
                string saveName = "Autosave" + Auto;
                if (++Auto > 3) Auto = 1;
                SaveAsync(saveName);
            }
        }
    }
}