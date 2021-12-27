using System;
using System.IO;
using System.Text;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    internal class StringSerializer : TypeSerializer
    {
        public StringSerializer() : base(typeof(string)) { }
        public override string ToString() => "StringSerializer";

        public override object Convert(object value)
        {
            return value?.ToString();
        }
        
        public override void Serialize(YamlNode parent, object obj)
        {
            string text = obj as string ?? "";
            parent.Value = text;
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            string value = (string)obj;
            writer.Write(value);
        }

        public override object Deserialize(BinaryReader reader)
        {
            string value = reader.ReadString();
            return value;
        }
    }

    internal class LocalizedTextSerializer : TypeSerializer
    {
        public LocalizedTextSerializer() : base(typeof(LocalizedText)) { }
        public override string ToString() => "LocalizedTextSerializer";

        public override object Convert(object value)
        {
            if (value is int id)   return new LocalizedText(id);
            if (value is string s)
            {
                // this is sort of a pre-optimization
                // only set Parse if text contains {id} token bracket
                if (s.IndexOf('{') != -1)
                    return new LocalizedText(s, LocalizationMethod.Parse);
                return new LocalizedText(s, LocalizationMethod.RawText);
            }
            Error(value, "LocalizedText -- expected int or format string");
            return new LocalizedText("INVALID TEXT", LocalizationMethod.RawText);
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var lt = (LocalizedText)obj;
            switch (lt.Method)
            {
                case LocalizationMethod.Id:
                    parent.Value = lt.Id;
                    break;
                case LocalizationMethod.NameId:
                case LocalizationMethod.RawText:
                case LocalizationMethod.Parse:
                    parent.Value = lt.String;
                    break;
            }
        }

        public override void Serialize(BinaryWriter writer, object obj)
        {
            var localizedText = (LocalizedText)obj;
            writer.Write((byte)localizedText.Method);
            writer.Write((ushort)localizedText.Id);
            switch (localizedText.Method) // only write string for these cases:
            {
                case LocalizationMethod.NameId:
                case LocalizationMethod.RawText:
                case LocalizationMethod.Parse:
                    writer.Write(localizedText.String);
                    break;
            }
        }

        public override object Deserialize(BinaryReader reader)
        {
            var method = (LocalizationMethod)reader.ReadByte();
            int id = reader.ReadUInt16();
            string str = null;

            switch (method)
            {
                case LocalizationMethod.NameId:
                case LocalizationMethod.RawText:
                case LocalizationMethod.Parse:
                    str = reader.ReadString();
                    break;
            }

            var localizedText = new LocalizedText(id, str, method);
            return localizedText;
        }
    }
}
