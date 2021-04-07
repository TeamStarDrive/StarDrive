using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SDGameTextToEnum
{
    public class LocalizationUsages
    {
        Dictionary<int, LocalizationUsage> Usages = new Dictionary<int, LocalizationUsage>();
        HashSet<int> Tooltips;
        LocalizationUsages() {}
        public bool Get(int id, out LocalizationUsage usage) => Usages.TryGetValue(id, out usage);
        public LocalizationUsage Get(int id) => Get(id, out LocalizationUsage usage) ? usage : null;
        public bool Contains(int id) => Usages.ContainsKey(id);

        static List<string> GetXmlFiles(string contentDir)
        {
            string[] names = Directory.GetFiles(contentDir, "*.xml", SearchOption.AllDirectories);
            var files = new List<string>();
            foreach (string name in names)
            {
                string path = name.Replace("\\", "/");
                if (!path.Contains("/Technology/"))
                    continue;
                if (!path.Contains("/Localization/") &&
                    !path.Contains("/Tooltips/") &&
                    !path.Contains("/Hulls/") &&
                    !path.Contains("/StarterShips/") &&
                    !path.Contains("/SavedDesigns/") &&
                    !path.Contains("/ShipDesigns/") &&
                    !path.Contains("/Players/") &&
                    !path.Contains("/Weapons/"))
                {
                    files.Add(path);
                }
            }
            return files;
        }

        static HashSet<int> GetTooltipIds(string gameDir)
        {
            var ser = new XmlSerializer(typeof(Tooltips));
            var tt = (Tooltips)ser.Deserialize(File.OpenRead($"{gameDir}/Tooltips/Tooltips.xml"));
            var tips = new HashSet<int>();
            foreach (ToolTip tip in tt.ToolTipsList)
            {
                tips.Add(tip.Data);
            }
            return tips;
        }

        XmlNode FindElement(XmlNode parent, string exactTagName)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    if (node.Name == exactTagName)
                        return node;
                    XmlNode found = FindElement(node, exactTagName);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        List<XmlNode> GetMatchingNodes(XmlNode root, string partialTag)
        {
            var matches = new List<XmlNode>();
            void RecurseNodes(XmlNodeList nodes)
            {
                foreach (XmlNode node in nodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        if (node.Name.Contains(partialTag))
                            matches.Add(node);
                        RecurseNodes(node.ChildNodes);
                    }
                }
            }
            RecurseNodes(root.ChildNodes);
            return matches;
        }

        string MakeIdentifier(string text)
        {
            return string.Concat(text.Split(' '));
        }

        void LoadFile(string file)
        {
            var doc = new XmlDocument();
            doc.Load(file);
            
            var indexNodes = GetMatchingNodes(doc, "Index");
            foreach (XmlNode node in indexNodes)
            {
                string tag = node.Name;
                if (tag == "FlagIndex")
                    continue;

                //Console.WriteLine($"<{node.Name}> {node.FirstChild.Value}");
                if (tag.Contains("Index") && int.TryParse(node.FirstChild.Value, out int id))
                {
                    var usageEnum = GetUsage(id, tag, file);

                    string suffix = tag.Replace("Index", "");
                    if (suffix == "Description") suffix = "Desc";
                    else if (suffix == "TroopName") suffix = "Name";
                    else if (suffix == "TroopDescription") suffix = "Desc";
                    else if (suffix == "NameTranslation") suffix = "Name";
                    else if (suffix == "ShortDescription") suffix = "Brief";

                    string name = MakeIdentifier(Path.GetFileNameWithoutExtension(file));
                    if (usageEnum == Usage.Technology)
                    {
                        if (node.ParentNode.Name == "UnlockedBonus")
                        {
                            XmlNode typeNode = FindElement(node.ParentNode, "BonusType");
                            if (typeNode != null)
                                name = name + "_" + MakeIdentifier(typeNode.FirstChild.Value);
                        }
                    }

                    var usage = new LocalizationUsage(id, usageEnum, name, suffix, file);
                    lock (Usages)
                    {
                        if (!Usages.ContainsKey(id))
                            Usages.Add(id, usage);
                    }
                }
            }
        }

        void Load(string gameDir, string modDir)
        {
            var xmlFiles = GetXmlFiles(gameDir);
            xmlFiles.AddRange(GetXmlFiles(modDir));

            Tooltips = GetTooltipIds(gameDir);

            void ProcessFiles(int start, int end)
            {
                for (int fileId = start; fileId < end; ++fileId)
                {
                    LoadFile(xmlFiles[fileId]);
                }
            }

            ProcessFiles(0, xmlFiles.Count);
            //Ship_Game.Parallel.For(xmlFiles.Count, ProcessFiles);

            LocalizationUsage[] flatUsages = Usages.Values.ToArray();
            Array.Sort(flatUsages, (a, b) => string.Compare(a.NameId, b.NameId));

            int progress = 0;
            foreach (LocalizationUsage u in flatUsages)
            {
                ++progress;

                string prefix = "BB";
                if (u.File.Contains("Combined Arms"))
                    prefix = "CA";

                string nameId = prefix+"_"+u.NameId;
                Log.Write(ConsoleColor.Gray, $"usage {progress} Id={u.Id} {nameId}");
            }
        }

        Usage GetUsage(int id, string tag, string path)
        {
            if (Tooltips.Contains(id)) return Usage.Tooltip;
            if (tag == "TroopName" || tag == "TroopDescription")
                return Usage.Troop;

            if (path.Contains("/Buildings/"))   return Usage.Building;
            if (path.Contains("/Weapons/"))     return Usage.Weapon;
            if (path.Contains("/ShipModules/")) return Usage.Module;
            if (path.Contains("/Technology/"))  return Usage.Technology;
            if (path.Contains("/Technology_HardCore/"))  return Usage.Technology;
            if (path.Contains("/Artifacts/"))  return Usage.Artifact;
            if (path.Contains("/Races/")) return Usage.Races;

            Log.Write(ConsoleColor.Yellow, $"Unknown usage id={id} {path}");
            return Usage.Unknown;
        }

        public static LocalizationUsages Create(string gameDir, string modDir)
        {
            var usages = new LocalizationUsages();
            usages.Load(gameDir, modDir);
            return usages;
        }
    }
}
