using System;
using System.Collections.Generic;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Empires.Components;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Universe;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using System.Threading.Tasks;

namespace Ship_Game;

public sealed partial class Empire
{
    public const float InitialBorderRadiusMultiplier = 3f;
    public const float MatureBorderRadiusMultiplier = 4.375f;
    public const float BorderShapeMaxVariation = 0.055f;
    public const float GaussianBorderThreshold = 0.60653066f; // exp(-0.5), boundary at one radius
    [StarData] public bool InfluenceActive { get; private set; } = true; // New espionage can disable Influence
    [StarData] public int InfluenceDisableChance { get; private set; }
    public struct InfluenceNode
    {
        public Vector2 Position;
        public float Radius;
        public GameObject Source; // Planet OR Ship OR System
        public bool KnownToPlayer;
        public InfluenceNode(GameObject source, float radius, bool knowToPlayer)
        {
            Position = source.Position;
            Radius = radius;
            Source = source;
            KnownToPlayer = knowToPlayer;
        }
    }

    float ThreatMatrixUpdateTimer;
    float PlayerProjectorScanTimer;
    int BorderConnectionUpdateCountdown;
    int BorderConnectionNodeCount = -1;
    int BorderTopologySignature;
    int PendingBorderTopology;
    int BorderGeneration;
    int PendingBorderGeneration;
    int BorderGeometrySignature;
    int PendingBorderGeometry;
    Task<BorderSnapshot> PendingBorders;
    internal volatile BorderSnapshot PreparedBorders;

    /// <summary>
    /// How often the ThreatMatrix is updated.
    /// It's not necessary to do it every frame.
    /// Somewhere around 0.5 - 1.0 seconds should be good enough
    /// </summary>
    const float ResetThreatMatrixSeconds = 1.0f;

    const float ResetPlayerProjectorScanSeconds = 5.0f;
    // Topology changes schedule a build immediately; gradual radius/position changes are
    // batched to avoid repeatedly rebuilding the connection graph.
    const int BorderConnectionUpdateIntervalTicks = 300;
        
    public EmpireFirstContact FirstContact = new();

    public bool HasBorderAccessTo(Empire borderOwner, bool civilianFreighter = false)
    {
        if (borderOwner == null || borderOwner == this || WeArePirates || borderOwner.IsDefeated
            || !borderOwner.InfluenceActive || !IsKnown(borderOwner))
            return true;
        if (IsAtWarWith(borderOwner) || IsAlliedWith(borderOwner)
            || IsOpenBordersTreaty(borderOwner))
            return true;
        return civilianFreighter && IsTradeTreaty(borderOwner);
    }

    readonly Array<InfluenceNode> OurBorderSystems = new(); // all of our systems
    readonly Array<InfluenceNode> OurBorderShips = new(); // SSP-s and some Bases

    readonly Array<InfluenceNode> OurSensorPlanets = new(); // all of our planets
    readonly Array<InfluenceNode> OurSensorShips = new(); // all ships

    readonly Array<InfluenceNode> TempBorderNodes = new();
    readonly Array<InfluenceNode> TempSensorNodes = new();

    public InfluenceNode[] BorderNodes = Empty<InfluenceNode>.Array;
    public InfluenceNode[] SensorNodes = Empty<InfluenceNode>.Array;
    // Authoritative bridge topology shared by rendering and movement rules.
    // Published as an immutable snapshot because simulation updates borders while
    // the main thread renders them. Never mutate an array after assigning it here.
    public InfluenceConnection[] BorderConnections = Empty<InfluenceConnection>.Array;
    public BorderNodeCache BorderNodeCache = new();
    
    void ClearInfluenceList()
    {
        OurSensorShips.Clear();
        OurSensorPlanets.Clear();
        OurBorderShips.Clear();

        BorderNodes = Empty<InfluenceNode>.Array;
        BorderConnections = Empty<InfluenceConnection>.Array;
        BorderConnectionNodeCount = -1;
        BorderConnectionUpdateCountdown = 0;
        PreparedBorders = null;
        BorderTopologySignature = 0;
        ++BorderGeneration;
        SensorNodes = Empty<InfluenceNode>.Array;
        TempSensorNodes.Clear();
        TempBorderNodes.Clear();
    }

