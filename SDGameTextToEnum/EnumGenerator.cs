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
        protected readonly Dictionary<int, EnumIdentifier> ExistingIdentifiers = new Dictionary<int, EnumIdentifier>();
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
            ExistingIdentifiers = new Dictionary<int, EnumIdentifier>(gen.ExistingIdentifiers);
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
            string[] lines = File.ReadAllLines(enumFile);
            var splitter = new [] { '=', ',', ' ', '\t' };
            int lineNumber = 0;
            foreach (string line in lines)
            {
                ++lineNumber;
                if (!line.Contains("=")) continue;
                string[] parts = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                string identifier = parts[0];
                int id = int.Parse(parts[1]);
                if (ExistingIdentifiers.TryGetValue(id, out EnumIdentifier existing))
                    Log.Write(ConsoleColor.Red, $"{Name} ID CONFLICT:\n"
                                               +$"  existing at {fileName} line {existing.Line}: {existing.Identifier} = {id}\n"
                                               +$"  addition at {fileName} line {lineNumber}: {identifier} = {id}");
                else
                    ExistingIdentifiers.Add(id, new EnumIdentifier{Line = lineNumber, Identifier=identifier});
            }
        }

        void ReadIdentifiersFromYaml(string yamlFile)
        {
            string[] lines = File.ReadAllLines(yamlFile);
            var splitter = new [] { ':', ' ', '\t' };
            int lineNumber = 0;
            foreach (string line in lines)
            {
                ++lineNumber;
                if (!line.Contains(":") || line[0] == ' ') continue;
                string[] parts = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                string identifier = parts[0];
                int id = int.Parse(parts[1]);
                if (!ExistingIdentifiers.ContainsKey(id))
                    ExistingIdentifiers.Add(id, new EnumIdentifier{Line = lineNumber, Identifier=identifier});
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

        string GetCommentSafeWord(string word)
        {
            return word.Replace('\n', ' ');
        }

        string GetIdentifier(int id, string[] words)
        {
            string name = "";
            if (ExistingIdentifiers.TryGetValue(id, out EnumIdentifier existing) &&
                !string.IsNullOrWhiteSpace(existing.Identifier))
            {
                name = existing.Identifier;
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

        public void AddLocalizations(string lang, IEnumerable<Token> localizations)
        {
            foreach ((int id, string text) in localizations)
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
            foreach (Localization loc in LocalizedText)
            {
                sw.WriteLine($"{loc.NameId}: {loc.Id}");
                foreach (LangText lt in loc.LangTexts)
                {
                    sw.WriteLine($"  {lt.Lang}: {lt.Text}");
                }
            }
            File.WriteAllText(outPath, sw.ToString(), Encoding.UTF8);
            Log.Write(ConsoleColor.Green, $"Wrote {outPath}");
        }
    }
}
