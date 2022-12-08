using System;
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

        public override string ToString()
        {
            return $"OnDeserializedEvt {Group.Type.Name}";
        }
    }

    // Invokes events in the order the types were sorted by the Serializer
    // Except that if [StarDataOnDeserialized] defines additional dependencies via `Required`
    // some events will be reordered to satisfy the event dependency order.
    public void InvokeEvents(TypeGroup[] allGroups)
    {
        Event[] events = GetEvents(allGroups);

        Map<Event, Array<Event>> evtRequirements = new();
        foreach (Event e in events)
            evtRequirements[e] = new();

        // build event requirements for each event type
        foreach (Event e in events)
        {
            if (e.Attr.Required != null) // first add all straight up requirements
            {
                foreach (Type requiredType in e.Attr.Required)
                {
                    Event weRequire = events.Find(d => d.Group.Type.Type == requiredType)!;
                    evtRequirements[e].AddUniqueRef(weRequire);
                }
            }

            // then update all `DeserializeBefore` events to Require this `e`
            if (e.Attr.DeserializeBefore != null)
            {
                foreach (Type requiredType in e.Attr.DeserializeBefore)
                {
                    Event requiresUs = events.Find(d => d.Group.Type.Type == requiredType)!;
                    evtRequirements[requiresUs].AddUniqueRef(e);
                }
            }
        }

        DependencySorter<Event>.Sort(events, e => evtRequirements[e].ToArr());

        // Call the events
        foreach (Event e in events)
        {
            if (Verbose) Log.Info($"OnDeserializedEvt {e.Group.Type}");
            for (int i = 0; i < e.Group.Count; ++i)
            {
                object instance = Objects[e.Group.BaseId + i];
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
