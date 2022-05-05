using System;
using System.Collections.Generic;
using System.IO;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Vector2d = SDGraphics.Vector2d;
using Vector3d = SDGraphics.Vector3d;
using Point = SDGraphics.Point;


using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Ship_Game.Data.Serialization.Types
{
    internal class Vector2Serializer : TypeSerializer
    {
        public Vector2Serializer() : base(typeof(Vector2)) { }
        public override string ToString() => "Vector2Serializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector2)obj;
            parent.Value = new object[]{ v.X, v.Y };
        }
        
        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var v = (Vector2)obj;
            writer.BW.Write(v.X);
            writer.BW.Write(v.Y);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Vector2 v;
            v.X = reader.BR.ReadSingle();
            v.Y = reader.BR.ReadSingle();
            return v;
        }

        public static Vector2 FromString(StringView s)
        {
            StringView first = s.Next(',');
            StringView second = s;
            return new Vector2(first.ToFloat(), second.ToFloat());
        }

        public static Vector2 FromString(string s)
        {
            string[] parts = s.Split(',');
            Vector2 p = default;
            if (parts.Length >= 1) p.X = Float(parts[0]);
            if (parts.Length >= 2) p.Y = Float(parts[1]);
            return p;
        }

        public static Vector2 ToVector(object value)
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

    internal class Vector2dSerializer : TypeSerializer
    {
        public Vector2dSerializer() : base(typeof(Vector2d)) { }
        public override string ToString() => "Vector2dSerializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector2d)obj;
            parent.Value = new object[] { v.X, v.Y };
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var v = (Vector2d)obj;
            writer.BW.Write(v.X);
            writer.BW.Write(v.Y);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Vector2d v;
            v.X = reader.BR.ReadDouble();
            v.Y = reader.BR.ReadDouble();
            return v;
        }

        public static Vector2d FromString(StringView s)
        {
            StringView first = s.Next(',');
            StringView second = s;
            return new Vector2d(first.ToDouble(), second.ToDouble());
        }

        public static Vector2d FromString(string s)
        {
            string[] parts = s.Split(',');
            Vector2d p = default;
            if (parts.Length >= 1) p.X = Double(parts[0]);
            if (parts.Length >= 2) p.Y = Double(parts[1]);
            return p;
        }

        public static Vector2d ToVector(object value)
        {
            if (value is object[] objects)
            {
                Vector2d v = default;
                if (objects.Length >= 1) v.X = Double(objects[0]);
                if (objects.Length >= 2) v.Y = Double(objects[1]);
                return v;
            }
            Error(value, "Vector2d -- expected [float,float]");
            return Vector2d.Zero;
        }
    }

    internal class Vector3Serializer : TypeSerializer
    {
        public Vector3Serializer() : base(typeof(Vector3)) { }
        public override string ToString() => "Vector3Serializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector3)obj;
            parent.Value = new object[] { v.X, v.Y, v.Z };
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var v = (Vector3)obj;
            writer.BW.Write(v.X);
            writer.BW.Write(v.Y);
            writer.BW.Write(v.Z);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Vector3 v;
            v.X = reader.BR.ReadSingle();
            v.Y = reader.BR.ReadSingle();
            v.Z = reader.BR.ReadSingle();
            return v;
        }

        public static Vector3 FromString(string s)
        {
            string[] parts = s.Split(',');
            Vector3 v = default;
            if (parts.Length >= 1) v.X = Float(parts[0]);
            if (parts.Length >= 2) v.Y = Float(parts[1]);
            if (parts.Length >= 3) v.Z = Float(parts[2]);
            return v;
        }

        public static Vector3 ToVector(object value)
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

    internal class Vector3dSerializer : TypeSerializer
    {
        public Vector3dSerializer() : base(typeof(Vector3d)) { }
        public override string ToString() => "Vector3dSerializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector3d)obj;
            parent.Value = new object[] { v.X, v.Y, v.Z };
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var v = (Vector3d)obj;
            writer.BW.Write(v.X);
            writer.BW.Write(v.Y);
            writer.BW.Write(v.Z);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Vector3d v;
            v.X = reader.BR.ReadDouble();
            v.Y = reader.BR.ReadDouble();
            v.Z = reader.BR.ReadDouble();
            return v;
        }

        public static Vector3d FromString(string s)
        {
            string[] parts = s.Split(',');
            Vector3d v = default;
            if (parts.Length >= 1) v.X = Double(parts[0]);
            if (parts.Length >= 2) v.Y = Double(parts[1]);
            if (parts.Length >= 3) v.Z = Double(parts[2]);
            return v;
        }

        public static Vector3d ToVector(object value)
        {
            if (value is object[] objects)
            {
                Vector3d v = default;
                if (objects.Length >= 1) v.X = Double(objects[0]);
                if (objects.Length >= 2) v.Y = Double(objects[1]);
                if (objects.Length >= 3) v.Z = Double(objects[2]);
                return v;
            }
            Error(value, "Vector3d -- expected [double,double,double]");
            return Vector3d.Zero;
        }
    }

    internal class Vector4Serializer : TypeSerializer
    {
        public Vector4Serializer() : base(typeof(Vector4)) { }
        public override string ToString() => "Vector4Serializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector4)obj;
            parent.Value = new object[]{ v.X, v.Y, v.Z, v.W };
        }
        
        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var v = (Vector4)obj;
            writer.BW.Write(v.X);
            writer.BW.Write(v.Y);
            writer.BW.Write(v.Z);
            writer.BW.Write(v.W);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Vector4 v;
            v.X = reader.BR.ReadSingle();
            v.Y = reader.BR.ReadSingle();
            v.Z = reader.BR.ReadSingle();
            v.W = reader.BR.ReadSingle();
            return v;
        }

        public static Vector4 FromString(string s)
        {
            string[] parts = s.Split(',');
            Vector4 v = default;
            if (parts.Length >= 1) v.X = Float(parts[0]);
            if (parts.Length >= 2) v.Y = Float(parts[1]);
            if (parts.Length >= 3) v.Z = Float(parts[2]);
            if (parts.Length >= 4) v.W = Float(parts[3]);
            return v;
        }

        public static Vector4 ToVector(object value)
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

    internal class PointSerializer : TypeSerializer
    {
        public PointSerializer() : base(typeof(Point)) { }
        public override string ToString() => "PointSerializer";

        public override object Convert(object value)
        {
            return ToPoint(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var p = (Point)obj;
            parent.Value = new object[]{ p.X, p.Y };
        }
        
        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var p = (Point)obj;
            writer.BW.WriteVLi32(p.X);
            writer.BW.WriteVLi32(p.Y);
        }

        public override object Deserialize(BinarySerializerReader reader)
        {
            Point p;
            p.X = reader.BR.ReadVLi32();
            p.Y = reader.BR.ReadVLi32();
            return p;
        }

        public static Point FromString(string s)
        {
            string[] parts = s.Split(',');
            Point p = default;
            if (parts.Length >= 1) p.X = int.Parse(parts[0]);
            if (parts.Length >= 2) p.Y = int.Parse(parts[1]);
            return p;
        }

        public static Point FromString(StringView s)
        {
            StringView first = s.Next(',');
            StringView second = s;
            return new Point(first.ToInt(), second.ToInt());
        }

        public static Point ToPoint(object value)
        {
            if (value is object[] objects)
            {
                Point p = default;
                if (objects.Length >= 1) p.X = Int(objects[0]);
                if (objects.Length >= 2) p.Y = Int(objects[1]);
                return p;
            }
            Error(value, "Point -- expected [int,int]");
            return Point.Zero;
        }
    }
}
