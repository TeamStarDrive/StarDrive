using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializer : UserTypeSerializer
    {
        public override string ToString() => $"BinarySerializer {TheType.GenericName()}";

        public BinarySerializer(Type type) : base(type)
        {
        }

        protected override TypeSerializerMap CreateTypeMap()
        {
            return new BinarySerializerMap();
        }

        void BuildObjectMap()
        {
            ResolveTypes();


        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            if (Mapping == null)
            {
                BuildObjectMap();
            }
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            return null;
        }
    }
}
