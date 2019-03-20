using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.Binary
{
    class BinarySerializers
    {
        readonly Map<Type, TypeSerializer> Serializers = new Map<Type, TypeSerializer>
        {
            (typeof(bool),   new BoolSerializer(1)),
            (typeof(byte),   new ByteSerializer(2)),
            (typeof(short),  new ShortSerializer(3)),
            (typeof(ushort), new UShortSerializer(4)),
            (typeof(int),    new IntSerializer(5)),
            (typeof(uint),   new UIntSerializer(6)),
            (typeof(float),  new FloatSerializer(7)),
            (typeof(double), new DoubleSerializer(8)),
            (typeof(Vector2), new Vector2Serializer(9)),
            (typeof(Vector3), new Vector3Serializer(10)),
            (typeof(Vector4), new Vector4Serializer(11)),
            (typeof(Color),   new ColorSerializer(12)),
            (typeof(string),  new StringSerializer(13)),
        };

        Array<TypeSerializer> Index;

        public TypeSerializer Get(Type type)
        {
            if (Serializers.TryGetValue(type, out TypeSerializer converter))
                return converter;

            return null;
        }
    }
}
