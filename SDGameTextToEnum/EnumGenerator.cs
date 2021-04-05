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
        protected string Name;
        protected readonly Dictionary<int, LangToken> ExistingIds = new Dictionary<int, LangToken>();
        protected readonly List<Localization> LocalizedText = new List<Localization>();
        protected readonly HashSet<string> EnumNames = new HashSet<string>();
        readonly char[] Space = { ' ' };

        public EnumGenerator(string enumNamespace, string enumName)
        {
            Namespace = enumNamespace;
            Name = enumName;
        }

        public EnumGenerator(EnumGenerator gen) // copy
        {
            Namespace = gen.Namespace;
            Name = gen.Name;
            ExistingIds = new Dictionary<int, LangToken>(gen.ExistingIds);
            EnumNames = new HashSet<string>(gen.EnumNames);
            foreach (Localization loc in gen.LocalizedText)
                LocalizedText.Add(new Localization(loc));
        }

        // Load existing identifiers
        // First from the Csharp enum file, then supplement with entries from yaml file
        public void LoadIdentifiers(string enumFile, string yamlFile)
        {
            if (File.Exists(enumFile))
                ReadIdentifiersFromCsharp(enumFile);

            if (File.Exists(yamlFile))
                ReadIdentifiersFromYaml(yamlFile);
        }

        void ReadIdentifiersFromCsharp(string enumFile)
        {
            string fileName = Path.GetFileName(enumFile);
            List<LangToken> tokens = LangToken.FromCSharp(enumFile);
            foreach (LangToken t in tokens)
            {
                if (ExistingIds.TryGetValue(t.Id, out LangToken e))
                    Log.Write(ConsoleColor.Red, $"{Name} ID CONFLICT:"
                                               +$"\n  existing at {fileName} line {e.Line}: {e.NameId} = {t.Id}"
                                               +$"\n  addition at {fileName} line {t.Line}: {t.NameId} = {t.Id}");
                else
                    ExistingIds.Add(t.Id, t);
            }
        }

        void ReadIdentifiersFromYaml(string yamlFile)
        {
            List<LangToken> tokens = LangToken.FromYaml(yamlFile);
            foreach (LangToken token in tokens)
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
            if (ExistingIds.TryGetValue(id, out LangToken existing) &&
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

        protected void AddLocalization(string lang, int id, string nameId, string text)
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
                LocalizedText.Add(new Localization(lang, id, nameId, comment, text));
            }
            else
            {
                Log.Write(ConsoleColor.Yellow, $"{Name}: Skipping empty enum entry {lang} {nameId} {id}: '{text}'");
            }
        }

        protected bool GetLocalization(List<Localization> localizedText, int id, out Localization loc)
        {
            loc = localizedText.FirstOrDefault(x => x.Id == id);
            return loc != null;
        }

        public void AddLocalizations(IEnumerable<LangToken> localizations)
        {
            foreach ((string lang, int id, string text) in localizations)
            {
                if (GetLocalization(LocalizedText, id, out Localization loc))
                    loc.AddText(lang, id, text);
                else
                    AddLocalization(lang, id, "", text);
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
            foreach (Localization loc in LocalizedText)
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
    }
}
