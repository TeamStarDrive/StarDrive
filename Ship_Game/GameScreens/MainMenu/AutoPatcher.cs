using System;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using SDUtils;
using Color = Microsoft.Xna.Framework.Graphics.Color;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// This will automatically apply the latest patch,
/// while showing progress
/// </summary>
internal class AutoPatcher : PopupWindow
{
    readonly GameScreen Screen;
    readonly ReleaseInfo Info;
    readonly bool IsMod;
    TaskResult CurrentTask;

    UIList ProgressSteps;

    public AutoPatcher(GameScreen screen, in ReleaseInfo info, bool isMod) : base(screen, 520, 220)
    {
        Screen = screen;
        Info = info;
        IsMod = isMod;
        TitleText = "AutoPatcher " + info.Name;
        CanEscapeFromScreen = false;
    }

    public override void LoadContent()
    {
        base.LoadContent();

        Log.LogEventStats(Log.GameEvent.AutoUpdateStarted);

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

    string GetPatchOutputFolder() => Path.GetFullPath(Path.Combine(Dir.StarDriveAppData, "Patches", Info.Version));
    static string GetPatchTempFolder() => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "PatchTemp"));

    // delete any stale files from StarDrivePlus/PatchTemp folder
    public static void TryDeletePatchTemp()
    {
        string tempDir = GetPatchTempFolder();
        TryDeleteFolder(tempDir);
    }

    static void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive:true);
        }
        catch {}
    }

    void Download(ProgressBarElement dp)
    {
        try
        {
            TryDeletePatchTemp();

            string outputFolder = GetPatchOutputFolder();
            TryDeleteFolder(outputFolder); // delete all stale data, just in case
            Directory.CreateDirectory(outputFolder);

            Log.Write($"Downloading {Info.ZipUrl} to {outputFolder}");
            TimeSpan timeout = TimeSpan.FromMinutes(60);
            string zipArchive = AutoUpdateChecker.DownloadZip(Info.ZipUrl, outputFolder, CurrentTask, dp.SetProgress, timeout);
            Log.Write($"Download finished: {outputFolder}");
            
            RunOnNextFrame(() =>
            {
                ProgressBarElement up = AddProgressBar($"Unzipping {Info.Version}");
                CurrentTask = Parallel.Run(() => Unzip(zipArchive, outputFolder, up));
            });
        }
        catch (Exception e)
        {
            // this can fail for a lot of reasons, so it's not a critical error
            Log.Warning($"Download {Info.ZipUrl} failed: {e.Message}");
            AddErrorMessageAndAllowExit("Download failed!", e.Message);
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
            Log.Error($"Unzip {zipArchive} failed: {e.Message}");
            AddErrorMessageAndAllowExit("Unzip failed!", e.Message);
        }
    }

    void UnzipWithProgress(string zipArchive, string outputFolder, 
                           TaskResult cancellableTask, ProgressBarElement p)
    {
        using ZipArchive source = ZipFile.Open(zipArchive, ZipArchiveMode.Read);
        int currentEntry = 0;
        int totalEntries = source.Entries.Count;
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

            p.SetProgress(ProgressBarElement.GetPercent(++currentEntry, totalEntries));
        }
    }

    void ApplyPatchFiles(string patchFilesFolder, ProgressBarElement ap)
    {
        try
        {
            ScreenManager.ResetHotLoadTargets(); // disable hotloading while patcher is running

            string gameDir = Directory.GetCurrentDirectory();
            if (IsMod) gameDir = Path.Combine(gameDir, GlobalStats.ModPath.Replace('/', '\\'));

            bool requiresElevation = gameDir.Contains("Program Files");
            if (requiresElevation)
            {
                if (!IsInRole(WindowsBuiltInRole.Administrator))
                    throw new InvalidOperationException("UAC Elevation failed: cannot overwrite StarDrive Program Files");
            }

            ApplyPatchFiles(patchFilesFolder, ap, gameDir);

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
            AddErrorMessageAndAllowExit("Apply Patch failed!", e.Message);
        }
    }

    void ApplyPatchFiles(string patchFilesFolder, ProgressBarElement ap, string gameDir)
    {
        string tempDir = GetPatchTempFolder();

        // in case the archive extracted files to Folder/ModName instead of Folder/
        var entries = Directory.GetFileSystemEntries(patchFilesFolder, "*", SearchOption.TopDirectoryOnly);
        if (entries.Length == 1)
            patchFilesFolder = entries[0];

        Array<string> filesToDelete = GetFilesToRemove(Path.Combine(patchFilesFolder, "Release.DeleteFiles.txt"));
        FileInfo[] filesToAdd = Dir.GetFiles(patchFilesFolder);

        int currentAction = 0;
        int totalActions = filesToDelete.Count + filesToAdd.Length;

        foreach (string toRemoveRelPath in filesToDelete)
        {
            string fullPath = Path.Combine(gameDir, toRemoveRelPath);
            Log.Write($"RemoveFile: {toRemoveRelPath}");
            SafeDelete(fullPath, toRemoveRelPath, tempDir);
            ap.SetProgress(ProgressBarElement.GetPercent(++currentAction, totalActions));
        }

        foreach (FileInfo toAdd in filesToAdd)
        {
            string srcFile = toAdd.FullName;
            string relPath = srcFile.Replace(patchFilesFolder, "").TrimStart('\\', '/');
            string dstFile = Path.Combine(gameDir, relPath);
            
            Log.Write($"ApplyPatch: {relPath}");
            SafeMove(srcFile, dstFile, relPath, tempDir);
            ap.SetProgress(ProgressBarElement.GetPercent(++currentAction, totalActions));
        }
    }

    static Array<string> GetFilesToRemove(string filesToDeleteTxt)
    {
        Array<string> toRemove = new();
        if (!File.Exists(filesToDeleteTxt))
            return toRemove;

        foreach (string line in File.ReadAllLines(filesToDeleteTxt))
        {
            // the RelPath of the file is always the last element
            // 1a91bdf1146eb32bf634cc11440ac23c196ae3ac;60B4088F-64EC-4983-A095-7E16577FCCD8;StarDrive.exe.Config
            string[] parts = line.Split(';');
            if (parts.Length > 0)
                toRemove.Add(parts[parts.Length - 1].Trim().TrimStart('\\', '/'));
        }
        
        File.Delete(filesToDeleteTxt); // remove this file to avoid copying it to game dir
        return toRemove;
    }

    void AddErrorMessageAndAllowExit(string title, string details)
    {
        RunOnNextFrame(() =>
        {
            CanEscapeFromScreen = true;

            var label = ProgressSteps.AddLabel(title);
            label.Color = Color.Red;
            label.Anim().Alpha(new(0.5f, 1.0f)).Loop();

            var detailsLabel = ProgressSteps.AddLabel(details);
            detailsLabel.Color = Color.Red;
        });
    }

    /// <summary>
    /// If the file is in use, it must be moved or renamed,
    /// however, moving between different drives would cause the file to be copied,
    /// so we always move it into game/PatchTemp folder
    /// </summary>
    static void SafeMove(string srcFile, string dstFile, string relPath, string tempDir)
    {
        string tmpFile = null;
        try
        {
            if (File.Exists(dstFile))
            {
                tmpFile = MoveToTempPath(tempDir, relPath, dstFile);
            }
            MoveAndCreateDirs(srcFile, dstFile);
        }
        catch (Exception e)
        {
            if (tmpFile != null) // restore the file if needed
            {
                File.Move(tmpFile, dstFile);
            }

            throw new IOException(relPath, e);
        }
    }

    /// <summary>
    /// If the file is in use, it must be moved or renamed,
    /// so we always move it to game/PatchTemp folder first
    /// </summary>
    static void SafeDelete(string fileToDelete, string relPath, string tempDir)
    {
        try
        {
            if (!File.Exists(fileToDelete))
                return; // nothing to do!

            string tmpFile = MoveToTempPath(tempDir, relPath, fileToDelete);
            try
            {
                // now try to delete the temp file, but no worries if it cannot be deleted right now
                // it will be deleted during next PatchTemp folder cleanup
                File.Delete(tmpFile);
            }
            catch
            {
            }
        }
        catch (Exception e)
        {
            throw new IOException(relPath, e);
        }
    }

    /// <summary>
    /// Moves `theFile` into `tempPath`, returning full path to the temp file,
    /// so that it can be restored if necessary
    /// </summary>
    static string MoveToTempPath(string tempPath, string relPath, string theFile)
    {
        string tmpFile = Path.Combine(tempPath, relPath);
        if (File.Exists(tmpFile))
        {
            try
            {
                File.Delete(tmpFile);
            }
            catch
            {
                // sometimes even the temp file might still be in use! in that case, copy the OLD temp file
                string tempTemp = tmpFile + "." + DateTime.Now.Ticks;
                File.Move(tmpFile, tempTemp);
            }
        }

        MoveAndCreateDirs(theFile, tmpFile);
        return tmpFile;
    }

    static void MoveAndCreateDirs(string sourceFile, string destinationFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Move(sourceFile, destinationFile);
        }
        catch (Exception e)
        {
            throw new IOException($"Move failed: {sourceFile} --> {destinationFile}", e);
        }
    }

    void RestartAsync()
    {
        Log.Write("AutoUpdate finished. Restarting in 3 seconds...");
        Log.FlushAllLogs();
        Log.LogEventStats(Log.GameEvent.AutoUpdateFinished);

        Thread.Sleep(2900);
        Program.RunCleanup();

        string args = string.Join(" ", Environment.GetCommandLineArgs().AsSpan(1).ToArray());
        Application.Exit();
        System.Diagnostics.Process.Start(Application.ExecutablePath, args);
    }

    static bool IsInRole(WindowsBuiltInRole role)
    {
        // Set the security policy context to windows security
        AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

        // Create a WindowsPrincipal object representing the current user
        WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());

        return principal.IsInRole(role);
    }
}
