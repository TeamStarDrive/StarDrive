﻿using System;
using System.IO;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Utils;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game.Universe.SolarBodies;

[StarDataType]
public sealed class PlanetTypes : IDisposable
{
    [StarData] public float PlanetScale; // base scale of the planet
    [StarData] public float RingsScale; // slightly bigger than the planet
    [StarData] public float CloudsScale; // slightly bigger than the planet
    [StarData] public float AtmosphereScale; // slightly bigger than clouds, a very subtle atmosphere effect, enabled for clouds, see `NoAtmosphere`
    [StarData] public float HaloScale; // a very subtle white halo effect towards the sun, enabled for clouds, see `NoHalo`
    [StarData] public float GlowScale; // colored glow effect around the planet

    // Mesh.BoundingSphere.Radius
    public float BasePlanetRadius;

    [StarData] public string PlanetMesh;
    [StarData] public string[] RingsMesh; // [rings.obj, rings.dds]
    [StarData] public string[] GlowEffect; // [glow.obj, glow.png]
    [StarData] public string[] FresnelEffect; // [fresnel.obj, fresnel.png]

    [StarData] public bool NewRenderer;

    [StarData] public AtmosphereType[] AtmosphereTypes;
    [StarData] public PlanetType[] Types;

    public Matrix RingsScaleMatrix;
    public Matrix CloudsScaleMatrix;
    public Matrix AtmosphereScaleMatrix;
    public Matrix HaloScaleMatrix;
    public Matrix GlowScaleMatrix;

    Map<int, PlanetType> PlanetTypeMap;
    Map<PlanetCategory, PlanetType[]> PlanetTypesByCategory;
    public PlanetRenderer Renderer;
    readonly RandomBase Random = new ThreadSafeRandom();

    void Initialize(GameContentManager content)
    {
        GameLoadingScreen.SetStatus("PlanetTypes");

        Types = Types.Sorted(p => p.Id);

        PlanetTypeMap = new(Types.Length);
        foreach (PlanetType type in Types)
        {
            if (!PlanetTypeMap.TryGetValue(type.Id, out PlanetType existing))
            {
                PlanetTypeMap[type.Id] = type;
            }
            else
            {
                Log.Error($"Duplicate PlanetType.Id={type.Id}. Ignoring New.\n  Old={existing}\n  New={type}");
            }
        }

        PlanetTypesByCategory = new();
        foreach (PlanetCategory c in Enum.GetValues(typeof(PlanetCategory)))
        {
            PlanetTypesByCategory[c] = Types.Filter(pt => pt.Category == c);
        }

        RingsScaleMatrix = Matrix.CreateScale(RingsScale);
        CloudsScaleMatrix = Matrix.CreateScale(CloudsScale);
        AtmosphereScaleMatrix = Matrix.CreateScale(AtmosphereScale);
        HaloScaleMatrix = Matrix.CreateScale(HaloScale);
        GlowScaleMatrix = Matrix.CreateScale(GlowScale);

        Renderer = new(content, this);

        BasePlanetRadius = Renderer.MeshSphere.Radius;

        foreach (PlanetType type in Types)
            type.Initialize(this, content, Renderer.MeshSphere);
    }

    public void Dispose()
    {
        Types = Empty<PlanetType>.Array;
        PlanetTypeMap.Clear();
        PlanetTypesByCategory.Clear();
        Mem.Dispose(ref Renderer);
    }

    static void OnPlanetTypesModified(GameContentManager content, FileInfo file)
    {
        ResourceManager.Planets?.Renderer.Dispose();

        var types = YamlParser.Deserialize<PlanetTypes>(file);
        types.Initialize(content);
        ResourceManager.Planets = types;

        var universe = ScreenManager.Instance.FindScreen<UniverseScreen>();
        if (universe != null)
        {
            foreach (Planet planet in universe.UState.Planets)
                planet.PType = types.Planet(planet.PType.Id);
        }
    }

    public static void LoadPlanetTypes(GameContentManager content)
    {
        ScreenManager.Instance.LoadAndEnableHotLoad("PlanetTypes.yaml",
            f => OnPlanetTypesModified(content, f));
    }

    public PlanetType RandomPlanet()
    {
        if (Types.Length == 0)
            throw new InvalidDataException("No defined PlanetTypes!");
        return Random.Item(Types);
    }

    public PlanetType RandomPlanet(PlanetCategory category)
    {
        if (Types.Length == 0)
            throw new InvalidDataException("No defined PlanetTypes!");
        return Random.Item(PlanetTypesByCategory[category]);
    }

    public PlanetType PlanetOrRandom(int planetId)
    {
        return PlanetTypeMap.TryGetValue(planetId, out PlanetType type)
            ? type : RandomPlanet();
    }

    public PlanetType PlanetOrRandom(int planetId, PlanetCategory randomCategory)
    {
        return PlanetTypeMap.TryGetValue(planetId, out PlanetType type)
            ? type : RandomPlanet(randomCategory);
    }

    public PlanetType Planet(int planetId)
    {
        return PlanetTypeMap[planetId];
    }

    /// <summary>
    /// Gets a new random moon based on allowed types for host planet
    /// WARNING: some planets do not allow moons, in which case this will THROW
    /// </summary>
    public PlanetType RandomMoon(PlanetType forHostPlanet)
    {
        if (forHostPlanet.MoonTypes.Length == 0)
            throw new InvalidOperationException($"No defined MoonTypes for {forHostPlanet.Name}!");

        PlanetCategory c = Random.Item(forHostPlanet.MoonTypes);
        return Random.Item(PlanetTypesByCategory[c]);
    }
}
