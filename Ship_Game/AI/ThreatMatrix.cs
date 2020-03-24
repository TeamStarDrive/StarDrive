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
            [Serialize(4)] public int EmpireId = 0;
            [XmlIgnore][JsonIgnore] public Ship Ship;

            public Pin(Ship ship, bool inBorders)
            {
                Position   = ship.Center;
                Strength   = ship.GetStrength();
                EmpireName = ship.loyalty.data.Traits.Name;
                InBorders  = inBorders;
                Ship       = ship;
            }

            public Pin(){}

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
                }

                InBorders = shipInBorders;
                Ship      = ship;
            }
        }

        //not sure we need this.
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
            return Pins.ContainsKey(guid);
        }
        
        public float StrengthOfAllEmpireShipsInBorders(Empire them)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.GetEmpire();
                    if (pinEmpire == them  && pin.InBorders)   
                        str += pin.Strength + 1;
                }
            }
            return str;
        }

        public float HighestStrengthOfAllEmpireThreats(Empire empire)
        {
            float highestStr = 0;
            Map<Empire, float> empireStrTable = new Map<Empire, float>();
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    Empire pinEmpire = pin.GetEmpire();
                    if (pinEmpire != null && !pinEmpire.isFaction)
                    {
                        float str = empire.IsEmpireAttackable(pinEmpire) ? pin.Strength : pin.Strength / 2;
                        if (!empireStrTable.ContainsKey(empire))
                            empireStrTable.Add(empire, str);
                        else
                            empireStrTable[empire] += str;
                    }
                }
            }
            if (empireStrTable.Count > 1)
                Log.Info("lala");

            if (empireStrTable.Count > 0)
                highestStr = empireStrTable.FindMaxValue(v => v);

            return highestStr;
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

        public float StrengthOfEmpireInSystem(Empire empire, SolarSystem system)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    var pinEmpire = pin.GetEmpire();
                    if (pinEmpire == empire)
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
                        Empire pinEmp = pin.GetEmpire();
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
                if (pin.Position.InRadius(position, radius))
                {
                    if (ship != null)
                    {
                        bool inSensor = ship.Center.InRadius(position, radius);
                        pin.Refresh(ship, pin.InBorders, inSensor);
                    }
                }
            }
        }

        public float PingNetRadarStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength:true);

        public float PingHostileStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength: false, true);
        public float PingNetHostileStr(Vector2 position, float radius, Empire us)
            => PingRadarStr(position, radius, us, netStrength: true, true);

        public float PingRadarStr(Vector2 position, float radius, Empire us, bool netStrength = false, bool hostileOnly = false)
        {
            float str = 0f;
            using (PinsMutex.AcquireReadLock())
            {
                foreach (Pin pin in Pins.Values)
                {
                    if (pin.Position.InRadius(position, radius))
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
            }
            return str;
        }

        public void UpdatePin(Ship ship, bool shipInBorders, bool inSensorRadius)
        {
            if (!Pins.TryGetValue(ship.guid, out Pin pin))
            {
                if (!inSensorRadius)
                    return; // don't add new pin if not in sensor radius

                pin = new Pin(ship, shipInBorders);
                Pins.Add(ship.guid, pin);
            }
            else
                pin.Refresh(ship, inSensorRadius, shipInBorders);
        }

        public bool RemovePin(Ship ship) => RemovePin(ship.guid);

        bool RemovePin(Guid shipGuid) => Pins.Remove(shipGuid);

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
            // NOTE: We can't lock the mutex here, because of `Ship.IsAttackable`
            //       causing recursive mutex
            return Pins.TryGetValue(s.guid, out Pin pin) && pin.InBorders;
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