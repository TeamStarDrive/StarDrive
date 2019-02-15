using System;
using Ship_Game.AI;

namespace Ship_Game
{
    public enum Goods
    {
        None,
        Production,
        Food,
        Colonists
    }

    public sealed class Good
    {
        public string UID;
        public bool IsCargo = true;
        public string Name;
        public string Description;
        public float Cost;
        public float Mass;
        public string IconTexturePath;
    }
}
