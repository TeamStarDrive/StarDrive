using SDUtils;
using Ship_Game.Utils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

/// <summary>
/// Contains necessary context for calling OnDeserialized events
/// </summary>
public class EventContextOnDeserialized
{
    readonly object Root;
    readonly object[] Objects;
    readonly bool Verbose;

    public EventContextOnDeserialized(object root, object[] objects, bool verbose)
    {
        Root = root;
        Objects = objects;
        Verbose = verbose;
    }

    class Event
    {
        public TypeGroup Group;
        public StarDataDeserialized Attr;
        public UserTypeSerializer.Deserialized Evt;
    }

    // Invokes events in the order the types were sorted by the Serializer
    // Except that if [StarDataOnDeserialized] defines additional dependencies via `Required`
    // some events will be reordered to satisfy the event dependency order.
    public void InvokeEvents(TypeGroup[] allGroups)
    {
        Event[] events = GetEvents(allGroups);

        DependencySorter<Event>.Sort(events, e =>
        {
            // find all Event instances that match the Type's from Attr.Required
            return e.Attr.Required?.Select(t => events.Find(d => d.Group.Type.Type == t)!);
        });

        // Call the events
        foreach (Event e in events)
        {
            if (Verbose) Log.Info($"OnDeserializedEvt {e.Group.Type}");
            for (int i = 0; i < e.Group.Count; ++i)
            {
                object instance = Objects[e.Group.BaseIndex + i];
                e.Evt(instance, Root);
            }
        }
    }

    Event[] GetEvents(TypeGroup[] allGroups)
    {
        var events = new Array<Event>();
        foreach (TypeGroup g in allGroups)
        {
            if (g.Ser is UserTypeSerializer us)
            {
                var (evt, a) = us.GetOnDeserializedEvt();
                if (evt != null)
                {
                    events.Add(new Event { Group = g, Attr = a, Evt = evt });
                }
            }
        }
        return events.ToArr();
    }
}
