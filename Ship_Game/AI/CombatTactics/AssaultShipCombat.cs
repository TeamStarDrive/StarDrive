using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class AssaultShipCombat : ShipAIPlan
    {
        readonly ShipAIPlan Artillery;
        Ship Target;

        public AssaultShipCombat(ShipAI ai) : base(ai)
        {
            Artillery = new Artillery(ai);
        }

        public override void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g)
        {
            if (Owner.IsSpoolingOrInWarp)
                return;

            Target = AI.Target;

            Artillery.Execute(timeStep, null);
            if (!Owner.Carrier.HasActiveTroopBays || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
                return;
            if (!Owner.loyalty.isFaction && Target?.shipData.Role <= ShipData.RoleName.drone)
                return;

            float totalTroopStrengthToCommit = Owner.Carrier.MaxTroopStrengthInShipToCommit + Owner.Carrier.MaxTroopStrengthInSpaceToCommit;
            if (totalTroopStrengthToCommit <= 0)
                return;

            if (Owner.Carrier.AssaultTargetShip(Target))
                return;

            //This is the auto invade feature. FB: this should be expanded to check for building strength and compare troops in ship vs planet
            if (Owner.TroopsAreBoardingShip)
                return;

            if (Owner.loyalty.WeArePirates)
            {
                if (totalTroopStrengthToCommit <= 0)
                    Owner.AI.OrderPirateFleeHome();

                return;
            }

            Planet invadeThis = Owner.System?.PlanetList.FindMinFiltered(
                                owner => owner.Owner != null && owner.Owner != Owner.loyalty && Owner.loyalty.IsAtWarWith(owner.Owner),
                                troops => troops.TroopsHere.Count);
            if (invadeThis != null)
                Owner.Carrier.AssaultPlanet(invadeThis);
        }
    }


}

