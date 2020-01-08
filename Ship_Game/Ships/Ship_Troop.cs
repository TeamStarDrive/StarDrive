using System;
using System.Collections.Generic;
using Ship_Game.AI;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        /// <summary>
        /// NOTE: By the original game design, this list contains
        /// our troops and also enemy troops.
        /// It can get mighty confusing, but that's what we got.
        /// </summary>
        public readonly Array<Troop> TroopList = new Array<Troop>();
        public int TroopCount => TroopList.Count;

        // TRUE if we have any troops present on this ship
        // @warning Some of these MAY be enemy troops!
        public bool HasLocalTroops => TroopList.Count > 0;
        
        // TRUE if we have local troops or if we launched these troops on to operations
        public bool HasTroopsPresentOrLaunched => TroopList.Count > 0
                                                  || Carrier.LaunchedAssaultShuttles > 0;

        public bool TroopsAreBoardingShip => TroopList.Count(troop => troop.Loyalty == loyalty) != TroopList.Count;
        public int NumPlayerTroopsOnShip  => TroopList.Count(troop => troop.Loyalty == EmpireManager.Player);
        public int NumAiTroopsOnShip      => TroopList.Count(troop => troop.Loyalty != EmpireManager.Player);

        public int NumTroopsRebasingHere
        {
            get
            {
                using (loyalty.GetShips().AcquireReadLock())
                {
                    return loyalty.GetShips().Count(s => s.AI.State == AIState.RebaseToShip &&
                                                    s.AI.EscortTarget == this);
                }
            }
        }

        public bool TryLandSingleTroopOnShip(Ship targetShip)
        {
            for (int i = 0; i < TroopList.Count; ++i)
            {
                Troop troop = TroopList[i];
                if (troop != null && troop.Loyalty == loyalty)
                {
                    troop.LandOnShip(targetShip);
                    return true;
                }
            }
            return false;
        }

        public bool TryLandSingleTroopOnPlanet(Planet targetPlanet)
        {
            for (int i = 0; i < TroopList.Count; ++i)
            {
                Troop troop = TroopList[i];
                if (troop != null && troop.Loyalty == loyalty && troop.TryLandTroop(targetPlanet))
                    return true;
            }

            return false;
        }

        public bool GetOurFirstTroop(out Troop troop)
        {
            for (int i = 0; i < TroopList.Count; ++i)
            {
                Troop t = TroopList[i];
                if (t != null && t.Loyalty == loyalty)
                {
                    troop = t;
                    return true;
                }
            }
            troop = null;
            return false;
        }

        public Array<Troop> GatherOurTroops(int maxTroops)
        {
            var troops = new Array<Troop>();
            for (int i = 0; i < TroopList.Count && troops.Count < maxTroops; ++i)
            {
                Troop troop = TroopList[i];
                if (troop != null && troop.Loyalty == loyalty)
                    troops.Add(troop);
            }
            return troops;
        }

        public int LandTroopsOnShip(Ship targetShip, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // NOTE: Need to create a copy of TroopList here,
            // because `LandOnShip` will modify TroopList
            Troop[] troopsToLand = TroopList.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                Troop troop = troopsToLand[i];
                if (troop != null && troop.Loyalty == loyalty)
                {
                    troop.LandOnShip(targetShip);
                    ++landed;
                }
            }
            return landed;
        }

        // This will launch troops without having issues with modifying it's own TroopsHere
        public int LandTroopsOnPlanet(Planet planet, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // @note: Need to create a copy of TroopList here,
            // because `TryLandTroop` will modify TroopList if landing is successful
            Troop[] troopsToLand = TroopList.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                Troop troop = troopsToLand[i];
                if (troop != null && troop.Loyalty == loyalty)
                {
                    if (troop.TryLandTroop(planet))
                        ++landed;
                    else break; // no more free tiles, probably
                }
            }
            return landed;
        }

        void SpawnTroopsForNewShip(ShipModule module)
        {
            string troopType    = "Wyvern";
            string tankType     = "Wyvern";
            string redshirtType = "Wyvern";

            IReadOnlyList<Troop> unlockedTroops = loyalty?.GetUnlockedTroops();
            if (unlockedTroops?.Count > 0)
            {
                troopType    = unlockedTroops.FindMax(troop => troop.SoftAttack).Name;
                tankType     = unlockedTroops.FindMax(troop => troop.HardAttack).Name;
                redshirtType = unlockedTroops.FindMin(troop => troop.SoftAttack).Name; // redshirts are weakest
                troopType    = (troopType == redshirtType) ? tankType : troopType;
            }

            for (int i = 0; i < module.TroopsSupplied; ++i) // TroopLoad (?)
            {
                string type = troopType;
                int numHangarsBays = Carrier.AllTroopBays.Length;
                if (numHangarsBays < TroopList.Count + 1) //FB: if you have more troop_capacity than hangars, consider adding some tanks
                {
                    type = troopType; // ex: "Space Marine"
                    if (TroopList.Count(troop => troop.Name == tankType) <= numHangarsBays)
                        type = tankType;
                    // number of tanks will be up to number of hangars bays you have. If you have 8 barracks and 8 hangar bays
                    // you will get 8 infantry. if you have  8 barracks and 4 bays, you'll get 4 tanks and 4 infantry .
                    // If you have  16 barracks and 4 bays, you'll still get 4 tanks and 12 infantry.
                    // logic here is that tanks needs hangarbays and barracks, and infantry just needs barracks.
                }

                Troop newTroop = ResourceManager.CreateTroop(type, loyalty);
                newTroop.LandOnShip(this);
            }
        }

        void UpdateTroopBoardingDefense()
        {
            TroopBoardingDefense = 0f;
            for (int i = 0; i < TroopList.Count; i++)
            {
                Troop troop = TroopList[i];
                troop.SetShip(this);
                if (troop.Loyalty == loyalty)
                    TroopBoardingDefense += troop.Strength;
            }
        }

        void RefreshMechanicalBoardingDefense()
        {
            MechanicalBoardingDefense =  ModuleSlotList.Sum(module => module.MechanicalBoardingDefense);
        }

        void DisengageExcessTroops(int troopsToRemove) // excess troops will leave the ship, usually after successful boarding
        {
            for (int i = 0; i < troopsToRemove && TroopList.NotEmpty; i++)
            {
                Troop troop = TroopList[0];
                Ship assaultShip     = CreateTroopShipAtPoint(GetAssaultShuttleName(loyalty), loyalty, Center, TroopList[0]);
                assaultShip.Velocity = UniverseRandom.RandomDirection() * assaultShip.SpeedLimit + Velocity;
                troop.LandOnShip(assaultShip);

                Ship friendlyTroopShipToRebase = FindClosestAllyToRebase(assaultShip);

                bool rebaseSucceeded = false;
                if (friendlyTroopShipToRebase != null)
                    rebaseSucceeded = friendlyTroopShipToRebase.Carrier.RebaseAssaultShip(assaultShip);

                if (!rebaseSucceeded) // did not found a friendly troopship to rebase to
                    assaultShip.AI.OrderRebaseToNearest();

                if (assaultShip.AI.State == AIState.AwaitingOrders) // nowhere to rebase
                    assaultShip.DoEscort(this);
            }
        }

        Ship FindClosestAllyToRebase(Ship ship)
        {
            ship.AI.ScanForCombatTargets(ship, ship.SensorRange); // to find friendlies nearby
            return ship.AI.FriendliesNearby.FindMinFiltered(
                troopShip => troopShip.Carrier.NumTroopsInShipAndInSpace < troopShip.TroopCapacity &&
                             troopShip.Carrier.HasActiveTroopBays,
                troopShip => ship.Center.SqDist(troopShip.Center));
        }

        void HealOurTroops()
        {
            for (int i = 0; i < TroopList.Count; ++i)
            {
                Troop troop = TroopList[i];
                if (troop.Loyalty == loyalty)
                {
                    troop.Strength = (troop.Strength += HealPerTurn).Clamped(0, troop.ActualStrengthMax);
                }
            }
        }
        
        void UpdateTroops()
        {
            UpdateTroopBoardingDefense();

            if (HealPerTurn > 0)
                HealOurTroops();

            Troop[] ownTroops   = GetGoodGuysOnShip();
            Troop[] enemyTroops = GetBadBoysOnShip();

            if (ownTroops.Length > 0)
            {
                // leave a garrison of 1 if a ship without barracks was boarded
                int troopThreshold = TroopCapacity + (TroopCapacity > 0 ? 0 : 1);
                if (!InCombat && enemyTroops.Length == 0 && ownTroops.Length > troopThreshold)
                {
                    DisengageExcessTroops(ownTroops.Length - troopThreshold);
                }
            }


            if (enemyTroops.Length > 0) // Combat!!
            {
                var combatTurn = new ShipTroopCombatTurn(this, ownTroops, enemyTroops);
                combatTurn.ResolveCombat();
            }
        }

        Troop[] GetGoodGuysOnShip() => TroopList.Filter(troop => troop.Loyalty == loyalty);
        Troop[] GetBadBoysOnShip()  => TroopList.Filter(troop => troop.Loyalty != loyalty);

        struct ShipTroopCombatTurn
        {
            readonly Ship Ship;
            Troop[] GoodGuys;
            Troop[] BadBoys;

            public ShipTroopCombatTurn(Ship ship, Troop[] ownTroops, Troop[] enemyTroops)
            {
                Ship = ship;
                GoodGuys = ownTroops;
                BadBoys = enemyTroops;
            }

            public void ResolveCombat()
            {
                ResolveOwnVersusEnemy();
                ResolveEnemyVersusOwn();

                // enemy troops won:
                if (GoodGuys.Length == 0 && Ship.MechanicalBoardingDefense <= 0f && BadBoys.Length > 0)
                {
                    Ship.ChangeLoyalty(changeTo: BadBoys[0].Loyalty);
                    Ship.RefreshMechanicalBoardingDefense();
                }
            }

            void ResolveOwnVersusEnemy()
            {
                if (GoodGuys.Length == 0 || BadBoys.Length == 0)
                    return;

                float ourCombinedDefense = 0f;

                for (int i = 0; i < Ship.MechanicalBoardingDefense; ++i)
                    if (UniverseRandom.RollDice(50f)) // 50%
                        ourCombinedDefense += 1f;

                foreach (Troop troop in GoodGuys)
                {
                    for (int i = 0; i < troop.Strength; ++i)
                        if (UniverseRandom.RollDice(troop.BoardingStrength))
                            ourCombinedDefense += 1f;
                }

                foreach (Troop troop in BadBoys)
                {
                    if (ourCombinedDefense > 0)
                        troop.DamageTroop(Ship, ref ourCombinedDefense);
                    else break;
                }

                BadBoys = Ship.GetBadBoysOnShip();
            }

            void ResolveEnemyVersusOwn()
            {
                if (BadBoys.Length == 0)
                    return;

                float enemyAttackPower = 0;
                foreach (Troop troop in BadBoys)
                {
                    for (int i = 0; i < troop.Strength; ++i)
                        if (UniverseRandom.RollDice(troop.BoardingStrength))
                            enemyAttackPower += 1.0f;
                }

                foreach (Troop us in GoodGuys)
                {
                    if (enemyAttackPower > 0)
                        us.DamageTroop(Ship, ref enemyAttackPower);
                    else break;
                }

                // spend rest of the attack strength to weaken mechanical defenses:
                if (enemyAttackPower > 0)
                {
                    Ship.MechanicalBoardingDefense = Math.Max(Ship.MechanicalBoardingDefense - enemyAttackPower, 0);
                }

                GoodGuys = Ship.GetGoodGuysOnShip();
            }
        }
    }
}