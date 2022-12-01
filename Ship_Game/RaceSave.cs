using System;
using System.Configuration;

namespace Ship_Game
{
    public sealed class RaceSave
    {
        public string Name;
        public string ModName = "";
        public string ModPath = "";
        public int Version;
        public RacialTrait Traits;

        public RaceSave()
        { }

        public RaceSave(RacialTrait traits)
        {
            Name = traits.Name;
            if (GlobalStats.HasMod)
            {
                ModName = GlobalStats.ActiveMod.ModName;
                ModPath = GlobalStats.ActiveMod.ModName;
            }
            Version = SavedGame.SaveGameVersion;
            Traits = traits;
        }
    }
}
