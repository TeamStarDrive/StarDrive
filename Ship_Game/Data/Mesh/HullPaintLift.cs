using System;
using System.IO;
using SDUtils;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Mesh;

// how far each tint map's dark paint is lifted toward its owner's color, from Content/HullPaintLift.yaml with a mod's file merged over it by tint map name
public static class HullPaintLift
{
    public const float DefaultShare = 0.85f;
    const float MaxShare = 0.95f;

    sealed class Table
    {
        public string ModDir;
        public float Default = DefaultShare;
        public readonly Map<string, float> Shares = new(StringComparer.OrdinalIgnoreCase);
    }

    static volatile Table Loaded;

    // the share of its owner's color's lightness that a texel at the paint's average lightness is lifted to; 0 leaves the paint at its own lightness
    public static float ShareOf(string tintMapFile)
    {
        Table table = Loaded;
        if (table == null || table.ModDir != ResourceManager.ModContentDir)
            Loaded = table = Load();
        return tintMapFile.NotEmpty() && table.Shares.TryGetValue(Path.GetFileName(tintMapFile), out float share) ? share : table.Default;
    }

    static Table Load()
    {
        var table = new Table { ModDir = ResourceManager.ModContentDir };
        try
        {
            using var parser = new YamlParser(ResourceManager.ContentInfo("HullPaintLift.yaml"), ResourceManager.GetModFile("HullPaintLift.yaml"));
            if (parser.Root.FindSubNode("Default", out YamlNode defaultNode) && ValidShare(defaultNode) is float defaultShare)
                table.Default = defaultShare;
            if (parser.Root.FindSubNode("TintMaps", out YamlNode tintMaps))
            {
                foreach (YamlNode row in tintMaps.Nodes)
                {
                    if (row.Name.IsEmpty())
                        Log.Warning($"HullPaintLift.yaml: '{row.Key}' is not a tint map file name, ignored; quote a name that starts with a digit");
                    else if (ValidShare(row) is float share)
                        table.Shares[row.Name] = share;
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning($"HullPaintLift.yaml could not be read, every tint map takes the default lift {DefaultShare}: {e.Message}");
        }
        return table;
    }

    static float? ValidShare(YamlNode node)
    {
        float share = node.Value switch { float f => f, int i => i, _ => float.NaN };
        if (share >= 0f && share <= MaxShare)
            return share;
        Log.Warning($"HullPaintLift.yaml: '{node.Name}: {node.Value}' is not a lift between 0 and {MaxShare}, ignored");
        return null;
    }
}
