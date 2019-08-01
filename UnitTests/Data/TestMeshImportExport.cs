using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SgMotion;
using Ship_Game;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Rendering;

namespace UnitTests.Data
{
    [TestClass]
    public class TestMeshImportExport : StarDriveTest
    {
        public TestMeshImportExport()
        {
            CreateGameInstance();
        }

        [TestMethod]
        public void ExportMesh()
        {
            //RootContent.RawContent.ExportXnbMesh(new FileInfo("Content/Model/Ships/Opteris/ship19b.xnb"), alwaysOverwrite:true);
            //RootContent.RawContent.ExportXnbMesh(new FileInfo("Content/Model/Ships/Ralyeh/ship17a.xnb"), alwaysOverwrite:true);
            //RootContent.RawContent.ExportAllXnbMeshes();

            var exporter = new MeshExporter(Content);
            
            SkinnedModel skinned = Content.LoadSkinnedModel("Model/Ships/Ralyeh/ship17b.xnb");
            exporter.Export(skinned, "ship17b", "MeshExport/Model/Ships/Ralyeh/ship17b.fbx");
        }

        [TestMethod]
        public void ImportMesh()
        {
            var importer = new MeshImporter(Content);
            //StaticMesh mesh = importer.Import("Content/Model/Ships", "");
        }
    }
}
