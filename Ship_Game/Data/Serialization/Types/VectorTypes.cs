using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.Data.Serialization.Types
{
    internal class Vector2Serializer : FloatSerializer
    {
        public override string ToString() => "Vector2Serializer";

        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector2 v = default;
                if (objects.Length >= 1) v.X = Float(objects[0]);
                if (objects.Length >= 2) v.Y = Float(objects[1]);
                return v;
            }
            Error(value, "Vector2 -- expected [float,float]");
            return Vector2.Zero;
        }
    }

    internal class Vector3Serializer : FloatSerializer
    {
        public override string ToString() => "Vector3Serializer";

        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector3 v = default;
                if (objects.Length >= 1) v.X = Float(objects[0]);
                if (objects.Length >= 2) v.Y = Float(objects[1]);
                if (objects.Length >= 3) v.Z = Float(objects[2]);
                return v;
            }
            Error(value, "Vector3 -- expected [float,float,float]");
            return Vector3.Zero;
        }
    }

    internal class Vector4Serializer : FloatSerializer
    {
        public override string ToString() => "Vector4Serializer";

        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                Vector4 v = default;
                if (objects.Length >= 1) v.X = Float(objects[0]);
                if (objects.Length >= 2) v.Y = Float(objects[1]);
                if (objects.Length >= 3) v.Z = Float(objects[2]);
                if (objects.Length >= 4) v.W = Float(objects[3]);
                return v;
            }
            Error(value, "Vector4 -- expected [float,float,float,float]");
            return Vector4.Zero;
        }
    }
}
