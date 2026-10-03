using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.Data;

// Only official builds (exactly Major.Minor.Patch) may report to Sentry;
// forked builds tag extra version parts and must be filtered out.
[TestClass]
public class VersionFormatTests
{
    [TestMethod]
    public void OfficialReleaseFormatsAreAccepted()
    {
        Assert.IsTrue(Log.IsOfficialVersionFormat("1.60.00046 release/jupiter-1.60/f83ab4a"));
        Assert.IsTrue(Log.IsOfficialVersionFormat("1.60.00046"));
        Assert.IsTrue(Log.IsOfficialVersionFormat("1.51.15120 release/mars-1.51/abc1234"));
    }

    [TestMethod]
    public void ForkedAndMalformedFormatsAreRejected()
    {
        Assert.IsFalse(Log.IsOfficialVersionFormat("1.60.00046.123 fork/jupiter-1.60/abc1234"));
        Assert.IsFalse(Log.IsOfficialVersionFormat("1.60.00046.1"));
        Assert.IsFalse(Log.IsOfficialVersionFormat("1.60"));
        Assert.IsFalse(Log.IsOfficialVersionFormat("1.60.dev"));
        Assert.IsFalse(Log.IsOfficialVersionFormat("1.60.-46"));
        Assert.IsFalse(Log.IsOfficialVersionFormat(""));
    }
}
