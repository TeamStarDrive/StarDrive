using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class FloatSerializer : TypeSerializer
    {
        public override string ToString() => "FloatSerializer";

        public override object Convert(object value)
        {
            if (value is float f)  return f;
            if (value is int i)    return (float)i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Float -- expected int or float or string");
            return 0.0f;
        }
    }

    internal class DoubleSerializer : TypeSerializer
    {
        public override string ToString() => "DoubleSerializer";

        public override object Convert(object value)
        {
            if (value is float f)  return (double)f;
            if (value is int i)    return (double)i;
            if (value is string s) return StringView.ToDouble(s);
            Error(value, "Double -- expected int or float or string");
            return 0.0;
        }
    }
}
