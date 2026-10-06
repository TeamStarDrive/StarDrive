using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace UnitTests.Ships;

[TestClass]
public class HullSaveTests : StarDriveTest
{
    public HullSaveTests()
    {
        LoadStarterShips("Vulcan Scout");
    }

    [TestMethod]
    public void AModHullIsSavedBackToItsOwnFile()
    {
        ShipHull hull = ResourceManager.Hulls[0];
        FileInfo vanillaSource = hull.Source;
        try
        {
            hull.Source = new FileInfo($"Mods/ZzMod/Hulls/{hull.HullName}.hull");
            Assert.AreEqual(hull.Source.FullName, ShipDesignScreen.HullSaveFile(hull.HullName).FullName);
        }
        finally
        {
            hull.Source = vanillaSource;
        }
    }

    [TestMethod]
    public void SavingUnderAnExistingHullsNameOverwritesThatHull()
    {
        ShipHull hull = ResourceManager.Hulls.FirstOrDefault(h => h.Source != null && h.HullName != $"{h.Style}/{h.VisibleName}"
                            && ResourceManager.Hulls.Count(o => o.Style == h.Style && o.VisibleName == h.VisibleName) == 1);
        Assert.IsNotNull(hull, "setup: a hull whose file name differs from its display name");

        ShipHull edited = hull.GetClone();
        edited.HullName = $"{hull.Style}/ZzEdited";
        edited.VisibleName = "ZzEdited";
        string hullName = ShipDesignScreen.HullSaveName(edited, hull.VisibleName);
        Assert.AreEqual(hull.HullName, hullName, "the save keeps the existing hull's name, so its designs still find it");
        Assert.AreEqual(hull.Source.FullName, ShipDesignScreen.HullSaveFile(hullName).FullName, "the save writes the existing hull's file");
    }

    [TestMethod]
    public void SavingTheEditedHullUnderItsOwnNameKeepsItsFileWhenAnotherHullSharesTheName()
    {
        ShipHull[] namesakes = ResourceManager.Hulls.GroupBy(h => (h.Style, h.VisibleName))
                                              .FirstOrDefault(g => g.Count() > 1)?.ToArray();
        Assert.IsNotNull(namesakes, "setup: two hulls of one style with the same display name (vanilla Terran 'Heavy Gunboat')");
        ShipHull edited = namesakes[1];
        Assert.AreEqual(edited.HullName, ShipDesignScreen.HullSaveName(edited.GetClone(), edited.VisibleName),
                        $"'{edited.HullName}' must not be saved over '{namesakes[0].HullName}'");
    }

    [TestMethod]
    public void ANameUsedOnlyByAnotherStylesHullMakesANewHull()
    {
        ShipHull hull = ResourceManager.Hulls[0];
        ShipHull edited = hull.GetClone();
        edited.Style = "ZzStyle";
        edited.VisibleName = "ZzOther";
        Assert.AreEqual($"ZzStyle/{hull.VisibleName}", ShipDesignScreen.HullSaveName(edited, hull.VisibleName));
    }

    [TestMethod]
    public void ANameThatIsAnotherHullsKeyIsThatHull()
    {
        ShipHull hull = ResourceManager.Hulls.FirstOrDefault(h => h.HullName.StartsWith(h.Style + "/")
                            && h.HullName != $"{h.Style}/{h.VisibleName}"
                            && !h.HullName.Substring(h.Style.Length + 1).Contains('/')
                            && !ResourceManager.Hulls.Any(o => o.Style == h.Style && o.VisibleName == h.HullName.Substring(h.Style.Length + 1)));
        Assert.IsNotNull(hull, "setup: a hull whose key is no hull's display name (vanilla has several, e.g. Remnant/Mothership)");
        string typed = hull.HullName.Substring(hull.Style.Length + 1);
        ShipHull edited = hull.GetClone();
        edited.HullName = $"{hull.Style}/ZzEdited";
        edited.VisibleName = "ZzEdited";

        Assert.AreSame(hull, ShipDesignScreen.ExistingHull(edited, typed), "the save must ask before it overwrites this hull");
        Assert.IsNull(ShipDesignScreen.HullNameProblem(edited, typed));
    }

