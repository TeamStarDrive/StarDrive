using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.Data.Serialization.Types
{
    internal class Vector2Serializer : TypeSerializer
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
    }

    internal class Vector3Serializer : TypeSerializer
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
    }

    internal class Vector4Serializer : TypeSerializer
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
    }

    internal class PointSerializer : TypeSerializer
    {
        public override string ToString() => "PointSerializer";

        public override object Convert(object value)
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
    }
}
