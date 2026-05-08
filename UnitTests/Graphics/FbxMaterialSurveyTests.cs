using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Data.Mesh;

namespace UnitTests.Graphics;

/// <summary>
/// §4.6.B(b) follow-up: surveys every .fbx under game/Content/Model/Ships/ for its
/// material values — Specular factor, diffuse / specular / normal / emissive texture
/// paths — so the user can audit which ships have correctly-set spec and which lost
/// it through the OBJ→FBX pipeline (Combined Arms etc.).
///
/// The MeshExporter.cs:216 bug passed `fx.SpecularPower` (16-64) as the C-API's
/// `specular` arg (expects 0-1), so blackbox FBXs may carry SpecularFactor values
/// outside the FBX SDK convention. OBJ-derived modded FBXs carry whatever Ns/1000
/// the source OBJ had — typically 0 if no Ns line was present.
///
/// Run via:
///   dotnet test --filter "FullyQualifiedName~FbxMaterialSurvey"
/// Output: c:\tmp\fbx-specular-survey.csv
/// </summary>
[TestClass]
public class FbxMaterialSurveyTests : StarDriveTest
{
    // §4.6 #9 diagnostic: dumps colour-channel material values (Diffuse, Ambient,
    // Emissive, Specular RGB) for the asteroid + cargo-ship FBXs that render
    // black-with-spec on the Universe screen. Prints to test output so we can
    // see whether DiffuseColor=(0,0,0) is the smoking gun without combing
    // through the CSV.
    [TestMethod]
    public void DumpBlackHullMaterials_AsteroidsAndCargo()
    {
        var dumper = new FbxMaterialDumper(Content);
        string contentRoot = Content.RootDirectory;
        string[] subdirs =
        {
            Path.Combine(contentRoot, "Model", "Asteroids"),
            Path.Combine(contentRoot, "Model", "Ships", "CargoShips"),
        };
        Console.WriteLine("FBX,GROUP,MAT,SPEC,DIF_RGB,AMB_RGB,EMI_RGB,SPC_RGB,ALPHA,DIFFUSE_PATH");
        int total = 0, blackDiffuse = 0;
        foreach (string dir in subdirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string fbxPath in Directory.EnumerateFiles(dir, "*.fbx", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(contentRoot, fbxPath);
                foreach (var row in dumper.SurveyFbx(fbxPath))
                {
                    total++;
                    bool isBlack = row.DiffuseColor.X < 0.001f
                                   && row.DiffuseColor.Y < 0.001f
                                   && row.DiffuseColor.Z < 0.001f;
                    if (isBlack) blackDiffuse++;
                    Console.WriteLine($"{rel},{row.GroupName},{row.MaterialName},{row.Specular:F3}," +
                        $"({row.DiffuseColor.X:F2},{row.DiffuseColor.Y:F2},{row.DiffuseColor.Z:F2})," +
                        $"({row.AmbientColor.X:F2},{row.AmbientColor.Y:F2},{row.AmbientColor.Z:F2})," +
                        $"({row.EmissiveColor.X:F2},{row.EmissiveColor.Y:F2},{row.EmissiveColor.Z:F2})," +
                        $"({row.SpecularColor.X:F2},{row.SpecularColor.Y:F2},{row.SpecularColor.Z:F2})," +
                        $"{row.Alpha:F2}," +
                        $"{Path.GetFileName(row.DiffusePath)}");
                }
            }
        }
        Console.WriteLine($"--- {total} material rows; {blackDiffuse} have DiffuseColor=(0,0,0) ---");
        Assert.IsTrue(total > 0, "No FBX material rows surveyed");
    }

    [TestMethod]
    public void DumpAllShipFbxMaterials()
    {
        string contentRoot = Content.RootDirectory;
        string shipsDir = Path.Combine(contentRoot, "Model", "Ships");
        Assert.IsTrue(Directory.Exists(shipsDir), $"Ships dir missing: {shipsDir}");
        SurveyDirectory(shipsDir, "c:\\tmp\\fbx-specular-survey.csv");
    }

