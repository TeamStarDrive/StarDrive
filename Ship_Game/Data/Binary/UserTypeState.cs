using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

public class UserTypeState : ObjectState
{
    public uint[] Fields;
    // if set != 0 by Scan(), this object will be Serialized partially
    public uint NumPartialSerializeFields;

    public UserTypeState(object obj, uint id) : base(obj, id)
    {
    }

    public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        uint numPartialFields = NumPartialSerializeFields;
        w.BW.WriteVLu32(numPartialFields); // [numPartialFields]

        if (numPartialFields > 0u)
        {
            for (int fieldIdx = 0; fieldIdx < Fields.Length; ++fieldIdx)
            {
                uint fieldPointer = Fields[fieldIdx];
                if (fieldPointer != 0)
                {
                    w.BW.WriteVLu32((uint)fieldIdx); // [fieldIdx]
                    w.BW.WriteVLu32(fieldPointer);   // [fieldPointer]
                }
            }
        }
        else
        {
            for (int fieldIdx = 0; fieldIdx < Fields.Length; ++fieldIdx)
            {
                w.BW.WriteVLu32(Fields[fieldIdx]); // [fieldPointer]

                if (ser.TypeName.Contains("UniverseState"))
                {
                    var us = (UserTypeSerializer)ser;
                    Log.Info($"UniverseState [{fieldIdx}]={Fields[fieldIdx]} {us.Fields[fieldIdx].Name}");
                }
            }
        }
    }

    public override void Remap(uint[] map)
    {
        Remap(map, Fields);
    }

    public override void Scan(ObjectScanner scanner, TypeSerializer ser)
    {
        var user = (UserTypeSerializer)ser;

        // get dynamic fields
        StarDataDynamicField[] dynamicF = user.InvokeOnSerializeEvt(Obj);

        // the # of fields remains constant because we rely on predefined object layout
        Fields = user.Fields.Length > 0 ? new uint[user.Fields.Length] : Empty<uint>.Array;

        for (int i = 0; i < user.Fields.Length; ++i)
        {
            DataField field = user.Fields[i];
            // HOTSPOT, some PROPERTIES can also perform computations here
            object obj = field.Get(Obj);
            uint fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
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
                uint fieldObjectId = scanner.ScanObjectState(field.Serializer, dynF.Value);
                Fields[fieldIdx] = fieldObjectId;
            }
        }
    }
}
