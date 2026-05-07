using System;
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
public record struct ReleaseInfo(string Name, string Version, string Changelog, List<string> ZipUrls, string InstallerUrl);

/// <summary>
/// Automatic update checker that will show a popup panel
/// if a new version is available.
/// </summary>
public class AutoUpdateChecker : UIElementContainer
{
    readonly GameScreen Screen;
    readonly UIList Popups;
    TaskResult AsyncTask;

    public AutoUpdateChecker(GameScreen screen) : base(screen.RectF)
    {
        Screen = screen;
        Popups = AddList(new(10, Screen.Height * 0.6f));
    }

    public override void OnAdded(UIElementContainer parent)
    {
        AsyncTask = Parallel.Run(() =>
        {
            string vanillaUrl = GlobalStats.VanillaDefaults.DownloadSite;
            GetVersionAsync("BlackBox", vanillaUrl, isMod: false);

            string modUrl = GlobalStats.ActiveMod?.Settings.DownloadSite;
            if (modUrl != null && vanillaUrl != modUrl)
                GetVersionAsync(GlobalStats.ModName, modUrl, isMod: true);
        });
    }

    public override void OnRemoved()
    {
        AsyncTask.Cancel();
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
            mb.Accepted = () => Screen.ScreenManager.AddScreen(new AutoPatcher(Screen, Info, IsMod));
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
    
    string RegexExtractTeamAndRepo(string url, string pattern) => Regex.Match(url, pattern).Groups[1].Value.Trim('/');

    void GetVersionAsync(string modName, string downloadUrl, bool isMod)
    {
        if (downloadUrl.IsEmpty())
            return;
        try
        {
            ReleaseInfo? info = null;
            if (downloadUrl.Contains("github.com"))
            {
                // "https://github.com/TeamStarDrive/StarDrive/releases" --> "TeamStarDrive/StarDrive"
                string teamAndRepo = RegexExtractTeamAndRepo(downloadUrl, "\\/([\\w-]+\\/[\\w-]+)\\/releases");
                downloadUrl = $"https://api.github.com/repos/{teamAndRepo}/releases/latest";
                info = GetLatestVersionInfoGitHub(downloadUrl, isMod);
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

    static bool IsLatestVerNewer(string latestVersion, bool isMod)
    {
        string currentVersion = !isMod ? GlobalStats.Version.Split(' ').First()
                                       : GlobalStats.ActiveMod.Mod.Version;

        if (!isMod && !IsSameMajorVer(latestVersion, currentVersion))
            return false;

        Log.Write($"AutoUpdater: latest  {latestVersion}");
        Log.Write($"AutoUpdater: current {currentVersion}");
        return string.CompareOrdinal(latestVersion, currentVersion) > 0;
    }

    static bool IsSameMajorVer(string latestVersion, string currentVersion)
    {
        if (JoinVersionParts(latestVersion) != JoinVersionParts(currentVersion))
        {
            Log.Write($"AutoUpdater: Major version mismatch: (latest) " +
                $"{latestVersion} != {currentVersion} (current). Will not update");
            return false;
        }

        return true;

        string JoinVersionParts(string version)
        {
            return string.Join(".", version.Split('.').Take(2));
        }
    }


    ReleaseInfo? GetLatestVersionInfoGitHub(string url, bool isMod)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask is { IsCancelRequested: true })
            return null;

        using JsonDocument doc = JsonDocument.Parse(jsonText);
        JsonElement latestRelease = doc.RootElement;
        string name = latestRelease.GetProperty("name").GetString();
        string tagName = latestRelease.GetProperty("tag_name").GetString();
        string changelog = latestRelease.GetProperty("body").GetString();
        string latestVersion = tagName.Split('-').FindMax(s => s.Count(c => c == '.')); // part-v1.2.4-withmostdots

        if (IsLatestVerNewer(latestVersion, isMod))
        {
            ReleaseInfo info = new(name, latestVersion, changelog, null, null);
            info.ZipUrls = new List<string>();
            foreach (JsonElement asset in latestRelease.GetProperty("assets").EnumerateArray())
            {
                string assetName = asset.GetProperty("name").GetString();
                if (assetName.EndsWith(".zip"))
                    info.ZipUrls.Add(asset.GetProperty("browser_download_url").GetString());
            }

            return info;
        }
        return null;
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
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (cancellableTask.IsCancelRequested) { cts.Cancel(); return; }
                    await Task.Delay(100, cts.Token).ContinueWith(_ => { });
                }
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
