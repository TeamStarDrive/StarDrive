using System;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game.Fleets
{

    public class GroupLeader
    {
        Fleet AssignedFleet;
        public enum GroupTactic
        {
            Defensive,
            Offensive
        }
        public Ship Leader { get; private set; }
        public int GroupSkill => Leader?.Level ?? 0;
        public GroupTactic Tactic { get; private set; }

        public GroupLeader(Ship ship, Fleet fleet)
        {
            Leader = ship;
            AssignedFleet = fleet;
            if (Leader != null)
            {
                var groupTactics = (GroupTactic[])Enum.GetValues(typeof(GroupTactic));
                Tactic = RandomMath.RandItem<GroupTactic>(groupTactics);
                ApplyTactic();
            }
        }
        void ApplyTactic()
        {
            if (Leader.Loyalty.isPlayer) return;

            switch (Tactic)
            {
                case GroupTactic.Defensive:
                    AssignedFleet.DefensiveTactic();
                    break;
                case GroupTactic.Offensive:
                    AssignedFleet.OffensiveTactic();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}