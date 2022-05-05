using System.Xml.Serialization;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    // @note These are automatically serialized from /DiplomacyDialogs/{Language}/
    public sealed class DialogOption
    {
        public object Target;
        [XmlElement(ElementName = "number")]
        public int Number;
        [XmlElement(ElementName = "words")]
        public string Words;
        public string SpecialInquiry = string.Empty;
        public string Response;
        public bool Hover;

        public override string ToString() => $"Words: {Words} Response: {Response}";

        public DialogOption()
        {
        }

        public DialogOption(int number, string words)
        {
            Number = number;
            Words = words;
        }
    }
}