using System;
using System.IO;
using System.Reflection;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class EnumSerializer : TypeSerializer
    {
        public override string ToString() => $"EnumSerializer {NiceTypeName}:{TypeId}";
        readonly Map<int, object> Mapping = new();
        readonly object DefaultValue;

        bool IsFlagsEnum;

        public EnumSerializer(Type toEnum) : base(toEnum)
        {
            Array values = toEnum.GetEnumValues();
            DefaultValue = values.GetValue(0);
            IsFlagsEnum = toEnum.GetCustomAttribute<FlagsAttribute>() != null;

            for (int i = 0; i < values.Length; ++i)
            {
                object enumValue = values.GetValue(i);
                int enumIndex = GetEnumIndex(enumValue);
                Mapping[enumIndex] = enumValue;
            }
        }

        static int GetEnumIndex(object enumValue)
        {
            int enumIndex = System.Convert.ToInt32(enumValue);
            return enumIndex;
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
            return DefaultValue;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var e = (Enum)obj;
            parent.Value = e.ToString();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            int enumIndex = GetEnumIndex(obj);
            writer.BW.WriteVLi32(enumIndex);
        }
        
        public override object Deserialize(BinarySerializerReader reader)
        {
            int enumIndex = reader.BR.ReadVLi32();
            if (Mapping.TryGetValue(enumIndex, out object enumValue))
                return enumValue;

            if (IsFlagsEnum && enumIndex != 0)
            {
                try
                {
                    return Enum.ToObject(Type, enumIndex);
                }
                catch
                {
                }
            }

            Error(enumIndex, $"Enum '{Type.Name}' -- using Default value '{DefaultValue}' instead");
            return DefaultValue;
        }
    }
}