namespace Ship_Game
{
    public sealed class Message
    {
        public string Text; // TODO: DEPRECATED, MUST BE REMOVED IN THE FUTURE
        public string LocalizedText;
        public int Index; // TODO: This is unused??
        public int SetPlayerContactStep;
        public int SetFactionContactStep;
        public bool SetWar;
        public bool EndWar;
        public Array<Response> ResponseOptions = new Array<Response>();
        public bool EndTransmission;
    }
}