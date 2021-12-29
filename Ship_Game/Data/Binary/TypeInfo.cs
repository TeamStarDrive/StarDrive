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
        public ushort StreamTypeId; // TypeId in Stream
        public TypeSerializer Ser; // Actual serializer
        public FieldInfo[] Fields;

        public Type Type => Ser.Type;

        public TypeInfo(ushort streamTypeId, TypeSerializer s, FieldInfo[] fields)
        {
            StreamTypeId = streamTypeId;
            Ser = s;
            Fields = fields;
            if (fields != null && s is UserTypeSerializer us)
            {
                for (uint fieldIdx = 0; fieldIdx < fields.Length; ++fieldIdx)
                {
                    FieldInfo f = fields[fieldIdx];
                    f.Field = us.GetFieldOrNull(f.Name);
                    if (f.Field != null)
                        f.Ser = f.Field.Serializer;
                }
            }
        }
    }

}
