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
        Array<TypeSerializer> Index;

        protected TypeSerializerMap()
        {
            Add<BoolSerializer>   (typeof(bool)   );
            Add<ByteSerializer>   (typeof(byte)   );
            Add<ShortSerializer>  (typeof(short)  );
            Add<UShortSerializer> (typeof(ushort) );
            Add<IntSerializer>    (typeof(int)    );
            Add<UIntSerializer>   (typeof(uint)   );
            Add<LongSerializer>   (typeof(long)   );
            Add<ULongSerializer>  (typeof(ulong)  );
            Add<FloatSerializer>  (typeof(float)  );
            Add<DoubleSerializer> (typeof(double) );
            Add<Vector2Serializer>(typeof(Vector2));
            Add<Vector3Serializer>(typeof(Vector3));
            Add<Vector4Serializer>(typeof(Vector4));
            Add<ColorSerializer>  (typeof(Color)  );
            Add<StringSerializer> (typeof(string) );
            Add<RangeSerializer>  (typeof(Range)  );
            Add<LocTextSerializer>(typeof(LocText));
        }

        protected abstract TypeSerializer AddUserTypeSerializer(Type type); 

        protected void Add<T>(Type type) where T : TypeSerializer, new()
        {
            Serializers[type] = new T { Id = ++NextId };
        }

        TypeSerializer Add(Type type, TypeSerializer serializer)
        {
            Serializers[type] = serializer;
            return serializer;
        }

        static Type GetListType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Array<>))
                    return type.GenericTypeArguments[0];
                if (type.GetInterfaces().Contains(typeof(IList)))
                    return type.GenericTypeArguments[0];
            }
            return null;
        }

        public TypeSerializer Get(Type type)
        {
            if (Serializers.TryGetValue(type, out TypeSerializer converter))
                return converter;

            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                return Add(type, new RawArraySerializer(elemType, Get(elemType)));
            }

            Type listElemType = GetListType(type);
            if (listElemType != null)
                return Add(type, new ArrayListSerializer(listElemType, Get(listElemType)));

            if (type.IsEnum)
                return Add(type, new EnumSerializer(type));

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
