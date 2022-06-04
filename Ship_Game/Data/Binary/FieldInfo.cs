using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using SDUtils;

namespace Ship_Game.Data.Binary
{
    public class FieldInfo
    {
        public ushort StreamTypeId; // TypeId in Stream
        public DataField Field;
        public TypeSerializer Ser; // Actual serializer
        public string Name;
        public Type Type => Ser.Type;

        public override string ToString()
        {
            return $"{StreamTypeId} {Type?.GetTypeName()} {Name}";
        }
    }

}
