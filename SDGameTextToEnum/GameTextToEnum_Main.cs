using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SDGameTextToEnum
{
    public struct Token
    {
        public int Index;
        public string Text;
    }
    public sealed class LocalizationFile
    {
        public List<Token> TokenList;
        public IEnumerable<TextToken> GetTokens(string lang) => TokenList.Select(t => new TextToken(lang, t.Index, null, t.Text));
    }
    public sealed class ToolTip
    {
        public int TIP_ID;   // Serialized from: Tooltips.xml
        public int Data;     // Serialized from: Tooltips.xml
        public string Title; // Serialized from: Tooltips.xml
    }
    public sealed class Tooltips
    {
        public List<ToolTip> ToolTipsList;
        public IEnumerable<TextToken> GetTokens(string lang)
            => ToolTipsList.Select(t 
                => new TextToken(lang, t.TIP_ID, null, t.Title){ ToolTipData = t.Data });
    }
    
    /// <summary>
    /// Converts StarDrive GameText into C# enums
    /// </summary>
    public static class GameTextToEnum_Main
    {
        static bool UseYAMLFileAsSource = true;

        static T Deserialize<T>(string path)
        {
            var ser = new XmlSerializer(typeof(T));
            return (T)ser.Deserialize(File.OpenRead(path));
        }

        static IEnumerable<TextToken> GetGameText(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetGameText: {lang} {path}");
            return Deserialize<LocalizationFile>(path).GetTokens(lang);
        }

        static IEnumerable<TextToken> GetToolTips(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetToolTips: {lang} {path}");
            return Deserialize<Tooltips>(path).GetTokens(lang);
        }

        static string MakeModPrefix(string modDir)
        {
            string dir = Path.GetDirectoryName(modDir);
            if (modDir.Last() != '/' && modDir.Last() != '\\')
                dir = Path.GetFileName(modDir);
            string[] words = dir.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("", words.Select(word => char.ToUpper(word[0])));
        }

        static LocalizationDB CreateGameTextEnum(string bbDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{bbDir}/GameText.yaml";
            var db = new LocalizationDB("Ship_Game", "GameText");
            db.LoadIdentifiers(enumFile, yamlFile);
            if (UseYAMLFileAsSource)
            {
                if (db.AddFromYaml(yamlFile, "BB"))
                {
                    db.AddFromYaml($"{bbDir}/GameText.Missing.RUS.yaml", "BB", logMerge:true);
                    db.AddFromYaml($"{bbDir}/GameText.Missing.SPA.yaml", "BB", logMerge:true);
                }
            }
            if (db.NumLocalizations == 0)
            {
                db.AddLocalizations(GetGameText("ENG", $"{bbDir}/Localization/English/GameText_EN.xml"), "BB");
                db.AddLocalizations(GetGameText("RUS", $"{bbDir}/Localization/Russian/GameText_RU.xml"), "BB");
                db.AddLocalizations(GetGameText("SPA", $"{bbDir}/Localization/Spanish/GameText.xml"), "BB");
            }
            db.ExportCsharp(enumFile);
            db.ExportYaml(yamlFile);
            db.ExportMissingTranslationsYaml("RUS", $"{bbDir}/GameText.Missing.RUS.yaml");
            db.ExportMissingTranslationsYaml("SPA", $"{bbDir}/GameText.Missing.SPA.yaml");

            if (Directory.Exists(modDir))
            {
                string prefix = MakeModPrefix(modDir);
                if (UseYAMLFileAsSource)
                {
                    if (db.AddFromModYaml($"{modDir}/GameText.yaml", prefix))
                    {
                        db.AddFromModYaml($"{modDir}/GameText.Missing.RUS.yaml", prefix, logMerge:true);
                        db.AddFromModYaml($"{modDir}/GameText.Missing.SPA.yaml", prefix, logMerge:true);
                    }
                }
                if (db.NumModLocalizations == 0)
                {
                    db.AddModLocalizations(GetGameText("ENG", $"{modDir}/Localization/English/GameText_EN.xml"), prefix);
                    db.AddModLocalizations(GetGameText("RUS", $"{modDir}/Localization/Russian/GameText_RU.xml"), prefix);
                }
                db.FinalizeModLocalization();
                db.ExportModYaml($"{modDir}/GameText.yaml");
                db.ExportMissingModYaml("RUS", $"{modDir}/GameText.Missing.RUS.yaml");
                db.ExportMissingModYaml("SPA", $"{modDir}/GameText.Missing.SPA.yaml");
            }
            return db;
        }

        // Tooltips is mostly a hack, because we don't use half of the EnumGenerator features
        static void CreateGameTipsEnum(string bbDir, string sourceDir, LocalizationDB db)
        {
            string enumFile = $"{sourceDir}/GameTips.cs";
            string yamlFile = $"{bbDir}/ToolTips.yaml";
            var gen = new LocalizationDB(db, "GameTips");
            gen.LoadIdentifiers(enumFile, yamlFile);
            if (UseYAMLFileAsSource)
            {
                gen.AddToolTips(TextToken.FromYaml(yamlFile));
            }
            if (gen.NumToolTips == 0)
            {
                gen.AddToolTips(GetToolTips("ANY", $"{bbDir}/Tooltips/Tooltips.xml"));
            }
            gen.ExportCsharp(enumFile);
            gen.ExportTipsYaml(yamlFile);
            // no tooltips for Mods
        }

        static void UpgradeGameXmls(string contentDir, LocalizationDB db)
        {
            UpgradeXmls(db, $"{contentDir}/Buildings", 
                             "NameTranslationIndex", "DescriptionIndex", "ShortDescriptionIndex");
        }

        static void UpgradeXmls(LocalizationDB db, string contentFolder, params string[] tags)
        {
            string[] xmls = Directory.GetFiles(contentFolder, "*.xml");
            foreach (string xmlFile in xmls)
                UpgradeXml(db, xmlFile, tags);
        }

        static void UpgradeXml(LocalizationDB db, string xmlFile, string[] tags)
        {
            if (!File.Exists(xmlFile))
                return;

            Log.Write(ConsoleColor.Blue, $"Upgrading XML Localizations: {xmlFile}");
            string[] lines = File.ReadAllLines(xmlFile);
            int modified = 0;
            Regex[] patterns = tags.Select(tag => new Regex($"<{tag}>.+\\d+.+<\\/{tag}>")).ToArray();
            Regex numberMatcher = new Regex($"\\d+");
            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                foreach (Regex pattern in patterns)
                {
                    if (pattern.Match(line).Success)
                    {
                        // replace number with the new id
                        int id = int.Parse(numberMatcher.Match(line).Value);
                        string nameId = db.GetNameId(id);
                        string replacement = numberMatcher.Replace(line, nameId);
                        Log.Write(ConsoleColor.Cyan, $"replace {id} => {nameId}");
                        ++modified;
                        lines[i] = replacement;
                        break;
                    }
                }
            }

            if (modified > 0)
            {
                //Log.Write(ConsoleColor.Green, $"Modified {modified} entries");
                //File.WriteAllLines(xmlFile, lines);
            }
        }

        public static void Main(string[] args)
        {
            string workingDir = Directory.GetCurrentDirectory();
            string contentDir = $"{workingDir}/Content";
            string outputDir = $"{workingDir}/Ship_Game/Data";
            string modDir = $"{workingDir}/StarDrive/Mods/Combined Arms";
            if (!Directory.Exists(contentDir) || !Directory.Exists(outputDir))
            {
                Log.Write(ConsoleColor.Red, "WorkingDir must be BlackBox code directory with Content and Ship_Game/Data folders!");
            }
            else
            {
                TextDatabases dbs = CreateGameTextEnum(contentDir, modDir, outputDir);
                CreateGameTipsEnum(contentDir, outputDir, dbs.Game);
                UpgradeGameXmls(contentDir, dbs.Game);
                UpgradeGameXmls(modDir, dbs.Mod);
            }

            Log.Write(ConsoleColor.Gray, "Press any key to continue...");
            Console.ReadKey(false);
        }
    }

    public static class Log
    {
        public static void Write(ConsoleColor color, string message)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = original;
        }
    }
}
