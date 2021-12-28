using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Serialization
{
    public abstract class CollectionSerializer : TypeSerializer
    {
        protected readonly Type ElemType;
        protected readonly TypeSerializer ElemSerializer;

        protected CollectionSerializer(Type type, Type elemType, TypeSerializer elemSerializer) : base(type)
        {
            IsCollection = true;
            ElemType = elemType;
            ElemSerializer = elemSerializer;
        }
    }
}
