using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using UnitTests.Serialization;

namespace UnitTests.Technologies;

[TestClass]
public class TechBonusIsolationTests : StarDriveTest
{
    public TechBonusIsolationTests()
    {
        CreateUniverseAndPlayerEmpire();
    }

    static float KineticDamage(Empire e) => e.data.WeaponTags[WeaponTag.Kinetic].Damage;
    static int CorvetteLevels(Empire e) => e.data.RoleLevels[(int)RoleName.corvette - 1];

    void UnlockWeaponAndLevelBonuses(Empire e)
    {
        e.UnlockTech("Plasma Ordnance", TechUnlockType.Normal);
        e.UnlockTech("Ace Training", TechUnlockType.Normal);
    }

    [TestMethod]
    public void ANewGameDoesNotStartWithTheLastGamesTechBonuses()
    {
        float damageAtStart = KineticDamage(Player);
        int levelsAtStart = CorvetteLevels(Player);
        UnlockWeaponAndLevelBonuses(Player);
        Assert.IsTrue(KineticDamage(Player) > damageAtStart, "setup: Plasma Ordnance must raise kinetic damage");
        Assert.IsTrue(CorvetteLevels(Player) > levelsAtStart, "setup: Ace Training must raise corvette levels");

        CreateUniverseAndPlayerEmpire();

        AssertEqual(0.0001f, damageAtStart, KineticDamage(Player), "a new game of the same race must not start with the last game's weapon bonuses");
        Assert.AreEqual(levelsAtStart, CorvetteLevels(Player), "a new game of the same race must not start with the last game's ship levels");
    }

    [TestMethod]
    public void RebelsDoNotShareTheirParentsTechBonuses()
    {
        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        float rebelDamage = KineticDamage(rebels);
        int rebelLevels = CorvetteLevels(rebels);

        UnlockWeaponAndLevelBonuses(Player);
        Assert.IsTrue(KineticDamage(Player) > rebelDamage, "setup: Plasma Ordnance must raise the parent's kinetic damage");
        Assert.IsTrue(CorvetteLevels(Player) > rebelLevels, "setup: Ace Training must raise the parent's corvette levels");

        AssertEqual(0.0001f, rebelDamage, KineticDamage(rebels), "the parent's research must not raise its rebels' weapon bonuses");
        Assert.AreEqual(rebelLevels, CorvetteLevels(rebels), "the parent's research must not raise its rebels' ship levels");
    }

    [StarDataType]
    class ParentAndRebels
    {
        [StarData] public EmpireData Parent;
        [StarData] public EmpireData Rebels;
    }

    [TestMethod]
    public void LoadingASaveSeparatesBonusesRebelsSharedWithTheirParent()
    {
        UnlockWeaponAndLevelBonuses(Player);
        Empire rebels = UState.CreateRebelsFromEmpireData(Player.data, Player);
        rebels.data.WeaponTags = Player.data.WeaponTags;
        rebels.data.RoleLevels = Player.data.RoleLevels;

        ParentAndRebels loaded = BinarySerializerTests.SerDes(new ParentAndRebels { Parent = Player.data, Rebels = rebels.data });

        Assert.AreNotSame(loaded.Parent.WeaponTags[WeaponTag.Kinetic], loaded.Rebels.WeaponTags[WeaponTag.Kinetic],
                          "a loaded save must give the rebels their own weapon bonuses");
        Assert.AreNotSame(loaded.Parent.RoleLevels, loaded.Rebels.RoleLevels, "a loaded save must give the rebels their own ship levels");
        AssertEqual(0.0001f, KineticDamage(Player), loaded.Rebels.WeaponTags[WeaponTag.Kinetic].Damage,
                    "the rebels keep the weapon bonuses they had when the game was saved");
        Assert.AreEqual(CorvetteLevels(Player), loaded.Rebels.RoleLevels[(int)RoleName.corvette - 1],
                        "the rebels keep the ship levels they had when the game was saved");
    }
}
