using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Serialization
{
    public abstract class TypeSerializerMap
    {
        readonly Map<Type, TypeSerializer> Serializers = new Map<Type, TypeSerializer>();
        readonly Array<TypeSerializer> Index = new Array<TypeSerializer>();

        protected TypeSerializerMap()
        {
            Index.Resize(TypeSerializer.MaxFundamentalTypes + 1);

            Set(1, typeof(bool),   new BoolSerializer()  );
            Set(2, typeof(byte),   new ByteSerializer()  );
            Set(3, typeof(short),  new ShortSerializer() );
            Set(4, typeof(ushort), new UShortSerializer());
            Set(5, typeof(int),    new IntSerializer()   );
            Set(6, typeof(uint),   new UIntSerializer()  );
            Set(7, typeof(long),   new LongSerializer()  );
            Set(8, typeof(ulong),  new ULongSerializer() );
            Set(9, typeof(float),  new FloatSerializer() );
            Set(10, typeof(double),  new DoubleSerializer() );
            Set(11, typeof(Vector2), new Vector2Serializer());
            Set(12, typeof(Vector3), new Vector3Serializer());
            Set(13, typeof(Vector4), new Vector4Serializer());
            Set(14, typeof(Color),   new ColorSerializer()  );
            Set(15, typeof(string),  new StringSerializer() );
            Set(16, typeof(LocalizedText), new LocalizedTextSerializer());
            Set(17, typeof(Range), new RangeSerializer());
            Set(18, typeof(TimeSpan), new TimeSpanSerializer());
        }

        public abstract TypeSerializer AddUserTypeSerializer(Type type);

        // Adds a new serializer type, used during Serialization
        protected TypeSerializer Add(Type type, TypeSerializer ser)
        {
            ser.Id = (ushort)Index.Count;
            ser.Type = type;
            if (ser.Id == (ushort.MaxValue-1))
                throw new IndexOutOfRangeException($"serializer.Id overflow -- too many types: {ser.Id}");

            if (Serializers.ContainsKey(type))
                throw new InvalidOperationException($"duplicate serializer: {ser}");

            Serializers[type] = ser;
            Index.Add(ser);
            return ser;
        }

        // Can be used to overwrite default serializers, used during Deserialization
        public TypeSerializer Set(ushort id, Type type, TypeSerializer ser)
        {
            if (id >= Index.Count)
                Index.Resize(id+1);

            ser.Id = id;
            ser.Type = type;
            Serializers[type] = ser;
            Index[id] = ser;
            return ser;
        }

        static Type GetListType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Array<>) ||
                    type.GetInterfaces().Contains(typeof(IList)))
                    return type.GenericTypeArguments[0];
            }
            return null;
        }

        static (Type Key, Type Value) GetMapTypes(Type type)
        {
            if (type.IsGenericType)
            {
                var genType = type.GetGenericTypeDefinition();
                if (genType == typeof(Map<,>) ||
                    genType == typeof(IDictionary<,>))
                    return (type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            }
            return (null, null);
        }

        public TypeSerializer[] GetCustomTypes()
        {
            return Index.Filter(s => s != null && !s.IsFundamentalType);
        }

        public TypeSerializer Get(int typeId)
        {
            if (typeId < Index.Count)
            {
                TypeSerializer ser = Index[typeId];
                if (ser != null)
                    return ser;
            }
            throw new InvalidDataException($"{this} unsupported typeId={typeId}");
        }

        public TypeSerializer Get(Type type)
        {
            if (Serializers.TryGetValue(type, out TypeSerializer converter))
                return converter;
            
            if (type.IsEnum)
                return Add(type, new EnumSerializer(type));

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                return Add(type, new RawArraySerializer(elemType, Get(elemType)));
            }

            Type listElemType = GetListType(type);
            if (listElemType != null)
                return Add(type, new ArrayListSerializer(listElemType, Get(listElemType)));

            (Type key, Type value) = GetMapTypes(type);
            if (key != null)
                return Add(type, new MapSerializer(key, Get(key), value, Get(value)));

            if (type.GetCustomAttribute<StarDataTypeAttribute>() != null)
                return Add(type, AddUserTypeSerializer(type));

            // Nullable<T>, ex: `[StarData] Color? MinColor;`
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return Add(type, Get(nullableType));

            throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }
    }
}