    public void UpdateContactsAndBorders(UniverseScreen us, FixedSimTime timeStep)
    {
        if (IsDefeated)
            return;

        us.ResetBordersPerf.Start();
        {
            ResetBorders();
        }
        us.ResetBordersPerf.Stop();

        us.ScanFromPlanetsPerf.Start();
        {
            // this will add SetSeen entries to ThreatMatrix
            ScanFromAllSensorPlanets(AI.ThreatMatrix);
        }
        us.ScanFromPlanetsPerf.Stop();

        FirstContact.CheckForFirstContacts(this);

        ThreatMatrixUpdateTimer -= timeStep.FixedTime;
        if (ThreatMatrixUpdateTimer <= 0f)
        {
            ThreatMatrixUpdateTimer = ResetThreatMatrixSeconds;

            // Political territory provides complete strategic surveillance, not
            // merely the smaller hardware sensor bubbles around individual assets.
            ScanFromOwnedBorders(AI.ThreatMatrix);

            us.ThreatMatrixPerf.Start();
            AI.ThreatMatrix.Update(new(time:ResetThreatMatrixSeconds));
            us.ThreatMatrixPerf.Stop();
        }

        if (isPlayer)
        {
            PlayerProjectorScanTimer -= timeStep.FixedTime;
            if (PlayerProjectorScanTimer <= 0)
            {
                PlayerProjectorScanTimer = ResetPlayerProjectorScanSeconds;

                us.PlayerProjectorScanPerf.Start();
                UpdatePlayerProjectorScan();
                us.PlayerProjectorScanPerf.Stop();
            }
        }
    }

    void SetInfluenceActive(bool value)
    {
        InfluenceActive = value;
        if (!value)
            ThreatDetector.Clear();
    }

    public void SetInfluenceDisableChance(int value)
    {
        InfluenceDisableChance = value;
    }

    void TryDisableInfluence()
    {
        SetInfluenceActive(true);
        if (InfluenceDisableChance == 0)
            return;

        SetInfluenceActive(Random.RollDice(InfluenceDisableChance));
        InfluenceDisableChance -= 1;
        if (InfluenceDisableChance == 0)
            Universe.Notifications.AddAgentResult(good: true, Localizer.Token(GameText.SubspaceProjectionStableAgain), this);
    }

    // For Player Projectors to display enemy empire flags if found in projection radius
    void UpdatePlayerProjectorScan()
    {
        if (!InfluenceActive)
            return;

        var playerProjectors = OwnedProjectors;
        float scanRadius = GetProjectorRadius();
        for (int i = 0; i < playerProjectors.Count; i++)
        {
            Ship projector = playerProjectors[i];
            projector.AI.ProjectorScan(scanRadius, ResetPlayerProjectorScanSeconds);
        }
    }

    void ScanFromAllSensorPlanets(ThreatMatrix threatMatrix)
    {
        InfluenceNode[] sensorNodes = SensorNodes;
        for (int i = 0; i < sensorNodes.Length; i++)
        {
            ref InfluenceNode node = ref sensorNodes[i];
            if (node.Source is Planet p)
                ScanForShipsFromPlanet(p, node.Position, node.Radius, threatMatrix);
        }
    }

    void ScanForShipsFromPlanet(Planet p, Vector2 pos, float radius, ThreatMatrix threatMatrix)
    {
        // TODO: sort System.ShipList into per-empire basis
        Array<Ship> ships = p.System.ShipList;
        int count = ships.Count;
        if (count == 0) // micro-optimization to avoid scanning empty systems
            return;

        // This loop ended up being much faster than Spatial.FindNearby
        Ship[] items = ships.GetInternalArrayItems();
        for (int i = 0; i < count; ++i)
        {
            Ship maybeEnemy = items[i];
            if (maybeEnemy.Loyalty != this && maybeEnemy.Position.InRadius(pos, radius))
            {
                threatMatrix.SetSeen(maybeEnemy, fromBackgroundThread: false);
            }
        }
    }

