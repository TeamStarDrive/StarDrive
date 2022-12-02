using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

// data used for saved fleet designs
[StarDataType]
public class FleetDataDesignNode
{
    // used to requisition new ships with the same name
    [StarData] public string ShipName;
    [StarData] public Vector2 RelativeFleetOffset;
    [StarData] public float VultureWeight = 0.5f;
    [StarData] public float AttackShieldedWeight = 0.5f;
    [StarData] public float AssistWeight = 0.5f;
    [StarData] public float DefenderWeight = 0.5f;
    [StarData] public float DPSWeight = 0.5f;
    [StarData] public float SizeWeight = 0.5f;
    [StarData] public float ArmoredWeight = 0.5f;
    [StarData] public CombatState CombatState;
    [StarData] public float OrdersRadius = 500000;

    public FleetDataDesignNode() {}
    public FleetDataDesignNode(FleetDataDesignNode copy)
    {
        ShipName = copy.ShipName;
        RelativeFleetOffset = copy.RelativeFleetOffset;
        VultureWeight = copy.VultureWeight;
        AttackShieldedWeight = copy.AttackShieldedWeight;
        AssistWeight = copy.AssistWeight;
        DefenderWeight = copy.DefenderWeight;
        DPSWeight = copy.DPSWeight;
        SizeWeight = copy.SizeWeight;
        ArmoredWeight = copy.ArmoredWeight;
        CombatState = copy.CombatState;
        OrdersRadius = copy.OrdersRadius;
    }
}

[StarDataType]
public sealed class FleetDesign
{
    [StarData] public string Name;
    [StarData] public int FleetIconIndex;
    [StarData] public Array<FleetDataDesignNode> Nodes = new();
    
    // during loading of the design, can we build the fleet?
    public bool CanBuildFleet;
    public SubTexture Icon => ResourceManager.FleetIcon(FleetIconIndex);
}

[StarDataType]
public sealed class FleetDataNode : FleetDataDesignNode
{
    [StarData] public Ship Ship;
    [StarData] public Goal Goal;

    public FleetDataNode() {}
    public FleetDataNode(FleetDataDesignNode copy) : base(copy) {}
    
    // makes a copy of this FleetDataNode as a FleetDataDesignNode only
    public FleetDataDesignNode GetDesignOnly()
    {
        return new FleetDataDesignNode(this);
    }

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
            if (ship?.Active != true || ship.AI.Target == null || ship.DesignRoleType != RoleType.Warship) 
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
        weight += ApplyTargetWeight(potential.ShieldPower, targetParameterTotals.Shield, AttackShieldedWeight);
        weight += ApplyTargetWeight(potential.ArmorMax, targetParameterTotals.Armor, ArmoredWeight);
        weight += ApplyTargetWeight(potential.SurfaceArea, targetParameterTotals.Size, SizeWeight);
        weight += ApplyTargetWeight(potential.HealthPercent, targetParameterTotals.Health, VultureWeight);
        normalizer += 5;

        return weight / normalizer;
    }
}
