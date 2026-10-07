using System;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.GameScreens.MainMenu;
using static Ship_Game.GameScreens.MainMenu.AutoUpdateChecker.UpdateAvailability;

namespace UnitTests.UI
{
    [TestClass]
    public class AutoUpdateCheckerTests
    {
        [TestMethod]
        // older latest -> nothing (the bug that triggered the fix)
        [DataRow("1.51.15118", "1.60.00000", None)]
        [DataRow("1.51.15118", "1.60.00002", None)]
        [DataRow("1.59.99999", "1.60.00000", None)]
        // equal -> nothing
        [DataRow("1.60.00000", "1.60.00000", None)]
        // same major.minor, newer build -> in-game patch
        [DataRow("1.60.00001", "1.60.00000", InGamePatch)]
        [DataRow("1.60.00002", "1.60.00000", InGamePatch)]
        [DataRow("1.60.10000", "1.60.00000", InGamePatch)]
        // cross-major (newer) -> popup
        [DataRow("1.60.00000", "1.51.15118", CrossMajor)]
        [DataRow("2.0.0",      "1.60.00000", CrossMajor)]
        [DataRow("1.61.0",     "1.60.00002", CrossMajor)]
        // unparseable -> Unparseable (don't fire on garbage)
        [DataRow("not-a-version", "1.60.00000", Unparseable)]
        [DataRow("1.60.00000", "garbage",      Unparseable)]
        [DataRow("",          "1.60.00000",   Unparseable)]
        public void ClassifyVanillaUpdate_Cases(string latest, string current,
            AutoUpdateChecker.UpdateAvailability expected)
        {
            Assert.AreEqual(expected, AutoUpdateChecker.ClassifyVanillaUpdate(latest, current));
        }

        [TestMethod]
        public void DisplayLabel_TrimsToMajorMinor_WithCodename()
        {
            Assert.AreEqual("Jupiter 1.60",
                AutoUpdateChecker.BuildMajorUpgradeDisplayLabel("1.60.00002", "Jupiter"));
        }

        [TestMethod]
        public void DisplayLabel_TrimsToMajorMinor_WithoutBuildNumber()
        {
            // GitHub release-tag form ("jupiter-release-1.60") yields "1.60" already
            Assert.AreEqual("Jupiter 1.60",
                AutoUpdateChecker.BuildMajorUpgradeDisplayLabel("1.60", "Jupiter"));
        }

        [TestMethod]
        public void DisplayLabel_FallsBackToBlackBox_WhenCodenameMissing()
        {
            Assert.AreEqual("BlackBox 1.60",
                AutoUpdateChecker.BuildMajorUpgradeDisplayLabel("1.60.00002", null));
            Assert.AreEqual("BlackBox 1.60",
                AutoUpdateChecker.BuildMajorUpgradeDisplayLabel("1.60.00002", ""));
        }