    void ScanFromOwnedBorders(ThreatMatrix threatMatrix)
    {
        if (!InfluenceActive)
            return;

        Ship[] ships = Universe.Ships;
        for (int i = 0; i < ships.Length; ++i)
        {
            Ship ship = ships[i];
            if (ship == null || !ship.Active || ship.Dying || ship.Loyalty == this
                || !IsInBorderTerritory(ship.Position))
                continue;

            threatMatrix.SetSeen(ship, fromBackgroundThread: false);
            // The scan runs once per second. A little grace prevents a one-frame
            // visibility flicker from update-order differences at the interval.
            ship.KnownByEmpires.SetSeen(this, ResetThreatMatrixSeconds + 0.25f);
            if (AlliedWithPlayer)
                ship.KnownByEmpires.SetSeen(Universe.Player, ResetThreatMatrixSeconds + 0.25f);
            if (!IsKnown(ship.Loyalty))
                FirstContact.SetReadyForContact(ship.Loyalty);
        }

        // Territory also explores celestial objects enclosed by it.
        IReadOnlyList<SolarSystem> systems = Universe.Systems;
        for (int i = 0; i < systems.Count; ++i)
        {
            SolarSystem system = systems[i];
            if (!IsInBorderTerritory(system.Position))
                continue;
            system.SetExploredBy(this);
            for (int p = 0; p < system.PlanetList.Count; ++p)
                system.PlanetList[p].SetExploredBy(this);
        }
    }

    public bool IsBorderNode(Ship ship)
    {
        return ship.IsSubspaceProjector
               || (WeAreRemnants && ship.Name == data.RemnantPortal)
               || (WeArePirates && Pirates.IsBase(ship));
    }

    // @return True if ship is a border node which provides influence in InfluenceTree
    public bool AddBorderNode(Ship ship)
    {
        bool known = IsShipKnownToPlayer(ship);
        bool isBorderNode = IsBorderNode(ship);
        if (isBorderNode)
        {
            OurBorderShips.Add(new(ship, GetStaticBorderInfluenceRadius(), known));
        }

        // all ships/stations/SSP-s are sensor nodes
        OurSensorShips.Add(new(ship, ship.SensorRange, known));
        return isBorderNode;
    }
        
    public void AddBorderNode(Planet planet)
    {
        bool empireKnown = IsThisEmpireKnownByPlayer();
        bool known = empireKnown || planet.IsExploredBy(Universe.Player);

        // NOTE: planets always provide influence and actually project it from system center.
        if (!IsSystemInOurBorderSystems(planet.System))
            OurBorderSystems.Add(new(planet.System, GetSystemInfluenceRadius(planet.System), known));

        OurSensorPlanets.Add(new(planet, planet.SensorRange, empireKnown));
    }

    public bool IsSystemInOurBorderSystems(SolarSystem system)
    {
        return OurBorderSystems.Any(n => n.Source == system);
    }

    /// <summary>
    /// Mature population projects more political/economic influence than a new
    /// outpost. Growth is deliberately sub-linear so a large ecumenopolis expands
    /// its frontier without dwarfing several ordinary colonies.
    /// </summary>
    public float GetSystemInfluenceRadius(SolarSystem system)
    {
        float growth = GetSystemInfluenceGrowth(system);
        // A fresh colony starts at 3x projector range. Population contributes
        // the reduced expansion amount, approaching 4.375x at maturity.
        float populationModifier = InitialBorderRadiusMultiplier
                                 + growth * (MatureBorderRadiusMultiplier - InitialBorderRadiusMultiplier);
        return GetProjectorRadius() * populationModifier;
    }

    public float GetStaticBorderInfluenceRadius()
        => GetProjectorRadius() * InitialBorderRadiusMultiplier;

