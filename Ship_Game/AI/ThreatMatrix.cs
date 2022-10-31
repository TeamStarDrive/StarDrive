using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI;

/// <summary>
/// Contains ThreatClusters of OUR ships and other RIVAL Empires ships.
/// Some of the rivals may be HOSTILE.
/// The ships are organized by Empires and Solar systems
/// </summary>
[StarDataType]
public sealed partial class ThreatMatrix
{
    // The empire which owns this ThreatMatrix
    [StarData] readonly Empire Owner;

    // All clusters that we know about, this is thread-safe
    // and updated atomically as a COPY
    [StarData] public ThreatCluster[] AllClusters { get; private set; } = Empty<ThreatCluster>.Array;

    // Qtree for quickly finding nearby clusters
    public GenericQtree ClustersMap { get; private set; }

    [StarDataConstructor] ThreatMatrix() { }

    public ThreatMatrix(Empire owner)
    {
        Owner = owner;
        InitializeOnConstruct(Owner.Universe);
    }

    [StarDataDeserialized]
    public void OnDeserialized(UniverseState us)
    {
        InitializeOnConstruct(us);
    }

    void InitializeOnConstruct(UniverseState us)
    {
        ClustersMap = new(us.Size, cellThreshold:16, smallestCell:8000);
        foreach (ThreatCluster cluster in AllClusters)
            ClustersMap.Insert(cluster);
    }

    ThreatCluster[] FindClusters(in SearchOptions opt)
    {
        return ClustersMap.Find<ThreatCluster>(in opt);
    }

    /// <summary>
    /// Find clusters at pos+radius of a single Empire
    /// </summary>
    public ThreatCluster[] FindClusters(Empire empire, Vector2 pos, float radius)
    {
        return FindClusters(new(pos, radius, GameObjectType.ThreatCluster)
        {
            OnlyLoyalty = empire
        });
    }

    /// <summary>
    /// Find HOSTILE clusters at pos+radius
    /// </summary>
    public ThreatCluster[] FindHostileClusters(Vector2 pos, float radius)
    {
        return FindClusters(new(pos, radius)
        {
            ExcludeLoyalty = Owner,
            Type = GameObjectType.ThreatCluster,
            FilterFunction = (o) => ((ThreatCluster)o).IsHostileTo(Owner)
        });
    }

    // Find all enemy clusters within radius
    public ThreatCluster[] FindHostileClustersByDist(Vector2 pos, float radius)
    {
        return FindClusters(new(pos, radius)
        {
            ExcludeLoyalty = Owner,
            Type = GameObjectType.ThreatCluster,
            SortByDistance = true,
            FilterFunction = (o) => ((ThreatCluster)o).IsHostileTo(Owner)
        });
    }

    static float GetStrength(ThreatCluster[] clusters)
    {
        float strength = 0f;
        for (int i = 0; i < clusters.Length; ++i) // PERF: using for loop instead of lambdas
            strength += clusters[i].Strength;
        return strength;
    }

    /// <summary> Get all strength of a specific empire (can be this.Owner) in a system</summary>
    public float GetStrengthAt(Empire empire, Vector2 pos, float radius)
    {
        return GetStrength(FindClusters(empire, pos, radius));
    }

    /// <summary> Get all strength of a specific hostile in a system</summary>
    public float GetHostileStrengthAt(Empire enemy, Vector2 pos, float radius)
    {
        return Owner.IsEmpireHostile(enemy)
            ? GetStrength(FindClusters(enemy, pos, radius))
            : 0f;
    }

    /// <summary> Get all strength of all hostiles in a system</summary>
    public float GetHostileStrengthAt(Vector2 pos, float radius)
    {
        return GetStrength(FindHostileClusters(pos, radius));
    }

    record struct ThreatAggregate(Empire Loyalty, float Strength);

    public Empire GetStrongestHostileAt(SolarSystem s)
    {
        return GetStrongestHostileAt(s.Position, s.Radius);
    }

    /// <summary>
    /// Returns the strongest empire at pos+radius. EXCLUDING US
    /// </summary>
    public Empire GetStrongestHostileAt(Vector2 pos, float radius)
    {
        ThreatCluster[] clusters = FindHostileClusters(pos, radius);
        if (clusters.Length == 0) return null;
        if (clusters.Length == 1) return clusters[0].Loyalty;

        var strengths = new ThreatAggregate[Owner.Universe.NumEmpires];
        foreach (ThreatCluster c in clusters)
        {
            ref ThreatAggregate aggregate = ref strengths[c.Loyalty.Id - 1];
            aggregate.Loyalty = c.Loyalty;
            aggregate.Strength += c.Strength;
        }
        return strengths.FindMax(c => c.Strength).Loyalty;
    }

    /// <summary>
    /// Gets all Faction clusters with a station
    /// </summary>
    public ThreatCluster[] GetAllFactionBases()
    {
        return AllClusters.Filter(c => c.HasStarBases && c.Loyalty.IsFaction);
    }
        
    // TODO: Maybe add clusters-by-system?
    public SolarSystem[] GetAllSystemsWithFactions()
    {
        return AllClusters.FilterSelect(
            c => c.System != null && c.Loyalty.IsFaction && c.Loyalty.IsEmpireHostile(Owner),
            c => c.System);
    }
        
    /// <summary>
    /// Gets the known strength for an empire
    /// </summary>
    public float KnownEmpireStrength(Empire empire)
    {
        // TODO: Maybe add clusters-by-faction ?
        float strength = 0f;
        ThreatCluster[] clusters = AllClusters;
        for (int i = 0; i < clusters.Length; ++i) // NOTE: using a raw loop for performance
        {
            ThreatCluster c = clusters[i];
            if (c.Loyalty == empire)
                strength += c.Strength;
        }
        return strength;
    }
    
    /// <summary>
    /// Gets the total strength for an empire's ThreatClusters
    /// which are within our Borders
    /// </summary>
    public float KnownEmpireStrengthInBorders(Empire empire)
    {
        float strength = 0f;
        ThreatCluster[] clusters = AllClusters;
        for (int i = 0; i < clusters.Length; ++i)
        {
            ThreatCluster cluster = clusters[i];
            if (cluster.Loyalty == empire && cluster.InBorders)
                strength += cluster.Strength;
        }
        return strength;
    }

    // This should realistically only get called once, when DiplomacyScreen is opened
    public void GetTechsFromPins(HashSet<string> techs, Empire empire)
    {
        foreach (ThreatCluster c in AllClusters)
        {
            if (c.Loyalty == empire)
            {
                foreach (Ship s in c.Ships)
                    techs.UnionWith(s.ShipData.TechsNeeded); // SLOW !!
            }
        }
    }
}