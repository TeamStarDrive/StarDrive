using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using SDUtils;
using Color = Microsoft.Xna.Framework.Color;

namespace Ship_Game.GameScreens.MainMenu;

// AutoPatcher — applies an in-line file-drop patch from GitHub Releases.
//
// Lifecycle:
//   1. AutoUpdateChecker discovers a newer release and pushes AutoPatcher
//      onto the screen stack. Vanilla scans /releases (the array) via
//      TrySelectMaxVersionRelease and picks the highest-version
//      NON-pre-release tag; same major.minor as current install routes to
//      this in-line patch flow, a higher major.minor routes to the
//      MajorUpgradeAvailablePopup instead (cross-major bumps can't be
//      applied as a file-drop). Pre-releases are deliberately skipped here
//      so maintainers can stage patches on GitHub without auto-pushing
//      them to every user — flip the pre-release flag off to roll out.
//      Mods take a separate path: /releases/latest, no version-line filter.
//   2. Download phase: pulls the patch zip(s) from Info.ZipUrls into
//      %APPDATA%\StarDrive\Patches\<version>\ (mods: Patches\<mod folder> <version>\). Chunked patches are concatenated
//      in name order before unzip — see Deploy/MakeInstaller.py for the
//      "001-…", "002-…" chunk layout.
//   3. Unzip phase: extracts into the same per-version cache folder.
//
// UAC self-elevation (Option A — split-pass):
//   The download/unzip pass runs in the original (non-elevated) process so it
//   can write under %APPDATA% without any prompt. After unzip, NeedsElevation
//   gates on `gameDir.Contains("Program Files")` && !IsInRole(Administrator).
//   If true:
//     a. WritePendingPatchMarker writes %APPDATA%\StarDrive\PendingPatch.json
//        with { Version, Name, ModPath }.
//     b. RelaunchAsAdminWithMarker spawns StarDrive.exe again with
//        --apply-patch=<version> and Verb="runas". Windows shows the UAC
//        dialog. The original process exits.
//     c. The new (elevated) process boots normally; Program.ParseMainArgs sees
//        --apply-patch and stashes the version in Program.ResumePatchVersion.
//     d. MainMenuScreen.LoadContent calls AutoPatcher.TryResumePending. If the
//        marker matches, it builds a synthetic ReleaseInfo and pushes a new
//        AutoPatcher with ResumeMode=true.
//     e. ResumeMode skips Download/Unzip (the cache is already on disk) and
//        jumps straight to DeleteStaleFiles + file-move into the install dir.
//   The marker is deleted on RestartAsync (success path) and on the
//   "cached patch missing" branch, so a failed second leg doesn't loop.
//
// Mods: NeedsElevation also fires when the mod folder lands inside
// "Program Files". Mod patches go through the same UAC dance.
// A mod package must be the whole mod: the mod folder is made to match it,
// so files the new version no longer has are deleted once every new file is in.
// Its Globals.yaml is copied last, so a failed copy keeps the old version.
/// <summary>
/// This will automatically apply the latest patch,
/// while showing progress
/// </summary>
internal class AutoPatcher : PopupWindow
{
    readonly GameScreen Screen;
    readonly ReleaseInfo Info;
    readonly string ModPath; // "Mods/Combined Arms/", null when patching BlackBox itself
    readonly bool ResumeMode; // true = elevated resume; skip download/unzip
    TaskResult CurrentTask;
    ModPackage ModUpdate;

    UIList ProgressSteps;

    bool IsMod => ModPath != null;

    public AutoPatcher(GameScreen screen, in ReleaseInfo info, string modPath, bool resumeMode = false)
        : base(screen, 520, 220)
    {
        Screen = screen;
        Info = info;
        ModPath = modPath;
        ResumeMode = resumeMode;
        TitleText = "AutoPatcher " + info.Name;
        CanEscapeFromScreen = false;
    }

    public static bool IsDevCopy(string modPath)
    {
        string git = Path.Combine(modPath, ".git");
        return Directory.Exists(git) || File.Exists(git);
    }

    public override void LoadContent()
    {
        bool devCopy = IsMod && IsDevCopy(ModPath);
        if (devCopy)
            MiddleText = Localizer.Token(GameText.ModDevCopyTip);

        base.LoadContent();

        //Log.LogEventStats(Log.GameEvent.AutoUpdateStarted);

        ProgressSteps = Add(new UIList(new(460, 200), ListLayoutStyle.ResizeList));
        ProgressSteps.AxisAlign = Align.TopCenter;
        ProgressSteps.SetLocalPos(0, 70);

        if (devCopy)
        {
            Log.Write($"AutoPatcher: {ModPath} is a git repository, not patching it");
            CanEscapeFromScreen = true;
            return;
        }

        if (ResumeMode)
        {
            // Elevated resume: download + unzip already done by the non-elevated
            // pre-elevation pass. Pre-fill those two bars to 100% so the user
            // sees the same 4-bar layout as the normal flow, then continue with
            // the file-move phases against the cached patch in
            // GetPatchOutputFolder().
            Log.Write($"AutoPatcher: resuming pre-downloaded patch {Info.Version} in elevated mode");
            string outputFolder = GetPatchOutputFolder();
            if (!Directory.Exists(outputFolder))
            {
                AddErrorMessageAndAllowExit(
                    "Cached patch missing",
                    $"Expected pre-downloaded patch at {outputFolder} but the folder is gone. Try the update again from the main menu.");
                DeletePendingPatchMarker();
                return;
            }
            AddProgressBar("Downloading").SetProgress(100);
            AddProgressBar($"Unzipping {Info.Version}").SetProgress(100);
            string patchFilesFolder = GetPatchFilesFolder(outputFolder);
            AddProgressAndRunTaskOnNextFrame("Deleting Stale Files", nextP => DeleteStaleFiles(patchFilesFolder, nextP));
            return;
        }

        ProgressBarElement p = AddProgressBar("Downloading");
        CurrentTask = Parallel.Run(() => Download(p));
    }

