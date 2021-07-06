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
        ushort NextId;
        readonly Map<Type, TypeSerializer> Serializers = new Map<Type, TypeSerializer>();
        readonly Array<TypeSerializer> Index = new Array<TypeSerializer>();
        public IReadOnlyList<TypeSerializer> TypesList => Index;

        protected TypeSerializerMap()
        {
            Add(typeof(bool),   new BoolSerializer()  );
            Add(typeof(byte),   new ByteSerializer()  );
            Add(typeof(short),  new ShortSerializer() );
            Add(typeof(ushort), new UShortSerializer());
            Add(typeof(int),    new IntSerializer()   );
            Add(typeof(uint),   new UIntSerializer()  );
            Add(typeof(long),   new LongSerializer()  );
            Add(typeof(ulong),  new ULongSerializer() );
            Add(typeof(float),  new FloatSerializer() );
            Add(typeof(double),  new DoubleSerializer() );
            Add(typeof(Vector2), new Vector2Serializer());
            Add(typeof(Vector3), new Vector3Serializer());
            Add(typeof(Vector4), new Vector4Serializer());
            Add(typeof(Color),   new ColorSerializer()  );
            Add(typeof(string),  new StringSerializer() );
            Add(typeof(LocalizedText), new LocalizedTextSerializer());
            Add(typeof(Range), new RangeSerializer());
            Add(typeof(TimeSpan), new TimeSpanSerializer());
        }

        protected abstract TypeSerializer AddUserTypeSerializer(Type type); 

        protected TypeSerializer Add(Type type, TypeSerializer ser)
        {
            ser.Id   = ++NextId;
            ser.Type = type;
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