    public float GetSystemInfluenceGrowth(SolarSystem system)
    {
        float populationBillions = 0f;
        for (int i = 0; i < system.PlanetList.Count; ++i)
        {
            Planet planet = system.PlanetList[i];
            if (planet.Owner == this)
                populationBillions += planet.PopulationBillion;
        }

        // Smooth asymptotic growth avoids sudden border jumps at population
        // thresholds. Roughly 63% mature at 4B and 92% mature at 10B.
        return 1f - (float)Math.Exp(-populationBillions / 4f);
    }

    public float GetBorderNodeGrowth(in InfluenceNode node)
    {
        return node.Source is SolarSystem system ? GetSystemInfluenceGrowth(system) : 1f;
    }

    /// <summary>A stable signed field for an organically curved node. Positive is
    /// inside. Harmonic phases depend only on persistent IDs, so outlines are
    /// identical after loading and do not animate or flicker.</summary>
    public float GetBorderNodeField(in InfluenceNode node, Vector2 point)
    {
        Vector2 offset = point - node.Position;
        float angle = (float)Math.Atan2(offset.Y, offset.X);
        float phase = node.Source.Id * 0.754877666f + Id * 1.618033989f;
        // Most variation lives in broad 2/3-lobe harmonics. The small 5-lobe
        // component keeps individuality without producing star-like cavities.
        float deformation = 0.032f * (float)Math.Sin(angle * 2f + phase)
                          + 0.017f * (float)Math.Sin(angle * 3f - phase * 0.73f)
                          + 0.006f * (float)Math.Sin(angle * 5f + phase * 1.37f);
        float organicRadius = node.Radius * (1f + deformation);
        return organicRadius - offset.Length();
    }

    /// <summary>The half-width of the territorial corridor joining two nodes.
    /// Connections are slightly wider than their endpoints and bulge at the
    /// midpoint, closing U-shaped notches while retaining a rounded silhouette.</summary>
    public float GetBorderConnectionRadius(in InfluenceConnection connection)
    {
        float maturity = Math.Min(GetBorderNodeGrowth(connection.Node1),
                                  GetBorderNodeGrowth(connection.Node2));
        return Math.Min(connection.Node1.Radius, connection.Node2.Radius)
             * (1.02f + maturity * 0.03f) * 1.12f;
    }

    public float GetBorderConnectionRadiusAt(in InfluenceConnection connection, float amount)
    {
        float maturity = Math.Min(GetBorderNodeGrowth(connection.Node1),
                                  GetBorderNodeGrowth(connection.Node2));
        float widthFactor = 1.02f + maturity * 0.03f;
        float radius1 = connection.Node1.Radius * widthFactor;
        float radius2 = connection.Node2.Radius * widthFactor;
        float t = amount.Clamped(0f, 1f);
        float taperedRadius = radius1 + (radius2 - radius1) * t;
        float roundedBulge = 1f + 0.12f * (float)Math.Sin(Math.PI * t);
        return taperedRadius * roundedBulge;
    }

    public float GetBorderNodeGaussianInfluence(in InfluenceNode node, Vector2 point)
    {
        Vector2 offset = point - node.Position;
        float cutoff = node.Radius * (1f + BorderShapeMaxVariation) * 3f;
        if (offset.SqLen() >= cutoff * cutoff)
            return 0f;
        float distance = offset.Length();
        // Signed field = organic radius - distance.
        float organicRadius = (distance + GetBorderNodeField(node, point)).LowerBound(1f);
        float normalized = distance / organicRadius;
        float normalized2 = normalized * normalized;
        return normalized2 >= 9f ? 0f : (float)Math.Exp(-0.5f * normalized2);
    }

