using System;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary;

public class TypeInfo
{
    // TypeId in Stream
    public readonly ushort StreamTypeId;
    public readonly string Name;

    // Actual serializer, can be null if type is deleted
    public TypeSerializer Ser;

    // for UserClasses, these are all the STREAM fields
    // ordered as they appear in the stream, eg Fields[0] == streamFieldIdx(0)
    public readonly FieldInfo[] Fields;
    public readonly bool IsStruct;
    public readonly SerializerCategory Category;

    public Type Type => Ser.Type;

    public override string ToString() => $"{StreamTypeId}:{Name}{FieldString}";

    string FieldString => Fields != null ? $" Fields={Fields.Length}" : "";

    public TypeInfo(uint streamTypeId, string name, TypeSerializer s, FieldInfo[] fields,
                    bool isStruct, SerializerCategory c)
    {
        StreamTypeId = (ushort)streamTypeId;
        Name = name;
        Fields = fields;
        IsStruct = s?.IsStruct ?? isStruct;
        Category = c;
        Ser = s;

        if (Fields != null && s is UserTypeSerializer us)
        {
            for (uint fieldIdx = 0; fieldIdx < Fields.Length; ++fieldIdx)
            {
                FieldInfo f = Fields[fieldIdx];
                f.Field = us.GetFieldOrNull(f.Name);
                if (f.Field != null)
                    f.Ser = f.Field.Serializer;
            }
        }
    }
}

