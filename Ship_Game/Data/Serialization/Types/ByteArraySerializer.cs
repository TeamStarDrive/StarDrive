using System;
using System.Collections;
using System.IO.Compression;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    // This is a specialized optimization for byte[]
    // to automatically compress large amount of data
    internal class ByteArraySerializer : RawArraySerializer
    {
        public override string ToString() => $"ByteArraySerializer<{ElemType.GetTypeName()}:{ElemSerializer.TypeId}>:{TypeId}";

        public ByteArraySerializer(TypeSerializerMap typeMap)
            : base(typeof(byte[]), typeof(byte), typeMap.Get(typeof(byte)))
        {
            Category = SerializerCategory.RawArray;
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            byte[] array = (byte[])obj;
            writer.BW.WriteVLu32((uint)array.Length);
            writer.BW.Write(array);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            int count = (int)reader.BR.ReadVLu32();
            byte[] array = reader.BR.ReadBytes(count);
            return array;
        }

        public override object CreateInstance()
        {
            throw new NotSupportedException();
        }
    }
}
