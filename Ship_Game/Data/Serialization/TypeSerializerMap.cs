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
            Set(id: 1, new BoolSerializer());
            Set(id: 2, new ByteSerializer());
            Set(id: 3, new SByteSerializer());
            Set(id: 4, new ShortSerializer());
            Set(id: 5, new UShortSerializer());
            Set(id: 6, new IntSerializer());
            Set(id: 7, new UIntSerializer());
            Set(id: 8, new LongSerializer());
            Set(id: 9, new ULongSerializer());
            Set(id: 10, new FloatSerializer());
            Set(id: 11, new DoubleSerializer());
            Set(id: 12, new Vector2Serializer());
            Set(id: 13, new Vector3Serializer());
            Set(id: 14, new Vector4Serializer());
            Set(id: 15, new Vector2dSerializer());
            Set(id: 16, new Vector3dSerializer());
            Set(id: 17, new PointSerializer());
            Set(id: 18, new ColorSerializer());
            Set(id: 19, new StringSerializer());
            Set(id: 20, new LocalizedTextSerializer());
            Set(id: 21, new RangeSerializer());
            Set(id: 22, new DateTimeSerializer());
            Set(id: 23, new TimeSpanSerializer());
            // ADD new types here, up to `TypeSerializer.MaxFundamentalTypes`
        }

        TypeSerializer Set(ushort id, TypeSerializer ser)
        {
            ser.Id = id;
            Serializers[ser.Type] = ser;
            FlatMap[id] = ser;
            return ser;
        }

        // Adds a TypeSerializer with IsUserClass == true
        public abstract TypeSerializer AddUserTypeSerializer(Type type);

        // Adds a new serializer type, used during Serialization
        public TypeSerializer Add(TypeSerializer ser)
        {
            return Add(ser.Type, ser);
        }

        // `type` - this can be an alias for an existing serializer
        TypeSerializer Add(Type type, TypeSerializer ser)
        {
            if (type == null)
                throw new ArgumentNullException($"serializer.Type cannot be null");

            ser.Id = (ushort)FlatMap.Count;
            if (ser.Id == (ushort.MaxValue - 1))
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
                return Add(new EnumSerializer(type));

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                return Add(new RawArraySerializer(type, elemType, Get(elemType)));
            }

            Type listElemType = GetListType(type);
            if (listElemType != null)
                return Add(new ArrayListSerializer(type, listElemType, Get(listElemType)));

            (Type key, Type value) = GetMapTypes(type);
            if (key != null)
                return Add(new MapSerializer(type, key, Get(key), value, Get(value)));

            if (type.GetCustomAttribute<StarDataTypeAttribute>() != null)
                return AddUserTypeSerializer(type);

            // Nullable<T>, ex: `[StarData] Color? MinColor;`
            Type nulledType = Nullable.GetUnderlyingType(type);
            if (nulledType != null) // create an alias: Color? -> Color
                return Add(type, Get(nulledType));

            throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }
    }
}
