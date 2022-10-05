using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SDUtils;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Serialization;

public abstract class TypeSerializerMap
{
    // mapping of Type to its Serializer metadata
    readonly Map<Type, TypeSerializer> Serializers = new();

    // flatmap of TypeSerializer.TypeId to TypeSerializer instances
    readonly Array<TypeSerializer> FlatMap = new();

    // all types, including Fundamental Types
    public TypeSerializer[] AllTypes => FlatMap.Filter(s => s != null);

    public int NumTypes => FlatMap.Count;
    public int MaxTypeId => FlatMap.Count - 1;

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
        Set(id: 18, new RectangleSerializer());
        Set(id: 19, new RectFSerializer());
        // DO NOT ADD ANYTHING IN THE MIDDLE HERE, OR YOU WILL BREAK UNIT TESTS
        Set(id: 20, new ColorSerializer());
        Set(id: 21, new StringSerializer());
        Set(id: 22, new LocalizedTextSerializer());
        Set(id: 23, new RangeSerializer());
        Set(id: 24, new DateTimeSerializer());
        Set(id: 25, new TimeSpanSerializer());
        Set(id: 26, new ByteArraySerializer(this));
        Set(id: 27, new SmallBitSetSerializer());
        Set(id: 28, new BitArraySerializer());
        // ADD new types here, up to `TypeSerializer.MaxFundamentalTypes`
    }

    void Set(ushort id, TypeSerializer ser)
    {
        if (id >= TypeSerializer.MaxFundamentalTypes)
            throw new InvalidOperationException("TypeSerializer.MaxFundamentalTypes exceeded!");
        ser.SetTypeId(id);
        Serializers[ser.Type] = ser;
        FlatMap[id] = ser;
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
            throw new ArgumentNullException(nameof(type));

        ser.SetTypeId(FlatMap.Count);
        if (Serializers.ContainsKey(type))
            throw new InvalidOperationException($"duplicate serializer: {ser}");

        Serializers[type] = ser;
        FlatMap.Add(ser);

        if (ser is UserTypeSerializer userSer)
            userSer.ResolveTypes();
        return ser;
    }

    static Type GetListElemType(Type type)
    {
        if (type.IsGenericType)
        {
            Type genericType = type.GetGenericTypeDefinition();
            if (genericType == typeof(Array<>) || 
                genericType == typeof(IReadOnlyList<>) ||
                genericType == typeof(IList<>) ||
                genericType == typeof(ICollection<>) ||
                genericType == typeof(IEnumerable<>))
                return type.GenericTypeArguments[0];

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Contains(typeof(IList)))
                return type.GenericTypeArguments[0];
        }
        return null;
    }

    static (Type Key, Type Value) GetMapKeyValueTypes(Type type)
    {
        if (type.IsGenericType)
        {
            var genType = type.GetGenericTypeDefinition();
            if (genType == typeof(Map<,>) ||
                genType == typeof(Dictionary<,>) ||
                genType == typeof(IDictionary<,>) ||
                genType == typeof(IReadOnlyDictionary<,>))
                return (type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
        }
        return (null, null);
    }

    static Type GetHashSetElemType(Type type)
    {
        if (type.IsGenericType)
        {
            var genType = type.GetGenericTypeDefinition();
            if (genType == typeof(ISet<>) || genType == typeof(HashSet<>))
                return type.GenericTypeArguments[0];
        }
        return null;
    }

    public TypeSerializer Get(uint typeId)
    {
        if (typeId < FlatMap.Count)
        {
            TypeSerializer ser = FlatMap[(int)typeId];
            if (ser != null)
                return ser;
        }
        throw new InvalidDataException($"{this} unsupported typeId={typeId}");
    }

    public bool TryGet(uint typeId, out TypeSerializer serializer)
    {
        if (typeId < FlatMap.Count)
        {
            serializer = FlatMap[(int)typeId];
            return serializer != null;
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
            TypeSerializer elemSerializer = Get(elemType);
            // NOTE: recursive types cause trouble here
            if (Serializers.TryGetValue(type, out TypeSerializer recursiveType))
                return recursiveType;
            return Add(type, new RawArraySerializer(type, elemType, elemSerializer));
        }

        Type setElemType = GetHashSetElemType(type);
        if (setElemType != null)
        {
            TypeSerializer elemSerializer = Get(setElemType);
            // NOTE: recursive types cause trouble here
            if (Serializers.TryGetValue(type, out TypeSerializer recursiveType))
                return recursiveType;
            return Add(type, new HashSetSerializer(type, setElemType, elemSerializer));
        }

        (Type key, Type value) = GetMapKeyValueTypes(type);
        if (key != null)
        {
            TypeSerializer keySerializer = Get(key);
            TypeSerializer valSerializer = Get(value);
            // NOTE: recursive types cause trouble here
            if (Serializers.TryGetValue(type, out TypeSerializer recursiveType))
                return recursiveType;
            return Add(type, new MapSerializer(type, key, keySerializer, value, valSerializer));
        }

        Type listElemType = GetListElemType(type);
        if (listElemType != null)
        {
            TypeSerializer elemSerializer = Get(listElemType);
            // NOTE: recursive types cause trouble here
            if (Serializers.TryGetValue(type, out TypeSerializer recursiveType))
                return recursiveType;
            return Add(type, new ArrayListSerializer(type, listElemType, elemSerializer));
        }

        if (type.GetCustomAttribute<StarDataTypeAttribute>() != null)
            return AddUserTypeSerializer(type);

        // Nullable<T>, ex: `[StarData] Color? MinColor;`
        Type nulledType = Nullable.GetUnderlyingType(type);
        if (nulledType != null) // create an alias: Color? -> Color
            return Add(type, Get(nulledType));

        throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
    }
}
