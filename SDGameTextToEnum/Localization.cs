using System;
using System.Collections.Generic;
using System.Linq;

namespace SDGameTextToEnum
{
    public struct Token
    {
        public int Index;
        public string Text;
        public Token(int index, string text)
        {
            Index = index;
            Text = text;
        }
        public void Deconstruct(out int id, out string text)
        {
            id = Index;
            text = Text;
        }
    }

    public class LangText
    {
        public string Lang;
        public string Text;
        public LangText(string lang, string text)
        {
            Lang = lang;
            Text = text;
        }
    }

    public struct EnumIdentifier
    {
        public int Line;
        public string Identifier;
    }

    public class Localization
    {
        public readonly int Id;
        public readonly string NameId;
        public readonly string Comment;
        public readonly List<LangText> LangTexts;
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
        public void AddText(string lang, int id, string text)
        {
            if (TryGetText(lang, out LangText lt))
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