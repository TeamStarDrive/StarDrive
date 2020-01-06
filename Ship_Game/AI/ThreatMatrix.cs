using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed class ThreatMatrix
    {
        public class Pin
        {
            [Serialize(0)] public Vector2 Position;
            [Serialize(1)] public float Strength;
            [Serialize(2)] public string EmpireName;
            [Serialize(3)] public bool InBorders;
            [XmlIgnore][JsonIgnore] public Ship Ship;
        }

        readonly ReaderWriterLockSlim PinsMutex = new ReaderWriterLockSlim();
        readonly Map<Guid, Pin> Pins = new Map<Guid, Pin>();

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
            using (PinsMutex.AcquireReadLock())
            {
                return Pins.ContainsKey(guid);
            }
        }
        
        public float StrengthOfAllEmpireShipsInBorders(Empire them)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                    if (pinEmpire == them  && pin.InBorders)   
                        str += pin.Strength + 1;
                }
            }
            return str;
        }

        public float StrengthOfAllEmpireThreats(Empire empire)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                    if (pinEmpire != null && !pinEmpire.isFaction && empire.IsEmpireAttackable(pinEmpire))
                        str += pin.Strength;
                }
            }
            return str;
        }

        public float StrengthOfEmpire(Empire empire)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                    if (pinEmpire == empire)
                        str += pin.Strength;
                }
            }
            return str;
        }

        public float StrengthOfEmpireInSystem(Empire empire, SolarSystem system)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.EmpireName.NotEmpty() && pin.EmpireName == empire.data.Traits.Name)
                        if (pin.Position.InRadius(system.Position, system.Radius))
                            str += pin.Strength;
                }
            }
            return str;
        }

        public float StrengthOfHostilesInRadius(Empire us, Vector2 center, float radius)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.Ship != null && pin.Position.InRadius(center, radius))
                    {
                        Empire pinEmp = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                        if (pinEmp != us && (pinEmp.isFaction || us.GetRelations(pinEmp).AtWar))
                            str += pin.Strength;
                    }
                }
            }
            return str;
        }
        
        Array<Ship> PingRadarShip(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Ship>();
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {                                
                    Ship ship = pin.Ship;
                    if (ship != null && position.InRadius(pin.Position, radius)
                                     && empire.IsEmpireAttackable(ship.loyalty, ship))
                    {
                        results.Add(pin.Ship);
                    }
                }
            }
            return results;
        }

        // Pings for enemy ships, chooses the closest enemy ship
        // and then pings around that ship to create a cluster of enemies
        public Array<Ship> PingRadarClosestEnemyCluster(Vector2 position, float radius, float granularity, Empire empire)
        {
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            if (pings.IsEmpty)
                return pings;

            Ship closest = pings.FindMin(ship => ship.Position.SqDist(position));
            return PingRadarShip(closest.Center, granularity, empire);
        }

        public Map<Vector2, float> PingRadarStrengthClusters(Vector2 position, float radius, float granularity, Empire empire)
        {
            var retList = new Map<Vector2, float>();
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            var filter = new HashSet<Ship>();

            for (int i = 0; i < pings.Count; i++)
            {
                Ship ship = pings[i];
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                Array<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count != 0)
                {
                    retList.Add(ship.Center, cluster.Sum(str => str.GetStrength()));
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
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            float largestCluster = 0;

            for (int i = 0; i < pings.Count; i++)
            {
                Ship ship = pings[i];
                if (ship == null || ship.IsGuardian || filter.Contains(ship))
                    continue;

                Array<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count != 0)
                {
                    float clusterStrength = cluster.Sum(str => str.GetStrength());
                    if (clusterStrength > largestCluster) largestCluster = clusterStrength;
                    filter.UnionWith(cluster);
                }
            }
            return largestCluster;

        }

        public void ClearPinsInSensorRange(Vector2 position, float radius)
        {
            Pin[] pins = PinValues; // somewhat atomic copy, since we're about to modify Pins Map.
            for (int i = 0; i < pins.Length; ++i)
            {
                Pin pin = pins[i];
                Ship ship = pin.Ship;
                if (ship != null && pin.Position.InRadius(position, radius))
                {
                    bool inSensor = ship.Center.InRadius(position, radius);
                    UpdatePin(ship, pin.InBorders, inSensor);
                }
            }
        }

        // NetStrength = OurStrength - HostileStrength
        public float PingNetRadarStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength:true);

        public float PingRadarStr(Vector2 position, float radius, Empire us, bool netStrength = false)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.Position.InRadius(position, radius))
                    {
                        Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                        if (pinEmpire != us && us.IsEmpireAttackable(pinEmpire))
                        {
                            str += pin.Strength;
                        }
                        else if (pinEmpire == us && netStrength)
                        {
                            str -= pin.Strength;
                        }
                    }
                }
            }
            return str;
        }

        public void UpdatePin(Ship ship)
        {
            using (PinsMutex.AcquireWriteLock())
            {
                if (ship.Active == false)
                {
                    Pins.Remove(ship.guid);
                }
                else if (Pins.TryGetValue(ship.guid, out Pin pin))
                {
                    pin.Position = ship.Center;
                }
                else
                {
                    Pins.Add(ship.guid, new Pin
                    {
                        Position = ship.Center,
                        Strength = ship.GetStrength(),
                        EmpireName = ship.loyalty.data.Traits.Name
                    });
                }
            }
        }

        public void UpdatePin(Ship ship, bool shipInBorders, bool inSensorRadius)
        {
            using (PinsMutex.AcquireWriteLock())
            {
                if (!Pins.TryGetValue(ship.guid, out Pin pin))
                {
                    if (!inSensorRadius)
                        return; // don't add new pin if not in sensor radius

                    pin = new Pin();
                    Pins.Add(ship.guid, pin);
                }

                if (inSensorRadius)
                {
                    pin.Position   = ship.Center;
                    pin.Strength   = ship.GetStrength();
                    pin.EmpireName = ship.loyalty.data.Traits.Name;
                }
                pin.InBorders = shipInBorders;
                pin.Ship      = ship; // NOTE: always setting because of save game data?
            }
        }

        public bool RemovePin(Ship ship)
        {
            using (PinsMutex.AcquireWriteLock())
            {
                return Pins.Remove(ship.guid);
            }
        }

        public void AddFromSave(SavedGame.GSAISAVE aiSave)
        {
            using (PinsMutex.AcquireWriteLock())
            {
                for (int i = 0; i < aiSave.PinGuids.Count; i++)
                {
                    Pins.Add(aiSave.PinGuids[i], aiSave.PinList[i]);
                }
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

        public void ClearBorders()
        {
            using (PinsMutex.AcquireWriteLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.InBorders)
                    {
                        if (!pin.Ship.Active || pin.Ship?.IsSubspaceProjector != true)
                            pin.InBorders = false;
                    }
                }
            }
        }

        public bool ShipInOurBorders(Ship s)
        {
            using (PinsMutex.AcquireReadLock())
            {
                return Pins.TryGetValue(s.guid, out Pin pin) && pin.InBorders;
            }
        }

        public void GetTechsFromPins(HashSet<string> techs, Empire empire)
        {
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                    if (pin.Ship != null && pin.Ship.loyalty == empire)
                        techs.UnionWith(pin.Ship.shipData.TechsNeeded);
            }
        }
    }
}