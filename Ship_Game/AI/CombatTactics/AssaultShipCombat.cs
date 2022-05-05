using SDGraphics;
using Ship_Game.Ships;

namespace Ship_Game.AI.CombatTactics
{
    public sealed class AssaultShipCombat
    {
        Ship Target => Owner.AI.Target;
        Ship Owner;
        ShipAI AI => Owner.AI;
        CarrierBays Carrier => Owner.Carrier;

        public AssaultShipCombat(Ship ship)
        {
            Owner = ship;
        }

        public void TryBoardShip()
        {
            if (Owner == null || Owner.IsSpoolingOrInWarp)
                return;

            if (!Owner.Carrier.HasActiveTroopBays || Owner.Carrier.NumTroopsInShipAndInSpace <= 0)
                return;
            if (!Owner.Loyalty.isFaction && Target?.ShipData.Role <= RoleName.drone)
                return;

            float totalTroopStrengthToCommit = Owner.Carrier.MaxTroopStrengthInShipToCommit + Owner.Carrier.MaxTroopStrengthInSpaceToCommit;
            if (totalTroopStrengthToCommit <= 0)
                return;

            if (!AssaultTargetShip(Target) && !Owner.TroopsAreBoardingShip)
            {
                if (Owner.Loyalty.WeArePirates && totalTroopStrengthToCommit <= 0)
                    Owner.AI.OrderPirateFleeHome();
            }
        }

        bool AssaultTargetShip(Ship targetShip)
        {
            if (Owner.SecondsAlive < 2)
                return false; // Initial Delay in launching shuttles if spawned

            if (Owner == null || targetShip == null || targetShip.Loyalty == Owner.Loyalty)
                return false;

            if (!Owner.Carrier.AnyAssaultOpsAvailable || !targetShip.Position.InRadius(Owner.Position, Owner.DesiredCombatRange * 2))
                return false;

            bool sendingTroops = false;
            float totalTroopStrengthToCommit = Carrier.MaxTroopStrengthInShipToCommit + Carrier.MaxTroopStrengthInSpaceToCommit;
            float enemyStrength = targetShip.BoardingDefenseTotal / 2;

            if ((Owner.Loyalty.isPlayer && Owner.AI.HasPriorityTarget || totalTroopStrengthToCommit > enemyStrength) 
                && (Owner.Loyalty.isFaction || targetShip.GetStrength() > 0f))
            {
                if (Carrier.MaxTroopStrengthInSpaceToCommit.AlmostZero() || Carrier.MaxTroopStrengthInSpaceToCommit < enemyStrength)
                    // This will launch salvos of assault shuttles if possible
                    sendingTroops = Carrier.ScrambleAssaultShips(enemyStrength);

                for (int i = 0; i < Carrier.AllTroopBays.Length; i++)
                {
                    ShipModule hangar = Carrier.AllTroopBays[i];
                    if (hangar.TryGetHangarShipActive(out Ship hangarShip))
                    {
                        sendingTroops = true;
                        if (!hangarShip.AI.HasPriorityOrder && hangarShip.AI.State != AIState.Boarding && hangarShip.AI.State != AIState.Resupply)
                            hangarShip.AI.OrderTroopToBoardShip(targetShip);
                    }
                }
            }

            return sendingTroops;
        }

        /// <summary>
        /// Expand later
        /// </summary>
        /// <returns></returns>
        public bool TryInvadePlanet()
        {
            //This is the auto invade feature. FB: this should be expanded to check for building strength and compare troops in ship vs planet
            if (Owner.SecondsAlive < 2)
                return false; // Initial Delay in launching shuttles if spawned

            if (Owner == null || !Owner.Carrier.AnyAssaultOpsAvailable || Owner.Loyalty.WeArePirates || Owner.TroopsAreBoardingShip)
                return false;

            Planet invadeThis = Owner.System?.PlanetList.FindMinFiltered(
                                owner => owner.Owner != null && owner.Owner != Owner.Loyalty && Owner.Loyalty.IsAtWarWith(owner.Owner),
                                troops => troops.TroopsHere.Count);
            if (invadeThis != null)
                Owner.Carrier.AssaultPlanet(invadeThis);
            
            return true;
        }
    }
}

