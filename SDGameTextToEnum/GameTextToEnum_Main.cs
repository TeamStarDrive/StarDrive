using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SDGameTextToEnum
{
    public sealed class LocalizationFile
    {
        public List<Token> TokenList;
    }

    public sealed class ToolTip
    {
        public int TIP_ID; // Serialized from: Tooltips.xml
        public int Data; // Serialized from: Tooltips.xml
        public string Title; // Serialized from: Tooltips.xml

    }

    public sealed class Tooltips
    {
        public List<ToolTip> ToolTipsList;
        public IEnumerable<Token> GetTokens() => ToolTipsList.Select(t => new Token(t.TIP_ID, t.Title));
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

        static LocalizationFile LoadYaml(string yamlFile)
        {
            return null;
        }

        static void CreateGameTextEnum(string contentDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameText.cs";
            string yamlFile = $"{contentDir}/GameText.yaml";
            var gen = new EnumGenerator("Ship_Game", "GameText");
            var eng = Deserialize<LocalizationFile>($"{contentDir}/Localization/English/GameText_EN.xml");
            var spa = Deserialize<LocalizationFile>($"{contentDir}/Localization/Spanish/GameText.xml");
            var rus = Deserialize<LocalizationFile>($"{contentDir}/Localization/Russian/GameText_RU.xml");
            gen.LoadIdentifiers(enumFile, yamlFile);
            gen.AddLocalizations("ENG", eng.TokenList);
            gen.AddLocalizations("SPA", spa.TokenList);
            gen.AddLocalizations("RUS", rus.TokenList);
            gen.ExportCsharp(enumFile);
            gen.ExportYaml(yamlFile);

            if (Directory.Exists(modDir))
            {
                string modYamlFile = $"{modDir}/GameText.yaml";
                var mod = new ModTextExporter(gen, "ModGameText");
                var eng2 = Deserialize<LocalizationFile>($"{modDir}/Localization/English/GameText_EN.xml");
                var rus2 = Deserialize<LocalizationFile>($"{modDir}/Localization/Russian/GameText_RU.xml");
                mod.AddModLocalizations("ENG", eng2.TokenList);
                mod.AddModLocalizations("RUS", rus2.TokenList);
                mod.ExportModYaml(modYamlFile);
            }
        }

        static void CreateGameTipsEnum(string contentDir, string modDir, string outputDir)
        {
            string enumFile = $"{outputDir}/GameTips.cs";
            string yamlFile = $"{contentDir}/ToolTips.yaml";
            var gen = new EnumGenerator("Ship_Game", "GameTips");
            var tips = Deserialize<Tooltips>($"{contentDir}/Tooltips/Tooltips.xml");
            gen.LoadIdentifiers(enumFile, yamlFile);
            gen.AddLocalizations("ENG", tips.GetTokens());
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