    public float GetBorderConnectionGaussianInfluence(in InfluenceConnection connection, Vector2 point)
    {
        Vector2 a = connection.Node1.Position;
        Vector2 ab = connection.Node2.Position - a;
        float length2 = ab.SqLen();
        float t = length2 < 1f ? 0f : ((point - a).Dot(ab) / length2).Clamped(0f, 1f);
        float radius = GetBorderConnectionRadiusAt(connection, t).LowerBound(1f);
        float normalized = point.Distance(a + ab * t) / radius;
        float normalized2 = normalized * normalized;
        return normalized2 >= 9f ? 0f : (float)Math.Exp(-0.5f * normalized2);
    }

    public static void AccumulateGaussianInfluence(ref float sumFourthPowers, float influence)
    {
        float squared = influence * influence;
        sumFourthPowers += squared * squared;
    }

    public static float CombineGaussianInfluence(float sumFourthPowers)
        => (float)Math.Sqrt(Math.Sqrt(sumFourthPowers));

    public float GetBorderClaimStrength(Vector2 point)
    {
        BorderSnapshot prepared = PreparedBorders;
        if (prepared != null)
        {
            float raw = prepared.Field.Strength(point, smooth: false);
            float smooth = prepared.Field.Strength(point);
            if (raw < 0f && smooth >= 0f)
            {
                // Closing a cavity must not claim a rival's existing enclave.
                foreach (Empire rival in Universe.Empires)
                {
                    if (rival == this || rival.IsDefeated || !rival.InfluenceActive) continue;
                    float rivalRaw = rival.PreparedBorders?.Field.Strength(point, smooth: false)
                                  ?? rival.GetRawBorderClaimStrength(point);
                    if (rivalRaw >= 0f && !BordersOverlapAt(rival, point)) return raw;
                }
            }
            return smooth;
        }
        return GetRawBorderClaimStrength(point);
    }

    float GetRawBorderClaimStrength(Vector2 point)
    {
        float influence = 0f;
        InfluenceNode[] nodes = BorderNodes;
        InfluenceConnection[] connections = BorderConnections;
        for (int i = 0; i < nodes.Length; ++i)
        {
            ref InfluenceNode node = ref nodes[i];
            AccumulateGaussianInfluence(ref influence, GetBorderNodeGaussianInfluence(node, point));
        }

        foreach (InfluenceConnection connection in connections)
            AccumulateGaussianInfluence(ref influence,
                GetBorderConnectionGaussianInfluence(connection, point));

        return CombineGaussianInfluence(influence) - GaussianBorderThreshold;
    }

    public bool IsInBorderTerritory(Vector2 point)
    {
        float ourStrength = GetBorderClaimStrength(point);
        if (ourStrength < 0f)
            return false;

        foreach (Empire other in Universe.Empires)
        {
            if (other == this || other.IsDefeated || !other.InfluenceActive)
                continue;
            float otherStrength = other.GetBorderClaimStrength(point);
            if (otherStrength >= 0f && BordersOverlapAt(other, point))
                continue;
            if (otherStrength > ourStrength + 0.001f
                || Math.Abs(otherStrength - ourStrength) <= 0.001f && other.Id < Id)
                return false;
        }
        return true;
    }

    /// <summary>Hostile occupation and colonies shared inside one solar system
    /// produce contested overlap instead of one influence field cutting a hole
    /// out of the other. Peaceful borders elsewhere still split normally.</summary>
    public bool BordersOverlapAt(Empire other, Vector2 point)
    {
        if (other == null || other == this)
            return false;
        if (WeArePirates || other.WeArePirates || WeAreRemnants || other.WeAreRemnants
            || IsAtWarWith(other))
            return true;

        InfluenceNode[] nodes = PreparedBorders?.Nodes ?? BorderNodes;
        for (int i = 0; i < nodes.Length; ++i)
        {
            ref InfluenceNode node = ref nodes[i];
            if (node.Source is not SolarSystem system || GetBorderNodeField(node, point) < 0f)
                continue;

            bool ours = false;
            bool theirs = false;
            for (int p = 0; p < system.PlanetList.Count && !(ours && theirs); ++p)
            {
                Empire owner = system.PlanetList[p].Owner;
                ours |= owner == this;
                theirs |= owner == other;
            }
            if (ours && theirs)
                return true;
        }
        return false;
    }

