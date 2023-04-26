using System;
using Ship_Game.Ships;
using SDGraphics;
using Ship_Game.Empires.Components;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Universe;
using Ship_Game.AI;
using System.Collections.Generic;

namespace Ship_Game;

public sealed partial class Empire
{
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

    /// <summary>
    /// How often the ThreatMatrix is updated.
    /// It's not necessary to do it every frame.
    /// Somewhere around 0.5 - 1.0 seconds should be good enough
    /// </summary>
    const float ResetThreatMatrixSeconds = 1.0f;

    const float ResetPlayerProjectorScanSeconds = 5.0f;
        
    public EmpireFirstContact FirstContact = new();

    readonly Array<InfluenceNode> OurBorderSystems = new(); // all of our systems
    readonly Array<InfluenceNode> OurBorderPlanets = new(); // all of our planets
    readonly Array<InfluenceNode> OurBorderShips = new(); // SSP-s and some Bases

    readonly Array<InfluenceNode> OurSensorPlanets = new(); // all of our planets
    readonly Array<InfluenceNode> OurSensorShips = new(); // all ships

    readonly Array<InfluenceNode> TempBorderNodes = new();
    readonly Array<InfluenceNode> TempSensorNodes = new();

    public InfluenceNode[] BorderNodes = Empty<InfluenceNode>.Array;
    public InfluenceNode[] SensorNodes = Empty<InfluenceNode>.Array;
    public BorderNodeCache BorderNodeCache = new();
    
    void ClearInfluenceList()
    {
        OurSensorShips.Clear();
        OurSensorPlanets.Clear();
        OurBorderShips.Clear();
        OurBorderPlanets.Clear();

        BorderNodes = Empty<InfluenceNode>.Array;
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

    // For Player Projectors to display enemy empire flags if found in projection radius
    void UpdatePlayerProjectorScan()
    {
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
            OurBorderShips.Add(new(ship, GetProjectorRadius(), known));
        }

        // all ships/stations/SSP-s are sensor nodes
        OurSensorShips.Add(new(ship, ship.SensorRange, known));
        return isBorderNode;
    }
        
    public void AddBorderNode(Planet planet)
    {
        bool empireKnown = IsThisEmpireKnownByPlayer();
        bool known = empireKnown || planet.IsExploredBy(Universe.Player);

        // NOTE: planets always provide influence
        OurBorderPlanets.Add(new(planet, planet.GetProjectorRange(), known));
        OurSensorPlanets.Add(new(planet, planet.SensorRange, empireKnown));
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
        else if (source is Planet)
        {
            RemoveBorderNode(source, OurBorderPlanets);
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

    void UpdateSensorAndBorderRadiuses()
    {
        ForceUpdateSensorRadiuses = false;

        bool useSensorRange = WeArePirates || WeAreRemnants;
        float projectorRadius = GetProjectorRadius();

        Span<InfluenceNode> sensorShips = OurSensorShips.AsSpan();
        Span<InfluenceNode> sensorPlanets = OurSensorPlanets.AsSpan();
        Span<InfluenceNode> borderShips = OurBorderShips.AsSpan();
        Span<InfluenceNode> borderPlanets = OurBorderPlanets.AsSpan();

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
            n.Radius = useSensorRange ? ((Ship)n.Source).SensorRange : projectorRadius;
        }
        foreach (ref InfluenceNode n in borderPlanets)
        {
            n.Radius = ((Planet)n.Source).GetProjectorRange();
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
        Span<InfluenceNode> borderPlanets = OurBorderPlanets.AsSpan();

        Span<SolarSystem> systems = OwnedSolarSystems.AsSpan();
        OurBorderSystems.Clear();
        OurBorderSystems.Resize(systems.Length);
        Span<InfluenceNode> borderSystems = OurBorderSystems.AsSpan();
        for (int i = 0; i < borderSystems.Length; ++i)
        {
            SolarSystem system = systems[i];
            ref InfluenceNode sn = ref borderSystems[i];
            sn.Position = system.Position;
            sn.Radius = 5000;
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
            foreach (ref InfluenceNode n in borderPlanets)
            {
                n.Position = n.Source.Position;
                n.KnownToPlayer = true;

                int whichSystem = OwnedSolarSystems.IndexOfRef(((Planet)n.Source).System);
                ref InfluenceNode sn = ref borderSystems[whichSystem];
                sn.Radius = Math.Max(sn.Radius, n.Radius);
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
            foreach (ref InfluenceNode n in borderPlanets)
            {
                n.Position = n.Source.Position;
                n.KnownToPlayer = ((Planet)n.Source).IsExploredBy(player);

                int whichSystem = OwnedSolarSystems.IndexOfRef(((Planet)n.Source).System);
                ref InfluenceNode sn = ref borderSystems[whichSystem];
                sn.Radius = Math.Max(sn.Radius, n.Radius);

                sn.KnownToPlayer |= n.KnownToPlayer;
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
        TempBorderNodes.AddRange(OurBorderSystems);
        TempBorderNodes.AddRange(OurBorderShips);
        BorderNodes = TempBorderNodes.ToArray();
        TempBorderNodes.Clear();

        UpdateOurSensorNodes();
        TempSensorNodes.AddRange(OurSensorPlanets);
        TempSensorNodes.AddRange(OurSensorShips);
        AddSensorsFromAllies(TempSensorNodes);
        AddSensorsFromMoles(TempSensorNodes);
        SensorNodes = TempSensorNodes.ToArray();
        TempSensorNodes.Clear();
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
        foreach (Mole mole in data.MoleList)
        {
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