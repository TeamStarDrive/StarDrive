using System;
using System.IO;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class EnumSerializer : TypeSerializer
    {
        public override string ToString() => $"EnumSerializer {Type.GetTypeName()}";
        readonly Map<int, object> Mapping = new Map<int, object>();
        readonly object DefaultValue;

        public EnumSerializer(Type toEnum) : base(toEnum)
        {
            Array values = Enum.GetValues(toEnum);
            DefaultValue = values.GetValue(0);
            for (int i = 0; i < values.Length; ++i)
            {
                object enumValue = values.GetValue(i);
                int enumIndex = (int)enumValue;
                Mapping[enumIndex] = enumValue;
            }
        }

        public override object Convert(object value)
        {
            try
            {
                if (value is string enumLiteral)
                    return Enum.Parse(Type, enumLiteral, ignoreCase:true);
                if (value is int enumIndex)
                    return Enum.ToObject(Type, enumIndex);
                Error(value, $"Enum '{Type.Name}' -- expected a string or int");
            }
            catch (Exception e)
            {
                Error(value, $"Enum '{Type.Name}' -- {e.Message}");
            }
            return Type.GetEnumValues().GetValue(0);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var e = (Enum)obj;
            parent.Value = e.ToString();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            int enumIndex = (int)obj;
            writer.BW.WriteVLi32(enumIndex);
        }
        
        public override object Deserialize(BinarySerializerReader reader)
        {
            int enumIndex = reader.BR.ReadVLi32();
            if (Mapping.TryGetValue(enumIndex, out object enumValue))
                return enumValue;

            Error(enumIndex, $"Enum '{Type.Name}' -- using Default value '{DefaultValue}' instead");
            return DefaultValue;
        }
    }
}