using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Binary
{
    internal class TypeSerializer
    {
        // Id which is valid in a single serialization context
        public readonly ushort Id;

        public TypeSerializer(ushort id)
        {
            Id = id;
        }

        public virtual void Serialize(MemoryStream ms, object obj)
        {
            
        }

        public virtual object Deserialize(MemoryStream ms)
        {
            return null;
        }
    }

    internal class BoolSerializer : TypeSerializer
    {
        public BoolSerializer(ushort id) : base(id) {}
    }
    
    internal class ByteSerializer : TypeSerializer
    {
        public ByteSerializer(ushort id) : base(id) {}
    }

    internal class ShortSerializer : TypeSerializer
    {
        public ShortSerializer(ushort id) : base(id) {}
    }

    internal class UShortSerializer : TypeSerializer
    {
        public UShortSerializer(ushort id) : base(id) {}
    }

    internal class IntSerializer : TypeSerializer
    {
        public IntSerializer(ushort id) : base(id) {}
    }

    internal class UIntSerializer : TypeSerializer
    {
        public UIntSerializer(ushort id) : base(id) {}
    }

    internal class FloatSerializer : TypeSerializer
    {
        public FloatSerializer(ushort id) : base(id) {}
    }

    internal class DoubleSerializer : TypeSerializer
    {
        public DoubleSerializer(ushort id) : base(id) {}
    }

    internal class Vector2Serializer : FloatSerializer
    {
        public Vector2Serializer(ushort id) : base(id) {}
    }

    internal class Vector3Serializer : FloatSerializer
    {
        public Vector3Serializer(ushort id) : base(id) {}
    }

    internal class Vector4Serializer : FloatSerializer
    {
        public Vector4Serializer(ushort id) : base(id) {}
    }

    internal class ColorSerializer : TypeSerializer
    {
        public ColorSerializer(ushort id) : base(id) {}
    }

    internal class EnumSerializer : TypeSerializer
    {
        readonly Type ToEnum;
        public EnumSerializer(Type toEnum, ushort id) : base(id)
        {
            ToEnum = toEnum;
        }
    }

    internal class StringSerializer : TypeSerializer
    {
        public StringSerializer(ushort id) : base(id) {}
    }
}
