using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SDGraphics;
using SDUtils;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// This will automatically apply the latest patch,
/// while showing progress
/// </summary>
internal class AutoPatcher : PopupWindow
{
    readonly GameScreen Screen;
    readonly ReleaseInfo Info;
    TaskResult CurrentTask;

    UIList ProgressSteps;

    public AutoPatcher(GameScreen screen, in ReleaseInfo info) : base(screen, 500, 180)
    {
        Screen = screen;
        Info = info;
        TitleText = "StarDrive BlackBox AutoPatcher";
    }

    public override void LoadContent()
    {
        base.LoadContent();

        ProgressSteps = Add(new UIList(new(460, 200), ListLayoutStyle.ResizeList));
        ProgressSteps.AxisAlign = Align.TopCenter;
        ProgressSteps.SetLocalPos(0, 70);

        ProgressBarElement p = AddProgressBar("Downloading");
        CurrentTask = Parallel.Run(() => Download(p));
    }

    public override void ExitScreen()
    {
        CurrentTask.Cancel();
        base.ExitScreen();
    }

    ProgressBarElement AddProgressBar(string progressLabel)
    {
        ProgressBarElement p = ProgressSteps.Add(new ProgressBarElement(new(0,0, ProgressSteps.Width, 18), 100));
        p.EnableProgressLabel(progressLabel, Fonts.TahomaBold9);
        return p;
    }

    void Download(ProgressBarElement dp)
    {
        try
        {
            string outputFolder = Path.GetFullPath(Path.Combine(Dir.StarDriveAppData, "Patches", Info.Version));
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, recursive:true); // delete all stale data, just in case
            Directory.CreateDirectory(outputFolder);

            Log.Write($"Downloading {Info.ZipUrl} to {outputFolder}");
            TimeSpan timeout = TimeSpan.FromMinutes(60);
            string zipArchive = AutoUpdater.DownloadZip(Info.ZipUrl, outputFolder, CurrentTask, dp.SetProgress, timeout);
            Log.Write($"Download finished: {outputFolder}");
            
            RunOnNextFrame(() =>
            {
                ProgressBarElement up = AddProgressBar($"Unzipping {Info.Version}");
                CurrentTask = Parallel.Run(() => Unzip(zipArchive, outputFolder, up));
            });
        }
        catch (Exception e)
        {
            Log.Warning($"DownloadAndUnzip {Info.ZipUrl} failed: {e.Message}");
        }
    }

    void Unzip(string zipArchive, string outputFolder, ProgressBarElement up)
    {
        try
        {
            Log.Write($"Unzipping {zipArchive} to {outputFolder}");
            UnzipWithProgress(zipArchive, outputFolder, CurrentTask, up);
            Log.Write($"Unzip finished: {outputFolder}");

            Log.Write($"Deleting archive {zipArchive}");
            File.Delete(zipArchive);

            RunOnNextFrame(() =>
            {
                ProgressBarElement ap = AddProgressBar("Applying Patch");
                CurrentTask = Parallel.Run(() => ApplyPatchFiles(outputFolder, ap));
            });
        }
        catch (Exception e)
        {
            Log.Warning($"Unzip {zipArchive} failed: {e.Message}");
        }
    }

    void UnzipWithProgress(string zipArchive, string outputFolder, 
                           TaskResult cancellableTask, ProgressBarElement p)
    {
        using ZipArchive source = ZipFile.Open(zipArchive, ZipArchiveMode.Read);
        int currentEntry = 0;
        int totalEntries = source.Entries.Count;
        int lastPercent = -1;
        foreach (ZipArchiveEntry entry in source.Entries)
        {
            if (cancellableTask.IsCancelRequested)
                throw new OperationCanceledException();

            string fullPath = Path.GetFullPath(Path.Combine(outputFolder, entry.FullName));
            if (!fullPath.StartsWith(outputFolder, StringComparison.OrdinalIgnoreCase))
                throw new IOException("ZipExtract: Relative paths not supported");

            if (Path.GetFileName(fullPath).Length == 0)
            {
                if (entry.Length != 0L)
                    throw new IOException("ZipExtract: Directory entry should not have any data");
                Directory.CreateDirectory(fullPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                entry.ExtractToFile(fullPath, overwrite:true);
            }

            ++currentEntry;
            int percent = ProgressBarElement.GetPercent(currentEntry, totalEntries);
            if (lastPercent != percent)
            {
                lastPercent = percent;
                p.SetProgress(percent);
            }
        }
    }

    void ApplyPatchFiles(string patchFilesFolder, ProgressBarElement ap)
    {
        try
        {
            FileInfo[] files = Dir.GetFiles(patchFilesFolder);
            int lastPercent = -1;
            for (int i = 0; i < files.Length; ++i)
            {
                FileInfo sourceFile = files[i];
                Log.Write($"Applying Patch: {sourceFile.FullName}");

                int percent = ProgressBarElement.GetPercent(i+1, files.Length);
                if (lastPercent != percent)
                {
                    lastPercent = percent;
                    ap.SetProgress(percent);
                }
            }

            RunOnNextFrame(() =>
            {
                ProgressSteps.AddLabel("Restarting StarDrive ...")
                    .Anim().Alpha(new(0.5f,1.0f)).Loop();
                CurrentTask = Parallel.Run(RestartAsync);
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "ApplyPatch failed");
        }
    }

    void RestartAsync()
    {
        Thread.Sleep(3000);

        string args = string.Join(" ", Environment.GetCommandLineArgs().AsSpan(1).ToArray());
        Application.Exit();
        System.Diagnostics.Process.Start(Application.ExecutablePath, args);
    }
}
