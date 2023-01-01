using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using SDGraphics;
using Ship_Game.UI;
using Ship_Game.Audio;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// All the necessary information needed for updating to a new release
/// </summary>
public record struct ReleaseInfo(string Name, string Version, string Changelog, string ZipUrl, string InstallerUrl);

/// <summary>
/// Automatic update checker that will show a popup panel
/// if a new version is available.
/// </summary>
public class AutoUpdateChecker : UIElementContainer
{
    readonly GameScreen Screen;
    TaskResult AsyncTask;

    public AutoUpdateChecker(GameScreen screen) : base(screen.RectF)
    {
        Screen = screen;
    }

    public override void OnAdded(UIElementContainer parent)
    {
        AsyncTask = Parallel.Run(() =>
        {
            GetVersionAsync("BlackBox", GlobalStats.VanillaDefaults.DownloadSite, isMod: false);
            if (GlobalStats.HasMod)
                GetVersionAsync(GlobalStats.ModName, GlobalStats.ActiveMod.Settings.DownloadSite, isMod: true);
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

            string text = "New Version\n" + info.Name;
            UILabel textLabel = base.Add(new UILabel(text, Fonts.Pirulen16));
            textLabel.TextAlign = TextAlign.HorizontalCenter;
            textLabel.AxisAlign = Align.CenterLeft;
            textLabel.SetLocalPos(132, 0);

            string portraitPath = isMod
                ? GlobalStats.ModPath + GlobalStats.ActiveMod?.Mod.IconPath
                : "Textures/Portraits/Human.dds";
            SubTexture portraitTex = updater.ContentManager.LoadTextureOrDefault(portraitPath);
            UIPanel portrait = base.Add(new UIPanel(LocalPos.Zero, new Vector2(62, 74), portraitTex));
            portrait.AxisAlign = Align.CenterLeft;
            portrait.SetLocalPos(48, 0);

            // pulsate alpha
            Anim().Time(0, 4, 1, 1).Alpha(new Range(0.5f, 1.0f)).Loop();
        }

        void Remove()
        {
            Updater.RemoveFromParent(); // remove AutoUpdater
            RemoveFromParent(); // remove self
        }

        void OnAutoUpdateClicked()
        {
            Log.LogEventStats(Log.GameEvent.AutoUpdateClicked);

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

                ToolTip.CreateTooltip(Info.Changelog);
            }
            return base.HandleInput(input);
        }
    }

    void NotifyLatestVersion(ReleaseInfo info, bool isMod)
    {
        Log.Write($"Latest Version: {info.Name} at {info.ZipUrl}");

        Screen.RunOnNextFrame(() =>
        {
            var notification = Add(new NewVersionPopup(this, info, isMod));
            float offset = isMod ? -notification.Height*0.9f : 0;
            float y = Screen.Height * 0.75f + offset;
            Vector2 endPos = new(10f, y);
            Vector2 startPos = new(endPos.X - (notification.Width + 20), y);

            notification.Anim() // slide in animation
                .FadeIn(delay:1.5f, duration:0.2f)
                .Pos(startPos, endPos)
                .Sfx(null, "sd_ui_notification_research_01")
            .ThenAnim() // followed by a small bounce
                .Time(0, 0.4f, 0.1f, 0.2f)
                .Pos(endPos, endPos-new Vector2(16,0));
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
                info = GetLatestVersionInfoGitHub(modName, downloadUrl, isMod);
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
        Log.Write($"AutoUpdater: latest  {latestVersion}");
        Log.Write($"AutoUpdater: current {currentVersion}");
        return string.CompareOrdinal(latestVersion, currentVersion) > 0;;
    }

    ReleaseInfo? GetLatestVersionInfoGitHub(string modName, string url, bool isMod)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask.IsCancelRequested)
            return null;

        dynamic latestRelease = new JavaScriptSerializer().DeserializeObject(jsonText);
        string name = latestRelease["name"];
        string tagName = latestRelease["tag_name"];
        string changelog = latestRelease["body"];
        string latestVersion = tagName.Split('-').Last();

        if (IsLatestVerNewer(latestVersion, isMod))
        {
            ReleaseInfo info = new(name, latestVersion, changelog, null, null);
            foreach (dynamic asset in latestRelease["assets"])
            {
                string assetName = asset["name"];
                if (assetName.EndsWith(".zip"))
                {
                    info.ZipUrl = asset["browser_download_url"];
                    return info;
                }
            }
        }
        return null;
    }

    ReleaseInfo? GetLatestVersionInfoBitBucket(string modName, string url, bool isMod)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask.IsCancelRequested)
            return null;

        dynamic downloads = new JavaScriptSerializer().DeserializeObject(jsonText);
        IEnumerable<dynamic> values = downloads["values"];
        dynamic value = values.First();
        string zipName = value["name"];
        string latestVersion = ParseVersionFromDownloadName(zipName);

        if (IsLatestVerNewer(latestVersion, isMod))
        {
            string downloadLink = value["links"]["self"]["href"];
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
        using WebClient wc = new();
        wc.UseDefaultCredentials = false;
        wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        wc.DownloadProgressChanged += OnProgressChanged;

        void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (cancellableTask.IsCancelRequested)
                ((WebClient)sender).CancelAsync();
        }

        try
        {
            var download = wc.DownloadStringTaskAsync(url);
            int timeoutMillis = (int)timeout.TotalMilliseconds;
            for (; timeoutMillis > 0; timeoutMillis -= 100)
            {
                if (download.Wait(100))
                    return download.Result;
                if (cancellableTask.IsCancelRequested)
                    break;
            }
        }
        catch (AggregateException e)
        {
            throw e.InnerException ?? e;
        }

        if (cancellableTask.IsCancelRequested)
            throw new OperationCanceledException("Download Request cancelled");
        throw new TimeoutException("Download Request timed out");
    }

    /// <summary>
    /// Downloads Zip from `url` into `localFolder`. The task can be cancelled by the user.
    /// Returns the path to the local file. Otherwise throws an exception on failure or cancellation.
    /// </summary>
    public static string DownloadZip(string url, string localFolder, TaskResult cancellableTask, 
                                     Action<int> onProgressPercent, TimeSpan timeout)
    {
        int lastPercent = -1;
        using WebClient wc = new();
        wc.UseDefaultCredentials = false;
        wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        wc.DownloadProgressChanged += OnProgressChanged;

        void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (cancellableTask.IsCancelRequested)
                ((WebClient)sender).CancelAsync();
            else if (onProgressPercent != null)
            {
                int newPercent = e.ProgressPercentage;
                if (lastPercent != newPercent)
                {
                    lastPercent = newPercent;
                    onProgressPercent(newPercent);
                }
            }
        }

        try
        {
            string localFile = Path.Combine(localFolder, Path.GetFileName(url));
            var download = wc.DownloadFileTaskAsync(url, localFile);

            int timeoutMillis = (int)timeout.TotalMilliseconds;
            for (; timeoutMillis > 0; timeoutMillis -= 100)
            {
                if (download.Wait(100))
                    return localFile;
                if (cancellableTask.IsCancelRequested)
                    break;
            }
        }
        catch (AggregateException e)
        {
            throw e.InnerException ?? e;
        }

        if (cancellableTask.IsCancelRequested)
            throw new OperationCanceledException("Download Request cancelled");
        throw new TimeoutException("Download Request timed out");
    }
}
