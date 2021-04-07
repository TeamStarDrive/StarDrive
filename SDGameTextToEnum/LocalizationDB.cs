using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SDGameTextToEnum
{
    public partial class LocalizationDB
    {
        readonly string Namespace;
        readonly string Name;
        readonly LocalizationUsages Usages;
        readonly Dictionary<int, TextToken> ExistingIds = new Dictionary<int, TextToken>();
        readonly List<Localization> LocalizedText = new List<Localization>();
        readonly List<Localization> ModText = new List<Localization>();
        readonly List<Localization> ToolTips = new List<Localization>();
        readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly string[] WordSeparators = { " ", "\t", "\r", "\n", "\"",
                                                 "\\t","\\r","\\n", "\\\"" };

        public string Prefix;
        public string ModPrefix;

        public int NumModLocalizations => ModText.Count;
        public int NumLocalizations => LocalizedText.Count;
        public int NumToolTips => ToolTips.Count;

        public LocalizationDB(string enumNamespace, string enumName, LocalizationUsages usages)
        {
            Namespace = enumNamespace;
            Name = enumName;
            Usages = usages;
        }

        public LocalizationDB(LocalizationDB gen, string newName) // copy
        {
            Namespace = gen.Namespace;
            Name = newName;
            Usages = gen.Usages;
            ExistingIds = new Dictionary<int, TextToken>(gen.ExistingIds);
            EnumNames = new HashSet<string>(gen.EnumNames);
            ToolTips = new List<Localization>(gen.ToolTips);
            foreach (Localization loc in gen.LocalizedText)
                LocalizedText.Add(new Localization(loc));
        }

        // Load existing identifiers
        // First from the Csharp enum file, then supplement with entries from yaml file
        public void LoadIdentifiers(string enumFile, string yamlFile)
        {
            ExistingIds.Clear();
            EnumNames.Clear();
            if (File.Exists(enumFile))
                ReadIdentifiersFromCsharp(enumFile);

            if (File.Exists(yamlFile))
                ReadIdentifiersFromYaml(yamlFile);
        }

        void ReadIdentifiersFromCsharp(string enumFile)
        {
            string fileName = Path.GetFileName(enumFile);
            List<TextToken> tokens = TextToken.FromCSharp(enumFile);
            foreach (TextToken t in tokens)
            {
                if (ExistingIds.TryGetValue(t.Id, out TextToken e))
                    Log.Write(ConsoleColor.Red, $"{Name} ID CONFLICT:"
                                               +$"\n  existing at {fileName}: {e.NameId} = {t.Id}"
                                               +$"\n  addition at {fileName}: {t.NameId} = {t.Id}");
                else
                    ExistingIds.Add(t.Id, t);
            }
        }

        void ReadIdentifiersFromYaml(string yamlFile)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            foreach (TextToken token in tokens)
                if (!ExistingIds.ContainsKey(token.Id))
                    ExistingIds.Add(token.Id, token);
        }

        string GetCapitalizedIdentifier(string word)
        {
            var sb = new StringBuilder();
            foreach (char c in word)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(sb.Length == 0 ? char.ToUpper(c) : char.ToLower(c));
                }
            }
            return sb.ToString();
        }

        string CreateNameId(string nameIdPrefix, int id, string[] words)
        {
            string name = nameIdPrefix + "_" ?? "";
            if (ExistingIds.TryGetValue(id, out TextToken existing) &&
                !string.IsNullOrWhiteSpace(existing.NameId))
            {
                name = existing.NameId;
            }
            else
            {
                int maxWords = 5;
                for (int i = 0; i < maxWords && i < words.Length; ++i)
                {
                    string identifier = GetCapitalizedIdentifier(words[i]);
                    if (identifier == "") // it was some invalid token like " + "
                    {
                        ++maxWords; // discount this word
                        continue;
                    }
                    name += identifier;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
                return "";

            if (EnumNames.Contains(name))
            {
                for (int suffix = 2; suffix < 100; ++suffix)
                {
                    if (!EnumNames.Contains(name + suffix))
                    {
                        name = name + suffix;
                        break;
                    }
                }
            }
            return name;
        }

        protected Localization AddNewLocalization(List<Localization> localizations, 
                                                  TextToken token, string nameIdPrefix)
        {
            string[] words = token.Text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
            const int maxCommentWords = 10;
            string comment = "";

            for (int i = 0; i < maxCommentWords && i < words.Length; ++i)
            {
                comment += words[i];
                if (i != (words.Length - 1) && i != (maxCommentWords - 1))
                    comment += " ";
            }

            if (Usages.Contains(token.Id))
                token.NameId = nameIdPrefix + "_" + Usages.Get(token.Id).NameId;
            else if (string.IsNullOrEmpty(token.NameId))
                token.NameId = CreateNameId(nameIdPrefix, token.Id, words);

            if (!string.IsNullOrEmpty(token.NameId))
            {
                EnumNames.Add(token.NameId);
                var loc = new Localization(token, comment);
                localizations.Add(loc);
                return loc;
            }
            else
            {
                Log.Write(ConsoleColor.Yellow, 
                    $"{Name}: skipping empty enum entry {token.Lang} {token.NameId} {token.Id}: '{token.Text}'");
                return null;
            }
        }

        protected bool GetLocalization(List<Localization> localizedText, int id, out Localization loc)
        {
            loc = localizedText.FirstOrDefault(x => x.Id == id);
            return loc != null;
        }

        public bool AddFromYaml(string yamlFile, bool logMerge = false)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddLocalizations(tokens, logMerge:logMerge);
            return true;
        }

        protected void AddLocalization(List<Localization> localizations, TextToken token, string nameIdPrefix, bool logMerge)
        {
            if (string.IsNullOrEmpty(token.Text))
                return;

            if (GetLocalization(localizations, token.Id, out Localization loc))
            {
                if (logMerge)
                    Log.Write(ConsoleColor.Green, $"Merged {token.Lang} {token.Id}: {token.Text}");
                loc.AddTranslation(new Translation(token.Id, token.Lang, token.Text));
            }
            else
            {
                AddNewLocalization(localizations, token, nameIdPrefix);
            }
        }

        public void AddLocalizations(IEnumerable<TextToken> localizations, bool logMerge = false)
        {
            foreach (TextToken token in localizations)
            {
                AddLocalization(LocalizedText, token, Prefix, logMerge);
            }
        }
        
        public string GetModNameId(int id)
        {
            if (GetLocalization(ModText, id, out Localization mod))
                return mod.NameId;
            return GetNameId(id);
        }

        public string GetNameId(int id)
        {
            if (GetLocalization(LocalizedText, id, out Localization loc))
                return loc.NameId;

            Log.Write(ConsoleColor.Red, $"{Name}: failed to find tooltip data with id={id}");
            return id.ToString();
        }

        public void AddToolTips(IEnumerable<TextToken> toolTips)
        {
            foreach (TextToken t in toolTips)
            {
                if (GetLocalization(ToolTips, t.Id, out Localization _))
                    Log.Write(ConsoleColor.Red, $"{Name}: duplicate tooltip with id={t.Id}");
                else
                {
                    Localization loc = AddNewLocalization(ToolTips, t, null);
                    if (loc != null)
                    {
                        loc.TipId = GetNameId(t.ToolTipData);
                        loc.Comment = $"{loc.TipId}: {loc.Comment}";
                    }
                }
            }
        }
        
        public bool AddFromModYaml(string yamlFile, bool logMerge = false)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddModLocalizations(tokens, logMerge);
            return true;
        }

        public void AddModLocalizations(IEnumerable<TextToken> localizations, bool logMerge = false)
        {
            // build ModTexts
            var uniqueToMod = new List<TextToken>();
            foreach (TextToken token in localizations)
            {
                if (GetLocalization(LocalizedText, token.Id, out Localization vanilla))
                    token.NameId = vanilla.NameId; // keep NameId from vanilla
                else
                    uniqueToMod.Add(token); // this is unique to the mod
                AddLocalization(ModText, token, ModPrefix, logMerge);
            }
        }

        public void FinalizeModLocalization()
        {
            // add in missing translations
            foreach (Localization mod in ModText)
            {
                if (GetLocalization(LocalizedText, mod.Id, out Localization vanilla))
                {
                    foreach (Translation tr in vanilla.Translations)
                        if (!mod.HasLang(tr.Lang))
                            mod.AddTranslation(tr);
                }
            }

            // NOTE: not really worth it actually
            bool shouldRemoveDuplicates = false;
            if (shouldRemoveDuplicates)
            {
                // then remove ModTexts which are complete duplicates from vanilla
                int numRemoved = ModText.RemoveAll(mod =>
                {
                    Localization dup = LocalizedText.FirstOrDefault(vanilla => vanilla.Equals(mod));
                    if (dup == null)
                        return false;
                    Log.Write(ConsoleColor.Gray, $"{Name}: remove duplicate {mod.Id} {mod.NameId}"
                                                +$"\n  mod: {mod.Translations[0].Text}"
                                                +$"\n  dup: {dup.Translations[0].Text}");
                    return true;
                });
                if (numRemoved > 0)
                    Log.Write(ConsoleColor.Gray, $"{Name}: removed {numRemoved} text entries that already matched vanilla text");
            }
        }
    }
}
