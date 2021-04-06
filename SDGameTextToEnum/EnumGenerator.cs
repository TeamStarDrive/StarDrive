using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SDGameTextToEnum
{
    public class EnumGenerator
    {
        public readonly string Namespace;
        public readonly string Name;
        protected readonly Dictionary<int, TextToken> ExistingIds = new Dictionary<int, TextToken>();
        protected readonly List<Localization> LocalizedText = new List<Localization>();
        protected readonly List<Localization> ToolTips = new List<Localization>();
        protected readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly char[] Space = { ' ' };

        public EnumGenerator(string enumNamespace, string enumName)
        {
            Namespace = enumNamespace;
            Name = enumName;
        }

        public EnumGenerator(EnumGenerator gen, string newName) // copy
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

        string GetIdentifier(int id, string[] words)
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

        protected Localization AddLocalization(List<Localization> localizations, string lang, int id, string nameId, string text)
        {
            string[] words = text.Split(Space, StringSplitOptions.RemoveEmptyEntries);
            const int maxCommentWords = 10;
            string comment = "";

            for (int i = 0; i < maxCommentWords && i < words.Length; ++i)
            {
                comment += GetCommentSafeWord(words[i]);
                if (i != (words.Length - 1) && i != (maxCommentWords - 1))
                    comment += " ";
            }

            // only generate a new name if not specified
            if (string.IsNullOrEmpty(nameId))
                nameId = GetIdentifier(id, words);

            if (!string.IsNullOrEmpty(nameId))
            {
                EnumNames.Add(nameId);
                var loc = new Localization(lang, id, nameId, comment, text);
                localizations.Add(loc);
                return loc;
            }
            else
            {
                Log.Write(ConsoleColor.Yellow, $"{Name}: Skipping empty enum entry {lang} {nameId} {id}: '{text}'");
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

        public void AddLocalizations(IEnumerable<TextToken> localizations)
        {
            foreach ((string lang, int id, string text) in localizations)
            {
                if (GetLocalization(LocalizedText, id, out Localization loc))
                    loc.AddText(lang, id, text);
                else
                    AddLocalization(LocalizedText, lang, id, "", text);
            }
        }
        
        string GetNameId(int id)
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
                    Localization loc = AddLocalization(ToolTips, "ANY", t.Id, "", t.Text);
                    if (loc != null)
                    {
                        loc.TipId = GetNameId(t.ToolTipData);
                    }
                }
            }
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
            File.WriteAllText(outPath, sw.ToString(), Encoding.UTF8);
            Log.Write(ConsoleColor.Green, $"Wrote {outPath}");
        }

        /// <summary>
        /// Uses all the generated Enum data to output a new YAML format file
        /// </summary>
        /// <param name="outPath"></param>
        public void ExportYaml(string outPath)
        {
            var sw = new StringWriter();
            sw.WriteLine( "# Version 1");
            sw.WriteLine($"# This file was auto-generated by SDGameTextToEnum.exe and is in sync with {Name}.cs");
            WriteYamlLoc(sw, LocalizedText);
            File.WriteAllText(outPath, sw.ToString(), Encoding.UTF8);
            Log.Write(ConsoleColor.Green, $"Wrote {outPath}");
        }

        protected string GetEscapedYamlString(string text)
        {
            string escaped = text;
            escaped = escaped.Replace("\r\n", "\\n");
            escaped = escaped.Replace("\n", "\\n");
            escaped = escaped.Replace("\t", "\\t");
            escaped = escaped.Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        protected void WriteYamlLoc(StringWriter sw, List<Localization> localizations)
        {
            foreach (Localization loc in localizations)
            {
                sw.WriteLine($"{loc.NameId}:");
                sw.WriteLine($" Id: {loc.Id}");
                foreach (LangText lt in loc.LangTexts)
                {
                    string text = GetEscapedYamlString(lt.Text);
                    sw.WriteLine($" {lt.Lang}: {text}");
                }
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
            File.WriteAllText(outPath, sw.ToString(), Encoding.UTF8);
            Log.Write(ConsoleColor.Green, $"Wrote {outPath}");
        }
    }
}
