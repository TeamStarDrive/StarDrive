using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDUtils;
using Ship_Game;
using Ship_Game.Codex;
using Ship_Game.Data.Yaml;

namespace UnitTests.Codex
{
    [TestClass]
    public class CodexHooksTests : StarDriveTest
    {
        static YamlNode Parse(string yaml)
        {
            using var parser = new YamlParser("hooks", new StringReader(yaml));
            return parser.Root;
        }

        [TestCleanup]
        public void RestoreShippedHooks() => CodexHooks.Reload();

        [TestMethod]
        public void ATooltipTokenResolvesToItsEntry()
        {
            CodexHooks.Load(Parse("IncomingOutGoingTip: economy_freighters_and_trade_routes\n"
                                + "EspionageLimitLevelTip: diplomacy_espionage\n"), // enum spelling; the yaml key is EspionageLevelLimitTip
                            new HashSet<string> { "economy_freighters_and_trade_routes", "diplomacy_espionage" });

            LocalizedText tip = GameText.IncomingOutGoingTip;
            AssertEqual("economy_freighters_and_trade_routes", CodexHooks.Find(tip));
            AssertEqual("diplomacy_espionage", CodexHooks.Find(GameText.EspionageLimitLevelTip), "a row may use the enum name");
            AssertEqual(null, CodexHooks.Find(GameText.NewGame), "an unhooked token has no entry");
            AssertEqual(null, CodexHooks.Find("raw status text"), "raw strings are never hooked");
        }

        [TestMethod]
        public void RowsPointingAtUnknownTokensOrEntriesAreDropped()
        {
            CodexHooks.Load(Parse("NoSuchTokenXyz: economy_economic_basics\n"
                                + "IncomingOutGoingTip: no_such_entry\n"
                                + "TradeTreatiesCreateWealthFor: economy_trade_treaties\n"),
                            new HashSet<string> { "economy_economic_basics", "economy_trade_treaties" });

            AssertEqual(1, CodexHooks.Count, "only the fully valid row survives");
            AssertEqual(null, CodexHooks.Find(GameText.IncomingOutGoingTip));
            AssertEqual("economy_trade_treaties", CodexHooks.Find(GameText.TradeTreatiesCreateWealthFor));
        }

        [TestMethod]
        public void OnlyVisibleTopicsAreHookable()
        {
            HashSet<string> uids = CodexEntry.HookableUids(CodexEntry.LoadAll());

            Assert.IsTrue(uids.Contains("warfare_ordnance"), "a topic nested two levels down is a target");
            Assert.IsFalse(uids.Contains("warfare_weapons"), "a bare header only expands, so it is not a target");
            Assert.IsTrue(uids.Contains("economy_the_colony_screen"), "a category with its own body reads like a topic");
            Assert.IsFalse(uids.Contains("tutorials_overview"), "a topic under a hidden branch is not a target");

            var bodyless = new Array<CodexEntry> { new() { UID = "no_body", TitleId = "CodexTitle", TextId = "NoSuchTokenXyz" } };
            Assert.IsFalse(CodexEntry.HookableUids(bodyless).Contains("no_body"), "a topic whose body token is missing has nothing to open");
        }

        [TestMethod]
        public void EveryVisibleEntryHasItsTokens()
        {
            var missing = new Array<string>();
            void Walk(Array<CodexEntry> entries)
            {
                if (entries == null)
                    return;
                foreach (CodexEntry e in entries)
                {
                    if (e.Hidden)
                        continue;
                    if (!Localizer.Token(e.TitleId, out _))
                        missing.Add($"{e.UID}: {e.TitleId}");
                    if (!e.HasVisibleChildren && !e.HasBody)
                        missing.Add($"{e.UID}: {e.TextId}");
                    Walk(e.Children);
                }
            }
            Walk(CodexEntry.LoadAll());
            AssertEqual(0, missing.Count, $"Codex.yaml entries whose tokens are not in GameText.yaml: {string.Join(", ", missing)}");
        }

        [TestMethod]
        public void ShippedHooksAllResolveAgainstShippedCodex()
        {
            Array<CodexEntry> roots = CodexEntry.LoadAll();
            Assert.IsTrue(roots.Count > 0, "Codex.yaml did not load");
            AssertEqual(0, CodexEntry.DuplicateUids(roots).Count,
                $"Codex.yaml repeats a UID: {string.Join(", ", CodexEntry.DuplicateUids(roots))}");
            HashSet<string> uids = CodexEntry.HookableUids(roots);

            var file = ResourceManager.GetModOrVanillaFile("CodexHooks.yaml");
            Assert.IsTrue(file.Exists, "CodexHooks.yaml is missing");
            using var parser = new YamlParser(file);
            AssertEqual(0, parser.Errors.Count, $"CodexHooks.yaml did not parse cleanly: {string.Join("; ", parser.Errors)}");
            int rows = parser.Root.Nodes.Count;
            Assert.IsTrue(rows > 0, "CodexHooks.yaml has no rows");

            foreach (YamlNode row in parser.Root.Nodes)
                Assert.IsTrue(uids.Contains(row.ValueText), $"'{row.Name}' points at '{row.ValueText}', which the Codex does not list");

            CodexHooks.Load(parser.Root, uids);
            AssertEqual(rows, CodexHooks.Count, "every shipped row must load");
        }
    }
}