    // §4.6.B(b) follow-up: CA ship FBXs live under `game/Mods/Combined Arms/`,
    // not under `game/Content/Model/Ships/`. Run this method to dump their
    // material values to a sibling CSV so we can derive their proposed
    // SpecularFactor overrides from the vanilla cluster-median table.
    [TestMethod]
    public void DumpCombinedArmsFbxMaterials()
    {
        string contentRoot = Content.RootDirectory;
        // contentRoot is `game/Content`; CA ships live under `game/Mods/Combined Arms/`
        string gameDir = Path.GetDirectoryName(contentRoot.TrimEnd('\\', '/'))
                         ?? throw new DirectoryNotFoundException("can't resolve game/ from content root");
        string caDir = Path.Combine(gameDir, "Mods", "Combined Arms");
        Assert.IsTrue(Directory.Exists(caDir), $"Combined Arms dir missing: {caDir}");
        SurveyDirectory(caDir, "c:\\tmp\\fbx-specular-survey-ca.csv");
    }

    // §4.6.B(b) follow-up: read both surveys, derive vanilla cluster medians
    // (keyed by diffuse-texture basename), and emit a proposed override per CA
    // material. Output `c:\tmp\fbx-ca-proposed-overrides.csv` columns:
    //   fbxRel,groupName,materialName,currentSpec,proposedSpec,inheritFrom,reason
    // The "inheritFrom" column points at the vanilla texture cluster the median
    // came from (or "<no-vanilla-match>" / "<survey-median>" for fallbacks).
    [TestMethod]
    public void ProposeCombinedArmsSpecularOverrides()
    {
        string vanillaCsv = "c:\\tmp\\fbx-specular-survey.csv";
        string caCsv      = "c:\\tmp\\fbx-specular-survey-ca.csv";
        string outPath    = "c:\\tmp\\fbx-ca-proposed-overrides.csv";
        Assert.IsTrue(File.Exists(vanillaCsv), $"Run DumpAllShipFbxMaterials first ({vanillaCsv} missing)");
        Assert.IsTrue(File.Exists(caCsv),      $"Run DumpCombinedArmsFbxMaterials first ({caCsv} missing)");

        // Build the vanilla cluster medians. Filter the same set of CA-origin
        // ships removed in earlier analysis (some were promoted into blackbox
        // but trace back to CA exports with the same broken Specular).
        var vanillaIgnore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cordrazine_Station.fbx", "Draylok_Station.fbx", "Kulrathi_Station.fbx",
            "Kuma Naka.fbx", "Kuma Oki.fbx", "Kuma Sukoshi.fbx", "Yamamoto.fbx",
            "OpterisStation.fbx", "Pollops_Station.fbx", "Ralyeh_Station.fbx",
            "AncientFrigate.fbx", "Behemoth.fbx", "RemnantPortal.fbx",
            "LightCruiser.fbx", "Vulfar_Station.fbx",
            "TypeWI.fbx", "TypeWII.fbx", "TypeWIII.fbx", "TypeXIX.fbx",
        };

        // Normalize basenames so `ship04_d_0.dds` (CA convention) matches
        // `ship04_d.dds` (vanilla without the suffix). The trailing `_<digit>`
        // before the extension is an XNB-decoder duplicate-strip artifact —
        // same texture content, different filename. Vanilla isn't consistent
        // (ship11_d.dds vs ship16_d_0.dds), so normalize on both sides.
        static string NormalizeTextureKey(string basename)
        {
            if (string.IsNullOrEmpty(basename)) return basename;
            int dot = basename.LastIndexOf('.');
            string stem = dot >= 0 ? basename.Substring(0, dot) : basename;
            string ext  = dot >= 0 ? basename.Substring(dot)    : "";
            int us = stem.LastIndexOf('_');
            if (us >= 0 && us < stem.Length - 1)
            {
                bool tailIsDigits = true;
                for (int i = us + 1; i < stem.Length; ++i)
                    if (!char.IsDigit(stem[i])) { tailIsDigits = false; break; }
                if (tailIsDigits) stem = stem.Substring(0, us);
            }
            return stem + ext;
        }

        var clusterValues = new Dictionary<string, List<float>>(StringComparer.OrdinalIgnoreCase);
        var clusterRawKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // normalized → original key for reporting
        foreach (var rec in ReadSurveyCsv(vanillaCsv))
        {
            string fbxName = Path.GetFileName(rec.FbxRel);
            if (vanillaIgnore.Contains(fbxName)) continue;
            string diffName = Path.GetFileName(rec.DiffusePath);
            if (string.IsNullOrEmpty(diffName)) continue;
            string norm = NormalizeTextureKey(diffName);
            if (!clusterValues.TryGetValue(norm, out var list))
            {
                clusterValues[norm] = list = new List<float>();
                clusterRawKey[norm] = diffName;
            }
            list.Add(rec.Specular);
        }

