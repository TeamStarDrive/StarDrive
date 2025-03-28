﻿using System;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    public static class GamePlayExtensions
    {
        /// <summary>
        /// Empire extension for getting available troops ships
        /// </summary>
        public static Array<Ship> GetAvailableTroopShips(this Empire empire, out int troopsInFleets)
        {
            var ships      = new Array<Ship>();
            troopsInFleets = 0;
            var collection = empire.OwnedShips;
            for (int x = 0; x < collection.Count; x++)
            {
                Ship ship = collection[x];
                if (!ship.Active
                    || ship.ShipData.Role != RoleName.troop
                    || ship.IsHangarShip
                    || ship.IsHomeDefense
                    || ship.AI.State == AIState.Scrap
                    || ship.AI.State == AIState.RebaseToShip
                    || ship.AI.HasPriorityOrder)
                {
                    continue;
                }

                if (ship.Fleet != null)
                    troopsInFleets += ship.GetOurTroops().Count;
                else
                    ships.Add(ship);
            }

            return ships;
        }
    }
}
