namespace Ship_Game.Tools.Localization
{
    public class Translation
    {
        public readonly int Id;
        public readonly string Lang;
        public readonly string Text;

        public Translation(int id, string lang, string text)
        {
            Id = id;
            Lang = lang;
            Text = text;
        }

        public Translation(Translation copy)
        {
            Id = copy.Id;
            Lang = copy.Lang;
            Text = copy.Text;
        }

        // properly escaped yaml safe string
        public string YamlString
        {
            get
            {
                string escaped = Text;
                escaped = escaped.Replace("\r\n", "\\n");
                escaped = escaped.Replace("\n", "\\n");
                escaped = escaped.Replace("\t", "\\t");
                escaped = escaped.Replace("\"", "\\\"");
                return "\"" + escaped + "\"";
            }
        }

        public override string ToString() => $"{Lang}: {Text}";
    }
}