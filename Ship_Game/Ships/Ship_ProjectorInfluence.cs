using System;
using System.Collections.Generic;
using Ship_Game.Empires;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public bool IsInBordersOf(Empire empire)
        {
            return Universe.Influence.IsInInfluenceOf(empire, Position);
        }

        public IEnumerable<Empire> GetProjectorInfluenceEmpires()
        {
            return Universe.Influence.GetEmpireInfluences(Position);
        }

        public bool IsInFriendlyProjectorRange
        {
            get
            {
                if (Universe == null) // for ShipTemplates
                    return false;
                (_, InfluenceStatus status) = Universe.Influence.GetPrimaryInfluence(Loyalty, Position);
                return status == InfluenceStatus.Friendly;
            }
        }

        public bool IsInHostileProjectorRange
        {
            get
            {
                if (Universe == null) // for ShipTemplates
                    return false;
                (_, InfluenceStatus status) = Universe.Influence.GetPrimaryInfluence(Loyalty, Position);
                return status == InfluenceStatus.Enemy;
            }
        }
    }
}