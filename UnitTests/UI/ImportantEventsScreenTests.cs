using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;

namespace UnitTests.UI;

[TestClass]
public class ImportantEventsScreenTests : StarDriveTest
{
    static readonly string LongText = string.Join(" ", Enumerable.Repeat("The Remnants are guardians of an ancient race.", 25));
    const string ShortText = "Cordrazine Collective has been defeated";

    public ImportantEventsScreenTests()
    {
        CreateUniverseAndPlayerEmpire();
        Universe.NotificationManager = new NotificationManager(Universe.ScreenManager, Universe);
    }

    void LogEvent(float starDate, string title, string message)
    {
        UState.StarDate = starDate;
        Universe.NotificationManager.AddNotification(new Notification { Important = true, Title = title, Message = message });
    }

    static int DetailLines(ImportantEventsScreen screen, string text)
        => 1 + Fonts.Arial12Bold.ParseTextToLines(text, screen.Details.ItemsList.ItemsHousing.W).Length;

    [TestMethod]
    public void TheNewestEntryOpensSelectedWithItsFullText()
    {
        LogEvent(1001f, "Empire Defeated", ShortText);
        LogEvent(1002f, "Remnant Story", LongText);

        var screen = new ImportantEventsScreen(Universe);
        Game.Manager.AddScreenAndLoadContent(screen);
        try
        {
            AssertEqual(LongText, screen.EventList[0].Event.Message, "newest entry first");
            Assert.IsTrue(screen.EventList[0].Selected, "the newest entry opens selected");
            Assert.IsFalse(screen.EventList[1].Selected);
            AssertGreaterThan(DetailLines(screen, LongText), 6, "setup: the text must be longer than a row");
            AssertEqual(DetailLines(screen, LongText), screen.Details.ItemsList.NumEntries,
                "the details box shows a title line and every line of the text");
        }
        finally
        {
            Game.Manager.RemoveScreen(screen);
        }
    }

    [TestMethod]
    public void SelectingAnEntryShowsItAndUnselectsThePreviousOne()
    {
        LogEvent(1001f, "Empire Defeated", ShortText);
        LogEvent(1002f, "Remnant Story", LongText);

        var screen = new ImportantEventsScreen(Universe);
        Game.Manager.AddScreenAndLoadContent(screen);
        try
        {
            screen.Select(screen.EventList[1]);

            Assert.IsTrue(screen.EventList[1].Selected);
            Assert.IsFalse(screen.EventList[0].Selected, "only one entry is selected at a time");
            AssertEqual(DetailLines(screen, ShortText), screen.Details.ItemsList.NumEntries,
                "the details box shows the selected entry only");
        }
        finally
        {
            Game.Manager.RemoveScreen(screen);
        }
    }

    [TestMethod]
    public void ALongDescriptionIsCutToFitTheRow()
    {
        LogEvent(1001f, "Remnant Story", LongText);
        var item = new ImportantEventListItem(UState.GetImportantEvents()[0]);

        string[] lines = item.FitDescription(LongText).Split('\n');
        int rowLines = (ImportantEventListItem.RowHeight - 8) / Fonts.Arial12Bold.LineSpacing;
        AssertEqual(rowLines, lines.Length, "as many lines as fit in the row");
        Assert.IsTrue(lines[lines.Length - 1].EndsWith("..."), "the cut is marked");
        foreach (string line in lines)
            AssertLessThanOrEqual(Fonts.Arial12Bold.MeasureString(line).X, 670f, "every line fits the column");

        AssertEqual(ShortText, item.FitDescription(ShortText), "a short text is not changed");
        AssertEqual("The Remnant. By destroying", item.FitDescription("The Remnant. \\n \\n By destroying"),
            "a paragraph break would waste a row line");
    }

    [TestMethod]
    public void TheEllipsisNeverPushesALinePastTheColumn()
    {
        LogEvent(1001f, "Remnant Story", LongText);
        var item = new ImportantEventListItem(UState.GetImportantEvents()[0]);

        string fullLine = "guardians";
        while (Fonts.Arial12Bold.MeasureString(fullLine + " guardians").X <= 670f)
            fullLine += " guardians";
        while (Fonts.Arial12Bold.MeasureString(fullLine + "i").X <= 670f)
            fullLine += "i";

        string cut = item.WithEllipsis(fullLine);
        Assert.IsTrue(cut.EndsWith("..."));
        AssertLessThanOrEqual(Fonts.Arial12Bold.MeasureString(cut).X, 670f, "a word is dropped to make room");
    }
}