    // @return True if source is a border node which provides influence in InfluenceTree
    public bool RemoveBorderNode(GameObject source)
    {
        if (source is Ship s)
        {
            RemoveBorderNode(source, OurBorderShips);
            RemoveBorderNode(source, OurSensorShips);
            return IsBorderNode(s);
        }
        else if (source is Planet p)
        {
            if (!p.System.HasPlanetsOwnedBy(this))
                RemoveBorderNode(source.System, OurBorderSystems);

            RemoveBorderNode(source, OurSensorPlanets);
            return true;
        }
        return false;
    }

    static void RemoveBorderNode(GameObject source, Array<InfluenceNode> nodes)
    {
        int count = nodes.Count;
        InfluenceNode[] rawNodes = nodes.GetInternalArrayItems();
        for (int i = 0; i < count; ++i)
        {
            if (rawNodes[i].Source == source)
            {
                nodes.RemoveAtSwapLast(i);
                break;
            }
        }
    }

    public bool ForceUpdateSensorRadiuses;

    // This is used only when ForceUpdateSensorRadiuses is true, which is rare
    void UpdateSensorAndBorderRadiuses()
    {
        ForceUpdateSensorRadiuses = false; 

        bool useSensorRange = WeArePirates || WeAreRemnants;
        float projectorRadius = GetProjectorRadius();
        float borderProjectorRadius = GetStaticBorderInfluenceRadius();

        Span<InfluenceNode> sensorShips = OurSensorShips.AsSpan();
        Span<InfluenceNode> sensorPlanets = OurSensorPlanets.AsSpan();
        Span<InfluenceNode> borderShips = OurBorderShips.AsSpan();
        Span<InfluenceNode> borderSystems = OurBorderSystems.AsSpan();

        foreach (ref InfluenceNode n in sensorShips)
        {
            n.Radius = ((Ship)n.Source).SensorRange;
        }
        foreach (ref InfluenceNode n in sensorPlanets)
        {
            n.Radius = ((Planet)n.Source).SensorRange;
        }
        foreach (ref InfluenceNode n in borderShips)
        {
            n.Radius = useSensorRange ? ((Ship)n.Source).SensorRange : borderProjectorRadius;
        }
        foreach (ref InfluenceNode n in borderSystems)
        {
            n.Radius = GetSystemInfluenceRadius((SolarSystem)n.Source);
        }
    }

    void UpdateOurSensorNodes()
    {
        bool knownToPlayer = IsThisEmpireWellKnownByPlayer();
        Span<InfluenceNode> sensorShips = OurSensorShips.AsSpan();
        Span<InfluenceNode> sensorPlanets = OurSensorPlanets.AsSpan();

        foreach (ref InfluenceNode n in sensorShips)
        {
            n.Position = n.Source.Position;
            n.KnownToPlayer = knownToPlayer;
        }
        foreach (ref InfluenceNode n in sensorPlanets)
        {
            n.Position = n.Source.Position;
            n.KnownToPlayer = knownToPlayer;
        }
    }

    void UpdateOurBorderNodes()
    {
        bool knownToPlayer = IsThisEmpireKnownByPlayer();

        Span<InfluenceNode> borderShips = OurBorderShips.AsSpan();
        Span<SolarSystem> systems = OwnedSolarSystems.AsSpan();
        OurBorderSystems.Clear();
        OurBorderSystems.Resize(systems.Length);
        Span<InfluenceNode> borderSystems = OurBorderSystems.AsSpan();
        for (int i = 0; i < borderSystems.Length; ++i)
        {
            SolarSystem system = systems[i];
            ref InfluenceNode sn = ref borderSystems[i];
            sn.Position = system.Position;
            sn.Radius = GetSystemInfluenceRadius(system);
            sn.Source = system;
            sn.KnownToPlayer = knownToPlayer;
        }
        
        if (knownToPlayer)
        {
            foreach (ref InfluenceNode n in borderShips)
            {
                n.Position = n.Source.Position;
                n.KnownToPlayer = true;
            }
        }
        else
        {
            var player = Universe.Player;
            foreach (ref InfluenceNode n in borderShips)
            {
                n.Position = n.Source.Position;
                n.KnownToPlayer = ((Ship)n.Source).InPlayerSensorRange;
            }

            for (int i = 0; i < OwnedPlanets.Count; i++)
            {
                Planet p = OwnedPlanets[i];
                int whichSystem = OwnedSolarSystems.IndexOfRef(p.System);
                ref InfluenceNode sn = ref borderSystems[whichSystem];
                sn.KnownToPlayer |= p.IsExploredBy(player);
            }
        }
    }

