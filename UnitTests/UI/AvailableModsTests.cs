using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.GameScreens.MainMenu;
using static Ship_Game.GameScreens.MainMenu.AvailableModsScreen.ModState;

namespace UnitTests.UI
{
    [TestClass]
    public class AvailableModsTests
    {
        [TestMethod]
        [DataRow(true,  "v1.60.0009", "v1.60.0009", false, DevCopy)]
        [DataRow(true,  null,         null,         true,  DevCopy)]
        [DataRow(false, "v1.60.0009", null,         false, Checking)]
        [DataRow(false, "v1.60.0009", null,         true,  Unavailable)]
        [DataRow(false, null,         "v1.60.0009", false, NotInstalled)]
        [DataRow(false, "v1.60.0008", "v1.60.0009", false, UpdateAvailable)]
        [DataRow(false, "",           "v1.60.0009", false, UpdateAvailable)]
        [DataRow(false, "v1.60.0009", "v1.60.0009", false, UpToDate)]
        [DataRow(false, "v1.60.0010", "v1.60.0009", false, UpToDate)]
        [DataRow(false, "v1.60.0009", "1.60.9.0",   false, UpToDate)]
        public void GetState_Cases(bool devCopy, string installed, string latest, bool checkFailed,
                                   AvailableModsScreen.ModState expected)
        {
            Assert.AreEqual(expected, AvailableModsScreen.GetState(devCopy, installed, latest, checkFailed));
        }

        [TestMethod]
        [DataRow("v1.60.0009", "v1.60.0009", true)]
        [DataRow("v1.60.0009", "1.60.0009",  true)]
        [DataRow("V1.60.9",    "v1.60.0009", true)]
        [DataRow("1.60.9.0",   "v1.60.0009", true)]
        [DataRow("v1.60.0002", "v1.60.0001", false)]
        [DataRow("v1.60.0008", "v1.60.0009", false)]
        [DataRow("",           "v1.60.0009", false)]
        [DataRow("Legacy 3.2", "legacy 3.2", true)]
        public void IsSameVersion_Cases(string installed, string latest, bool expected)
        {
            Assert.AreEqual(expected, AvailableModsScreen.IsSameVersion(installed, latest));
        }

        [TestMethod]
        public void PageUrl_IsTheRepoPage()
        {
            var mod = new AvailableModsScreen.AvailableMod("Combined Arms", "Combined Arms",
                                                           "https://github.com/TeamStarDrive/CombinedArms/releases");
            Assert.AreEqual("https://github.com/TeamStarDrive/CombinedArms", mod.PageUrl);
            Assert.AreEqual("Mods/Combined Arms/", mod.ModPath);
        }

        static string NewTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "SDAvailableModsTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        static void WriteFile(string dir, string relPath)
        {
            string path = Path.Combine(dir, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, relPath);
        }

