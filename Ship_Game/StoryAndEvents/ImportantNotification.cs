using Ship_Game.Data.Serialization;

namespace Ship_Game;

// A permanent log entry for notifications marked Important
// (empire defeat, merge/surrender, remnant story progression).
// Viewable in the ImportantEventsScreen via the minimap button.
[StarDataType]
public sealed class ImportantNotification
{
    [StarData] public readonly float StarDate;
    [StarData] public readonly string Title;
    [StarData] public readonly string Message;
    [StarData] public readonly Empire RelevantEmpire;
    [StarData] public readonly string IconPath;

    [StarDataConstructor]
    ImportantNotification() {}

    public ImportantNotification(float starDate, Notification n)
    {
        StarDate       = starDate;
        Title          = n.Title ?? "";
        Message        = n.LogMessage ?? n.Message ?? "";
        RelevantEmpire = n.RelevantEmpire;
        IconPath       = n.IconPath;
    }
}
