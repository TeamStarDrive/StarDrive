namespace Ship_Game
{
    public sealed class Message
    {
        public string Text;
        public int Index;
        public int SetPlayerContactStep;
        public int SetFactionContactStep;
        public bool SetWar;
        public bool EndWar;
        public Array<Response> ResponseOptions = new Array<Response>();
        public bool EndTransmission;
    }
}