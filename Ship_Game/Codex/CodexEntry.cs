using System.Collections.Generic;
using System.Text;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Codex
{
    [StarDataType]
    public sealed class CodexEntry
    {
        [StarData] public string UID;
        [StarData] public string TitleId;
        [StarData] public string ShortDescId;
        [StarData] public string TextId;
        [StarData] public string Link;
        [StarData] public string VideoPath;
        // Authored but not shown: the branch stays in the yaml so it can be turned
        // back on with one line, and OpenAt() treats it as missing meanwhile.
        [StarData] public bool Hidden;
        [StarData] public Array<CodexEntry> Children;

        // The shipped (or mod) tree with NameIds resolved. Read fresh on every call,
        // so an author editing Codex.yaml sees the change the next time the screen
        // or the hook table loads.
        public static Array<CodexEntry> LoadAll()
        {
            var file = ResourceManager.GetModOrVanillaFile("Codex.yaml");
            Array<CodexEntry> roots = file != null && file.Exists
                ? YamlParser.DeserializeArray<CodexEntry>(file)
                : new Array<CodexEntry>();
            // YamlParser doesn't fire [StarDataDeserialized] hooks, so trigger
            // the UID-driven NameId derivation here explicitly.
            foreach (CodexEntry root in roots)
                root.ResolveDefaults();
            return roots;
        }

        // UIDs a tooltip may link to: listed by the screen (not under a hidden
        // branch) and either a topic or a category with a body of its own. A bare
        // header only expands when clicked, so a link to one would open the Codex
        // with nothing selected.
        public static HashSet<string> HookableUids(Array<CodexEntry> roots)
        {
            var uids = new HashSet<string>();
            CollectHookable(roots, uids);
            return uids;
        }

        static void CollectHookable(Array<CodexEntry> entries, HashSet<string> uids)
        {
            if (entries == null)
                return;
            foreach (CodexEntry e in entries)
            {
                if (e.Hidden)
                    continue;
                if (!string.IsNullOrEmpty(e.UID) && (!e.HasVisibleChildren || e.HasBody))
                    uids.Add(e.UID);
                if (e.HasVisibleChildren)
                    CollectHookable(e.Children, uids);
            }
        }

        // A category shows this when clicked, the same as a topic would
        public bool HasBody => !string.IsNullOrEmpty(TextId) && Localizer.Token(TextId, out _);

        public bool HasVisibleChildren
        {
            get
            {
                if (Children == null)
                    return false;
                for (int i = 0; i < Children.Count; ++i)
                    if (!Children[i].Hidden)
                        return true;
                return false;
            }
        }

        // UIDs that appear more than once anywhere in the tree, hidden or not.
        // The deep-link map and the hooks key on UID, so a repeat would make the
        // later entry win silently.
        public static Array<string> DuplicateUids(Array<CodexEntry> roots)
        {
            var seen = new HashSet<string>();
            var dupes = new Array<string>();
            CollectDuplicates(roots, seen, dupes);
            return dupes;
        }

        static void CollectDuplicates(Array<CodexEntry> entries, HashSet<string> seen, Array<string> dupes)
        {
            if (entries == null)
                return;
            foreach (CodexEntry e in entries)
            {
                if (!string.IsNullOrEmpty(e.UID) && !seen.Add(e.UID) && !dupes.Contains(e.UID))
                    dupes.Add(e.UID);
                CollectDuplicates(e.Children, seen, dupes);
            }
        }

        // Derive GameText NameIds from UID by convention when they aren't set
        // explicitly. Caller must invoke after YamlParser.DeserializeArray since
        // the yaml deserializer doesn't fire [StarDataDeserialized] hooks.
        // Mapping: "blackbox_what_is" → TitleId "CodexBlackboxWhatIs",
        //                                ShortDescId "CodexBlackboxWhatIsShort",
        //                                TextId "CodexBlackboxWhatIsText".
        public void ResolveDefaults()
        {
            if (!string.IsNullOrEmpty(UID))
            {
                string baseId = "Codex" + PascalCase(UID);
                if (string.IsNullOrEmpty(TitleId))     TitleId     = baseId;
                if (string.IsNullOrEmpty(ShortDescId)) ShortDescId = baseId + "Short";
                if (string.IsNullOrEmpty(TextId))      TextId      = baseId + "Text";
            }
            if (Children != null)
            {
                foreach (CodexEntry child in Children)
                    child.ResolveDefaults();
            }
        }

        // "black_box_updates" → "BlackBoxUpdates". Treats '_' and other non-
        // alphanumerics as word separators; preserves digits.
        static string PascalCase(string slug)
        {
            var sb = new StringBuilder(slug.Length);
            bool startOfWord = true;
            foreach (char c in slug)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    if (startOfWord && c >= 'a' && c <= 'z')
                        sb.Append((char)(c - 32));
                    else
                        sb.Append(c);
                    startOfWord = false;
                }
                else
                {
                    startOfWord = true;
                }
            }
            return sb.ToString();
        }
    }
}
