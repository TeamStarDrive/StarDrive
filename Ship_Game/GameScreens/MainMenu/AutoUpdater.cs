using Ship_Game.SpriteSystem;
using System;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using SDGraphics;
using Ship_Game.UI;
using Ship_Game.Data;
using Ship_Game.Audio;

namespace Ship_Game.GameScreens.MainMenu;

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

    public record struct ReleaseInfo(string Name, string ZipUrl, string InstallerUrl);

    class VersionPopup : UIPanel
    {
        ReleaseInfo Info;

        public VersionPopup(GameContentManager content, in ReleaseInfo info)
            : base(content.LoadTextureOrDefault("Textures/MMenu/popup_banner_small.png"))
        {
            Info = info;

            string text = "New Version\n" + info.Name;
            UILabel textLabel = base.Add(new UILabel(text, Fonts.Pirulen16));
            textLabel.TextAlign = TextAlign.HorizontalCenter;
            textLabel.AxisAlign = Align.CenterLeft;
            textLabel.SetLocalPos(132, 0);

            string portraitPath = GlobalStats.ActiveMod?.Mod.IconPath ?? "Textures/Portraits/Human.dds";
            SubTexture portraitTex = content.LoadTextureOrDefault(portraitPath);
            UIPanel portrait = base.Add(new UIPanel(LocalPos.Zero, new Vector2(62, 74), portraitTex));
            portrait.AxisAlign = Align.CenterLeft;
            portrait.SetLocalPos(48, 0);

            // pulsate alpha
            Anim().Time(0, 4, 1, 1).Alpha(new Range(0.5f, 1.0f)).Loop();
        }

        public override bool HandleInput(InputState input)
        {
            bool hovering = HitTest(input.CursorPosition);
            GameCursors.SetCurrentCursor(hovering ? GameCursors.AggressiveNav : GameCursors.Regular);

            if (hovering && input.LeftMouseClick)
            {
                GameAudio.AffirmativeClick();
                System.Diagnostics.Process.Start(Info.InstallerUrl);
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
            var notification = Add(new VersionPopup(ContentManager, info));
            Vector2 endPos = new(10f, Screen.Height * 0.75f);
            Vector2 startPos = new(endPos.X - (notification.Width + 20), endPos.Y);
            notification.Anim()
                .FadeIn(delay:1.5f, duration:0.5f)
                .Pos(startPos, endPos)
                .Sfx("sd_ui_tactical_pause", "sd_ui_notification_research_01");
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
        string jsonText = DownloadWithCancel(url, AsyncTask);
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

        ReleaseInfo info = new(name, null, null);

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
    static string DownloadWithCancel(string url, TaskResult cancellableTask)
    {
        //HttpWebRequest request = WebRequest.CreateHttp(url);
        //request.ContentType = "application/json";
        //request.Method = "GET";
        //request.Timeout = 30 * 1000;
        //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
        //request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
        //request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.None;

        //using WebResponse response = request.GetResponse();
        //using StreamReader reader = new(response.GetResponseStream()!);

        //string text = reader.ReadToEnd();
        //return text;

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
            for (int timeout = 30 * 1000; timeout > 0; timeout -= 100)
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
}
