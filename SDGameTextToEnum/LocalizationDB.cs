using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SDGameTextToEnum
{
    public class LocalizationDB
    {
        public readonly string Namespace;
        public readonly string Name;
        protected readonly Dictionary<int, TextToken> ExistingIds = new Dictionary<int, TextToken>();
        protected readonly List<Localization> LocalizedText = new List<Localization>();
        protected readonly List<Localization> ToolTips = new List<Localization>();
        protected readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly char[] Space = { ' ' };

        public LocalizationDB(string enumNamespace, string enumName)
        {
            Namespace = enumNamespace;
            Name = enumName;
        }

        public LocalizationDB(LocalizationDB gen, string newName) // copy
        {
            Namespace = gen.Namespace;
            Name = newName;
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

        string GetCommentSafeWord(string word)
        {
            return word.Replace('\n', ' ');
        }

        string CreateNameId(int id, string[] words)
        {
            string name = "";
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

        protected Localization AddNewLocalization(List<Localization> localizations, TextToken token)
        {
            string[] words = token.Text.Split(Space, StringSplitOptions.RemoveEmptyEntries);
            const int maxCommentWords = 10;
            string comment = "";

            for (int i = 0; i < maxCommentWords && i < words.Length; ++i)
            {
                comment += GetCommentSafeWord(words[i]);
                if (i != (words.Length - 1) && i != (maxCommentWords - 1))
                    comment += " ";
            }

            // only generate a new name if not specified
            if (string.IsNullOrEmpty(token.NameId))
                token.NameId = CreateNameId(token.Id, words);

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

        public int NumLocalizations => LocalizedText.Count;
        public int NumToolTips => ToolTips.Count;

        public bool AddFromYaml(string yamlFile, bool logMerge = false)
        {
            List<TextToken> tokens = TextToken.FromYaml(yamlFile);
            if (tokens.Count == 0)
                return false;
            AddLocalizations(tokens, logMerge);
            return true;
        }

        protected void AddLocalization(List<Localization> localizations, TextToken token, bool logMerge)
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
                AddNewLocalization(localizations, token);
            }
        }

        public void AddLocalizations(IEnumerable<TextToken> localizations, bool logMerge = false)
        {
            foreach (TextToken token in localizations)
            {
                AddLocalization(LocalizedText, token, logMerge);
            }
        }
        
        public virtual string GetNameId(int id)
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
                    Localization loc = AddNewLocalization(ToolTips, t);
                    if (loc != null)
                    {
                        loc.TipId = GetNameId(t.ToolTipData);
                    }
                }
            }
        }

        protected static void WriteToFile(StringWriter sw, string outPath)
        {
            File.WriteAllText(outPath, sw.ToString(), Encoding.UTF8);
            Log.Write(ConsoleColor.Green, $"Wrote {outPath}");
        }

        /// <summary>
        /// Uses all the collected Localization data to output a new C# Enum file
        /// </summary>
        /// <param name="outPath"></param>
        public void ExportCsharp(string outPath)
        {
            var sw = new StringWriter();
            sw.WriteLine( "// ReSharper disable UnusedMember.Global");
            sw.WriteLine( "// ReSharper disable IdentifierTypo");
            sw.WriteLine( "// ReSharper disable CommentTypo");
            sw.WriteLine($"namespace {Namespace}");
            sw.WriteLine( "{");
            sw.WriteLine( "    /// <summary>");
            sw.WriteLine( "    /// This file was auto-generated by SDGameTextToEnum.exe");
            sw.WriteLine( "    /// </summary>");
            sw.WriteLine($"    public enum {Name}");
            sw.WriteLine( "    {");
            List<Localization> locs = ToolTips.Count > 0 ? ToolTips : LocalizedText;
            foreach (Localization loc in locs)
            {
                sw.WriteLine($"        /// <summary>{loc.Comment}</summary>");
                sw.WriteLine($"        {loc.NameId} = {loc.Id},");
            }
            sw.WriteLine("    }");
            sw.WriteLine("}");
            WriteToFile(sw, outPath);
        }

        /// <summary>
        /// Uses all the generated Enum data to output a new YAML format file
        /// </summary>
        /// <param name="outPath"></param>
        public void ExportYaml(string outPath)
        {
            var sw = new StringWriter();
            sw.WriteLine( "# Version 1");
            sw.WriteLine($"# This file was auto-generated by SDGameTextToEnum.exe");
            WriteYamlLoc(sw, LocalizedText);
            WriteToFile(sw, outPath);
        }

        public void ExportMissingTranslationsYaml(string lang, string outPath)
        {
            var missing = GetMissingLocalizations(lang, LocalizedText);
            if (missing.Count == 0)
            {
                File.Delete(outPath);
                return;
            }

            var sw = new StringWriter();
            sw.WriteLine( "# Version 1");
            sw.WriteLine($"# This file was auto-generated by SDGameTextToEnum.exe");
            WriteMissingYamlLoc(sw, lang, missing);
            WriteToFile(sw, outPath);
        }
        
        protected void WriteYamlLoc(StringWriter sw, List<Localization> localizations)
        {
            foreach (Localization loc in localizations)
            {
                sw.WriteLine($"{loc.NameId}:");
                sw.WriteLine($" Id: {loc.Id}");
                foreach (Translation lt in loc.Translations)
                    sw.WriteLine($" {lt.Lang}: {lt.YamlString}");
            }
        }

        protected List<Localization> GetMissingLocalizations(string lang, List<Localization> localizations)
        {
            var missing = new List<Localization>();
            foreach (Localization loc in localizations)
                if (!loc.TryGetText(lang, out Translation t) || string.IsNullOrEmpty(t.Text))
                    missing.Add(loc);
            return missing;
        }
        
        protected void WriteMissingYamlLoc(StringWriter sw, string lang, List<Localization> missing)
        {
            foreach (Localization m in missing)
            {
                Translation eng = m.GetText("ENG");
                sw.WriteLine($"{m.NameId}:");
                sw.WriteLine($" Id: {m.Id}");
                sw.WriteLine($" {lang}: \"\" # ENG: {eng.YamlString}");
            }
        }

        public void ExportTipsYaml(string outPath)
        {
            var sw = new StringWriter();
            sw.WriteLine( "# Version 1");
            sw.WriteLine($"# This file was auto-generated by SDGameTextToEnum.exe and is in sync with {Name}.cs");
            foreach (Localization loc in ToolTips)
            {
                sw.WriteLine($"{loc.NameId}:");
                sw.WriteLine($" Id: {loc.Id}");
                sw.WriteLine($" TextId: {loc.TipId}");
            }
            WriteToFile(sw, outPath);
        }
    }
}
