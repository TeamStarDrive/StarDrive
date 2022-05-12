using System;
using System.Collections.Generic;
using Ship_Game.Empires;
using Ship_Game.Gameplay;
using Ship_Game.Universe;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        InfluenceStatus CurrentInfluenceStatus;

        void UpdateInfluenceStatus()
        {
            CurrentInfluenceStatus = Universe.Influence.GetInfluenceStatus(Loyalty, Position);
        }

        public bool IsInBordersOf(Empire empire)
        {
            return Universe.Influence.IsInInfluenceOf(empire, Position);
        }

        public IEnumerable<Empire> GetProjectorInfluenceEmpires()
        {
            return Universe.Influence.GetEmpireInfluences(Position);
        }

        public bool IsInFriendlyProjectorRange => CurrentInfluenceStatus == InfluenceStatus.Friendly;
        public bool IsInHostileProjectorRange => CurrentInfluenceStatus == InfluenceStatus.Enemy;
    }
}