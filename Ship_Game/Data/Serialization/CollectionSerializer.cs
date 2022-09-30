using System;
using Ship_Game.Data.Binary;

namespace Ship_Game.Data.Serialization
{
    public abstract class CollectionSerializer : TypeSerializer
    {
        public readonly Type ElemType;
        public readonly TypeSerializer ElemSerializer;

        protected CollectionSerializer(Type type, Type elemType, TypeSerializer elemSerializer) : base(type)
        {
            IsCollection = true;
            Category = SerializerCategory.Collection;
            ElemType = elemType;
            ElemSerializer = elemSerializer;
        }

        /// <summary>
        /// Get number of elements in a collection instance
        /// </summary>
        public abstract int Count(object instance);

        /// <summary>
        /// Get an element from a collection instance
        /// </summary>
        public abstract object GetElementAt(object instance, int index);

        /// <summary>
        /// Collections only: Deserialize into an existing object instance
        /// </summary>
        public abstract void Deserialize(BinarySerializerReader reader, object instance);
    }
}
