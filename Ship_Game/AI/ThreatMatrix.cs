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
    /// <summary>
    /// The empire which owns this ThreatMatrix
    /// </summary>
    [StarData] public readonly Empire Owner;

    /// <summary>
    /// OUR observed clusters, always up-to-date.
    /// This is thread-safe and updated atomically as a COPY
    /// </summary>
    [StarData] public ThreatCluster[] OurClusters { get; private set; } = Empty<ThreatCluster>.Array;

    /// <summary>
    /// RIVALS observed cluster, Ships list is cleared periodically to avoid stale entries.
    /// This is thread-safe and updated atomically as a COPY
    /// </summary>
    [StarData] public ThreatCluster[] RivalClusters { get; private set; } = Empty<ThreatCluster>.Array;

    /// <summary>
    /// Qtree for quickly finding nearby clusters
    /// </summary>
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
        ClustersMap = new(us.UniverseWidth, cellThreshold:16, smallestCell:8000);
        foreach (ThreatCluster c in OurClusters)
            ClustersMap.Insert(c);
        foreach (ThreatCluster c in RivalClusters)
            ClustersMap.Insert(c);
    }

    ThreatCluster[] FindClusters(ref SearchOptions opt)
    {
        return ClustersMap.Find<ThreatCluster>(ref opt);
    }

    /// <summary>
    /// Find clusters at pos+radius of a single Empire
    /// </summary>
    public ThreatCluster[] FindClusters(Empire empire, Vector2 pos, float radius)
    {
        SearchOptions opt = new(pos, radius, GameObjectType.ThreatCluster)
        {
            OnlyLoyalty = empire
        };
        return FindClusters(ref opt);
    }

    /// <summary>
    /// Find HOSTILE clusters at pos+radius
    /// </summary>
    public ThreatCluster[] FindHostileClusters(Vector2 pos, float radius)
    {
        SearchOptions opt = new(pos, radius)
        {
            ExcludeLoyalty = Owner,
            Type = GameObjectType.ThreatCluster,
            FilterFunction = (o) => ((ThreatCluster)o).IsHostileTo(Owner)
        };
        return FindClusters(ref opt);
    }

    // Find all enemy clusters within radius
    public ThreatCluster[] FindHostileClustersByDist(Vector2 pos, float radius)
    {
        SearchOptions opt = new(pos, radius)
        {
            ExcludeLoyalty = Owner,
            Type = GameObjectType.ThreatCluster,
            SortByDistance = true,
            FilterFunction = (o) => ((ThreatCluster)o).IsHostileTo(Owner)
        };
        return FindClusters(ref opt);
    }

    static float GetStrength(ThreatCluster[] clusters)
    {
        float strength = 0f;
        for (int i = 0; i < clusters.Length; ++i) // PERF: using for loop instead of lambdas
            strength += clusters[i].Strength;
        return strength;
    }

    static float GetStrengthNoResearchStations(ThreatCluster[] clusters)
    {
        float strength = 0f;
        for (int i = 0; i < clusters.Length; ++i) // PERF: using for loop instead of lambdas
            strength += clusters[i].StrengthNoResearchStations;
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

    /// <summary> Get all strength of all hostiles in a system without Research Stations Strength</summary>
    public float GetHostileStrengthNoResearchStationsAt(Vector2 pos, float radius)
    {
        return GetStrengthNoResearchStations(FindHostileClusters(pos, radius));
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
    /// Gets all Rival Faction clusters with a station
    /// </summary>
    public ThreatCluster[] GetAllFactionBases()
    {
        return RivalClusters.Filter(c => c.HasStarBases && c.Loyalty.IsFaction);
    }

    /// <summary>
    /// Gets all systems where rival hostile factions exist
    /// </summary>
    public ICollection<SolarSystem> GetAllSystemsWithFactions()
    {
        HashSet<SolarSystem> systems = new();
        foreach (ThreatCluster c in RivalClusters)
            if (c.System != null && c.Loyalty.IsFaction && c.Loyalty.IsEmpireHostile(Owner))
                systems.Add(c.System);
        return systems;
    }

    /// <summary>
    /// Gets the known strength for an empire
    /// </summary>
    public float KnownEmpireStrength(Empire empire)
    {
        // TODO: Maybe add clusters-by-faction ?
        float strength = 0f;
        ThreatCluster[] clusters = (empire == Owner) ? OurClusters : RivalClusters;
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
        ThreatCluster[] clusters = (empire == Owner) ? OurClusters : RivalClusters;
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
        ThreatCluster[] clusters = (empire == Owner) ? OurClusters : RivalClusters;
        foreach (ThreatCluster c in clusters)
        {
            if (c.Loyalty == empire)
            {
                foreach (Ship s in c.Ships)
                    techs.UnionWith(s.ShipData.TechsNeeded); // SLOW !!
            }
        }
    }

    // When an empire has the Astronomers trait, it needs to be aware of remnant presence
    // in the system so it wont send lone colony ships
    public void UpdateRemnantPresenceAstronomers(SolarSystem s)
    {
        for (int i = 0; i < s.ShipList.Count; i++)
        {
            Ship ship = s.ShipList[i];
            SetSeen(ship, fromBackgroundThread: false);
        }
    }
}