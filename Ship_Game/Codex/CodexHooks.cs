using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Codex
{
    public static class CodexHooks
    {
        static Map<int, string> ByTokenId = new();
        static Map<string, string> ByNameId = new();
        static bool Loaded;

        public static int Count => ByNameId.Count;

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

        public static void Reload() => Reload(CodexEntry.LoadAll());

        public static void Reload(Array<CodexEntry> roots)
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
                Load(parser.Root, CodexEntry.HookableUids(roots));
            }
            catch (Exception e)
            {
                Log.Error(e, "CodexHooks: could not load CodexHooks.yaml");
                Load(null, knownUids: null);
            }
        }

        public static void Invalidate() => Loaded = false;

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

        static bool TryGetTokenId(string nameId, out int id)
        {
            if (Localizer.TryGetTokenId(nameId, out id))
                return true;
            if (Enum.TryParse(nameId, out GameText token) && Enum.IsDefined(typeof(GameText), token) && (int)token > 0)
            {
                id = (int)token;
                return true;
            }
            return false;
        }
    }
}
