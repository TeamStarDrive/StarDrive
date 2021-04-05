using System;
using System.Collections.Generic;
using System.IO;

namespace SDGameTextToEnum
{
    public class LangToken
    {
        public string Lang;
        public int Id;
        public string NameId;
        public string Text;
        public int Line;
        public LangToken(string lang, int id, string nameId, string text, int lineNo = -1)
        {
            Lang = lang;
            Id = id;
            NameId = nameId;
            Text = text;
            Line = lineNo;
        }
        public void Deconstruct(out string lang, out int id, out string text)
        {
            lang = Lang;
            id = Id;
            text = Text;
        }

        public static List<LangToken> FromYaml(string yamlFile)
        {
            Log.Write(ConsoleColor.Cyan, $"FromYaml: {yamlFile}");
            string fileName = Path.GetFileName(yamlFile);
            int id = 0;
            int lineNo = 0;
            string nameId = "";
            var splitter = new []{ ':' };
            var tokens = new List<LangToken>();
            string[] lines = File.ReadAllLines(yamlFile);

            foreach (string line in lines)
            {
                ++lineNo;
                if (line.Length < 3 || line.StartsWith("#"))
                    continue;
                
                string[] parts = line.Split(splitter, 2);
                if (parts.Length <= 1)
                {
                    Log.Write(ConsoleColor.Red, $"{fileName}:{lineNo} syntax error: {line}");
                }
                else if (char.IsLetter(line[0]))
                {
                    nameId = parts[0].Trim();
                }
                else
                {
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();
                    if (key == "Id")
                    {
                        id = int.Parse(val);
                    }
                    else if (key == "ENG" || key == "RUS" || key == "SPA")
                    {
                        string lang = key;
                        string text = val;
                        tokens.Add(new LangToken(lang, id, nameId, text, lineNo));
                    }
                    else
                    {
                        Log.Write(ConsoleColor.Red, $"{fileName}:{lineNo} unexpected token: {line}");
                    }
                }
            }
            return tokens;
        }

        public static List<LangToken> FromCSharp(string enumFile)
        {
            Log.Write(ConsoleColor.Cyan, $"FromCSharp: {enumFile}");
            var tokens = new List<LangToken>();
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
                tokens.Add(new LangToken("ENG", id, nameId, "", lineNo));
            }
            return tokens;
        }
    }
}