        static string[] AcceptedStaleFiles(string mod, string package)
        {
            Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read), "the package should be accepted as a complete mod");
            string[] files = read.StaleFiles.ToArray();
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            return files;
        }

        [TestMethod]
        public void StaleModFiles_AreTheFilesThePackageDoesNotHave()
        {
            string root = NewTempDir();
            try
            {
                string mod = Path.Combine(root, "Mods", "Some Mod");
                string package = Path.Combine(root, "Patch", "Some Mod");
                WriteFile(mod, "Globals.yaml");
                WriteFile(mod, @"ShipDesigns\Kept.design");
                WriteFile(mod, @"ShipDesigns\Removed.design");
                WriteFile(mod, @"Hulls\OLD\Gone.hull");
                WriteFile(mod, @"Textures\CASE.png");
                WriteFile(mod, @".git\config");
                WriteFile(package, "Globals.yaml");
                WriteFile(package, @"ShipDesigns\Kept.design");
                WriteFile(package, @"ShipDesigns\New.design");
                WriteFile(package, @"textures\case.png");

                CollectionAssert.AreEqual(new[] { @"Hulls\OLD\Gone.hull", @"ShipDesigns\Removed.design" },
                                          AcceptedStaleFiles(mod, package));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_APackageWithoutGlobalsIsNotACompleteMod()
        {
            string root = NewTempDir();
            try
            {
                string mod = Path.Combine(root, "Mods", "Some Mod");
                WriteFile(mod, "Globals.yaml");
                WriteFile(mod, @"ShipDesigns\A.design");
                WriteFile(mod, @"ShipDesigns\B.design");
                WriteFile(mod, @"ShipDesigns\C.design");

                string package = Path.Combine(root, "Patch");
                WriteFile(package, @"ShipDesigns\A.design");
                WriteFile(package, @"ShipDesigns\B.design");
                WriteFile(package, @"ShipDesigns\C.design");
                WriteFile(package, @"ShipDesigns\D.design");
                Assert.IsFalse(AutoPatcher.TryReadModPackage(mod, package, out _), "installed mod");
                Assert.IsFalse(AutoPatcher.TryReadModPackage(Path.Combine(root, "Mods", "New Mod"), package, out _), "fresh install");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_APackageThatDeletesMostOfTheModIsNotACompleteMod()
        {
            string root = NewTempDir();
            try
            {
                string mod = Path.Combine(root, "Mods", "Some Mod");
                WriteFile(mod, "Globals.yaml");
                WriteFile(mod, @"ShipDesigns\A.design");
                WriteFile(mod, @"ShipDesigns\B.design");
                WriteFile(mod, @"ShipDesigns\C.design");

                string nested = Path.Combine(root, "Patch");
                WriteFile(nested, "Globals.yaml");
                WriteFile(nested, @"Some Mod\Globals.yaml");
                WriteFile(nested, @"Some Mod\ShipDesigns\A.design");
                WriteFile(nested, @"Some Mod\ShipDesigns\B.design");
                WriteFile(nested, @"Some Mod\ShipDesigns\C.design");
                Assert.IsFalse(AutoPatcher.TryReadModPackage(mod, nested, out var read));
                Assert.AreEqual(0, read.StaleFiles.Count);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_AFolderWithoutGlobalsIsCleanedOut()
        {
            string root = NewTempDir();
            try
            {
                string brokenInstall = Path.Combine(root, "Mods", "Some Mod");
                WriteFile(brokenInstall, "Leftover1.txt");
                WriteFile(brokenInstall, "Leftover2.txt");
                string package = Path.Combine(root, "Patch");
                WriteFile(package, "Globals.yaml");
                WriteFile(package, "Races.xml");
                CollectionAssert.AreEqual(new[] { "Leftover1.txt", "Leftover2.txt" }, AcceptedStaleFiles(brokenInstall, package));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_OnlyInAFolderDirectlyUnderMods()
        {
            string root = NewTempDir();
            try
            {
                string notAMod = Path.Combine(root, "Game");
                string package = Path.Combine(root, "Patch");
                WriteFile(notAMod, "Globals.yaml");
                WriteFile(notAMod, "StarDrive.exe");
                WriteFile(package, "Globals.yaml");
                WriteFile(package, "Races.xml");
                WriteFile(Path.Combine(root, "Mods"), "Other.txt");
                Assert.AreEqual(0, AcceptedStaleFiles(notAMod, package).Length);
                Assert.AreEqual(0, AcceptedStaleFiles(Path.Combine(root, "Mods") + "/", package).Length);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_ThrowsWhenThePackageCannotBeListed()
        {
            string root = NewTempDir();
            try
            {
                string mod = Path.Combine(root, "Mods", "Some Mod");
                WriteFile(mod, "Globals.yaml");
                Assert.ThrowsException<DirectoryNotFoundException>(
                    () => AutoPatcher.TryReadModPackage(mod, Path.Combine(root, "Missing"), out _));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        static void MakeJunction(string link, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(link)!);
            var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
            {
                CreateNoWindow = true, UseShellExecute = false
            });
            mklink!.WaitForExit();
            if (!Directory.Exists(link) || (File.GetAttributes(link) & FileAttributes.ReparsePoint) == 0)
                Assert.Inconclusive("could not create a directory junction");
        }

        static void DeleteWithJunction(string root, string link)
        {
            if (Directory.Exists(link))
                Directory.Delete(link);
            Directory.Delete(root, recursive: true);
        }

        [TestMethod]
        public void StaleModFiles_DoNotFollowAJunctionOutOfTheMod()
        {
            string root = NewTempDir();
            string link = Path.Combine(root, "Mods", "Some Mod", "Link");
            try
            {
                string mod = Path.Combine(root, "Mods", "Some Mod");
                string outside = Path.Combine(root, "Outside");
                string package = Path.Combine(root, "Patch");
                WriteFile(mod, "Globals.yaml");
                WriteFile(mod, "Races.xml");
                WriteFile(outside, "Secret.txt");
                WriteFile(package, "Globals.yaml");
                WriteFile(package, "Races.xml");
                MakeJunction(link, outside);

                Assert.AreEqual(0, AcceptedStaleFiles(mod, package).Length);
            }
            finally
            {
                DeleteWithJunction(root, link);
            }
        }

        [TestMethod]
        public void StaleModFiles_NoneWhenTheModFolderIsALink()
        {
            string root = NewTempDir();
            string mod = Path.Combine(root, "Mods", "Some Mod");
            try
            {
                string export = Path.Combine(root, "Export");
                string package = Path.Combine(root, "Patch");
                WriteFile(export, "Globals.yaml");
                WriteFile(export, "Races.xml");
                WriteFile(export, "Extra.txt");
                WriteFile(package, "Globals.yaml");
                WriteFile(package, "Races.xml");
                MakeJunction(mod, export);

                Assert.AreEqual(0, AcceptedStaleFiles(mod, package).Length);
                Assert.AreEqual(0, AcceptedStaleFiles(mod + "\\", package).Length, "the mod path ends with a separator in game");
            }
            finally
            {
                DeleteWithJunction(root, mod);
            }
        }

        [TestMethod]
        public void PatchTempCleanup_DoesNotFollowAJunction()
        {
            string root = NewTempDir();
            string tempDir = Path.Combine(root, "PatchTemp");
            string link = Path.Combine(tempDir, "Link");
            try
            {
                string outside = Path.Combine(root, "Outside");
                WriteFile(tempDir, @"Old\StarDrive.dll");
                WriteFile(outside, "Secret.txt");
                MakeJunction(link, outside);

                AutoPatcher.TryDeleteFilesAndFolder(tempDir);
                Assert.IsTrue(File.Exists(Path.Combine(outside, "Secret.txt")), "a file behind the junction was deleted");
                Assert.IsFalse(File.Exists(Path.Combine(tempDir, @"Old\StarDrive.dll")));
            }
            finally
            {
                DeleteWithJunction(root, link);
            }
        }

        [TestMethod]
        public void PatchTempCleanup_ALinkedFolderIsOnlyUnlinked()
        {
            string root = NewTempDir();
            string tempDir = Path.Combine(root, "PatchTemp");
            try
            {
                string outside = Path.Combine(root, "Outside");
                WriteFile(outside, "Secret.txt");
                MakeJunction(tempDir, outside);

                AutoPatcher.TryDeleteFilesAndFolder(tempDir);
                Assert.IsTrue(File.Exists(Path.Combine(outside, "Secret.txt")), "a file behind the junction was deleted");
                Assert.IsFalse(Directory.Exists(tempDir));
            }
            finally
            {
                DeleteWithJunction(root, tempDir);
            }
        }

        static string ReadFile(string dir, string relPath) => File.ReadAllText(Path.Combine(dir, relPath));

        static string NewModUpdate(string root, out string mod, out string package)
        {
            mod = Path.Combine(root, "Mods", "Some Mod");
            package = Path.Combine(root, "Patch");
            File.WriteAllText(Path.Combine(Directory.CreateDirectory(mod).FullName, "Globals.yaml"), "v1");
            File.WriteAllText(Path.Combine(mod, "Races.xml"), "v1");
            WriteFile(mod, "Dropped.txt");
            File.WriteAllText(Path.Combine(Directory.CreateDirectory(package).FullName, "Globals.yaml"), "v2");
            File.WriteAllText(Path.Combine(package, "Races.xml"), "v2");
            return Path.Combine(root, "PatchTemp");
        }

        [TestMethod]
        public void CopyFiles_AModGetsItsNewVersionAndLosesTheDroppedFiles()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));

                var skipped = AutoPatcher.CopyFiles(package, mod, tempDir, read, null);
                Assert.AreEqual(0, skipped.Count);
                Assert.AreEqual("v2", ReadFile(mod, "Globals.yaml"));
                Assert.AreEqual("v2", ReadFile(mod, "Races.xml"));
                Assert.IsFalse(File.Exists(Path.Combine(mod, "Dropped.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void CopyFiles_AfterASkippedFileTheModKeepsItsVersionAndItsFiles()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));

                using (new FileStream(Path.Combine(mod, "Races.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var skipped = AutoPatcher.CopyFiles(package, mod, tempDir, read, null);
                    CollectionAssert.AreEquivalent(new[] { "Races.xml", "Globals.yaml" }, skipped.ToArray());
                }
                Assert.AreEqual("v1", ReadFile(mod, "Globals.yaml"), "the mod must keep showing its old version");
                Assert.AreEqual("v1", ReadFile(mod, "Races.xml"));
                Assert.IsTrue(File.Exists(Path.Combine(mod, "Dropped.txt")), "the old version still needs its files");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        [DataRow("Races.xml")]
        [DataRow("Globals.yaml")]
        public void CopyFiles_AFileThatLeftThePackageAfterTheCheckLeavesTheOldVersion(string removed)
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));
                File.Delete(Path.Combine(package, removed)); // antivirus took it

                var skipped = AutoPatcher.CopyFiles(package, mod, tempDir, read, null);
                CollectionAssert.AreEquivalent(new[] { removed, "Globals.yaml" }.Distinct().ToArray(), skipped.ToArray());
                Assert.AreEqual("v1", ReadFile(mod, "Globals.yaml"));
                Assert.IsTrue(File.Exists(Path.Combine(mod, "Dropped.txt")), "the old version still needs its files");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void CopyFiles_AStaleFileThatCannotBeRemovedLeavesTheOldVersionShowing()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));

                using (new FileStream(Path.Combine(mod, "Dropped.txt"), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var skipped = AutoPatcher.CopyFiles(package, mod, tempDir, read, null);
                    CollectionAssert.AreEquivalent(new[] { "Dropped.txt", "Globals.yaml" }, skipped.ToArray());
                }
                Assert.AreEqual("v1", ReadFile(mod, "Globals.yaml"), "Update must stay offered");
                Assert.AreEqual("v2", ReadFile(mod, "Races.xml"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void CopyFiles_NothingIsWrittenThroughALinkInTheMod()
        {
            string root = NewTempDir();
            string link = Path.Combine(root, "Mods", "Some Mod", "Textures");
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                string outside = Path.Combine(root, "Outside");
                File.WriteAllText(Path.Combine(Directory.CreateDirectory(outside).FullName, "Ship.png"), "outside");
                File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(package, "Textures")).FullName, "Ship.png"), "v2");
                MakeJunction(link, outside);
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));

                var skipped = AutoPatcher.CopyFiles(package, mod, tempDir, read, null);
                CollectionAssert.AreEquivalent(new[] { @"Textures\Ship.png", "Globals.yaml" }, skipped.ToArray());
                Assert.AreEqual("outside", ReadFile(outside, "Ship.png"));
                Assert.AreEqual("v1", ReadFile(mod, "Globals.yaml"));
            }
            finally
            {
                DeleteWithJunction(root, link);
            }
        }

        [TestMethod]
        public void CopyFiles_NewFoldersInThePackageAreCopied()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                WriteFile(package, @"Textures\Chukk\Ship.png");
                WriteFile(package, @"Audio\Theme.wav");
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));

                Assert.AreEqual(0, AutoPatcher.CopyFiles(package, mod, tempDir, read, null).Count);
                Assert.IsTrue(File.Exists(Path.Combine(mod, @"Textures\Chukk\Ship.png")));
                Assert.IsTrue(File.Exists(Path.Combine(mod, @"Audio\Theme.wav")));
                Assert.AreEqual("v2", ReadFile(mod, "Globals.yaml"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void CopyFiles_AModPackageDeleteListIsNeitherUsedNorCopied()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string mod, out string package);
                File.WriteAllText(Path.Combine(package, "Release.DeleteFiles.txt"), "1a91;60B4;Races.xml");
                Assert.IsTrue(AutoPatcher.TryReadModPackage(mod, package, out var read));
                CollectionAssert.AreEquivalent(new[] { "Globals.yaml", "Races.xml" }, read.Files.ToArray());

                AutoPatcher.RemoveReleaseDeleteFiles(package, mod, tempDir, isMod: true, null);
                Assert.AreEqual("v1", ReadFile(mod, "Races.xml"), "a mod's delete list must not run before the copy");
                Assert.AreEqual(0, AutoPatcher.CopyFiles(package, mod, tempDir, read, null).Count);
                Assert.IsFalse(File.Exists(Path.Combine(mod, "Release.DeleteFiles.txt")));
                Assert.AreEqual("v2", ReadFile(mod, "Races.xml"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void CopyFiles_ABlackBoxPatchCopiesEveryFileItCan()
        {
            string root = NewTempDir();
            try
            {
                string tempDir = NewModUpdate(root, out string game, out string package);
                WriteFile(game, "Audio.xml");
                WriteFile(package, "Audio.xml");
                using (new FileStream(Path.Combine(game, "Audio.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    var skipped = AutoPatcher.CopyFiles(package, game, tempDir, null, null);
                    CollectionAssert.AreEqual(new[] { "Audio.xml" }, skipped.ToArray());
                }
                Assert.AreEqual("v2", ReadFile(game, "Globals.yaml"), "a file listed after a skipped one is still copied");
                Assert.AreEqual("v2", ReadFile(game, "Races.xml"));
                Assert.IsTrue(File.Exists(Path.Combine(game, "Dropped.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        [DataRow(@"C:\Game\Content\a.png", @"C:\Game",  true)]
        [DataRow(@"C:\Game\Content\a.png", @"C:\Game\", true)]
        [DataRow(@"D:\Content\a.png",      @"D:\",      true)]
        [DataRow(@"C:\Game 2\a.png",       @"C:\Game",  false)]
        [DataRow(@"C:\Game\..\a.png",      @"C:\Game",  false)]
        [DataRow(@"C:\Game",               @"C:\Game",  false)]
        public void IsInsideFolder_Cases(string path, string folder, bool expected)
        {
            Assert.AreEqual(expected, AutoPatcher.IsInsideFolder(path, folder));
        }

        [TestMethod]
        public void FilesToRemove_OnlyInsideTheGameFolder()
        {
            string root = NewTempDir();
            try
            {
                string game = Path.Combine(root, "Game");
                string list = Path.Combine(root, "Release.DeleteFiles.txt");
                File.WriteAllLines(list, new[]
                {
                    @"1a91;60B4;Content\Old.png",
                    @"1a91;60B4;\..\Outside.txt",
                    @"1a91;60B4;C:\Windows\win.ini",
                    @"1a91;60B4;Content\..\Content\Kept.png",
                    "",
                });
                CollectionAssert.AreEqual(new[] { @"Content\Old.png", @"Content\Kept.png" },
                                          AutoPatcher.GetFilesToRemove(list, game).ToArray());
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void Unzip_RejectsAnEntryNextToTheFolder()
        {
            string root = NewTempDir();
            try
            {
                string zip = Path.Combine(root, "patch.zip");
                using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                {
                    using var writer = new StreamWriter(archive.CreateEntry("../Patch 1x/Evil.txt").Open());
                    writer.Write("evil");
                }
                string output = Path.Combine(root, "Patch 1");
                Assert.ThrowsExactly<IOException>(() => AutoPatcher.UnzipWithProgress(zip, output, null, null));
                Assert.IsFalse(File.Exists(Path.Combine(root, "Patch 1x", "Evil.txt")));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void StaleModFiles_NoneForAFreshInstall()
        {
            string root = NewTempDir();
            try
            {
                string package = Path.Combine(root, "Patch");
                WriteFile(package, "Globals.yaml");
                Assert.AreEqual(0, AcceptedStaleFiles(Path.Combine(root, "Mods", "Not Installed"), package).Length);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [TestMethod]
        public void DevCopy_IsAModFolderWithGit()
        {
            string root = NewTempDir();
            try
            {
                string plain = Path.Combine(root, "Plain") + "/";
                string repo = Path.Combine(root, "Repo") + "/";
                string worktree = Path.Combine(root, "Worktree") + "/";
                WriteFile(plain, "Globals.yaml");
                WriteFile(repo, @".git\HEAD");
                WriteFile(worktree, ".git");

                Assert.IsFalse(AutoPatcher.IsDevCopy(plain));
                Assert.IsTrue(AutoPatcher.IsDevCopy(repo));
                Assert.IsTrue(AutoPatcher.IsDevCopy(worktree), "a git worktree has a .git file, not a folder");
                Assert.IsFalse(AutoPatcher.IsDevCopy(Path.Combine(root, "Missing") + "/"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        const string GameDir = @"C:\Games\StarDrive";

        static string Marker(string version, string modPath)
            => JsonSerializer.Serialize(new { Version = version, Name = "Combined Arms v1.60.0010", ModPath = modPath });

        [TestMethod]
        [DataRow(null, true)]
        [DataRow("Mods/Combined Arms/", true)]
        [DataRow(@"Mods\Star Trek", true)]
        [DataRow("", false)]
        [DataRow("Mods/", false)]
        [DataRow("Mods/../", false)]
        [DataRow("Mods/../../Windows/System32/", false)]
        [DataRow("Mods/Combined Arms/Races/", false)]
        [DataRow(@"C:\Windows\System32\", false)]
        public void ResumeMarker_OnlyAFolderDirectlyUnderMods(string modPath, bool accepted)
        {
            var marker = AutoPatcher.ParseMarker(Marker("v1.60.0010", modPath), "v1.60.0010", GameDir);
            Assert.AreEqual(accepted, marker != null, modPath);
        }

        [TestMethod]
        public void ResumeMarker_ForAnotherVersionOrUnreadableIsIgnored()
        {
            Assert.IsNull(AutoPatcher.ParseMarker(Marker("v1.60.0009", "Mods/Combined Arms/"), "v1.60.0010", GameDir));
            Assert.IsNull(AutoPatcher.ParseMarker("{ not json", "v1.60.0010", GameDir));
            Assert.IsNull(AutoPatcher.ParseMarker("null", "v1.60.0010", GameDir));
        }

        [TestMethod]
        [DataRow("https://github.com/TeamStarDrive/CombinedArms/releases",
                 "https://api.github.com/repos/TeamStarDrive/CombinedArms/releases")]
        [DataRow("https://github.com/TeamStarDrive/StarTrekShatteredAlliance/releases",
                 "https://api.github.com/repos/TeamStarDrive/StarTrekShatteredAlliance/releases")]
        public void GitHubReleasesApi_FromDownloadSite(string downloadSite, string expected)
        {
            Assert.AreEqual(expected, AutoUpdateChecker.GitHubReleasesApi(downloadSite));
        }

        [TestMethod]
        public void ReleaseInfo_SortsZipChunksAndSumsTheirSize()
        {
            string json = """
            {
                "name": "Star Trek v1.60.0001", "tag_name": "v1.60.0001", "body": "notes",
                "assets": [
                    { "name": "002-StarTrek.zip", "size": 300, "browser_download_url": "https://x/002-StarTrek.zip" },
                    { "name": "Globals.yaml",     "size": 7,   "browser_download_url": "https://x/Globals.yaml" },
                    { "name": "001-StarTrek.zip", "size": 200, "browser_download_url": "https://x/001-StarTrek.zip" }
                ]
            }
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            ReleaseInfo info = AutoUpdateChecker.ToReleaseInfo(doc.RootElement);
            Assert.AreEqual("v1.60.0001", info.Version);
            CollectionAssert.AreEqual(new[] { "https://x/001-StarTrek.zip", "https://x/002-StarTrek.zip" }, info.ZipUrls);
            Assert.AreEqual(500L, info.DownloadBytes);
        }
    }
}
