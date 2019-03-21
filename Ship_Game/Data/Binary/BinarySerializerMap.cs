using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    class BinarySerializerMap : TypeSerializerMap
    {
        public BinarySerializerMap()
        {
        }

        protected override TypeSerializer AddUserTypeSerializer(Type type)
        {
            return new BinarySerializer(type);
        }
    }
}
