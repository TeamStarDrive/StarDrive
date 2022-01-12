using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    // @note This is parsed from PlanetTypes.yaml; All fields are immutable.
    [StarDataType]
    public class PlanetType
    {
        [StarData] public readonly int Id;
        [StarData] public readonly string Name; // descriptive name of this PlanetType, used in error message etc
        [StarData] public readonly PlanetCategory Category;
        [StarData] public readonly LocalizedText Composition;
        [StarData] public readonly string IconPath;
        [StarData] public readonly string DiffuseMap;
        [StarData] public readonly string SpecularMap;
        [StarData] public readonly string NormalMap;
        [StarData] public readonly string EmissiveMap;
        [StarData] public readonly float SpecularPower = 0.0f; // 0.0 == no specural effects at all

        [StarData] public readonly string PlanetTile;
        [StarData] public readonly Color? Glow; // planetary glow
        [StarData] public readonly bool Clouds;
        [StarData] public readonly bool Atmosphere;
        [StarData] public readonly bool Habitable;
        [StarData] public readonly Range HabitableTileChance = new Range(minMax:20);
        [StarData] public readonly Range PopPerTile;
        [StarData] public readonly Range BaseFertility;
        [StarData] public readonly float MinBaseFertility; // Clamp(MinFertility, float.Max)
        [StarData] public readonly float Scale = 0f;

        // Allowed moon types for this planet
        [StarData] public readonly PlanetCategory[] MoonTypes = Empty<PlanetCategory>.Array;

        public override string ToString() => $"PlanetType {Id} {Name} {Category} {IconPath} {DiffuseMap}";

        public Model PlanetModel;
        public LightingEffect Material;

        // pre-load everything necessary
        public void Initialize(GameContentManager content, Model planetModel)
        {
            var fx = new LightingEffect(content.Device);
            fx.MaterialName          = $"Mat_Planet_{Id}_{Name}";
            fx.MaterialFile          = "";
            fx.ProjectFile           = "PlanetType.cs";
            fx.DiffuseMapFile        = DiffuseMap;
            fx.EmissiveMapFile       = EmissiveMap;
            fx.NormalMapFile         = NormalMap;
            fx.SpecularColorMapFile  = SpecularMap;
            fx.DiffuseMapTexture  = TryLoadTexture(content, DiffuseMap);
            fx.EmissiveMapTexture = TryLoadTexture(content, EmissiveMap);
            fx.NormalMapTexture   = TryLoadTexture(content, NormalMap);
            fx.SpecularColorMapTexture = TryLoadTexture(content, SpecularMap);
            fx.SpecularPower  = SpecularPower; // default: 4
            fx.SpecularAmount = 0.25f; // default: 0.25
            fx.FresnelReflectBias            = 0.0f; // default: 0
            fx.FresnelReflectOffset          = 1.0f; // default: 1
            fx.FresnelMicrofacetDistribution = 0.4f; // default: 0.4
            fx.ParallaxScale                 = 0.0f; // default: 0
            fx.ParallaxOffset                = 0.0f; // default: 0
            Material = fx;
            PlanetModel = planetModel;
        }

        static Texture2D TryLoadTexture(GameContentManager content, string texturePath)
        {
            if (texturePath.IsEmpty())
                return null;
            return content.Load<Texture2D>(texturePath);
        }

        public SceneObject CreatePlanetSO()
        {
            var so = StaticMesh.SceneObjectFromModel(PlanetModel, Material);
            return so;
        }
    }
}
