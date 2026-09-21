using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Codex
{
    // Tooltip token -> Codex entry. Authored in CodexHooks.yaml as a flat map of
    // GameText NameId to entry UID, so wiring a tooltip to its entry is a content
    // change, not a code change: every tooltip showing that token, on whichever
    // screen, gets the "Press F1 for details" line and F1 opens the entry.
    public static class CodexHooks
    {
        static Map<int, string> ByTokenId = new();
        static Map<string, string> ByNameId = new();
        static bool Loaded;

        public static int Count => ByNameId.Count;

        // The entry a tooltip links to, or null. Only localized tooltips can be
        // hooked: a raw or formatted string is dynamic status text, not a concept.
        public static string Find(in LocalizedText tip)
        {
            if (!Loaded)
                Reload();

            if (tip.Id > 0)
                return ByTokenId.TryGetValue(tip.Id, out string byId) ? byId : null;
            if (tip.Method == LocalizationMethod.NameId && tip.String.NotEmpty())
                return ByNameId.TryGetValue(tip.String, out string byName) ? byName : null;
            return null;
        }

        // Re-read CodexHooks.yaml, checking every row against the entries the
        // Codex will actually list. The Codex screen calls this when it opens, so
        // an author can edit the table with the game running.
        public static void Reload()
        {
            try
            {
                var file = ResourceManager.GetModOrVanillaFile("CodexHooks.yaml");
                if (file == null || !file.Exists)
                {
                    Load(null, knownUids: null);
                    return;
                }

                using var parser = new YamlParser(file);
                Load(parser.Root, CodexEntry.HookableUids(CodexEntry.LoadAll()));
            }
            catch (Exception e)
            {
                // the first Find happens inside a tooltip hover; a locked or broken
                // file must cost the hooks, not the frame
                Log.Error(e, "CodexHooks: could not load CodexHooks.yaml");
                Load(null, knownUids: null);
            }
        }

        // Content is being unloaded (mod switch): the next Find re-reads the table
        public static void Invalidate() => Loaded = false;

        // The testable core. `root` is the flat map; `knownUids` are the topics a
        // row may target (null skips that check). A row naming an unknown token,
        // an unknown or hidden entry, or a header is dropped with a warning, so a
        // typo shows up in the log rather than as an F1 that does nothing.
        public static void Load(YamlNode root, HashSet<string> knownUids)
        {
            var byId   = new Map<int, string>();
            var byName = new Map<string, string>();

            if (root != null)
            {
                foreach (YamlNode row in root.Nodes)
                {
                    string nameId = row.Name;
                    string uid    = row.ValueText;
                    if (nameId.IsEmpty() || uid.IsEmpty())
                    {
                        Log.Warning($"CodexHooks: ignoring '{row}', expected 'TokenNameId: codex_uid'");
                        continue;
                    }
                    if (knownUids != null && !knownUids.Contains(uid))
                    {
                        Log.Warning($"CodexHooks: '{nameId}' points at '{uid}', which is not a topic the Codex lists");
                        continue;
                    }
                    if (!TryGetTokenId(nameId, out int id))
                    {
                        Log.Warning($"CodexHooks: '{nameId}' is not a GameText token");
                        continue;
                    }
                    if (byName.ContainsKey(nameId))
                        Log.Warning($"CodexHooks: '{nameId}' appears twice, the last row wins");
                    byId[id]       = uid;
                    byName[nameId] = uid;
                }
            }

            ByTokenId = byId;
            ByNameId  = byName;
            Loaded    = true;
        }

        // A row may name the token as GameText.yaml keys it or as the GameText enum
        // spells it; a few differ (a BB_ prefix, a typo in the yaml key).
        static bool TryGetTokenId(string nameId, out int id)
        {
            if (Localizer.TryGetTokenId(nameId, out id))
                return true;
            if (Enum.TryParse(nameId, out GameText token) && (int)token > 0)
            {
                id = (int)token;
                return true;
            }
            return false;
        }
    }
}
