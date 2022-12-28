using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// This will automatically apply the latest patch,
/// while showing progress
/// </summary>
internal class AutoPatcher : PopupWindow
{
    GameScreen Screen;
    ReleaseInfo Info;
    ProgressBarElement Progress;
    UILabel ProgressLabel;

    TaskResult DownloadTask;

    public AutoPatcher(GameScreen screen, in ReleaseInfo info) : base(screen, 800, 600)
    {
        Screen = screen;
        Info = info;

        // unfortunately progress-bars are still old-style UI, no automation
        RectF progressRect = RectF;
        progressRect = progressRect.Widen(-80);
        progressRect.H = 18;
        Progress = Add(new ProgressBarElement(progressRect));

        ProgressLabel = Progress.Add(new UILabel("Downloading: 0%", Fonts.Consolas18));
        ProgressLabel.TextAlign = TextAlign.Center;
        ProgressLabel.Pos = progressRect.Center;

        DownloadTask = Parallel.Run(DownloadAsync);
    }

    void DownloadAsync()
    {
        try
        {
            string localFolder = Dir.StarDriveAppData + "Patches/" + Info.Version;
            Directory.CreateDirectory(localFolder);

            Log.Write($"Downloading {Info.ZipUrl} to {localFolder}");
            string localZipFile = AutoUpdater.DownloadZip(Info.ZipUrl, localFolder, DownloadTask, onProgressPercent:
                (percent) => ProgressLabel.Text = $"Downloading: {percent}%",
                timeoutMillis: 60 * 60 * 1000 /* 60 minutes timeout */);

            Log.Write($"Download finished: {localFolder}");
            ProgressLabel.Text = $"Unzipping {Info.Version}";
            ZipFile.ExtractToDirectory(localZipFile, localFolder);
        }
        catch (Exception e)
        {
            Log.Warning($"DownloadAsync {Info.ZipUrl} failed: {e.Message}");
        }

    }
}
