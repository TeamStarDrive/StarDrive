using System;
using System.IO;

namespace Ship_Game.Data.Serialization.Types
{
    internal class EnumSerializer : TypeSerializer
    {
        public override string ToString() => $"EnumSerializer {ToEnum.GetTypeName()}";
        readonly Type ToEnum;
        readonly Map<int, object> Mapping = new Map<int, object>();
        readonly object DefaultValue;

        public EnumSerializer(Type toEnum)
        {
            ToEnum = toEnum;
            Array values = Enum.GetValues(ToEnum);
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
                    return Enum.Parse(ToEnum, enumLiteral, ignoreCase:true);
                if (value is int enumIndex)
                    return Enum.ToObject(ToEnum, enumIndex);
                Error(value, $"Enum '{ToEnum.Name}' -- expected a string or int");
            }
            catch (Exception e)
            {
                Error(value, $"Enum '{ToEnum.Name}' -- {e.Message}");
            }
            return ToEnum.GetEnumValues().GetValue(0);
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            int enumIndex = (int)obj;
            writer.Write(enumIndex);
        }
        
        public override object Deserialize(BinaryReader reader)
        {
            int enumIndex = reader.ReadInt32();
            if (Mapping.TryGetValue(enumIndex, out object enumValue))
                return enumValue;

            Error(enumIndex, $"Enum '{ToEnum.Name}' -- using Default value '{DefaultValue}' instead");
            return DefaultValue;
        }
    }
}