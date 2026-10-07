using System;
using System.Diagnostics;
using System.IO;
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
            Assert.IsTrue(AutoPatcher.TryGetStaleModFiles(mod, package, out var stale), "the package should be accepted as a complete mod");
            string[] files = stale.ToArray();
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
                Assert.IsFalse(AutoPatcher.TryGetStaleModFiles(mod, package, out _), "installed mod");
                Assert.IsFalse(AutoPatcher.TryGetStaleModFiles(Path.Combine(root, "Mods", "New Mod"), package, out _), "fresh install");
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
                Assert.IsFalse(AutoPatcher.TryGetStaleModFiles(mod, nested, out var stale));
                Assert.AreEqual(0, stale.Count);
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
                    () => AutoPatcher.TryGetStaleModFiles(mod, Path.Combine(root, "Missing"), out _));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
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

                var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{outside}\"")
                {
                    CreateNoWindow = true, UseShellExecute = false
                });
                mklink!.WaitForExit();
                if (!File.Exists(Path.Combine(link, "Secret.txt")))
                    Assert.Inconclusive("could not create a directory junction");

                Assert.AreEqual(0, AcceptedStaleFiles(mod, package).Length);
            }
            finally
            {
                if (Directory.Exists(link))
                    Directory.Delete(link);
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
