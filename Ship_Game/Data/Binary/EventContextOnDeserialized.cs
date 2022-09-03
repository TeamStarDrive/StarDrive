using SDUtils;
using Ship_Game.Utils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

/// <summary>
/// Contains necessary context for calling OnDeserialized events
/// </summary>
public class EventContextOnDeserialized
{
    object Root;
    object[] Objects;

    public EventContextOnDeserialized(TypeSerializerMap typeMap, object root, object[] objects)
    {
        Root = root;
        Objects = objects;
    }

    void CallEvents(UserTypeSerializer.Deserialized deserialized, int count, int baseIndex)
    {
        for (int i = 0; i < count; ++i)
        {
            object instance = Objects[baseIndex + i];
            deserialized(instance, Root);
        }
    }

    record struct PendingEvent(TypeGroup g, UserTypeSerializer.Deserialized evt, StarDataDeserialized a);

    // Invokes events in the order the types were sorted by the Serializer
    // Except that if [StarDataOnDeserialized] defines additional dependencies via `Required`
    // some events will be reordered to satisfy the event dependency order.
    public void InvokeEvents(TypeGroup[] allGroups)
    {
        Array<PendingEvent> pending = GetPendingEvents(allGroups);


    }

    Array<PendingEvent> GetPendingEvents(TypeGroup[] allGroups)
    {
        var pending = new Array<PendingEvent>();
        foreach (var g in allGroups)
        {
            if (g.Ser is UserTypeSerializer us)
            {
                var (evt, a) = us.GetOnDeserializedEvt();
                if (evt != null)
                {
                    pending.Add(new(g, evt, a));
                    continue;
                }
            }
        }
        return pending;
    }
}
