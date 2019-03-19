using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class AssaultShipCombat : ShipAIPlan
    {
        readonly ShipAIPlan SecondaryPlan;
        Ship Target;

        public AssaultShipCombat(ShipAI ai) : base(ai)
        {
            SecondaryPlan = new Artillery(ai);
        }

        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
            if (Owner.isSpooling)
                return;

            Target = AI.Target as Ship;

            SecondaryPlan.Execute(elapsedTime, null);
            if (!Owner.Carrier.HasTroopBays || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
                return;
            if (!Owner.loyalty.isFaction && Target?.shipData.Role <= ShipData.RoleName.drone)
                return;

            float totalTroopStrengthToCommit = Owner.Carrier.MaxTroopStrengthInShipToCommit + Owner.Carrier.MaxTroopStrengthInSpaceToCommit;
            if (totalTroopStrengthToCommit <= 0)
                return;

            bool boarding = false;
            if (Target is Ship shipTarget)
            {
                float enemyStrength = shipTarget.BoardingDefenseTotal * 1.5f; // FB: assume the worst, ensure boarding success!

                if (totalTroopStrengthToCommit > enemyStrength &&
                    (Owner.loyalty.isFaction || shipTarget.GetStrength() > 0f))
                {
                    if (Owner.Carrier.MaxTroopStrengthInSpaceToCommit < enemyStrength && Target.Center.InRadius(Owner.Center, Owner.maxWeaponsRange))
                        Owner.Carrier.ScrambleAssaultShips(enemyStrength); // This will launch salvos of assault shuttles if possible

                    for (int i = 0; i < Owner.Carrier.AllTroopBays.Length; i++)
                    {
                        ShipModule hangar = Owner.Carrier.AllTroopBays[i];
                        if (hangar.GetHangarShip() == null)
                            continue;
                        hangar.GetHangarShip().AI.OrderTroopToBoardShip(shipTarget);
                    }
                    boarding = true;
                }
            }
            //This is the auto invade feature. FB: this should be expanded to check for building stength and compare troops in ship vs planet
            if (boarding || Owner.TroopsAreBoardingShip)
                return;

            Planet invadeThis = Owner.System?.PlanetList.FindMinFiltered(
                                owner => owner.Owner != null && owner.Owner != Owner.loyalty && Owner.loyalty.GetRelations(owner.Owner).AtWar,
                                troops => troops.TroopsHere.Count);
            if (invadeThis != null)
                Owner.Carrier.AssaultPlanet(invadeThis);
        }
    }


}

