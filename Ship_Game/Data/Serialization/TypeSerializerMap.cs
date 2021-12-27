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

            // WARNING: After version 1 is deployed, DO NOT
            //          CHANGE ANY SERIALIZER ID VALUES.
            //          Changing an ID will break compatibility of
            //          fundamental types. Only adding new types is allowed.
            Set(id: 1, typeof(bool),   new BoolSerializer()  );
            Set(id: 2, typeof(byte),   new ByteSerializer()  );
            Set(id: 3, typeof(sbyte),  new SByteSerializer() );
            Set(id: 4, typeof(short),  new ShortSerializer() );
            Set(id: 5, typeof(ushort), new UShortSerializer());
            Set(id: 6, typeof(int),    new IntSerializer()   );
            Set(id: 7, typeof(uint),   new UIntSerializer()  );
            Set(id: 8, typeof(long),   new LongSerializer()  );
            Set(id: 9, typeof(ulong),  new ULongSerializer() );
            Set(id: 10, typeof(float), new FloatSerializer() );
            Set(id: 11, typeof(double),  new DoubleSerializer() );
            Set(id: 12, typeof(Vector2), new Vector2Serializer());
            Set(id: 13, typeof(Vector3), new Vector3Serializer());
            Set(id: 14, typeof(Vector4), new Vector4Serializer());
            Set(id: 15, typeof(Vector2d), new Vector2dSerializer());
            Set(id: 16, typeof(Vector3d), new Vector3dSerializer());
            Set(id: 17, typeof(Point),    new PointSerializer() );
            Set(id: 18, typeof(Color),   new ColorSerializer()  );
            Set(id: 19, typeof(string),  new StringSerializer() );
            Set(id: 20, typeof(LocalizedText), new LocalizedTextSerializer());
            Set(id: 21, typeof(Range), new RangeSerializer());
            Set(id: 22, typeof(DateTime), new DateTimeSerializer());
            Set(id: 23, typeof(TimeSpan), new TimeSpanSerializer());
            // ADD new types here, up to `TypeSerializer.MaxFundamentalTypes`
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
