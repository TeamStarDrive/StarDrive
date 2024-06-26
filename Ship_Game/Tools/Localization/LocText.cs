﻿using System;
using System.Collections.Generic;
using System.Linq;
using SDUtils;

namespace Ship_Game.Tools.Localization
{
    public class LocText
    {
        public static string[] SupportedLangs = new[]{ "ENG", "RUS", "SPA" };
        public int Id;
        public string NameId;
        public string Comment;
        public List<Translation> Translations;
        public string TipId;

        public override string ToString() => $"{NameId}({Id}) {Translations[0]}";

        public LocText(TextToken token, string comment)
        {
            Id = token.Id;
            NameId = token.NameId;
            Comment = comment;
            Translations = new List<Translation>{new Translation(token.Id, token.Lang, token.Text)};
        }

        public LocText(LocText copy)
        {
            Id = copy.Id;
            NameId = copy.NameId;
            Comment = copy.Comment;
            Translations = new List<Translation>(copy.Translations.Select(x => new Translation(x)));
        }

        public bool TryGetText(string lang, out Translation text)
        {
            text = Translations.FirstOrDefault(x => x.Lang == lang);
            return text != null;
        }

        public Translation GetText(string lang)
        {
            Translation text = Translations.FirstOrDefault(x => x.Lang == lang);
            if (text == null)
                throw new Exception($"{NameId}({Id}) failed to get lang={lang}");
            return text;
        }

        public void AddTranslation(Translation tr)
        {
            if (!SupportedLangs.Contains(tr.Lang))
            {
                Log.Write(ConsoleColor.Yellow, $"unsupported langugage: {tr.Lang}");
            }
            else if (TryGetText(tr.Lang, out Translation ex))
            {
                Log.Write(ConsoleColor.Yellow, "id already exists:\n" +
                                              $"  existing {ex.Lang}: {ex.Id}={ex.Text}\n" +
                                              $"  addition {tr.Lang}: {tr.Id}={tr.Text}");
            }
            else
            {
                Translations.Add(tr);
            }
        }

        public bool Equals(LocText other)
        {
            if (Id != other.Id || Translations.Count != other.Translations.Count)
                return false;

            foreach (Translation ourTr in Translations)
            {
                if (other.TryGetText(ourTr.Lang, out Translation otherTr))
                {
                    if (ourTr.Text != otherTr.Text)
                        return false;
                }
                else
                {
                    return false; // other Localization does not have this translation
                }
            }
            return true;
        }

        public bool HasLang(string lang)
        {
            foreach (Translation tr in Translations)
                if (tr.Lang == lang)
                    return true;
            return false;
        }
    }
}