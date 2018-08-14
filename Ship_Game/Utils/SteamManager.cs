using Ship_Game;
using System;
using System.Runtime.InteropServices;
using System.Text;

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

	public static string steamName;

	static SteamManager()
	{
		isInitialized = false;
		overlayIsUp = false;
		statsInit = false;
		achievementsLoaded = false;
		SPDataLoaded = false;
		MPDataLoaded = false;
		weapon1Loaded = false;
		weapon2Loaded = false;
		weapon3Loaded = false;
		bankWeapon1Loaded = false;
		bankWeapon2Loaded = false;
		bankWeapon3Loaded = false;
		cgNEATSettingsLoaded = false;
		gameSettingsLoaded = false;
		keyMapLoaded = false;
		steamID = (ulong)0;
		steamName = "";
	}

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayAchievements();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayCommunity();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayFriends();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayOfficialGameGroup();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayPlayers();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlaySettings();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayToGARStore();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void ActivateOverlayWebPage(string url);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool FileExists(string pchFileName);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool GetAchievement(string pchName, ref bool pbAchieved);

	public static string GetFileOnRemoteStorage(string fileName)
	{
		int fileSize = GetFileSize(fileName);
		byte[] fileBytes = new byte[fileSize];
		GetFileOnRemoteStorage(fileName, fileBytes, fileSize);
		return (new UTF8Encoding()).GetString(fileBytes);
	}

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	private static extern IntPtr GetFileOnRemoteStorage(string fileName, byte[] data, int bytesToRead);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern int GetFileSize(string fileName);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern float GetStatFLOAT(string statName);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern int GetStatINT(string statName);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern ulong GetSteamID();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern IntPtr GetSteamName();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool IsOverlayEnabled();

	public static bool LoadAllAchievements()
	{
		return true;
	}

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool RequestCurrentStats();

	public static bool SaveAllGARPlayerStats()
	{
		SetStatINT("times_played (0-1)", GlobalStats.TimesPlayed);
		return SaveAllStatAndAchievementChanges();
	}

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool SaveAllStatAndAchievementChanges();

	public static bool SaveFileOnRemoteStorage(string fileName, string fileContents)
	{
		byte[] saveStrBytes = (new UTF8Encoding()).GetBytes(fileContents);
		return SaveFileOnRemoteStorage(fileName, saveStrBytes, (int)saveStrBytes.Length);
	}

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	private static extern bool SaveFileOnRemoteStorage(string fileName, byte[] data, int bytesToWrite);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool SetAchievement(string Achievementname);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void SetOverlayPosition(int corner);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool SetStatFLOAT(string statName, float statValue);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool SetStatINT(string statName, int statValue);

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern bool SteamInitialize();

	[DllImport("GARSteamManager", CharSet=CharSet.None, ExactSpelling=false)]
	public static extern void SteamShutdown();
}