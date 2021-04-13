using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    // @note This is parsed from PlanetTypes.yaml; All fields are immutable.
    [StarDataType]
    public class PlanetType
    {
        [StarData] public readonly int Id;
        [StarData] public readonly PlanetCategory Category;
        [StarData] public readonly LocalizedText Composition;
        [StarData] public readonly string IconPath;
        [StarData] public readonly string MeshPath;
        [StarData] public readonly string PlanetTile;
        [StarData] public readonly PlanetGlow Glow;
        [StarData] public readonly bool EarthLike;
        [StarData] public readonly bool Habitable;
        [StarData] public readonly Range HabitableTileChance = new Range(minMax:20);
        [StarData] public readonly Range PopPerTile;
        [StarData] public readonly Range BaseFertility;
        [StarData] public readonly float MinBaseFertility; // Clamp(MinFertility, float.Max)
        [StarData] public readonly float Scale = 0f;

        public override string ToString() => $"PlanetType {Id} {Category} {IconPath} {MeshPath}";
    }

    public enum PlanetGlow
    {
        None, Terran, Red, White, Aqua, Orange
    }
}
