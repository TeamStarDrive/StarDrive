using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public IEnumerable<LangToken> GetTokens(string lang) => TokenList.Select(t => new LangToken(lang, t.Index, null, t.Text));
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
        public IEnumerable<LangToken> GetTokens(string lang) => ToolTipsList.Select(t => new LangToken(lang, t.TIP_ID, null, t.Title));
    }
    
    /// <summary>
    /// Converts StarDrive GameText into C# enums
    /// </summary>
    public static class GameTextToEnum_Main
    {
        static T Deserialize<T>(string path)
        {
            var ser = new XmlSerializer(typeof(T));
            return (T)ser.Deserialize(File.OpenRead(path));
        }

        static IEnumerable<LangToken> GetGameText(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetGameText: {lang} {path}");
            return Deserialize<LocalizationFile>(path).GetTokens(lang);
        }

        static IEnumerable<LangToken> GetToolTips(string lang, string path)
        {
            Log.Write(ConsoleColor.Cyan, $"GetToolTips: {lang} {path}");
            return Deserialize<Tooltips>(path).GetTokens(lang);
        }

        static void CreateGameTextEnum(string contentDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{contentDir}/GameText.yaml";
            var gen = new EnumGenerator("Ship_Game", "GameText");
            gen.LoadIdentifiers(enumFile, yamlFile);
            if (File.Exists(yamlFile))
            {
                gen.AddLocalizations(LangToken.FromYaml(yamlFile));
            }
            else
            {
                gen.AddLocalizations(GetGameText("ENG", $"{contentDir}/Localization/English/GameText_EN.xml"));
                gen.AddLocalizations(GetGameText("SPA", $"{contentDir}/Localization/Spanish/GameText.xml"));
                gen.AddLocalizations(GetGameText("RUS", $"{contentDir}/Localization/Russian/GameText_RU.xml"));
            }
            gen.ExportCsharp(enumFile);
            gen.ExportYaml(yamlFile);

            if (Directory.Exists(modDir))
            {
                string modYamlFile = $"{modDir}/GameText.yaml";
                var mod = new ModTextExporter(gen, "ModGameText");
                if (File.Exists(modYamlFile))
                {
                    gen.AddLocalizations(LangToken.FromYaml(modYamlFile));
                }
                else
                {
                    mod.AddModLocalizations(GetGameText("ENG", $"{modDir}/Localization/English/GameText_EN.xml"));
                    mod.AddModLocalizations(GetGameText("RUS", $"{modDir}/Localization/Russian/GameText_RU.xml"));
                }
                mod.ExportModYaml(modYamlFile);
            }
        }

        static void CreateGameTipsEnum(string contentDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameTips.cs";
            string yamlFile = $"{contentDir}/ToolTips.yaml";
            var gen = new EnumGenerator("Ship_Game", "GameTips");
            gen.LoadIdentifiers(enumFile, yamlFile);
            if (File.Exists(yamlFile))
            {
                gen.AddLocalizations(LangToken.FromYaml(yamlFile));
            }
            else
            {
                gen.AddLocalizations(GetToolTips("ENG", $"{contentDir}/Tooltips/Tooltips.xml"));
            }
            gen.ExportCsharp(enumFile);
            gen.ExportYaml(yamlFile);

            // no tooltips for Mods
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
                CreateGameTextEnum(contentDir, modDir, outputDir);
                CreateGameTipsEnum(contentDir, modDir, outputDir);
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
