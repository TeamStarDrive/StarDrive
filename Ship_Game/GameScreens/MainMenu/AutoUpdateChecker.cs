using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using SDGraphics;
using Ship_Game.UI;
using Ship_Game.Audio;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// All the necessary information needed for updating to a new release
/// </summary>
public record struct ReleaseInfo(string Name, string Version, string Changelog, List<string> ZipUrls, string InstallerUrl)
{
    public long DownloadBytes { get; init; }
}

/// <summary>
/// Automatic update checker that will show a popup panel
/// if a new version is available.
/// </summary>
public class AutoUpdateChecker : UIElementContainer
{
    readonly GameScreen Screen;
    readonly UIList Popups;
    TaskResult AsyncTask;
    bool MajorReleaseUpgradeNotified;

    public AutoUpdateChecker(GameScreen screen) : base(screen.RectF)
    {
        Screen = screen;
        Popups = AddList(new(10, Screen.Height * 0.6f));
    }

    public override void OnAdded(UIElementContainer parent)
    {
        // Resume mode: AutoPatcher is about to apply the exact version we'd
        // otherwise notify about, so skip the scan entirely. Without this the
        // "New Version!" popup slides in from the left at the same time the
        // patcher chrome appears, which is both noisy and confusing — the user
        // has already accepted UAC and is watching the patch apply.
        if (Program.ResumePatchVersion.NotEmpty())
        {
            Log.Write("AutoUpdater: resume mode — skipping version scan");
            return;
        }

        AsyncTask = Parallel.Run(() =>
        {
            string vanillaUrl = GlobalStats.VanillaDefaults.DownloadSite;
            GetVersionAsync("BlackBox", vanillaUrl, isMod: false);

            // If a major vanilla upgrade is pending, the mod popup is noise —
            // user must upgrade vanilla first; mod compatibility with the new
            // major isn't guaranteed and the mod patch may be moot.
            if (MajorReleaseUpgradeNotified)
            {
                Log.Write("AutoUpdater: Major vanilla release upgrade pending, skipping mod version check");
                return;
            }

            string modUrl = GlobalStats.ActiveMod?.Settings.DownloadSite;
            if (modUrl != null && vanillaUrl != modUrl)
                GetVersionAsync(GlobalStats.ModName, modUrl, isMod: true);
        });
    }

    public override void OnRemoved()
    {
        // AsyncTask is null when we bailed out of OnAdded (e.g. resume mode),
        // so make the cancel call null-safe — a hot-reload of MainMenus.yaml
        // (which patches do) triggers RemoveAll on this component and would
        // otherwise NRE here, tearing down MainMenuScreen mid-unload.
        AsyncTask?.Cancel();
    }

    class NewVersionPopup : UIPanel
    {
        GameScreen Screen => Updater.Screen;
        readonly AutoUpdateChecker Updater;
        readonly ReleaseInfo Info;
        readonly bool IsMod;

        public NewVersionPopup(AutoUpdateChecker updater, in ReleaseInfo info, bool isMod)
            : base(updater.ContentManager.LoadTextureOrDefault("Textures/MMenu/popup_banner_small.png"))
        {
            Updater = updater;
            Info = info;
            IsMod = isMod;

            string text = "New Version!\n" + info.Name;
            UILabel textLabel = base.Add(new UILabel(text, Fonts.Pirulen16));
            textLabel.TextAlign = TextAlign.HorizontalCenter;
            textLabel.AxisAlign = Align.CenterLeft;
            textLabel.SetLocalPos(125, 0);
            UILabel textLabelClick = base.Add(new UILabel("(click to update)", Fonts.Pirulen12));
            textLabelClick.TextAlign = TextAlign.HorizontalCenter;
            textLabelClick.AxisAlign = Align.CenterLeft;
            textLabelClick.SetLocalPos(125, 30);

            SubTexture portraitTex = isMod 
                ? GlobalStats.ActiveMod?.LoadPortrait(Screen)
                : updater.ContentManager.LoadTextureOrDefault("Textures/Portraits/Human.dds");

            UIPanel portrait = base.Add(new UIPanel(new LocalPos(48,0), new(62, 74), portraitTex));
            portrait.AxisAlign = Align.CenterLeft;

            // pulsate alpha
            Anim().Time(0, 4, 1, 1).Alpha(new Range(0.5f, 1.0f)).Loop();
        }

