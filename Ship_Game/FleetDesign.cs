using System;
using System.Data.Common;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class FleetDataNode
    {
        [XmlIgnore][JsonIgnore] public Ship Ship { get; set; }

        [Serialize(0)] public Guid ShipGuid;
        [Serialize(1)] public Guid GoalGUID;
        [Serialize(2)] public string ShipName;
        [Serialize(3)] public Vector2 FleetOffset;
        [Serialize(4)] public float VultureWeight = 0.5f;
        [Serialize(5)] public float AttackShieldedWeight = 0.5f;
        [Serialize(6)] public float AssistWeight = 0.5f;
        [Serialize(7)] public float DefenderWeight = 0.5f;
        [Serialize(8)] public float DPSWeight = 0.5f;
        [Serialize(9)] public float SizeWeight = 0.5f;
        [Serialize(10)] public float ArmoredWeight = 0.5f;
        [XmlElement(ElementName = "orders")]
        [Serialize(11)] public Orders Order;
        [Serialize(12)] public CombatState CombatState;
        [Serialize(13)] public Vector2 OrdersOffset;
        [Serialize(14)] public float OrdersRadius = 500000;//0.5f;

        public void SetCombatStance(CombatState stance)
        {
            CombatState = stance;
            Ship?.SetCombatStance(stance);
        }

        public static float ApplyTargetWeight(float targetValue, float avgTargetsValue, float weightTypeValue)
        {
            // if fleet setting is above 0.5 then a ship with higher than average value will have more weightTypeValue. 

            // range between -ratio under and postive ratio over average          
            float rawWeight = (targetValue - avgTargetsValue) / avgTargetsValue.LowerBound(1);

            // above or below average * fleet weightTypeValue should give a positive value when both are the same sign. 
            // and negative when opposite. 
            float weight = (weightTypeValue - 0.5f) * rawWeight;
            return weight.UpperBound(1);

            // ex.
            // fleet value weightTypeValue is 0.5
            // target value is under average by -.25f
            // 0.5 * -0.25 = -0.125
        }

        public float ApplyFleetWeight(Array<Ship> fleetShips, Ship potential, ShipAI.TargetParameterTotals targetParameterTotals)
        {
            float weight = 0;

            bool defend = false;
            bool assist = false;

            for (int i = 0; i < fleetShips.Count; i++)
            {
                Ship ship = fleetShips[i];
                if (ship?.Active != true || ship.AI.Target == null || ship.DesignRoleType != ShipData.RoleType.Warship) 
                    continue;

                if (potential.AI.Target == ship) defend = true;
                if (ship.AI.Target == potential) assist = true;
                
                if (defend && assist) 
                    break;
            }
            int normalizer = 0;
            if (defend) {weight += DefenderWeight; normalizer++;}
            if (assist) {weight += AssistWeight; normalizer++;}


            weight += ApplyTargetWeight(potential.TotalDps, targetParameterTotals.DPS, DPSWeight);
            weight += ApplyTargetWeight(potential.shield_power, targetParameterTotals.Shield, AttackShieldedWeight);
            weight += ApplyTargetWeight(potential.armor_max, targetParameterTotals.Armor, ArmoredWeight);
            weight += ApplyTargetWeight(potential.SurfaceArea, targetParameterTotals.Size, SizeWeight);
            weight += ApplyTargetWeight(potential.HealthPercent, targetParameterTotals.Health, VultureWeight);
            normalizer += 5;

            return weight / normalizer;
        }

        public FleetDataNode Clone()
        {
            return (FleetDataNode)MemberwiseClone();
        }
    }

    public sealed class FleetDesign
    {
        [Serialize(0)] public Array<FleetDataNode> Data = new Array<FleetDataNode>();
        [Serialize(1)] public int FleetIconIndex;
        [Serialize(2)] public string Name;
        [XmlIgnore][JsonIgnore] public SubTexture Icon => ResourceManager.FleetIcon(FleetIconIndex);

        public void Rotate(float facing)
        {
            foreach (FleetDataNode node in Data)
            {
                float radians = facing + Vector2.Zero.RadiansToTarget(node.FleetOffset);
                float distance = node.FleetOffset.Length();
                node.FleetOffset = Vector2.Zero.PointFromRadians(radians, distance);
            }
        }
    }
}