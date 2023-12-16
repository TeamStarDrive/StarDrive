using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;
using Rectangle = SDGraphics.Rectangle;
using Vector4 = SDGraphics.Vector4;

namespace Ship_Game.Universe.SolarBodies
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
        [StarData] public readonly string[] AtmosphereType = Empty<string>.Array;
        [StarData] public readonly bool Habitable;
        [StarData] public readonly Range HabitableTileChance = new Range(minMax:20);
        [StarData] public readonly Range PopPerTile;
        [StarData] public readonly Range BaseFertility;
        [StarData] public readonly float MinBaseFertility; // Clamp(MinFertility, float.Max)
        [StarData] public readonly float Scale = 1f; // warning: this may not be the final scale of the planet! modifiers apply
        [StarData] public readonly float ResearchableChance = 0;
        [StarData] public readonly float MiningChance = 0;

        // Allowed moon types for this planet
        [StarData] public readonly PlanetCategory[] MoonTypes = Empty<PlanetCategory>.Array;

        public override string ToString() => $"PlanetType {Id} {Name} {Category} {IconPath} {DiffuseMap}";

        public PlanetTypes Types;

        public bool Glow;
        public Vector4 GlowColor;
        public float Fresnel = 1f;
        public bool Clouds;
        public Texture2D CloudsMap;
        public bool NoHalo;
        public bool NoAtmosphere;

        public StaticMesh PlanetModel;
        public Texture2D DiffuseTex;
        public Texture2D SpecularTex;
        public Texture2D NormalsTex;
        public Texture2D EmissiveTex;

        public LightingEffect Material;

        // pre-load everything necessary
        public void Initialize(PlanetTypes types, GameContentManager content, StaticMesh planetModel)
        {
            Types = types;
            PlanetModel = planetModel;
            
            // don't load these textures in unit tests, we can easily OOM
            // because XNA can't release textures fast enough
            if (GlobalStats.IsUnitTest)
                return;

            DiffuseTex = TryLoadTexture(content, DiffuseMap);
            SpecularTex = TryLoadTexture(content, SpecularMap);
            NormalsTex = TryLoadTexture(content, NormalMap);
            EmissiveTex = TryLoadTexture(content, EmissiveMap);

            Material = CreateMaterial(content);

            foreach (string atmoType in AtmosphereType)
            {
                AtmosphereType atm = types.AtmosphereTypes.Find(at => at.Id == atmoType);
                if (atm == null)
                {
                    Log.Error($"AtmosphereType {atmoType} not defined!");
                    continue;
                }

                if (atm.Clouds != null)
                {
                    Clouds = true;
                    CloudsMap = TryLoadTexture(content, atm.Clouds);
                }

                if (atm.Glow.W > 0f)
                {
                    Glow = true;
                    GlowColor = atm.Glow;
                    if (GlowColor.X > 1f) GlowColor.X /= 255f;
                    if (GlowColor.Y > 1f) GlowColor.Y /= 255f;
                    if (GlowColor.Z > 1f) GlowColor.Z /= 255f;
                    if (GlowColor.W > 1f) GlowColor.W /= 255f;
                    Fresnel *= atm.Fresnel;
                }

                NoHalo |= atm.NoHalo;
                NoAtmosphere |= atm.NoAtmosphere;
            }
        }

        LightingEffect CreateMaterial(GameContentManager content)
        {
            var fx = new LightingEffect(content.Device);
            fx.MaterialName = $"Mat_Planet_{Id}_{Name}";
            fx.MaterialFile = "";
            fx.ProjectFile = "PlanetType.cs";
            fx.DiffuseMapFile = DiffuseMap;
            fx.EmissiveMapFile = EmissiveMap;
            fx.NormalMapFile = NormalMap;
            fx.SpecularColorMapFile = SpecularMap;
            fx.DiffuseMapTexture = DiffuseTex;
            fx.EmissiveMapTexture = EmissiveTex;
            fx.NormalMapTexture = NormalsTex;
            fx.SpecularColorMapTexture = SpecularTex;
            fx.SpecularPower = SpecularPower; // default: 4
            fx.SpecularAmount = 0.25f; // default: 0.25
            fx.FresnelReflectBias = 0.0f; // default: 0
            fx.FresnelReflectOffset = 1.0f; // default: 1
            fx.FresnelMicrofacetDistribution = 0.4f; // default: 0.4
            fx.ParallaxScale = 0.0f; // default: 0
            fx.ParallaxOffset = 0.0f; // default: 0
            return fx;
        }

        static Texture2D TryLoadTexture(GameContentManager content, string texturePath)
        {
            if (texturePath.IsEmpty())
                return null;
            return content.Load<Texture2D>(texturePath);
        }

        public SceneObject CreatePlanetSO()
        {
            return PlanetModel.CreateSceneObject(effect: Material);
        }
    }
}
