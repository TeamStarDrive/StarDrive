using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

// Base state for a serialized object
public class ObjectState
{
    public object Obj;
    public uint Id; // ID of this object, 0 means null
    public override string ToString() => $"ObjState {Obj.GetType().Name} Id={Id} Obj={Obj}";

    public ObjectState(object obj, uint id) { Obj = obj; Id = id; }

    // Scan for child objects
    public virtual void Scan(ObjectScanner scanner, TypeSerializer ser)
    {
        // Fundamental types don't have anything to scan
    }

    // Remaps object id-s
    public virtual void Remap(uint[] map)
    {
        Id = map[Id];
    }

    protected void Remap(uint[] map, uint[] fields)
    {
        Id = map[Id];
        if (fields != null)
        {
            for (int i = 0; i < fields.Length; ++i)
            {
                uint oldId = fields[i];
                if (oldId != 0)
                    fields[i] = map[oldId];
            }
        }
    }

    // Serialize this ObjectState into a binary writer
    public virtual void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        ser.Serialize(w, Obj);
    }
}