    bool IsShipKnownToPlayer(Ship ship)
    {
        return ship.InPlayerSensorRange || IsThisEmpireKnownByPlayer();
    }

    bool IsThisEmpireWellKnownByPlayer()
    {
        var us = Universe;
        bool wellKnown = isPlayer
                         || us.Player?.IsAlliedWith(this) == true // support unit tests without Player
                         || us.Debug && (us.Screen.SelectedShip == null || us.Screen.SelectedShip.Loyalty == this);
        return wellKnown;
    }

    bool IsThisEmpireKnownByPlayer()
    {
        return IsThisEmpireWellKnownByPlayer()
               || Universe.Player?.IsTradeOrOpenBorders(this) == true;
    }

    /// <summary>
    /// Border nodes are empire's projector influence from SSP's and Planets
    /// Sensor nodes are used to show the sensor range of things. Ship, planets, spies, etc
    /// </summary>
    void ResetBorders()
    {
        if (ForceUpdateSensorRadiuses)
            UpdateSensorAndBorderRadiuses();

        UpdateOurBorderNodes();
        // TODO: use double-buffered approach here, because # of nodes doesn't always change
        if (InfluenceActive)
        {
            TempBorderNodes.AddRange(OurBorderSystems);
            TempBorderNodes.AddRange(OurBorderShips);
        }

        BorderNodes = TempBorderNodes.ToArray();
        TempBorderNodes.Clear();
        var topology = new HashCode();
        foreach (InfluenceNode node in BorderNodes)
        {
            topology.Add(node.Source.Id);
            topology.Add(node.KnownToPlayer);
        }
        int signature = topology.ToHashCode();
        bool topologyChanged = BorderConnectionNodeCount != BorderNodes.Length || BorderTopologySignature != signature;
        if (topologyChanged)
        {
            BorderTopologySignature = signature;
            ++BorderGeneration;
            BorderConnectionNodeCount = BorderNodes.Length;
            // Keep a valid completed border during additions. Removals or newly
            // hidden nodes invalidate it immediately so lost territory cannot linger.
            if (!CanRetainBorderSnapshot(BorderNodes))
            {
                PreparedBorders = null;
                BorderConnections = Empty<InfluenceConnection>.Array;
            }
            BorderConnectionUpdateCountdown = 0;
        }
        if (PendingBorders?.IsCompleted == true)
        {
            if (PendingBorders.IsCompletedSuccessfully && PendingBorderTopology == signature
                && PendingBorderGeneration == BorderGeneration)
            {
                PreparedBorders = PendingBorders.Result;
                BorderConnections = PreparedBorders.Connections;
                BorderGeometrySignature = PendingBorderGeometry;
                BorderConnectionUpdateCountdown = BorderConnectionUpdateIntervalTicks;
            }
            else if (PendingBorders.IsFaulted)
            {
                Log.Error(PendingBorders.Exception, "Background border geometry failed");
                BorderConnectionUpdateCountdown = 60;
            }
            PendingBorders = null;
        }
        if (PendingBorders == null && BorderConnectionUpdateCountdown-- <= 0)
        {
            InfluenceNode[] nodes = BorderNodes;
            float projectorRadius = GetProjectorRadius();
            var geometry = new HashCode();
            geometry.Add(signature);
            geometry.Add(projectorRadius);
            float quantum = Math.Max(1f, projectorRadius * 0.01f);
            for (int i = 0; i < nodes.Length; ++i)
            {
                geometry.Add((int)(nodes[i].Position.X / quantum));
                geometry.Add((int)(nodes[i].Position.Y / quantum));
                geometry.Add((int)(nodes[i].Radius / quantum));
            }
            int geometrySignature = geometry.ToHashCode();
            if (PreparedBorders != null && BorderGeometrySignature == geometrySignature)
                BorderConnectionUpdateCountdown = BorderConnectionUpdateIntervalTicks;
            else
            {
                var samples = new BorderField.Node[nodes.Length];
                for (int i = 0; i < nodes.Length; ++i)
                    samples[i] = new(nodes[i].Position, nodes[i].Radius,
                        nodes[i].Source.Id * 0.754877666f + Id * 1.618033989f, GetBorderNodeGrowth(nodes[i]));
                PendingBorderTopology = signature;
                PendingBorderGeneration = BorderGeneration;
                PendingBorderGeometry = geometrySignature;
                PendingBorders = BorderWorker.TryRun(() => new BorderSnapshot(nodes, samples, projectorRadius));
            }
        }

        UpdateOurSensorNodes();
        TempSensorNodes.AddRange(OurSensorPlanets);
        TempSensorNodes.AddRange(OurSensorShips);
        AddSensorsFromAllies(TempSensorNodes);
        AddSensorsFromMoles(TempSensorNodes);
        SensorNodes = TempSensorNodes.ToArray();
        TempSensorNodes.Clear();
    }

