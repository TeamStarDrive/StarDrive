namespace Ship_Game
{
    //Added by McShooterz: defines new enemy fleets to spawn in place of Remnant
    public sealed class HostileFleets
    {
        public Array<Fleet> Fleets = new Array<Fleet>();

        public struct Fleet
        {
            public string Empire;
            public Array<string> Ships;
        }
    }
}
