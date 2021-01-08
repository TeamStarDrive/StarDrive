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
        readonly Array<Troop> OurTroops = new Array<Troop>();
        readonly Array<Troop> HostileTroops = new Array<Troop>();

        // OUR troops count
        public int TroopCount => OurTroops.Count;

        private float TroopUpdateTimer = GlobalStats.TurnTimer;

        // TRUE if we have any troops present on this ship
        // @warning Some of these MAY be enemy troops!
        public bool HasOurTroops => OurTroops.Count > 0;
        
        // TRUE if we have own troops or if we launched these troops on to operations
        public bool HasTroopsPresentOrLaunched => HasOurTroops || Carrier.LaunchedAssaultShuttles > 0;

        public bool TroopsAreBoardingShip => HostileTroops.Count > 0;
        public int NumPlayerTroopsOnShip  => loyalty.isPlayer ? OurTroops.Count : HostileTroops.Count;
        public int NumAiTroopsOnShip      => loyalty.isPlayer ? HostileTroops.Count : OurTroops.Count;

        public bool IsIdleSingleTroopship => Name == loyalty.data.DefaultTroopShip
                                             && HasOurTroops
                                             && (AI.State == AIState.AwaitingOrders || AI.State == AIState.Orbit || AI.State == AIState.HoldPosition)
                                             && fleet == null && !InCombat;

        // NOTE: could be an enemy troop or a friendly one
        public void AddTroop(Troop troop)
        {
            if (troop.Loyalty == loyalty)
                OurTroops.Add(troop);
            else
                HostileTroops.Add(troop);

            troop.SetShip(this);
        }

        public void RemoveAnyTroop(Troop troop)
        {
            if (troop.Loyalty == loyalty)
                OurTroops.RemoveRef(troop);
            else
                HostileTroops.RemoveRef(troop);
        }

        public void KillOneOfOurTroops()
        {
            if (OurTroops.Count == 0)
                return;

            Troop troop = OurTroops.RandItem();
            RemoveAnyTroop(troop);
        }

        public void KillAllTroops()
        {
            for (int i = OurTroops.Count - 1; i >= 0; i--)
                OurTroops.RemoveAt(i);
        }

        public float GetOurTroopStrength(int maxTroops)
        {
            float strength = 0;
            for (int i = 0; i < maxTroops && i < OurTroops.Count; ++i)
                strength += OurTroops[i].Strength;
            return strength;
        }

        public int NumTroopsRebasingHere
        {
            get
            {
                using (loyalty.GetShips().AcquireReadLock())
                {
                    return loyalty.GetShips().Count(s => s.AI.State == AIState.RebaseToShip
                                                    && s.AI.EscortTarget == this);
                }
            }
        }

        public void TryLandSingleTroopOnShip(Ship targetShip)
        {
            if (GetOurFirstTroop(out Troop first))
                first.LandOnShip(targetShip);
        }

        public bool TryLandSingleTroopOnPlanet(Planet targetPlanet)
        {
            return GetOurFirstTroop(out Troop first) && first.TryLandTroop(targetPlanet);
        }

        public bool GetOurFirstTroop(out Troop troop)
        {
            if (OurTroops.Count > 0)
            {
                troop = OurTroops[0];
                return true;
            }
            troop = null;
            return false;
        }

        public IReadOnlyList<Troop> GetOurTroops(int maxTroops = 0)
        {
            if (maxTroops == 0 || maxTroops >= OurTroops.Count)
                return OurTroops;

            var troops = new Array<Troop>(maxTroops);
            for (int i = 0; i < maxTroops; ++i)
                troops.Add(OurTroops[i]);
            return troops;
        }

        // NOTE: This will move the hostile troop list to our troops after successful board
        public void ReCalculateTroopsAfterBoard()
        {
            for (int i = HostileTroops.Count - 1; i >= 0;  i--)
            {
                Troop troop = HostileTroops[i];
                if (troop.Loyalty == loyalty)
                {
                    HostileTroops.RemoveAtSwapLast(i);
                    AddTroop(troop);
                }
            }
        }

        // NOTE: In case of a rebellion, usually
        public void SwitchTroopLoyalty(Empire newLoyalty)
        {
            for (int i = 0; i < OurTroops.Count; i++)
            {
                Troop troop = OurTroops[i];
                if (troop.Loyalty != newLoyalty)
                    troop.ChangeLoyalty(newLoyalty);
            }
        }
        
        public Array<Troop> GetFriendlyAndHostileTroops()
        {
            var all = new Array<Troop>(OurTroops.Count + HostileTroops.Count);
            all.AddRange(OurTroops);
            all.AddRange(HostileTroops);
            return all;
        }

        public int LandTroopsOnShip(Ship targetShip, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // NOTE: Need to create a copy of TroopList here,
            // because `LandOnShip` will modify TroopList
            Troop[] troopsToLand = OurTroops.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                troopsToLand[i].LandOnShip(targetShip);
                ++landed;
            }
            return landed;
        }

        // This will launch troops without having issues with modifying it's own TroopsHere
        public int LandTroopsOnPlanet(Planet planet, int maxTroopsToLand = 0)
        {
            int landed = 0;
            // @note: Need to create a copy of TroopList here,
            // because `TryLandTroop` will modify TroopList if landing is successful
            Troop[] troopsToLand = OurTroops.ToArray();
            for (int i = 0; i < troopsToLand.Length; ++i)
            {
                if (maxTroopsToLand != 0 && landed >= maxTroopsToLand)
                    break;

                if (troopsToLand[i].TryLandTroop(planet))
                    ++landed;
                else break; // no more free tiles, probably
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
                if (numHangarsBays < OurTroops.Count + 1) //FB: if you have more troop_capacity than hangars, consider adding some tanks
                {
                    type = troopType; // ex: "Space Marine"
                    if (OurTroops.Count(troop => troop.Name == tankType) <= numHangarsBays)
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

        void RefreshMechanicalBoardingDefense()
        {
            MechanicalBoardingDefense =  ModuleSlotList.Sum(module => module.MechanicalBoardingDefense);
        }

        public void DisengageExcessTroops(int troopsToRemove) // excess troops will leave the ship, usually after successful boarding
        {
            Troop[] toRemove = OurTroops.ToArray();
            for (int i = 0; i < troopsToRemove && i < toRemove.Length; ++i)
            {
                Troop troop = toRemove[i];
                Ship assaultShip = CreateTroopShipAtPoint(loyalty.GetAssaultShuttleName(), loyalty, Center, troop);
                assaultShip.Velocity = UniverseRandom.RandomDirection() * assaultShip.SpeedLimit + Velocity;

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
        
        void UpdateTroops(FixedSimTime timeSinceLastUpdate)
        {
            if (OurTroops.Count == 0 && HostileTroops.Count == 0)
            {
                TroopUpdateTimer = GlobalStats.TurnTimer;
                return;
            }

            CalcTroopBoardingDefense();
            TroopUpdateTimer -= timeSinceLastUpdate.FixedTime;
            if (TroopUpdateTimer > 0)
                return;

            TroopUpdateTimer = GlobalStats.TurnTimer;
            if (OurTroops.Count > 0)
            {
                // leave a garrison of 1 if a ship without barracks was boarded
                int troopThreshold = TroopCapacity + (TroopCapacity > 0 ? 0 : 1);
                if (!InCombat && HostileTroops.Count == 0 && OurTroops.Count > troopThreshold)
                {
                    DisengageExcessTroops(OurTroops.Count - troopThreshold);
                }
            }

            if (HostileTroops.Count > 0) // Combat!!
            {
                float hostilesAvgLevel = HostileTroops.Sum(t => t.Level) / (float)HostileTroops.Count;
                float ourAvgLevel      = OurTroops.Count == 0 ? 0 : OurTroops.Sum(t => t.Level) / (float)(OurTroops.Count);

                if (hostilesAvgLevel.Less(ourAvgLevel)) // We attack first 
                {
                    TroopCombatDefenseTurn(hostilesAvgLevel);
                    TroopCombatHostilesAttackingTurn();
                }
                else // Hostiles attack first
                {
                    TroopCombatHostilesAttackingTurn();
                    TroopCombatDefenseTurn(hostilesAvgLevel);
                }

                if (OurTroops.Count == 0 &&
                    MechanicalBoardingDefense <= 0f &&
                    HostileTroops.Count > 0) // enemy troops won:
                {
                    ChangeLoyalty(changeTo: HostileTroops[0].Loyalty);
                }
            }

            HealTroops();
        }

        public void HealTroops(bool healOne = false) // HealOne is from geodetic manager AffectNearbyShips
        {
            if (InCombat)
                return;

            float heal = healOne ? 1 : HealPerTurn;
            for (int i = 0; i < OurTroops.Count; i++)
            {
                Troop troop = OurTroops[i];
                troop.HealTroop(heal);
            }
        }

        void CalcTroopBoardingDefense()
        {
            TroopBoardingDefense = 0f;
            for (int i = 0; i < OurTroops.Count; i++)
            {
                Troop troop = OurTroops[i];
                TroopBoardingDefense += troop.Strength;
            }
        }

            void TroopCombatDefenseTurn(float hostilesAvgLevel)
        {

            if (OurTroops.Count == 0 && MechanicalBoardingDefense.AlmostEqual(0))
                return;

            float ourCombinedDefense      = 0f;
            float mechanicalDefenseChance = EMPdisabled ? 20 : 50; // 50% or 20% if EMPed
            for (int i = 0; i < MechanicalBoardingDefense; ++i)
                if (UniverseRandom.RollDice(mechanicalDefenseChance - hostilesAvgLevel)) 
                    ourCombinedDefense += 1f;

            foreach (Troop troop in OurTroops)
            {
                for (int i = 0; i < troop.Strength; ++i)
                    if (UniverseRandom.RollDice(troop.BoardingStrength))
                        ourCombinedDefense += 1f;
            }

            foreach (Troop badBoy in HostileTroops.ToArray()) // BadBoys will be modified
            {
                if (ourCombinedDefense > 0)
                    badBoy.DamageTroop(this, ref ourCombinedDefense);
                else break;
            }
        }

        void TroopCombatHostilesAttackingTurn()
        {
            if (HostileTroops.Count == 0)
                return;

            float enemyAttackPower = 0;
            foreach (Troop troop in HostileTroops)
            {
                for (int i = 0; i < troop.Strength; ++i)
                    if (UniverseRandom.RollDice(troop.BoardingStrength))
                        enemyAttackPower += 1f;
            }

            // Deal with mechanical defense first
            MechanicalBoardingDefense -= enemyAttackPower;
            float remainingAttack      = MechanicalBoardingDefense < 0 ? Math.Abs(MechanicalBoardingDefense) : 0;
            MechanicalBoardingDefense  = MechanicalBoardingDefense.LowerBound(0);

            // spend rest of the attack strength to damage defending troops
            foreach (Troop goodGuy in OurTroops.ToArray()) // OurTroops will be modified
            {
                if (remainingAttack > 0)
                    goodGuy.DamageTroop(this, ref remainingAttack);
                else 
                    break;
            }
        }
    }
}