        void Remove()
        {
            var elements = Updater.Popups.GetElements();
            int index = elements.IndexOf(this);
            RemoveFromParent(); // remove self

            // remove AutoUpdater if all popups dismissed
            if (elements.Count(e => e is NewVersionPopup) == 0)
            {
                Updater.RemoveFromParent();
            }
            else // animate all other popups to shift up
            {
                for (int i = index; i < elements.Count; ++i)
                {
                    UIElementV2 e = elements[i];
                    Vector2 endPos = new(e.X, e.Y - Height - Updater.Popups.Padding.Y);
                    e.SlideIn(e.Pos, endPos, 0.15f).Bounce(new(0,8));
                }
            }
        }

        void OnAutoUpdateClicked()
        {
            //Log.LogEventStats(Log.GameEvent.AutoUpdateClicked);
            Remove();
            var mb = new MessageBoxScreen(Screen, "This will automatically update to the latest version. Continue?", 10f);
            mb.Accepted = () => Screen.ScreenManager.AddScreen(new AutoPatcher(Screen, Info, IsMod ? GlobalStats.ModPath : null));
            Screen.ScreenManager.AddScreen(mb);
        }

        public override bool HandleInput(InputState input)
        {
            bool hovering = HitTest(input.CursorPosition);
            GameCursors.SetCurrentCursor(hovering ? GameCursors.AggressiveNav : GameCursors.Regular);

            if (hovering)
            {
                if (input.LeftMouseClick)
                {
                    GameAudio.AffirmativeClick();
                    OnAutoUpdateClicked();
                    return true;
                }
                if (input.RightMouseClick)
                {
                    GameAudio.ButtonMouseOver();
                    Remove();
                    return true;
                }

                ToolTip.CreateTooltip(Info.Changelog, "", null, maxWidth:720);
            }
            return base.HandleInput(input);
        }
    }

    // Fires only on cross-major-version mismatch (e.g., 1.51 → 1.60).
    // Click opens upgrade-url.txt in browser and exits the game so the
    // user can run the new installer. No in-game patcher path — major
    // bumps can't be applied as a file-drop patch.
    class MajorUpgradeAvailablePopup : UIPanel
    {
        readonly string Url;

        public MajorUpgradeAvailablePopup(AutoUpdateChecker updater, string displayLabel, string url)
            : base(updater.ContentManager.LoadTextureOrDefault("Textures/MMenu/popup_banner_small.png"))
        {
            Url = url;

            UILabel headline = base.Add(new UILabel("Major Release Available!", Fonts.Pirulen16, Microsoft.Xna.Framework.Color.Red));
            headline.TextAlign = TextAlign.HorizontalCenter;
            headline.AxisAlign = Align.CenterLeft;
            headline.SetLocalPos(20, -20);

            UILabel version = base.Add(new UILabel(displayLabel, Fonts.Pirulen12));
            version.TextAlign = TextAlign.HorizontalCenter;
            version.AxisAlign = Align.CenterLeft;
            version.SetLocalPos(20, 2);

            UILabel hint = base.Add(new UILabel("(click to download. game will close)", Fonts.Pirulen12));
            hint.TextAlign = TextAlign.HorizontalCenter;
            hint.AxisAlign = Align.CenterLeft;
            hint.SetLocalPos(20, 22);

            // pulsate alpha (matches NewVersionPopup visual cue)
            Anim().Time(0, 4, 1, 1).Alpha(new Range(0.5f, 1.0f)).Loop();
        }

        void OnUpgradeClicked()
        {
            Log.Write($"AutoUpdater: User clicked MajorUpgradeAvailablePopup → opening {Url} and exiting");
            try
            {
                Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Log.Warning($"AutoUpdater: failed to launch browser for {Url}: {e.Message}");
            }
            StarDriveGame.Instance.Exit();
        }

