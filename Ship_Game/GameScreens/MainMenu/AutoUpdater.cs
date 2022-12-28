using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.UI;
using Ship_Game.Audio;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// All the necessary information needed for updating to a new release
/// </summary>
public record struct ReleaseInfo(string Name, string Version, string ZipUrl, string InstallerUrl);

/// <summary>
/// Automatic update checker that will show a popup panel
/// if a new version is available.
/// </summary>
public class AutoUpdater : UIElementContainer
{
    readonly GameScreen Screen;
    TaskResult AsyncTask;

    public AutoUpdater(GameScreen screen) : base(screen.RectF)
    {
        Screen = screen;
    }

    public override void OnAdded(UIElementContainer parent)
    {
        AsyncTask = Parallel.Run(GetVersionAsync);
    }

    public override void OnRemoved()
    {
        AsyncTask.Cancel();
    }

    class NewVersionPopup : UIPanel
    {
        GameScreen Screen => Updater.Screen;
        readonly AutoUpdater Updater;
        readonly ReleaseInfo Info;

        public NewVersionPopup(AutoUpdater updater, in ReleaseInfo info)
            : base(updater.ContentManager.LoadTextureOrDefault("Textures/MMenu/popup_banner_small.png"))
        {
            Updater = updater;
            Info = info;

            string text = "New Version\n" + info.Name;
            UILabel textLabel = base.Add(new UILabel(text, Fonts.Pirulen16));
            textLabel.TextAlign = TextAlign.HorizontalCenter;
            textLabel.AxisAlign = Align.CenterLeft;
            textLabel.SetLocalPos(132, 0);

            string portraitPath = GlobalStats.ActiveMod?.Mod.IconPath ?? "Textures/Portraits/Human.dds";
            SubTexture portraitTex = updater.ContentManager.LoadTextureOrDefault(portraitPath);
            UIPanel portrait = base.Add(new UIPanel(LocalPos.Zero, new Vector2(62, 74), portraitTex));
            portrait.AxisAlign = Align.CenterLeft;
            portrait.SetLocalPos(48, 0);

            // pulsate alpha
            Anim().Time(0, 4, 1, 1).Alpha(new Range(0.5f, 1.0f)).Loop();
        }

        void OnAutoUpdateClicked()
        {
            Updater.RemoveFromParent(); // remove AutoUpdater
            RemoveFromParent(); // remove self

            GameAudio.AffirmativeClick();

            Screen.ScreenManager.AddScreen(new MessageBoxScreen(Screen,
                "This will automatically update to the latest version. Continue?", 10f)
            {
                Accepted = () => Screen.ScreenManager.AddScreen(new AutoPatcher(Screen, Info)),
                Cancelled = () => Screen.Add(new AutoUpdater(Screen)), // should we show AutoUpdater again?
            });
            //System.Diagnostics.Process.Start(Info.InstallerUrl);
        }

        public override bool HandleInput(InputState input)
        {
            bool hovering = HitTest(input.CursorPosition);
            GameCursors.SetCurrentCursor(hovering ? GameCursors.AggressiveNav : GameCursors.Regular);

            if (hovering && input.LeftMouseClick)
            {
                OnAutoUpdateClicked();
                return true;
            }
            return base.HandleInput(input);
        }
    }

    void NotifyLatestVersion(ReleaseInfo info)
    {
        Log.Info($"Latest Version: {info.Name} at {info.ZipUrl}");

        Screen.RunOnNextFrame(() =>
        {
            var notification = Add(new NewVersionPopup(this, info));
            Vector2 endPos = new(10f, Screen.Height * 0.75f);
            Vector2 startPos = new(endPos.X - (notification.Width + 20), endPos.Y);

            notification.Anim() // slide in animation
                .FadeIn(delay:1.5f, duration:0.2f)
                .Pos(startPos, endPos)
                .Sfx(null, "sd_ui_notification_research_01")
            .ThenAnim() // followed by a small bounce
                .Time(0, 0.4f, 0.1f, 0.2f)
                .Pos(endPos, endPos-new Vector2(16,0));
        });
    }

    void GetVersionAsync()
    {
        string downloadUrl = GlobalStats.Defaults.DownloadSite;
        try
        {
            if (downloadUrl.Contains("github.com"))
            {
                // "https://github.com/TeamStarDrive/StarDrive/releases" --> "TeamStarDrive/StarDrive"
                string teamAndRepo = downloadUrl.Replace("https://", "").Replace("github.com/", "").Replace("/releases", "");
                downloadUrl = $"https://api.github.com/repos/{teamAndRepo}/releases/latest";

                ReleaseInfo? info = GetLatestVersionInfoGitHub(downloadUrl);
                if (info != null)
                    NotifyLatestVersion(info.Value);
            }
            else
            {
                Log.Warning($"AutoUpdater: unsupported download url {downloadUrl}");
            }
        }
        catch (Exception e)
        {
            // can easily fail due to network issues etc, shouldn't be a big deal
            Log.Warning($"GetVersionAsync {downloadUrl} failed: {e.Message}");
        }
    }

    ReleaseInfo? GetLatestVersionInfoGitHub(string url)
    {
        using WebClient wc = new();
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout:TimeSpan.FromSeconds(30));
        if (AsyncTask.IsCancelRequested)
            return null;

        dynamic latestRelease = new JavaScriptSerializer().DeserializeObject(jsonText);
        string name = latestRelease["name"];
        string tagName = latestRelease["tag_name"];
        string latestVersion = tagName.Split('-').Last();
        string currentVersion = GlobalStats.Version.Split(' ').First();
        Log.Info($"AutoUpdater: latest  {latestVersion}");
        Log.Info($"AutoUpdater: current {currentVersion}");

        bool latestIsNewer = string.CompareOrdinal(latestVersion, currentVersion) > 0;
        if (!latestIsNewer)
            return null;

        ReleaseInfo info = new(name, latestVersion, null, null);

        foreach (dynamic asset in latestRelease["assets"])
        {
            string assetName = asset["name"];
            if (assetName.Contains(".zip"))
            {
                info.ZipUrl = asset["browser_download_url"];
            }
            else if (assetName.Contains(".exe"))
            {
                info.InstallerUrl = asset["browser_download_url"];
            }
        }

        return info.ZipUrl.NotEmpty() ? info : null;
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
