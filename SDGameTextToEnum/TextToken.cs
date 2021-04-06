using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game;
using Ship_Game.Data.Yaml;

namespace SDGameTextToEnum
{
    public class TextToken
    {
        public string Lang;
        public int Id;
        public string NameId;
        public string Text;
        public int ToolTipData; // external ref for tooltips
        public TextToken(string lang, int id, string nameId, string text)
        {
            Lang = lang;
            Id = id;
            NameId = nameId;
            Text = text;
        }
        public void Deconstruct(out string lang, out int id, out string text)
        {
            lang = Lang;
            id = Id;
            text = Text;
        }

        public static List<TextToken> FromYaml(string yamlFile)
        {
            Log.Write(ConsoleColor.Cyan, $"FromYaml: {yamlFile}");
            var tokens = new List<TextToken>();
            using var parser = new YamlParser(yamlFile);
            foreach (KeyValuePair<object, LangToken> kv in parser.DeserializeMap<LangToken>())
            {
                string nameId = (string)kv.Key;
                LangToken t = kv.Value;
                if (t.ENG != null) tokens.Add(new TextToken("ENG", t.Id, nameId, t.ENG));
                if (t.RUS != null) tokens.Add(new TextToken("RUS", t.Id, nameId, t.RUS));
                if (t.SPA != null) tokens.Add(new TextToken("SPA", t.Id, nameId, t.SPA));
            }
            return tokens;
        }

        public static List<TextToken> FromCSharp(string enumFile)
        {
            Log.Write(ConsoleColor.Cyan, $"FromCSharp: {enumFile}");
            var tokens = new List<TextToken>();
            string[] lines = File.ReadAllLines(enumFile);
            var splitter = new [] { '=', ',', ' ', '\t' };
            int lineNo = 0;
            foreach (string line in lines)
            {
                ++lineNo;
                if (!line.Contains("=")) continue;
                string[] parts = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;
                string nameId = parts[0];
                int id = int.Parse(parts[1]);
                tokens.Add(new TextToken("ENG", id, nameId, ""));
            }
            return tokens;
        }
    }
}