        public override bool HandleInput(InputState input)
        {
            bool hovering = HitTest(input.CursorPosition);
            GameCursors.SetCurrentCursor(hovering ? GameCursors.AggressiveNav : GameCursors.Regular);

            if (hovering)
            {
                if (input.LeftMouseClick)
                {
                    GameAudio.AffirmativeClick();
                    OnUpgradeClicked();
                    return true;
                }
                if (input.RightMouseClick)
                {
                    GameAudio.ButtonMouseOver();
                    RemoveFromParent();
                    return true;
                }
            }
            return base.HandleInput(input);
        }
    }

    void NotifyLatestVersion(ReleaseInfo info, bool isMod)
    {
        Log.Write($"Latest Version: {info.Name} at {info.ZipUrls}");

        Screen.RunOnNextFrame(() =>
        {
            var notification = Popups.Add(new NewVersionPopup(this, info, isMod));
            Popups.PerformLayout();

            Vector2 endPos = notification.Pos;
            Vector2 startPos = new(endPos.X - (notification.Width + 20), endPos.Y);

            float delay = isMod ? 2f : 1.5f;
            notification.SlideIn(startPos, endPos, 0.2f, delay:delay)
                .Sfx(null, "sd_ui_notification_research_01")
                .Bounce(new(-16,0));
        });
    }
    
    static string RegexExtractTeamAndRepo(string url, string pattern) => Regex.Match(url, pattern).Groups[1].Value.Trim('/');

    // "https://github.com/TeamStarDrive/StarDrive/releases" --> "https://api.github.com/repos/TeamStarDrive/StarDrive/releases"
    public static string GitHubReleasesApi(string downloadUrl)
    {
        string teamAndRepo = RegexExtractTeamAndRepo(downloadUrl, "\\/([\\w-]+\\/[\\w-]+)\\/releases");
        return $"https://api.github.com/repos/{teamAndRepo}/releases";
    }

    /// <summary>
    /// The newest release of a mod for this BlackBox line, whatever version is installed.
    /// Null if the repo has no release for this line. Throws on network errors.
    /// </summary>
    public static ReleaseInfo? GetLatestModRelease(string downloadUrl, TaskResult cancellableTask)
    {
        string apiBase = GitHubReleasesApi(downloadUrl);
        Version currentLine = TryParseCurrentVanillaLine();
        string url = currentLine != null ? apiBase : apiBase + "/latest";
        string jsonText = DownloadWithCancel(url, cancellableTask, timeout: TimeSpan.FromSeconds(30));

        using JsonDocument doc = JsonDocument.Parse(jsonText);
        JsonElement release = doc.RootElement;
        if (release.ValueKind == JsonValueKind.Array)
        {
            bool ModLinePredicate(Version v) => v.Major == currentLine.Major && v.Minor == currentLine.Minor;
            if (!TrySelectMaxVersionRelease(doc.RootElement, ModLinePredicate, out release, out _))
                return null;
        }

        ReleaseInfo info = ToReleaseInfo(release);
        if (info.ZipUrls.Count == 0)
        {
            Log.Warning($"AutoUpdater: {downloadUrl} release {info.Version} has no zip to download");
            return null;
        }
        return info;
    }

    internal static ReleaseInfo ToReleaseInfo(JsonElement release)
    {
        string tagName = release.GetProperty("tag_name").GetString();
        var zipUrls = new List<string>();
        long downloadBytes = 0;
        // Sort by asset name. Chunked patches use a `001-`, `002-` numeric prefix
        // (MakeInstaller.py); GitHub's API returns assets in upload order, which
        // currently happens to be alphabetical because patch-build.yml uses
        // Get-ChildItem (alpha default) — but that's incidental. Sort here so a
        // future workflow change or manual re-upload via the UI can't reorder
        // chunks and corrupt the concatenated archive.
        var zipAssets = release.GetProperty("assets").EnumerateArray()
            .Where(a => a.GetProperty("name").GetString().EndsWith(".zip"))
            .OrderBy(a => a.GetProperty("name").GetString(), StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement asset in zipAssets)
        {
            zipUrls.Add(asset.GetProperty("browser_download_url").GetString());
            if (asset.TryGetProperty("size", out JsonElement size) && size.ValueKind == JsonValueKind.Number)
                downloadBytes += size.GetInt64();
        }

        return new(release.GetProperty("name").GetString(), ExtractVersionPartFromTag(tagName),
                   release.GetProperty("body").GetString(), zipUrls, null) { DownloadBytes = downloadBytes };
    }

