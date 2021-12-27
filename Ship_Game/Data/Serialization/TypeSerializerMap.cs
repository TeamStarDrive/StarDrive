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
        // mapping of Type to its Serializer metadata
        readonly Map<Type, TypeSerializer> Serializers = new Map<Type, TypeSerializer>();

        // flatmap of TypeSerializer.Id to TypeSerializer instances
        readonly Array<TypeSerializer> FlatMap = new Array<TypeSerializer>();

        protected TypeSerializerMap()
        {
            FlatMap.Resize(TypeSerializer.MaxFundamentalTypes);

            Set(1, typeof(bool),   new BoolSerializer()  );
            Set(2, typeof(byte),   new ByteSerializer()  );
            Set(3, typeof(sbyte),  new SByteSerializer() );
            Set(4, typeof(short),  new ShortSerializer() );
            Set(5, typeof(ushort), new UShortSerializer());
            Set(6, typeof(int),    new IntSerializer()   );
            Set(7, typeof(uint),   new UIntSerializer()  );
            Set(8, typeof(long),   new LongSerializer()  );
            Set(9, typeof(ulong),  new ULongSerializer() );
            Set(10, typeof(float), new FloatSerializer() );
            Set(11, typeof(double),  new DoubleSerializer() );
            Set(12, typeof(Vector2), new Vector2Serializer());
            Set(13, typeof(Vector3), new Vector3Serializer());
            Set(14, typeof(Vector4), new Vector4Serializer());
            Set(15, typeof(Color),   new ColorSerializer()  );
            Set(16, typeof(string),  new StringSerializer() );
            Set(17, typeof(LocalizedText), new LocalizedTextSerializer());
            Set(18, typeof(Range), new RangeSerializer());
            Set(19, typeof(TimeSpan), new TimeSpanSerializer());
        }

        TypeSerializer Set(ushort id, Type type, TypeSerializer ser)
        {
            ser.Id = id;
            ser.Type = type;
            Serializers[type] = ser;
            FlatMap[id] = ser;
            return ser;
        }

        // Adds a TypeSerializer with IsUserClass == true
        public abstract TypeSerializer AddUserTypeSerializer(Type type);

        // Adds a new serializer type, used during Serialization
        public TypeSerializer Add(Type type, TypeSerializer ser)
        {
            ser.Id = (ushort)FlatMap.Count;
            ser.Type = type;
            if (ser.Id == (ushort.MaxValue-1))
                throw new IndexOutOfRangeException($"serializer.Id overflow -- too many types: {ser.Id}");

            if (Serializers.ContainsKey(type))
                throw new InvalidOperationException($"duplicate serializer: {ser}");

            Serializers[type] = ser;
            FlatMap.Add(ser);

            if (ser is UserTypeSerializer userSer)
                userSer.ResolveTypes();
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
            return FlatMap.Filter(s => s != null && !s.IsFundamentalType);
        }

        public TypeSerializer Get(int typeId)
        {
            if (typeId < FlatMap.Count)
            {
                TypeSerializer ser = FlatMap[typeId];
                if (ser != null)
                    return ser;
            }
            throw new InvalidDataException($"{this} unsupported typeId={typeId}");
        }

        public bool TryGet(int typeId, out TypeSerializer serializer)
        {
            if (typeId < FlatMap.Count)
            {
                serializer = FlatMap[typeId];
                return true;
            }
            serializer = null;
            return false;
        }

        public bool TryGet(Type type, out TypeSerializer serializer)
        {
            return Serializers.TryGetValue(type, out serializer);
        }

        public TypeSerializer Get(Type type)
        {
            if (Serializers.TryGetValue(type, out TypeSerializer serializer))
                return serializer;
            
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
                return AddUserTypeSerializer(type);

            // Nullable<T>, ex: `[StarData] Color? MinColor;`
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return Add(type, Get(nullableType));

            throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }
    }
}
