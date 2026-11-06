using System;

namespace Ship_Game;

public partial class UniverseScreen
{
    /// <summary>
    /// Saves forcefully Pause the game, until the auto-save is complete
    /// </summary>
    public bool IsSaving { get; internal set; }
    string PendingSaveName;
    int Auto = 1;

    public SavedGame Save(string saveName, bool throwOnError = false)
    {
        try
        {
            ApplyPendingChangesBeforeSave();
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

    // a change of hands applies here, on the sim thread, with what it sets off, so the save worker finds none half done
    void ApplyPendingChangesBeforeSave() => UState.Objects.UpdateLists(removeInactiveObjects: true);

    public void SaveAsync(string saveName, bool resetLogOnComplete = false)
    {
        ApplyPendingChangesBeforeSave();
        IsSaving = true;
        var savedGame = new SavedGame(this);
        savedGame.SaveAsync(saveName, (error) =>
        {
            IsSaving = false;
            if (error != null)
            {
                Log.Error(error, $"Universe.SaveAsync('{saveName}') failed");
            }
            else if (resetLogOnComplete)
            {
                // Bound blackbox.log growth (and the tail attached to Sentry reports)
                // by truncating it after each successful autosave. Startup rotation to
                // blackbox.old is unchanged.
                Log.ResetCurrentLog();
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
        float now = GameBase.Base.TotalElapsed;
        if (!UState.Paused && LastSaveCheckTime > 0f) // a gap with no sim turns, behind a full screen or a minimized window, is not play
            PlayedSinceSave += Math.Min(now - LastSaveCheckTime, 1f);
        LastSaveCheckTime = now;

        if (PendingSaveName.NotEmpty())
        {
            SaveAsync(PendingSaveName);
            PendingSaveName = null;
            PlayedSinceSave = 0f; // a save of the player's resets the autosave timer
        }
        else if (PlayedSinceSave >= GlobalStats.AutoSaveFreq)
        {
            PlayedSinceSave = 0f;
            string saveName = "Autosave" + Auto;
            if (++Auto > 3) Auto = 1;
            SaveAsync(saveName, resetLogOnComplete: true);
        }
    }
}