    void GetVersionAsync(string modName, string downloadUrl, bool isMod)
    {
        if (downloadUrl.IsEmpty())
            return;
        try
        {
            ReleaseInfo? info = null;
            if (downloadUrl.Contains("github.com"))
            {
                string apiBase = GitHubReleasesApi(downloadUrl);

                // Both vanilla and mods now hit /releases (array of all
                // published) and filter by vanilla's current major.minor line.
                //
                // Vanilla: pick the highest-version release. If its major.minor
                // differs from the current install, route to the cross-major
                // popup (file-gated by game/upgrade-url.txt). Otherwise treat
                // it as an in-game patch candidate. This decouples the release
                // line from GitHub's "Set as latest" flag — a hotfix can be
                // published not-as-latest without breaking discovery, and the
                // latest flag stays free to point at a legacy line.
                //
                // Mods: pick the highest-version release whose tag matches
                // vanilla's major.minor (mods now version-align with vanilla
                // as v<major>.<minor>.NNNN). Pre-releases skipped via
                // TrySelectMaxVersionRelease. Fallback: if the installed mod
                // version doesn't parse to vanilla's line (legacy free-form
                // version), any matching mod release is promoted as an update
                // so users on old mod versions get a path forward.
                //
                // Dev builds (currentLine == null) fall back to /releases/latest
                // for both — degraded but functional.
                Version currentLine = TryParseCurrentVanillaLine();
                if (currentLine != null)
                    info = GetLatestVersionInfoGitHub(apiBase, isMod, currentLine);
                else
                    info = GetLatestVersionInfoGitHub(apiBase + "/latest", isMod, currentLine: null);
            }
            else if (downloadUrl.Contains("bitbucket.org"))
            {
                // "https://bitbucket.org/codegremlins/combined-arms/downloads/" --> "codegremlins/combined-arms"
                string teamAndRepo = RegexExtractTeamAndRepo(downloadUrl, "\\/([\\w-]+\\/[\\w-]+)\\/downloads");
                downloadUrl = $"https://api.bitbucket.org/2.0/repositories/{teamAndRepo}/downloads";
                info = GetLatestVersionInfoBitBucket(modName, downloadUrl, isMod);
            }
            else
            {
                Log.Warning($"AutoUpdater: unsupported download url {downloadUrl}");
            }

            if (info != null)
            {
                NotifyLatestVersion(info.Value, isMod);
            }
        }
        catch (Exception e)
        {
            // can easily fail due to network issues etc, shouldn't be a big deal
            Log.Warning($"GetVersionAsync {modName} {downloadUrl} failed: {e.Message}");
        }
    }

    bool IsLatestVerNewer(string latestVersion, bool isMod, string codename = null)
    {
        string currentVersion = !isMod ? GlobalStats.Version.Split(' ').First()
                                       : GlobalStats.ActiveMod.Mod.Version;

        Log.Write($"AutoUpdater: latest  {latestVersion}");
        Log.Write($"AutoUpdater: current {currentVersion}");

        // Mods are now version-aligned with vanilla (v<major>.<minor>.NNNN).
        // Strip the optional 'v' prefix and compare numerically. Fallback:
        // when the installed mod version doesn't parse to vanilla's line
        // (legacy free-form Version string, or a different major.minor),
        // promote the candidate as an update so users on old mod versions
        // get a one-click path to the new aligned line.
        if (isMod)
            return IsModLatestNewer(latestVersion, currentVersion);

        switch (ClassifyVanillaUpdate(latestVersion, currentVersion))
        {
            case UpdateAvailability.Unparseable:
                Log.Warning($"AutoUpdater: unparseable version (latest='{latestVersion}', current='{currentVersion}'), skipping");
                return false;
            case UpdateAvailability.None:
                return false;
            case UpdateAvailability.CrossMajor:
                Log.Write($"AutoUpdater: Cross-major upgrade {currentVersion} -> {latestVersion}");
                NotifyMajorUpgradeIfConfigured(latestVersion, codename);
                return false;
            case UpdateAvailability.InGamePatch:
                return true;
            default:
                return false;
        }
    }

