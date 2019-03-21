using System;

namespace Ship_Game.Data.Serialization.Types
{
    internal class EnumSerializer : TypeSerializer
    {
        public override string ToString() => $"EnumSerializer {ToEnum.GenericName()}";
        readonly Type ToEnum;

        public EnumSerializer(Type toEnum)
        {
            ToEnum = toEnum;
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
    }
}