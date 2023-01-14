using System;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CA1401

namespace Ship_Game.Utils;

public static class SteamManager
{
    public static bool isInitialized;
    public static bool overlayIsUp;
    public static bool statsInit;
    public static bool achievementsLoaded;
    public static bool SPDataLoaded;
    public static bool MPDataLoaded;
    public static bool weapon1Loaded;
    public static bool weapon2Loaded;
    public static bool weapon3Loaded;
    public static bool bankWeapon1Loaded;
    public static bool bankWeapon2Loaded;
    public static bool bankWeapon3Loaded;
    public static bool cgNEATSettingsLoaded;
    public static bool gameSettingsLoaded;
    public static bool keyMapLoaded;
    public static ulong steamID;
    public static string steamName = "";

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayAchievements();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayCommunity();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayFriends();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayOfficialGameGroup();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayPlayers();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlaySettings();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void ActivateOverlayToGARStore();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern void ActivateOverlayWebPage(string url);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool FileExists(string pchFileName);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool GetAchievement(string pchName, ref bool pbAchieved);

    public static string GetFileOnRemoteStorage(string fileName)
    {
        int fileSize = GetFileSize(fileName);
        byte[] fileBytes = new byte[fileSize];
        GetFileOnRemoteStorage(fileName, fileBytes, fileSize);
        return (new UTF8Encoding()).GetString(fileBytes);
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern IntPtr GetFileOnRemoteStorage(string fileName, byte[] data, int bytesToRead);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern int GetFileSize(string fileName);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern float GetStatFLOAT(string statName);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern int GetStatINT(string statName);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern ulong GetSteamID();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern IntPtr GetSteamName();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool IsOverlayEnabled();

    public static bool LoadAllAchievements()
    {
        return true;
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern bool RequestCurrentStats();

    public static bool SaveAllGARPlayerStats()
    {
        SetStatINT("times_played (0-1)", GlobalStats.TimesPlayed);
        return SaveAllStatAndAchievementChanges();
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern bool SaveAllStatAndAchievementChanges();

    public static bool SaveFileOnRemoteStorage(string fileName, string fileContents)
    {
        byte[] saveStrBytes = (new UTF8Encoding()).GetBytes(fileContents);
        return SaveFileOnRemoteStorage(fileName, saveStrBytes, saveStrBytes.Length);
    }

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool SaveFileOnRemoteStorage(string fileName, byte[] data, int bytesToWrite);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern bool SetAchievement(string Achievementname);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern void SetOverlayPosition(int corner);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool SetStatFLOAT(string statName, float statValue);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    static extern bool SetStatINT(string statName, int statValue);

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern bool SteamInitialize();

    [DllImport("GARSteamManager", CharSet=CharSet.Ansi, ExactSpelling=false)]
    public static extern void SteamShutdown();
}