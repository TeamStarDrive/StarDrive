using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{

    public sealed partial class ShipAI
    {
        public bool UseSensorsForTargets = true;
        public CombatState CombatState = AI.CombatState.AttackRuns;
        public CombatAI CombatAI = new CombatAI();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();
        public Ship EscortTarget;
        private bool AttackRunStarted;
        private float AttackRunAngle;
        private float RunTimer;
        private float ScanForThreatTimer;
        public Planet ExterminationTarget;
        public bool Intercepting;
        public Guid TargetGuid;
        public bool IgnoreCombat;
        public bool BadGuysNear;
        public bool Troopsout = false;
        public Ship TargetShip;
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        public Guid EscortTargetGuid;
        public GameplayObject Target;
        private Vector2 AttackVector = Vector2.Zero;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool HasPriorityTarget;
        private float TriggerDelay;

        public void FireOnTarget()
        {
            // base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
            if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp || Owner.disabled || Owner.Weapons.Count == 0)
                return;

            if (BadGuysNear) // Determine if there is something to shoot at
            {
                RefreshTarget();
                UpdateTrackedProjectiles();

                for (int i = 0; i < Owner.Weapons.Count; i++)
                {
                    Weapon weapon = Owner.Weapons[i];
                    weapon.TargetChangeTimer -= 0.0167f;
                    if (CanFireWeapon(weapon))
                        TargetWeapon(weapon);
                }

                FireWeapons();
            }
            TargetShip = null;
        }

        private bool CanFireWeapon(Weapon weapon)
        {
            // Reasons for this weapon not to fire 
            if (!weapon.moduleAttachedTo.Active
                || weapon.timeToNextFire > 0f
                || !weapon.moduleAttachedTo.Powered || weapon.IsRepairDrone || weapon.isRepairBeam
                || weapon.PowerRequiredToFire > Owner.PowerCurrent
                || weapon.TargetChangeTimer > 0f
            )
                return false;
            if ((!weapon.TruePD || !weapon.Tag_PD) && Owner.PlayerShip)
                return false;

            var moduletarget = weapon.fireTarget as ShipModule;
            var projTarget = weapon.fireTarget as Projectile;

            // if firing at the primary target mark weapon as firing on primary.
            if (projTarget != null && weapon.fireTarget == Target)
            {
                weapon.PrimaryTarget = true;
            }
            else if (moduletarget != null && moduletarget.GetParent() == TargetShip)
            {
                weapon.PrimaryTarget = true;
            }

            //check if weapon target as a gameplay object is still a valid target    
            if (weapon.fireTarget != null && !Owner.CheckIfInsideFireArc(weapon, weapon.fireTarget)
                //check here if the weapon can fire on main target.                                                           
                || Target != null && weapon.SalvoTimer <= 0 && weapon.BeamDuration <= 0 &&
                !weapon.PrimaryTarget && projTarget == null && Owner.CheckIfInsideFireArc(weapon, Target)
            )
            {
                weapon.TargetChangeTimer = 0.1f * weapon.moduleAttachedTo.XSIZE * weapon.moduleAttachedTo.YSIZE;
                weapon.fireTarget = null;
                if (weapon.isTurret) weapon.TargetChangeTimer *= .5f;
                if (weapon.Tag_PD)   weapon.TargetChangeTimer *= .5f;
                if (weapon.TruePD)   weapon.TargetChangeTimer *= .25f;
            }
            // if weapon target is null reset primary target and decrement target change timer.
            if (weapon.fireTarget == null && !Owner.PlayerShip)
                weapon.PrimaryTarget = false;

            // Reasons for this weapon not to fire                    
            return weapon.fireTarget != null || weapon.TargetChangeTimer <= 0f;
        }

        private void TargetWeapon(Weapon weapon)
        {
            //Can this weapon fire on ships
            if (!weapon.TruePD)
            {
                //if there are projectile to hit and weapons that can shoot at them. do so. 
                if (TrackProjectiles.Count > 0 && weapon.Tag_PD)
                {
                    int maxTrackable = Owner.TrackingPower + Owner.Level;
                    for (int i = 0;  i < maxTrackable && i < TrackProjectiles.Count; i++)
                    {
                        Projectile proj = TrackProjectiles[i];

                        if (proj == null || !proj.Active || proj.Health <= 0f || !proj.Weapon.Tag_Intercept)
                            continue;
                        if (Owner.CheckIfInsideFireArc(weapon, proj))
                        {
                            weapon.fireTarget = proj;
                            break;
                        }
                    }
                }

                //Is primary target valid
                if (weapon.fireTarget == null && Owner.CheckIfInsideFireArc(weapon, Target))
                {
                    weapon.fireTarget = Target;
                    weapon.PrimaryTarget = true;
                }

                //Find alternate target to fire on
                //this seems to be very expensive code. 
                if (weapon.fireTarget == null && Owner.TrackingPower > 0)
                {
                    //limit to one target per level.
                    int tracking = Owner.TrackingPower + Owner.Level;
                    for (int i = 0; i < PotentialTargets.Count && i < tracking; i++) //
                    {
                        Ship potentialTarget = PotentialTargets[i];
                        if (potentialTarget == TargetShip)
                        {
                            tracking++;
                            continue;
                        }
                        if (!Owner.CheckIfInsideFireArc(weapon, potentialTarget))
                            continue;
                        weapon.fireTarget = potentialTarget;
                        //AddTargetsTracked++;
                        break;
                    }
                }
                //If a ship was found to fire on, change to target an internal module if target is visible  || weapon.Tag_Intercept
                if (weapon.fireTarget is Ship targetShip && (GlobalStats.ForceFullSim || Owner.InFrustum || targetShip.InFrustum))
                {
                    weapon.fireTarget = targetShip.GetRandomInternalModule(weapon);
                }
            }
            //No ship to target, check for projectiles
            if (weapon.fireTarget == null && weapon.Tag_PD)
            {
                int maxTrackable = Owner.TrackingPower + Owner.Level;
                for (int i = 0;  i < TrackProjectiles.Count && i < maxTrackable; i++)
                {
                    Projectile proj = TrackProjectiles[i];
                    if (proj == null || !proj.Active || proj.Health <= 0f || !proj.Weapon.Tag_Intercept)
                        continue;
                    if (Owner.CheckIfInsideFireArc(weapon, proj))
                    {
                        weapon.fireTarget = proj;
                        break;
                    }
                }
            }
        }

        private void UpdateTrackedProjectiles()
        {
            bool hasPointDefense = OwnerHasPointDefense();

            TrackProjectiles.Clear();
            if (Owner.Mothership != null)
                TrackProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
            if (Owner.TrackingPower > 0 && hasPointDefense) // update projectile list                         
            {
                // @todo This usage of GetNearby is slow! Consider creating a specific SpatialManager search function
                foreach (GameplayObject go in Owner.GetObjectsInSensors(GameObjectType.Proj))
                {
                    var missile = (Projectile) go;
                    if (missile.Loyalty != Owner.loyalty && missile.Weapon.Tag_Intercept)
                        TrackProjectiles.Add(missile);
                }
                TrackProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
            }
        }

        private bool OwnerHasPointDefense()
        {
            for (int i = 0; i < Owner.Weapons.Count; ++i)
            {
                Weapon purge = Owner.Weapons[i];
                if (purge.Tag_PD || purge.TruePD)
                    return true;
            }
            return false;
        }

        private void RefreshTarget()
        {
            TargetShip = Target as Ship;

            //Target is dead or dying, will need a new one.
            if (Target?.Active == false || TargetShip?.dying == true)
            {
                for (int i = 0; i < Owner.Weapons.Count; ++i)
                {
                    Weapon purge = Owner.Weapons[i];
                    if (purge.PrimaryTarget)
                    {
                        purge.PrimaryTarget = false;
                        purge.fireTarget = null;
                        purge.SalvoTarget = null;
                    }
                }
                Target     = null;
                TargetShip = null;
            }
        }

        //this section actually fires the weapons. This whole firing section can be moved to some other area of the code. 
        // This code is very expensive. 
        private void FireWeapons()
        {
            float lag = Empire.Universe.Lag;
            int weaponsFired = 0;
            foreach (Weapon weapon in Owner.Weapons)
            {
                if (weapon.fireTarget == null || !weapon.moduleAttachedTo.Active || !(weapon.timeToNextFire <= 0f) ||
                    !weapon.moduleAttachedTo.Powered) continue;
                if (weapon.fireTarget is Ship)
                {
                    FireOnTargetNonVisible(weapon, weapon.fireTarget);
                }
                else
                {
                    GameplayObject target = weapon.fireTarget;
                    if (weapon.isBeam)
                    {
                        weapon.FireTargetedBeam(target);
                    }
                    else if (weapon.Tag_Guided)
                    {
                        if (weaponsFired > 10 && lag > 0.05 && !GlobalStats.ForceFullSim && !weapon.Tag_Intercept && weapon.fireTarget is ShipModule targetModule)
                            FireOnTargetNonVisible(weapon, targetModule.GetParent());
                        else
                        {
                            float rotation = Owner.Rotation + weapon.moduleAttachedTo.Facing.ToRadians();
                            weapon.Fire(new Vector2((float)Math.Sin(rotation), -(float)Math.Cos(rotation)), target);
                        }
                        ++weaponsFired;
                    }
                    else
                    {
                        if (weaponsFired > 10 && lag > 0.05 && !GlobalStats.ForceFullSim && weapon.fireTarget is ShipModule targetModule)
                            FireOnTargetNonVisible(weapon, targetModule.GetParent());
                        else
                            CalculateAndFire(weapon, target, false);
                        ++weaponsFired;
                    }
                }
            }
        }

        public void CalculateAndFire(Weapon weapon, GameplayObject target, bool SalvoFire)
        {
            GameplayObject realTarget = (target is ShipModule sm) ? sm.GetParent() : target;

            Vector2 predictedPos = weapon.Center.FindPredictedTargetPosition(
                weapon.Owner.Velocity, weapon.ProjectileSpeed, target.Center, realTarget.Velocity);

            if (Owner.CheckIfInsideFireArc(weapon, predictedPos))
            {
                Vector2 direction = (predictedPos - weapon.Center).Normalized();
                if (SalvoFire) weapon.FireSalvo(direction, target);
                else weapon.Fire(direction, target);
            }
        }

        private void FireOnTargetNonVisible(Weapon w, GameplayObject fireTarget)
        {
            if (Owner.Ordinance < w.OrdinanceRequiredToFire || Owner.PowerCurrent < w.PowerRequiredToFire)
                return;
            w.timeToNextFire = w.fireDelay;
            if (w.IsRepairDrone)
                return;
            if (TargetShip == null || !TargetShip.Active || TargetShip.dying || !w.TargetValid(TargetShip.shipData.Role)
                || TargetShip.engineState == Ship.MoveState.Warp || !Owner.CheckIfInsideFireArc(w, TargetShip))
                return;

            Owner.Ordinance -= w.OrdinanceRequiredToFire;
            Owner.PowerCurrent -= w.PowerRequiredToFire;
            Owner.PowerCurrent -= w.BeamPowerCostPerSecond * w.BeamDuration;
            Owner.InCombatTimer = 15f;

            if (fireTarget is Projectile)
            {
                fireTarget.Damage(w.Owner, w.DamageAmount);
                return;
            }

            if (!(fireTarget is Ship targetShip))
            {
                if (fireTarget is ShipModule shipModule)
                    targetShip = shipModule.GetParent();
                else return; // aaarggh!
            }

            w.timeToNextFire = w.fireDelay;
            if (targetShip.NumExternalSlots == 0)
            {
                targetShip.Die(null, true);
                return;
            } //@todo invisible ecm and such should match visible


            if (targetShip.AI.CombatState == CombatState.Evade) // fbedard: firing on evading ship can miss !
                if (RandomMath.RandomBetween(0f, 100f) < 5f + targetShip.experience)
                    return;

            if (targetShip.shield_power > 0f)
                targetShip.DamageShieldInvisible(Owner, w.InvisibleDamageAmount);
            else
                targetShip.FindClosestUnshieldedModule(Owner.Center)?.Damage(Owner, w.InvisibleDamageAmount);
        }

        public GameplayObject ScanForCombatTargets(Vector2 Position, float Radius)
        {
            BadGuysNear = false;
            FriendliesNearby.Clear();
            PotentialTargets.Clear();
            NearbyShips.Clear();
            //this.TrackProjectiles.Clear();

            if (HasPriorityTarget && Target == null)
            {
                HasPriorityTarget = false;
                if (TargetQueue.Count > 0)
                {
                    HasPriorityTarget = true;
                    Target = TargetQueue.First<Ship>();
                }
            }
            if (Target != null)
                if ((Target as Ship).loyalty == Owner.loyalty)
                {
                    Target = null;
                    HasPriorityTarget = false;
                }


                else if (
                    !Intercepting && (Target as Ship).engineState == Ship.MoveState.Warp
                ) //||((double)Vector2.Distance(Position, this.Target.Center) > (double)Radius ||
                {
                    Target = (GameplayObject) null;
                    if (!HasPriorityOrder && Owner.loyalty != UniverseScreen.player)
                        State = AIState.AwaitingOrders;
                    return (GameplayObject) null;
                }
            //Doctor: Increased this from 0.66f as seemed slightly on the low side. 
            CombatAI.PreferredEngagementDistance = Owner.maxWeaponsRange * 0.75f;
            SolarSystem thisSystem = Owner.System;
            if (thisSystem != null)
                for (int i = 0; i < thisSystem.PlanetList.Count; i++)
                {
                    Planet p = thisSystem.PlanetList[i];
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) &&
                                  Owner.Center.InRadius(p.Position, Radius);             
                }
            {
                if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
                {
                    var sw = new ShipWeight
                    {
                        Ship = EscortTarget.AI.Target as Ship,
                        Weight = 2f
                    };
                    NearbyShips.Add(sw);
                }
                GameplayObject[] nearbyShips = Owner.GetObjectsInSensors(GameObjectType.Ship);
                foreach (var go in nearbyShips)
                {
                    var nearbyShip = (Ship)go;
                    if (!nearbyShip.Active || nearbyShip.dying
                        || Owner.Center.OutsideRadius(nearbyShip.Center, Radius + (Radius < 0.01f ? 10000 : 0)))
                        continue;

                    Empire empire = nearbyShip.loyalty;
                    if (empire == Owner.loyalty)
                    {
                        FriendliesNearby.Add(nearbyShip);
                        continue;
                    }
                    bool isAttackable = Owner.loyalty.IsEmpireAttackable(nearbyShip.loyalty, nearbyShip);
                    if (!isAttackable) continue;
                    BadGuysNear = true;
                    if (Radius < 1)
                        continue;
                    var sw = new ShipWeight
                    {
                        Ship = nearbyShip,
                        Weight = 1f
                    };
                    NearbyShips.Add(sw);

                    if (BadGuysNear && nearbyShip.AI.Target is Ship targetShip &&
                        targetShip == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                    {
                        sw.Weight = 3f;
                    }
                }
            }


            #region supply ship logic   //fbedard: for launch only

            if (Owner.GetHangars().Find(hangar => hangar.IsSupplyBay) != null &&
                Owner.engineState != Ship.MoveState.Warp) // && !this.Owner.isSpooling
            {
                IOrderedEnumerable<Ship> sortedList = null;
                {
                    sortedList = FriendliesNearby.FilterBy(ship => ship != Owner
                                                                && ship.engineState != Ship.MoveState.Warp
                                                                && ship.AI.State != AIState.Scrap
                                                                && ship.AI.State != AIState.Resupply
                                                                && ship.AI.State != AIState.Refit
                                                                && ship.Mothership == null
                                                                && ship.OrdinanceMax > 0
                                                                && ship.Ordinance / ship.OrdinanceMax < 0.5f
                                                                && !ship.IsTethered())
                        .OrderBy(ship => Math.Truncate(Vector2.Distance(Owner.Center, ship.Center) + 4999) / 5000)
                        .ThenByDescending(ship => ship.OrdinanceMax - ship.Ordinance);
//                      .OrderBy(ship => ship.HasSupplyBays).ThenBy(ship => ship.OrdAddedPerSecond).ThenBy(ship => Math.Truncate((Vector2.Distance(this.Owner.Center, ship.Center) + 4999)) / 5000).ThenBy(ship => ship.OrdinanceMax - ship.Ordinance);
                }

                if (sortedList.Any() )
                {
                    var skip = 0;
                    var inboundOrdinance = 0f;
                    if (Owner.HasSupplyBays)
                        foreach (ShipModule hangar in Owner.GetHangars().Where(hangar => hangar.IsSupplyBay))
                        {
                            if (hangar.GetHangarShip() != null && hangar.GetHangarShip().Active)
                            {
                                if (hangar.GetHangarShip().AI.State != AIState.Ferrying &&
                                    hangar.GetHangarShip().AI.State != AIState.ReturnToHangar &&
                                    hangar.GetHangarShip().AI.State != AIState.Resupply &&
                                    hangar.GetHangarShip().AI.State != AIState.Scrap)
                                {
                                    if (sortedList.Skip(skip).Count() > 0)
                                    {
                                        var g1 = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                                        hangar.GetHangarShip().AI.EscortTarget = sortedList.Skip(skip).First();

                                        hangar.GetHangarShip().AI.IgnoreCombat = true;
                                        hangar.GetHangarShip().AI.OrderQueue.Clear();
                                        hangar.GetHangarShip().AI.OrderQueue.Enqueue(g1);
                                        hangar.GetHangarShip().AI.State = AIState.Ferrying;
                                        continue;
                                    }
                                    else
                                    {
                                        //hangar.GetHangarShip().QueueTotalRemoval();
                                        hangar.GetHangarShip().AI.State =
                                            AIState.ReturnToHangar; //shuttle with no target
                                        continue;
                                    }
                                }
                                else if (sortedList.Skip(skip).Count() > 0 &&
                                         hangar.GetHangarShip().AI.EscortTarget == sortedList.Skip(skip).First() &&
                                         hangar.GetHangarShip().AI.State == AIState.Ferrying)
                                {
                                    inboundOrdinance = inboundOrdinance + 100f;
                                    if ((inboundOrdinance + sortedList.Skip(skip).First().Ordinance) /
                                        sortedList.First().OrdinanceMax > 0.5f)
                                    {
                                        skip++;
                                        inboundOrdinance = 0;
                                        continue;
                                    }
                                }
                                continue;
                            }
                            if (!hangar.Active || hangar.hangarTimer > 0f ||
                                Owner.Ordinance >= 100f && sortedList.Skip(skip).Any())
                                continue;
                            if (ResourceManager.ShipsDict["Supply_Shuttle"].Mass / 5f >
                                Owner.Ordinance) //fbedard: New spawning cost
                                continue;
                            Ship shuttle =
                                Ship.CreateShipFromHangar("Supply_Shuttle", Owner.loyalty, Owner.Center, Owner);
                            shuttle.VanityName = "Supply Shuttle";
                            //shuttle.shipData.Role = ShipData.RoleName.supply;
                            //shuttle.GetAI().DefaultAIState = AIState.Flee;
                            shuttle.Velocity = UniverseRandom.RandomDirection() * shuttle.speed + Owner.Velocity;
                            if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                                shuttle.Velocity = Vector2.Normalize(shuttle.Velocity) * shuttle.speed;
                            Owner.Ordinance -= shuttle.Mass / 5f;

                            if (Owner.Ordinance >= 100f)
                            {
                                inboundOrdinance = inboundOrdinance + 100f;
                                Owner.Ordinance = Owner.Ordinance - 100f;
                                hangar.SetHangarShip(shuttle);
                                var g = new ShipGoal(Plan.SupplyShip, Vector2.Zero, 0f);
                                shuttle.AI.EscortTarget = sortedList.Skip(skip).First();
                                shuttle.AI.IgnoreCombat = true;
                                shuttle.AI.OrderQueue.Clear();
                                shuttle.AI.OrderQueue.Enqueue(g);
                                shuttle.AI.State = AIState.Ferrying;
                            }
                            else //fbedard: Go fetch ordinance when mothership is low on ordinance
                            {
                                shuttle.Ordinance = 0f;
                                hangar.SetHangarShip(shuttle);
                                shuttle.AI.IgnoreCombat = true;
                                shuttle.AI.State = AIState.Resupply;
                                shuttle.AI.OrderResupplyNearest(true);
                            }
                            break;
                        }
                }
            }
            if (Owner.shipData.Role == ShipData.RoleName.supply && Owner.Mothership == null)
                OrderScrapShip(); //Destroy shuttle without mothership

            #endregion

            //}           
            for (int i = 0; i < NearbyShips.Count; i++)
            {
                ShipWeight nearbyShip = NearbyShips[i];
                if (nearbyShip.Ship.loyalty != Owner.loyalty)
                {
                    if (Target as Ship == nearbyShip.Ship)
                        nearbyShip.Weight += 3;
                    if (nearbyShip.Ship.Weapons.Count == 0)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.Weight = vultureWeight.Weight + CombatAI.PirateWeight;
                    }

                    if (nearbyShip.Ship.Health / nearbyShip.Ship.HealthMax < 0.5f)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.Weight = vultureWeight.Weight + CombatAI.VultureWeight;
                    }
                    if (nearbyShip.Ship.Size < 30)
                    {
                        ShipWeight smallAttackWeight = nearbyShip;
                        smallAttackWeight.Weight = smallAttackWeight.Weight + CombatAI.SmallAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            smallAttackWeight.Weight *= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            smallAttackWeight.Weight /= 2f;
                    }
                    if (nearbyShip.Ship.Size > 30 && nearbyShip.Ship.Size < 100)
                    {
                        ShipWeight mediumAttackWeight = nearbyShip;
                        mediumAttackWeight.Weight = mediumAttackWeight.Weight + CombatAI.MediumAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            mediumAttackWeight.Weight *= 1.5f;
                    }
                    if (nearbyShip.Ship.Size > 100)
                    {
                        ShipWeight largeAttackWeight = nearbyShip;
                        largeAttackWeight.Weight = largeAttackWeight.Weight + CombatAI.LargeAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            largeAttackWeight.Weight /= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            largeAttackWeight.Weight *= 2f;
                    }
                    float rangeToTarget = Vector2.Distance(nearbyShip.Ship.Center, Owner.Center);
                    if (rangeToTarget <= CombatAI.PreferredEngagementDistance)
                        // && Vector2.Distance(nearbyShip.ship.Center, this.Owner.Center) >= this.Owner.maxWeaponsRange)
                    {
                        ShipWeight shipWeight = nearbyShip;
                        shipWeight.Weight = (int) Math.Ceiling(shipWeight.Weight + 5 *
                                                               ((CombatAI.PreferredEngagementDistance -
                                                                 Vector2.Distance(Owner.Center, nearbyShip.Ship.Center))
                                                                / CombatAI.PreferredEngagementDistance))
                            ;
                    }
                    else if (rangeToTarget > CombatAI.PreferredEngagementDistance + Owner.velocityMaximum * 5)
                    {
                        ShipWeight shipWeight1 = nearbyShip;
                        shipWeight1.Weight = shipWeight1.Weight -
                                             2.5f * (rangeToTarget /
                                                     (CombatAI.PreferredEngagementDistance +
                                                      Owner.velocityMaximum * 5));
                    }
                    if (Owner.Mothership != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.Ship.Center, Owner.Mothership.Center);
                        if (rangeToTarget < CombatAI.PreferredEngagementDistance)
                            nearbyShip.Weight += 1;
                    }
                    if (EscortTarget != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.Ship.Center, EscortTarget.Center);
                        if (rangeToTarget < 5000
                        ) // / (this.CombatAI.PreferredEngagementDistance +this.Owner.velocityMaximum ))
                            nearbyShip.Weight += 1;
                        else
                            nearbyShip.Weight -= 2;
                        if (nearbyShip.Ship.AI.Target == EscortTarget)
                            nearbyShip.Weight += 1;
                    }
                    if (nearbyShip.Ship.Weapons.Count < 1)
                        nearbyShip.Weight -= 3;

                    foreach (ShipWeight otherShip in NearbyShips)
                        if (otherShip.Ship.loyalty != Owner.loyalty)
                        {
                            if (otherShip.Ship.AI.Target != Owner)
                                continue;
                            ShipWeight selfDefenseWeight = nearbyShip;
                            selfDefenseWeight.Weight = selfDefenseWeight.Weight + 0.2f * CombatAI.SelfDefenseWeight;
                        }
                    //else if (otherShip.ship.GetAI().Target != nearbyShip.ship)
                    //{
                    //    continue;
                    //}
                }
                else
                {
                    NearbyShips.QueuePendingRemoval(nearbyShip);
                }
            }

            NearbyShips.ApplyPendingRemovals();
            IEnumerable<ShipWeight> sortedList2 =
                from potentialTarget in NearbyShips
                orderby potentialTarget.Weight
                descending //, Vector2.Distance(potentialTarget.ship.Center,this.Owner.Center) 
                select potentialTarget;

            {
                //this.PotentialTargets.ClearAdd() ;//.ToList() as BatchRemovalCollection<Ship>;

                //trackprojectiles in scan for targets.

                PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.Ship));
                // .Where(potentialTarget => Vector2.Distance(potentialTarget.Center, this.Owner.Center) < this.CombatAI.PreferredEngagementDistance));
            }
            if (Target != null && !Target.Active)
            {
                Target = null;
                HasPriorityTarget = false;
            }
            else if (Target != null && Target.Active && HasPriorityTarget)
            {
                var ship = Target as Ship;
                if (Owner.loyalty.GetRelations(ship.loyalty).AtWar || Owner.loyalty.isFaction || ship.loyalty.isFaction)
                    BadGuysNear = true;
                return Target;
            }
            if (sortedList2.Count<ShipWeight>() > 0)
                Target = sortedList2.ElementAt<ShipWeight>(0).Ship;

            if (Owner.Weapons.Count > 0 || Owner.GetHangars().Count > 0)
                return Target;
            return null;
        }

        private void SetCombatStatus(float elapsedTime)
        {
            //if(this.State==AIState.Scrap)
            //{
            //    this.Target = null;
            //    this.Owner.InCombatTimer = 0f;
            //    this.Owner.InCombat = false;
            //    this.TargetQueue.Clear();
            //    return;

            //}
            var radius = 30000f;
            Vector2 senseCenter = Owner.Center;
            if (UseSensorsForTargets)
                if (Owner.Mothership != null)
                {
                    if (Vector2.Distance(Owner.Center, Owner.Mothership.Center) <=
                        Owner.Mothership.SensorRange - Owner.SensorRange)
                    {
                        senseCenter = Owner.Mothership.Center;
                        radius = Owner.Mothership.SensorRange;
                    }
                }
                else
                {
                    radius = Owner.SensorRange;
                    if (Owner.inborders) radius += 10000;
                }
            else if (Owner.Mothership != null)
                senseCenter = Owner.Mothership.Center;


            if (Owner.fleet != null)
            {
                if (!HasPriorityTarget)
                    Target = ScanForCombatTargets(senseCenter, radius);
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            else if (!HasPriorityTarget)
            {
                //#if DEBUG
                //                if (this.State == AIState.Intercept && this.Target != null)
                //                    Log.Info(this.Target); 
                //#endif
                if (Owner.Mothership != null)
                {
                    Target = ScanForCombatTargets(senseCenter, radius);

                    if (Target == null)
                        Target = Owner.Mothership.AI.Target;
                }
                else
                {
                    Target = ScanForCombatTargets(senseCenter, radius);
                }
            }
            else
            {
                if (Owner.Mothership != null)
                    Target = ScanForCombatTargets(senseCenter, radius) ?? Owner.Mothership.AI.Target;
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            if (State == AIState.Resupply)
                return;
            if (
                (Owner.shipData.Role == ShipData.RoleName.freighter ||
                 Owner.shipData.ShipCategory == ShipData.Category.Civilian) && Owner.CargoSpaceMax > 0 ||
                Owner.shipData.Role == ShipData.RoleName.scout || Owner.isConstructor ||
                Owner.shipData.Role == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply ||
                State == AIState.ReturnToHangar || State == AIState.Colonize ||
                Owner.shipData.Role == ShipData.RoleName.supply)
                return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                bool doreturn = !(Owner.fleet != null && State == AIState.FormationWarp &&
                                  Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f);
                if (doreturn)
                    return;
            }
            if (Owner.fleet != null)
                foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
                {
                    if (datanode.Ship != Owner)
                        continue;
                    Node = datanode;
                    break;
                }
            if (Target != null && !Owner.InCombat)
            {
                Owner.InCombatTimer = 15f;
                if (!HasPriorityOrder && OrderQueue.NotEmpty && OrderQueue.PeekFirst.Plan != Plan.DoCombat)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
                else if (!HasPriorityOrder)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
                else
                {
                    if (CombatState == CombatState.HoldPosition || OrderQueue.NotEmpty)
                        return;
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                }
            }
        }

        private void SetCombatStatusorig(float elapsedTime)
        {
            if (Owner.fleet != null)
                if (!HasPriorityTarget)
                    Target = ScanForCombatTargets(Owner.Center, 30000f);
                else
                    ScanForCombatTargets(Owner.Center, 30000f);
            else if (!HasPriorityTarget)
                Target = ScanForCombatTargets(Owner.Center, 30000f);
            else
                ScanForCombatTargets(Owner.Center, 30000f);
            if (State == AIState.Resupply)
                return;
            if ((Owner.shipData.Role == ShipData.RoleName.freighter ||
                 Owner.shipData.ShipCategory == ShipData.Category.Civilian ||
                 Owner.shipData.Role == ShipData.RoleName.scout || Owner.isConstructor ||
                 Owner.shipData.Role == ShipData.RoleName.troop || IgnoreCombat || State == AIState.Resupply ||
                 State == AIState.ReturnToHangar) && !Owner.IsSupplyShip)
                return;
            if (Owner.fleet != null && State == AIState.FormationWarp)
            {
                var doreturn = true;
                if (Owner.fleet != null && State == AIState.FormationWarp &&
                    Vector2.Distance(Owner.Center, Owner.fleet.Position + Owner.FleetOffset) < 15000f)
                    doreturn = false;
                if (doreturn)
                    return;
            }
            if (Owner.fleet != null)
                foreach (FleetDataNode datanode in Owner.fleet.DataNodes)
                {
                    if (datanode.Ship != Owner)
                        continue;
                    Node = datanode;
                    break;
                }
            if (Target != null && !Owner.InCombat)
            {
                Owner.InCombat = true;
                Owner.InCombatTimer = 15f;
                if (!HasPriorityOrder && OrderQueue.NotEmpty && OrderQueue.PeekFirst.Plan != Plan.DoCombat)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
                if (!HasPriorityOrder)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
                if (HasPriorityOrder && CombatState != CombatState.HoldPosition && OrderQueue.IsEmpty)
                {
                    var combat = new ShipGoal(Plan.DoCombat, Vector2.Zero, 0f);
                    State = AIState.Combat;
                    OrderQueue.PushToFront(combat);
                    return;
                }
            }
            else if (Target == null)
            {
                Owner.InCombat = false;
            }
        }

        public void DropBombsAtGoal(ShipGoal goal, float radius)
        {
            if (Owner.Center.InRadius(goal.TargetPlanet.Position, radius))
            {
                foreach (ShipModule bombBay in Owner.BombBays)
                {
                    if (bombBay.BombTimer > 0f)
                        continue;
                    var bomb = new Bomb(new Vector3(Owner.Center, 0f), Owner.loyalty)
                    {
                        WeaponName = bombBay.BombType
                    };
                    var wepTemplate = ResourceManager.WeaponsDict[bombBay.BombType];

                    if (Owner.Ordinance > wepTemplate.OrdinanceRequiredToFire)
                    {
                        Owner.Ordinance -= wepTemplate.OrdinanceRequiredToFire;
                        bomb.SetTarget(goal.TargetPlanet);
                        UniverseScreen.BombList.Add(bomb);
                        bombBay.BombTimer = wepTemplate.fireDelay;
                    }
                }
            }
        }


        public class ShipWeight
        {
            public Ship Ship;

            public float Weight;
            public bool DefendEscort;

            public ShipWeight() {}
        }
    }
}