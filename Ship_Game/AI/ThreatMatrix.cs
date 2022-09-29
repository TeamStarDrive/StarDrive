using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class ThreatMatrix
    {
        [StarData] Empire Owner;

        [StarDataType]
        public class Pin
        {
            [StarData] public Vector2 Position;
            [StarData] public float Strength;

            /// <summary>
            /// This indicates that the PIN is in borders not the current ship status. 
            /// </summary>
            [StarData] public bool InBorders;
            [StarData] public int PinId;
            [StarData] public readonly Ship Ship;
            public Empire Empire => Ship.Loyalty;
            public SolarSystem System => Ship.System;

            [StarDataConstructor] Pin(){}

            public Pin(Ship ship, bool inBorders)
            {
                Ship = ship;
                Position = ship.Position;
                Strength = ship.GetStrength();
                InBorders = inBorders;
            }

            public void Refresh(bool inSensorRadius, bool shipInBorders)
            {
                if (inSensorRadius)
                {
                    Position = Ship.Position;
                    Strength = Ship.GetStrength();
                    InBorders = shipInBorders;
                }
            }
        }

        [StarDataConstructor]
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
        Map<SolarSystem, Pin[]> SystemThreatMap = new();
        Map<Empire, Pin[]> KnownEmpireStrengths = new();

        public ThreatMatrix(Map<int,Pin> matrix, Empire empire)
        {
            Pins = matrix;
            Owner = empire;
        }
        // not sure we need this.
        readonly ReaderWriterLockSlim PinsMutex = new();
        [StarData] Map<int, Pin> Pins = new();

        readonly SafeQueue<Action> PendingThreadActions = new();

        public bool ContainsGuid(int pinId)
        {
            return Pins.ContainsKey(pinId);
        }
        
        public float StrengthOfAllEmpireShipsInBorders(Empire us, Empire them)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.Empire;
                    if (pinEmpire == them && pin.Ship?.IsInBordersOf(us) == true)
                    {
                        if (pin.Ship != null && (pin.Ship.System?.IsExclusivelyOwnedBy(us) ?? true))
                            str += pin.Strength;
                    }
                }
            }
            return str;
        }

        public float StrengthOfEmpireInSystem(Empire empire, SolarSystem system) => GetStrengthInSystem(system, p => p.Empire == empire);

        public float StrengthOfHostilesInRadius(Empire us, Vector2 center, float radius)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.Position.InRadius(center, radius))
                    {
                        Empire pinEmp = pin.Empire;
                        if (pinEmp != us && (pinEmp.IsFaction || us.IsAtWarWith(pinEmp)))
                            str += pin.Strength;
                    }
                }
            }
            return str;
        }
        
        Array<Ship> PingRadarShip(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Ship>();
            var pins = Pins.Values.ToArr();
            for (int i = pins.Length - 1; i >= 0; i--)
            {
                Pin pin = pins[i];
                Ship ship = pin.Ship;
                if (ship != null && position.InRadius(pin.Position, radius) && empire.IsEmpireHostile(ship.Loyalty))
                {
                    results.Add(pin.Ship);
                }
            }

            return results;
        }

        Array<Pin> PingRadarPins(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Pin>();
            var pins = Pins.Values.ToArr();
            for (int i = pins.Length - 1; i >= 0; i--)
            {
                Pin pin = pins[i];
                if (position.InRadius(pin.Position, radius) && empire.IsEmpireHostile(pin.Empire))
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
                    Empire pinEmpire = pin.Empire;
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

        /// <summary> ThreadSafe </summary>
        public Pin[] GetAllFactionBases()
        {
            using (PinsMutex.AcquireReadLock())
                return KnownBases.Filter(b => b.Empire.IsFaction) ?? Empty<Pin>.Array;
        }

        Pin[] GetAllHostileBases() => FilterPins(p => p.Ship?.IsPlatformOrStation == true && Owner?.IsEmpireHostile(p.Empire) == true);

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
                    Map<Empire, float> empires = new Map<Empire, float>();
                    for (int i = 0; i < pins.Length; i++)
                    {
                        Pin pin        = pins[i];
                        Empire loyalty = pin.Empire;
                        float str      = pin.Strength;

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
            pins.Any(p => p.Empire.IsFaction && Owner.IsEmpireHostile(p.Empire)));

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
            var map = new Map<SolarSystem, Array<KeyValuePair<int, Pin>>>();
            using (PinsMutex.AcquireReadLock())
                map = Pins.GroupByFiltered(p => p.Value.System,
                p => p.Value.System != null && Owner.IsEmpireHostile(p.Value.Empire));

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
            var map = new Map<Empire, Array<KeyValuePair<int, Pin>>>();
            using (PinsMutex.AcquireReadLock())
                map = Pins.GroupByFiltered(p => p.Value.Empire,
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
                    Empire pinEmpire = pin.Ship?.Loyalty ?? pin.Empire;
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
            // Add new?
            if (!Pins.TryGetValue(ship.Id, out Pin pin))
            {
                if (!inSensorRadius)
                    return; // don't add new pin if not in sensor radius

                pin = new(ship, shipInBorders);
                Pins.Add(ship.Id, pin);
            }
            else if (ship.Active) // update existing
                pin.Refresh(inSensorRadius, shipInBorders);
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
                threatCopy = new ThreatMatrix(new Map<int, Pin>(Pins), owner);
            }

            var ships      = new Array<Ship>(owner.OwnedShips);
            ships.AddRange(owner.GetProjectors());

           
            var array = owner.Universum.GetAllies(owner);
            for (int i = 0; i < array.Count; i++)
            {
                var empire = array[i];
                ships.AddRange(empire.OwnedShips);
                ships.AddRange(empire.GetProjectors());
            }

            var pinsNeedRemoval = new Array<KeyValuePair<int, Pin>>();
            // add or update pins for ship targets
            for (int i = 0; i < ships.Count; i++)
            {
                var ship = ships[i];
                if (ship?.Active != true)
                    continue;

                var targets = ship.AI.PotentialTargets.ToArr();
                for (int x = 0; x < targets.Length; x++)
                {
                    var target = targets[x];
                    if (target != null)
                        threatCopy.AddOrUpdatePin(target, target.IsInBordersOf(owner), true);
                }

                var threatCopyPins = threatCopy.GetPins();
                for (int x = threatCopyPins.Length - 1; x >= 0; x--)
                {
                    Pin pin = threatCopyPins[x];
                    if (ship.Position.InRadius(pin.Position, ship.SensorRange))
                    {
                        if (pin.Ship?.Active != true)
                            threatCopy.Pins.Remove(pin.PinId);
                        else if (!ship.Position.InRadius(pin.Ship.Position, ship.SensorRange))
                            threatCopy.Pins.Remove(pin.PinId);
                    }
                }
            }

            foreach (var kv in threatCopy.Pins)
            {
                var pinShip = kv.Value.Ship;

                // Deal with only pins ships that are known to the empire
                if (pinShip.KnownByEmpires.KnownBy(owner))
                {
                    if (pinShip.Active && !pinShip.Dying && pinShip.Loyalty != owner &&
                        !owner.IsAlliedWith(pinShip.Loyalty))
                    {
                        threatCopy.AddOrUpdatePin(pinShip, pinShip.IsInBordersOf(owner), true);
                    }
                    else
                    {
                        pinsNeedRemoval.Add(kv);
                    }
                }
            }

            for (int x = 0; x < pinsNeedRemoval.Count; x++)
            {
                var pin = pinsNeedRemoval[x];
                threatCopy.Pins.Remove(pin.Key);
            }

            using (PinsMutex.AcquireWriteLock())
            {
                Pins = threatCopy.Pins;
            }

            KnownBases              = GetAllHostileBases();
            SystemThreatMap         = GetSystemPinMap();
            KnownSystemsWithEnemies = GetAllSystemsWith(pins => pins.Any(p => Owner.IsEmpireHostile(p.Empire)));
            KnownEmpireStrengths    = GetEmpirePinMap();
            return true;
        }

        public Pin[] GetPins() => Pins.AtomicValuesArray();

        public bool RemovePin(Ship ship) => RemovePin(ship.Id);

        bool RemovePin(int shipId)
        {
            using (PinsMutex.AcquireWriteLock())
                return Pins.Remove(shipId);
        }

        public void GetTechsFromPins(HashSet<string> techs, Empire empire)
        {
            using (PinsMutex.AcquireReadLock())
            {
                Pin[] pins = Pins.Values.ToArr();
                for (int i = 0; i < pins.Length; i++)
                {
                    Pin pin = pins[i];
                    if (pin.Ship != null && pin.Ship.Loyalty == empire)
                        techs.UnionWith(pin.Ship.ShipData.TechsNeeded);
                }
            }
        }
    }
}