        // Survey-wide median of vanilla, used as fallback when CA texture is
        // CA-exclusive (no vanilla cluster to inherit from).
        var allVanillaSpecs = clusterValues.Values.SelectMany(v => v).ToList();
        allVanillaSpecs.Sort();
        float surveyMedian = allVanillaSpecs.Count > 0
            ? allVanillaSpecs[allVanillaSpecs.Count / 2]
            : 0.1875f;

        var clusterMedians = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in clusterValues)
        {
            kv.Value.Sort();
            clusterMedians[kv.Key] = kv.Value[kv.Value.Count / 2];
        }

        // Self-contained CA factions whose ships ship their own diffuse textures
        // and shouldn't be normalized against vanilla. The Dauntless mod is the
        // canonical example — distinct hull aesthetic, mod-only diffuse maps,
        // owner explicitly opted out.
        var skipFactions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Dauntless",
        };

        // Specific FBX basenames to skip even when they don't sit under a
        // skipFactions folder. ts2_modi / ts3modE are Dauntless-naming ships
        // that live at the `mod models/` root rather than under Dauntless/.
        var skipFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ts2_modi.fbx",
            "ts3modE.fbx",
        };

        // Per-faction fallback cluster — when a CA ship's diffuse texture
        // doesn't match any vanilla cluster directly, but the FBX lives under
        // a known faction folder, inherit from that faction's primary vanilla
        // texture cluster instead of the generic survey-wide median. Maps
        // faction folder name → representative vanilla diffuse basename.
        var factionCluster = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cordrazine", "ship16_d_0.dds" }, // → 0.0938
            { "Draylok",    "ship18_d_0.dds" }, // → 0.0625
            { "Kulrathi",   "ship12_d_0.dds" }, // → 0.1875
            { "Opteris",    "ship19_d_0.dds" }, // → 0.0938
            { "Pollops",    "ship15_d.dds"   }, // → 0.2188
            { "Ralyeh",     "ship17_d_0.dds" }, // → 0.2188
            { "Vulfar",     "ship13_d.dds"   }, // → 0.2188
            { "Vulfen",     "ship13_d.dds"   }, // alt spelling
            { "Terran",     "ship10_d.dds"   }, // → 0.2188 (cluster median, NOT outliers)
        };

        // Specific FBX basenames whose faction can't be inferred from path.
        // shipyard / Station_Small live at mod-models root but are visually
        // Terran-style stations.
        var fileFaction = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "shipyard.fbx",     "Terran" },
            { "Station_Small.fbx", "Terran" },
        };

        var rows = new List<string>
        {
            "fbxRelativePath,groupName,materialName,currentSpec,proposedSpec,inheritFrom,reason"
        };

        int total = 0, changed = 0, sameValue = 0, skipped = 0;
        foreach (var rec in ReadSurveyCsv(caCsv))
        {
            // Faction skip — match on path component (case-insensitive).
            bool skip = false;
            foreach (string seg in rec.FbxRel.Split(new[] { '\\', '/' }))
            {
                if (skipFactions.Contains(seg)) { skip = true; break; }
            }
            // Also skip individual files that live outside a faction folder.
            if (!skip && skipFiles.Contains(Path.GetFileName(rec.FbxRel)))
                skip = true;
            if (skip) { skipped++; continue; }

            total++;
            string diffName = Path.GetFileName(rec.DiffusePath);
            float proposed;
            string from, reason;

            if (string.IsNullOrEmpty(diffName))
            {
                proposed = rec.Specular;
                from = "";
                reason = "no-diffuse-path; keep current";
            }
            else
            {
                string norm = NormalizeTextureKey(diffName);
                if (clusterMedians.TryGetValue(norm, out float median))
                {
                    proposed = median;
                    from = clusterRawKey[norm];
                    int n = clusterValues[norm].Count;
                    reason = $"vanilla-cluster-median (n={n})";
                }
                else
                {
                    // Try faction fallback. Walk the path for a known faction
                    // folder; if none, check the file-specific override.
                    string faction = null;
                    foreach (string seg in rec.FbxRel.Split(new[] { '\\', '/' }))
                    {
                        if (factionCluster.ContainsKey(seg)) { faction = seg; break; }
                    }
                    if (faction == null && fileFaction.TryGetValue(Path.GetFileName(rec.FbxRel), out string ff))
                        faction = ff;

                    if (faction != null
                        && factionCluster.TryGetValue(faction, out string facKey)
                        && clusterMedians.TryGetValue(NormalizeTextureKey(facKey), out float facMedian))
                    {
                        proposed = facMedian;
                        from = facKey;
                        reason = $"faction-fallback ({faction})";
                    }
                    else
                    {
                        proposed = surveyMedian;
                        from = "<survey-median>";
                        reason = "no vanilla cluster; survey-wide median";
                    }
                }
            }

            // If the CA ship's current spec is sensible (in [0.05, 0.5] vanilla
            // band) AND close to the proposed value, keep current — it was
            // probably exported correctly from a SunBurn material.
            bool inSensibleBand = rec.Specular >= 0.05f && rec.Specular <= 0.5f;
            bool closeToProposed = Math.Abs(rec.Specular - proposed) < 0.03f;
            if (inSensibleBand && closeToProposed)
            {
                proposed = rec.Specular;
                reason = "current looks fine; keep";
                sameValue++;
            }
            else if (Math.Abs(rec.Specular - proposed) > 0.001f)
            {
                changed++;
            }
            else
            {
                sameValue++;
            }

            rows.Add(string.Join(",",
                Csv(rec.FbxRel),
                Csv(rec.GroupName),
                Csv(rec.MaterialName),
                rec.Specular.ToString("F4"),
                proposed.ToString("F4"),
                Csv(from),
                Csv(reason)));
        }

        File.WriteAllLines(outPath, rows);
        Console.WriteLine($"Proposed overrides for {total} CA materials ({skipped} skipped factions) → {changed} changed, {sameValue} unchanged → {outPath}");
        Assert.IsTrue(total > 0, "No CA materials read; check survey CSVs");
    }

    struct SurveyRecord
    {
        public string FbxRel;
        public string GroupName;
        public string MaterialName;
        public float  Specular;
        public string DiffusePath;
        public string SpecularPath;
        public string NormalPath;
        public string EmissivePath;
    }

    static IEnumerable<SurveyRecord> ReadSurveyCsv(string path)
    {
        bool first = true;
        foreach (string line in File.ReadLines(path))
        {
            if (first) { first = false; continue; }
            // Survey output uses simple commas without nested quotes (paths
            // can contain commas in theory but in this corpus they don't).
            // Defensive: split on comma respecting "..." quoting.
            string[] parts = SplitCsv(line);
            if (parts.Length < 8) continue;
            yield return new SurveyRecord
            {
                FbxRel       = parts[0],
                GroupName    = parts[1],
                MaterialName = parts[2],
                Specular     = float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : 0f,
                DiffusePath  = parts[4],
                SpecularPath = parts[5],
                NormalPath   = parts[6],
                EmissivePath = parts[7],
            };
        }
    }

    static string[] SplitCsv(string line)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        bool inQuote = false;
        for (int i = 0; i < line.Length; ++i)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"'); ++i;
                }
                else inQuote = !inQuote;
            }
            else if (c == ',' && !inQuote)
            {
                parts.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        parts.Add(sb.ToString());
        return parts.ToArray();
    }

    void SurveyDirectory(string root, string outPath)
    {
        var dumper = new FbxMaterialDumper(Content);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

        var rows = new List<string>
        {
            "fbxRelativePath,groupName,materialName,Specular,DiffusePath,SpecularPath,NormalPath,EmissivePath,AmbR,AmbG,AmbB,DifR,DifG,DifB,SpcR,SpcG,SpcB,EmiR,EmiG,EmiB,Alpha"
        };

        int fbxCount = 0;
        int rowCount = 0;
        foreach (string fbxPath in Directory.EnumerateFiles(root, "*.fbx", SearchOption.AllDirectories))
        {
            fbxCount++;
            string rel = Path.GetRelativePath(root, fbxPath);
            try
            {
                foreach (var row in dumper.SurveyFbx(fbxPath))
                {
                    rows.Add(BuildCsv(rel, row));
                    rowCount++;
                }
            }
            catch (Exception e)
            {
                rows.Add(BuildCsv(rel, new FbxMaterialDumper.MaterialRow
                {
                    GroupName = "<error>",
                    MaterialName = e.GetType().Name,
                    Specular = -1,
                    DiffusePath = e.Message,
                }));
                rowCount++;
            }
        }

        File.WriteAllLines(outPath, rows);
        Console.WriteLine($"Surveyed {fbxCount} FBX files → {rowCount} material rows → {outPath}");
        Assert.IsTrue(fbxCount > 0, $"No FBX files found under {root}");
    }

    static string BuildCsv(string fbxRel, FbxMaterialDumper.MaterialRow row)
    {
        var sb = new StringBuilder();
        sb.Append(Csv(fbxRel)).Append(',');
        sb.Append(Csv(row.GroupName)).Append(',');
        sb.Append(Csv(row.MaterialName)).Append(',');
        sb.Append(row.Specular.ToString("F4")).Append(',');
        sb.Append(Csv(row.DiffusePath)).Append(',');
        sb.Append(Csv(row.SpecularPath)).Append(',');
        sb.Append(Csv(row.NormalPath)).Append(',');
        sb.Append(Csv(row.EmissivePath)).Append(',');
        sb.Append(row.AmbientColor.X.ToString("F3")).Append(',');
        sb.Append(row.AmbientColor.Y.ToString("F3")).Append(',');
        sb.Append(row.AmbientColor.Z.ToString("F3")).Append(',');
        sb.Append(row.DiffuseColor.X.ToString("F3")).Append(',');
        sb.Append(row.DiffuseColor.Y.ToString("F3")).Append(',');
        sb.Append(row.DiffuseColor.Z.ToString("F3")).Append(',');
        sb.Append(row.SpecularColor.X.ToString("F3")).Append(',');
        sb.Append(row.SpecularColor.Y.ToString("F3")).Append(',');
        sb.Append(row.SpecularColor.Z.ToString("F3")).Append(',');
        sb.Append(row.EmissiveColor.X.ToString("F3")).Append(',');
        sb.Append(row.EmissiveColor.Y.ToString("F3")).Append(',');
        sb.Append(row.EmissiveColor.Z.ToString("F3")).Append(',');
        sb.Append(row.Alpha.ToString("F3"));
        return sb.ToString();
    }

    static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.IndexOfAny(new[] { ',', '"', '\n' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}

/// <summary>
/// Subclass of MeshImporter to access MeshInterface's protected native FBX APIs
/// (SDMeshOpen / SDMeshGetGroup / SDMeshClose + the SdMaterial struct surface) for
/// material-only inspection. Avoids the texture-load and effect-construction
/// branches that ImportStaticMesh runs through.
/// </summary>
internal sealed class FbxMaterialDumper : MeshImporter
{
    public FbxMaterialDumper(Ship_Game.Data.GameContentManager content) : base(content) { }

    public struct MaterialRow
    {
        public string GroupName;
        public string MaterialName;
        public float  Specular;
        public string DiffusePath;
        public string SpecularPath;
        public string NormalPath;
        public string EmissivePath;
        public SDGraphics.Vector3 AmbientColor;
        public SDGraphics.Vector3 DiffuseColor;
        public SDGraphics.Vector3 SpecularColor;
        public SDGraphics.Vector3 EmissiveColor;
        public float Alpha;
    }

    // C# state machines can't carry pointer locals across `yield return`, so this
    // collects synchronously into a list and returns it eagerly.
    public unsafe List<MaterialRow> SurveyFbx(string fbxPath)
    {
        var rows = new List<MaterialRow>();
        SdMesh* mesh = SDMeshOpen(fbxPath);
        if (mesh == null)
        {
            rows.Add(new MaterialRow
            {
                GroupName = "<open-failed>",
                MaterialName = "",
                Specular = -1,
            });
            return rows;
        }

        var seen = new HashSet<long>();
        try
        {
            for (int i = 0; i < mesh->NumGroups; ++i)
            {
                SdMeshGroup* g = SDMeshGetGroup(mesh, i);
                if (g == null || g->Mat == null) continue;

                long matPtr = (long)g->Mat;
                if (!seen.Add(matPtr)) continue;  // dedupe shared materials

                SdMaterial* m = g->Mat;
                rows.Add(new MaterialRow
                {
                    GroupName     = g->Name.AsString,
                    MaterialName  = m->Name.AsString,
                    Specular      = m->Specular,
                    DiffusePath   = m->DiffusePath.AsString,
                    SpecularPath  = m->SpecularPath.AsString,
                    NormalPath    = m->NormalPath.AsString,
                    EmissivePath  = m->EmissivePath.AsString,
                    AmbientColor  = m->AmbientColor,
                    DiffuseColor  = m->DiffuseColor,
                    SpecularColor = m->SpecularColor,
                    EmissiveColor = m->EmissiveColor,
                    Alpha         = m->Alpha,
                });
            }
        }
        finally
        {
            SDMeshClose(mesh);
        }
        return rows;
    }
}