    // Pure: decide whether a candidate mod release supersedes the installed
    // mod version. Both sides are stripped of an optional 'v' prefix and
    // parsed via System.Version. When both parse and share major.minor, do
    // a direct numeric compare. Otherwise fallback: promote the candidate
    // (the installed mod is on a legacy free-form Version or a different
    // major.minor line and should be upgraded to the new aligned release).
    public static bool IsModLatestNewer(string latestVersion, string currentVersion)
    {
        string latest = StripVPrefix(latestVersion);
        if (!Version.TryParse(latest, out var newV))
        {
            // Candidate itself unparseable — should not happen post
            // TrySelectMaxVersionRelease but keep the bailout for safety.
            Log.Warning($"AutoUpdater: mod latest version '{latestVersion}' unparseable, skipping");
            return false;
        }

        string current = StripVPrefix(currentVersion);
        if (Version.TryParse(current, out var curV)
            && curV.Major == newV.Major && curV.Minor == newV.Minor)
        {
            return newV > curV;
        }

        Log.Write($"AutoUpdater: mod fallback — current '{currentVersion}' doesn't align with latest '{latestVersion}', promoting");
        return true;
    }

    public enum UpdateAvailability
    {
        None,         // latest <= current
        InGamePatch,  // same major.minor, latest > current — file-drop patch
        CrossMajor,   // different major.minor, latest > current — fresh installer
        Unparseable,  // either version string failed Version.TryParse
    }

    // Pure: classify a vanilla-line update purely from version strings.
    // Parses both via System.Version so 1.51.15118 < 1.60.00000 sorts numerically
    // (string ordinal compare misorders these and surfaces older Mars-line tags
    // pinned alongside Jupiter on the GitHub Releases page as "newer").
    public static UpdateAvailability ClassifyVanillaUpdate(string latestVersion, string currentVersion)
    {
        if (!Version.TryParse(latestVersion, out var latest) ||
            !Version.TryParse(currentVersion, out var current))
            return UpdateAvailability.Unparseable;
        if (latest <= current)
            return UpdateAvailability.None;
        if (latest.Major != current.Major || latest.Minor != current.Minor)
            return UpdateAvailability.CrossMajor;
        return UpdateAvailability.InGamePatch;
    }

    // Schedules a top-left MajorUpgradeAvailablePopup if game/upgrade-url.txt
    // is present with a valid URL. Absence of the file is the "stay silent"
    // signal — preserves the historical log-only behavior on major-mismatch.
    void NotifyMajorUpgradeIfConfigured(string latestVersion, string codename)
    {
        string url = TryReadUpgradeUrl();
        if (url == null)
            return;

        MajorReleaseUpgradeNotified = true;
        string displayLabel = BuildMajorUpgradeDisplayLabel(latestVersion, codename);
        Log.Write($"AutoUpdater: Major release {latestVersion} ({displayLabel}) available at {url}");
        Screen.RunOnNextFrame(() =>
        {
            var popup = Add(new MajorUpgradeAvailablePopup(this, displayLabel, url));
            popup.SetLocalPos(10, 30);
        });
    }

    // "1.60.00002" + "Jupiter" -> "Jupiter 1.60". Codename comes from the
    // GitHub tag (`jupiter-release-1.60` -> "Jupiter"); when absent we fall
    // back to "BlackBox 1.60" so the popup still reads sensibly. Full build
    // number stays in Log.Write for diagnostics — only the user-facing label
    // is trimmed to major.minor.
    public static string BuildMajorUpgradeDisplayLabel(string latestVersion, string codename)
    {
        string majorMinor = string.Join(".", latestVersion.Split('.').Take(2));
        return codename.NotEmpty() ? $"{codename} {majorMinor}" : $"BlackBox {majorMinor}";
    }

