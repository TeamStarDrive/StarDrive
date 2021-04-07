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

        static LocalizationDB CreateGameTextEnum(string gameDir, string bbDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{bbDir}/GameText.yaml";

            var usages = LocalizationUsages.Create(gameDir, modDir);
            var db = new LocalizationDB("Ship_Game", "GameText", usages);
            db.LoadIdentifiers(enumFile, yamlFile);
            db.Prefix = "BB";
            db.ModPrefix = MakeModPrefix(modDir);

            if (UseYAMLFileAsSource)
            {
                if (db.AddFromYaml(yamlFile))
                {
                    db.AddFromYaml($"{bbDir}/GameText.Missing.RUS.yaml", logMerge:true);
                    db.AddFromYaml($"{bbDir}/GameText.Missing.SPA.yaml", logMerge:true);
                }
            }
            if (db.NumLocalizations == 0)
            {
                db.AddLocalizations(GetGameText("ENG", $"{bbDir}/Localization/English/GameText_EN.xml"));
                db.AddLocalizations(GetGameText("RUS", $"{bbDir}/Localization/Russian/GameText_RU.xml"));
                db.AddLocalizations(GetGameText("SPA", $"{bbDir}/Localization/Spanish/GameText.xml"));
            }
            db.ExportCsharp(enumFile);
            db.ExportYaml(yamlFile);
            db.ExportMissingTranslationsYaml("RUS", $"{bbDir}/GameText.Missing.RUS.yaml");
            db.ExportMissingTranslationsYaml("SPA", $"{bbDir}/GameText.Missing.SPA.yaml");

            if (Directory.Exists(modDir))
            {
                if (UseYAMLFileAsSource)
                {
                    if (db.AddFromModYaml($"{modDir}/GameText.yaml"))
                    {
                        db.AddFromModYaml($"{modDir}/GameText.Missing.RUS.yaml", logMerge:true);
                        db.AddFromModYaml($"{modDir}/GameText.Missing.SPA.yaml", logMerge:true);
                    }
                }
                if (db.NumModLocalizations == 0)
                {
                    db.AddModLocalizations(GetGameText("ENG", $"{modDir}/Localization/English/GameText_EN.xml"));
                    db.AddModLocalizations(GetGameText("RUS", $"{modDir}/Localization/Russian/GameText_RU.xml"));
                }
                db.FinalizeModLocalization();
                db.ExportModYaml($"{modDir}/GameText.yaml");
                db.ExportMissingModYaml("RUS", $"{modDir}/GameText.Missing.RUS.yaml");
                db.ExportMissingModYaml("SPA", $"{modDir}/GameText.Missing.SPA.yaml");
            }
            return db;
        }

        static void UpgradeGameXmls(string contentDir, LocalizationDB db, bool mod)
        {
            UpgradeXmls(db, mod, $"{contentDir}/Buildings", 
                             "NameTranslationIndex", "DescriptionIndex", "ShortDescriptionIndex");
        }

        static void UpgradeXmls(LocalizationDB db, bool mod, string contentFolder, params string[] tags)
        {
            string[] xmls = Directory.GetFiles(contentFolder, "*.xml");
            foreach (string xmlFile in xmls)
                UpgradeXml(db, mod, xmlFile, tags);
        }

        static void UpgradeXml(LocalizationDB db, bool mod, string xmlFile, string[] tags)
        {
            if (!File.Exists(xmlFile))
                return;

            Log.Write(ConsoleColor.Blue, $"Upgrading XML Localizations: {xmlFile}");
            string[] lines = File.ReadAllLines(xmlFile);
            int modified = 0;
            Regex[] patterns = tags.Select(tag => new Regex($"<{tag}>.+\\d+.+<\\/{tag}>")).ToArray();
            Regex numberMatcher = new Regex("\\d+");
            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                foreach (Regex pattern in patterns)
                {
                    if (pattern.Match(line).Success)
                    {
                        // replace number with the new id
                        int id = int.Parse(numberMatcher.Match(line).Value);
                        string nameId = mod ? db.GetModNameId(id) : db.GetNameId(id);
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

        static void ReplaceCsharpTokens(string codeDir, LocalizationDB db)
        {
            string[] codeFiles = Directory.GetFiles(codeDir, "*.cs", SearchOption.AllDirectories);
            foreach (string fileName in codeFiles)
            {
                ReplaceInCsharpFile(fileName, db);
            }
        }

        static void ReplaceInCsharpFile(string fileName, LocalizationDB db)
        {
            string[] lines = File.ReadAllLines(fileName);
            int modified = 0;
            int i = 0;
            void ModifyCurrentLine(string newValue)
            {
                lines[i] = newValue;
                ++modified;
                --i; // after we modify current line, skip back to reprocess this line
            }

            var mInteger = new Regex("\\d+");
            var mLocToken = new Regex("Localizer\\.Token\\(\\d+\\)");
            var mLocText  = new Regex("new LocalizedText\\(\\d+\\)");
            Func<string, string> rLocToken = (nameId) => $"Localizer.Token(GameText.{nameId})";
            Func<string, string> rLocText = (nameId) => $"GameText.{nameId}";

            bool ReplaceIntWithNameId(string line, Regex matcher, Func<string, string> replacement)
            {
                var m = matcher.Match(line);
                if (m.Success)
                {
                    var intM = mInteger.Match(m.Value);
                    if (intM.Success && int.TryParse(intM.Value, out int id))
                    {
                        string nameId = db.GetNameId(id);
                        string replaceWith = replacement(nameId);
                        ModifyCurrentLine(line.Replace(m.Value, replaceWith));
                        return true;
                    }
                }
                return false;
            }

            for (; i < lines.Length; ++i)
            {
                if (ReplaceIntWithNameId(lines[i], mLocToken, rLocToken)) continue;
                if (ReplaceIntWithNameId(lines[i], mLocText, rLocText)) continue;
            }

            if (modified > 0)
            {
                Log.Write(ConsoleColor.Green, $"Modified  {fileName}  ({modified})");
                File.WriteAllLines(fileName, lines);
            }
        }

        public static void Main(string[] args)
        {
            string solutionDir = Directory.GetCurrentDirectory();
            string gameDir = $"{solutionDir}/StarDrive/Content";
            string bbDir = $"{solutionDir}/Content";
            string codeDir = $"{solutionDir}/Ship_Game";
            string outputDir = $"{solutionDir}/Ship_Game/Data";
            string modDir = $"{solutionDir}/StarDrive/Mods/Combined Arms";
            Directory.SetCurrentDirectory($"{solutionDir}/StarDrive");

            if (!Directory.Exists(gameDir) ||
                !Directory.Exists(bbDir)   ||
                !Directory.Exists(outputDir))
            {
                Log.Write(ConsoleColor.Red, "WorkingDir must be BlackBox code directory with Content and Ship_Game/Data folders!");
            }
            else
            {
                LocalizationDB db = CreateGameTextEnum(gameDir, bbDir, modDir, outputDir);
                UpgradeGameXmls(bbDir, db, mod:false);
                UpgradeGameXmls(modDir, db, mod:true);
                ReplaceCsharpTokens(codeDir, db);
            }

            Log.Write(ConsoleColor.Gray, "Press any key to continue...");
            Console.ReadKey(false);
            Ship_Game.Parallel.ClearPool();
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
