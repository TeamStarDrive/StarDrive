using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using System.IO;

namespace UnitTests.Serialization;

[TestClass]
public class SerializationRegressionTests : StarDriveTest
{
    [TestMethod]
    public void RaceSaveScreen_CanSaveAndLoad()
    {
        var save = new RaceSave()
        {
            Name = "Race Save",
            ModName = "MyMod",
            ModPath = "Mods/MyMod/",
            Traits = ResourceManager.MajorRaces[0].Traits.GetClone(),
        };
        string path = Path.GetTempFileName();
        SaveRaceScreen.Save(path, save);
        RaceSave load = SaveRaceScreen.Load(new(path));
        AssertMemberwiseEqual(save, load, "Expected saved RaceSave to equal loaded RaceSave");
    }

    [TestMethod]
    public void SaveNewGameSetupScreen_CanSaveAndLoad()
    {
        var save = new SetupSave(new())
        {
            Name = "New saved setup",
            ModName = "MyMod",
            ModPath = "Mods/MyMod/",
        };
        string path = Path.GetTempFileName();
        SaveNewGameSetupScreen.Save(path, save);
        SetupSave load = SaveNewGameSetupScreen.Load(new(path));
        AssertMemberwiseEqual(save, load, "Expected saved SetupSave to equal loaded SetupSave");
    }

    static string SavedFile(SetupSave save)
    {
        string path = Path.GetTempFileName();
        SaveNewGameSetupScreen.Save(path, save);
        return path;
    }

    static SetupSave SavedAndLoaded(SetupSave save) => SaveNewGameSetupScreen.Load(new(SavedFile(save)));

    static T WithSalvageOpsFlag<T>(bool on, Func<T> get)
    {
        bool flag = GlobalStats.Defaults.EnableSalvageOps;
        try
        {
            GlobalStats.Defaults.EnableSalvageOps = on;
            return get();
        }
        finally
        {
            GlobalStats.Defaults.EnableSalvageOps = flag;
        }
    }

    [TestMethod]
    public void ASetupSavedOnPatch00071StartsSalvageOpsFromTheGlobalsFlag()
    {
        string path = SavedFile(new SetupSave(new() { DisableSalvageOps = true }) { Name = "Saved on 00071" });
        string[] lines = File.ReadAllLines(path);
        string[] withoutMark = lines.Where(line => !line.Contains("SalvageOpsChosen")).ToArray();
        AssertEqual(lines.Length - 1, withoutMark.Length, "setup: the file loses its one mark line, as one saved on 00071 has none");
        File.WriteAllLines(path, withoutMark);

        SetupSave load = SaveNewGameSetupScreen.Load(new(path));
        AssertTrue(load.Settings.DisableSalvageOps, "setup: the setup holds the box ticked");
        AssertFalse(load.SettingsToStart().DisableSalvageOps, "a new game from it has salvage ops on, as the Globals flag does");
    }

    [TestMethod]
    public void ASetupKeepsTheSalvageOpsBoxThePlayerTicked()
    {
        var save = new SetupSave(new() { DisableSalvageOps = true }) { Name = "Ticked by the player" };
        AssertTrue(save.SalvageOpsChosen, "setup: with salvage ops on in Globals the box is the player's");
        AssertTrue(SavedAndLoaded(save).SettingsToStart().DisableSalvageOps, "a new game from it keeps salvage ops off");
    }

    [TestMethod]
    public void ASetupSavedWhileTheGlobalsFlagWasOffLeavesSalvageOpsToTheFlag()
    {
        SetupSave save = WithSalvageOpsFlag(false, () => new SetupSave(new()) { Name = "Saved with no salvage content" });
        AssertTrue(save.Settings.DisableSalvageOps, "setup: with the flag off the box was locked ticked");
        AssertFalse(SavedAndLoaded(save).SettingsToStart().DisableSalvageOps, "once the flag is on, a new game from it has salvage ops on");
    }

    [TestMethod]
    public void WhileTheGlobalsFlagIsOffEverySetupStartsWithTheSalvageOpsBoxTicked()
    {
        SetupSave load = SavedAndLoaded(new SetupSave(new() { DisableSalvageOps = false }) { Name = "Unticked by the player" });
        AssertTrue(load.SalvageOpsChosen, "setup: the box was the player's");
        AssertFalse(load.Settings.DisableSalvageOps, "setup: the player unticked it");
        AssertTrue(WithSalvageOpsFlag(false, () => load.SettingsToStart().DisableSalvageOps),
                   "loaded in a mod with the flag off, as a vanilla setup can be, the box is ticked");
    }

    // The save list draws the player's flag straight from the header, using FlagIndex == -1
    // to mean "written before the field existed, fall back to the race-name lookup".
    // Flag 0 is a real, pickable flag and must survive as 0, never as the sentinel.
    // Note this passes with or without the DefaultValue attribute, because HeaderData
    // always serializes under full layout; it guards the round trip, not the attribute.
    [TestMethod]
    public void SaveHeader_FlagIndexZero_SurvivesRoundTrip()
    {
        foreach (int flagIndex in new[] { 0, -1, 1, 7 })
        {
            var header = new HeaderData
            {
                Version     = SavedGame.SaveGameVersion,
                SaveName    = "HeaderRoundTrip",
                StarDate    = "1000.0",
                PlayerName  = "A Renamed Race",
                RealDate    = "1/1/2026 12:00 AM",
                ModName     = "",
                Time        = new System.DateTime(2026, 1, 1),
                FlagIndex   = flagIndex,
                EmpireColor = new Microsoft.Xna.Framework.Color(68, 140, 203, 255),
            };

            string path = Path.GetTempFileName();
            using (var w = new Ship_Game.Data.Binary.Writer(new FileStream(path, FileMode.Create)))
                Ship_Game.Data.Binary.BinarySerializer.SerializeMultiType(w, new object[] { header }, false);

            HeaderData load = Ship_Game.GameScreens.LoadGame.LoadGame.PeekHeader(new(path));
            // PeekHeader swallows every exception and returns null, so assert before deref
            Assert.IsNotNull(load, $"PeekHeader returned null for FlagIndex {flagIndex}");
            AssertEqual(flagIndex, load.FlagIndex, $"FlagIndex {flagIndex} must round-trip exactly");
            AssertEqual(header.EmpireColor, load.EmpireColor, "EmpireColor must round-trip exactly");
        }
    }
}
