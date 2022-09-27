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
        int numFields = user.Fields.Length;
        Fields = numFields > 0 ? new uint[numFields] : Empty<uint>.Array;

        int numDefaults = 0;
        int fullLayoutSize = 0;

        for (int fieldIdx = 0; fieldIdx < user.Fields.Length; ++fieldIdx)
        {
            DataField field = user.Fields[fieldIdx];
            // HOTSPOT, some PROPERTIES can also perform computations here
            object obj = field.Get(Obj);

            uint fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
            if (fieldObjectId == 0) ++numDefaults;
            fullLayoutSize += Writer.PredictVLuSize(fieldObjectId);

            Fields[fieldIdx] = fieldObjectId;
        }

        // dynamic fields override existing fields
        // TODO: instead of override, prevent original value from being scanned and written
        if (dynamicF != null)
        {
            foreach (StarDataDynamicField dynF in dynamicF)
            {
                int fieldIdx = user.Fields.IndexOf(f => f.Name == dynF.Name);
                if (fieldIdx == -1)
                    throw new($"StarDataDynamicField: Could not find a [StarData] field with Name=`{dynF.Name}`");

                // undo stats for previous value
                uint oldObjectId = Fields[fieldIdx];
                if (oldObjectId == 0) --numDefaults;
                fullLayoutSize -= Writer.PredictVLuSize(oldObjectId);

                // and now replace the current object
                DataField field = user.Fields[fieldIdx];
                object obj = dynF.Value;

                uint fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
                if (fieldObjectId == 0) ++numDefaults;
                fullLayoutSize += Writer.PredictVLuSize(fieldObjectId);

                Fields[fieldIdx] = fieldObjectId;
            }
        }

        // in order to figure out whether we should use FullLayout or PartialLayout,
        // we calculate the partial layout size and see if it's even 1 byte smaller
        // this is because every byte counts over network transfers
        if (numDefaults > 0) // do we have default fields?
        {
            // default fields are always 0 (1 byte), so just subtract from full layout size
            int validFields = (numFields - numDefaults);
            int partialLayoutSize = validFields + (fullLayoutSize - numDefaults);
            if (partialLayoutSize < fullLayoutSize)
            {
                NumPartialSerializeFields = (uint)validFields;
            }
        }
    }
}
