using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class BoolSerializer : TypeSerializer
    {
        public override string ToString() => "BoolSerializer";

        public override object Convert(object value)
        {
            if (value is bool) return value;
            if (value is string s)
            {
                return s == "true" || s == "True";
            }
            Error(value, "Bool -- expected string 'true' or 'false'");
            return false;
        }
    }
    
    internal class ByteSerializer : TypeSerializer
    {
        public override string ToString() => "ByteSerializer";

    }

    internal class ShortSerializer : TypeSerializer
    {
        public override string ToString() => "ShortSerializer";

    }

    internal class UShortSerializer : TypeSerializer
    {
        public override string ToString() => "UShortSerializer";

    }

    internal class IntSerializer : TypeSerializer
    {
        public override string ToString() => "IntSerializer";

        public override object Convert(object value)
        {
            if (value is int)      return value;
            if (value is float f)  return (int)f;
            if (value is string s) return StringView.ToInt(s);
            Error(value, "Int -- expected string or float");
            return 0;
        }
    }

    internal class UIntSerializer : TypeSerializer
    {
        public override string ToString() => "UIntSerializer";

    }
}
