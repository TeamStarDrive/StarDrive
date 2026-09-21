using System.Text;
using SDUtils;
using Ship_Game.Data.Serialization;

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
