using System;
using System.Collections.Generic;
using System.Linq;

namespace SDGameTextToEnum
{
    public class LangText
    {
        public string Lang;
        public string Text;
        public LangText(string lang, string text)
        {
            Lang = lang;
            Text = text;
        }

        // properly escaped yaml safe string
        public string YamlString
        {
            get
            {
                string escaped = Text;
                escaped = escaped.Replace("\r\n", "\\n");
                escaped = escaped.Replace("\n", "\\n");
                escaped = escaped.Replace("\t", "\\t");
                escaped = escaped.Replace("\"", "\\\"");
                return "\"" + escaped + "\"";
            }
        }
    }

    public class Localization
    {
        public static string[] SupportedLangs = new[]{ "ENG", "RUS", "SPA" };

        public readonly int Id;
        public readonly string NameId;
        public readonly string Comment;
        public readonly List<LangText> LangTexts;
        public string TipId;
        public Localization(string lang, int id, string nameId, string comment, string text)
        {
            Id = id;
            NameId = nameId;
            Comment = comment;
            LangTexts = new List<LangText>{ new LangText(lang, text) };
        }
        public Localization(Localization copy)
        {
            Id = copy.Id;
            NameId = copy.NameId;
            Comment = copy.Comment;
            LangTexts = new List<LangText>(copy.LangTexts.Select(x => new LangText(x.Lang, x.Text)));
        }
        public bool TryGetText(string lang, out LangText text)
        {
            text = LangTexts.FirstOrDefault(x => x.Lang == lang);
            return text != null;
        }
        public LangText GetText(string lang)
        {
            LangText text = LangTexts.FirstOrDefault(x => x.Lang == lang);
            if (text == null)
                throw new Exception($"{NameId}({Id}) failed to get lang={lang}");
            return text;
        }
        public void AddText(string lang, int id, string text)
        {
            if (!SupportedLangs.Contains(lang))
            {
                Log.Write(ConsoleColor.Yellow, $"unsupported langugage: {lang}");
            }
            else if (TryGetText(lang, out LangText lt))
            {
                Log.Write(ConsoleColor.Yellow,  "id already exists:\n" +
                                                $"  existing {lang}: {id}={lt.Text}\n" +
                                                $"  addition {lang}: {id}={text}");
            }
            else
            {
                LangTexts.Add(new LangText(lang, text));
            }
        }
    }
}