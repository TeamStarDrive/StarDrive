using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Ship_Game.Tools.Localization
{
    /// <summary>
    /// Converts StarDrive GameText into C# enums
    /// </summary>
    public static class LocalizationTool
    {
        public static bool UseYAMLFileAsSource = true;

        static IEnumerable<TextToken> GetGameText(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetGameText: {lang} {path}");
            var ser = new XmlSerializer(typeof(LocalizationFile));
            var loc = (LocalizationFile)ser.Deserialize(File.OpenRead(path));
            return loc.TokenList.Select(t => new TextToken(lang, t.Index, null, t.Text));
        }

        static LocalizationDB CreateGameTextEnum(string gameContent, string modContent, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{gameContent}/GameText.yaml";

            var db = new LocalizationDB("Ship_Game", "GameText", gameContent, modContent);
            db.LoadIdentifiers(enumFile, yamlFile);

            if (UseYAMLFileAsSource)
            {
                if (db.AddFromYaml(yamlFile))
                {
                    db.AddFromYaml($"{gameContent}/GameText.Missing.RUS.yaml", logMerge:true);
                    db.AddFromYaml($"{gameContent}/GameText.Missing.SPA.yaml", logMerge:true);
                }
            }
            if (db.NumLocalizations == 0)
            {
                db.AddLocalizations(GetGameText("ENG", $"{gameContent}/Localization/English/GameText_EN.xml"));
                db.AddLocalizations(GetGameText("RUS", $"{gameContent}/Localization/Russian/GameText_RU.xml"));
                db.AddLocalizations(GetGameText("SPA", $"{gameContent}/Localization/Spanish/GameText.xml"));
            }

            if (Directory.Exists(outputDir))
                db.ExportCsharp(enumFile);

            db.ExportYaml(yamlFile);
            db.ExportMissingTranslationsYaml("RUS", $"{gameContent}/GameText.Missing.RUS.yaml");
            db.ExportMissingTranslationsYaml("SPA", $"{gameContent}/GameText.Missing.SPA.yaml");

            if (Directory.Exists(modContent))
            {
                if (UseYAMLFileAsSource)
                {
                    if (db.AddFromModYaml($"{modContent}/GameText.yaml"))
                    {
                        db.AddFromModYaml($"{modContent}/GameText.Missing.RUS.yaml", logMerge:true);
                        db.AddFromModYaml($"{modContent}/GameText.Missing.SPA.yaml", logMerge:true);
                    }
                }
                if (db.NumModLocalizations == 0)
                {
                    db.AddModLocalizations(GetGameText("ENG", $"{modContent}/Localization/English/GameText_EN.xml"));
                    db.AddModLocalizations(GetGameText("RUS", $"{modContent}/Localization/Russian/GameText_RU.xml"));
                }
                db.FinalizeModLocalization();
                db.ExportModYaml($"{modContent}/GameText.yaml");
                db.ExportMissingModYaml("RUS", $"{modContent}/GameText.Missing.RUS.yaml");
                db.ExportMissingModYaml("SPA", $"{modContent}/GameText.Missing.SPA.yaml");
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
            Regex[] patterns = tags.Select(tag => new Regex($"<{tag}>.+\\d+.+<\\/{tag}>"));
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

        public static void Run(string mod = "")
        {
            string starDrive = Directory.GetCurrentDirectory();
            string gameContent = $"{starDrive}/Content";
            string modContent = mod.NotEmpty() ? $"{starDrive}/Mods/{mod}" : "";
            
            if (!Directory.Exists(gameContent))
                throw new Exception($"Could not find StarDrive/Content at: {gameContent}");
            
            if (mod.NotEmpty() && !Directory.Exists(modContent))
                throw new Exception($"Could not find Mod at: {modContent}");

            string solutionDir = Path.GetFullPath($"{starDrive}/..");
            string bbContent = $"{solutionDir}/Content"; // OPTIONAL
            string codeDir = $"{solutionDir}/Ship_Game"; // OPTIONAL
            string outputDir = $"{codeDir}/Data"; // OPTIONAL

            LocalizationDB db = CreateGameTextEnum(gameContent, modContent, outputDir);

            if (Directory.Exists(bbContent))
                UpgradeGameXmls(bbContent, db, mod:false);

            if (mod.NotEmpty())
                UpgradeGameXmls(modContent, db, mod:true);

            if (Directory.Exists(codeDir))
                ReplaceCsharpTokens(codeDir, db);
        }
    }
}
