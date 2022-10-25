using Ship_Game.Ships;
using Ship_Game.Spatial;
using SDGraphics;
using Ship_Game.Empires.Components;
using SDUtils;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game;

public sealed partial class Empire
{
    public struct InfluenceNode
    {
        public Vector2 Position;
        public float Radius;
        public GameObject Source; // Planet OR Ship
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
    const float ResetThreatMatrixSeconds = 2;
        
    public EmpireFirstContact FirstContact = new();

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
        if (IsEmpireDead())
            return;

        us.ResetBordersPerf.Start();
        {
            ResetBorders();
        }
        us.ResetBordersPerf.Stop();

        us.ScanFromPlanetsPerf.Start();
        {
            ScanFromAllSensorPlanets();
        }
        us.ScanFromPlanetsPerf.Stop();

        FirstContact.CheckForFirstContacts(this);

        ThreatMatrixUpdateTimer -= timeStep.FixedTime;
        if (ThreatMatrixUpdateTimer <= 0f)
        {
            ThreatMatrixUpdateTimer = ResetThreatMatrixSeconds;

            us.ThreatMatrixPerf.Start();
            AI.ThreatMatrix.Update();
            us.ThreatMatrixPerf.Stop();
        }
    }

    void ScanFromAllSensorPlanets()
    {
        InfluenceNode[] sensorNodes = SensorNodes;
        for (int i = 0; i < sensorNodes.Length; i++)
        {
            ref InfluenceNode node = ref sensorNodes[i];
            if (node.Source is Planet)
                ScanForShipsFromPlanet(node.Position, node.Radius);
        }
    }

    void ScanForShipsFromPlanet(Vector2 pos, float radius)
    {
        // find ships in radius of node.
        SpatialObjectBase[] targets = Universe.Spatial.FindNearby(
            GameObjectType.Ship, pos, radius, maxResults:1024, excludeLoyalty:this
        );

        for (int i = 0; i < targets.Length; i++)
        {
            var maybeEnemy = (Ship)targets[i];
            AI.ThreatMatrix.SetSeen(maybeEnemy);
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

        int nSensorShips = OurSensorShips.Count;
        int nSensorPlanets = OurSensorPlanets.Count;
        int nBorderShips = OurBorderShips.Count;
        int nBorderPlanets = OurBorderPlanets.Count;
        InfluenceNode[] sensorShips = OurSensorShips.GetInternalArrayItems();
        InfluenceNode[] sensorPlanets = OurSensorPlanets.GetInternalArrayItems();
        InfluenceNode[] borderShips = OurBorderShips.GetInternalArrayItems();
        InfluenceNode[] borderPlanets = OurBorderPlanets.GetInternalArrayItems();

        for (int i = 0; i < nSensorShips; ++i)
        {
            ref InfluenceNode n = ref sensorShips[i];
            n.Radius = ((Ship)n.Source).SensorRange;
        }
        for (int i = 0; i < nSensorPlanets; ++i)
        {
            ref InfluenceNode n = ref sensorPlanets[i];
            n.Radius = ((Planet)n.Source).SensorRange;
        }
        for (int i = 0; i < nBorderShips; ++i)
        {
            ref InfluenceNode n = ref borderShips[i];
            n.Radius = useSensorRange ? ((Ship)n.Source).SensorRange : projectorRadius;
        }
        for (int i = 0; i < nBorderPlanets; ++i)
        {
            ref InfluenceNode n = ref borderPlanets[i];
            n.Radius = ((Planet)n.Source).GetProjectorRange();
        }
    }

    void UpdateOurSensorNodes()
    {
        bool knownToPlayer = IsThisEmpireWellKnownByPlayer();

        int nSensorShips = OurSensorShips.Count;
        int nSensorPlanets = OurSensorPlanets.Count;
        InfluenceNode[] sensorShips = OurSensorShips.GetInternalArrayItems();
        InfluenceNode[] sensorPlanets = OurSensorPlanets.GetInternalArrayItems();

        for (int i = 0; i < nSensorShips; ++i)
        {
            ref InfluenceNode n = ref sensorShips[i];
            n.Position = n.Source.Position;
            n.KnownToPlayer = knownToPlayer;
        }
        for (int i = 0; i < nSensorPlanets; ++i)
        {
            ref InfluenceNode n = ref sensorPlanets[i];
            n.Position = n.Source.Position;
            n.KnownToPlayer = knownToPlayer;
        }
    }

    void UpdateOurBorderNodes()
    {
        bool knownToPlayer = IsThisEmpireKnownByPlayer();

        int nBorderShips = OurBorderShips.Count;
        int nBorderPlanets = OurBorderPlanets.Count;
        InfluenceNode[] borderShips = OurBorderShips.GetInternalArrayItems();
        InfluenceNode[] borderPlanets = OurBorderPlanets.GetInternalArrayItems();

        if (knownToPlayer)
        {
            for (int i = 0; i < nBorderShips; ++i)
            {
                ref InfluenceNode n = ref borderShips[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = true;
            }
            for (int i = 0; i < nBorderPlanets; ++i)
            {
                ref InfluenceNode n = ref borderPlanets[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = true;
            }
        }
        else
        {
            var player = Universe.Player;
            for (int i = 0; i < nBorderShips; ++i)
            {
                ref InfluenceNode n = ref borderShips[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = ((Ship)n.Source).InPlayerSensorRange;
            }
            for (int i = 0; i < nBorderPlanets; ++i)
            {
                ref InfluenceNode n = ref borderPlanets[i];
                n.Position = n.Source.Position;
                n.KnownToPlayer = ((Planet)n.Source).IsExploredBy(player);
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
        UpdateOurSensorNodes();
        UpdateOurBorderNodes();

        TempBorderNodes.AddRange(OurBorderPlanets);
        TempBorderNodes.AddRange(OurBorderShips);
        TempSensorNodes.AddRange(OurSensorPlanets);
        TempSensorNodes.AddRange(OurSensorShips);
        AddSensorsFromAllies(TempSensorNodes);
        AddSensorsFromMoles(TempSensorNodes);
            
        BorderNodes = TempBorderNodes.ToArray();
        SensorNodes = TempSensorNodes.ToArray();
        TempBorderNodes.Clear();
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