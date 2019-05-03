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

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            if (Owner.isSpooling)
                return;

            Target = AI.Target as Ship;

            Artillery.Execute(elapsedTime, null);
            if (!Owner.Carrier.HasActiveTroopBays || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
                return;
            if (!Owner.loyalty.isFaction && Target?.shipData.Role <= ShipData.RoleName.drone)
                return;

            float totalTroopStrengthToCommit = Owner.Carrier.MaxTroopStrengthInShipToCommit + Owner.Carrier.MaxTroopStrengthInSpaceToCommit;
            if (totalTroopStrengthToCommit <= 0)
                return;

            if (Owner.Carrier.AssaultTargetShip(Target))
                return;

            //This is the auto invade feature. FB: this should be expanded to check for building stength and compare troops in ship vs planet
            if (Owner.TroopsAreBoardingShip)
                return;

            Planet invadeThis = Owner.System?.PlanetList.FindMinFiltered(
                                owner => owner.Owner != null && owner.Owner != Owner.loyalty && Owner.loyalty.GetRelations(owner.Owner).AtWar,
                                troops => troops.TroopsHere.Count);
            if (invadeThis != null)
                Owner.Carrier.AssaultPlanet(invadeThis);
        }
    }


}

