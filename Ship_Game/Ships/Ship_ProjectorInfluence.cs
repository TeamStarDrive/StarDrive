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

        /// <summary>
        /// Whether this ship may legally cross another empire's projected border.
        /// An undiscovered empire cannot enforce a border the ship does not know
        /// exists; crossing it enables the normal first-contact sensor flow.
        /// Warships may enter an enemy's territory during a declared war; peaceful
        /// access requires an alliance or open-borders treaty. Trade treaties grant
        /// the narrower access their civilian freighters need, without opening the
        /// border to the rest of the empire's navy.
        /// </summary>
        public bool HasBorderAccessTo(Empire borderOwner)
        {
            return Loyalty.HasBorderAccessTo(borderOwner, civilianFreighter: IsFreighter);
        }

        public bool IsInFriendlyProjectorRange => CurrentInfluenceStatus == InfluenceStatus.Friendly;
        public bool IsInHostileProjectorRange => CurrentInfluenceStatus == InfluenceStatus.Enemy;
    }
}
