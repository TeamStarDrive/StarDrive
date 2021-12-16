using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class Vector2Serializer : TypeSerializer
    {
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
        
        public override void Serialize(BinaryWriter writer, object obj)
        {
            var v = (Vector3)obj;
            writer.Write(v.X);
            writer.Write(v.Y);
        }

        public override object Deserialize(BinaryReader reader)
        {
            Vector2 v;
            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
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

    internal class Vector3Serializer : TypeSerializer
    {
        public override string ToString() => "Vector3Serializer";

        public override object Convert(object value)
        {
            return ToVector(value);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var v = (Vector3)obj;
            parent.Value = new object[]{ v.X, v.Y, v.Z };
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var v = (Vector3)obj;
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        public override object Deserialize(BinaryReader reader)
        {
            Vector3 v;
            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();
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

    internal class Vector4Serializer : TypeSerializer
    {
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
        
        public override void Serialize(BinaryWriter writer, object obj)
        {
            var v = (Vector4)obj;
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
            writer.Write(v.W);
        }

        public override object Deserialize(BinaryReader reader)
        {
            Vector4 v;
            v.X = reader.ReadSingle();
            v.Y = reader.ReadSingle();
            v.Z = reader.ReadSingle();
            v.W = reader.ReadSingle();
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
        
        public override void Serialize(BinaryWriter writer, object obj)
        {
            var p = (Point)obj;
            writer.Write(p.X);
            writer.Write(p.Y);
        }

        public override object Deserialize(BinaryReader reader)
        {
            Point p;
            p.X = reader.ReadInt32();
            p.Y = reader.ReadInt32();
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