    [TestMethod]
    public void ANewHullCannotTakeAnotherHullsFileName()
    {
        ShipHull hull = ResourceManager.Hulls.First(h => h.Source != null);
        ShipHull edited = hull.GetClone();
        edited.Style = "ZzStyle";
        edited.HullName = "ZzStyle/ZzEdited";
        edited.VisibleName = "ZzEdited";
        string typed = Path.GetFileNameWithoutExtension(hull.Source.Name);

        Assert.IsNull(ShipDesignScreen.ExistingHull(edited, typed), "setup: no ZzStyle hull has that name");
        Assert.IsNotNull(ShipDesignScreen.HullNameProblem(edited, typed), "hulls load by file name, so the new file would hide the old hull");
    }

    [TestMethod]
    public void AHullNameMustBeAValidFileName()
    {
        ShipHull edited = ResourceManager.Hulls[0].GetClone();
        Assert.IsNotNull(ShipDesignScreen.HullNameProblem(edited, "Zz?"));
        Assert.IsNull(ShipDesignScreen.HullNameProblem(edited, "ZzUnusedName"));
    }

    [TestMethod]
    public void AnExistingHullIsSavedInPlaceEvenIfItsNameIsNoFileName()
    {
        ShipHull edited = ResourceManager.Hulls[0].GetClone();
        edited.VisibleName = "Zz Style/Zz Name"; // Star Trek's Torotha is shown as "Orion Syndicate/Torotha"
        Assert.IsNull(ShipDesignScreen.HullNameProblem(edited, edited.VisibleName));
    }

    [TestMethod]
    public void ANewHullIsSavedToTheActiveModsHullsFolder()
    {
        Assert.AreEqual(new FileInfo("Content/Hulls/ZzStyle/ZzNew.hull").FullName,
                        ShipDesignScreen.HullSaveFile("ZzStyle/ZzNew").FullName);
        try
        {
            GlobalStats.ActiveMod = new ModEntry(new GamePlayGlobals { Mod = new ModInformation { Name = "ZzMod" } });
            GlobalStats.ModPath = "Mods/ZzMod/";
            Assert.AreEqual(new FileInfo("Mods/ZzMod/Hulls/ZzStyle/ZzNew.hull").FullName,
                            ShipDesignScreen.HullSaveFile("ZzStyle/ZzNew").FullName);
        }
        finally
        {
            GlobalStats.SetActiveModNoSave(null);
        }
    }

    [TestMethod]
    public void APositionRoundedToNegativeZeroIsSavedAsZero()
    {
        var thruster = new ShipHull.ThrusterZone { Position = new Vector3(0, 0, 2), Scale = 32 };
        thruster.SetWorldPos2D(new Vector2(-0.3f, -0.4f));
        Assert.IsTrue(float.IsNegative(thruster.Position.X), "setup: the editor rounds a small negative position to -0");

        ShipHull hull = ResourceManager.Hulls[0].GetClone();
        hull.MeshOffset = new Vector2(-0f, 3);
        hull.Thrusters = new[] { thruster };
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"HullSaveTests_{Guid.NewGuid():N}.hull"));
        try
        {
            hull.Save(file);
            string text = File.ReadAllText(file.FullName);
            StringAssert.Contains(text, "MeshOffset=0,3");
            StringAssert.Contains(text, "Thruster=0,0,2,32");
        }
        finally
        {
            file.Delete();
        }
    }

    [TestMethod]
    public void SavingAHullCreatesItsStyleFolder()
    {
        string root = Path.Combine(Path.GetTempPath(), "HullSaveTests_" + Guid.NewGuid().ToString("N"));
        var file = new FileInfo(Path.Combine(root, "ZzStyle", "ZzNew.hull"));
        try
        {
            ResourceManager.Hulls[0].GetClone().Save(file);
            Assert.IsTrue(File.Exists(file.FullName));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
