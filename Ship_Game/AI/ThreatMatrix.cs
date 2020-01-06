using System;
using System.Collections.Generic;
using System.Linq;
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
            [Serialize(1)] public Vector2 Velocity;
            [Serialize(2)] public float Strength;
            [Serialize(3)] public string EmpireName;
            [Serialize(4)] public bool InBorders;
            [XmlIgnore][JsonIgnore] public Ship Ship;
        }

        public Map<Guid, Pin> Pins = new Map<Guid, Pin>();
        public Map<Guid, Ship> Ship = new Map<Guid, Ship>();

        public bool ContainsGuid(Guid guid) => Pins.ContainsKey(guid);
        
        
        public float StrengthOfAllEmpireShipsInBorders(Empire them)
        {
            float str = 0f;
            foreach (var kv in Pins)
            {
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (pinEmpire == them  && kv.Value.InBorders)   
                str = str + kv.Value.Strength +1;
                
            }
            return str;
        }
        public float StrengthOfAllEmpireThreats(Empire empire) => StrengthOfAllThreats(empire, false);
        
        public float StrengthOfAllThreats(Empire empire, bool factionAlso)
        {
            float str = 0f;
            foreach (var kv in Pins)
            {
                if(kv.Value.EmpireName == string.Empty) continue;
                
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (!pinEmpire.isFaction || factionAlso)
                    if (empire.IsEmpireAttackable(pinEmpire))
                        str += kv.Value.Strength;
            }
            return str;
        }

        public float StrengthOfEmpire(Empire empire)
        {
            float str = 0f;
            foreach (var kv in Pins)
            {
                if (kv.Value.EmpireName == string.Empty) continue;

                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (pinEmpire != empire) continue;                
                str += kv.Value.Strength;

            }
            return str;
        }

        public float StrengthOfEmpireInSystem(Empire empire, SolarSystem system)
        {
            float str = 0f;

            foreach (var kv in Pins)
            {
                if (kv.Value.EmpireName == string.Empty) continue;
                if (kv.Value.EmpireName != empire.data.Traits.Name) continue;
                if (system.Position.OutsideRadius(kv.Value.Position, system.Radius)) continue;
                str += kv.Value.Strength;

            }
            return str;
        }
        
        // @todo Document this in more detail.
        public Array<Ship> PingRadarShip(Vector2 position, float radius, Empire empire)
        {
            var results = new Array<Ship>();
            foreach (KeyValuePair<Guid, Pin> kv in Pins)
            {                                
                Ship ship = kv.Value.Ship;
                if (ship != null && empire.IsEmpireAttackable(ship.loyalty, ship) &&
                    position.InRadius(kv.Value.Position, radius))
                {
                    results.Add(kv.Value.Ship);
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

            for (int index = 0; index < pings.Count; index++)
            {
                Ship ship = pings[index];
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                Array<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count == 0) continue;

                retList.Add(ship.Center, cluster.Sum(str => str.GetStrength()));
                filter.UnionWith(cluster);
            }
            return retList;

        }
        public struct StrengthCluster
        {
            public float Strength;
            public Vector2 Postition;
            public float Radius;
            public float Granularity;
            public Empire Empire;
        }

        public StrengthCluster FindLargestStengthClusterLimited(StrengthCluster strengthCluster, float maxStrength, Vector2 posOffSetInArea)
        {            
            Map<Vector2, float> strengthClusters = PingRadarStrengthClusters(strengthCluster.Postition, strengthCluster.Radius,
                strengthCluster.Granularity, strengthCluster.Empire);

            Vector2 clusterPostion = strengthClusters
                .FindMaxKeyByValuesFiltered(str => str < maxStrength, str => str);

            if (clusterPostion == default(Vector2))
            {
                strengthCluster.Strength = 0;
                return strengthCluster;
            }

            strengthCluster.Postition = clusterPostion;
            strengthCluster.Strength  = strengthClusters[clusterPostion];
            return strengthCluster;
        }


        public float PingRadarStrengthLargestCluster(Vector2 position, float radius, Empire empire, float granularity = 50000f)
        {
            var filter = new HashSet<Ship>();
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            float largestCluster = 0;

            for (int index = 0; index < pings.Count; index++)
            {
                Ship ship = pings[index];
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
            Pin[] pins = Pins.Values.ToArray(); // somewhat atomic copy, since we're about to modify it.
            for (int i = 0; i < pins.Length; ++i)
            {
                Pin pin = pins[i];
                Ship ship = pin.Ship;
                if (ship == null || position.OutsideRadius(pin.Position, radius))
                    continue;
                bool insensor = ship.Center.InRadius(position, radius);
                UpdatePin(ship, pin.InBorders, insensor);            
            }
        }

        public float PingRadarStr(Vector2 position, float radius, Empire us, bool factionOnly , bool any = false, bool netStr = false)
        {
            float str = 0f;
            foreach (var kv in Pins)            
            {
                Pin pin = kv.Value;
                Empire pinEmpire = pin.Ship?.loyalty ?? EmpireManager.GetEmpireByName(pin.EmpireName);
                if (factionOnly && !pinEmpire.isFaction) continue;
                if (us == pinEmpire || !us.IsEmpireAttackable(pinEmpire) || position.OutsideRadius(pin.Position, radius))
                {
                    if (netStr && us == pinEmpire)
                        str -= pin.Strength;
                    continue;
                }
                str += pin.Strength;
                if (any) break;
            }
            return str;
        }

        public float PingRadarStr(Vector2 position, float radius, Empire us) => PingRadarStr(position, radius, us, false);
        public float PingNetRadarStr(Vector2 position, float radius, Empire us) => PingRadarStr(position, radius, us, false, netStr:true);

        public void UpdatePin(Ship ship)
        {
            if (ship == null) return;
            if (ship.Active == false)
            {
                Pins.Remove(ship.guid);
                return;
            }
            
            if (!Pins.TryGetValue(ship.guid, out Pin pin) || pin == null)
            {
                Pins[ship.guid] = new Pin
                {
                    Position   = ship.Center,
                    Strength   = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name
                };
                return;
            }
            pin.Velocity = ship.Center - pin.Position;
            pin.Position = ship.Center;
        }

        public void UpdatePin(Ship ship, bool shipInBorders, bool inSensorRadius)
        {           
            Pins.TryGetValue(ship.guid, out Pin pin);
            if (pin == null && inSensorRadius)
            {
                Pins[ship.guid] = new Pin
                {
                    Position   = ship.Center,
                    Strength   = ship.GetStrength(),
                    EmpireName = ship.loyalty.data.Traits.Name,
                    Ship       = ship,
                    InBorders  = shipInBorders
                };
            }
            else if (pin != null)
            {
                if (inSensorRadius)
                {
                    pin.Velocity   = ship.Center - pin.Position;
                    pin.Position   = ship.Center;
                    pin.Strength   = ship.GetStrength();
                    pin.EmpireName = ship.loyalty.data.Traits.Name;
                }
                pin.InBorders = shipInBorders;
                pin.Ship      = ship;
            }
        }

        public bool RemovePin(Ship ship)
        {
            return Pins.Remove(ship.guid);
        }

        public void ClearBorders()
        {
            foreach (KeyValuePair<Guid, Pin> kv in Pins)
            {
                Pin pin = kv.Value;
                if (pin.InBorders)
                {
                    if (!pin.Ship.Active || pin.Ship?.IsSubspaceProjector != true)
                        pin.InBorders = false;
                }
            }
        }

        public bool ShipInOurBorders(Ship s)
        {
            return Pins.TryGetValue(s.guid, out Pin pin) && pin.InBorders;
        }
    }
}