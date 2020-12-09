using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.AI
{
    public sealed class ThreatMatrix
    {
        Empire Owner;

        public class Pin
        {
            [Serialize(0)] public Vector2 Position;
            [Serialize(1)] public float Strength;
            [Serialize(2)] public string EmpireName;

            /// <summary>
            /// This indicates that the PIN is in borders not the current ship status. 
            /// </summary>
            [Serialize(3)] public bool InBorders;
            [Serialize(4)] public int EmpireId    = 0;
            [Serialize(5)] public Guid SystemGuid = Guid.Empty;
            [Serialize(6)] public Guid PinGuid;
            [XmlIgnore][JsonIgnore] public Ship Ship;
            [XmlIgnore][JsonIgnore] public SolarSystem System { get; private set; }
            
            public void SetSystem(SolarSystem system)
            {
                SystemGuid = system?.guid ?? Guid.Empty;
                System     = system;
            }

            public Pin(Ship ship, bool inBorders)
            {
                Position   = ship.Center;
                Strength   = ship.GetStrength();
                EmpireName = ship.loyalty.data.Traits.Name;
                InBorders  = inBorders;
                Ship       = ship;
                PinGuid    = ship.guid;
                SetSystem(ship.System);
            }

            public Pin(){}

            public void RestoreUnSerializedData(in Guid shipGuid)
            {
                Ship ship = Empire.Universe.Objects.FindShip(shipGuid);
                if (ship == null) return;

                PinGuid = shipGuid;
                Ship    = ship;

                if (SystemGuid != Guid.Empty)
                    System = SolarSystem.GetSolarSystemFromGuid(SystemGuid);
            }

            public Empire GetEmpire()
            {
                if (EmpireId > 0) return EmpireManager.GetEmpireById(EmpireId);
                if (EmpireName.NotEmpty()) return EmpireManager.GetEmpireByName(EmpireName);
                return Ship?.loyalty;
            }

            public void Refresh(Ship ship, bool inSensorRadius, bool shipInBorders)
            {
                if (inSensorRadius)
                {
                    Position   = ship.Center;
                    Strength   = ship.GetStrength();
                    EmpireName = ship.loyalty.data.Traits.Name;
                    EmpireId   = ship.loyalty.Id;
                    InBorders  = shipInBorders;
                    var system = ship.System;
                    SetSystem(system);
                }
                Ship = ship;
            }

            public bool IsPinInRadius(Vector2 point, float radius)
            {
                if (Ship == null) return false;
                return Position.InRadius(point, radius);
            }

            public static Pin FindPinByGuid(Guid pinGuid, Empire empire)
            {
                var pins = empire.GetEmpireAI().ThreatMatrix.GetPins();
                var pin = pins.Find(p => p.PinGuid == pinGuid);
                return pin;
            }

            public Guid GetGuid() => Ship?.guid ?? PinGuid;
        }

        public ThreatMatrix(Empire empire)
        {
            Owner = empire;
        }

        public void SetOwner(Empire owner)
        {
            Owner = owner;
        }

        Pin[] KnownBases = new Pin[0];
        SolarSystem[] KnownSystemsWithEnemies = new SolarSystem[0];
        Map<SolarSystem, Pin[]> SystemThreatMap = new Map<SolarSystem, Pin[]>();
        Map<Empire, Pin[]> KnownEmpireStrengths = new Map<Empire, Pin[]>();

        public Pin[] GetKnownBases() => KnownBases;
        public SolarSystem[] GetHostileSystems() => KnownSystemsWithEnemies;

        public ThreatMatrix(Dictionary<Guid,Pin> matrix, Empire empire)
        {
            Pins = new Map<Guid, Pin>(matrix);
            Owner = empire;
        }
        // not sure we need this.
        readonly ReaderWriterLockSlim PinsMutex = new ReaderWriterLockSlim();
        Map<Guid, Pin> Pins = new Map<Guid, Pin>();
        
        int UpdateTimer = 0;
        int UpdateTickTimerReset = 1 ;

        [XmlIgnore][JsonIgnore] readonly SafeQueue<Action> PendingThreadActions = new SafeQueue<Action>();

        public Pin[] PinValues
        {
            get
            {
                using (PinsMutex.AcquireReadLock())
                {
                    return Pins.Values.ToArray();
                }
            }
        }

        public bool ContainsGuid(Guid guid)
        {
            return Pins.ContainsKey(guid);
        }
        
        public float StrengthOfAllEmpireShipsInBorders(Empire us, Empire them)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.GetEmpire();
                    if (pinEmpire == them && pin.Ship?.IsInBordersOf(us) == true)
                    {
                        if (pin.Ship != null && (pin.Ship.System?.IsOnlyOwnedBy(us) ?? true))
                            str += pin.Strength;
                    }
                }
            }
            return str;
        }

        public float KnownStrengthOfAllEmpireThreats(Empire empire)
        {
            Map<Empire, float> empireStrTable = new Map<Empire, float>();
            for (int i = 0; i < EmpireManager.Empires.Count; ++i)
            {
                empire = EmpireManager.Empires[i];
                empireStrTable.Add(empire, 0);
            }

            foreach (Pin pin in Pins.Values)
            {
                Empire pinEmpire = pin.GetEmpire();
                if (pinEmpire != null && !pinEmpire.WeAreRemnants)
                {
                    float str = empire.IsEmpireAttackable(pinEmpire) ? pin.Strength : pin.Strength / 2;
                    empireStrTable[pinEmpire] += str;
                }
            }

            float knownStr = empireStrTable.Values.Sum();
            return knownStr;
        }

        public float StrengthOfEmpire(Empire empire)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.GetEmpire();
                    if (pinEmpire == empire)
                        str += pin.Strength;
                }
            }
            return str;
        }

        public float StrengthOfEmpireInSystem(Empire empire, SolarSystem system) => GetStrengthInSystem(system, p => p.GetEmpire() == empire);

        public float StrengthOfHostilesInRadius(Empire us, Vector2 center, float radius)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.Ship != null && pin.Position.InRadius(center, radius))
                    {
                        Empire pinEmp = pin.GetEmpire();
                        if (pinEmp != us && (pinEmp.isFaction || us.IsAtWarWith(pinEmp)))
                            str += pin.Strength;
                    }
                }
            }
            return str;
        }
        
        Array<Ship> PingRadarShip(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Ship>();
            var pins = Pins.Values.ToArray();
            for (int i = pins.Length - 1; i >= 0; i--)
            {
                Pin pin = pins[i];
                Ship ship = pin.Ship;
                if (ship != null && position.InRadius(pin.Position, radius) && empire.IsEmpireHostile(ship.loyalty))
                {
                    results.Add(pin.Ship);
                }
            }

            return results;
        }

        Array<Pin> PingRadarPins(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Pin>();
            var pins = Pins.Values.ToArray();
            for (int i = pins.Length - 1; i >= 0; i--)
            {
                Pin pin = pins[i];
                if (position.InRadius(pin.Position, radius) && empire.IsEmpireHostile(pin.GetEmpire()))
                {
                    results.Add(pin);
                }
            }

            return results;
        }

        // Pings for enemy ships, chooses the closest enemy ship
        // and then pings around that ship to create a cluster of enemies
        public Array<Ship> PingRadarClosestEnemyCluster(Vector2 position, float radius, float granularity, Empire empire)
        {
            Array<Pin> pings = GetEnemyPinsInRadius(position, radius, empire);
            if (pings.IsEmpty)
                return new Array<Ship>();

            Pin closest = pings.FindMin(ship => ship.Position.SqDist(position));
            return PingRadarShip(closest.Position, granularity, empire);
        }

        public Map<Vector2, float> PingRadarStrengthClusters(AO ao, float granularity, Empire empire) => PingRadarStrengthClusters(ao.Center, ao.Radius, granularity, empire);
        public Map<Vector2, float> PingRadarStrengthClusters(Vector2 position, float radius, float granularity, Empire empire)
        {
            var retList       = new Map<Vector2, float>();
            Array<Pin> pings  = GetEnemyPinsInRadius(position, radius, empire);
            var filter        = new HashSet<Pin>();

            for (int i = 0; i < pings.Count; i++)
            {
                var ping = pings[i];
                if ((filter.Contains(ping) || retList.ContainsKey(ping.Position)))
                    continue;

                Array<Pin> cluster = PingRadarPins(ping.Position, granularity, empire);
                if (cluster.NotEmpty)
                {
                    retList.Add(ping.Position, cluster.Sum(str => str.Strength));
                    filter.UnionWith(cluster);
                }
            }
            return retList;
        }

        public struct StrengthCluster
        {
            public float Strength;
            public Vector2 Position;
            public float Radius;
            public float Granularity;
            public Empire Empire;
        }

        public StrengthCluster FindLargestStrengthClusterLimited(StrengthCluster strengthCluster, float maxStrength, Vector2 posOffSetInArea)
        {            
            Map<Vector2, float> strengthClusters = PingRadarStrengthClusters(strengthCluster.Position, strengthCluster.Radius,
                strengthCluster.Granularity, strengthCluster.Empire);

            Vector2 clusterPos = strengthClusters
                .FindMaxKeyByValuesFiltered(str => str < maxStrength, str => str);

            if (clusterPos == default)
            {
                strengthCluster.Strength = 0;
                return strengthCluster;
            }

            strengthCluster.Position = clusterPos;
            strengthCluster.Strength  = strengthClusters[clusterPos];
            return strengthCluster;
        }


        public float PingRadarStrengthLargestCluster(Vector2 position, float radius, Empire empire, float granularity = 50000f)
        {
            var filter = new HashSet<Ship>();
            Array<Pin> pings = GetEnemyPinsInRadius(position, radius, empire);
            float largestCluster = 0;

            for (int i = 0; i < pings.Count; i++)
            {
                var pin = pings[i];
                Ship ship = pin.Ship;
                if (ship == null || ship.IsGuardian || filter.Contains(ship))
                    continue;

                Array<Ship> cluster = PingRadarShip(pin.Position, granularity, empire);
                if (cluster.Count != 0)
                {
                    float clusterStrength = cluster.Sum(str => str.GetStrength());
                    if (clusterStrength > largestCluster) largestCluster = clusterStrength;
                    filter.UnionWith(cluster);
                }
            }
            return largestCluster;

        }

        public float PingNetRadarStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength:true);

        public float PingHostileStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength: false, true);
        public float PingHostileStr(AO ao, Empire us) => PingRadarStr(ao.Center, ao.Radius, us, netStrength: false, true);
        public float PingNetHostileStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength: true, true);

        public float PingRadarStr(Vector2 position, float radius, Empire us, bool netStrength = false, bool hostileOnly = false)
        {
            float str = 0f;
            Pin[] pins = GetPins();
            for (int i = 0; i < pins.Length; i++)
            {
                Pin pin = pins[i];
                if (pin?.Position.InRadius(position, radius) == true)
                {
                    Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                    if (!hostileOnly)
                    {
                        str += us.IsEmpireAttackable(pinEmpire) ? pin.Strength : 0;
                    }
                    else
                    {
                        str += us.IsEmpireHostile(pinEmpire) ? pin.Strength : 0;
                    }

                    if (netStrength)
                        str -= pinEmpire == us ? pin.Strength : 0;
                }
            }

            return str;
        }

        public float AnyEnemies(Vector2 position, float radius, Empire us, bool netStrength = false, bool hostileOnly = false)
        {
            float str = 0f;
            Pin[] pins = GetPins();
            for (int i = 0; i < pins.Length; i++)
            {
                Pin pin = pins[i];
                if (pin?.Position.InRadius(position, radius) == true)
                {
                    Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                    if (!hostileOnly)
                    {
                        str += us.IsEmpireAttackable(pinEmpire) ? pin.Strength : 0;
                    }
                    else
                    {
                        str += us.IsEmpireHostile(pinEmpire) ? pin.Strength : 0;
                    }

                    if (netStrength)
                        str -= pinEmpire == us ? pin.Strength : 0;
                }
            }

            return str;
        }


        public bool AnyThreatInSystem(SolarSystem system)
        {
            var b = FindAnyPin(p =>
            {
            if (p.Ship.loyalty.WeArePirates && p.Ship.System == system && p.Strength > 0 && Owner.IsEmpireHostile(p.GetEmpire()))
                    return true;
            return false;

            });
            if (b == null) 
                return false;
            return true;
        }

        /// <summary> ThreadSafe </summary>
        public Pin[] GetAllFactionBases()
        {
            using (PinsMutex.AcquireReadLock())
                return KnownBases.Filter(b => b.GetEmpire().isFaction) ?? Empty<Pin>.Array;
        }

        Pin[] GetAllHostileBases() => FilterPins(p => p.Ship?.IsPlatformOrStation == true && Owner?.IsEmpireHostile(p.GetEmpire()) == true);

        /// <summary> Return the ship strength of filtered ships in a system. It will not tell you if no pins were in the system. </summary>
        public float GetStrengthInSystem(SolarSystem system, Predicate<Pin> filter)
        {
            using (PinsMutex.AcquireReadLock())
                if (SystemThreatMap.TryGetValue(system, out Pin[] pins))
                {
                    return pins.Filter(filter).Sum(p => p.Strength);
                }
            return 0;
        }

        /// <summary>
        /// Returns the strongest empire in this system. Can return null.
        /// </summary>
        /// <returns></returns>
        public Empire GetDominantEmpireInSystem(SolarSystem system)
        {
            using (PinsMutex.AcquireReadLock())
                if (SystemThreatMap.TryGetValue(system, out Pin[] pins))
                {
                    var filteredPins = pins.Filter(p => p.Ship?.Active == true);
                    Map<Empire, float> empires = new Map<Empire, float>();
                    for (int i = 0; i < filteredPins.Length; i++)
                    {

                        Pin pin        = filteredPins[i];
                        Empire loyalty = pin.Ship.loyalty;
                        float str      = pin.Ship.GetStrength();

                        if (empires.ContainsKey(loyalty))
                            empires[loyalty] += str;
                        else if (Owner != loyalty)
                        {
                            var rel = Owner.GetRelations(loyalty);
                            if (rel.IsHostile)
                                empires.Add(loyalty, str);
                        }
                    }

                    return empires.Count == 0 ? null : empires.SortedDescending(s => s.Value).First().Key;
                }

            return null;
        }

        /// <summary> Returns true if there are any pins in the target system </summary>
        public bool AnyKnownThreatsInSystem(SolarSystem system) => SystemThreatMap.Keys.Contains(system);

        public SolarSystem[] GetAllSystemsWithFactions() => GetAllSystemsWith(pins => 
            pins.Any(p=>p.GetEmpire().isFaction && Owner.IsEmpireHostile(p.GetEmpire())));

        SolarSystem[] GetAllSystemsWith(Predicate<Pin[]> filter)
        {
            Array<SolarSystem> systems = new Array<SolarSystem>();
            using (PinsMutex.AcquireReadLock())
            {
                foreach(var kv in SystemThreatMap)
                {
                    if (filter(kv.Value)) 
                        systems.AddUnique(kv.Key);
                }
                return systems.ToArray();
            }
        }

        Map<SolarSystem, Pin[]> GetSystemPinMap()
        {
            var map = new Map<SolarSystem, Array<KeyValuePair<Guid, Pin>>>();
            using (PinsMutex.AcquireReadLock())
                map = Pins.GroupByFiltered(p => p.Value.System,
                p => p.Value.System != null && Owner.IsEmpireHostile(p.Value.GetEmpire()));

            var newMap = new Map<SolarSystem, Pin[]>();
            foreach (var pair in map)
            {
                var key = pair.Key;
                Pin[] values = pair.Value.Select(v=> v.Value);
                newMap.Add(pair.Key, values);
            }
            return newMap;
        }

        public float KnownEmpireStrength(Empire empire, Predicate<Pin> filter)
        {
            float str = 0;
            if (KnownEmpireStrengths.TryGetValue(empire, out var pins))
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    var pin = pins[i];
                    if (filter(pin))
                        str += pin.Strength;
                }
            }

            return str;
        }

        public SolarSystem[] KnownHostileSystems(Predicate<SolarSystem> filter) => KnownSystemsWithEnemies.Filter(filter);

        Map<Empire, Pin[]> GetEmpirePinMap()
        {
            var map = new Map<Empire, Array<KeyValuePair<Guid, Pin>>>();
            using (PinsMutex.AcquireReadLock())
                map = Pins.GroupByFiltered(p => p.Value.GetEmpire(),
                p => p.Value.Strength > 0);

            var newMap = new Map<Empire, Pin[]>();
            foreach (var pair in map)
            {
                var key = pair.Key;
                Pin[] values = pair.Value.Select(v => v.Value);
                newMap.Add(pair.Key, values);
            }
            return newMap;
        }

        public Pin FindAnyPin(Predicate<Pin> predicate)
        {
            using (PinsMutex.AcquireReadLock())
            {
                var pins = Pins.AtomicValuesArray();

                for (int i = 0; i < pins.Length; i++)
                {
                    var pin = pins[i];
                    if (predicate(pin)) return pin;
                }
            }
            return null;
        }

        public Pin[] FilterPins(Predicate<Pin> predicate)
        {
            using (PinsMutex.AcquireReadLock())
                return Pins.Values.Filter(predicate);
        }


        public Array<Pin> GetEnemyPinsInAO(AO ao, Empire us) => GetEnemyPinsInRadius(ao.Center, ao.Radius, us);
        public Array<Pin> GetEnemyPinsInRadius(Vector2 position, float radius, Empire us)
        {
            var pins = new Array<Pin>();
            {
                Pin[] pins1 = GetPins();
                for (int i = 0; i < pins1.Length; i++)
                {
                    Pin pin = pins1[i];
                    if (pin?.Position.InRadius(position, radius) != true) continue;
                    Empire pinEmpire = pin.Ship?.loyalty ?? pin.GetEmpire();
                    {
                        if (us.IsEmpireHostile(pinEmpire))
                            pins.Add(pin);
                    }
                }
            }
            return pins;
        }

        public void AddOrUpdatePin(Ship ship, bool shipInBorders, bool inSensorRadius)
        {
            if (!Pins.TryGetValue(ship?.guid ?? Guid.Empty, out Pin pin))
            {
                if (!inSensorRadius)
                    return; // don't add new pin if not in sensor radius

                pin = new Pin(ship, shipInBorders);
                Pins.Add(ship.guid, pin);
            }
            else if (ship?.Active == true)
                pin.Refresh(ship, inSensorRadius, shipInBorders);
        }

        public void ProcessPendingActions()
        {
            try
            {
                while (PendingThreadActions.NotEmpty)
                {
                    PendingThreadActions.Dequeue()?.Invoke();
                }
            }
            catch
            {
                Log.Error($"ThreatMatrix Update Failed with {PendingThreadActions.Count} in queue");
            }
        }

        public bool UpdateAllPins(Empire owner)
        {
            if (PendingThreadActions.NotEmpty ) return false;
            
            ThreatMatrix threatCopy;
            using (PinsMutex.AcquireReadLock())
            {
                var pins = Pins.ToDictionary(key=> key.Key, pin=> pin.Value);
                threatCopy = new ThreatMatrix(pins, owner);
            }

            var ships      = owner.GetShips().Clone();
            ships.AddRange(owner.GetProjectors());

            var pinsWithNotSeenShips = new Array<KeyValuePair<Guid,Pin>>();
            
            var array = EmpireManager.GetAllies(owner);
            for (int i = 0; i < array.Count; i++)
            {
                var empire = array[i];
                ships.AddRange(empire.GetShips());
                ships.AddRange(empire.GetProjectors());
            }

            // add or update pins for ship targets
            for (int i = 0; i < ships.Count; i++)
            {
                var ship = ships[i];
                if (ship?.Active != true) continue;

                var targets = ship.AI.PotentialTargets.ToArray();

                for (int x = 0; x < targets.Length; x++)
                {
                    var target = targets[x];
                    threatCopy.AddOrUpdatePin(target, target.IsInBordersOf(owner), true);
                }
            }

            // separate seen pins with pin.ship that arent seen. 
            foreach (var kv in threatCopy.Pins)
            {
                var pinShip = kv.Value?.Ship;
                // not dead or dying and seen
                if (pinShip?.dying == false && pinShip.Active &&
                    pinShip.KnownByEmpires.KnownBy(owner))
                {
                    if (pinShip.loyalty != owner && !owner.IsAlliedWith(pinShip.loyalty))
                        threatCopy.AddOrUpdatePin(pinShip, pinShip.IsInBordersOf(owner), true);
                }
                else
                    pinsWithNotSeenShips.Add(kv);
            }
            
            // remove seen pins with pin.ship that arent seen. 
            for (int i = 0; i < ships.Count; i++)
            {
                var ship = ships[i];
                if (ship?.Active != true) continue;
                for (int x = 0; x < pinsWithNotSeenShips.Count; x++)
                {
                    var pin = pinsWithNotSeenShips[x];
                    if (pin.Value?.Ship?.Active != true)
                    {
                        threatCopy.Pins.Remove(pin.Key);
                    }
                    else if (pin.Value.Position.InRadius(ship.Position, ship.SensorRange))
                    {
                        threatCopy.Pins.Remove(pin.Key);
                    }
                }
            }


            using (PinsMutex.AcquireWriteLock())
            {
                Pins = threatCopy.Pins;
            }
            KnownBases              = GetAllHostileBases();
            SystemThreatMap         = GetSystemPinMap();
            KnownSystemsWithEnemies = GetAllSystemsWith(pins => pins.Any(p => Owner.IsEmpireHostile(p.GetEmpire())));
            KnownEmpireStrengths    = GetEmpirePinMap();
            // threat matrix testing. 
            //var PinsWithSystems = Pins.FilterValues(p => p.System != null && p.Ship.System?.Name.Contains("Mil") == true);

            //Empire.Universe.RunOnEmpireThread(() =>

            //    {
            //        var oldPinsWithSystems = Pins.FilterValues(p => p.System == null && p.Ship.System?.Name.Contains("Mil") == true);

            //           var test = threatCopy.Pins;

            //        var newPinsWithSystems = threatCopy.Pins.FilterValues(p => p.System != null && p.Ship.System?.Name.Contains("Mil") == true);

            //    });
            return true;
        }

        public Pin[] GetPins() => Pins.AtomicValuesArray();

        public bool RemovePin(Ship ship) => RemovePin(ship.guid);

        bool RemovePin(Guid shipGuid)
        {
            using(PinsMutex.AcquireWriteLock())
                return Pins.Remove(shipGuid);
        }

        public void AddFromSave(SavedGame.GSAISAVE aiSave, Empire owner)
        {
            Owner = owner;
            using (PinsMutex.AcquireWriteLock())
            {
                for (int i = 0; i < aiSave.PinGuids.Count; i++)
                {
                    var key = aiSave.PinGuids[i];
                    var value = aiSave.PinList[i];
                    Pins.Add(key, value);
                }
            }
        }

        public void RestorePinGuidsFromSave()
        {
            foreach (var kv in Pins)
            {
                kv.Value.RestoreUnSerializedData(kv.Key);
            }
        }

        public void WriteToSave(SavedGame.GSAISAVE aiSave)
        {
            aiSave.PinGuids = new Array<Guid>();
            aiSave.PinList = new Array<Pin>();

            using (PinsMutex.AcquireReadLock())
            {
                foreach (KeyValuePair<Guid, Pin> guid in Pins)
                {
                    aiSave.PinGuids.Add(guid.Key);
                    aiSave.PinList.Add(guid.Value);
                }
            }
        }

        public void GetTechsFromPins(HashSet<string> techs, Empire empire)
        {
            using (PinsMutex.AcquireReadLock())
            {
                Pin[] pins = Pins.Values.ToArray();
                for (int i = 0; i < pins.Length; i++)
                {
                    Pin pin = pins[i];
                    if (pin.Ship != null && pin.Ship.loyalty == empire)
                        techs.UnionWith(pin.Ship.shipData.TechsNeeded);
                }
            }
        }
    }
}