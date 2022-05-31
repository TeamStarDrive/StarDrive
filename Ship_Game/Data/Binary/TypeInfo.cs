using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    public class TypeInfo
    {
        public readonly ushort StreamTypeId; // TypeId in Stream
        public readonly string Name;
        public TypeSerializer Ser; // Actual serializer
        public readonly FieldInfo[] Fields;
        public readonly bool IsPointerType;
        public readonly SerializerCategory Category;

        public Type Type => Ser.Type;

        public override string ToString() => $"{Name} Id={StreamTypeId} Fields={Fields?.Length}";

        public TypeInfo(uint streamTypeId, string name, TypeSerializer s, FieldInfo[] fields,
                        bool isPointer, SerializerCategory c)
        {
            StreamTypeId = (ushort)streamTypeId;
            Name = name;
            Fields = fields;
            IsPointerType = isPointer;
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

}