    // "jupiter-release-1.60"  -> "Jupiter"
    // "mars-patch-1.51.15118" -> "Mars"
    // null/empty/non-alpha first segment -> null (caller falls back to "BlackBox")
    public static string ExtractCodenameFromTag(string tagName)
    {
        if (tagName.IsEmpty())
            return null;
        string first = tagName.Split('-').FirstOrDefault();
        if (first.IsEmpty() || !first.All(char.IsLetter))
            return null;
        return char.ToUpperInvariant(first[0]) + first.Substring(1).ToLowerInvariant();
    }

    static string TryReadUpgradeUrl()
    {
        const string path = "upgrade-url.txt";
        try
        {
            if (!File.Exists(path))
                return null;

            foreach (string line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                    return trimmed;
            }
            return null;
        }
        catch (Exception e)
        {
            Log.Warning($"AutoUpdater: failed to read {path}: {e.Message}");
            return null;
        }
    }


    // Parses the current vanilla install version (e.g. "1.60.00000 (abcdef1)" -> Version 1.60.0).
    // Returns null on dev builds / unparseable strings — caller falls back to /releases/latest.
    static Version TryParseCurrentVanillaLine()
    {
        string currentVer = GlobalStats.Version.Split(' ').First();
        if (Version.TryParse(currentVer, out var v))
            return v;
        Log.Warning($"AutoUpdater: cannot parse current vanilla version '{currentVer}' — falling back to /releases/latest");
        return null;
    }

    static string ExtractVersionPartFromTag(string tagName)
        => tagName.Split('-').FindMax(s => s.Count(c => c == '.')); // part-v1.2.4-withmostdots

    // Strip optional leading 'v'/'V' so Version.TryParse can consume tags
    // like `v1.60.0014` (mod convention: v<major>.<minor>.NNNN).
    static string StripVPrefix(string s)
    {
        if (s.IsEmpty()) return s;
        char first = s[0];
        return (first == 'v' || first == 'V') ? s.Substring(1) : s;
    }