    bool CanRetainBorderSnapshot(InfluenceNode[] nodes)
    {
        BorderSnapshot previous = PreparedBorders;
        if (previous == null || previous.Nodes.Length > nodes.Length) return false;
        var current = new Dictionary<GameObject, bool>();
        foreach (InfluenceNode node in nodes) current[node.Source] = node.KnownToPlayer;
        foreach (InfluenceNode node in previous.Nodes)
            if (!current.TryGetValue(node.Source, out bool known) || node.KnownToPlayer && !known)
                return false;
        return true;
    }

    void AddSensorsFromAllies(Array<InfluenceNode> sensorNodes)
    {
        bool knownToPlayer = isPlayer;

        foreach (Empire ally in Universe.Empires)
        {
            if (GetRelations(ally, out Relationship relation) && relation.Treaty_Alliance)
            {
                int nSensorShips = ally.OurSensorShips.Count;
                int nSensorPlanets = ally.OurSensorPlanets.Count;
                InfluenceNode[] sensorShips = ally.OurSensorShips.GetInternalArrayItems();
                InfluenceNode[] sensorPlanets = ally.OurSensorPlanets.GetInternalArrayItems();

                for (int i = 0; i < nSensorShips; ++i)
                {
                    InfluenceNode n = sensorShips[i];
                    n.KnownToPlayer |= knownToPlayer;
                    sensorNodes.Add(n);
                }
                for (int i = 0; i < nSensorPlanets; ++i)
                {
                    InfluenceNode n = sensorPlanets[i];
                    n.KnownToPlayer |= knownToPlayer;
                    sensorNodes.Add(n);
                }
            }
        }
    }

    // Moles are spies who have successfully been planted during 'Infiltrate' type missions
    void AddSensorsFromMoles(Array<InfluenceNode> sensorNodes)
    {
        if (data.MoleList.IsEmpty)
            return;

        float projectorRadius = GetProjectorRadius();
        for (int i = 0; i < data.MoleList.Count; i++)
        {
            Mole mole = data.MoleList[i];
            var p = Universe.GetPlanet(mole.PlanetId);
            if (p != null)
            {
                sensorNodes.Add(new InfluenceNode
                {
                    Position = p.Position,
                    Radius = projectorRadius,
                    KnownToPlayer = isPlayer
                });
            }
        }
    }

}