        [TestMethod]
        [DataRow("jupiter-release-1.60",   "Jupiter")]
        [DataRow("jupiter-patch-1.60.00002", "Jupiter")]
        [DataRow("mars-patch-1.51.15118",  "Mars")]
        [DataRow("MARS-release-1.51",      "Mars")] // case-folding
        [DataRow("v1.60.00002",            null)]   // no codename token
        [DataRow("1.60",                   null)]   // numeric-only first segment
        [DataRow("",                       null)]
        [DataRow(null,                     null)]
        public void ExtractCodename_Cases(string tag, string expected)
        {
            Assert.AreEqual(expected, AutoUpdateChecker.ExtractCodenameFromTag(tag));
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_SkipsPreReleases()
        {
            // Pre-release 1.60.00012 must NOT win against a stable 1.60.00009 —
            // matches GitHub's /releases/latest semantic and prevents staged
            // patches from auto-distributing.
            string json = """
            [
                { "tag_name": "jupiter-patch-1.60.00012", "prerelease": true },
                { "tag_name": "jupiter-patch-1.60.00009", "prerelease": false }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, predicate: null,
                out _, out Version best);
            Assert.IsTrue(found, "Stable release should be selected when a higher pre-release exists");
            Assert.AreEqual(new Version(1, 60, 9), best);
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_AllStable_PicksHighest()
        {
            string json = """
            [
                { "tag_name": "jupiter-patch-1.60.00010", "prerelease": false },
                { "tag_name": "jupiter-patch-1.60.00012", "prerelease": false },
                { "tag_name": "jupiter-patch-1.60.00011", "prerelease": false }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, predicate: null,
                out _, out Version best);
            Assert.IsTrue(found);
            Assert.AreEqual(new Version(1, 60, 12), best);
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_AllPreRelease_ReturnsFalse()
        {
            // If every release in the array is flagged pre-release, the scan
            // returns false and the caller falls back to no-update behavior —
            // never silently distributes a pre-release.
            string json = """
            [
                { "tag_name": "jupiter-patch-1.60.00011", "prerelease": true },
                { "tag_name": "jupiter-patch-1.60.00012", "prerelease": true }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, predicate: null,
                out _, out _);
            Assert.IsFalse(found);
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_MissingPrereleaseField_TreatsAsStable()
        {
            // GitHub API responses always include `prerelease`, but be defensive
            // — if the field is missing for any reason, treat as stable so a
            // malformed payload can't silently hide releases.
            string json = """
            [
                { "tag_name": "jupiter-patch-1.60.00009" }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, predicate: null,
                out _, out Version best);
            Assert.IsTrue(found);
            Assert.AreEqual(new Version(1, 60, 9), best);
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_StripsVPrefixOnModTags()
        {
            // Mod tag convention is v<major>.<minor>.NNNN. The `v` must be
            // stripped before Version.TryParse or the tag is silently
            // discarded and TrySelectMaxVersionRelease returns false.
            string json = """
            [
                { "tag_name": "v1.60.0010", "prerelease": false },
                { "tag_name": "v1.60.0014", "prerelease": false },
                { "tag_name": "v1.60.0012", "prerelease": false }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, predicate: null,
                out _, out Version best);
            Assert.IsTrue(found, "v-prefixed mod tags must parse");
            Assert.AreEqual(new Version(1, 60, 14), best);
        }

        [TestMethod]
        public void TrySelectMaxVersionRelease_PredicateFiltersToVanillaLine()
        {
            // Mod releases page mixes a higher 1.61 release with the
            // 1.60 line. With a Jupiter 1.60 install, the major.minor
            // predicate must skip 1.61 and pick the highest 1.60.NNNN.
            string json = """
            [
                { "tag_name": "v1.61.0001", "prerelease": false },
                { "tag_name": "v1.60.0014", "prerelease": false },
                { "tag_name": "v1.60.0009", "prerelease": false },
                { "tag_name": "v1.51.0042", "prerelease": false }
            ]
            """;
            using JsonDocument doc = JsonDocument.Parse(json);
            bool ModLinePredicate(Version v) => v.Major == 1 && v.Minor == 60;
            bool found = AutoUpdateChecker.TrySelectMaxVersionRelease(
                doc.RootElement, ModLinePredicate,
                out _, out Version best);
            Assert.IsTrue(found, "Highest 1.60.NNNN must be picked when predicate filters out 1.61/1.51");
            Assert.AreEqual(new Version(1, 60, 14), best);
        }

        [TestMethod]
        public void IsModLatestNewer_SameLine_PicksByPatch()
        {
            // Both parse and share major.minor — direct numeric compare.
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0014", "v1.60.0009"),
                "Higher patch on same line should be newer");
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("v1.60.0009", "v1.60.0009"),
                "Equal versions should not trigger update");
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("v1.60.0005", "v1.60.0009"),
                "Older patch on same line should not be newer");
        }

        [TestMethod]
        public void IsModLatestNewer_StripsVPrefixOnBothSides()
        {
            // The `v` prefix must be tolerated on either side independently —
            // mod authors might tag with `v` and ship Version without it, or
            // vice versa during the transition.
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0014", "1.60.0009"));
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("1.60.0014",  "v1.60.0009"));
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("v1.60.0009", "1.60.0009"));
        }

        [TestMethod]
        public void IsModLatestNewer_AMissingRevisionCountsAsZero()
        {
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("1.60.9.0", "v1.60.0009"), "the same version with a fourth number");
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("v1.60.0009", "1.60.9.0"));
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("1.60.9.1", "v1.60.0009"));
        }

        [TestMethod]
        public void IsModLatestNewer_Fallback_LegacyUnparseableCurrent()
        {
            // Installed mod version is the legacy free-form string (e.g. "0.5b"
            // or "2024-09-rev3") that won't parse to System.Version. The new
            // aligned release should be promoted as an update so users on
            // legacy mod versions get a one-click upgrade path.
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0014", "0.5b"),
                "Legacy unparseable current must fall back to promoting the candidate");
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0014", "Combined Arms v3.2"),
                "Free-form current must fall back to promoting the candidate");
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0014", ""),
                "Empty current must fall back to promoting the candidate");
        }

        [TestMethod]
        public void IsModLatestNewer_Fallback_DifferentVanillaLine()
        {
            // Installed mod version parses cleanly but on a different
            // major.minor (mod was tracking Mars 1.51, vanilla is now
            // Jupiter 1.60). Treat the new aligned release as an update.
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("v1.60.0001", "v1.51.42"),
                "Mod on a different vanilla line must be promoted to the new aligned release");
            Assert.IsTrue(AutoUpdateChecker.IsModLatestNewer("1.60.0001", "1.51.42"),
                "Same case without v-prefix");
        }

        [TestMethod]
        public void IsModLatestNewer_UnparseableCandidate_ReturnsFalse()
        {
            // Should not happen post-TrySelectMaxVersionRelease (which only
            // emits parseable tags), but if a malformed candidate slips
            // through we refuse rather than promoting blindly.
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("garbage", "v1.60.0009"));
            Assert.IsFalse(AutoUpdateChecker.IsModLatestNewer("", "v1.60.0009"));
        }
    }
}