    ReleaseInfo? GetLatestVersionInfoGitHub(string url, bool isMod, Version currentLine)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask is { IsCancelRequested: true })
            return null;

        using JsonDocument doc = JsonDocument.Parse(jsonText);

        // /releases returns a JSON array; /releases/latest returns a single object.
        // GetVersionAsync picks the URL based on whether a current install line
        // could be parsed, so handle both shapes here.
        JsonElement latestRelease;
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            if (currentLine == null)
            {
                Log.Warning("AutoUpdater: /releases array returned but no currentLine — refusing to guess");
                return null;
            }

            if (isMod)
            {
                // Mods filter to vanilla's major.minor line upfront. No
                // cross-major popup for mods — the popup is vanilla-only,
                // and a mod release that doesn't match the current vanilla
                // line just isn't relevant (it's for a different BlackBox
                // major, the user must upgrade vanilla first).
                bool ModLinePredicate(Version v) =>
                    v.Major == currentLine.Major && v.Minor == currentLine.Minor;
                if (!TrySelectMaxVersionRelease(doc.RootElement, ModLinePredicate, out JsonElement modBest, out _))
                {
                    Log.Write($"AutoUpdater: no mod releases match vanilla line {currentLine.Major}.{currentLine.Minor}");
                    return null;
                }
                latestRelease = modBest;
            }
            else
            {
                if (!TrySelectMaxVersionRelease(doc.RootElement, predicate: null, out JsonElement maxOverall, out Version maxOverallVer))
                {
                    Log.Write("AutoUpdater: /releases empty or no parseable versions");
                    return null;
                }
                // Cross-major: highest published release is on a different
                // line than current install. Route through the popup
                // (file-gated by game/upgrade-url.txt) and skip the in-game
                // patcher — major bumps can't be applied as a file-drop.
                if (maxOverallVer.Major != currentLine.Major || maxOverallVer.Minor != currentLine.Minor)
                {
                    string overallTag = maxOverall.GetProperty("tag_name").GetString();
                    string overallVersion = ExtractVersionPartFromTag(overallTag);
                    string overallCodename = ExtractCodenameFromTag(overallTag);
                    string currentVer = GlobalStats.Version.Split(' ').First();
                    if (ClassifyVanillaUpdate(overallVersion, currentVer) == UpdateAvailability.CrossMajor)
                    {
                        Log.Write($"AutoUpdater: cross-major upgrade detected: {currentVer} -> {overallVersion}");
                        NotifyMajorUpgradeIfConfigured(overallVersion, overallCodename);
                    }
                    return null;
                }
                // Same line: max-overall is also the max-in-line, use it as
                // the in-game patch candidate.
                latestRelease = maxOverall;
            }
        }
        else
        {
            latestRelease = doc.RootElement;
        }

        string tagName = latestRelease.GetProperty("tag_name").GetString();
        string latestVersion = ExtractVersionPartFromTag(tagName);
        string codename = ExtractCodenameFromTag(tagName);

        if (IsLatestVerNewer(latestVersion, isMod, codename))
            return ToReleaseInfo(latestRelease);
        return null;
    }

    // Pick the highest-versioned release on a /releases array. Version-based
    // (not date-based) so a security hotfix published to an older line can't
    // trick a newer install into "downgrading" — only an actual higher Version
    // wins. The version segment is the dot-richest split-on-'-' part of the
    // tag. Pass a `predicate` to scope to a release line (e.g. major.minor
    // match); pass null to scan all releases.
    //
    // Pre-releases are skipped — they're for staging/QA and shouldn't
    // auto-distribute via the in-line patcher or fire cross-major popups.
    // Matches GitHub's /releases/latest semantic (which also excludes
    // pre-releases). To force-rollout a patch to all users, publish it
    // without the pre-release flag.
    public static bool TrySelectMaxVersionRelease(JsonElement releases, Func<Version, bool> predicate,
                                                  out JsonElement best, out Version bestVer)
    {
        best = default;
        bestVer = null;
        foreach (JsonElement release in releases.EnumerateArray())
        {
            if (release.TryGetProperty("prerelease", out JsonElement prereleaseEl)
                && prereleaseEl.ValueKind == JsonValueKind.True)
                continue;
            if (!release.TryGetProperty("tag_name", out JsonElement tagEl))
                continue;
            string tag = tagEl.GetString();
            if (tag.IsEmpty())
                continue;
            string verPart = StripVPrefix(ExtractVersionPartFromTag(tag));
            if (verPart.IsEmpty() || !Version.TryParse(verPart, out var v))
                continue;
            if (predicate != null && !predicate(v))
                continue;
            if (bestVer == null || v > bestVer)
            {
                bestVer = v;
                best = release;
            }
        }
        return bestVer != null;
    }

    ReleaseInfo? GetLatestVersionInfoBitBucket(string modName, string url, bool isMod)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask is { IsCancelRequested: true })
            return null;

        using JsonDocument doc = JsonDocument.Parse(jsonText);
        JsonElement value = doc.RootElement.GetProperty("values").EnumerateArray().First();
        string zipName = value.GetProperty("name").GetString();
        string latestVersion = ParseVersionFromDownloadName(zipName);

        if (IsLatestVerNewer(latestVersion, isMod))
        {
            List<string> downloadLink = [value.GetProperty("links").GetProperty("self").GetProperty("href").GetString()];
            string prettyName = $"{modName} {latestVersion}";
            return new(prettyName, latestVersion, zipName, downloadLink, null);
        }
        return null;
    }

    static string ParseVersionFromDownloadName(string name)
    {
        if (name.Contains("_v") || name.Contains("-v"))
        {
            foreach (string part in name.Split('_','-'))
                if (part.Length >= 2 && part[0] == 'v' && char.IsDigit(part[1]))
                    return part.Substring(1);
        }

        if (name.Contains("CombinedArms"))
            return name.Replace("CombinedArms", "").Split('_')[0];

        // fallback, first substring which contains only digits and '.'
        foreach (string part in name.Split('_','-'))
            if (part.All(c => char.IsDigit(c) || c == '.'))
                return part;
        return null;
    }

    // Download utility which can be cancel itself via another `cancellableTask`
    public static string DownloadWithCancel(string url, TaskResult cancellableTask, TimeSpan timeout)
    {
        using var cts = LinkCancellation(cancellableTask, timeout);
        using HttpClient http = CreateHttpClient();
        try
        {
            return http.GetStringAsync(url, cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            if (cancellableTask is { IsCancelRequested: true })
                throw new OperationCanceledException("Download Request cancelled");
            throw new TimeoutException("Download Request timed out");
        }
    }

    /// <summary>
    /// Downloads Zip from `url` into `localFolder`. The task can be cancelled by the user.
    /// Returns the path to the local file. Otherwise throws an exception on failure or cancellation.
    /// If there are several urls, they will be downloaded sequentially.
    /// </summary>
    public static List<string> DownloadZip(List<string> urls, string localFolder, TaskResult cancellableTask,
                                     Action<int> onProgressPercent, TimeSpan timeout)
    {
        using var cts = LinkCancellation(cancellableTask, timeout);
        using HttpClient http = CreateHttpClient();
        List<string> localFiles = new(urls.Count);
        try
        {
            foreach (string url in urls)
            {
                string localFile = Path.Combine(localFolder, Path.GetFileName(url));
                DownloadFileWithProgress(http, url, localFile, onProgressPercent, cts.Token)
                    .GetAwaiter().GetResult();
                localFiles.Add(localFile);
            }
        }
        catch (OperationCanceledException)
        {
            if (cancellableTask is { IsCancelRequested: true })
                throw new OperationCanceledException("Download Request cancelled");
            throw new TimeoutException("Download Request timed out");
        }
        return localFiles;
    }

    static HttpClient CreateHttpClient()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36");
        return http;
    }

    // Bridges the legacy TaskResult-based cancellation onto a CancellationToken.
    // Polls IsCancelRequested at 100ms granularity so the existing Cancel-button UX
    // still works without changing its surface.
    static CancellationTokenSource LinkCancellation(TaskResult cancellableTask, TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        if (cancellableTask != null)
        {
            // Snapshot the token before the async hop. Caller wraps cts in `using`; once
            // disposed, cts.Token getter throws ObjectDisposedException — and because this
            // is fire-and-forget, the exception used to surface as UnobservedTaskException
            // at finalization. The CancellationToken struct itself is safe post-Dispose.
            // IsComplete in the loop guard ensures the task exits naturally after a
            // successful download (Dispose alone doesn't set IsCancellationRequested).
            CancellationToken token = cts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && !cancellableTask.IsComplete)
                    {
                        if (cancellableTask.IsCancelRequested)
                        {
                            try { cts.Cancel(); } catch (ObjectDisposedException) { }
                            return;
                        }
                        await Task.Delay(100, token);
                    }
                }
                catch (OperationCanceledException) { /* expected on timeout/cancel */ }
                catch (ObjectDisposedException)    { /* expected if caller disposed cts */ }
            });
        }
        return cts;
    }

    static async Task DownloadFileWithProgress(HttpClient http, string url, string localFile,
                                               Action<int> onProgressPercent, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        long? total = response.Content.Headers.ContentLength;
        await using Stream src = await response.Content.ReadAsStreamAsync(ct);
        await using FileStream dst = File.Create(localFile);
        byte[] buffer = new byte[81920];
        long received = 0;
        int lastPercent = -1;
        int read;
        while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, read), ct);
            received += read;
            if (onProgressPercent != null && total is > 0)
            {
                int pct = (int)(received * 100 / total.Value);
                if (pct != lastPercent) { lastPercent = pct; onProgressPercent(pct); }
            }
        }
    }
}
