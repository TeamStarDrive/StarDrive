﻿using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game.Universe.SolarBodies // Fat Bastard - Refactored March 21, 2019
{
    public class GeodeticManager
    {
        private float SystemCombatTimer;
        private readonly Planet P;
        private float Population  => P.Population;
        private Empire Owner      => P.Owner;
        private Shield Shield     => P.Shield;
        private Vector2 Center    => P.Center;
        private SceneObject SO    => P.SO;
        private bool HasSpacePort => P.HasSpacePort;
        private int Level         => P.Level;
        private int NumShipYards  => Stations.Values.Count(s => s.shipData.IsShipyard);
        private Map<Guid,Ship> Stations      => P.OrbitalStations;
        private float RepairPerTurn          => P.RepairPerTurn;
        private SolarSystem ParentSystem     => P.ParentSystem;
        private int TurnsSinceTurnover       => P.TurnsSinceTurnover;
        private float ShieldStrengthCurrent  => P.ShieldStrengthCurrent;
        private Array<PlanetGridSquare> TilesList        => P.TilesList;
        private BatchRemovalCollection<Troop> TroopsHere => P.TroopsHere;

        public GeodeticManager (Planet planet)
        {
            P = planet;
        }

        public void Update(float elapsedTime)
        {
            if (P.ParentSystem.HostileForcesPresent(Owner))
                SystemCombatTimer += elapsedTime;
            else
                SystemCombatTimer = 0f;
        }

        public void DropBomb(Bomb bomb)
        {
            if (bomb.Owner == Owner)
                return; // No friendly_fire

            DeclareWarOnBombingEmpire(bomb);
            P.SetInGroundCombat();
            if (ShieldStrengthCurrent > 0f)
                DamageColonyShields(bomb);
            else
            {
                var orbitalDrop = new OrbitalDrop
                {
                    TargetTile = SelectTargetTile(bomb),
                    Surface = P
                };

                orbitalDrop.DamageColonySurface(bomb);
                bomb.PlayCombatScreenEffects(P, orbitalDrop);
                if (Population <= 0f)
                {
                    P.WipeOutColony(bomb.Owner);
                    return;
                }

                bomb.ResolveSpecialBombActions(P); // This is for "Free Owlwoks" bomb
            }
        }

        private PlanetGridSquare SelectTargetTile(Bomb bomb)
        {
            var priorityTargets = TilesList.Filter(t => t.CombatBuildingOnTile 
                                                        || t.TroopsAreOnTile && t.SingleTroop.Loyalty != bomb.Owner);

            // If there are priority targets, choose one of them.
            return priorityTargets.Length > 0 ? priorityTargets.RandItem() 
                                              : TilesList.RandItem();
        }

        private void DeclareWarOnBombingEmpire(Bomb bomb)
        {
            if (Owner != null && !Owner.GetRelations(bomb.Owner).AtWar
                              && TurnsSinceTurnover > 10
                              && Empire.Universe.PlayerEmpire == bomb.Owner)
            {
                Owner.GetEmpireAI().DeclareWarOn(bomb.Owner, WarType.DefensiveWar);
            }
        }

        private void DamageColonyShields(Bomb bomb)
        {
            if (Empire.Universe.viewState <= UniverseScreen.UnivScreenState.SystemView
                && Empire.Universe.Frustum.Contains(P.Center, P.OrbitalRadius * 2))
            {
                Shield.HitShield(P, bomb, Center, SO.WorldBoundingSphere.Radius + 100f);
            }

            P.ShieldStrengthCurrent = Math.Max(P.ShieldStrengthCurrent - bomb.HardDamageMax, 0);
        }

        public void AffectNearbyShips() // Refactored by Fat Bastard - 23, July 2018
        {
            float repairPool = CalcRepairPool();
            int garrisonSize = P.GarrisonSize;
            bool spaceCombat = P.SpaceCombatNearPlanet;
            for (int i = 0; i < ParentSystem.ShipList.Count; i++)
            {
                Ship ship         = ParentSystem.ShipList[i];
                bool loyaltyMatch = ship.loyalty == Owner;

                if (ship.loyalty.isFaction)
                    AddTroopsForFactions(ship);

                if (loyaltyMatch)
                {
                    if (ship.Position.InRadius(Center, 5000f) || ship.GetTether() == P)
                    {
                        SupplyShip(ship);
                        RepairShip(ship, repairPool);
                        if (!spaceCombat)
                            LoadTroops(ship, garrisonSize);
                    }
                }
            }
        }

        private void SupplyShip(Ship ship)
        {
            if (ship.shipData.Role == ShipData.RoleName.platform) // platforms always get max ordnance to retain platforms Vanilla functionality
            {
                ship.ChangeOrdnance(ship.OrdinanceMax);
                ship.AddPower(ship.PowerStoreMax);
            }
            else
            {
                float supply = Level;
                supply      *= HasSpacePort ? 5f : 2f;
                supply      *= ship.InCombat ? 0.1f : 10f;
                supply       = Math.Max(.1f, supply);
                ship.AddPower(supply);
                ship.ChangeOrdnance(supply);
            }
        }

        private float CalcRepairPool()
        {
            float outOfCombatBonus = P.SpaceCombatNearPlanet ? 0.1f : 10;
            float repairPool       = RepairPerTurn * outOfCombatBonus /  P.ShipBuildingModifier;

            return repairPool;
        }

        private void RepairShip(Ship ship, float repairPool)
        {
            ship.AI.TerminateResupplyIfDone();
            //Modified by McShooterz: Repair based on repair pool, if no combat in system
            if (!HasSpacePort || ship.Health.AlmostEqual(ship.HealthMax))
                return;

            int repairLevel = Level + NumShipYards;
            ship.ApplyAllRepair(repairPool, repairLevel, repairShields: true);
        }

        private void LoadTroops(Ship ship, int garrisonSize)
        {
            if (TroopsHere.Count <= garrisonSize || ship.TroopCapacity == 0 
                                                 || ship.TroopCapacity <= ship.TroopList.Count 
                                                 || P.MightBeAWarZone(P.Owner))
            {
                return;
            }

            int troopCount = ship.Carrier.NumTroopsInShipAndInSpace;
            foreach (PlanetGridSquare pgs in TilesList)
            {
                if (troopCount >= ship.TroopCapacity || TroopsHere.Count <= garrisonSize)
                    break;

                if (pgs.TroopsAreOnTile && pgs.SingleTroop.Loyalty == Owner)
                {
                    Ship troopShip = pgs.SingleTroop.Launch();
                    troopShip?.AI.OrderRebaseToShip(ship);
                }
            }
        }

        private void AddTroopsForFactions(Ship ship)
        {
            // @todo FB - need to separate this to a method which will return a troop based on faction
            if ((SystemCombatTimer % 30).AlmostZero()  && ship.TroopCapacity > ship.TroopList.Count)
                ship.TroopList.Add(ResourceManager.CreateTroop("Wyvern", ship.loyalty));
        }
    }
}