    bool NeedsElevation()
    {
        string gameDir = Directory.GetCurrentDirectory();
        if (IsMod) gameDir = Path.Combine(gameDir, ModPath.Replace('/', '\\'));
        bool inProgramFiles = gameDir.Contains("Program Files");
        return inProgramFiles && !IsInRole(WindowsBuiltInRole.Administrator);
    }

    // Persisted between the non-elevated download/unzip pass and the elevated
    // apply pass. Stores enough Info for the elevated instance to construct an
    // AutoPatcher in resume mode without re-querying GitHub.
    class PendingPatchMarker
    {
        public string Version { get; set; }
        public string Name    { get; set; }
        public string ModPath { get; set; }
    }

    static string PendingPatchMarkerPath
        => Path.Combine(Dir.StarDriveAppData, "PendingPatch.json");

    static void WritePendingPatchMarker(in ReleaseInfo info, string modPath)
    {
        try
        {
            var marker = new PendingPatchMarker
            {
                Version = info.Version,
                Name    = info.Name,
                ModPath = modPath,
            };
            string json = JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PendingPatchMarkerPath, json);
            Log.Write($"AutoPatcher: wrote pending-patch marker {PendingPatchMarkerPath}");
        }
        catch (Exception e)
        {
            Log.Warning($"AutoPatcher: failed to write marker: {e.Message}");
        }
    }

    static void DeletePendingPatchMarker()
    {
        try
        {
            if (File.Exists(PendingPatchMarkerPath))
                File.Delete(PendingPatchMarkerPath);
        }
        catch (Exception e)
        {
            Log.Warning($"AutoPatcher: failed to delete marker: {e.Message}");
        }
    }

    // Called from MainMenuScreen.LoadContent when the game starts up. If the
    // current launch carries --apply-patch=<version>, find the marker, build a
    // synthetic ReleaseInfo, and push an AutoPatcher in resume mode.
    public static void TryResumePending(GameScreen screen)
    {
        if (Program.ResumePatchVersion.IsEmpty())
            return;

        string requestedVer = Program.ResumePatchVersion;
        // ResumePatchVersion stays set for the lifetime of the elevated process
        // — AutoUpdateChecker uses it to know "we're applying a patch right
        // now, don't show a 'new version available' popup". The post-success
        // restart spawns a new process *without* --apply-patch (see
        // RestartAsync), so the field naturally clears on the next launch.
        // Re-entry safety on this same process is handled by the marker check
        // below — once RestartAsync deletes it, TryResumePending bails here.

        if (!File.Exists(PendingPatchMarkerPath))
        {
            Log.Warning($"AutoPatcher: --apply-patch={requestedVer} but no marker at {PendingPatchMarkerPath}; ignoring");
            return;
        }

        PendingPatchMarker marker = null;
        try
        {
            marker = JsonSerializer.Deserialize<PendingPatchMarker>(File.ReadAllText(PendingPatchMarkerPath));
        }
        catch (Exception e)
        {
            Log.Warning($"AutoPatcher: failed to read marker: {e.Message}; ignoring");
            DeletePendingPatchMarker();
            return;
        }

        if (marker == null || marker.Version != requestedVer)
        {
            Log.Warning($"AutoPatcher: marker version '{marker?.Version}' != arg '{requestedVer}'; ignoring");
            DeletePendingPatchMarker();
            return;
        }

        Log.Write($"AutoPatcher: --apply-patch={requestedVer} -> resuming (mod={marker.ModPath}, elevated={IsInRole(WindowsBuiltInRole.Administrator)})");

        // ZipUrls/Changelog/InstallerUrl unused on the resume path; pass through
        // empty placeholders so the synthetic ReleaseInfo is well-formed.
        var synthetic = new ReleaseInfo(marker.Name, marker.Version, "", new List<string>(), null);
        // Wait for MainMenuScreen's fade-in (TransitionOnTime = 1s) to fully
        // complete before adding the AutoPatcher screen. Without this delay,
        // PopupWindow.LoadContent computes its chrome rectangles against the
        // transient mid-fade screen state and the progress-bar PerformLayout
        // pass never propagates Rect to the inner ProgressBar widget — the
        // bar then renders at (0,0,width,18) instead of inside the popup,
        // and the chrome looks misaligned. 1.5s gives the menu time to fully
        // settle before the patch screen pops on top.
        Parallel.Run(() =>
        {
            Thread.Sleep(1500);
            screen.RunOnNextFrame(() =>
            {
                screen.ScreenManager.AddScreen(new AutoPatcher(screen, synthetic, marker.ModPath, resumeMode: true));
            });
        });
    }

    // Self-elevation: write marker, relaunch with --apply-patch=<version> and
    // Verb = "runas" (UAC prompt fires), exit. The elevated instance's
    // TryResumePending picks up where we left off.
    void RelaunchAsAdminWithMarker()
    {
        try
        {
            Log.Write("AutoPatcher: install dir requires elevation; writing marker and relaunching as admin");
            WritePendingPatchMarker(Info, ModPath);

            // De-duplicate: if the user somehow already had --apply-patch on the
            // command line (shouldn't happen, but defensive), strip it before
            // re-appending the current version.
            string argString = string.Join(" ",
                Environment.GetCommandLineArgs().Skip(1)
                    .Where(a => !a.StartsWith("--apply-patch", StringComparison.OrdinalIgnoreCase))
                    .Append($"--apply-patch={Info.Version}"));

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = Application.ExecutablePath,
                UseShellExecute = true,    // required for Verb = "runas"
                Verb            = "runas", // triggers the Windows UAC prompt
                Arguments       = argString,
            };

            // Release blackbox.log BEFORE spawning the elevated child. Process.Start
            // returns as soon as the kernel hands back a handle for the new
            // process; the child boots immediately and Log.Initialize re-opens
            // the same path with FileMode.Create. If we still hold the handle,
            // the child crashes with "file is being used by another process".
            Log.Close();

            System.Diagnostics.Process.Start(psi);

            Thread.Sleep(500); // give the elevated instance a moment to claim the window
            Program.RunCleanup();
            Application.Exit();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // ERROR_CANCELLED (1223) -> user clicked No on the UAC dialog. Drop
            // the marker so a future re-attempt doesn't see stale state.
            Log.Warning($"AutoPatcher: UAC elevation denied: {ex.Message}");
            DeletePendingPatchMarker();
            AddErrorMessageAndAllowExit(
                "Admin rights required",
                "The patch is downloaded but needs admin rights to write to your install folder.\nClick the update notification again and accept the UAC prompt, or right-click 'StarDrive.exe' and choose 'Run as administrator'.");
        }
    }

    public override void ExitScreen()
    {
        CurrentTask?.Cancel();
        base.ExitScreen(); // will call this.Dispose(true)
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            CurrentTask?.Dispose();
        base.Dispose(disposing);
    }

    ProgressBarElement AddProgressBar(string progressLabel)
    {
        ProgressBarElement p = ProgressSteps.Add(new ProgressBarElement(new(0,0, ProgressSteps.Width, 18), 100));
        p.EnableProgressLabel(progressLabel, Fonts.TahomaBold9);
        if (ResumeMode)
        {
            // Resume runs before the parent screen has fully settled — the
            // bar's PerformLayout would not otherwise fire before first draw,
            // leaving ProgressBar.Rect at the constructor origin (0,0) instead
            // of inside the popup. Force the layout pass here. Normal flow
            // gets enough idle Update ticks before drawing for this to fire
            // incidentally; the explicit call is unnecessary there.
            ProgressSteps.PerformLayout();
        }
        return p;
    }

    void AddProgressAndRunTaskOnNextFrame(string progressLabel, Action<ProgressBarElement> action)
    {
        RunOnNextFrame(() =>
        {
            ProgressBarElement nextProgress = AddProgressBar(progressLabel);
            CurrentTask = Parallel.Run(() => action(nextProgress));
        });
    }

    string GetPatchOutputFolder() => Path.GetFullPath(Path.Combine(Dir.StarDriveAppData, "Patches", PatchCacheName));
    string PatchCacheName => IsMod ? $"{Path.GetFileName(ModPath.TrimEnd('/', '\\'))} {Info.Version}" : Info.Version;
    static string GetPatchTempFolder() => Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "PatchTemp"));

    // delete any stale files from StarDrivePlus/PatchTemp folder
    public static void TryDeletePatchTemp()
    {
        string tempDir = GetPatchTempFolder();
        TryDeleteFolder(tempDir);
    }

    static void TryDeletePatchTempFiles() => TryDeleteFilesAndFolder(GetPatchTempFolder());

    // file by file, so the files this process still has open do not stop the rest; never through a junction or symlink
    internal static void TryDeleteFilesAndFolder(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && !IsReparsePoint(dir))
            {
                var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint };
                foreach (FileInfo file in new DirectoryInfo(dir).EnumerateFiles("*", options))
                {
                    try { file.Delete(); } catch {}
                }
            }
        }
        catch {}
        TryDeleteFolder(dir);
    }

    static bool IsReparsePoint(string dir)
    {
        string path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dir));
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    internal static bool IsInsideFolder(string path, string folder)
    {
        string root = Path.GetFullPath(folder);
        if (!Path.EndsInDirectorySeparator(root))
            root += Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    // delete any legacy files which could accidentally be left in the game Content dir
    public static void CleanupLegacyIncompatibleFiles()
    {
        string[] files = {
            "Content\\Textures\\Buildings\\icon_refınery_48x48.xnb",
            "Content\\Textures\\Buildings\\icon_refınery_64x64.xnb",
        };

        string tempDir = GetPatchTempFolder();
        foreach (string relPath in files)
            SafeDelete(relPath, tempDir);
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

    void Download(ProgressBarElement p)
    {
        try
        {
            TryDeletePatchTemp();

            string outputFolder = GetPatchOutputFolder();
            TryDeleteFolder(outputFolder); // delete all stale data, just in case
            Directory.CreateDirectory(outputFolder);

            Log.Write($"Downloading {Info.ZipUrls} to {outputFolder}");
            TimeSpan timeout = TimeSpan.FromMinutes(60);
            List<string> zipChunks = AutoUpdateChecker.DownloadZip(Info.ZipUrls, outputFolder, CurrentTask, p.SetProgress, timeout);
            Log.Write($"Download finished: {outputFolder}");
            
            string zipArchive = PostProcessMultipleZipChunks(zipChunks);
            AddProgressAndRunTaskOnNextFrame($"Unzipping {Info.Version}", nextP => Unzip(zipArchive, outputFolder, nextP));
        }
        catch (Exception e)
        {
            // this can fail for a lot of reasons, so it's not a critical error
            Log.Warning($"Download {Info.ZipUrls} failed: {e.Message}");
            AddErrorMessageAndAllowExit("Download failed!", e.Message);
        }
    }

    string PostProcessMultipleZipChunks(List<string> zipChunks)
    {
        if (zipChunks.Count == 1)
            return zipChunks[0]; // there is only 1 file

        FileInfo firstChunk = new(zipChunks[0]);
        string newFile = Path.Combine(firstChunk.DirectoryName, "combined.zip");
        using (FileStream output = File.Create(newFile))
        {
            foreach (string part in zipChunks)
            {
                using (FileStream input = File.OpenRead(part))
                {
                    input.CopyTo(output);
                }
            }
        }

        foreach (string part in zipChunks)
        {
            try
            {
                Log.Write($"Deleting zip chunk {part}");
                File.Delete(part);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to delete zip chunk {part}: {e.Message}");
            }
        }

        Log.Write($"Combined {zipChunks.Count} zip chunks into {newFile}");
        return newFile;
    }

    void Unzip(string zipArchive, string outputFolder, ProgressBarElement p)
    {
        try
        {
            Log.Write($"Unzipping {zipArchive} to {outputFolder}");
            UnzipWithProgress(zipArchive, outputFolder, CurrentTask, p);
            Log.Write($"Unzip finished: {outputFolder}");

            Log.Write($"Deleting archive {zipArchive}");
            File.Delete(zipArchive);

            // Elevation check sits HERE (post-unzip, pre-file-moves). Download
            // and unzip work fine non-elevated (write to AppData), so we save
            // bandwidth by deferring UAC until we actually need to write into
            // $INSTDIR. The cached download survives a UAC denial — the user
            // can re-trigger the popup and we resume from this exact point.
            if (NeedsElevation())
            {
                RunOnNextFrame(() =>
                {
                    var label = ProgressSteps.AddLabel("Patch downloaded — UAC prompt to apply patch in 3 seconds...");
                    label.Color = Color.Yellow;
                    label.Anim().Alpha(new(0.5f, 1.0f)).Loop();
                    // Give the user a moment to read the label before UAC steals
                    // focus. Without this, the prompt appears almost
                    // simultaneously with the label and the user has no time
                    // to understand why the OS is asking for elevation.
                    CurrentTask = Parallel.Run(() =>
                    {
                        Thread.Sleep(3000);
                        RelaunchAsAdminWithMarker();
                    });
                });
                return;
            }

            string patchFilesFolder = GetPatchFilesFolder(outputFolder);
            AddProgressAndRunTaskOnNextFrame("Deleting Stale Files", nextP => DeleteStaleFiles(patchFilesFolder, nextP));
        }
        catch (Exception e)
        {
            Log.Error($"Unzip {zipArchive} failed: {e.Message}");
            AddErrorMessageAndAllowExit("Unzip failed!", e.Message);
        }
    }

    internal static void UnzipWithProgress(string zipArchive, string outputFolder,
                                           TaskResult cancellableTask, ProgressBarElement p)
    {
        using ZipArchive source = ZipFile.Open(zipArchive, ZipArchiveMode.Read);
        int currentEntry = 0;
        int totalEntries = source.Entries.Count;
        foreach (ZipArchiveEntry entry in source.Entries)
        {
            if (cancellableTask?.IsCancelRequested == true)
                throw new OperationCanceledException();

            string fullPath = Path.GetFullPath(Path.Combine(outputFolder, entry.FullName));
            if (!IsInsideFolder(fullPath, outputFolder))
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

            p?.SetProgress(ProgressBarElement.GetPercent(++currentEntry, totalEntries));
        }
    }

    string GetGameDirectory()
    {
        string gameDir = Directory.GetCurrentDirectory();
        if (IsMod) gameDir = Path.Combine(gameDir, ModPath.Replace('/', '\\'));

        bool requiresElevation = gameDir.Contains("Program Files");
        if (requiresElevation)
        {
            if (!IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("UAC Elevation failed: cannot overwrite StarDrive Program Files");
        }
        return gameDir;
    }

    static string GetPatchFilesFolder(string outputFolder)
    {
        // in case the archive extracted files to Folder/ModName instead of Folder/
        var entries = Directory.GetFileSystemEntries(outputFolder, "*", SearchOption.TopDirectoryOnly);
        if (entries.Length == 1)
            return entries[0];
        return outputFolder;
    }

    void DeleteStaleFiles(string patchFilesFolder, ProgressBarElement p)
    {
        try
        {
            string gameDir = GetGameDirectory();
            string tempDir = GetPatchTempFolder();

            if (IsMod && !TryReadModPackage(gameDir, patchFilesFolder, out ModUpdate))
            {
                AddErrorMessageAndAllowExit(Localizer.Token(GameText.ModPackageIncomplete), Localizer.Token(GameText.ModPackageIncompleteTip));
                return;
            }

            RemoveReleaseDeleteFiles(patchFilesFolder, gameDir, tempDir, IsMod, p);
            p.SetProgress(100);

            AddProgressAndRunTaskOnNextFrame("Copying New Files", nextP => CopyNewFiles(patchFilesFolder, nextP));
        }
        catch (Exception e)
        {
            Log.Error(e, "DeleteStaleFiles failed");
            AddErrorMessageAndAllowExit("Delete Stale Files failed!", e.Message);
        }
    }
    
    // a mod's dropped files are removed by CopyFiles once every new file is in, so a mod's list is not used
    internal static void RemoveReleaseDeleteFiles(string patchFilesFolder, string gameDir, string tempDir, bool isMod, ProgressBarElement p)
    {
        if (isMod)
            return;
        Array<string> filesToDelete = GetFilesToRemove(Path.Combine(patchFilesFolder, ReleaseDeleteFiles), gameDir);
        int currentAction = 0;
        foreach (string toRemoveRelPath in filesToDelete)
        {
            string fullPath = Path.Combine(gameDir, toRemoveRelPath);
            Log.Write($"RemoveFile: {toRemoveRelPath}");
            SafeDelete(fullPath, toRemoveRelPath, tempDir);
            p?.SetProgress(ProgressBarElement.GetPercent(++currentAction, filesToDelete.Count));
        }
    }

    internal static Array<string> GetFilesToRemove(string filesToDeleteTxt, string gameDir)
    {
        Array<string> toRemove = new();
        if (!File.Exists(filesToDeleteTxt))
            return toRemove;

        foreach (string line in File.ReadAllLines(filesToDeleteTxt))
        {
            // the RelPath of the file is always the last element
            // 1a91bdf1146eb32bf634cc11440ac23c196ae3ac;60B4088F-64EC-4983-A095-7E16577FCCD8;StarDrive.exe.Config
            string[] parts = line.Split(';');
            string relPath = parts[parts.Length - 1].Trim().TrimStart('\\', '/');
            if (relPath.Length == 0)
                continue;
            string fullPath = Path.GetFullPath(Path.Combine(gameDir, relPath));
            if (IsInsideFolder(fullPath, gameDir))
                toRemove.Add(Path.GetRelativePath(gameDir, fullPath));
            else
                Log.Warning($"RemoveFile skipped (outside {gameDir}): {relPath}");
        }
        
        File.Delete(filesToDeleteTxt); // remove this file to avoid copying it to game dir
        return toRemove;
    }

    const string ReleaseDeleteFiles = "Release.DeleteFiles.txt";

    // a mod package's files, and the installed mod's files it no longer has, relative to their folders
    internal sealed class ModPackage
    {
        public readonly Array<string> Files = new();
        public readonly Array<string> StaleFiles = new();
    }

    // A mod package must be the whole mod: false when it has no Globals.yaml, or when it would delete more than
    // half of an installed mod. Its Release.DeleteFiles.txt is not used: the mod folder is made to match the package.
    public static bool TryReadModPackage(string modDir, string packageDir, out ModPackage package)
    {
        package = new();
        var packageFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (FileInfo file in ListFiles(packageDir))
        {
            string relPath = Path.GetRelativePath(packageDir, file.FullName);
            if (relPath.Equals(ReleaseDeleteFiles, StringComparison.OrdinalIgnoreCase))
                continue;
            packageFiles.Add(relPath);
            package.Files.Add(relPath);
        }
        Array<string> stale = package.StaleFiles;
        if (!packageFiles.Contains("Globals.yaml"))
        {
            Log.Warning($"AutoPatcher: {packageDir} has no Globals.yaml, not a complete mod");
            return false;
        }

        if (!Directory.Exists(modDir) || !IsFolderInMods(modDir))
            return true;
        if (IsReparsePoint(modDir))
        {
            Log.Warning($"AutoPatcher: {modDir} is a link, the files the new version dropped are kept");
            return true;
        }

        int modFiles = 0;
        foreach (FileInfo file in ListFiles(modDir))
        {
            string relPath = Path.GetRelativePath(modDir, file.FullName);
            if (IsGitPath(relPath))
                continue;
            ++modFiles;
            if (!packageFiles.Contains(relPath))
                stale.Add(relPath);
        }

        if (stale.Count * 2 > modFiles && File.Exists(Path.Combine(modDir, "Globals.yaml")))
        {
            Log.Warning($"AutoPatcher: {packageDir} would delete {stale.Count} of the {modFiles} files in {modDir}, not a complete mod");
            stale.Clear();
            return false;
        }
        return true;
    }

    static bool IsFolderInMods(string modDir)
    {
        string parent = Path.GetDirectoryName(Path.GetFullPath(modDir).TrimEnd('\\', '/'));
        return string.Equals(Path.GetFileName(parent), "Mods", StringComparison.OrdinalIgnoreCase);
    }

    // throws on IO errors, and does not follow junctions or symlinks out of the folder
    static FileInfo[] ListFiles(string dir)
    {
        return new DirectoryInfo(dir).GetFiles("*", new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false,
        });
    }

    static bool IsGitPath(string relPath)
    {
        string first = relPath.Split('\\', '/')[0];
        return first.Equals(".git", StringComparison.OrdinalIgnoreCase);
    }

    void CopyNewFiles(string patchFilesFolder, ProgressBarElement ap)
    {
        try
        {
            string gameDir = GetGameDirectory();
            string tempDir = GetPatchTempFolder();
            Array<string> skipped = CopyFiles(patchFilesFolder, gameDir, tempDir, IsMod ? ModUpdate : null, ap);

            // Apply succeeded — now safe to drop the staging cache. Crucially this only
            // runs on the SUCCESS path: if the loop above threw, the staging dir is
            // preserved so the user can re-trigger the patch and we resume against an
            // intact cached download. The old File.Move-based design destroyed sources
            // as it went, leaving a half-gutted staging dir on any mid-apply failure.
            TryDeleteFolder(GetPatchOutputFolder());

            bool modUnfinished = IsMod && skipped.Count > 0;
            if (modUnfinished)
                RunOnNextFrame(() => AddWarningLabel(Localizer.Token(GameText.ModInstallUnfinished)));
            else if (skipped.Count > 0)
                RunOnNextFrame(() => AddWarningLabel($"Patch applied ({skipped.Count} file(s) skipped — see blackbox.log; usually antivirus interference)"));

            RunOnNextFrame(() =>
            {
                ProgressSteps.AddLabel("Restarting StarDrive ...")
                    .Anim().Alpha(new(0.5f,1.0f)).Loop();
                CurrentTask = Parallel.Run(() => RestartAsync(waitMs: modUnfinished ? 10_000 : 2900));
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "CopyNewFiles failed");
            AddErrorMessageAndAllowExit("Copy New Files failed!", e.Message);
        }
    }

    // a mod: its checked files (none through a link in it), then its stale files go, then Globals.yaml, each only if all before worked
    internal static Array<string> CopyFiles(string patchFilesFolder, string gameDir, string tempDir,
                                            ModPackage mod, ProgressBarElement ap)
    {
        bool isMod = mod != null;
        string[] filesToAdd = isMod
            ? mod.Files.OrderBy(IsModGlobals).ToArray()
            : Dir.GetFiles(patchFilesFolder).Select(f => f.FullName.Replace(patchFilesFolder, "").TrimStart('\\', '/')).ToArray();
        var skipped = new Array<string>();
        var plainDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int currentAction = 0;
        int lockedFiles = 0;
        // Per-file retry budget for stash-aside in MoveAndCreateDirs. Starts at 3 attempts
        // (100/200/300 ms escalating waits) — handles isolated AV scans. After 5 files have
        // exhausted retries we conclude the lock is systemic (AV scanning the whole patch
        // dir, OneDrive batch-syncing, etc.) and drop to a single attempt so we don't burn
        // minutes waiting on locks that won't clear this run. Skipped files keep their old
        // version and the user re-runs the patcher to pick them up next round.
        int maxRetries = 3;
        const int systemicLockThreshold = 5;
        foreach (string relPath in filesToAdd)
        {
            string srcFile = Path.Combine(patchFilesFolder, relPath);
            string dstFile = Path.Combine(gameDir, relPath);

            if (isMod && IsModGlobals(relPath))
            {
                if (skipped.Count == 0 && File.Exists(srcFile))
                    RemoveStaleModFiles(gameDir, tempDir, mod.StaleFiles, skipped);
                if (skipped.Count > 0)
                {
                    Log.Warning($"CopyFile skipped: {relPath}, the mod keeps its old version after {skipped.Count} skipped file(s)");
                    skipped.Add(relPath);
                    ap?.SetProgress(ProgressBarElement.GetPercent(++currentAction, filesToAdd.Length));
                    continue;
                }
            }
            else if (isMod && IsLinkedBelow(gameDir, relPath, plainDirs))
            {
                Log.Warning($"CopyFile skipped (a folder on its path is a link or cannot be read): {relPath}");
                skipped.Add(relPath);
                ap?.SetProgress(ProgressBarElement.GetPercent(++currentAction, filesToAdd.Length));
                continue;
            }

            // Source can vanish between the package listing and now — typically
            // AV/Defender quarantining a freshly-extracted asset (esp. .png/.exe) or
            // OneDrive/Dropbox re-syncing the AppData folder. Skip and continue rather
            // than aborting the entire patch over one cosmetic icon. The user can
            // re-run the patcher to retry; missing textures fall back to x_red so the
            // game stays usable in the meantime.
            if (!File.Exists(srcFile))
            {
                Log.Warning($"CopyFile skipped (source missing — AV or sync interference?): {relPath}");
                skipped.Add(relPath);
                ap?.SetProgress(ProgressBarElement.GetPercent(++currentAction, filesToAdd.Length));
                continue;
            }

            Log.Write($"CopyFile: {relPath}");
            try
            {
                SafeCopy(srcFile, dstFile, relPath, tempDir, maxRetries);
            }
            catch (IOException e)
            {
                // Dst file is locked (AV scan, OneDrive sync, game holding the texture, etc.).
                // SafeCopy already restored the previous dst on failure, so the install is
                // intact for this file. Skip rather than aborting — staging cache survives,
                // user can re-run the patcher to pick up whatever was locked this round.
                // SafeCopy wraps as `new IOException(relPath, e)`, so e.Message is just the
                // relPath we already log. Walk to the inner exception for the actual cause
                // (sharing violation, ACL denied, disk full, etc.).
                string cause = e.InnerException?.Message ?? e.Message;
                Log.Warning($"CopyFile skipped (destination locked — AV or in-use?): {relPath}: {cause}");
                skipped.Add(relPath);
                if (++lockedFiles == systemicLockThreshold && maxRetries > 1)
                {
                    Log.Warning($"AutoPatcher: {systemicLockThreshold} locked files — dropping retries to 1 for remaining files");
                    maxRetries = 1;
                }
            }
            ap?.SetProgress(ProgressBarElement.GetPercent(++currentAction, filesToAdd.Length));
        }

        return skipped;
    }

    static bool IsModGlobals(string relPath) => relPath.Equals("Globals.yaml", StringComparison.OrdinalIgnoreCase);

    // true when a folder between the mod folder and the file is a junction or symlink
    static bool IsLinkedBelow(string modDir, string relPath, HashSet<string> plainDirs)
    {
        for (string dir = Path.GetDirectoryName(relPath); !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
        {
            if (plainDirs.Contains(dir))
                continue;
            try
            {
                if (IsReparsePoint(Path.Combine(modDir, dir)))
                    return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return true; // cannot tell, so the file is skipped
            }
            plainDirs.Add(dir);
        }
        return false;
    }

    static void RemoveStaleModFiles(string modDir, string tempDir, Array<string> staleModFiles, Array<string> skipped)
    {
        foreach (string staleRelPath in staleModFiles)
        {
            Log.Write($"RemoveFile: {staleRelPath}");
            try
            {
                SafeDelete(Path.Combine(modDir, staleRelPath), staleRelPath, tempDir);
            }
            catch (IOException e)
            {
                Log.Warning($"RemoveFile skipped (in use?): {staleRelPath}: {e.InnerException?.Message ?? e.Message}");
                skipped.Add(staleRelPath);
            }
        }
    }

    void AddWarningLabel(string text)
    {
        UILabel label = ProgressSteps.AddLabel("");
        label.MultilineText = new(label.Font.ParseTextToLines(text, ProgressSteps.Width));
        label.Color = Color.Yellow;
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

            Log.LogEventStats(Log.GameEvent.AutoUpdateFailed, message: $"{title}: {details}");
        });
    }

    /// <summary>
    /// Copies a patch file from the staging cache (%APPDATA%\StarDrive\Patches\&lt;ver&gt;)
    /// onto the install. If the destination exists, it is first moved aside into
    /// game/PatchTemp so an in-use file (e.g. StarDrive.exe replacing itself) can
    /// still be replaced — that move is intra-volume and atomic.
    ///
    /// The src→dst transfer is a Copy (not Move) so the staging cache survives a
    /// mid-apply failure intact and the user can retry. Pre-Jupiter this was a
    /// Move, which destroyed each source on success and left a half-gutted cache
    /// on any failure (no recovery without re-downloading).
    /// </summary>
    static void SafeCopy(string srcFile, string dstFile, string relPath, string tempDir, int maxRetries = 3)
    {
        string tmpFile = null;
        try
        {
            if (File.Exists(dstFile))
            {
                tmpFile = MoveToTempPath(tempDir, relPath, dstFile, maxRetries);
            }
            CopyAndCreateDirs(srcFile, dstFile);
        }
        catch (Exception e)
        {
            if (tmpFile != null) // restore the file if needed
            {
                // Restore best-effort: if even the restore fails we can't fix it,
                // and chaining the original cause is more useful than a noisy throw
                // from inside a catch. Log the swallowed reason so an operator
                // reading the patch log can correlate a missing file with the
                // failed restore (otherwise the install just appears truncated).
                try { File.Move(tmpFile, dstFile); }
                catch (Exception restoreEx)
                {
                    Log.Warning(ConsoleColor.Red,
                        $"AutoPatcher: failed to restore {relPath} from {tmpFile}: {restoreEx.Message}");
                }
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

    static void SafeDelete(string fileToDelete, string tempDir)
    {
        SafeDelete(fileToDelete, fileToDelete, tempDir);
    }

    /// <summary>
    /// Moves `theFile` into `tempPath`, returning full path to the temp file,
    /// so that it can be restored if necessary
    /// </summary>
    static string MoveToTempPath(string tempPath, string relPath, string theFile, int maxRetries = 3)
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

        MoveAndCreateDirs(theFile, tmpFile, maxRetries);
        return tmpFile;
    }

    // Used only on the destination side (stashing in-use files into PatchTemp) where
    // Move is genuinely the right primitive — intra-volume, atomic, leaves no copy.
    // For the src(staging-cache) → dst(install) transfer, use CopyAndCreateDirs so
    // a mid-apply failure doesn't destroy the staging cache.
    static void MoveAndCreateDirs(string sourceFile, string destinationFile, int maxAttempts = 3)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

        // Retry on transient sharing violations: AV/Defender often holds a handle on a
        // freshly-touched file for ~100ms after scan start. Linear backoff (100/200/300 ms)
        // covers the common single-file case. CopyFiles drops maxAttempts to 1 once it
        // detects a systemic lock pattern (>5 files failed), to avoid burning minutes on a
        // patch where AV is scanning the whole batch.
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(sourceFile, destinationFile);
                return;
            }
            catch (Exception e) when ((e is IOException || e is UnauthorizedAccessException) && attempt < maxAttempts)
            {
                Thread.Sleep(100 * attempt);
            }
            catch (Exception e)
            {
                throw new IOException($"Move failed: {sourceFile} --> {destinationFile}", e);
            }
        }
    }

    static void CopyAndCreateDirs(string sourceFile, string destinationFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            // overwrite:true — patches semantically OVERWRITE the install. SafeCopy
            // also moves any existing dst aside into PatchTemp first, but treating
            // overwrite as the contract (rather than relying on the prior move) is
            // both clearer and safer: re-running the patcher against an already-
            // partially-applied install just clobbers, instead of throwing because
            // some files survived from the previous attempt. Especially relevant
            // for mod patches where users often re-apply the same version.
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
        catch (Exception e)
        {
            throw new IOException($"Copy failed: {sourceFile} --> {destinationFile}", e);
        }
    }

    void RestartAsync(int waitMs)
    {
        Log.Write($"AutoUpdate finished. Restarting in {waitMs / 1000f:0} seconds...");
        bool elevated = IsInRole(WindowsBuiltInRole.Administrator);
        if (elevated)
        {
            Log.Write("AutoPatcher: running as admin, cleaning PatchTemp and trying to relaunch StarDrive without admin rights");
            TryDeletePatchTempFiles();
        }
        Log.FlushAllLogs();
        //Log.LogEventStats(Log.GameEvent.AutoUpdateFinished);

        // Apply succeeded — drop the marker so a future launch doesn't try to
        // resume against an already-applied patch.
        DeletePendingPatchMarker();

        Thread.Sleep(waitMs);
        // RunCleanup releases blackbox.log via Log.Close before we spawn the
        // replacement process — same race as RelaunchAsAdminWithMarker.
        // Application.Exit() is async (just queues a quit message), so
        // Process.Start fires before the old process's message loop has
        // unwound and released file handles. The new patched instance opens
        // the same log path with FileMode.Create at startup, which throws
        // "file is being used by another process" if we still hold it here.
        Program.RunCleanup();

        // Strip --apply-patch from the relaunch args. The elevated instance
        // had it set to drive the resume; the new (non-elevated) instance
        // shouldn't see it or TryResumePending would log a stale-marker warning.
        string args = string.Join(" ",
            Environment.GetCommandLineArgs().Skip(1)
                .Where(a => !a.StartsWith("--apply-patch", StringComparison.OrdinalIgnoreCase)));

        Application.Exit();
        try
        {
            if (!elevated || !UnelevatedProcess.TryStart(Application.ExecutablePath, args, Directory.GetCurrentDirectory()))
                System.Diagnostics.Process.Start(Application.ExecutablePath, args);
        }
        catch (Exception ex)
        {
            // Log is closed; surface via MessageBox so the user knows the
            // patch applied but the relaunch failed (AV quarantined the exe,
            // bad path, etc.) and they need to relaunch manually.
            System.Windows.Forms.MessageBox.Show(
                $"Patch was installed, but restarting the game failed:\n{ex.Message}\n\nPlease relaunch StarDrive manually.",
                "StarDrive — Restart failed",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }
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
