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
        public float StrengthOfAllThreats(Empire empire)
        {
            float str = 0f;
            foreach (var kv in Pins)
            {
                if(kv.Value.EmpireName == string.Empty) continue;
                
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (!empire.IsEmpireAttackable(pinEmpire)) continue;
                str +=kv.Value.Strength;               

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
                if (system.Position.OutsideRadius(kv.Value.Position, system.StarRadius)) continue;
                str += kv.Value.Strength;

            }
            return str;
        }

        public Array<Ship> GetAllProjectorsExcept(Empire empire)
        {
            var projectors = new Array<Ship>();
            foreach (var kv in Pins.Values.ToArray())
            {
                if (kv.EmpireName == string.Empty || kv.Ship == null) continue;
                if (kv.Ship?.Name != "Subspace Projector") continue;                
                Empire pinEmpire = kv.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.EmpireName);
                if (pinEmpire == empire) continue;
                projectors.Add(kv.Ship);
            }
            return projectors;
        }

        public Vector2 GetPositionOfNearestEnemyWithinRadius(Vector2 position, float radius, Empire us)
        {
            var enemies = new Array<Pin>();
            foreach (var kv in Pins)
            {
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (!us.IsEmpireAttackable(pinEmpire) || position.OutsideRadius(kv.Value.Position , radius))				
                    continue;
                
                enemies.Add(kv.Value);
            }
            return enemies.Count == 0 ? Vector2.Zero : enemies.FindMin(ene => ene.Ship.Center.SqDist(position)).Ship.Center;
        }

        public Array<Pin> PingRadar(Vector2 position, float radius)
        {
            var retList = new Array<Pin>();
            foreach (var kv in Pins)
            {
                if (position.OutsideRadius(kv.Value.Position, radius)) continue;
                
                retList.Add(kv.Value);
            }
            return retList;
        }
      
        public Array<GameplayObject> PingRadarObj(Vector2 position, float radius)
        {
            var retList = new Array<GameplayObject>();
            foreach (var kv in Pins)
            {
                if (position.OutsideRadius(kv.Value.Position, radius)) continue;
                
                retList.Add(kv.Value.Ship);
            }
            return retList;
        }
        public Array<Ship> PingRadarShip(Vector2 position, float radius)
        {
            Array<Ship> retList = new Array<Ship>();
            foreach (var kv in Pins)
            {
                if (position.OutsideRadius(kv.Value.Position, radius)) continue;
  
                retList.Add(kv.Value.Ship);
            }
            return retList;
        }
        public Array<Ship> PingRadarShip(Vector2 position, float radius,Empire empire)
        {
            Array<Ship> retList = new Array<Ship>();
            foreach (var kv in Pins)
            {                                
                Ship ship = kv.Value.Ship;
                if (ship == null) continue;

                if (!empire.IsEmpireAttackable(ship.loyalty, ship)) continue;
                if (position.OutsideRadius(kv.Value.Position, radius)) continue;
                retList.Add(kv.Value.Ship);
                
            }
            return retList;
        }
        public Map<Vector2, Ship[]> PingRadarShipClustersByVector(Vector2 position, float radius, float granularity,Empire empire, bool truePosition = false)
        {
            var retList = new Map<Vector2, Ship[]>();
            Array<Ship> pings = PingRadarShip(position,radius,empire);
            HashSet<Ship > filter = new HashSet<Ship>();
            
            foreach(Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;
                if (position.Distance(ship.Center) > radius) continue;
                Array<Ship> cluster = PingRadarShip(ship.Center, granularity,empire);
                if (cluster.Count == 0)
                    continue;
                retList.Add(ship.Center, cluster.ToArray());                
                filter.UnionWith(cluster);

            }
            return retList;

        }
        public Map<Ship, Ship[]> PingRadarShipClustersByShip(Vector2 position, float radius, float granularity, Empire empire)
        {
            var retList = new Map<Ship, Ship[]>();
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            var filter = new HashSet<Ship>();

            foreach (Ship ship in pings)
            {
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship))
                    continue;

                Array<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count == 0) continue;

                retList.Add(ship, cluster.ToArray());
                filter.UnionWith(cluster);

            }
            return retList;

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

        public StrengthCluster FindLargestStengthClusterLimited(StrengthCluster strengthCluster, float maxStength, Vector2 posOffSetInArea)
        {            
            Map<Vector2, float> strengthClusters = PingRadarStrengthClusters(strengthCluster.Postition, strengthCluster.Radius,
                strengthCluster.Granularity, strengthCluster.Empire);

            Vector2 clusterPostion = strengthClusters.MaxKeyByValuesFiltered(str => str, str => str < maxStength);

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
            var retList = new Map<Vector2, float>();
            Array<Ship> pings = PingRadarShip(position, radius, empire);
            float largestCluster =0;
            var filter = new HashSet<Ship>();

            for (int index = 0; index < pings.Count; index++)
            {
                Ship ship = pings[index];
                if (ship == null || filter.Contains(ship) || retList.ContainsKey(ship.Center))
                    continue;

                Array<Ship> cluster = PingRadarShip(ship.Center, granularity, empire);
                if (cluster.Count == 0) continue;

                float clusterStrength =cluster.Sum(str => str.GetStrength());
                if (clusterStrength > largestCluster) largestCluster = clusterStrength;                
                filter.UnionWith(cluster);
            }
            return largestCluster;

        }
        
        public Vector2 PingRadarAvgPos(Vector2 position, float radius, Empire us)
        {
            var pos = new Vector2();
            int count = 0;
            foreach (var kv in Pins)
            {
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (pinEmpire == us || position.OutsideRadius(kv.Value.Position, radius) || !us.IsEmpireAttackable(pinEmpire))
                    continue;

                ++count;
                pos += kv.Value.Position;
            }
            if (count > 0)
                pos /= count;
            return pos;
        }


        public void ClearPinsInSensorRange(Vector2 position, float radius)
        {
            foreach (Pin pin in Pins.Values)
            {
                Ship ship = pin.Ship;
                if (ship == null || position.OutsideRadius(pin.Position, radius))
                    continue;
                bool insensor = ship.Center.InRadius(position, radius);
                UpdatePin(ship, pin.InBorders, insensor);            
            }
        }
        public float PingRadarStr(Vector2 position, float radius, Empire us, bool factionOnly , bool any = false)
        {
            float str = 0f;
            foreach (var kv in Pins)            
            {
                Empire pinEmpire = kv.Value.Ship?.loyalty ?? EmpireManager.GetEmpireByName(kv.Value.EmpireName);
                if (factionOnly && !pinEmpire.isFaction) continue;
                if (us == pinEmpire || position.OutsideRadius(kv.Value.Position, radius) || !us.IsEmpireAttackable(pinEmpire))
                    continue;                      
                str += kv.Value.Strength;
                if (any) break;
            }
            return str;
        }
        public float PingRadarStr(Vector2 position, float radius, Empire us) => PingRadarStr(position, radius, us, false);
        public float PingRadarAny(Vector2 position, float radius, Empire us) => PingRadarStr(position, radius, us, false, true);

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
        public void UpdatePin(Ship ship, bool shipinBorders, bool inSensorRadius)
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
                    InBorders  = shipinBorders
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
                pin.InBorders = shipinBorders;
                pin.Ship      = ship;
            }
        }
        public bool RemovePin(Ship ship) => (Pins.Remove(ship.guid));

        public void ClearBorders ()
        {
            foreach (var kv in Pins)
            {
                if (kv.Value.InBorders)
                    if (kv.Value.Ship?.Name == "Subspace Projector"  && kv.Value.Ship.Active)
                        continue;
                    kv.Value.InBorders = false;
            }
        }
        public void UpdatePinShip(Ship s, Guid guid)
        {
            if (!s.Active)
            {
                Pins.Remove(guid);
            }
            else if (!Pins.TryGetValue(s.guid, out Pin pin))
            {
                Pins.Add(s.guid, new Pin
                {
                    Position   = s.Center,
                    Strength   = s.GetStrength(),
                    EmpireName = s.loyalty.data.Traits.Name
                });
            }
            else
            {
                pin.Velocity = s.Center - pin.Position;
                pin.Position = s.Center;
            }

        }

        public bool ShipInOurBorders(Ship s)
        {
            return Pins.TryGetValue(s.guid, out Pin pin) && pin.InBorders;
        }

        public Array<Ship> FindShipsInOurBorders()
        {
            var temp = new Array<Ship>();
            foreach (Pin p in Pins.Values)
               if (p.InBorders && p.Ship != null)
                   temp.Add(p.Ship);
            return temp;
        }        

        public class Pin
        {
            [Serialize(0)] public Vector2 Position;
            [Serialize(1)] public Vector2 Velocity;
            [Serialize(2)] public float Strength;
            [Serialize(3)] public string EmpireName;
            [Serialize(4)] public bool InBorders;

            [XmlIgnore][JsonIgnore] public Ship Ship;
        }
    }
}