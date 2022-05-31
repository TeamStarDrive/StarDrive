using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class ColorSerializer : TypeSerializer
    {
        public ColorSerializer() : base(typeof(Color)) { }
        public override string ToString() => $"ColorSerializer:{TypeId}";

        public override object Convert(object value)
        {
            if (value is object[] objects)
            {
                if (objects[0] is int)
                {
                    byte r = 255, g = 255, b = 255, a = 255;
                    if (objects.Length >= 1) r = Byte(objects[0]);
                    if (objects.Length >= 2) g = Byte(objects[1]);
                    if (objects.Length >= 3) b = Byte(objects[2]);
                    if (objects.Length >= 4)
                    {
                        if (objects[3] is float fa)
                            a = (byte)(fa * 255).Clamped(0, 255);
                        else
                            a = Byte(objects[3]);
                    }
                    return new Color(r, g, b, a);
                }
                else
                {
                    float r = 1f, g = 1f, b = 1f, a = 1f;
                    if (objects.Length >= 1) r = Float(objects[0]);
                    if (objects.Length >= 2) g = Float(objects[1]);
                    if (objects.Length >= 3) b = Float(objects[2]);
                    if (objects.Length >= 4) a = Float(objects[3]);
                    return new Color(r, g, b, a);
                }
            }
            if (value is int i) // short hand to get [i,i,i,i]
            {
                i = i.Clamped(0, 255);
                return new Color((byte)i, (byte)i, (byte)i, (byte)i);
            }
            if (value is float f) // short hand to get [f,f,f,f]
            {
                f = f.Clamped(0f, 1f);
                return new Color(f, f, f, f);
            }
            Error(value, "Color -- expected [int,int,int,int] or [float,float,float,float] or int or number");
            return Color.Red;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var c = (Color)obj;
            parent.Value = new object[]{ (int)c.R, (int)c.G, (int)c.B, (int)c.A };
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            var color = (Color)obj;
            writer.BW.Write(color.PackedValue);
        }
        
        public override object Deserialize(BinarySerializerReader reader)
        {
            var color = new Color { PackedValue = reader.BR.ReadUInt32() };
            return color;
        }
    }
}