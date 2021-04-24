using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ship_Game.Tools.Localization
{
    public partial class LocalizationDB
    {
        readonly string Namespace;
        readonly string Name;
        readonly LocUsageDB Usages;
        readonly Array<TextToken> ExistingIds = new Array<TextToken>();
        readonly Array<LocText> LocalizedText = new Array<LocText>();
        readonly Array<LocText> ModText = new Array<LocText>();
        readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly string[] WordSeparators = { " ", "\t", "\r", "\n", "\"",
                                                 "\\t","\\r","\\n", "\\\"" };

        public string Prefix;
        public string ModPrefix;
        public readonly bool PreferCsharpNameIds;

        public int NumModLocalizations => ModText.Count;
        public int NumLocalizations => LocalizedText.Count;

        public LocalizationDB(string enumNamespace, string enumName, int mode, string gameContent, string modContent)
        {
            Namespace = enumNamespace;
            Name = enumName;
            Prefix = "BB";
            ModPrefix = MakeModPrefix(modContent);
            Usages = new LocUsageDB(gameContent, modContent, Prefix, ModPrefix);
            PreferCsharpNameIds = mode == 2;
        }

        static string MakeModPrefix(string modDir)
        {
            if (modDir.IsEmpty())
                return "";
            string dir = Path.GetDirectoryName(modDir);
            if (modDir.Last() != '/' && modDir.Last() != '\\')
                dir = Path.GetFileName(modDir);
            string[] words = dir.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(word => char.ToUpper(word[0])));
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

        bool GetExistingId(int id, string nameId, out TextToken e)
        {
            for (int i = 0; i < ExistingIds.Count; ++i)
            {
                TextToken existing = ExistingIds[i];
                if (nameId != null && existing.NameId == nameId) { e = existing; return true; }
                if (id > 0 && existing.Id == id) { e = existing; return true; }
            }
            e = null;
            return false;
        }

        void ReadIdentifiersFromCsharp(string enumFile)
        {
            string fileName = Path.GetFileName(enumFile);
            Array<TextToken> tokens = TextToken.FromCSharp(enumFile);
            foreach (TextToken t in tokens)
            {
                if (GetExistingId(t.Id, t.NameId, out TextToken e))
                    Log.Write(ConsoleColor.Red, $"{Name} ID CONFLICT:"
                                               +$"\n  existing at {fileName}: {e.NameId} = {t.Id}"
                                               +$"\n  addition at {fileName}: {t.NameId} = {t.Id}");
                else
                    ExistingIds.Add(t);
            }
        }

        void ReadIdentifiersFromYaml(string yamlFile)
        {
            Array<TextToken> tokens = TextToken.FromYaml(yamlFile);
            foreach (TextToken t in tokens)
            {
                if (!GetExistingId(t.Id, t.NameId, out _))
                    ExistingIds.Add(t);
            }
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
            string name = nameIdPrefix + "_";
            if (GetExistingId(id, null, out TextToken existing) && existing.NameId.NotEmpty())
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

            if (name.IsEmpty())
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

        protected LocText AddNewLocalization(Array<LocText> localizations, 
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

            if (token.Id == 1987)
                Log.Write("ugh");

            if (token.Id > 0 && Usages.Contains(token.Id))
                token.NameId = nameIdPrefix + "_" + Usages.Get(token.Id).NameId;
            else if (PreferCsharpNameIds && token.Id > 0 && GetExistingId(token.Id, null, out TextToken existing) && existing.NameId.NotEmpty())
                token.NameId = existing.NameId;
            else if (token.NameId.IsEmpty())
                token.NameId = CreateNameId(nameIdPrefix, token.Id, words);

            if (token.NameId.NotEmpty())
            {
                EnumNames.Add(token.NameId);
                var loc = new LocText(token, comment);
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

        protected bool GetLocalization(Array<LocText> localizedText, int id, string nameId, out LocText loc)
        {
            loc = id > 0 ? localizedText.FirstOrDefault(x => x.Id == id) : null;

            if (loc == null && nameId.NotEmpty())
                loc = localizedText.FirstOrDefault(x => x.NameId == nameId);

            return loc != null;
        }

        public bool AddFromYaml(string yamlFile, bool logMerge = false)
        {
            Array<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddLocalizations(tokens, logMerge:logMerge);
            return true;
        }

        protected void AddLocalization(Array<LocText> localizations, TextToken token, string nameIdPrefix, bool logMerge)
        {
            if (string.IsNullOrEmpty(token.Text))
                return;

            if (GetLocalization(localizations, token.Id, token.NameId, out LocText loc))
            {
                if (logMerge)
                    Log.Write(ConsoleColor.Green, $"Merged {token.Lang} {token.Id} {token.NameId}: {token.Text}");
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
            if (GetLocalization(ModText, id, null, out LocText mod))
                return mod.NameId;
            return GetNameId(id);
        }

        public string GetNameId(int id)
        {
            if (GetLocalization(LocalizedText, id, null, out LocText loc))
                return loc.NameId;

            Log.Write(ConsoleColor.Red, $"{Name}: failed to find tooltip data with id={id}");
            return id.ToString();
        }

        public bool AddFromModYaml(string yamlFile, bool logMerge = false)
        {
            Array<TextToken> tokens = TextToken.FromYaml(yamlFile);
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
                if (GetLocalization(LocalizedText, token.Id, token.NameId, out LocText vanilla))
                    token.NameId = vanilla.NameId; // keep NameId from vanilla
                else
                    uniqueToMod.Add(token); // this is unique to the mod
                AddLocalization(ModText, token, ModPrefix, logMerge);
            }
        }

        public void FinalizeModLocalization()
        {
            // add in missing translations
            foreach (LocText mod in ModText)
            {
                if (GetLocalization(LocalizedText, mod.Id, mod.NameId, out LocText vanilla))
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
                int numRemoved = 0;
                ModText.RemoveAll(mod =>
                {
                    LocText dup = LocalizedText.FirstOrDefault(vanilla => vanilla.Equals(mod));
                    if (dup == null)
                        return false;
                    ++numRemoved;
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
