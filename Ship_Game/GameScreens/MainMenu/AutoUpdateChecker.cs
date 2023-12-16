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
using System.Threading.Tasks;
using SDUtils;

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

                ToolTip.CreateTooltip(Info.Changelog, "", null, maxWidth:720);
            }
            return base.HandleInput(input);
        }
    }

    void NotifyLatestVersion(ReleaseInfo info, bool isMod)
    {
        Log.Write($"Latest Version: {info.Name} at {info.ZipUrl}");

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
        return string.CompareOrdinal(latestVersion, currentVersion) > 0;
    }

    ReleaseInfo? GetLatestVersionInfoGitHub(string modName, string url, bool isMod)
    {
        string jsonText = DownloadWithCancel(url, AsyncTask, timeout: TimeSpan.FromSeconds(30));
        if (AsyncTask is { IsCancelRequested: true })
            return null;

        dynamic latestRelease = new JavaScriptSerializer().DeserializeObject(jsonText);
        string name = latestRelease["name"];
        string tagName = latestRelease["tag_name"];
        string changelog = latestRelease["body"];
        string latestVersion = tagName.Split('-').FindMax(s => s.Count(c => c == '.')); // part-v1.2.4-withmostdots

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
        if (AsyncTask is { IsCancelRequested: true })
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
        using WebClient wc = CreateWebClient((sender, e) =>
        {
            if (cancellableTask is { IsCancelRequested: true } && sender is WebClient webClient)
                webClient.CancelAsync();
        });

        var download = wc.DownloadStringTaskAsync(url);
        WaitForTask(download, cancellableTask, timeout);
        return download.Result;
    }

    /// <summary>
    /// Downloads Zip from `url` into `localFolder`. The task can be cancelled by the user.
    /// Returns the path to the local file. Otherwise throws an exception on failure or cancellation.
    /// </summary>
    public static string DownloadZip(string url, string localFolder, TaskResult cancellableTask, 
                                     Action<int> onProgressPercent, TimeSpan timeout)
    {
        int lastPercent = -1;
        using WebClient wc = CreateWebClient((sender, e) =>
        {
            if (cancellableTask is { IsCancelRequested: true } && sender is WebClient webClient)
                webClient.CancelAsync();
            else if (onProgressPercent != null && e != null)
            {
                int newPercent = e.ProgressPercentage;
                if (lastPercent != newPercent)
                {
                    lastPercent = newPercent;
                    onProgressPercent(newPercent);
                }
            }
        });

        string localFile = Path.Combine(localFolder, Path.GetFileName(url));
        var download = wc.DownloadFileTaskAsync(url, localFile);
        WaitForTask(download, cancellableTask, timeout);
        return localFile;
    }

    static WebClient CreateWebClient(DownloadProgressChangedEventHandler e)
    {
        WebClient wc = new();
        wc.UseDefaultCredentials = false;
        wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        wc.DownloadProgressChanged += e;
        return wc;
    }

    static void WaitForTask(Task task, TaskResult cancellableTask, TimeSpan timeout)
    {
        try
        {
            for (int timeoutMs = (int)timeout.TotalMilliseconds; timeoutMs > 0; timeoutMs -= 100)
            {
                if (task.Wait(100)) return;
                if (cancellableTask is { IsCancelRequested: true }) break;
            }
        }
        catch (AggregateException e)
        {
            throw e.InnerException ?? e;
        }

        if (cancellableTask is { IsCancelRequested: true })
            throw new OperationCanceledException("Download Request cancelled");
        throw new TimeoutException("Download Request timed out");
    }
}
