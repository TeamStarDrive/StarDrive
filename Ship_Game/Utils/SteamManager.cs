using System.Diagnostics;

namespace Ship_Game.Utils;

// Steamworks integration is parked while BlackBoxPlus is in public alpha.
// The vendored x86 GARSteamManager.dll + steam_api.dll were dropped (no x64
// source). When the project gets partner-backend access (or scope changes
// enough to justify it), wire Steamworks.NET against the existing AppID
// 220680 schema — recipe in x64Migration/migration-plan-phase2.md
// "Deferred Final Step" appendix. Until then, IsInitialized stays false and
// every public method no-ops; the rest of the codebase already gates on the
// flag.
public static class SteamManager
{
    public static bool IsInitialized;

    public static bool Initialize() => false;

    public static void Shutdown() { }

    public static bool RequestStats() => false;

    public static bool AchievementUnlocked(string achievementName) => false;

    public static void ActivateWebOverlay(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
