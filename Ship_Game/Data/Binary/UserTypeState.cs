using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

public class UserTypeState : ObjectState
{
    public int[] Fields;

    public UserTypeState(object obj, int id) : base(obj, id)
    {
    }

    public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        for (int i = 0; i < Fields.Length; ++i)
            w.BW.WriteVLu32((uint)Fields[i]);
    }

    public override void Remap(int[] map)
    {
        Remap(map, Fields);
    }

    public override void Scan(RecursiveScanner scanner, TypeSerializer ser)
    {
        var user = (UserTypeSerializer)ser;

        // get dynamic fields
        StarDataDynamicField[] dynamicF = user.InvokeOnSerializeEvt(Obj);

        // the # of fields remains constant because we rely on predefined object layout
        Fields = user.Fields.Length > 0 ? new int[user.Fields.Length] : Empty<int>.Array;

        for (int i = 0; i < user.Fields.Length; ++i)
        {
            DataField field = user.Fields[i];
            // HOTSPOT, some PROPERTIES can also perform computations here
            object obj = field.Get(Obj);
            int fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
            Fields[i] = fieldObjectId;
        }

        // dynamic fields override existing fields
        // TODO: instead of override, prevent original value from being scanned and written
        if (dynamicF != null)
        {
            for (int i = 0; i < dynamicF.Length; ++i)
            {
                StarDataDynamicField dynF = dynamicF[i];
                int fieldIdx = user.Fields.IndexOf(f => f.Name == dynF.Name);
                if (fieldIdx == -1)
                    throw new($"StarDataDynamicField: Could not find a [StarData] field with Name=`{dynF.Name}`");

                // and now replace the current object
                DataField field = user.Fields[fieldIdx];
                int fieldObjectId = scanner.ScanObjectState(field.Serializer, dynF.Value);
                Fields[fieldIdx] = fieldObjectId;
            }
        }
    }
}
