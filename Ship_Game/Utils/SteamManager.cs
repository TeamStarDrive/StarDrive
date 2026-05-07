using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable CA1060, CA1401

namespace Ship_Game.Utils;

public static class SteamManager
{
    public static bool IsInitialized;

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayAchievements();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayCommunity();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayFriends();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayOfficialGameGroup();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayPlayers();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlaySettings();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void ActivateOverlayToGARStore();

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool FileExists(string pchFileName);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool GetAchievement(string pchName, ref bool pbAchieved);

    public static string GetFileOnRemoteStorage(string fileName)
    {
        int fileSize = GetFileSize(fileName);
        byte[] fileBytes = new byte[fileSize];
        GetFileOnRemoteStorage(fileName, fileBytes, fileSize);
        return (new UTF8Encoding()).GetString(fileBytes);
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern IntPtr GetFileOnRemoteStorage(string fileName, byte[] data, int bytesToRead);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern int GetFileSize(string fileName);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern float GetStatFLOAT(string statName);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern int GetStatINT(string statName);

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern ulong GetSteamID();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern IntPtr GetSteamName();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern bool IsOverlayEnabled();
    
    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern void ActivateOverlayWebPage(string url);

    public static void ActivateWebOverlay(string url)
    {
        if (IsInitialized)
            ActivateOverlayWebPage(url);
        else // fallback to whatever Windows uses for URL-s
            Process.Start(url);
    }

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern bool RequestCurrentStats();

    public static bool RequestStats()
    {
        return IsInitialized && RequestCurrentStats();
    }

    public static bool SaveAllGARPlayerStats()
    {
        if (!IsInitialized) return false;
        SetStatINT("times_played (0-1)", GlobalStats.TimesPlayed);
        return SaveAllStatAndAchievementChanges();
    }

    [DllImport("GARSteamManager", ExactSpelling=false)]
    public static extern bool SaveAllStatAndAchievementChanges();

    public static bool SaveFileOnRemoteStorage(string fileName, string fileContents)
    {
        if (!IsInitialized) return false;
        byte[] saveStrBytes = (new UTF8Encoding()).GetBytes(fileContents);
        return SaveFileOnRemoteStorage(fileName, saveStrBytes, saveStrBytes.Length);
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool SaveFileOnRemoteStorage(string fileName, byte[] data, int bytesToWrite);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool SetAchievement(string Achievementname);

    public static bool AchievementUnlocked(string achievementName)
    {
        return IsInitialized && SetAchievement(achievementName) && SaveAllStatAndAchievementChanges();
    }

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void SetOverlayPosition(int corner);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool SetStatFLOAT(string statName, float statValue);

    [DllImport("GARSteamManager", CharSet=CharSet.Unicode, ExactSpelling=false)]
    static extern bool SetStatINT(string statName, int statValue);

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern bool SteamInitialize();

    [DllImport("GARSteamManager", ExactSpelling=false)]
    static extern void SteamShutdown();

    public static bool Initialize()
    {
        // TODO Phase 4: GARSteamManager.dll and steam_api.dll are vendored x86 binaries
        // that fail to load in the x64 process (BadImageFormat 0x8007000B). The custom
        // GARSteamManager wrapper has no source in this repo, so we can't simply rebuild
        // it for x64. Disabled here so the boot log stays clean — every public method on
        // this class already gates on `IsInitialized`, so no P/Invoke fires while it is
        // false. Restore by migrating the ~25 P/Invokes to Steamworks.NET (recipe in
        // x64Migration/migration-plan-phase2.md "Deferred Final Step" appendix).
        IsInitialized = false;
        Log.Info("SteamManager disabled (x64 wrapper not yet available); achievements/stats/cloud-saves inactive");
        return false;
    }

    public static void Shutdown()
    {
        if (IsInitialized)
        {
            IsInitialized = false;
            SteamShutdown();
        }
    }
}