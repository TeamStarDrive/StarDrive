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
