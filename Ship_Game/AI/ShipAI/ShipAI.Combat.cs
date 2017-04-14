using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class ShipAI
    {
        public bool UseSensorsForTargets = true;
        public CombatState CombatState = AI.CombatState.AttackRuns;
        public CombatAI CombatAI = new CombatAI();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();
        public Ship EscortTarget;
        private bool AttackRunStarted;
        private float AttackRunAngle;
        private float runTimer;
        private float ScanForThreatTimer;
        public Planet ExterminationTarget;
        public bool Intercepting;
        public Guid TargetGuid;
        public bool IgnoreCombat;
        public bool BadGuysNear;
        public bool troopsout = false;
        public Ship TargetShip;
        public Array<Projectile> TrackProjectiles = new Array<Projectile>();
        public Guid EscortTargetGuid;
        public GameplayObject Target;
        private Vector2 AttackVector = Vector2.Zero;
        public Array<Ship> TargetQueue = new Array<Ship>();
        public bool hasPriorityTarget;
        private float TriggerDelay = 0;

        public void FireOnTarget()
        {
            try
            {
                TargetShip = Target as Ship;
                //Relationship enemy =null;
                //base reasons not to fire. @TODO actions decided by loyalty like should be the same in all areas. 
                if (!Owner.hasCommand || Owner.engineState == Ship.MoveState.Warp || Owner.disabled ||
                    Owner.Weapons.Count == 0)
                    return;

                var hasPD = false;
                //Determine if there is something to shoot at
                if (BadGuysNear)
                {
                    //Target is dead or dying, will need a new one.
                    if ((Target?.Active ?? false) == false || (TargetShip?.dying ?? false))
                    {
                        foreach (Weapon purge in Owner.Weapons)
                        {
                            if (purge.Tag_PD || purge.TruePD)
                                hasPD = true;
                            if (purge.PrimaryTarget)
                            {
                                purge.PrimaryTarget = false;
                                purge.fireTarget = null;
                                purge.SalvoTarget = null;
                            }
                        }
                        Target = null;
                        TargetShip = null;
                    }
                    foreach (Weapon purge in Owner.Weapons)
                    {
                        if (purge.Tag_PD || purge.TruePD)
                            hasPD = true;
                        else continue;
                        break;
                    }
                    TrackProjectiles.Clear();
                    if (Owner.Mothership != null)
                        TrackProjectiles.AddRange(Owner.Mothership.AI.TrackProjectiles);
                    if (Owner.TrackingPower > 0 && hasPD) //update projectile list                         
                    {
                        foreach (Projectile missile in Owner.GetNearby<Projectile>())
                        {
                            if (missile.Loyalty != Owner.loyalty && missile.Weapon.Tag_Intercept)
                                TrackProjectiles.Add(missile);
                        }
                        TrackProjectiles.Sort(missile => Owner.Center.SqDist(missile.Center));
                    }

                    float lag = Empire.Universe.Lag;
                    //Go through each weapon
                    float index = 0; //count up weapons.
                    //save target ship if it is a ship.
                    TargetShip = Target as Ship;
                    //group of weapons into chunks per thread available
                    //var source = Enumerable.Range(0, Owner.Weapons.Count).ToArray();
                    //        var rangePartitioner = Partitioner.Create(0, source.Length);
                    //handle each weapon group in parallel
                    //System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
                    //Parallel.For(Owner.Weapons.Count, (start, end) =>
                    {
                        //standard for loop through each weapon group.
                        for (int T = 0; T < Owner.Weapons.Count; T++)
                        {
                            Weapon weapon = Owner.Weapons[T];
                            weapon.TargetChangeTimer -= 0.0167f;
                            //Reasons for this weapon not to fire 
                            if (!weapon.moduleAttachedTo.Active
                                || weapon.timeToNextFire > 0f
                                || !weapon.moduleAttachedTo.Powered || weapon.IsRepairDrone || weapon.isRepairBeam
                                || weapon.PowerRequiredToFire > Owner.PowerCurrent
                                || weapon.TargetChangeTimer > 0
                            )
                                continue;
                            if ((!weapon.TruePD || !weapon.Tag_PD) && Owner.isPlayerShip())
                                continue;
                            var moduletarget = weapon.fireTarget as ShipModule;
                            //if firing at the primary target mark weapon as firing on primary.
                            if (!(weapon.fireTarget is Projectile) && weapon.fireTarget != null &&
                                (weapon.fireTarget == Target ||
                                 moduletarget != null && moduletarget.GetParent() as GameplayObject == Target))
                                weapon.PrimaryTarget = true;
                            //check if weapon target as a gameplay object is still a valid target    
                            if (weapon.fireTarget != null)
                                if (weapon.fireTarget != null && !Owner.CheckIfInsideFireArc(weapon, weapon.fireTarget)
                                    //check here if the weapon can fire on main target.                                                           
                                    || Target != null && weapon.SalvoTimer <= 0 && weapon.BeamDuration <= 0 &&
                                    !weapon.PrimaryTarget && !(weapon.fireTarget is Projectile) &&
                                    Owner.CheckIfInsideFireArc(weapon, Target)
                                )
                                {
                                    weapon.TargetChangeTimer =
                                        .1f * weapon.moduleAttachedTo.XSIZE * weapon.moduleAttachedTo.YSIZE;
                                    weapon.fireTarget = null;
                                    if (weapon.isTurret)
                                        weapon.TargetChangeTimer *= .5f;
                                    if (weapon.Tag_PD)
                                        weapon.TargetChangeTimer *= .5f;
                                    if (weapon.TruePD)
                                        weapon.TargetChangeTimer *= .25f;
                                }
                            //if weapon target is null reset primary target and decrement target change timer.
                            if (weapon.fireTarget == null && !Owner.isPlayerShip())
                                weapon.PrimaryTarget = false;
                            //Reasons for this weapon not to fire                    
                            if (weapon.fireTarget == null && weapon.TargetChangeTimer > 0)
                                continue;
                            //main targeting loop. little check here to disable the whole thing for debugging.
                            if (true)
                            {
                                //Can this weapon fire on ships
                                if (BadGuysNear && !weapon.TruePD)
                                {
                                    //if there are projectile to hit and weapons that can shoot at them. do so. 
                                    if (TrackProjectiles.Count > 0 && weapon.Tag_PD)
                                        for (var i = 0;
                                            i < TrackProjectiles.Count &&
                                            i < Owner.TrackingPower + Owner.Level;
                                            i++)
                                        {
                                            Projectile proj;
                                            {
                                                proj = TrackProjectiles[i];
                                            }

                                            if (proj == null || !proj.Active || proj.Health <= 0 ||
                                                !proj.Weapon.Tag_Intercept)
                                                continue;
                                            if (Owner.CheckIfInsideFireArc(weapon,
                                                proj as GameplayObject))
                                            {
                                                weapon.fireTarget = proj;
                                                //AddTargetsTracked++;
                                                break;
                                            }
                                        }
                                    //Is primary target valid
                                    if (weapon.fireTarget == null)
                                        if (Owner.CheckIfInsideFireArc(weapon, Target))
                                        {
                                            weapon.fireTarget = Target;
                                            weapon.PrimaryTarget = true;
                                        }

                                    //Find alternate target to fire on
                                    //this seems to be very expensive code. 
                                    if (true)
                                        if (weapon.fireTarget == null && Owner.TrackingPower > 0)
                                        {
                                            //limit to one target per level.
                                            int tracking = Owner.TrackingPower;
                                            for (var i = 0;
                                                i < PotentialTargets.Count &&
                                                i < tracking + Owner.Level;
                                                i++) //
                                            {
                                                Ship potentialTarget = PotentialTargets[i];
                                                if (potentialTarget == TargetShip)
                                                {
                                                    tracking++;
                                                    continue;
                                                }
                                                if (
                                                    !Owner.CheckIfInsideFireArc(weapon,
                                                        potentialTarget))
                                                    continue;
                                                weapon.fireTarget = potentialTarget;
                                                //AddTargetsTracked++;
                                                break;
                                            }
                                        }
                                    //If a ship was found to fire on, change to target an internal module if target is visible  || weapon.Tag_Intercept
                                    if (weapon.fireTarget is Ship &&
                                        (GlobalStats.ForceFullSim || Owner.InFrustum ||
                                         (weapon.fireTarget as Ship).InFrustum))
                                        weapon.fireTarget =
                                            (weapon.fireTarget as Ship).GetRandomInternalModule(
                                                weapon);
                                }
                                //No ship to target, check for projectiles
                                if (weapon.fireTarget == null && weapon.Tag_PD)

                                    for (var i = 0;
                                        i < TrackProjectiles.Count &&
                                        i < Owner.TrackingPower + Owner.Level;
                                        i++)
                                    {
                                        Projectile proj;
                                        proj = TrackProjectiles[i];


                                        if (proj == null || !proj.Active || proj.Health <= 0 ||
                                            !proj.Weapon.Tag_Intercept)
                                            continue;
                                        if (Owner.CheckIfInsideFireArc(weapon,
                                            proj as GameplayObject))
                                        {
                                            weapon.fireTarget = proj;
                                            break;
                                        }
                                    }
                            }
                        }
                    } //);
                    //this section actually fires the weapons. This whole firing section can be moved to some other area of the code. This code is very expensive. 
                    if (true)
                        foreach (Weapon weapon in Owner.Weapons)
                            if (weapon.fireTarget != null && weapon.moduleAttachedTo.Active &&
                                weapon.timeToNextFire <= 0f && weapon.moduleAttachedTo.Powered)
                                if (!(weapon.fireTarget is Ship))
                                {
                                    GameplayObject target = weapon.fireTarget;
                                    if (weapon.isBeam)
                                    {
                                        weapon.FireTargetedBeam(target);
                                    }
                                    else if (weapon.Tag_Guided)
                                    {
                                        if (index > 10 && lag > .05 && !GlobalStats.ForceFullSim &&
                                            !weapon.Tag_Intercept && weapon.fireTarget is ShipModule)
                                            FireOnTargetNonVisible(weapon,
                                                (weapon.fireTarget as ShipModule).GetParent());
                                        else
                                            weapon.Fire(
                                                new Vector2(
                                                    (float) Math.Sin(
                                                        (double) Owner.Rotation +
                                                        weapon.moduleAttachedTo.Facing.ToRadians()),
                                                    -(float) Math.Cos(
                                                        (double) Owner.Rotation +
                                                        weapon.moduleAttachedTo.Facing.ToRadians())), target);
                                        index++;
                                    }
                                    else
                                    {
                                        if (index > 10 && lag > .05 && !GlobalStats.ForceFullSim &&
                                            weapon.fireTarget is ShipModule)
                                            FireOnTargetNonVisible(weapon,
                                                (weapon.fireTarget as ShipModule).GetParent());
                                        else
                                            CalculateAndFire(weapon, target, false);
                                        index++;
                                    }
                                }
                                else
                                {
                                    FireOnTargetNonVisible(weapon, weapon.fireTarget);
                                }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "FireOnTarget() crashed");
            }
            TargetShip = null;
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

            if (hasPriorityTarget && Target == null)
            {
                hasPriorityTarget = false;
                if (TargetQueue.Count > 0)
                {
                    hasPriorityTarget = true;
                    Target = TargetQueue.First<Ship>();
                }
            }
            if (Target != null)
                if ((Target as Ship).loyalty == Owner.loyalty)
                {
                    Target = null;
                    hasPriorityTarget = false;
                }


                else if (
                    !Intercepting && (Target as Ship).engineState == Ship.MoveState.Warp
                ) //||((double)Vector2.Distance(Position, this.Target.Center) > (double)Radius ||
                {
                    Target = (GameplayObject) null;
                    if (!HasPriorityOrder && Owner.loyalty != universeScreen.player)
                        State = AIState.AwaitingOrders;
                    return (GameplayObject) null;
                }
            //Doctor: Increased this from 0.66f as seemed slightly on the low side. 
            CombatAI.PreferredEngagementDistance = Owner.maxWeaponsRange * 0.75f;
            SolarSystem thisSystem = Owner.System;
            if (thisSystem != null)
                foreach (Planet p in thisSystem.PlanetList)
                {
                    BadGuysNear = BadGuysNear || Owner.loyalty.IsEmpireAttackable(p.Owner) &&
                                  Owner.Center.InRadius(p.Position, Radius);
                    //@TODO remove below once new logic is checked
                    //Empire emp = p.Owner;
                    //if (emp !=null && emp != Owner.loyalty)
                    //{
                    //    Relationship test = null;
                    //    Owner.loyalty.TryGetRelations(emp, out test);
                    //    if (!test.Treaty_OpenBorders || !test.Treaty_NAPact || Vector2.Distance(Owner.Center, p.Position) >Radius)
                    //        BadGuysNear = true;
                    //    break;
                    //}
                }
            {
                if (EscortTarget != null && EscortTarget.Active && EscortTarget.AI.Target != null)
                {
                    var sw = new ShipWeight
                    {
                        ship = EscortTarget.AI.Target as Ship,
                        weight = 2f
                    };
                    NearbyShips.Add(sw);
                }
                Ship[] nearbyShips = Owner.GetNearby<Ship>();
                foreach (Ship nearbyShip in nearbyShips)
                {
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
                        ship = nearbyShip,
                        weight = 1f
                    };
                    NearbyShips.Add(sw);

                    if (BadGuysNear && nearbyShip.AI.Target is Ship targetShip &&
                        targetShip == EscortTarget && nearbyShip.engineState != Ship.MoveState.Warp)
                    {
                        sw.weight = 3f;
                    }
                }
            }


            #region supply ship logic   //fbedard: for launch only

            if (Owner.GetHangars().Find(hangar => hangar.IsSupplyBay) != null &&
                Owner.engineState != Ship.MoveState.Warp) // && !this.Owner.isSpooling
            {
                IOrderedEnumerable<Ship> sortedList = null;
                {
                    sortedList = FriendliesNearby.Where(ship => ship != Owner
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

                if (sortedList.Count() > 0)
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
            foreach (ShipWeight nearbyShip in NearbyShips)
                // Doctor: I put modifiers for the ship roles Fighter and Bomber in here, so that when searching for targets they prioritise their targets based on their selected ship role.
                // I'll additionally put a ScanForCombatTargets into the carrier fighter code such that they use this code to select their own weighted targets.
                //Parallel.ForEach(this.NearbyShips, nearbyShip =>
                if (nearbyShip.ship.loyalty != Owner.loyalty)
                {
                    if (Target as Ship == nearbyShip.ship)
                        nearbyShip.weight += 3;
                    if (nearbyShip.ship.Weapons.Count == 0)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + CombatAI.PirateWeight;
                    }

                    if (nearbyShip.ship.Health / nearbyShip.ship.HealthMax < 0.5f)
                    {
                        ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + CombatAI.VultureWeight;
                    }
                    if (nearbyShip.ship.Size < 30)
                    {
                        ShipWeight smallAttackWeight = nearbyShip;
                        smallAttackWeight.weight = smallAttackWeight.weight + CombatAI.SmallAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            smallAttackWeight.weight *= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            smallAttackWeight.weight /= 2f;
                    }
                    if (nearbyShip.ship.Size > 30 && nearbyShip.ship.Size < 100)
                    {
                        ShipWeight mediumAttackWeight = nearbyShip;
                        mediumAttackWeight.weight = mediumAttackWeight.weight + CombatAI.MediumAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            mediumAttackWeight.weight *= 1.5f;
                    }
                    if (nearbyShip.ship.Size > 100)
                    {
                        ShipWeight largeAttackWeight = nearbyShip;
                        largeAttackWeight.weight = largeAttackWeight.weight + CombatAI.LargeAttackWeight;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                            largeAttackWeight.weight /= 2f;
                        if (Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                            largeAttackWeight.weight *= 2f;
                    }
                    float rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, Owner.Center);
                    if (rangeToTarget <= CombatAI.PreferredEngagementDistance)
                        // && Vector2.Distance(nearbyShip.ship.Center, this.Owner.Center) >= this.Owner.maxWeaponsRange)
                    {
                        ShipWeight shipWeight = nearbyShip;
                        shipWeight.weight = (int) Math.Ceiling(shipWeight.weight + 5 *
                                                               ((CombatAI.PreferredEngagementDistance -
                                                                 Vector2.Distance(Owner.Center, nearbyShip.ship.Center))
                                                                / CombatAI.PreferredEngagementDistance))
                            ;
                    }
                    else if (rangeToTarget > CombatAI.PreferredEngagementDistance + Owner.velocityMaximum * 5)
                    {
                        ShipWeight shipWeight1 = nearbyShip;
                        shipWeight1.weight = shipWeight1.weight -
                                             2.5f * (rangeToTarget /
                                                     (CombatAI.PreferredEngagementDistance +
                                                      Owner.velocityMaximum * 5));
                    }
                    if (Owner.Mothership != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, Owner.Mothership.Center);
                        if (rangeToTarget < CombatAI.PreferredEngagementDistance)
                            nearbyShip.weight += 1;
                    }
                    if (EscortTarget != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, EscortTarget.Center);
                        if (rangeToTarget < 5000
                        ) // / (this.CombatAI.PreferredEngagementDistance +this.Owner.velocityMaximum ))
                            nearbyShip.weight += 1;
                        else
                            nearbyShip.weight -= 2;
                        if (nearbyShip.ship.AI.Target == EscortTarget)
                            nearbyShip.weight += 1;
                    }
                    if (nearbyShip.ship.Weapons.Count < 1)
                        nearbyShip.weight -= 3;

                    foreach (ShipWeight otherShip in NearbyShips)
                        if (otherShip.ship.loyalty != Owner.loyalty)
                        {
                            if (otherShip.ship.AI.Target != Owner)
                                continue;
                            ShipWeight selfDefenseWeight = nearbyShip;
                            selfDefenseWeight.weight = selfDefenseWeight.weight + 0.2f * CombatAI.SelfDefenseWeight;
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
            //this.PotentialTargets = this.NearbyShips.Where(loyalty=> loyalty.ship.loyalty != this.Owner.loyalty) .OrderBy(weight => weight.weight).Select(ship => ship.ship).ToList();
            //if (this.Owner.Role == ShipData.RoleName.platform)
            //{
            //    this.NearbyShips.ApplyPendingRemovals();
            //    IEnumerable<ArtificialIntelligence.ShipWeight> sortedList =
            //        from potentialTarget in this.NearbyShips
            //        orderby Vector2.Distance(this.Owner.Center, potentialTarget.ship.Center)
            //        select potentialTarget;
            //    if (sortedList.Count<ArtificialIntelligence.ShipWeight>() > 0)
            //    {
            //        this.Target = sortedList.ElementAt<ArtificialIntelligence.ShipWeight>(0).ship;
            //    }
            //    return this.Target;
            //}
            NearbyShips.ApplyPendingRemovals();
            IEnumerable<ShipWeight> sortedList2 =
                from potentialTarget in NearbyShips
                orderby potentialTarget.weight
                descending //, Vector2.Distance(potentialTarget.ship.Center,this.Owner.Center) 
                select potentialTarget;

            {
                //this.PotentialTargets.ClearAdd() ;//.ToList() as BatchRemovalCollection<Ship>;

                //trackprojectiles in scan for targets.

                PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.ship));
                // .Where(potentialTarget => Vector2.Distance(potentialTarget.Center, this.Owner.Center) < this.CombatAI.PreferredEngagementDistance));
            }
            if (Target != null && !Target.Active)
            {
                Target = null;
                hasPriorityTarget = false;
            }
            else if (Target != null && Target.Active && hasPriorityTarget)
            {
                var ship = Target as Ship;
                if (Owner.loyalty.GetRelations(ship.loyalty).AtWar || Owner.loyalty.isFaction || ship.loyalty.isFaction)
                    BadGuysNear = true;
                return Target;
            }
            if (sortedList2.Count<ShipWeight>() > 0)
                Target = sortedList2.ElementAt<ShipWeight>(0).ship;

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
                if (!hasPriorityTarget)
                    Target = ScanForCombatTargets(senseCenter, radius);
                else
                    ScanForCombatTargets(senseCenter, radius);
            }
            else if (!hasPriorityTarget)
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
                    node = datanode;
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
                if (!hasPriorityTarget)
                    Target = ScanForCombatTargets(Owner.Center, 30000f);
                else
                    ScanForCombatTargets(Owner.Center, 30000f);
            else if (!hasPriorityTarget)
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
                    node = datanode;
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
                        universeScreen.BombList.Add(bomb);
                        bombBay.BombTimer = wepTemplate.fireDelay;
                    }
                }
            }
        }


    }
}