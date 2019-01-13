using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;

namespace Ship_Game
{
    public class PlanetTypeInfo
    {
        [StarData(true)] public int Id;
        [StarData] public PlanetCategory Category;
        [StarData] public LocText Composition;
        [StarData] public string MeshPath;
        [StarData] public string PlanetTile;
        [StarData] public PlanetGlow Glow;
        [StarData] public bool EarthLike;
        [StarData] public bool Habitable;
        [StarData] public Range HabitableTileChance = new Range(minMax:20);
        [StarData] public Range MaxPop;
        [StarData] public Range Fertility;
        [StarData] public float MinFertility; // Clamp(MinFertility, float.Max)
        [StarData] public SunZone Zone = SunZone.Any;
        [StarData] public float Scale = 0f;

        public string IconPath => "Planets/" + Id;
    }

    public enum PlanetGlow
    {
        None, Terran, Red, White, Aqua, Orange
    }
}
