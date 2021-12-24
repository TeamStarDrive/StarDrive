using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Serialization
{
    public abstract class TypeSerializerMap
    {
        ushort NextId = TypeSerializer.MaxFundamentalTypes + 1;
        readonly Map<Type, TypeSerializer> Serializers = new Map<Type, TypeSerializer>();
        readonly Array<TypeSerializer> Index = new Array<TypeSerializer>();
        public IReadOnlyList<TypeSerializer> TypesList => Index;

        protected TypeSerializerMap()
        {
            AddFundamental(1, typeof(bool),   new BoolSerializer()  );
            AddFundamental(2, typeof(byte),   new ByteSerializer()  );
            AddFundamental(3, typeof(short),  new ShortSerializer() );
            AddFundamental(4, typeof(ushort), new UShortSerializer());
            AddFundamental(5, typeof(int),    new IntSerializer()   );
            AddFundamental(6, typeof(uint),   new UIntSerializer()  );
            AddFundamental(7, typeof(long),   new LongSerializer()  );
            AddFundamental(8, typeof(ulong),  new ULongSerializer() );
            AddFundamental(9, typeof(float),  new FloatSerializer() );
            AddFundamental(10, typeof(double),  new DoubleSerializer() );
            AddFundamental(11, typeof(Vector2), new Vector2Serializer());
            AddFundamental(12, typeof(Vector3), new Vector3Serializer());
            AddFundamental(13, typeof(Vector4), new Vector4Serializer());
            AddFundamental(14, typeof(Color),   new ColorSerializer()  );
            AddFundamental(15, typeof(string),  new StringSerializer() );
            AddFundamental(16, typeof(LocalizedText), new LocalizedTextSerializer());
            AddFundamental(17, typeof(Range), new RangeSerializer());
            AddFundamental(18, typeof(TimeSpan), new TimeSpanSerializer());
        }

        protected abstract TypeSerializer AddUserTypeSerializer(Type type); 

        void AddFundamental(int id, Type type, TypeSerializer ser)
        {
            if (id >= TypeSerializer.MaxFundamentalTypes)
                throw new IndexOutOfRangeException("Max limit of fundamental types reached");

            ser.Id = (ushort)id;
            ser.Type = type;
            Serializers[type] = ser;
            Index.Add(ser);
        }

        protected TypeSerializer Add(Type type, TypeSerializer ser)
        {
            ser.Id = NextId++;
            ser.Type = type;
            if (ser.Id == (ushort.MaxValue-1))
                throw new IndexOutOfRangeException($"serializer.Id overflow -- too many types: {ser.Id}");

            Serializers[type] = ser;
            Index.Add(ser);
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
