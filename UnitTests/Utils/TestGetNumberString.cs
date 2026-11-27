using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Utils;

// Locks the format contract of HelperFunctions.GetNumberString so UI labels
// (research bar, money leech, ship stats, etc.) stay consistent.
[TestClass]
public class TestGetNumberString
{
    [TestMethod]
    [DataRow(0f, "0")]
    [DataRow(0.25f, "0.25")]
    [DataRow(95.75f, "95.75")]
    [DataRow(100f, "100")]
    [DataRow(950.7f, "950.7")]
    [DataRow(999f, "999")]
    [DataRow(1000f, "1000")]
    [DataRow(9500f, "9500")]
    [DataRow(10000f, "10k")]
    [DataRow(57750f, "57.75k")]
    [DataRow(950700f, "950.7k")]
    [DataRow(1_000_000f, "1M")]
    [DataRow(1_500_000f, "1.5M")]
    [DataRow(100_000_000f, "100M")]
    [DataRow(950_700_000f, "950.7M")]
    [DataRow(1_000_000_000f, "1B")]
    [DataRow(2_500_000_000f, "2.5B")]
    [DataRow(950_700_000_000f, "950.7B")]
    public void FormatsExpectedString(float value, string expected)
    {
        Assert.AreEqual(expected, value.GetNumberString());
    }

    [TestMethod]
    [DataRow(-0.25f, "-0.25")]
    [DataRow(-9500f, "-9500")]
    [DataRow(-57750f, "-57.75k")]
    [DataRow(-1_500_000f, "-1.5M")]
    [DataRow(-2_500_000_000f, "-2.5B")]
    public void FormatsNegativeValues(float value, string expected)
    {
        Assert.AreEqual(expected, value.GetNumberString());
    }

    [TestMethod]
    [DataRow(57750f, "57.8k")]
    [DataRow(-57750f, "-57.8k")]
    [DataRow(950700f, "950.7k")]
    [DataRow(1_250_000f, "1.3M")]
    [DataRow(950_700_000f, "950.7M")]
    [DataRow(1_250_000_000f, "1.3B")]
    public void CompactDropsSecondDecimalInKMAndBBands(float value, string expected)
    {
        Assert.AreEqual(expected, value.GetNumberString(compact: true));
    }
}
