using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Ship_Game.Gameplay
{
	public sealed class ArtificialIntelligence : IDisposable
	{
       // public Task fireTask;
        public bool UseSensorsForTargets =true;
        public bool ClearOrdersNext;

		private Vector2 aiNewDir;

		//private int aiNumSeen;

		public static UniverseScreen universeScreen;

		public Ship Owner;

		public GameplayObject Target;

		public AIState State = AIState.AwaitingOrders;

		public Ship_Game.Gameplay.CombatState CombatState = Ship_Game.Gameplay.CombatState.AttackRuns;

		public Guid OrbitTargetGuid;

		public Ship_Game.CombatAI CombatAI = new Ship_Game.CombatAI();
        //public Ship_Game.CombatAI CombatAI = new Ship_Game.CombatAI(This);

		public BatchRemovalCollection<ArtificialIntelligence.ShipWeight> NearbyShips = new BatchRemovalCollection<ArtificialIntelligence.ShipWeight>();

		//public List<Ship> PotentialTargets = new List<Ship>();
        public BatchRemovalCollection<Ship> PotentialTargets = new BatchRemovalCollection<Ship>();

        //private Vector2 direction = Vector2.Zero;     //Not referenced in code, removing to save memory -Gretman

        private int resupplystep;

		public Planet resupplyTarget;

		public Planet start;

		public Planet end;

		private SolarSystem SystemToPatrol;

		private List<Planet> PatrolRoute = new List<Planet>();

		private int stopNumber;

		private Planet PatrolTarget;

		public SolarSystem SystemToDefend;

		public Guid SystemToDefendGuid;

        //private List<SolarSystem> SystemsToExplore = new List<SolarSystem>();         //Not referenced in code, removing to save memory -Gretman

        public SolarSystem ExplorationTarget;

		public Ship EscortTarget;

		public Guid EscortTargetGuid;

        //private List<float> Distances = new List<float>();            //Not referenced in code, removing to save memory -Gretman

        private float findNewPosTimer;

		private Goal ColonizeGoal;

		private Planet awaitClosest;

        //public bool inOrbit;          //Not referenced in code, removing to save memory -Gretman

        private Vector2 OrbitPos;

		private float DistanceLast;

		public bool HasPriorityOrder;

        //private Vector2 negativeRotation = Vector2.One;          //Not referenced in code, removing to save memory -Gretman

        public int GotoStep;

		private bool AttackRunStarted;

		private float AttackRunAngle;

		private float runTimer;

		private Vector2 AttackVector = Vector2.Zero;

		public AIState DefaultAIState = AIState.AwaitingOrders;

		private FleetDataNode node;

		public bool HadPO;

		private float ScanForThreatTimer;

		public Vector2 MovePosition;

		private float DesiredFacing;

		private Vector2 FinalFacingVector;

		//public LinkedList<ArtificialIntelligence.ShipGoal> OrderQueue = new LinkedList<ArtificialIntelligence.ShipGoal>();

        public SafeQueue<ArtificialIntelligence.ShipGoal> OrderQueue = new SafeQueue<ArtificialIntelligence.ShipGoal>();
		public Queue<Vector2> ActiveWayPoints = new Queue<Vector2>();

		public Planet ExterminationTarget;

		public string FoodOrProd;

		//private float moveTimer;          //Not referenced in code, removing to save memory -Gretman

        public bool hasPriorityTarget;

		public bool Intercepting;

		public List<Ship> TargetQueue = new List<Ship>();

		private float TriggerDelay = 0;

		public Guid TargetGuid;

        //public Guid ColonizeTargetGuid;          //Not referenced in code, removing to save memory -Gretman

        public Planet ColonizeTarget;

		public bool ReadyToWarp = true;

		public Planet OrbitTarget;

		private float OrbitalAngle = RandomMath.RandomBetween(0f, 360f);

		public bool IgnoreCombat;

		public BatchRemovalCollection<Ship> FriendliesNearby = new BatchRemovalCollection<Ship>();

		public bool BadGuysNear;
        //added by gremlin: new troopsout property. Change this to use actual troopsout 
        public bool troopsout = false;

        private float UtilityModuleCheckTimer;
        public object wayPointLocker;
        public Ship TargetShip;
        //public ReaderWriterLockSlim orderqueue = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        //public  List<Task> TaskList = new List<Task>();
        //public Dictionary<Weapon, GameplayObject> visible = new Dictionary<Weapon, GameplayObject>();
       // private GameplayObject secondarytarget = null;
        //private GameplayObject pdtarget = null;
        //private float targetChangeTimer =0;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;
        public List<Projectile> TrackProjectiles = new List<Projectile>();
        private static float[] DmgLevel = { 0.25f, 0.85f, 0.65f, 0.45f, 0.45f, 0.45f, 0.0f };  //fbedard: dmg level for repair
                
		public ArtificialIntelligence()
		{
		}

		public ArtificialIntelligence(Ship owner)
		{
			this.Owner = owner;
			this.State = AIState.AwaitingOrders;
            
            this.wayPointLocker = new Object();
		}
        /*
		private void aPlotCourseToNew(Vector2 endPos, Vector2 startPos)
		{
			float Distance = Vector2.Distance(startPos, endPos);
			if (Distance < this.Owner.CalculateRange())
			{
                lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Enqueue(endPos);
				}
				return;
			}
			bool startInBorders = false;
			bool endInBorders = false;
            this.Owner.loyalty.BorderNodeLocker.EnterReadLock();
			{
				foreach (Empire.InfluenceNode node in this.Owner.loyalty.BorderNodes)
				{
					if (Vector2.Distance(node.Position, startPos) <= node.Radius)
					{
						startInBorders = true;
					}
					if (Vector2.Distance(node.Position, endPos) > node.Radius)
					{
						continue;
					}
					endInBorders = true;
				}
			}
            this.Owner.loyalty.BorderNodeLocker.ExitReadLock();
			if (startInBorders && endInBorders)
			{
				bool AllTravelIsInBorders = true;
				float angle = HelperFunctions.findAngleToTarget(startPos, endPos);
				int numChecks = (int)Distance / 2500;
				for (int i = 0; i < numChecks; i++)
				{
					bool goodPoint = false;
					Vector2 pointToCheck = HelperFunctions.findPointFromAngleAndDistance(startPos, angle, (float)(2500 * i));
                    this.Owner.loyalty.BorderNodeLocker.EnterReadLock();
					{
						foreach (Empire.InfluenceNode node in this.Owner.loyalty.BorderNodes)
						{
							if (Vector2.Distance(node.Position, pointToCheck) > (node.Radius * 0.85))
							{
								continue;
							}
							goodPoint = true;
							break;
						}
						if (!goodPoint)
						{
							AllTravelIsInBorders = false;
						}
					}
                    this.Owner.loyalty.BorderNodeLocker.ExitReadLock();
				}
				if (AllTravelIsInBorders)
				{
					lock (this.wayPointLocker)
					{
						this.ActiveWayPoints.Enqueue(endPos);
					}
					return;
				}
			}
			IOrderedEnumerable<Ship> sortedList =
                from ship in this.Owner.loyalty.GetProjectors()
				orderby this.Owner.CalculateRange() - Vector2.Distance(startPos, ship.Center)
				select ship;
			bool aCloserNodeExists = false;
			foreach (Ship ship1 in sortedList)
			{
				if (this.Owner.CalculateRange() - Vector2.Distance(startPos, ship1.Center) < 0f)
				{
					continue;
				}
				float DistanceFromProjectorToFinalSpot = Vector2.Distance(ship1.Center, endPos);
				Vector2.Distance(this.Owner.Center, ship1.Center);
				if (DistanceFromProjectorToFinalSpot >= Distance)
				{
					continue;
				}
				aCloserNodeExists = true;
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Enqueue(ship1.Center);
				}
				this.PlotCourseToNew(endPos, ship1.Center);
				break;
			}
			if (!aCloserNodeExists)
			{
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Enqueue(endPos);
				}
			}
		}
        */
        private void AwaitOrdersWithPlot(float elapsedTime)
        {            
            if (this.State != AIState.Resupply)
                this.HasPriorityOrder = false;
            AIState savestate = this.State;
            if (this.awaitClosest != null)
            {
                if(Vector2.Distance(this.awaitClosest.Position,this.Owner.Center) > Empire.ProjectorRadius *2)
                {
                    this.OrderMoveTowardsPosition(this.awaitClosest.Position, 0, Vector2.Zero, false, this.awaitClosest);
                    this.State = savestate;
                }
                else
                    this.DoOrbit(this.awaitClosest, elapsedTime);
            }
            else if (this.Owner.GetSystem() == null)
            {
                if (this.SystemToDefend != null)
                {
                    //this.DoOrbit(this.SystemToDefend.PlanetList[0], elapsedTime);
                    this.awaitClosest = this.SystemToDefend.PlanetList[0];
                    if (Vector2.Distance(this.awaitClosest.Position, this.Owner.Center) > Empire.ProjectorRadius * 2)
                    {
                        this.OrderMoveTowardsPosition(this.awaitClosest.Position, 0, Vector2.Zero, false, this.awaitClosest);
                        this.State = savestate;
                    }
                    else
                        this.DoOrbit(this.awaitClosest, elapsedTime);
                    return;
                }
                IOrderedEnumerable<SolarSystem> sortedList = null;
                if(!this.Owner.loyalty.isFaction)
                    sortedList =
                    from solarsystem in this.Owner.loyalty.GetOwnedSystems()
                    orderby Vector2.Distance(this.Owner.Center, solarsystem.Position)
                    select solarsystem;
                else if(this.Owner.loyalty.isFaction)
                {
                    sortedList =
                        from solarsystem in Ship.universeScreen.SolarSystemDict.Values
                        orderby Vector2.Distance(this.Owner.Center, solarsystem.Position) < 800000
                        , this.Owner.loyalty.GetOwnedSystems().Contains(solarsystem)
                        select solarsystem;

                }
                
                if (sortedList.Count<SolarSystem>() > 0)
                {
                    ///this.DoOrbit(sortedList.First<SolarSystem>().PlanetList[0], elapsedTime);
                    this.awaitClosest = sortedList.First<SolarSystem>().PlanetList[0];
                    if (Vector2.Distance(this.awaitClosest.Position, this.Owner.Center) > Empire.ProjectorRadius * 2)
                    {
                        this.OrderMoveTowardsPosition(this.awaitClosest.Position, 0, Vector2.Zero, false, this.awaitClosest);
                        this.State = savestate;
                    }
                    else
                        this.DoOrbit(this.awaitClosest, elapsedTime);
                    return;
                }
            }
            else
            {


                float closestD = 999999f;
                bool closestUS = false;
                foreach (Planet p in this.Owner.GetSystem().PlanetList)
                {
                    if (awaitClosest == null)
                        awaitClosest = p;
                    bool us = false;
                    if (this.Owner.loyalty.isFaction)
                    {
                        us = p.Owner != null || p.habitable;
                    }
                    else
                        us = p.Owner == this.Owner.loyalty;
                    if (closestUS && !us)
                        continue;
                    float Distance = Vector2.Distance(this.Owner.Center, p.Position);
                    if (us == closestUS)
                    {
                        if (Distance >= closestD)
                        {
                            continue;
                        }

                    }
                    closestUS = us;
                    closestD = Distance;
                    this.awaitClosest = p;


                }

            }
        }
        private void AwaitOrders(float elapsedTime)
		{
            //if ((this.Owner.GetSystem() ==null && this.State == AIState.Intercept) 
            //    || this.Target != null && this.Owner.GetSystem()!=null && this.Target.GetSystem()!=null && this.Target.GetSystem()==this.Owner.GetSystem())
            //    return;
            //if (this.Owner.InCombatTimer > elapsedTime * -5 && ScanForThreatTimer < 2 - elapsedTime * 5)
            //    this.ScanForThreatTimer = 0;
            if(this.State != AIState.Resupply)
            this.HasPriorityOrder = false;            
			if (this.awaitClosest != null)
			{
				this.DoOrbit(this.awaitClosest, elapsedTime);
			}
			else if (this.Owner.GetSystem() == null)
			{
				if(this.SystemToDefend != null)
                {
                    this.DoOrbit(this.SystemToDefend.PlanetList[0], elapsedTime);
                    this.awaitClosest = this.SystemToDefend.PlanetList[0];
                    return;
                }                
                IOrderedEnumerable<SolarSystem> sortedList = 
					from solarsystem in this.Owner.loyalty.GetOwnedSystems()                    
					orderby Vector2.Distance(this.Owner.Center, solarsystem.Position)
					select solarsystem;
                if (this.Owner.loyalty.isFaction)
                {
                    sortedList =
                        from solarsystem in Ship.universeScreen.SolarSystemDict.Values
                        orderby Vector2.Distance(this.Owner.Center, solarsystem.Position) < 800000
                        , this.Owner.loyalty.GetOwnedSystems().Contains(solarsystem)
                        select solarsystem;
                       
                }
                else
				if (sortedList.Count<SolarSystem>() > 0)
				{
					this.DoOrbit(sortedList.First<SolarSystem>().PlanetList[0], elapsedTime);
					this.awaitClosest = sortedList.First<SolarSystem>().PlanetList[0];
					return;
				}
			}
			else
			{
				
                
                float closestD = 999999f;
                bool closestUS =false;
				foreach (Planet p in this.Owner.GetSystem().PlanetList)
				{
                    if (awaitClosest == null)
                        awaitClosest = p;
                    bool us = false;
                    if(this.Owner.loyalty.isFaction)
                    {
                        us = p.Owner != null || p.habitable;
                    }
                    else
                        us = p.Owner == this.Owner.loyalty;
                    if (closestUS && !us)
                        continue;
                    float Distance = Vector2.Distance(this.Owner.Center, p.Position);
                    if (us == closestUS)
                    {
                        if (Distance >= closestD)
                        {
                            continue;
                        }
                        
                    }                    
                    closestUS = us;
                    closestD = Distance;
                    this.awaitClosest = p;
                    

				}
                
			}
		}

		private void AwaitOrdersPlayer(float elapsedTime)
		{
			this.HasPriorityOrder = false;
            if (this.Owner.InCombatTimer > elapsedTime * -5 && ScanForThreatTimer < 2 - elapsedTime * 5)
                this.ScanForThreatTimer = 0;
            if (this.EscortTarget != null)
                this.State = AIState.Escort;
            else
                if (!this.HadPO)
                {
                    if (this.SystemToDefend != null)
                    {
                        this.DoOrbit(this.SystemToDefend.PlanetList[0], elapsedTime);
                        this.awaitClosest = this.SystemToDefend.PlanetList[0];
                        return;
                    } 
                    if (this.awaitClosest != null)
                    {
                        this.DoOrbit(this.awaitClosest, elapsedTime);
                        return;
                    }
                    List<Planet> planets = new List<Planet>();
                    foreach (KeyValuePair<Guid, Planet> entry in ArtificialIntelligence.universeScreen.PlanetsDict)
                    {
                        planets.Add(entry.Value);
                    }
                    IOrderedEnumerable<Planet> sortedList =
                        from planet in planets
                        orderby  Vector2.Distance(planet.Position, this.Owner.Center) + (this.Owner.loyalty != planet.Owner ? 300000 : 0)
                        select planet;
                    this.awaitClosest = sortedList.First<Planet>();
                }
                else
                {
                    if(this.Owner.GetSystem() != null && this.Owner.GetSystem().OwnerList.Contains(this.Owner.loyalty))
                    {
                        this.HadPO = false;
                    return;
                    }
                    this.Stop(elapsedTime);
                }

		}

		private void Colonize(Planet TargetPlanet)
		{
			if (Vector2.Distance(this.Owner.Center, TargetPlanet.Position) > 2000f)
			{
				this.OrderQueue.RemoveFirst();
				this.OrderColonization(TargetPlanet);
				this.State = AIState.Colonize;
				return;
			}
			if (TargetPlanet.Owner != null || !TargetPlanet.habitable)
			{
				if (this.ColonizeGoal != null)
				{
					Goal colonizeGoal = this.ColonizeGoal;
					colonizeGoal.Step = colonizeGoal.Step + 1;
					this.Owner.loyalty.GetGSAI().Goals.QueuePendingRemoval(this.ColonizeGoal);
				}
				this.State = AIState.AwaitingOrders;
				this.OrderQueue.Clear();
				return;
			}
			this.ColonizeTarget = TargetPlanet;
			this.ColonizeTarget.Owner = this.Owner.loyalty;
			this.ColonizeTarget.system.OwnerList.Add(this.Owner.loyalty);
            if (!this.Owner.loyalty.AutoColonize && this.Owner.loyalty.isPlayer)
            {
                this.ColonizeTarget.colonyType = Planet.ColonyType.Colony;
                this.ColonizeTarget.GovernorOn = false;
            }
            else
                this.ColonizeTarget.colonyType = this.Owner.loyalty.AssessColonyNeeds(this.ColonizeTarget);
			if (this.Owner.loyalty.isPlayer )  //.data.CurrentAutoColony))
			{
				ArtificialIntelligence.universeScreen.NotificationManager.AddColonizedNotification(this.ColonizeTarget, EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty));
                
              
			}
			//lock (GlobalStats.OwnedPlanetsLock)
			{
				this.Owner.loyalty.AddPlanet(this.ColonizeTarget);
			}
			this.ColonizeTarget.InitializeSliders(this.Owner.loyalty);
			this.ColonizeTarget.ExploredDict[this.Owner.loyalty] = true;
			List<string> BuildingsAdded = new List<string>();
			foreach (ModuleSlot slot in this.Owner.ModuleSlotList)
			{
				if (slot.module == null || slot.module.ModuleType != ShipModuleType.Colony || slot.module.DeployBuildingOnColonize == null || BuildingsAdded.Contains(slot.module.DeployBuildingOnColonize))
				{
					continue;
				}
				Building building = ResourceManager.GetBuilding(slot.module.DeployBuildingOnColonize);
				bool ok = true;
				if (building.Unique)
				{
					foreach (Building b in this.ColonizeTarget.BuildingList)
					{
						if (b.Name != building.Name)
						{
							continue;
						}
						ok = false;
						break;
					}
				}
				if (!ok)
				{
					continue;
				}
				BuildingsAdded.Add(slot.module.DeployBuildingOnColonize);
				this.ColonizeTarget.BuildingList.Add(building);
				this.ColonizeTarget.AssignBuildingToTileOnColonize(building);
			}
			Planet colonizeTarget = this.ColonizeTarget;
			colonizeTarget.TerraformPoints = colonizeTarget.TerraformPoints + this.Owner.loyalty.data.EmpireFertilityBonus;
			this.ColonizeTarget.Crippled_Turns = 0;
			if (StatTracker.SnapshotsDict.ContainsKey(ArtificialIntelligence.universeScreen.StarDate.ToString("#.0")))
			{
				StatTracker.SnapshotsDict[ArtificialIntelligence.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this.Owner.loyalty)].Events.Add(string.Concat(this.Owner.loyalty.data.Traits.Name, " colonized ", this.ColonizeTarget.Name));
				NRO nro = new NRO()
				{
					Node = this.ColonizeTarget.Position,
					Radius = 300000f,
					StarDateMade = ArtificialIntelligence.universeScreen.StarDate
				};
				StatTracker.SnapshotsDict[ArtificialIntelligence.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this.Owner.loyalty)].EmpireNodes.Add(nro);
			}
			foreach (Goal g in this.Owner.loyalty.GetGSAI().Goals)
			{
				if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != this.ColonizeTarget)
				{
					continue;
				}
				this.Owner.loyalty.GetGSAI().Goals.QueuePendingRemoval(g);
				break;
			}
			this.Owner.loyalty.GetGSAI().Goals.ApplyPendingRemovals();
			if (this.ColonizeTarget.system.OwnerList.Count > 1)
			{
				foreach (Planet p in this.ColonizeTarget.system.PlanetList)
				{
					if (p.Owner == this.ColonizeTarget.Owner || p.Owner == null)
					{
						continue;
					}
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
					{
						if (p.Owner.GetRelations().ContainsKey(this.Owner.loyalty) && !p.Owner.GetRelations()[this.Owner.loyalty].Treaty_OpenBorders)
						{
							p.Owner.DamageRelationship(this.Owner.loyalty, "Colonized Owned System", 20f, p);
						}
					}
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
				}
			}
			foreach (ModuleSlot slot in this.Owner.ModuleSlotList)
			{
				if (slot.module.ModuleType != ShipModuleType.Colony)
				{
					continue;
				}
				Planet foodHere = this.ColonizeTarget;
				foodHere.FoodHere = foodHere.FoodHere + slot.module.numberOfFood;
				Planet productionHere = this.ColonizeTarget;
				productionHere.ProductionHere = productionHere.ProductionHere + slot.module.numberOfEquipment;
				Planet population = this.ColonizeTarget;
				population.Population = population.Population + slot.module.numberOfColonists;
			}
            //Added by McShooterz: Remove troops from colonized planet
            bool TroopsRemoved = false;
            bool PlayerTroopsRemoved = false;

			List<Troop> toLaunch = new List<Troop>();
            foreach (Troop t in TargetPlanet.TroopsHere)
			{
                if (t != null && t.GetOwner() != null && !t.GetOwner().isFaction && t.GetOwner().data.DefaultTroopShip != null && t.GetOwner() != this.ColonizeTarget.Owner && this.ColonizeTarget.Owner.GetRelations().ContainsKey(t.GetOwner()) && !this.ColonizeTarget.Owner.GetRelations()[t.GetOwner()].AtWar)
				    toLaunch.Add(t);
			}
			foreach (Troop t in toLaunch)
			{
                t.Launch();
                TroopsRemoved = true;
                if (t.GetOwner().isPlayer)
                    PlayerTroopsRemoved = true;
			}
			toLaunch.Clear();
            /*            
            for (int i = 0; i < this.ColonizeTarget.TroopsHere.Count; i++)
            {
                Troop troop = this.ColonizeTarget.TroopsHere[i];
                if (troop != null && troop.GetOwner() != null && !troop.GetOwner().isFaction && troop.GetOwner().data.DefaultTroopShip != null && troop.GetOwner() != this.ColonizeTarget.Owner && this.ColonizeTarget.Owner.GetRelations().ContainsKey(troop.GetOwner()) && !this.ColonizeTarget.Owner.GetRelations()[troop.GetOwner()].AtWar)
                {
                    troop.Launch();
                    TroopsRemoved = true;
                    if (troop.GetOwner().isPlayer)
                        PlayerTroopsRemoved = true;
                }
            }
            */
            if (TroopsRemoved)
            {
                if (PlayerTroopsRemoved)
                    universeScreen.NotificationManager.AddTroopsRemovedNotification(this.ColonizeTarget);
                else if (this.ColonizeTarget.Owner.isPlayer)
                    universeScreen.NotificationManager.AddForeignTroopsRemovedNotification(this.ColonizeTarget);
            }
			this.Owner.QueueTotalRemoval();
		}

		private void DeRotate()
		{
			if (this.Owner.yRotation > 0f)
			{
				Ship owner = this.Owner;
				owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
				if (this.Owner.yRotation < 0f)
				{
					this.Owner.yRotation = 0f;
					return;
				}
			}
			else if (this.Owner.yRotation < 0f)
			{
				Ship ship = this.Owner;
				ship.yRotation = ship.yRotation + this.Owner.yBankAmount;
				if (this.Owner.yRotation > 0f)
				{
					this.Owner.yRotation = 0f;
				}
			}
		}

        private void DoAssaultShipCombat(float elapsedTime)
        {
            this.DoNonFleetArtillery(elapsedTime);
            if ((!this.Owner.loyalty.isFaction && ((this.Target as Ship).shipData.Role < ShipData.RoleName.drone)) || this.Owner.GetHangars().Count == 0)
                return;
            float OurTroopStrength = 0f;
            float OurOutStrength = 0f;
            int tcount = 0;
            foreach (ShipModule s in this.Owner.GetHangars())
            {
                if (s.IsTroopBay)
                {
                    if (s.GetHangarShip() != null)
                    {
                        foreach (Troop st in s.GetHangarShip().TroopList)
                        {
                            OurTroopStrength += st.Strength;
                            if (s.GetHangarShip().GetAI().EscortTarget == this.Target || s.GetHangarShip().GetAI().EscortTarget == null
                                || s.GetHangarShip().GetAI().EscortTarget == this.Owner)
                                OurOutStrength += st.Strength;
                        }
                    }
                    if (s.hangarTimer <= 0)
                    {
                        tcount++;
                    }
                }

            }
            foreach (Troop t in this.Owner.TroopList)
            {
                if (tcount <= 0)
                    break;
                OurTroopStrength = OurTroopStrength + (float)t.Strength;
                tcount--;

            }

            if (OurTroopStrength <= 0)
                return;

            if (this.Target == null)
            {
                if (!this.Owner.InCombat && this.Owner.GetSystem() != null && this.Owner.GetSystem().OwnerList.Count > 0)
                {
                    this.Owner.ScrambleAssaultShips(0);
                    foreach (Planet p in this.Owner.GetSystem().PlanetList)
                    {
                        if (p.Owner != null)
                        {
                            this.OrderAssaultPlanet(p);
                            break;
                        }
                    }

                }
            }

            float EnemyStrength = 0f;
            if (this.Target != null && this.Target is Ship)
            {
                EnemyStrength = (this.Target as Ship).MechanicalBoardingDefense + (this.Target as Ship).TroopBoardingDefense;
            }



            if ((OurTroopStrength + OurOutStrength) > EnemyStrength && (this.Owner.loyalty.isFaction || (this.Target as Ship).GetStrength() > 0f))
            {
                if (OurOutStrength < EnemyStrength)
                {
                    this.Owner.ScrambleAssaultShips(EnemyStrength);
                }
                foreach (ShipModule hangar in this.Owner.GetHangars())
                {
                    if (!hangar.IsTroopBay || hangar.GetHangarShip() == null) //|| !(hangar.GetHangarShip().shipData.Role == ShipData.RoleName.troop)
                    {
                        continue;
                    }
                    hangar.GetHangarShip().GetAI().OrderTroopToBoardShip(this.Target as Ship);
                }

            }

        }

		private void DoAttackRunOrig(float elapsedTime)
		{
			float distanceToTarget = Vector2.Distance(this.Owner.Center, this.Target.Center);
			if (distanceToTarget > this.Owner.Radius * 3f + this.Target.Radius && distanceToTarget > this.Owner.maxWeaponsRange / 2f)
			{
				this.runTimer = 0f;
				this.AttackRunStarted = false;
				this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
				return;
			}
			if (distanceToTarget < this.Owner.maxWeaponsRange)
			{
				ArtificialIntelligence artificialIntelligence = this;
				artificialIntelligence.runTimer = artificialIntelligence.runTimer + elapsedTime;
				if (this.runTimer >= 7f)
				{
					this.DoNonFleetArtillery(elapsedTime);
					return;
				}
				Vector2 projectedPosition = this.Target.Center;
				ArtificialIntelligence target = this;
				target.aiNewDir = target.aiNewDir + (this.findVectorToTarget(this.Owner.Center, projectedPosition) * 0.35f);
				if (distanceToTarget < (this.Owner.Radius + this.Target.Radius) * 3f && !this.AttackRunStarted)
				{
					this.AttackRunStarted = true;
					int ran = (int)((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(1f, 100f);
					ran = (ran <= 50 ? 1 : -1);
					this.AttackRunAngle = (float)ran * ((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(75f, 100f) + MathHelper.ToDegrees(this.Owner.Rotation);
					this.AttackVector = this.findPointFromAngleAndDistance(this.Owner.Center, this.AttackRunAngle, 1500f);
				}
				this.AttackVector = this.findPointFromAngleAndDistance(this.Owner.Center, this.AttackRunAngle, 1500f);
				this.MoveInDirection(this.AttackVector, elapsedTime);
			}
		}
        //aded by gremlin Deveksmod Attackrun
        private void DoAttackRun(float elapsedTime)
        {

            float distanceToTarget = Vector2.Distance(this.Owner.Center, this.Target.Center);
            float spacerdistance = this.Owner.Radius * 3 + this.Target.Radius;
            if (spacerdistance > this.Owner.maxWeaponsRange * .35f)
                spacerdistance = this.Owner.maxWeaponsRange * .35f;


            if (distanceToTarget > spacerdistance && distanceToTarget > this.Owner.maxWeaponsRange * .35f)
            {
                this.runTimer = 0f;
                this.AttackRunStarted = false;
                this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
                return;
            }


            if (distanceToTarget < this.Owner.maxWeaponsRange * .35f)// *.35f)
            {
                ArtificialIntelligence artificialIntelligence = this;
                artificialIntelligence.runTimer = artificialIntelligence.runTimer + elapsedTime;
                if ((double)this.runTimer > 7f) //this.Owner.Weapons.Average(delay => delay.timeToNextFire)) //7 * (this.Owner.maxWeaponsRange + 1) / (this.Owner.GetSTLSpeed()+ 1))
                {
                    this.DoNonFleetArtillery(elapsedTime);
                    return;

                }
                //if (!AttackRunStarted )
                //{
                //    this.Stop(elapsedTime);
                //    //return;
                //}
                Vector2 projectedPosition = this.Target.Center + this.Target.Velocity;
                ArtificialIntelligence target = this;
                target.aiNewDir = target.aiNewDir + (this.findVectorToTarget(this.Owner.Center, projectedPosition) * 0.35f);
                if (distanceToTarget < (this.Owner.Radius + this.Target.Radius) * 3f && !this.AttackRunStarted)
                {
                    this.AttackRunStarted = true;
                    int ran = (int)((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(1f, 100f);
                    ran = (ran <= 50 ? 1 : -1);
                    this.AttackRunAngle = (float)ran * ((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(75f, 100f) + MathHelper.ToDegrees(this.Owner.Rotation);
                    this.AttackVector = this.findPointFromAngleAndDistance(this.Owner.Center, this.AttackRunAngle, 1500f);
                }
                this.AttackVector = this.findPointFromAngleAndDistance(this.Owner.Center, this.AttackRunAngle, 1500f);
                this.MoveInDirection(this.AttackVector, elapsedTime);
                if (this.runTimer > 2)
                {
                    this.DoNonFleetArtillery(elapsedTime);
                    return;
                }

            }
            //else
            //{
            //    this.DoNonFleetArtillery(elapsedTime);
            //}


        }
		private void DoBoardShip(float elapsedTime)
		{
			this.hasPriorityTarget = true;
			this.State = AIState.Boarding;
			if (this.EscortTarget == null || !this.EscortTarget.Active)
			{
				this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
				return;
			}
			if (this.EscortTarget.loyalty == this.Owner.loyalty)
			{
				//this.OrderReturnToHangar();
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
				return;
			}
			this.ThrustTowardsPosition(this.EscortTarget.Center, elapsedTime, this.Owner.speed);
			float Distance = Vector2.Distance(this.Owner.Center, this.EscortTarget.Center);
			//added by gremlin distance at which troops can board enemy ships
            if (Distance < this.EscortTarget.Radius + 300f)
			{
				if (this.Owner.TroopList.Count > 0)
				{
					this.EscortTarget.TroopList.Add(this.Owner.TroopList[0]);
					this.Owner.QueueTotalRemoval();
					return;
				}
			}
			else if (Distance > 10000f && this.Owner.Mothership != null && this.Owner.Mothership.GetAI().CombatState == Ship_Game.Gameplay.CombatState.AssaultShip)
			{
				this.OrderReturnToHangar();
			}
		}

        private void DoCombat(float elapsedTime)
        {


            Ship ctarget = this.Target as Ship;
            if (this.Target != null && (!this.Target.Active || ctarget.engineState == Ship.MoveState.Warp))
            {
                this.Intercepting = false;
                this.Target = null;
                this.Target = this.PotentialTargets.Where(t => t.Active && t.engineState != Ship.MoveState.Warp && Vector2.Distance(t.Center, this.Owner.Center) <= this.Owner.SensorRange).FirstOrDefault() as GameplayObject;
                if (Target ==null )
                {
                    
                    this.ClearOrdersNext = true;
                    this.HadPO = true;
                    //this.AwaitOrders(elapsedTime);
                    //this.State = this.DefaultAIState;
                    //this.OrderQueue.Clear();
                    return;                
                }
                
            }
            //Ship ship = null;
            if (this.Target == null) 
            {
                this.Target = this.PotentialTargets.Where(t => t.Active && t.engineState != Ship.MoveState.Warp && Vector2.Distance(t.Center, this.Owner.Center) <= this.Owner.SensorRange).FirstOrDefault() as GameplayObject;
                this.Intercepting = false;
                if (this.Target == null)
                {
                    
                    this.ClearOrdersNext = true;
                    this.HadPO = true;
                    //this.OrderQueue.Clear();
                    //this.State = this.DefaultAIState;
                    return;
                }
                if(!this.Target.Active)
                {
                    
                    this.ClearOrdersNext = true;
                    this.HadPO = true;
                    //this.OrderQueue.Clear();
                    //this.State = this.DefaultAIState;
                    return; 
                }
                
                
                //this.ScanForThreatTimer = 0;
                //return;               
            }
            this.awaitClosest = null;
            this.State = AIState.Combat;
            this.Owner.InCombat = true;
            this.Owner.InCombatTimer = 15f;
            if (this.Owner.Mothership != null && this.Owner.Mothership.Active)
            {
                //if (!this.hasPriorityTarget
                //    && !this.HasPriorityOrder&& this.Target != null 
                //    && this.Owner.Mothership.GetAI().Target == null 
                //    && !this.Owner.Mothership.GetAI().HasPriorityOrder 
                //    && !this.Owner.Mothership.GetAI().hasPriorityTarget)
                //{
                //    this.Owner.Mothership.GetAI().Target = this.Target;
                //    this.Owner.Mothership.GetAI().State = AIState.Combat;
                //    this.Owner.Mothership.InCombatTimer = 15f;
                //}
                if (this.Owner.shipData.Role != ShipData.RoleName.troop
                    && (this.Owner.Health / this.Owner.HealthMax < DmgLevel[(int)this.Owner.shipData.ShipCategory] || (this.Owner.shield_max > 0 && this.Owner.shield_percent <= 0))
                    || (this.Owner.OrdinanceMax > 0 && this.Owner.Ordinance / this.Owner.OrdinanceMax <= .1f)
                    || (this.Owner.PowerCurrent <=1f && this.Owner.PowerDraw / this.Owner.PowerFlowMax <=.1f)
                    )
                    this.OrderReturnToHangar();
            }
            if (this.State!= AIState.Resupply && this.Owner.OrdinanceMax > 0f && this.Owner.Ordinance / this.Owner.OrdinanceMax < 0.05f &&  !this.hasPriorityTarget)//this.Owner.loyalty != ArtificialIntelligence.universeScreen.player)
            {
                if (FriendliesNearby.Where(supply => supply.HasSupplyBays && supply.Ordinance >= 100).Count() == 0)
                {
                    this.OrderResupplyNearest(false);
                    return;
                }
            }
            if(this.State != AIState.Resupply && !this.Owner.loyalty.isFaction && State == AIState.AwaitingOrders && this.Owner.TroopCapacity >0 && this.Owner.TroopList.Count < this.Owner.GetHangars().Where(hangar=> hangar.IsTroopBay).Count() *.5f)
            {
                this.OrderResupplyNearest(false);
                return;
            }
            //if(this.Owner.Level >2 && this.Owner.Health / this.Owner.HealthMax <.5f&&  !(this.HasPriorityOrder||this.hasPriorityTarget))
            if (this.State != AIState.Resupply && this.Owner.Health >0 && this.Owner.Health / this.Owner.HealthMax < DmgLevel[(int)this.Owner.shipData.ShipCategory] 
                && this.Owner.shipData.Role >= ShipData.RoleName.supply)  //fbedard: repair level
                if (this.Owner.fleet == null ||  !this.Owner.fleet.HasRepair)
                {
                    this.OrderResupplyNearest(false);
                    return;
                }
            if (Vector2.Distance(this.Target.Center, this.Owner.Center) < 10000f)
            {
                if (this.Owner.engineState != Ship.MoveState.Warp && this.Owner.GetHangars().Count > 0 && !this.Owner.ManualHangarOverride)
                {
                    if (!this.Owner.FightersOut) this.Owner.FightersOut = true;
                }
                if (this.Owner.engineState == Ship.MoveState.Warp)
                {
                    this.Owner.HyperspaceReturn();
                }

            }
            else if (this.CombatState != CombatState.HoldPosition && this.CombatState != CombatState.Evade)
            {
                this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
                return;
            }
            if (!this.HasPriorityOrder && !this.hasPriorityTarget && this.Owner.Weapons.Count == 0 && this.Owner.GetHangars().Count == 0)
            {
                this.CombatState = CombatState.Evade;
            } //
            if (!this.Owner.loyalty.isFaction && this.Owner.GetSystem() != null && this.TroopsOut == false && this.Owner.GetHangars().Where(troops => troops.IsTroopBay).Count() > 0 || this.Owner.hasTransporter)
            {
                if (this.Owner.TroopList.Where(troop => troop.GetOwner() == this.Owner.loyalty).Count() > 0 && this.Owner.TroopList.Where(troop => troop.GetOwner() != this.Owner.loyalty).Count() == 0)
                {
                    Planet invadeThis = null;
                    foreach (Planet invade in this.Owner.GetSystem().PlanetList.Where(owner => owner.Owner != null && owner.Owner != this.Owner.loyalty).OrderBy(troops => troops.TroopsHere.Count))
                    {
                        if (this.Owner.loyalty.GetRelations()[invade.Owner].AtWar)
                        {
                            invadeThis = invade;
                            break;
                        }
                    }
                    if (!this.TroopsOut && !this.Owner.hasTransporter)
                    {
                        if (invadeThis != null)
                        {
                            this.TroopsOut = true;
                            foreach (Ship troop in this.Owner.GetHangars().Where(troop => troop.IsTroopBay && troop.GetHangarShip() != null && troop.GetHangarShip().Active).Select(ship => ship.GetHangarShip()))
                            {
                                troop.GetAI().OrderAssaultPlanet(invadeThis);
                            }
                        }
                        else if (this.Target != null && this.Target is Ship && (this.Target as Ship).shipData.Role >= ShipData.RoleName.drone)
                        {
                            if (this.Owner.GetHangars().Where(troop => troop.IsTroopBay).Count() * 60 >= (this.Target as Ship).MechanicalBoardingDefense)
                            {
                                this.TroopsOut = true;
                                foreach (ShipModule hangar in this.Owner.GetHangars())
                                {
                                    if (hangar.GetHangarShip() == null || this.Target == null || !(hangar.GetHangarShip().shipData.Role == ShipData.RoleName.troop) || ((this.Target as Ship).shipData.Role < ShipData.RoleName.drone))
                                    {
                                        continue;
                                    }
                                    hangar.GetHangarShip().GetAI().OrderTroopToBoardShip(this.Target as Ship);
                                }
                            }
                        }
                        else
                        {
                            this.TroopsOut = false;
                        }
                    }

                }
            }
            
            { 
            if (this.Owner.fleet == null)
            {
                switch (this.CombatState)
                {
                    case CombatState.Artillery:
                        {
                            this.DoNonFleetArtillery(elapsedTime);
                            break;
                        }
                    case CombatState.OrbitLeft:
                        {
                            this.OrbitShipLeft(this.Target as Ship, elapsedTime);
                            break;
                        }
                    case CombatState.BroadsideLeft:
                        {
                            this.DoNonFleetBroadsideLeft(elapsedTime);
                            break;
                        }
                    case CombatState.OrbitRight:
                        {
                            this.OrbitShip(this.Target as Ship, elapsedTime);
                            break;
                        }
                    case CombatState.BroadsideRight:
                        {
                            this.DoNonFleetBroadsideRight(elapsedTime);
                            break;
                        }
                    case CombatState.AttackRuns:
                        {
                            this.DoAttackRun(elapsedTime);
                            break;
                        }
                    case CombatState.HoldPosition:
                        {
                            this.DoHoldPositionCombat(elapsedTime);
                            break;
                        }
                    case CombatState.Evade:
                        {
                            this.DoEvadeCombat(elapsedTime);
                            break;
                        }
                    case CombatState.AssaultShip:
                        {
                            this.DoAssaultShipCombat(elapsedTime);
                            break;
                        }
                    case CombatState.ShortRange:
                        {
                            this.DoNonFleetArtillery(elapsedTime);
                            break;
                        }
                }
            }
            else if (this.Owner.fleet != null)
            {
                switch (this.CombatState)
                {
                    case CombatState.Artillery:
                        {
                            this.DoNonFleetArtillery(elapsedTime);
                            break;
                        }
                    case CombatState.OrbitLeft:
                        {
                            this.OrbitShipLeft(this.Target as Ship, elapsedTime);
                            break;
                        }
                    case CombatState.BroadsideLeft:
                        {
                            this.DoNonFleetBroadsideLeft(elapsedTime);
                            break;
                        }
                    case CombatState.OrbitRight:
                        {
                            this.OrbitShip(this.Target as Ship, elapsedTime);
                            break;
                        }
                    case CombatState.BroadsideRight:
                        {
                            this.DoNonFleetBroadsideRight(elapsedTime);
                            break;
                        }
                    case CombatState.AttackRuns:
                        {
                            this.DoAttackRun(elapsedTime);
                            break;
                        }
                    case CombatState.HoldPosition:
                        {
                            this.DoHoldPositionCombat(elapsedTime);
                            break;
                        }
                    case CombatState.Evade:
                        {
                            this.DoEvadeCombat(elapsedTime);
                            break;
                        }
                    case CombatState.AssaultShip:
                        {
                            this.DoAssaultShipCombat(elapsedTime);
                            break;
                        }
                    case CombatState.ShortRange:
                        {
                            this.DoNonFleetArtillery(elapsedTime);
                            break;
                        }
                }
            }
                if (this.Target != null)
                    return;
                this.Owner.InCombat = false;
            }
        }

        //added by gremlin : troops out property        
        public bool TroopsOut
        {
            get
            {
                //this.troopsout = false;
                if (this.Owner.TroopsOut)
                {
                    this.troopsout = true;
                    return true;
                }

                if (this.Owner.TroopList.Count == 0)
                {
                    this.troopsout = true;
                    return true;
                }
                if (this.Owner.GetHangars().Where(troopbay => troopbay.IsTroopBay).Count() == 0)
                {
                    this.troopsout = true;
                    return true;
                }
                if (this.Owner.TroopList.Where(loyalty => loyalty.GetOwner() != this.Owner.loyalty).Count() > 0)
                {
                    this.troopsout = true;
                    return true;
                }

                if (this.troopsout == true)
                {
                    foreach (ShipModule hangar in this.Owner.GetHangars())
                    {
                        if (hangar.IsTroopBay && (hangar.GetHangarShip() == null || hangar.GetHangarShip() != null && !hangar.GetHangarShip().Active) && hangar.hangarTimer <= 0)
                        {
                            this.troopsout = false;
                            break;

                        }

                    }
                }
                return this.troopsout;
            }
            set
            {
                this.troopsout = value;
                if (this.troopsout)
                {
                    this.Owner.ScrambleAssaultShips(0);
                    return;
                }
                this.Owner.RecoverAssaultShips();
            }
        }

        //added by gremlin : troop asssault planet
        public void OrderAssaultPlanet(Planet p)
        {
            this.State = AIState.AssaultPlanet;
            this.OrbitTarget = p;
            ArtificialIntelligence.ShipGoal shipGoal = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.LandTroop, Vector2.Zero, 0f)
            {
                TargetPlanet = OrbitTarget
            };
           
            this.OrderQueue.Clear();
            this.OrderQueue.AddLast(shipGoal);
            
        }
        public void OrderAssaultPlanetorig(Planet p)
        {
            this.State = AIState.AssaultPlanet;
            this.OrbitTarget = p;
        }

		private void DoDeploy(ArtificialIntelligence.ShipGoal shipgoal)
		{
			if (shipgoal.goal == null)
			{
				return;
			}
            Planet target = shipgoal.TargetPlanet;
            if (shipgoal.goal.TetherTarget != Guid.Empty)
            {
                if (target == null)
                    ArtificialIntelligence.universeScreen.PlanetsDict.TryGetValue(shipgoal.goal.TetherTarget, out target);
                shipgoal.goal.BuildPosition = target.Position + shipgoal.goal.TetherOffset;                
            }
            if (target !=null && Vector2.Distance(target.Position + shipgoal.goal.TetherOffset, this.Owner.Center) > 200f)
			{				
                shipgoal.goal.BuildPosition = target.Position + shipgoal.goal.TetherOffset;
				this.OrderDeepSpaceBuild(shipgoal.goal);
				return;
			}
			Ship platform = ResourceManager.CreateShipAtPoint(shipgoal.goal.ToBuildUID, this.Owner.loyalty, shipgoal.goal.BuildPosition);
			if (platform == null)
			{
				return;
			}
			foreach (SpaceRoad road in this.Owner.loyalty.SpaceRoadsList)
			{
				foreach (RoadNode node in road.RoadNodesList)
				{
					if (node.Position != shipgoal.goal.BuildPosition)
					{
						continue;
					}
					node.Platform = platform;
					if (!StatTracker.SnapshotsDict.ContainsKey(ArtificialIntelligence.universeScreen.StarDate.ToString("#.0")))
					{
						continue;
					}
					NRO nro = new NRO()
					{
						Node = node.Position,
						Radius = 300000f,
						StarDateMade = ArtificialIntelligence.universeScreen.StarDate
					};
					StatTracker.SnapshotsDict[ArtificialIntelligence.universeScreen.StarDate.ToString("#.0")][EmpireManager.EmpireList.IndexOf(this.Owner.loyalty)].EmpireNodes.Add(nro);
				}
			}
			if (shipgoal.goal.TetherTarget != Guid.Empty)
			{
				platform.TetherToPlanet(ArtificialIntelligence.universeScreen.PlanetsDict[shipgoal.goal.TetherTarget]);
				platform.TetherOffset = shipgoal.goal.TetherOffset;
			}
			this.Owner.loyalty.GetGSAI().Goals.Remove(shipgoal.goal);
			this.Owner.QueueTotalRemoval();
		}

		private void DoEvadeCombat(float elapsedTime)
		{

            Vector2 AverageDirection = new Vector2();
            int count = 0;
            foreach (ArtificialIntelligence.ShipWeight ship in this.NearbyShips)
            {
                if (ship.ship.loyalty == this.Owner.loyalty || !ship.ship.loyalty.isFaction && !this.Owner.loyalty.GetRelations()[ship.ship.loyalty].AtWar)
                {
                    continue;
                }
                AverageDirection = AverageDirection + this.findVectorToTarget(this.Owner.Center, ship.ship.Center);
                count++;
            }
            if (count != 0)
            {
                AverageDirection = AverageDirection / (float)count;
                AverageDirection = Vector2.Normalize(AverageDirection);
                AverageDirection = Vector2.Negate(AverageDirection);
                
                
                {
                    AverageDirection = AverageDirection * 7500f;
                    this.ThrustTowardsPosition(AverageDirection + this.Owner.Center, elapsedTime, this.Owner.speed);
                }
            }
		}

		public void DoExplore(float elapsedTime)
		{
			this.HasPriorityOrder = true;
			this.IgnoreCombat = true;
			if (this.ExplorationTarget == null)
			{
				this.ExplorationTarget = this.Owner.loyalty.GetGSAI().AssignExplorationTarget(this.Owner);
				if (this.ExplorationTarget == null)
				{
					this.OrderQueue.Clear();
					this.State = AIState.AwaitingOrders;
					return;
				}
			}
			else if (this.DoExploreSystem(elapsedTime))
			{
                if (this.Owner.loyalty == ArtificialIntelligence.universeScreen.player)
                {
                    //added by gremlin  add shamatts notification here
                    string planetsInfo = "";
                    Dictionary<string, int> planetsTypesNumber = new Dictionary<string, int>();
                    SolarSystem system = this.ExplorationTarget;
                    if (system.PlanetList.Count > 0)
                    {
                        foreach (Planet planet in system.PlanetList)
                        {
                            // some planets don't have Type set and it is null
                            if (planet.Type == null)
                            {
                                planet.Type = "Other";
                            }

                            if (!planetsTypesNumber.ContainsKey(planet.Type))
                            {
                                planetsTypesNumber.Add(planet.Type, 1);
                            }
                            else
                            {
                                planetsTypesNumber[planet.Type] += 1;
                            }
                        }

                        foreach (KeyValuePair<string, int> pair in planetsTypesNumber)
                        {
                            planetsInfo = planetsInfo + "\n" + pair.Value + " " + pair.Key;
                        }
                    }

                    Notification cNote = new Notification()
                    {
                        Pause = false,
                        //RelevantEmpire = this.Owner.loyalty,
                        Message = string.Concat(system.Name, " system explored."),
                        ReferencedItem1 = system,
                        //IconPath = "NewUI/icon_planet_terran_01_mid",
                        IconPath = string.Concat("Suns/", system.SunPath),
                        Action = "SnapToExpandSystem",
                        ClickRect = new Rectangle(Planet.universeScreen.NotificationManager.NotificationArea.X, Planet.universeScreen.NotificationManager.NotificationArea.Y, 64, 64),
                        DestinationRect = new Rectangle(Planet.universeScreen.NotificationManager.NotificationArea.X, Planet.universeScreen.NotificationManager.NotificationArea.Y + Planet.universeScreen.NotificationManager.NotificationArea.Height - (Planet.universeScreen.NotificationManager.NotificationList.Count + 1) * 70, 64, 64)

                    };
                    cNote.Message = cNote.Message + planetsInfo;
                    if (system.combatTimer > 0)
                    {
                        cNote.Message += "\nCombat in system!!!";
                    }
                    if (system.OwnerList.Count > 0 && !system.OwnerList.Contains(this.Owner.loyalty))
                    {
                        cNote.Message += "\nContested system!!!";
                    }

                    foreach (Planet stuff in system.PlanetList)
                    {

                        foreach (Building tile in stuff.BuildingList)
                        {
                            if (tile.IsCommodity)
                            {

                                cNote.Message += "\n" + tile.Name + " on " + stuff.Name;
                                break;
                            }

                        }

                    }

                    AudioManager.PlayCue("sd_ui_notification_warning");
                    lock (GlobalStats.NotificationLocker)
                    {
                        Planet.universeScreen.NotificationManager.NotificationList.Add(cNote);
                    }
                }
                this.ExplorationTarget = null;
                            
			}
		}

		private bool DoExploreSystem(float elapsedTime)
		{
			this.SystemToPatrol = this.ExplorationTarget;
			if (this.PatrolRoute == null || this.PatrolRoute.Count == 0)
			{
				foreach (Planet p in this.SystemToPatrol.PlanetList)
				{
					this.PatrolRoute.Add(p);
				}
				if (this.SystemToPatrol.PlanetList.Count == 0)
				{
					return this.ExploreEmptySystem(elapsedTime, this.SystemToPatrol);
                    
                        

				}
			}
			else
			{
				this.PatrolTarget = this.PatrolRoute[this.stopNumber];
				if (this.PatrolTarget.ExploredDict[this.Owner.loyalty])
				{
					ArtificialIntelligence artificialIntelligence = this;
					artificialIntelligence.stopNumber = artificialIntelligence.stopNumber + 1;
					if (this.stopNumber == this.PatrolRoute.Count)
					{
						this.stopNumber = 0;
						this.PatrolRoute.Clear();
                       
						return true;
					}
				}
				else
				{
					this.MovePosition = this.PatrolTarget.Position;
					float Distance = Vector2.Distance(this.Owner.Center, this.MovePosition);
					if (Distance < 75000f)
					{
						this.PatrolTarget.system.ExploredDict[this.Owner.loyalty] = true;
					}
					if (Distance > 15000f)
					{
                        if (this.Owner.velocityMaximum > Distance && this.Owner.speed >= this.Owner.velocityMaximum)
                            this.Owner.speed = Distance;
                        this.ThrustTowardsPosition(this.MovePosition, elapsedTime, this.Owner.speed);
					}
					else if (Distance >= 5500f)
					{
                        if (this.Owner.velocityMaximum > Distance && this.Owner.speed >= this.Owner.velocityMaximum)
                            this.Owner.speed = Distance;
                        this.ThrustTowardsPosition(this.MovePosition, elapsedTime, this.Owner.speed);
					}
					else
					{
						this.ThrustTowardsPosition(this.MovePosition, elapsedTime, this.Owner.speed);
						if (Distance < 500f)
						{
							this.PatrolTarget.ExploredDict[this.Owner.loyalty] = true;
							ArtificialIntelligence artificialIntelligence1 = this;
							artificialIntelligence1.stopNumber = artificialIntelligence1.stopNumber + 1;
							if (this.stopNumber == this.PatrolRoute.Count)
							{
								this.stopNumber = 0;
								this.PatrolRoute.Clear();
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private void DoFleetArtillery(float elapsedTime)
		{
			this.findVectorToTarget(this.Owner.Center, this.Target.Center);
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, this.Target.Center);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
			if (Vector2.Distance(this.Owner.Center, this.Owner.fleet.Position + this.Owner.FleetOffset) > 2500f)
			{
				this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
				return;
			}
			if (angleDiff <= 0.02f)
			{
				this.DeRotate();
				return;
			}
			this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f));
		}

		private void DoHoldPositionCombat(float elapsedTime)
		{
			if (this.Owner.Velocity.Length() > 0f)
			{
                if (this.Owner.engineState == Ship.MoveState.Warp)
                    this.Owner.HyperspaceReturn();
                Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(Vector2.Normalize(this.Owner.Velocity), forward));
				float facing = (Vector2.Dot(Vector2.Normalize(this.Owner.Velocity), right) > 0f ? 1f : -1f);
				if (angleDiff <= 0.2f)
				{
					this.Stop(elapsedTime);
					return;
				}
				this.RotateToFacing(elapsedTime, angleDiff, facing);
				return;
			}
			this.findVectorToTarget(this.Owner.Center, this.Target.Center);
            //renamed forward, right and anglediff
			Vector2 forward2 = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right2 = new Vector2(-forward2.Y, forward2.X);
			Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, this.Target.Center);
			float angleDiff2 = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward2));
			if (angleDiff2 <= 0.02f)
			{
				this.DeRotate();
				return;
			}
			this.RotateToFacing(elapsedTime, angleDiff2, (Vector2.Dot(VectorToTarget, right2) > 0f ? 1f : -1f));
		}

        
		private void DoLandTroop(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
            if (this.Owner.shipData.Role != ShipData.RoleName.troop || this.Owner.TroopList.Count == 0)
                this.DoOrbit(goal.TargetPlanet, elapsedTime); //added by gremlin.

            float radius = goal.TargetPlanet.ObjectRadius + this.Owner.Radius * 2;
            float distCenter = Vector2.Distance(goal.TargetPlanet.Position, this.Owner.Center);

            if (this.Owner.shipData.Role == ShipData.RoleName.troop && this.Owner.TroopList.Count > 0)
			{
                if (this.Owner.engineState == Ship.MoveState.Warp && distCenter < 7500f)
                    this.Owner.HyperspaceReturn();
                if (distCenter < radius  )
                    this.ThrustTowardsPosition(goal.TargetPlanet.Position, elapsedTime, this.Owner.speed > 200 ? this.Owner.speed*.90f : this.Owner.velocityMaximum);
                else
                    this.ThrustTowardsPosition(goal.TargetPlanet.Position, elapsedTime, this.Owner.speed);
                if (distCenter < goal.TargetPlanet.ObjectRadius && goal.TargetPlanet.AssignTroopToTile(this.Owner.TroopList[0]))
                        this.Owner.QueueTotalRemoval();
                return;
			}
            else if (this.Owner.loyalty == goal.TargetPlanet.Owner || goal.TargetPlanet.GetGroundLandingSpots() == 0 || this.Owner.TroopList.Count <= 0 || (this.Owner.shipData.Role != ShipData.RoleName.troop && (this.Owner.GetHangars().Where(hangar => hangar.hangarTimer <= 0 && hangar.IsTroopBay).Count() == 0 && !this.Owner.hasTransporter)))//|| goal.TargetPlanet.GetGroundStrength(this.Owner.loyalty)+3 > goal.TargetPlanet.GetGroundStrength(goal.TargetPlanet.Owner)*1.5)
			{                
				if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
				{
					this.HadPO = true;
				}
				this.HasPriorityOrder = false;
                this.State = this.DefaultAIState;
				this.OrderQueue.Clear();
                System.Diagnostics.Debug.WriteLine("Do Land Troop: Troop Assault Canceled");
			}
            else if (distCenter < radius)
			{
				List<Troop> ToRemove = new List<Troop>();
                //if (Vector2.Distance(goal.TargetPlanet.Position, this.Owner.Center) < 3500f)
				{
                    //Get limit of troops to land
                    int LandLimit = this.Owner.GetHangars().Where(hangar => hangar.hangarTimer <= 0 && hangar.IsTroopBay).Count();
                    foreach (ShipModule module in this.Owner.Transporters.Where(module => module.TransporterTimer <= 1f))
                        LandLimit += module.TransporterTroopLanding;
                    //Land troops
                    foreach (Troop troop in this.Owner.TroopList)
                    {
                        if (troop == null || troop.GetOwner() != this.Owner.loyalty)
                            continue;
                        if (goal.TargetPlanet.AssignTroopToTile(troop))
                        {
                            ToRemove.Add(troop);
                            LandLimit--;
                            if (LandLimit < 1)
                                break;
                        }
                        else
                            break;
                    }
                    //Clear out Troops
                    if (ToRemove.Count > 0)
                    {
                        bool flag; // = false;
                        foreach (Troop RemoveTroop in ToRemove)
                        {
                            flag = false;
                            foreach (ShipModule module in this.Owner.GetHangars())
                            {
                                if (module.hangarTimer < module.hangarTimerConstant)
                                {
                                    module.hangarTimer = module.hangarTimerConstant;
                                    flag = true;
                                    break;
                                }
                            }
                            if (flag)
                                continue;
                            foreach (ShipModule module in this.Owner.Transporters)
                                if (module.TransporterTimer < module.TransporterTimerConstant)
                                {
                                    module.TransporterTimer = module.TransporterTimerConstant;
                                    flag = true;
                                    break;
                                }
                        }
                                //module.TransporterTimer = module.TransporterTimerConstant;
                            foreach (Troop to in ToRemove)
                                this.Owner.TroopList.Remove(to);
                        
                    }
				}
			}
		}

        private void DoNonFleetArtillery(float elapsedTime)
        {
            //Heavily modified by Gretman
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
            Vector2 right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, this.Target.Center);
            float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
            float DistanceToTarget = Vector2.Distance(this.Owner.Center, this.Target.Center) *.75f;

            float AdjustedRange = (this.Owner.maxWeaponsRange - this.Owner.Radius);

            if (DistanceToTarget > AdjustedRange) 
            {
                this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
                return;
            }
            else if (DistanceToTarget < AdjustedRange //* 0.75f 
                && Vector2.Distance(this.Owner.Center + (this.Owner.Velocity * elapsedTime), this.Target.Center) < DistanceToTarget 
                || DistanceToTarget < (this.Owner.Radius)) //Center + Radius = Dont touch me
            {
                this.Owner.Velocity = this.Owner.Velocity + (Vector2.Normalize(-forward) * (elapsedTime * this.Owner.GetSTLSpeed()));
                //if(this.Owner.Velocity.Length() > this.Owner.velocityMaximum)
                //    this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum; ;
                    
            }

            if (angleDiff <= 0.02f)
            {
                this.DeRotate();
                return;
            }
            this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f));
        }

        private void DoNonFleetBroadsideRight(float elapsedTime)
        {
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
            Vector2 right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, this.Target.Center);
            float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, right));
            float DistanceToTarget = Vector2.Distance(this.Owner.Center, this.Target.Center);
            if (DistanceToTarget > this.Owner.maxWeaponsRange)
            {
                this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
                return;
            }
            if (DistanceToTarget < this.Owner.maxWeaponsRange * 0.70f && Vector2.Distance(this.Owner.Center + (this.Owner.Velocity * elapsedTime), this.Target.Center) < DistanceToTarget)
            {
                Ship owner = this.Owner;
                this.Owner.Velocity = Vector2.Zero;
                //owner.Velocity = owner.Velocity + (Vector2.Normalize(-left) * (elapsedTime * this.Owner.velocityMaximum));
            }
            if (angleDiff <= 0.02f)
            {
                this.DeRotate();
                return;
            }
            this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, forward) > 0f ? -1f : 1f));
        }

        private void DoNonFleetBroadsideLeft(float elapsedTime)
        {
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
            Vector2 right = new Vector2(-forward.Y, forward.X);
            Vector2 left = new Vector2(forward.Y, -forward.X);
            Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, this.Target.Center);
            float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, left));
            float DistanceToTarget = Vector2.Distance(this.Owner.Center, this.Target.Center);
            if (DistanceToTarget > this.Owner.maxWeaponsRange)
            {
                this.ThrustTowardsPosition(this.Target.Center, elapsedTime, this.Owner.speed);
                return;
            }
            if (DistanceToTarget < this.Owner.maxWeaponsRange * 0.70f && Vector2.Distance(this.Owner.Center + (this.Owner.Velocity * elapsedTime), this.Target.Center) < DistanceToTarget)
            {
                Ship owner = this.Owner;
                this.Owner.Velocity = Vector2.Zero;
                //owner.Velocity = owner.Velocity + (Vector2.Normalize(-left) * (elapsedTime * this.Owner.velocityMaximum));
            }
            if (angleDiff <= 0.02f)
            {
                this.DeRotate();
                return;
            }
            this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, forward) > 0f ? 1f : -1f));
        }

        //added by gremlin devksmod doorbit
        //private void DoOrbit(Planet OrbitTarget, float elapsedTime)
        //{            
        //    if (this.findNewPosTimer > 0f)
        //    {
        //        ArtificialIntelligence artificialIntelligence = this;
        //        artificialIntelligence.findNewPosTimer = artificialIntelligence.findNewPosTimer - elapsedTime;

        //    }
        //    else
        //    {
        //        this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 2500f);
        //        if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500f)
        //        {
        //            ArtificialIntelligence orbitalAngle = this;
        //            orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
        //            if (this.OrbitalAngle >= 360f)
        //            {
        //                ArtificialIntelligence orbitalAngle1 = this;
        //                orbitalAngle1.OrbitalAngle = orbitalAngle1.OrbitalAngle - 360f;
        //            }
        //            this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 2500f);
        //            if (this.inOrbit == false) this.inOrbit = true;
        //        }
        //        this.findNewPosTimer = 1.5f;

        //    }
        //    float single = Vector2.Distance(this.Owner.Center, this.OrbitPos);
        //    if (single < 7500f)
        //    {
        //        this.Owner.HyperspaceReturn();
        //        if (this.State != AIState.Bombard && this.State!=AIState.AssaultPlanet && this.State != AIState.BombardTroops && this.State!=AIState.Boarding && !this.IgnoreCombat)
        //        {
        //            this.HasPriorityOrder = false;
        //        }

        //    }
        //    if (single <= 15000f)
        //    {
        //        if (this.Owner.speed > 150f && this.Owner.engineState != Ship.MoveState.Warp)
        //        {
        //            this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, 150f);//this.Owner.speed / 3.5f);
        //            return;
        //        }
        //        if (this.Owner.engineState != Ship.MoveState.Warp)
        //        {
        //            this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
        //        }
        //        return;
        //    }
        //    Vector2 vector2 = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, OrbitTarget.Position));
        //    Vector2 vector21 = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
        //    Vector2 vector22 = new Vector2(-vector21.Y, vector21.X);
        //    Math.Acos((double)Vector2.Dot(vector2, vector21));
        //    Vector2.Dot(vector2, vector22);
        //    this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
        //}

        /*
        private void DoOrbit(Planet OrbitTarget, float elapsedTime)
        {
            float radius = OrbitTarget.ObjectRadius + this.Owner.Radius + 1500f;
            if (this.Owner.velocityMaximum == 0)
                return;
            float distanceToOrbitSpot = Vector2.Distance(this.OrbitPos, this.Owner.Center);
            Vector2 test = Vector2.Subtract(this.Owner.Center, this.OrbitPos);
            if ((double)this.findNewPosTimer <= 0.0)//|| distanceToOrbitSpot <1500)
            {

                if (distanceToOrbitSpot < radius || this.Owner.speed == 0)
                {

                    this.OrbitalAngle += MathHelper.ToDegrees((float)Math.Asin(this.Owner.yBankAmount * 10));//   this.Owner.rotationRadiansPerSecond > 1 ? 1 : this.Owner.rotationRadiansPerSecond )) ;//< .1f ? .1f : this.Owner.rotationRadiansPerSecond));//* elapsedTime);// MathHelper.ToDegrees((float)Math.Asin((double)this.Owner.rotationRadiansPerSecond / 1500.0));
                    if ((double)this.OrbitalAngle >= 360.0)
                        this.OrbitalAngle -= 360f;
                }
                this.findNewPosTimer = elapsedTime * 10;// this.Owner.rotationRadiansPerSecond > 1 ? 1 : this.Owner.rotationRadiansPerSecond;///< .1f ? .1f : this.Owner.rotationRadiansPerSecond) ;// this.Owner.rotationRadiansPerSecond;// elapsedTime;// 1; //1.5
                this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, radius);// 1500 ); //2500f //OrbitTarget.ObjectRadius +1000 + this.Owner.Radius);// 2500f);
            }
            else
                this.findNewPosTimer -= elapsedTime;
            float num1 = distanceToOrbitSpot;// Vector2.Distance(this.Owner.Center, this.OrbitPos);
            if ((double)num1 < 7500.0)
            {
                this.Owner.HyperspaceReturn();
                if (this.State != AIState.Bombard)
                    this.HasPriorityOrder = false;
            }
            if (num1 > 15000.0)
            {

                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
            }
            else if //(num1 < 5000 &&  this.Owner.engineState != Ship.MoveState.Warp) 
                (num1 < 5000 && Vector2.Distance(this.Owner.Center, OrbitTarget.Position) < radius + 1000f && this.Owner.engineState != Ship.MoveState.Warp)    //1200.0 && this.Owner.engineState != Ship.MoveState.Warp) (double)this.Owner.speed > 50&&
            {

                float opt = num1;// Vector2.Distance(this.Owner.Center, this.OrbitPos);
                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
            }
            else
            {
                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);// > 50 ? 50 : this.Owner.speed);
            }
        }
        */

        //DoOrbit is used by the following plans: Bombard, Landtroop, BombTroops, Exterminate
        private void DoOrbit(Planet OrbitTarget, float elapsedTime)  //fbedard: my version of DoOrbit, fastest possible?
        {            
            if (this.Owner.velocityMaximum == 0)
                return;

            if (this.Owner.GetShipData().ShipCategory == ShipData.Category.Civilian && Vector2.Distance(OrbitTarget.Position, this.Owner.Center) > Empire.ProjectorRadius * 2)
            {
                //this.PlotCourseToNew(OrbitPos, this.Owner.Center);
                this.OrderMoveTowardsPosition(OrbitPos, 0, Vector2.Zero, false, this.OrbitTarget);
                //this.ThrustTowardsPosition(OrbitTarget.Position, elapsedTime, this.Owner.speed);
                this.OrbitPos = OrbitTarget.Position;
                return;
            }

            if (Vector2.Distance(OrbitTarget.Position, this.Owner.Center) > 15000f)
            {
                this.ThrustTowardsPosition(OrbitTarget.Position, elapsedTime, this.Owner.speed);
                this.OrbitPos = OrbitTarget.Position;
                return;
            }

            float radius = OrbitTarget.ObjectRadius + this.Owner.Radius +1200f;
            float distanceToOrbitSpot = Vector2.Distance(this.OrbitPos, this.Owner.Center);          
            
            if (this.findNewPosTimer <= 0f)
            {
                if (distanceToOrbitSpot <= radius || this.Owner.speed == 0f)
                {                    
                    this.OrbitalAngle += MathHelper.ToDegrees((float)Math.Asin(this.Owner.yBankAmount * 10f));
                    if (this.OrbitalAngle >= 360f)
                        this.OrbitalAngle -= 360f;
                }
                this.findNewPosTimer =  elapsedTime * 10f;
                this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, radius);
            }
            else
                this.findNewPosTimer -= elapsedTime;

            if (distanceToOrbitSpot < 7500f)
            {
                if (this.Owner.engineState == Ship.MoveState.Warp)
                    this.Owner.HyperspaceReturn();
                if (this.State != AIState.Bombard)
                    this.HasPriorityOrder = false;
            }
            if (distanceToOrbitSpot < 500f)
            {
                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed > 300f ? 300f : this.Owner.speed);
            }
            else
                {
                    this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
                }
        }
        
       /*  
        private void DoOrbitBut(Planet OrbitTarget, float elapsedTime)
        {
            if(this.OrbitPos !=Vector2.Zero)
            this.OrbitPos += OrbitTarget.Position;
            float distanceToOrbitSpot = Vector2.Distance(OrbitTarget.Position, this.Owner.Center);
            if ((this.findNewPosTimer <= 0.0 && distanceToOrbitSpot < 5000) || this.OrbitPos == Vector2.Zero)
            {
                this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 1500); //2500f //OrbitTarget.ObjectRadius +1000 + this.Owner.Radius);// 2500f);
                if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500.0)
                {
                    this.OrbitalAngle = (int)this.OrbitalAngle + 15f;
                    if (this.OrbitalAngle >= 360.0)
                        this.OrbitalAngle -= 360f;
                    this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 2500f); //OrbitTarget.ObjectRadius + 1000 + this.Owner.Radius);// 2500f);
                }
                this.findNewPosTimer = 1f;
            }
            else
            {
                this.findNewPosTimer -= elapsedTime;
                if (OrbitPos != null && Vector2.Distance(OrbitTarget.Position, this.OrbitPos) > 15000)
                    this.OrbitPos = Vector2.Zero;
            }
            float num1 = Vector2.Distance(this.Owner.Center, this.OrbitPos);
            if (num1 < 7500.0)
            {
                this.Owner.HyperspaceReturn();
                if (this.State != AIState.Bombard)
                    this.HasPriorityOrder = false;
            }
            if (num1 > 15000.0)
            {
                this.ThrustTowardsPosition(OrbitTarget.Position, elapsedTime, this.Owner.speed);
            }
            else if (this.Owner.speed > 50 && num1 < 5000 && this.Owner.engineState != Ship.MoveState.Warp)    //1200.0 && this.Owner.engineState != Ship.MoveState.Warp)
            {
                float maxSpeed = this.Owner.velocityMaximum;
                this.Owner.speed = maxSpeed / (OrbitTarget.OrbitalRadius * (float)Math.PI * 2 * elapsedTime);   //      this.Owner.GetSTLSpeed() > 200 ? 200 : this.Owner.speed;

                this.RotateToFaceMovePosition(elapsedTime, this.OrbitPos);
                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
            }
            else
            {
                if (this.Owner.engineState == Ship.MoveState.Warp)
                    return;

                this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);// > 50 ? 50 : this.Owner.speed);
            }
        }
		private void DoOrbitNoWarp(Planet OrbitTarget, float elapsedTime)
		{
			if (this.findNewPosTimer > 0f)
			{
				ArtificialIntelligence artificialIntelligence = this;
				artificialIntelligence.findNewPosTimer = artificialIntelligence.findNewPosTimer - elapsedTime;
			}
			else
			{
				this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 2500f);
				if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500f)
				{
					ArtificialIntelligence orbitalAngle = this;
					orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
					if (this.OrbitalAngle >= 360f)
					{
						ArtificialIntelligence orbitalAngle1 = this;
						orbitalAngle1.OrbitalAngle = orbitalAngle1.OrbitalAngle - 360f;
					}
					this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, OrbitTarget.Position, 2500f);
				}
				this.findNewPosTimer = 0.5f;
			}
			Vector2.Distance(this.Owner.Center, OrbitTarget.Position);
			if (this.Owner.speed > 1200f)
			{
				this.MoveTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed / 3.5f);
				return;
			}
			this.MoveTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed / 2f);
		}
        */

		private void DoRebase(ArtificialIntelligence.ShipGoal Goal)
		{
			if (this.Owner.TroopList.Count == 0)
			{
				this.Owner.QueueTotalRemoval();
			}
			else if (Goal.TargetPlanet.AssignTroopToTile(this.Owner.TroopList[0]))
			{
				this.Owner.TroopList.Clear();
				this.Owner.QueueTotalRemoval();
				return;
			}
            else
            {
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
            }
		}

		private void DoRefitORIG(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
			QueueItem qi = new QueueItem()
			{
				isShip = true,
				productionTowards = 0f,
				sData = ResourceManager.ShipsDict[goal.VariableString].GetShipData()
			};
			if (qi.sData == null)
			{
				this.OrderQueue.Clear();
				this.State = AIState.AwaitingOrders;
			}
			int cost = (int)(ResourceManager.ShipsDict[goal.VariableString].GetCost(this.Owner.loyalty) - this.Owner.GetCost(this.Owner.loyalty));
			if (cost < 0)
			{
				cost = 0;
			}
			cost = cost + 10 * (int)UniverseScreen.GamePaceStatic;
			qi.Cost = (float)cost;
			qi.isRefit = true;
			this.OrbitTarget.ConstructionQueue.Add(qi);
			this.Owner.QueueTotalRemoval();
		}

        //added by gremlin refit while in fleet
        private void DoRefit(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
        {
            QueueItem qi = new QueueItem()
            {
                isShip = true,
                productionTowards = 0f,
                sData = ResourceManager.ShipsDict[goal.VariableString].GetShipData()
            };

            if (qi.sData == null)
            {
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
            }
            int cost = (int)(ResourceManager.ShipsDict[goal.VariableString].GetCost(this.Owner.loyalty) - this.Owner.GetCost(this.Owner.loyalty));
            if (cost < 0)
            {
                cost = 0;
            }
            cost = cost + 10 * (int)UniverseScreen.GamePaceStatic;
            if (this.Owner.loyalty.isFaction)
                qi.Cost = 0;
            else
                qi.Cost = (float)cost;
            qi.isRefit = true;
            //Added by McShooterz: refit keeps name and level
            if(this.Owner.VanityName != this.Owner.Name)
                qi.RefitName = this.Owner.VanityName;
            qi.sData.Level = (byte)this.Owner.Level;
            if (this.Owner.fleet != null)
            {

                FleetDataNode node = this.Owner.fleet.DataNodes.Where(thenode => thenode.GetShip() == this.Owner).First();

                Goal refitgoal = new Goal
                {
                    beingBuilt = ResourceManager.ShipsDict[goal.VariableString],

                    GoalName = "FleetRequisition",


                };
                refitgoal.Step = 1;
                refitgoal.beingBuilt.fleet = this.Owner.fleet;
                refitgoal.beingBuilt.RelativeFleetOffset = node.FleetOffset;
                node.GoalGUID = refitgoal.guid;
                refitgoal.SetFleet(this.Owner.fleet);
                refitgoal.SetPlanetWhereBuilding(this.OrbitTarget);

                this.Owner.loyalty.GetGSAI().Goals.Add(refitgoal);


                qi.Goal = refitgoal;
            }
            this.OrbitTarget.ConstructionQueue.Add(qi);
            this.Owner.QueueTotalRemoval();
        }

		private void DoRepairDroneLogic(Weapon w)
		{
            try
            {
                this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                if (this.Owner.loyalty.GetShips().Where<Ship>((Ship ship) =>
                    {
                        if (ship.Health / ship.HealthMax >= 0.95f || !ship.Active)
                        {
                            return false;
                        }
                        return Vector2.Distance(this.Owner.Center, ship.Center) < 20000f;
                    }).Count<Ship>() == 0)
                {
                    this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                    return;
                }
                this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
            }
            catch
            {
                this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                return;
            }
            //bool flag = false;
            //for (int x = 0; x < this.Owner.loyalty.GetShips().Count; x++)
            //{
            //    Ship ship;
            //    try
            //    {
            //        ship = this.Owner.loyalty.GetShips()[x];
            //    }
            //    catch { continue; }
            //    if (ship.Health == ship.HealthMax || ship.Health / ship.HealthMax >= 0.95f)
            //        continue;
            //    if (Vector2.Distance(this.Owner.Center, ship.Center) < 20000f)
            //    {
            //        flag = true;
            //        break;
            //    }

            //}

            //if (!flag)
            //    return;
			using (IEnumerator<Ship> enumerator = this.Owner.GetAI().FriendliesNearby.Where<Ship>((Ship ship) => {
				if (Vector2.Distance(this.Owner.Center, ship.Center) >= 20000f || !ship.Active)
				{
					return false;
				}
				return ship.Health / ship.HealthMax < 0.95f;
			}).OrderBy<Ship, float>((Ship ship) => Vector2.Distance(this.Owner.Center, ship.Center)).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					Ship friendliesNearby = enumerator.Current;
					Vector2 target = this.findVectorToTarget(w.Center, friendliesNearby.Center);
					target.Y = target.Y * -1f;
					w.FireDrone(Vector2.Normalize(target));
				}
			}
		}

        private void DoRepairBeamLogic(Weapon w)
        {
            //foreach (Ship ship in w.GetOwner().loyalty.GetShips()
            foreach (Ship ship in this.FriendliesNearby
                .Where(ship => ship.Active && ship != w.GetOwner() 
                    && ship.Health / ship.HealthMax <.9f
                    && Vector2.Distance(this.Owner.Center, ship.Center) <= w.Range + 500f)
                    .OrderBy(ship => ship.Health))
            {
                if (ship != null)
                {
                    w.FireTargetedBeam(ship);
                    return;
                }
            }
        }

        private void DoOrdinanceTransporterLogic(ShipModule module)
        {
            foreach (Ship ship in module.GetParent().loyalty.GetShips().Where(ship => Vector2.Distance(this.Owner.Center, ship.Center) <= module.TransporterRange + 500f && ship.Ordinance < ship.OrdinanceMax && !ship.hasOrdnanceTransporter).OrderBy(ship => ship.Ordinance).ToList())
            {
                if (ship != null)
                {
                    module.TransporterTimer = module.TransporterTimerConstant;
                    float TransferAmount = 0f;
                    //check how much can be taken
                    if (module.TransporterOrdnance > module.GetParent().Ordinance)
                        TransferAmount = module.GetParent().Ordinance;
                    else
                        TransferAmount = module.TransporterOrdnance;
                    //check how much can be given
                    if (TransferAmount > ship.OrdinanceMax - ship.Ordinance)
                        TransferAmount = ship.OrdinanceMax - ship.Ordinance;
                    //Transfer
                    ship.Ordinance += TransferAmount;
                    module.GetParent().Ordinance -= TransferAmount;
                    module.GetParent().PowerCurrent -= module.TransporterPower * (TransferAmount / module.TransporterOrdnance);
                    if(this.Owner.InFrustum && ResourceManager.SoundEffectDict.ContainsKey("transporter"))
                    {
                        GameplayObject.audioListener.Position = ShipModule.universeScreen.camPos;
                        AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict["transporter"], GameplayObject.audioListener, module.GetParent().emitter, 0.5f);
                    }
                    return;
                }
            }
        }

        private void DoAssaultTransporterLogic(ShipModule module)
        {
            foreach (ArtificialIntelligence.ShipWeight ship in this.NearbyShips.Where(Ship => Ship.ship.loyalty != null && Ship.ship.loyalty != this.Owner.loyalty && Ship.ship.shield_power <= 0 && Vector2.Distance(this.Owner.Center, Ship.ship.Center) <= module.TransporterRange + 500f).OrderBy(Ship => Vector2.Distance(this.Owner.Center, Ship.ship.Center)))
            {
                if (ship != null)
                {
                    byte TroopCount = 0;
                    bool Transported = false;
                    for (byte i = 0; i < this.Owner.TroopList.Count(); i++)
                    {
                        if (this.Owner.TroopList[i] == null)
                            continue;
                        if (this.Owner.TroopList[i].GetOwner() == this.Owner.loyalty)
                        {
                            ship.ship.TroopList.Add(this.Owner.TroopList[i]);
                            this.Owner.TroopList.Remove(this.Owner.TroopList[i]);
                            TroopCount++;
                            Transported = true;
                        }
                        if (TroopCount == module.TransporterTroopAssault)
                            break;
                    }
                    if (Transported)
                    {
                        module.TransporterTimer = module.TransporterTimerConstant;
                        if (this.Owner.InFrustum && ResourceManager.SoundEffectDict.ContainsKey("transporter"))
                        {
                            GameplayObject.audioListener.Position = ShipModule.universeScreen.camPos;
                            AudioManager.PlaySoundEffect(ResourceManager.SoundEffectDict["transporter"], GameplayObject.audioListener, module.GetParent().emitter, 0.5f);
                        }
                        return;
                    }
                }
            }
        }



		private void DoResupply(float elapsedTime)
		{
			switch (this.resupplystep)
			{
				case 0:
				{
					List<Planet> potentials = new List<Planet>();
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
					foreach (Planet p in this.Owner.loyalty.GetPlanets())
					{
						if (!p.HasShipyard)
						{
							continue;
						}
						potentials.Add(p);
					}
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
					if (potentials.Count <= 0)
					{
						break;
					}
					IOrderedEnumerable<Planet> sortedList = 
						from planet in potentials
						orderby Vector2.Distance(this.Owner.Center, planet.Position)
						select planet;
					this.resupplyTarget = sortedList.ElementAt<Planet>(0);
					this.resupplystep = 1;
					return;
				}
				case 1:
				{
					this.DoOrbit(this.resupplyTarget, elapsedTime);					
                    //if (this.Owner.Ordinance != this.Owner.OrdinanceMax || this.Owner.Health != this.Owner.HealthMax || Vector2.Distance(this.resupplyTarget.Position, this.Owner.Center) >= 7500f)
                    if (this.Owner.Ordinance < this.Owner.OrdinanceMax || this.Owner.Health < this.Owner.HealthMax)
					{
						break;
					}
					this.State = AIState.AwaitingOrders;
					if (this.Owner.loyalty.isPlayer)// == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
					{
						this.HadPO = true;
					}
					this.HasPriorityOrder = false;
					break;
				}
				default:
				{
					return;
				}
			}
		}

		private void DoReturnToHangar(float elapsedTime)
		{
			if (this.Owner.Mothership == null || !this.Owner.Mothership.Active)
			{
				this.OrderQueue.Clear();
				return;
			}
			this.ThrustTowardsPosition(this.Owner.Mothership.Center, elapsedTime, this.Owner.speed);
			//if (Vector2.Distance(this.Owner.Center, this.Owner.Mothership.Center) < 1000f)
            if (Vector2.Distance(this.Owner.Center, this.Owner.Mothership.Center) < this.Owner.Mothership.Radius + 300f)
			{
				if (this.Owner.Mothership.TroopCapacity > this.Owner.Mothership.TroopList.Count && this.Owner.TroopList.Count == 1)
				{
					this.Owner.Mothership.TroopList.Add(this.Owner.TroopList[0]);
				}
                if (this.Owner.shipData.Role == ShipData.RoleName.supply)  //fbedard: Supply ship return with Ordinance
                    this.Owner.Mothership.Ordinance += this.Owner.Ordinance;
                this.Owner.Mothership.Ordinance += this.Owner.Mass / 5f;        //fbedard: New spawning cost
                if (this.Owner.Mothership.Ordinance > this.Owner.Mothership.OrdinanceMax)
                    this.Owner.Mothership.Ordinance = this.Owner.Mothership.OrdinanceMax;
                this.Owner.QueueTotalRemoval();
                foreach (ShipModule hangar in this.Owner.Mothership.GetHangars())
                {
                    if (hangar.GetHangarShip() != this.Owner)
                    {
                        continue;
                    }
                    //added by gremlin: prevent fighters from relaunching immediatly after landing.
                    float ammoReloadTime = this.Owner.OrdinanceMax * .1f;
                    float shieldrechargeTime = this.Owner.shield_max * .1f;
                    float powerRechargeTime = this.Owner.PowerStoreMax * .1f;

                    float rearmTime = this.Owner.Health;
                    rearmTime += this.Owner.Ordinance*.1f;
                    rearmTime += this.Owner.PowerCurrent * .1f;
                    rearmTime += this.Owner.shield_power * .1f;
                    rearmTime /= (this.Owner.HealthMax + ammoReloadTime + shieldrechargeTime + powerRechargeTime);                    
                        rearmTime = (1.01f - rearmTime) * (hangar.hangarTimerConstant *(1.01f- (this.Owner.Level + hangar.GetParent().Level)/10 ));  // fbedard: rearm time from 50% to 150%
                        if (rearmTime < 0)
                            rearmTime = 1;
                    //CG: if the fighter is fully functional reduce rearm time to very little. The default 5 minute hangar timer is way too high. It cripples fighter usage.
                    //at 50% that is still 2.5 minutes if the fighter simply launches and returns. with lag that can easily be 10 or 20 minutes. 
                    //at 1.01 that should be 3 seconds for the default hangar.
                    
                    //rearmTime = rearmConstant * rearmTime;
                    //rearmTime = (rearmConstant - rearmConstant * rearmTime) + hangar.hangarTimer > 0 ? hangar.hangarTimer : 0;
                    hangar.SetHangarShip(null);
                    hangar.hangarTimer = rearmTime;
                    hangar.installedSlot.HangarshipGuid = Guid.Empty;
                   
                }
			}
		}

		private void DoSupplyShip(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
			if (this.EscortTarget == null || !this.EscortTarget.Active)
			{
				this.OrderQueue.Clear();
                this.OrderResupplyNearest(false);
				return;
			}
            if (this.EscortTarget.GetAI().State == AIState.Resupply || this.EscortTarget.GetAI().State == AIState.Scrap ||this.EscortTarget.GetAI().State == AIState.Refit)
            {
                this.OrderQueue.Clear();
                this.OrderResupplyNearest(false);
                return;
            }
			this.ThrustTowardsPosition(this.EscortTarget.Center, elapsedTime, this.Owner.speed);
			if (Vector2.Distance(this.Owner.Center, this.EscortTarget.Center) < this.EscortTarget.Radius + 300f)
			{
                float ord_amt = this.Owner.Ordinance;
				Ship escortTarget = this.EscortTarget;
                if (this.EscortTarget.Ordinance + ord_amt > this.EscortTarget.OrdinanceMax)
                    ord_amt = this.EscortTarget.OrdinanceMax - this.EscortTarget.Ordinance;
                this.EscortTarget.Ordinance += ord_amt;
                this.Owner.Ordinance -= ord_amt;
				this.OrderQueue.Clear();
                if (this.Owner.Ordinance > 0)
                    this.State = AIState.AwaitingOrders;
                else
                    this.Owner.ReturnToHangar();

				/*
                this.Owner.QueueTotalRemoval();
				if (this.Owner.Mothership != null)
				{
					foreach (ShipModule hangar in this.Owner.Mothership.GetHangars())
					{
						if (hangar.GetHangarShip() != this.Owner)
						{
							continue;
						}
						hangar.hangarTimer = 1f;
					}
				}
                */
			}
		}

		private void DoSystemDefense(float elapsedTime)
		{
            //if (this.Owner.InCombat || this.State == AIState.Intercept)                
            //        return;
      
            if (this.SystemToDefend == null)
				this.SystemToDefend = this.Owner.GetSystem();               
           //if(this.Owner.GetSystem() != this.SystemToDefend)               
            if (this.SystemToDefend == null || (this.awaitClosest != null && this.awaitClosest.Owner == this.Owner.loyalty))
                this.AwaitOrders(elapsedTime);
            else
                this.OrderSystemDefense(this.SystemToDefend);
           //this.State = AIState.AwaitingOrders;               
		}

		private void DoTroopToShip(float elapsedTime)
		{
			if (this.EscortTarget == null || !this.EscortTarget.Active)
			{
				this.OrderQueue.Clear();
				return;
			}
			this.MoveTowardsPosition(this.EscortTarget.Center, elapsedTime);
			if (Vector2.Distance(this.Owner.Center, this.EscortTarget.Center) < this.EscortTarget.Radius +300f)
			{
				if (this.EscortTarget.TroopCapacity > this.EscortTarget.TroopList.Count)
				{
					this.EscortTarget.TroopList.Add(this.Owner.TroopList[0]);
					this.Owner.QueueTotalRemoval();
					return;
				}
				this.OrbitShip(this.EscortTarget, elapsedTime);
			}
		}

        private void DropoffGoods()
        {
            if (this.end != null)
            {
                if (this.Owner.loyalty.data.Traits.Mercantile > 0f)
                {
                    this.Owner.loyalty.AddTradeMoney(this.Owner.CargoSpace_Used * this.Owner.loyalty.data.Traits.Mercantile);
                }

                if (this.Owner.GetCargo()["Food"] > 0f)
                {
                    int maxfood = (int)this.end.MAX_STORAGE - (int)this.end.FoodHere;
                    if (this.end.FoodHere + this.Owner.GetCargo()["Food"] <= this.end.MAX_STORAGE)
                    {
                        Planet foodHere = this.end;
                        foodHere.FoodHere = foodHere.FoodHere + (float)((int)this.Owner.GetCargo()["Food"]);
                        this.Owner.GetCargo()["Food"] = 0f;
                    }
                    else
                    {
                        Planet planet = this.end;
                        planet.FoodHere = planet.FoodHere + (float)maxfood;
                        Dictionary<string, float> cargo = this.Owner.GetCargo();
                        Dictionary<string, float> strs = cargo;
                        cargo["Food"] = strs["Food"] - (float)maxfood;
                    }
                }
                if (this.Owner.GetCargo()["Production"] > 0f)
                {
                    int maxprod = (int)this.end.MAX_STORAGE - (int)this.end.ProductionHere;
                    if (this.end.ProductionHere + this.Owner.GetCargo()["Production"] <= this.end.MAX_STORAGE)
                    {
                        Planet productionHere = this.end;
                        productionHere.ProductionHere = productionHere.ProductionHere + (float)((int)this.Owner.GetCargo()["Production"]);
                        this.Owner.GetCargo()["Production"] = 0f;
                    }
                    else
                    {
                        Planet productionHere1 = this.end;
                        productionHere1.ProductionHere = productionHere1.ProductionHere + (float)maxprod;
                        Dictionary<string, float> item = this.Owner.GetCargo();
                        Dictionary<string, float> strs1 = item;
                        item["Production"] = strs1["Production"] - (float)maxprod;
                    }
                }
            }
            this.start = null;
            this.end = null;
            this.OrderQueue.RemoveFirst();
            this.OrderTrade(5f);
        }

		private void DropoffPassengers()
		{
            if (this.end == null)
            {
                this.OrderQueue.RemoveFirst();
                this.OrderTransportPassengers(0.1f);
                return;
            }

            while (this.Owner.GetCargo()["Colonists_1000"] > 0f)
			{
				Dictionary<string, float> cargo = this.Owner.GetCargo();
				cargo["Colonists_1000"] = cargo["Colonists_1000"] - 1f;
				Planet population = this.end;
				population.Population = population.Population + (float)this.Owner.loyalty.data.Traits.PassengerModifier;
			}
			if (this.end.Population > this.end.MaxPopulation + this.end.MaxPopBonus)
			{
				this.end.Population = this.end.MaxPopulation + this.end.MaxPopBonus;
			}
			this.OrderQueue.RemoveFirst();
            this.start = null;
            this.end = null;
			this.OrderTransportPassengers(5f);
		}

		private bool ExploreEmptySystem(float elapsedTime, SolarSystem system)
		{
			if (system.ExploredDict[this.Owner.loyalty])
			{
				return true;
			}
			this.MovePosition = system.Position;
			float Distance = Vector2.Distance(this.Owner.Center, this.MovePosition);
			if (Distance < 75000f)
			{
				system.ExploredDict[this.Owner.loyalty] = true;
				return true;
			}
			if (Distance > 75000f)
			{
				this.ThrustTowardsPosition(this.MovePosition, elapsedTime, this.Owner.speed);
			}
			return false;
		}

        private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
        {
            Vector2 vector2 = new Vector2(0.0f, 0.0f);
            float num1 = angle;
            float num2 = distance;
            int num3 = 0;
            float num4 = 0.0f;
            float num5 = 0.0f;
            if ((double)num1 > 360.0)
                num1 -= 360f;
            if ((double)num1 < 90.0)
            {
                float num6 = (float)((double)(90f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 1;
            }
            else if ((double)num1 > 90.0 && (double)num1 < 180.0)
            {
                float num6 = (float)((double)(num1 - 90f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 2;
            }
            else if ((double)num1 > 180.0 && (double)num1 < 270.0)
            {
                float num6 = (float)((double)(270f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 3;
            }
            else if ((double)num1 > 270.0 && (double)num1 < 360.0)
            {
                float num6 = (float)((double)(num1 - 270f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 4;
            }
            if ((double)num1 == 0.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y - num2;
            }
            if ((double)num1 == 90.0)
            {
                vector2.X = position.X + num2;
                vector2.Y = position.Y;
            }
            if ((double)num1 == 180.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y + num2;
            }
            if ((double)num1 == 270.0)
            {
                vector2.X = position.X - num2;
                vector2.Y = position.Y;
            }
            if (num3 == 1)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y - num4;
            }
            else if (num3 == 2)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 3)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 4)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y - num4;
            }
            return vector2;
        }

		private Vector2 findPointFromAngleAndDistanceorg(Vector2 position, float angle, float distance)
		{

            double theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			double D = distance;
			int gammaQuadrant = 0;
			double oppY = 0f;
			double adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
                theta = theta * Math.PI / 180.0;  //3.14159274f / 180f;
				oppY = D * Math.Sin(theta);
				adjX = D * Math.Cos(theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * Math.Sin(theta);
				adjX = D * Math.Cos(theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * Math.Sin(theta);
				adjX = D * Math.Cos(theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * Math.Sin(theta);
				adjX = D * Math.Cos(theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - (float)D;
			}
			if (gamma == 90f)
			{
                TargetPosition.X = position.X + (float)D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
                TargetPosition.Y = position.Y + (float)D;
			}
			if (gamma == 270f)
			{
                TargetPosition.X = position.X - (float)D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
                TargetPosition.X = position.X + (float)adjX;
                TargetPosition.Y = position.Y - (float)oppY;
			}
			else if (gammaQuadrant == 2)
			{
                TargetPosition.X = position.X + (float)adjX;
                TargetPosition.Y = position.Y + (float)oppY;
			}
			else if (gammaQuadrant == 3)
			{
                TargetPosition.X = position.X - (float)adjX;
                TargetPosition.Y = position.Y + (float)oppY;
			}
			else if (gammaQuadrant == 4)
			{
                TargetPosition.X = position.X - (float)adjX;
                TargetPosition.Y = position.Y - (float)oppY;
			}
			return TargetPosition;
		}

		private Vector2 findPointFromAngleAndDistanceUsingRadians(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = MathHelper.ToDegrees(angle);
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		private Vector2 findVectorBehindTarget(GameplayObject ship, float distance)
		{
			Vector2 vector2 = new Vector2(0f, 0f);
			Vector2 forward = new Vector2((float)Math.Sin((double)ship.Rotation), -(float)Math.Cos((double)ship.Rotation));
			forward = Vector2.Normalize(forward);
			return ship.Position - (forward * distance);
		}

		private Vector2 findVectorToTarget(Vector2 OwnerPos, Vector2 TargetPos)
		{
			Vector2 Vec2Target = new Vector2(0f, 0f)
			{
				X = -(OwnerPos.X - TargetPos.X),
				Y = OwnerPos.Y - TargetPos.Y
			};
			return Vec2Target;
		}

        public void FireOnTarget() //(float elapsedTime)
        {
            //Reasons not to fire
            //try
            {
                TargetShip = this.Target as Ship;
                //Relationship enemy =null;
                //base reasons not to fire. 
                if (!this.Owner.hasCommand ||this.Owner.engineState == Ship.MoveState.Warp || this.Owner.disabled || this.Owner .Weapons.Count==0 
                    //||
                    //((TargetShip != null && !this.Owner.loyalty.isFaction) && (this.Owner.loyalty.GetRelations().TryGetValue(TargetShip.loyalty, out enemy)
                    //&& enemy != null && (enemy.Treaty_Peace || enemy.Treaty_Alliance || enemy.Treaty_NAPact))))
                )
                {
                    return;
                }
                bool hasPD = false;
                //Determine if there is something to shoot at
                if (this.BadGuysNear) //|| this.Owner.InCombat
                {
                    //Target is dead or dying, will need a new one.
                    if (this.Target != null && (!this.Target.Active || TargetShip != null && TargetShip.dying))
                    {
                        foreach (Weapon purge in this.Owner.Weapons)
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
                        this.Target = null;
                        TargetShip = null;
                    }
                    foreach (Weapon purge in this.Owner.Weapons)
                    {
                        

                        if (purge.Tag_PD || purge.TruePD)
                            hasPD = true;
                        else continue;
                        break;
                    }
                    this.TrackProjectiles.Clear(); 
                    if (this.Owner.Mothership != null)
                    {
                        this.TrackProjectiles.AddRange(this.Owner.Mothership.GetAI().TrackProjectiles);
                    }
                    if (this.Owner.TrackingPower > 0 && hasPD)                                      
                    //update projectile list
                    {

                        if (this.Owner.GetSystem() != null)
                        {
                            foreach (GameplayObject missile in this.Owner.GetSystem().spatialManager.GetNearby(this.Owner))
                            {
                                Projectile targettrack = missile as Projectile;
                                if (targettrack == null || targettrack.loyalty == this.Owner.loyalty || !targettrack.weapon.Tag_Intercept)
                                    continue;
                                this.TrackProjectiles.Add(targettrack);

                            }
                        }
                        else
                        {
                            foreach (GameplayObject missile in UniverseScreen.DeepSpaceManager.GetNearby(this.Owner))
                            {
                                Projectile targettrack = missile as Projectile;
                                if (targettrack == null || targettrack.loyalty == this.Owner.loyalty || !targettrack.weapon.Tag_Intercept)
                                    continue;
                                this.TrackProjectiles.Add(targettrack);
                            }
                            
                        }
                        
                        this.TrackProjectiles = this.TrackProjectiles.OrderBy(prj =>  Vector2.Distance(this.Owner.Center, prj.Center)).ToList();

                    }
       
                    float lag = Ship.universeScreen.Lag;
                    //Go through each weapon
                    float index = 0; //count up weapons.
                    //save target ship if it is a ship.
                    this.TargetShip = this.Target as Ship;
                    //group of weapons into chunks per thread available
                    //sbyte AddTargetsTracked = 0;
                    var source = Enumerable.Range(0, this.Owner.Weapons.Count).ToArray();
                            var rangePartitioner = Partitioner.Create(0, source.Length);
                    //handle each weapon group in parallel
                            Parallel.ForEach(rangePartitioner, (range, loopState) =>
                                           {
                                               //standard for loop through each weapon group.
                                               for (int T = range.Item1; T < range.Item2; T++)
                                               {
                                                   Weapon weapon = this.Owner.Weapons[T];
                                                   weapon.TargetChangeTimer -= 0.0167f;
                                                   //Reasons for this weapon not to fire 
                                                   if ( !weapon.moduleAttachedTo.Active 
                                                       || weapon.timeToNextFire > 0f 
                                                       || !weapon.moduleAttachedTo.Powered || weapon.IsRepairDrone || weapon.isRepairBeam
                                                       || weapon.PowerRequiredToFire > this.Owner.PowerCurrent
                                                       || weapon.TargetChangeTimer >0
                                                       )
                                                   {
                                                       continue;
                                                       //return;
                                                   }
                                                   if ((!weapon.TruePD || !weapon.Tag_PD) && this.Owner.isPlayerShip())
                                                       continue;
                                                   ShipModule moduletarget = weapon.fireTarget as ShipModule;
                                                   //if firing at the primary target mark weapon as firing on primary.
                                                   if (!(weapon.fireTarget is Projectile) && weapon.fireTarget != null && (weapon.fireTarget == this.Target || (moduletarget != null && (moduletarget.GetParent() as GameplayObject) == this.Target)))
                                                       weapon.PrimaryTarget = true;                                                   
                                                    //check if weapon target as a gameplay object is still a valid target    
                                                   if (weapon.fireTarget !=null )
                                                   {
                                                       
                                                       if (( weapon.fireTarget !=null && !this.Owner.CheckIfInsideFireArc(weapon, weapon.fireTarget))                                                           
                                                           //check here if the weapon can fire on main target.                                                           
                                                           || (this.Target != null && weapon.SalvoTimer <=0 && weapon.BeamDuration <=0 && (!weapon.PrimaryTarget && !(weapon.fireTarget is Projectile) && this.Owner.CheckIfInsideFireArc(weapon, this.Target)))                                                         
                                                           )
                                                       {
                                                           weapon.TargetChangeTimer = .1f * weapon.moduleAttachedTo.XSIZE * weapon.moduleAttachedTo.YSIZE;
                                                           weapon.fireTarget = null;
                                                           //if (weapon.isBeam || weapon.isMainGun)
                                                           //    weapon.TargetChangeTimer = .90f;
                                                           if (weapon.isTurret)
                                                               weapon.TargetChangeTimer *= .5f;
                                                           if(weapon.Tag_PD)
                                                           {
                                                               weapon.TargetChangeTimer *= .5f;
                                                           }
                                                           if (weapon.TruePD)
                                                           {
                                                               weapon.TargetChangeTimer *= .25f;
                                                           }

                             
                                                           
                                                       }
                                                       
                                                   }
                                                   //if weapon target is null reset primary target and decrement target change timer.
                                                   if (weapon.fireTarget == null && !this.Owner.isPlayerShip())
                                                   {
                                                       
                                                       if (weapon.PrimaryTarget != false)
                                                           weapon.PrimaryTarget = false;
                                                   }
                                                   //Reasons for this weapon not to fire                    
                                                   if (weapon.fireTarget == null && weapon.TargetChangeTimer >0 ) // ||!weapon.moduleAttachedTo.Active || weapon.timeToNextFire > 0f || !weapon.moduleAttachedTo.Powered || weapon.IsRepairDrone || weapon.isRepairBeam)
                                                   {
                                                       continue;
                                                       //return;
                                                   }
                                                   //main targeting loop. little check here to disable the whole thing for debugging.
                                                   if (true)
                                                   {
                                                       //Can this weapon fire on ships
                                                       if (this.BadGuysNear && !weapon.TruePD  )
                                                       {
                                                           //if there are projectile to hit and weapons that can shoot at them. do so. 
                                                           if(this.TrackProjectiles.Count >0 && weapon.Tag_PD )
                                                           {
                                                               for (int i = 0; i < this.TrackProjectiles.Count && i < this.Owner.TrackingPower + this.Owner.Level; i++)
                                                               {
                                                                   Projectile proj;
                                                                   {
                                                                       proj = this.TrackProjectiles[i];
                                                                   }

                                                                   if (proj == null || !proj.Active || proj.Health <= 0 || !proj.weapon.Tag_Intercept)
                                                                       continue;
                                                                   if (this.Owner.CheckIfInsideFireArc(weapon, proj as GameplayObject))
                                                                   {
                                                                       weapon.fireTarget = proj;
                                                                       //AddTargetsTracked++;
                                                                       break;
                                                                   }
                                                               }
                                                           }
                                                           //Is primary target valid
                                                           if (weapon.fireTarget == null)
                                                               if (this.Owner.CheckIfInsideFireArc(weapon, this.Target))
                                                               {
                                                                   weapon.fireTarget = this.Target;
                                                                   weapon.PrimaryTarget = true;
                                                               }

                                                           //Find alternate target to fire on
                                                           //this seems to be very expensive code. 
                                                           if (true)
                                                           {
                                                               if (weapon.fireTarget == null && this.Owner.TrackingPower > 0)
                                                               {
                                                                   //limit to one target per level.
                                                                   sbyte tracking = this.Owner.TrackingPower;
                                                                   for (int i = 0; i < this.PotentialTargets.Count && i < tracking + this.Owner.Level; i++) //
                                                                   {
                                                                       Ship PotentialTarget = this.PotentialTargets[i];
                                                                       if (PotentialTarget == this.TargetShip)
                                                                       {
                                                                           tracking++;
                                                                           continue;                                                                           
                                                                       }
                                                                       if (!this.Owner.CheckIfInsideFireArc(weapon, PotentialTarget))
                                                                       {
                                                                           continue;
                                                                       }
                                                                       weapon.fireTarget = PotentialTarget;
                                                                       //AddTargetsTracked++;
                                                                       break;

                                                                   }
                                                               } 
                                                           }
                                                           //If a ship was found to fire on, change to target an internal module if target is visible  || weapon.Tag_Intercept
                                                           if (weapon.fireTarget != null)
                                                           {
                                                               if (weapon.fireTarget is Ship && (GlobalStats.ForceFullSim || this.Owner.InFrustum || (weapon.fireTarget as Ship).InFrustum))// || (this.Owner.InFrustum || this.Target != null && TargetShip.InFrustum)))
                                                               {
                                                                   weapon.fireTarget = (weapon.fireTarget as Ship).GetRandomInternalModule(weapon);
                                                                   //weapon.fireTarget;// = fireTarget;
                                                               }

                                                           }
                                                       }
                                                       //No ship to target, check for projectiles
                                                       if (weapon.fireTarget == null && weapon.Tag_PD)
                                                       {

                                                          
                                                          

                                                           //projectile list is created in teh scan for combat combats method.
                                                           if (weapon.fireTarget == null)
                                                           {

                                                               for (int i = 0; i < this.TrackProjectiles.Count && i < this.Owner.TrackingPower + this.Owner.Level; i++)
                                                               {
                                                                   Projectile proj;
                                                                   {
                                                                       proj = this.TrackProjectiles[i];
                                                                   }

                                                                   if (proj == null || !proj.Active || proj.Health <= 0 || !proj.weapon.Tag_Intercept)
                                                                       continue;
                                                                   if (this.Owner.CheckIfInsideFireArc(weapon, proj as GameplayObject))
                                                                   {
                                                                       weapon.fireTarget = proj;
                                                                       //AddTargetsTracked++;
                                                                       break;
                                                                   }
                                                               }



                                                           }




                                                       }

                                                   }
                                                   //if (weapon.fireTarget !=null && )
                                                   //    weapon.fireTarget = null;

                                               }
                                           });
                    //this section actually fires the weapons. This whole firing section can be moved to some other area of the code. This code is very expensive. 
                    if(1==1)
                    foreach (Weapon weapon in this.Owner.Weapons)
                    {
                        if (weapon.fireTarget != null &&(weapon.moduleAttachedTo.Active && weapon.timeToNextFire <= 0f && weapon.moduleAttachedTo.Powered ))
                        {
                            if (!(weapon.fireTarget is Ship))
                            {
                                GameplayObject target = weapon.fireTarget;
                                if (weapon.isBeam)
                                    weapon.FireTargetedBeam(target);
                                else if (weapon.Tag_Guided)
                                {
                                    if ((index > 10 && lag > .05 && !GlobalStats.ForceFullSim) && (!weapon.Tag_Intercept) && (weapon.fireTarget is ShipModule))
                                        this.FireOnTargetNonVisible(weapon, (weapon.fireTarget as ShipModule).GetParent());
                                    else
                                        weapon.Fire(new Vector2((float)Math.Sin((double)this.Owner.Rotation + MathHelper.ToRadians(weapon.moduleAttachedTo.facing)), -(float)Math.Cos((double)this.Owner.Rotation + MathHelper.ToRadians(weapon.moduleAttachedTo.facing))), target);
                                    index++;
                                }
                                else
                                {
                                    if (index > 10 && lag > .05 && !GlobalStats.ForceFullSim && (weapon.fireTarget is ShipModule))
                                        this.FireOnTargetNonVisible(weapon, (weapon.fireTarget as ShipModule).GetParent());
                                    else
                                        CalculateAndFire(weapon, target, false);
                                    index++;
                                }
                            }
                            else
                                this.FireOnTargetNonVisible(weapon, weapon.fireTarget);
                        }
                    }

                }
            }
            //catch (Exception e)
            {
#if DEBUG
                //System.Diagnostics.Debug.WriteLine(e.InnerException); 
#endif

            }

            this.TargetShip = null;

        }

        public void CalculateAndFire(Weapon weapon, GameplayObject target, bool SalvoFire)
        {
            ShipModule moduleTarget = target as ShipModule;
            Projectile projectileTarget = target as Projectile;
            Vector2 dir = Vector2.Zero;
            Vector2 projectedPosition = Vector2.Zero;
            //this.moduleTarget = target as ShipModule;

            if (projectileTarget !=null)
            {
                float distance = Vector2.Distance(weapon.Center, projectileTarget.Center) + projectileTarget.Velocity.Length() == 0 ? 0 : 500;
                 dir = Vector2.Zero;
      
                 dir = (Vector2.Normalize(this.findVectorToTarget(weapon.Center, projectileTarget.Center)) * (weapon.ProjectileSpeed + this.Owner.Velocity.Length()));

                float timeToTarget = distance / dir.Length();

                projectedPosition = projectileTarget.Center + (projectileTarget.Velocity * timeToTarget);
                distance = Vector2.Distance(weapon.Center, projectedPosition);
                dir = (Vector2.Normalize(this.findVectorToTarget(weapon.Center, projectedPosition)) * (weapon.ProjectileSpeed + this.Owner.Velocity.Length()));
                timeToTarget = distance / dir.Length();
                projectedPosition = projectileTarget.Center + ((projectileTarget.Velocity * timeToTarget) * 0.85f);
            }
            else if (moduleTarget !=null)
            {
                float distance = Vector2.Distance(weapon.Center, moduleTarget.Center) + moduleTarget.Velocity.Length() == 0 ? 0 : 500;
                 dir = Vector2.Zero;

                dir = (Vector2.Normalize(this.findVectorToTarget(weapon.Center, target.Center)) * (weapon.ProjectileSpeed + this.Owner.Velocity.Length()));

                float timeToTarget = distance / dir.Length();
                projectedPosition = target.Center;
    //            if (moduleTarget.GetParent().Velocity.Length() > 0.0f && moduleTarget.GetParent().speed <= 0)
             //       System.Diagnostics.Debug.WriteLine(this.Owner.GetSystemName() + " - Velocity error compensator in calculate and fire. Fix weird velocity");
  //              if (moduleTarget.GetParent().Velocity.Length() > 0.0f && moduleTarget.GetParent().speed > 0)
                    projectedPosition = moduleTarget.Center + (moduleTarget.GetParent().Velocity * timeToTarget);
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine(this.Owner.GetSystemName() + " - posistion compensator in calculate and fire.");
                //    projectedPosition = moduleTarget.Center;
                //}
                if (projectedPosition != moduleTarget.Center && moduleTarget.GetParent().Velocity.Length() <= 0)
                {
                    System.Diagnostics.Debug.WriteLine(this.Owner.GetSystem().Name + " - calculate and fire error");
                    //moved docs target correction here. 
                    Vector2 fireatstationary = Vector2.Zero;
                    fireatstationary = Vector2.Normalize(this.findVectorToTarget(weapon.Center, moduleTarget.Center));
                    if (SalvoFire)
                        weapon.FireSalvo(fireatstationary, target);
                    else
                        weapon.Fire(fireatstationary, target);
                    return;

                }
                distance = Vector2.Distance(weapon.Center, projectedPosition);
                if (moduleTarget.GetParent().Velocity.Length() > 0.0f) 
                    dir = (Vector2.Normalize(this.findVectorToTarget(weapon.Center, projectedPosition)) * (weapon.ProjectileSpeed + this.Owner.Velocity.Length()));
                else
                {
                    dir = this.findVectorToTarget(weapon.Center, projectedPosition);

                }
                timeToTarget = distance / dir.Length();
                projectedPosition = target.Center + (moduleTarget.GetParent().Velocity * timeToTarget);
            }


            dir = this.findVectorToTarget(weapon.Center, projectedPosition);
            dir.Y = dir.Y * -1f;
            if (moduleTarget ==null  || moduleTarget.GetParent().Velocity.Length() >0)
                dir = Vector2.Normalize(dir);

            if (SalvoFire)
                weapon.FireSalvo(dir, target);
            else
                weapon.Fire(dir, target);
        }

        private void FireOnTargetNonVisible(Weapon w, GameplayObject fireTarget)
        {
            if (this.Owner.Ordinance < w.OrdinanceRequiredToFire || this.Owner.PowerCurrent < w.PowerRequiredToFire)
            {
                return;
            }
            w.timeToNextFire = w.fireDelay;
            if (w.IsRepairDrone)
            {
                return;
            }
            if (TargetShip == null || !TargetShip.Active || TargetShip.dying || !w.TargetValid(TargetShip.shipData.Role)
                || TargetShip.engineState == Ship.MoveState.Warp || !this.Owner.CheckIfInsideFireArc(w, TargetShip))
                return;
            Ship owner = this.Owner;
            owner.Ordinance = owner.Ordinance - w.OrdinanceRequiredToFire;
            Ship powerCurrent = this.Owner;
            powerCurrent.PowerCurrent = powerCurrent.PowerCurrent - w.PowerRequiredToFire;
            powerCurrent.PowerCurrent -= w.BeamPowerCostPerSecond * w.BeamDuration;

            this.Owner.InCombatTimer = 15f;
            if (fireTarget is Projectile)
            {
                fireTarget.Damage(w.GetOwner(), w.DamageAmount);
                return;
            }
            if (!(fireTarget is Ship))
            {
                if (fireTarget is ShipModule)
                {
                    w.timeToNextFire = w.fireDelay;
                    IOrderedEnumerable<ModuleSlot> sortedList =
                        from slot in (fireTarget as ShipModule).GetParent().ExternalSlots
                        orderby Vector2.Distance(slot.module.Center, this.Owner.Center)
                        select slot;
                    float damage = w.DamageAmount;
                    if (w.isBeam)
                    {
                        damage = damage * 90f;
                    }
                    if (w.SalvoCount > 0)
                    {
                        damage = damage * (float)w.SalvoCount;
                    }
                    sortedList.First<ModuleSlot>().module.Damage(this.Owner, damage);
                }
                return;
                (fireTarget as Ship).MoveModulesTimer = 2;
            }
            w.timeToNextFire = w.fireDelay;
            if ((fireTarget as Ship).ExternalSlots.Count == 0)
            {
                (fireTarget as Ship).Die(null, true);
                return;
            }
            if ((fireTarget as Ship).GetAI().CombatState == CombatState.Evade)   //fbedard: firing on evading ship can miss !
                if (RandomMath.RandomBetween(0f, 100f) < (5f + (fireTarget as Ship).experience))
                    return;

            float nearest = 0;
            ModuleSlot ClosestES = null;
            //bad fix for external module badness.
            //Ray ffer = new Ray();
            //BoundingBox target = new BoundingBox();
            //ffer.Position=new Vector3(this.Owner.Center,0f);

            try
            {
                foreach (ModuleSlot ES in (fireTarget as Ship).ExternalSlots)
                {
                    if (ES.module.ModuleType == ShipModuleType.Dummy || !ES.module.Active || ES.module.Health <= 0)
                        continue;
                    float temp = Vector2.Distance(ES.module.Center, this.Owner.Center);
                    if (nearest == 0 || temp < nearest)
                    {
                        nearest = temp;
                        ClosestES = ES;
                    } 
                }
            }
            catch { }
            if (ClosestES == null)
                return;
            // List<ModuleSlot> 
            IEnumerable<ModuleSlot> ExternalSlots = (fireTarget as Ship).ExternalSlots.
                Where(close => close.module.Active && close.module.ModuleType != ShipModuleType.Dummy && close.module.quadrant == ClosestES.module.quadrant && close.module.Health > 0);//.ToList();   //.OrderByDescending(shields=> shields.Shield_Power >0);//.ToList();
            if ((fireTarget as Ship).shield_power > 0f)
            {
                for (int i = 0; i < (fireTarget as Ship).GetShields().Count; i++)
                {
                    if ((fireTarget as Ship).GetShields()[i].Active && (fireTarget as Ship).GetShields()[i].shield_power > 0f)
                    {
                        float damage = w.DamageAmount;
                        if (w.isBeam)
                        {
                            damage = damage * 90f;
                        }
                        if (w.SalvoCount > 0)
                        {
                            damage = damage * (float)w.SalvoCount;
                        }
                        (fireTarget as Ship).GetShields()[i].Damage(this.Owner, damage);
                        return;
                    }
                }
                return;
            }
            //this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(0f, 100f) <= 50f ||
            if (ExternalSlots.ElementAt(0).module.shield_power > 0f)
            {
                for (int i = 0; i < ExternalSlots.Count(); i++)
                {
                    if (ExternalSlots.ElementAt(i).module.Active && ExternalSlots.ElementAt(i).module.shield_power <= 0f)
                    {
                        float damage = w.DamageAmount;
                        if (w.isBeam)
                        {
                            damage = damage * 90f;
                        }
                        if (w.SalvoCount > 0)
                        {
                            damage = damage * (float)w.SalvoCount;
                        }
                        ExternalSlots.ElementAt(i).module.Damage(this.Owner, damage);
                        return;
                    }
                }
                return;
            }

            for (int i = 0; i < ExternalSlots.Count(); i++)
            {
                if (ExternalSlots.ElementAt(i).module.Active && ExternalSlots.ElementAt(i).module.shield_power <= 0f)
                {
                    float damage = w.DamageAmount;
                    if (w.isBeam)
                    {
                        damage = damage * 90f;
                    }
                    if (w.SalvoCount > 0)
                    {
                        damage = damage * (float)w.SalvoCount;
                    }
                    ExternalSlots.ElementAt(i).module.Damage(this.Owner, damage);
                    return;
                }
            }
        }
        
		private Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
		{
			return this.findPointFromAngleAndDistance(center, angle, radius);
		}

		public void GoColonize(Planet p)
		{
			this.State = AIState.Colonize;
			this.ColonizeTarget = p;
			this.GotoStep = 0;
		}

		public void GoColonize(Planet p, Goal g)
		{
			this.State = AIState.Colonize;
			this.ColonizeTarget = p;
			this.ColonizeGoal = g;
			this.GotoStep = 0;
			this.OrderColonization(p);
		}

		public void GoRebase(Planet p)
		{
			this.HasPriorityOrder = true;
			this.State = AIState.Rebase;
			this.OrbitTarget = p;
			this.findNewPosTimer = 0f;
            //this.moveTimer = 0f;          //Not referenced in code, removing to save memory -Gretman
            this.GotoStep = 0;
			this.HasPriorityOrder = true;
			this.MovePosition.X = p.Position.X;
			this.MovePosition.Y = p.Position.Y;
		}

		public void GoTo(Vector2 movePos, Vector2 facing)
		{
			this.GotoStep = 0;
			if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
			{
				this.HasPriorityOrder = true;
			}
			this.MovePosition.X = movePos.X;
			this.MovePosition.Y = movePos.Y;
			this.FinalFacingVector = facing;
			this.State = AIState.MoveTo;
		}

		public void HoldPosition()
		{
                if (this.Owner.isSpooling || this.Owner.engineState == Ship.MoveState.Warp)
                {
                    this.Owner.HyperspaceReturn();
                }
                this.State = AIState.HoldPosition;
                this.Owner.isThrusting = false;
		}

		private void MakeFinalApproach(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
		{
            if (Goal.TargetPlanet != null)
            {
                lock (this.wayPointLocker)
                {
                    this.ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Position);
                    Goal.MovePosition = Goal.TargetPlanet.Position;
                }
            }
            //if (this.RotateToFaceMovePosition(elapsedTime, Goal.MovePosition))
            //{
            //    Goal.SpeedLimit *= .9f;
            //}
            //else
            //{
            //    Goal.SpeedLimit *= 1.1f;
            //    if (this.Owner.engineState == Ship.MoveState.Sublight)
            //    {
            //        if (Goal.SpeedLimit > this.Owner.GetSTLSpeed())
            //            Goal.SpeedLimit = this.Owner.GetSTLSpeed();
            //    }
            //    else if (Goal.SpeedLimit > this.Owner.GetmaxFTLSpeed)
            //        Goal.SpeedLimit = this.Owner.GetmaxFTLSpeed;
            //}
            this.Owner.HyperspaceReturn();
			Vector2 velocity = this.Owner.Velocity;
            if (Goal.TargetPlanet != null)
                velocity += Goal.TargetPlanet.Position;
			float timetostop = velocity.Length() / Goal.SpeedLimit;
			float Distance = Vector2.Distance(this.Owner.Center, Goal.MovePosition);
			if (Distance / (Goal.SpeedLimit + 0.001f) <= timetostop)
			{
				this.OrderQueue.RemoveFirst();
			}
			else
			{
                if (DistanceLast == Distance)
                    Goal.SpeedLimit++;
                this.ThrustTowardsPosition(Goal.MovePosition, elapsedTime, Goal.SpeedLimit);
			}
			this.DistanceLast = Distance;
		}
        //added by gremlin Deveksmod MakeFinalApproach
        private void MakeFinalApproachDev(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
        {
            float speedLimit = (int)Goal.SpeedLimit;

            this.Owner.HyperspaceReturn();
            Vector2 velocity = this.Owner.Velocity;
            float Distance = Vector2.Distance(this.Owner.Center, Goal.MovePosition);
            double timetostop;

            timetostop = (double)velocity.Length() / speedLimit;

            //if(this.RotateToFaceMovePosition(elapsedTime, Goal))
            //{
            //    speedLimit--;
            //}
            //else
            //{
            //    speedLimit++;
            //    if(speedLimit > this.Owner.GetSTLSpeed())
            //        speedLimit=this.Owner.GetSTLSpeed();
            //}
            

            
            //ShipGoal preserveGoal = this.OrderQueue.Last();

            //if ((preserveGoal.TargetPlanet != null && this.Owner.fleet == null && Vector2.Distance(preserveGoal.TargetPlanet.Position, this.Owner.Center) > 7500) || this.DistanceLast == Distance)
            //{

            //    this.OrderQueue.Clear();
            //    this.OrderQueue.AddFirst(preserveGoal);
            //    return;
            //}

            if ((double)Distance / velocity.Length() <= timetostop)  //+ .005f) //(Distance  / (velocity.Length() ) <= timetostop)//
            {
                this.OrderQueue.RemoveFirst();
            }
            else
            {
                Goal.SpeedLimit = speedLimit;

                this.ThrustTowardsPosition(Goal.MovePosition, elapsedTime, speedLimit);
            }
            this.DistanceLast = Distance;
        }
		private void MakeFinalApproachFleet(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
		{
			float Distance = Vector2.Distance(this.Owner.Center, Goal.fleet.Position + this.Owner.FleetOffset);
			if (Distance < 100f || this.DistanceLast > Distance)
			{
				this.OrderQueue.RemoveFirst();
			}
			else
			{
				this.MoveTowardsPosition(Goal.fleet.Position + this.Owner.FleetOffset, elapsedTime, Goal.fleet.speed);
			}
			this.DistanceLast = Distance;
		}

		private void MoveInDirection(Vector2 direction, float elapsedTime)
		{
			if (!this.Owner.EnginesKnockedOut)
			{
				this.Owner.isThrusting = true;
				Vector2 wantedForward = Vector2.Normalize(direction);
				Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.22f)
				{
					this.Owner.isTurning = true;
					float RotAmount = Math.Min(angleDiff, facing * elapsedTime * this.Owner.rotationRadiansPerSecond);
					if (Math.Abs(RotAmount) > angleDiff)
					{
						RotAmount = (RotAmount <= 0f ? -angleDiff : angleDiff);
					}
					if (RotAmount > 0f)
					{
						if (this.Owner.yRotation > -this.Owner.maxBank)
						{
							Ship owner = this.Owner;
							owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
						}
					}
					else if (RotAmount < 0f && this.Owner.yRotation < this.Owner.maxBank)
					{
						Ship ship = this.Owner;
						ship.yRotation = ship.yRotation + this.Owner.yBankAmount;
					}
					Ship rotation = this.Owner;
					rotation.Rotation = rotation.Rotation + RotAmount;
				}
				else if (this.Owner.yRotation > 0f)
				{
					Ship owner1 = this.Owner;
					owner1.yRotation = owner1.yRotation - this.Owner.yBankAmount;
					if (this.Owner.yRotation < 0f)
					{
						this.Owner.yRotation = 0f;
					}
				}
				else if (this.Owner.yRotation < 0f)
				{
					Ship ship1 = this.Owner;
					ship1.yRotation = ship1.yRotation + this.Owner.yBankAmount;
					if (this.Owner.yRotation > 0f)
					{
						this.Owner.yRotation = 0f;
					}
				}
				Ship velocity = this.Owner;
				velocity.Velocity = velocity.Velocity + (Vector2.Normalize(forward) * (elapsedTime * this.Owner.speed));
				if (this.Owner.Velocity.Length() > this.Owner.velocityMaximum)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * this.Owner.velocityMaximum;
				}
			}
		}

		private void MoveInDirectionAtSpeed(Vector2 direction, float elapsedTime, float speed)
		{
			if (speed == 0f)
			{
				this.Owner.isThrusting = false;
				this.Owner.Velocity = Vector2.Zero;
				return;
			}
			if (!this.Owner.EnginesKnockedOut)
			{
				this.Owner.isThrusting = true;
				Vector2 wantedForward = Vector2.Normalize(direction);
				Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff <= 0.02f)
				{
					this.DeRotate();
				}
				else
				{
					this.Owner.isTurning = true;
					Ship owner = this.Owner;
					owner.Rotation = owner.Rotation + Math.Min(angleDiff, facing * elapsedTime * this.Owner.rotationRadiansPerSecond);
				}
				Ship velocity = this.Owner;
				velocity.Velocity = velocity.Velocity + (Vector2.Normalize(forward) * (elapsedTime * speed));
				if (this.Owner.Velocity.Length() > speed)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * speed;
				}
			}
		}

		private void MoveTowardsPosition(Vector2 Position, float elapsedTime)
		{
			if (Vector2.Distance(this.Owner.Center, Position) < 50f)
			{
				this.Owner.Velocity = Vector2.Zero;
				return;
			}
			Position = Position - this.Owner.Velocity;
			if (!this.Owner.EnginesKnockedOut)
			{
				this.Owner.isThrusting = true;
				Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, Position));
				Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.02f)
				{
					float RotAmount = Math.Min(angleDiff, facing * elapsedTime * this.Owner.rotationRadiansPerSecond);
					if (RotAmount > 0f)
					{
						if (this.Owner.yRotation > -this.Owner.maxBank)
						{
							Ship owner = this.Owner;
							owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
						}
					}
					else if (RotAmount < 0f && this.Owner.yRotation < this.Owner.maxBank)
					{
						Ship ship = this.Owner;
						ship.yRotation = ship.yRotation + this.Owner.yBankAmount;
					}
					this.Owner.isTurning = true;
					Ship rotation = this.Owner;
					rotation.Rotation = rotation.Rotation + RotAmount;
				}
				float speedLimit = this.Owner.speed;
				if (this.Owner.isSpooling)
				{
					speedLimit = speedLimit * this.Owner.loyalty.data.FTLModifier;
				}
				else if (Vector2.Distance(Position, this.Owner.Center) < speedLimit)
				{
					speedLimit = Vector2.Distance(Position, this.Owner.Center) * 0.75f;
				}
				Ship velocity = this.Owner;
				velocity.Velocity = velocity.Velocity + (Vector2.Normalize(forward) * (elapsedTime * speedLimit));
				if (this.Owner.Velocity.Length() > speedLimit)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * speedLimit;
				}
			}
		}

		private void MoveTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit)
		{
			if (speedLimit == 0f)
			{
				speedLimit = 200f;
			}
			Position = Position - this.Owner.Velocity;
			if (!this.Owner.EnginesKnockedOut)
			{
				this.Owner.isThrusting = true;
				Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, Position));
				Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
				Vector2 right = new Vector2(-forward.Y, forward.X);
				float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
				float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
				if (angleDiff > 0.02f)
				{
					float RotAmount = Math.Min(angleDiff, facing * elapsedTime * this.Owner.rotationRadiansPerSecond);
					if (RotAmount > 0f)
					{
						if (this.Owner.yRotation > -this.Owner.maxBank)
						{
							Ship owner = this.Owner;
							owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
						}
					}
					else if (RotAmount < 0f && this.Owner.yRotation < this.Owner.maxBank)
					{
						Ship ship = this.Owner;
						ship.yRotation = ship.yRotation + this.Owner.yBankAmount;
					}
					this.Owner.isTurning = true;
					Ship rotation = this.Owner;
					rotation.Rotation = rotation.Rotation + RotAmount;
				}
				if (this.Owner.isSpooling)
				{
					speedLimit = speedLimit * this.Owner.loyalty.data.FTLModifier;
				}
				Ship velocity = this.Owner;
				velocity.Velocity = velocity.Velocity + (Vector2.Normalize(forward) * (elapsedTime * speedLimit));
				if (this.Owner.Velocity.Length() > speedLimit)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * speedLimit;
				}
			}
		}

		private void MoveToWithin1000(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
        {

            float distWaypt = 15000f; //fbedard
            if (this.ActiveWayPoints.Count > 1)  
                distWaypt = Empire.ProjectorRadius / 2f;

            if (this.OrderQueue.Count > 1 && this.OrderQueue.Skip(1).First().Plan != Plan.MoveToWithin1000 && goal.TargetPlanet != null)
            {
                lock (this.wayPointLocker)
                {
                    this.ActiveWayPoints.Last().Equals(goal.TargetPlanet.Position);
                    goal.MovePosition = goal.TargetPlanet.Position;
                }
            }
            float speedLimit =  (int)(this.Owner.speed)  ;
            float single = Vector2.Distance(this.Owner.Center, goal.MovePosition);
            if (this.ActiveWayPoints.Count <= 1)
            {
                if (single  < this.Owner.speed)
                {
                    speedLimit = single;
                    //this.Owner.speed =this.Owner.speed < single ? this.Owner.speed: single;
                }
      
            }
            this.ThrustTowardsPosition(goal.MovePosition, elapsedTime, speedLimit);
            if (this.ActiveWayPoints.Count <= 1)
            {
                if (single <= 1500f)
                {
                    lock (this.wayPointLocker)
                    {
                        if (this.ActiveWayPoints.Count > 1)
                        {
                            this.ActiveWayPoints.Dequeue();
                        }
                        if (this.OrderQueue.Count > 0)
                        {
                            this.OrderQueue.RemoveFirst();
                        }
                    }
                }
                //else if(this.ColonizeTarget !=null)
                //{
                //    lock (this.wayPointLocker)
                //    {
                //        this.ActiveWayPoints.First().Equals(this.ColonizeTarget.Position);
                //        this.OrderQueue.First().MovePosition = this.ColonizeTarget.Position;
                //    }
                //}
                

            }
            else if (this.Owner.engineState == Ship.MoveState.Warp)
            {
                if (single <= distWaypt)
                {
                    lock (this.wayPointLocker)
                    {
                        this.ActiveWayPoints.Dequeue();
                        if (this.OrderQueue.Count > 0)
                        {
                            this.OrderQueue.RemoveFirst();
                        }
                    }
                }
                //if (this.ColonizeTarget != null )
                //{
                //    lock (this.wayPointLocker)
                //    {

                //        if (this.OrderQueue.Where(cgoal => cgoal.Plan == Plan.MoveToWithin1000).Count() == 1)
                //        {
                //            this.ActiveWayPoints.First().Equals(this.ColonizeTarget.Position);
                //            this.OrderQueue.First().MovePosition = this.ColonizeTarget.Position;
                //        }
                //    }
                //}
            }
            else if (single <= 1500f)
            {
                lock (this.wayPointLocker)
                {
                    this.ActiveWayPoints.Dequeue();
                    if (this.OrderQueue.Count > 0)
                    {
                        this.OrderQueue.RemoveFirst();
                    }
                }
            }
            //else if (this.ColonizeTarget != null)
            //{
            //    lock (this.wayPointLocker)
            //    {
            //        this.ActiveWayPoints.First().Equals(this.ColonizeTarget.Position);
            //    }
            //}
        }

		private void MoveToWithin1000Fleet(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
			float Distance = Vector2.Distance(this.Owner.Center, goal.fleet.Position + this.Owner.FleetOffset);
            float speedLimit = goal.SpeedLimit;
            if (this.Owner.velocityMaximum >= Distance)
            {
                speedLimit = Distance;
            }
            
            if (Distance > 10000f)
			{
				this.Owner.EngageStarDrive();
			}
			else if (Distance < 1000f)
			{
				this.Owner.HyperspaceReturn();
				this.OrderQueue.RemoveFirst();
				return;
			}
            this.MoveTowardsPosition(goal.fleet.Position + this.Owner.FleetOffset, elapsedTime, speedLimit);
		}

		private void OrbitShip(Ship ship, float elapsedTime)
		{
			this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, ship.Center, 1500f);
			if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500f)
			{
				ArtificialIntelligence orbitalAngle = this;
				orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle + 15f;
				if (this.OrbitalAngle >= 360f)
				{
					ArtificialIntelligence artificialIntelligence = this;
					artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
				}
				this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, ship.Position, 2500f);
			}
			this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
		}

		private void OrbitShipLeft(Ship ship, float elapsedTime)
		{
			this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, ship.Center, 1500f);
			if (Vector2.Distance(this.OrbitPos, this.Owner.Center) < 1500f)
			{
				ArtificialIntelligence orbitalAngle = this;
				orbitalAngle.OrbitalAngle = orbitalAngle.OrbitalAngle - 15f;
				if (this.OrbitalAngle >= 360f)
				{
					ArtificialIntelligence artificialIntelligence = this;
					artificialIntelligence.OrbitalAngle = artificialIntelligence.OrbitalAngle - 360f;
				}
				this.OrbitPos = this.GeneratePointOnCircle(this.OrbitalAngle, ship.Position, 2500f);
			}
			this.ThrustTowardsPosition(this.OrbitPos, elapsedTime, this.Owner.speed);
		}

		public void OrderAllStop()
		{
			this.OrderQueue.Clear();
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.State = AIState.HoldPosition;
            this.HasPriorityOrder = false;
			ArtificialIntelligence.ShipGoal stop = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Stop, Vector2.Zero, 0f);            
			this.OrderQueue.AddLast(stop);
		}

	

		public void OrderAttackSpecificTarget(Ship toAttack)
		{
			this.TargetQueue.Clear();
            
			if (toAttack == null)
			{
				return;
			}

			if (this.Owner.loyalty.GetRelations().ContainsKey(toAttack.loyalty))
			{
				if (!this.Owner.loyalty.GetRelations()[toAttack.loyalty].Treaty_Peace)
				{
					if (this.State == AIState.AttackTarget && this.Target == toAttack)
					{
						return;
					}
					if (this.State == AIState.SystemDefender && this.Target == toAttack)
					{
						return;
					}
                    if (this.Owner.Weapons.Count == 0 || this.Owner.shipData.Role == ShipData.RoleName.troop)
					{
						this.OrderInterceptShip(toAttack);
						return;
					}
					this.Intercepting = true;
					lock (this.wayPointLocker)
					{
						this.ActiveWayPoints.Clear();
					}
					this.State = AIState.AttackTarget;
					this.Target = toAttack;
					this.Owner.InCombatTimer = 15f;
					this.OrderQueue.Clear();
					this.IgnoreCombat = false;
					this.TargetQueue.Add(toAttack);
					this.hasPriorityTarget = true;
					this.HasPriorityOrder = false;
					ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
					this.OrderQueue.AddLast(combat);
					return;
				}
				this.OrderInterceptShip(toAttack);
			}
		}

		public void OrderBombardPlanet(Planet toBombard)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.State = AIState. Bombard;
			this.Owner.InCombatTimer = 15f;
			this.OrderQueue.Clear();
			this.HasPriorityOrder = true;
			ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Bombard, Vector2.Zero, 0f)
			{
				TargetPlanet = toBombard
			};
			this.OrderQueue.AddLast(combat);
		}

        public void OrderBombardTroops(Planet toBombard)
        {
            lock (this.wayPointLocker)
            {
                this.ActiveWayPoints.Clear();
            }
            this.State = AIState.BombardTroops;
            this.Owner.InCombatTimer = 15f;
            this.OrderQueue.Clear();
            this.HasPriorityOrder = true;
            ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.BombTroops, Vector2.Zero, 0f)
            {
                TargetPlanet = toBombard
            };
            this.OrderQueue.AddLast(combat);
        }

		public void OrderColonization(Planet toColonize)
		{
			if (toColonize == null)
			{
				return;
			}
			this.ColonizeTarget = toColonize;
			this.OrderMoveTowardsPosition(toColonize.Position, 0f, new Vector2(0f, -1f), true, toColonize);
            ArtificialIntelligence.ShipGoal colonize = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Colonize, toColonize.Position, 0f)
			{
				TargetPlanet = this.ColonizeTarget
			};
			this.OrderQueue.AddLast(colonize);
			this.State = AIState.Colonize;
		}

		public void OrderDeepSpaceBuild(Goal goal)
		{
			
            this.OrderQueue.Clear();
            
      
            this.OrderMoveTowardsPosition(goal.BuildPosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Owner.Center, goal.BuildPosition)), this.findVectorToTarget(this.Owner.Center, goal.BuildPosition), true,null);
			ArtificialIntelligence.ShipGoal Deploy = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DeployStructure, goal.BuildPosition, MathHelper.ToRadians(HelperFunctions.findAngleToTarget(this.Owner.Center, goal.BuildPosition)))
			{
				goal = goal,
				VariableString = goal.ToBuildUID                
			};            
			this.OrderQueue.AddLast(Deploy);
          
		}

		public void OrderExplore()
		{
			if (this.State == AIState.Explore && this.ExplorationTarget != null)
			{
				return;
			}
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.OrderQueue.Clear();
			this.State = AIState.Explore;
			ArtificialIntelligence.ShipGoal Explore = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Explore, Vector2.Zero, 0f);
			this.OrderQueue.AddLast(Explore);
		}

		public void OrderExterminatePlanet(Planet toBombard)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.State = AIState.Exterminate;
			this.OrderQueue.Clear();
			ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Exterminate, Vector2.Zero, 0f)
			{
				TargetPlanet = toBombard
			};
			this.OrderQueue.AddLast(combat);
		}

		public void OrderFindExterminationTarget(bool ClearOrders)
		{
			if (this.ExterminationTarget == null || this.ExterminationTarget.Owner == null)
			{
				List<Planet> plist = new List<Planet>();
				foreach (KeyValuePair<Guid, Planet> planetsDict in ArtificialIntelligence.universeScreen.PlanetsDict)
				{
					if (planetsDict.Value.Owner == null)
					{
						continue;
					}
					plist.Add(planetsDict.Value);
				}
				IOrderedEnumerable<Planet> sortedList = 
					from planet in plist
					orderby Vector2.Distance(this.Owner.Center, planet.Position)
					select planet;
				if (sortedList.Count<Planet>() > 0)
				{
					this.ExterminationTarget = sortedList.First<Planet>();
					this.OrderExterminatePlanet(this.ExterminationTarget);
					return;
				}
			}
			else if (this.ExterminationTarget != null && this.OrderQueue.Count == 0)
			{
				this.OrderExterminatePlanet(this.ExterminationTarget);
			}
		}

		public void OrderFormationWarp(Vector2 destination, float facing, Vector2 fvec)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.OrderQueue.Clear();
			this.OrderMoveDirectlyTowardsPosition(destination, facing, fvec, true, this.Owner.fleet.speed);
			this.State = AIState.FormationWarp;
		}

		public void OrderFormationWarpQ(Vector2 destination, float facing, Vector2 fvec)
		{
			this.OrderMoveDirectlyTowardsPosition(destination, facing, fvec, false, this.Owner.fleet.speed);
			this.State = AIState.FormationWarp;
		}

		public void OrderInterceptShip(Ship toIntercept)
		{
			this.Intercepting = true;
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.State = AIState.Intercept;
			this.Target = toIntercept;
			this.hasPriorityTarget = true;
			this.HasPriorityOrder = false;
			this.OrderQueue.Clear();
		}

		public void OrderLandAllTroops(Planet target)
		{
            if ((this.Owner.shipData.Role == ShipData.RoleName.troop || this.Owner.HasTroopBay || this.Owner.hasTransporter) && this.Owner.TroopList.Count > 0 && target.GetGroundLandingSpots() > 0)
            {
                this.HasPriorityOrder = true;
                this.State = AIState.AssaultPlanet;
                this.OrbitTarget = target;
                this.OrderQueue.Clear();
                lock (this.ActiveWayPoints) 
                this.ActiveWayPoints.Clear();
                ArtificialIntelligence.ShipGoal goal = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.LandTroop, Vector2.Zero, 0f)
                {
                    TargetPlanet = target
                };
                this.OrderQueue.AddLast(goal);
            }
            //else if (this.Owner.BombBays.Count > 0 && target.GetGroundStrength(this.Owner.loyalty) ==0)  //universeScreen.player == this.Owner.loyalty && 
            //{
            //    this.State = AIState.Bombard;
            //    this.OrderBombardTroops(target);
            //}
		}

		public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
		{
			this.Target = null;
			this.hasPriorityTarget = false;
			Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, position));
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
			Vector2.Dot(wantedForward, right);
			if (angleDiff > 0.2f)
			{
				this.Owner.HyperspaceReturn();
			}
			this.OrderQueue.Clear();
			if (ClearOrders)
			{
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
			}
			if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
			{
				this.HasPriorityOrder = true;
			}
			this.State = AIState.MoveTo;
			this.MovePosition = position;
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Enqueue(position);
			}
			this.FinalFacingVector = fVec;
			this.DesiredFacing = desiredFacing;
			lock (this.wayPointLocker)
			{
				for (int i = 0; i < this.ActiveWayPoints.Count; i++)
				{
					Vector2 waypoint = this.ActiveWayPoints.ToArray()[i];
					if (i != 0)
					{
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(to1k);
					}
					else
					{
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToFaceMovePosition, waypoint, 0f));
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(to1k);
					}
					if (i == this.ActiveWayPoints.Count - 1)
					{
						ArtificialIntelligence.ShipGoal finalApproach = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproach, waypoint, desiredFacing)
						{
							SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(finalApproach);
						ArtificialIntelligence.ShipGoal slow = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.StopWithBackThrust, waypoint, 0f)
						{
							SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(slow);
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToDesiredFacing, waypoint, desiredFacing));
					}
				}
			}
		}

		public void OrderMoveDirectlyTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders, float speedLimit)
		{
			this.Target = null;
			this.hasPriorityTarget = false;
			Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, position));
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
			Vector2.Dot(wantedForward, right);
			if (angleDiff > 0.2f)
			{
				this.Owner.HyperspaceReturn();
			}
			this.OrderQueue.Clear();
			if (ClearOrders)
			{
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
			}
			if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
			{
				this.HasPriorityOrder = true;
			}
			this.State = AIState.MoveTo;
			this.MovePosition = position;
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Enqueue(position);
			}
			this.FinalFacingVector = fVec;
			this.DesiredFacing = desiredFacing;
			lock (this.wayPointLocker)
			{
				for (int i = 0; i < this.ActiveWayPoints.Count; i++)
				{
					Vector2 waypoint = this.ActiveWayPoints.ToArray()[i];
					if (i != 0)
					{
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							SpeedLimit = speedLimit
						};
						this.OrderQueue.AddLast(to1k);
					}
					else
					{
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToFaceMovePosition, waypoint, 0f));
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							SpeedLimit = speedLimit
						};
						this.OrderQueue.AddLast(to1k);
					}
					if (i == this.ActiveWayPoints.Count - 1)
					{
						ArtificialIntelligence.ShipGoal finalApproach = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproach, waypoint, desiredFacing)
						{
							SpeedLimit = speedLimit
						};
						this.OrderQueue.AddLast(finalApproach);
						ArtificialIntelligence.ShipGoal slow = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.StopWithBackThrust, waypoint, 0f)
						{
							SpeedLimit = speedLimit
						};
						this.OrderQueue.AddLast(slow);
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToDesiredFacing, waypoint, desiredFacing));
					}
				}
			}
		}

		public void OrderMoveToFleetPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders, float SpeedLimit, Fleet fleet)
		{
			SpeedLimit = this.Owner.speed;
			if (ClearOrders)
			{
				this.OrderQueue.Clear();
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
			}
			this.State = AIState.MoveTo;
			this.MovePosition = position;
			this.FinalFacingVector = fVec;
			this.DesiredFacing = desiredFacing;
			bool inCombat = this.Owner.InCombat;
			this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToFaceMovePosition, this.MovePosition, 0f));
			ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000Fleet, this.MovePosition, desiredFacing)
			{
				SpeedLimit = SpeedLimit,
				fleet = fleet
			};
			this.OrderQueue.AddLast(to1k);
			ArtificialIntelligence.ShipGoal finalApproach = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproachFleet, this.MovePosition, desiredFacing)
			{
				SpeedLimit = SpeedLimit,
				fleet = fleet
			};
			this.OrderQueue.AddLast(finalApproach);
			this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateInlineWithVelocity, Vector2.Zero, 0f));
			ArtificialIntelligence.ShipGoal slow = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.StopWithBackThrust, position, 0f)
			{
				SpeedLimit = this.Owner.speed
			};
			this.OrderQueue.AddLast(slow);
			this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToDesiredFacing, this.MovePosition, desiredFacing));
		}

		public void OrderMoveTowardsPosition( Vector2  position , float desiredFacing, Vector2 fVec, bool ClearOrders, Planet TargetPlanet)
		{
            this.DistanceLast = 0f;
            this.Target = null;
			this.hasPriorityTarget = false;
			Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, position));
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
			Vector2.Dot(wantedForward, right);
			if (angleDiff > 0.2f)
			{
				this.Owner.HyperspaceReturn();
			}
            this.OrderQueue.Clear();
            if (ClearOrders)
			{                
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
			}
			if (ArtificialIntelligence.universeScreen != null && this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
			{
				this.HasPriorityOrder = true;
			}
			this.State = AIState.MoveTo;
			this.MovePosition = position;
           // try
            {
                this.PlotCourseToNew(position, (this.ActiveWayPoints.Count > 0 ? this.ActiveWayPoints.Last<Vector2>() : this.Owner.Center));
            }
         //   catch
            //{
            //    lock (this.wayPointLocker)
            //    {
            //        this.ActiveWayPoints.Clear();
            //    }
            //}
            this.FinalFacingVector = fVec;
			this.DesiredFacing = desiredFacing;

			lock (this.wayPointLocker)
			{
                            Planet p;
            Vector2 waypoint;
                int AWPC = this.ActiveWayPoints.Count;
                for (int i = 0; i < AWPC; i++)
				{
					p =null;
                    waypoint = this.ActiveWayPoints.ToArray()[i];
					if (i != 0)
					{
                        if (AWPC - 1 == i)
                            p = TargetPlanet;

                        ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							TargetPlanet=p,
                            SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(to1k);
					}
					else
					{
						if(AWPC -1 ==i)
                            p = TargetPlanet;
                        this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToFaceMovePosition, waypoint, 0f));
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
                            TargetPlanet = p,
                            SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(to1k);
					}
                    if (i == AWPC - 1)
					{
						ArtificialIntelligence.ShipGoal finalApproach = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproach, waypoint, desiredFacing)
						{
							TargetPlanet=p,
                            SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(finalApproach);
						ArtificialIntelligence.ShipGoal slow = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.StopWithBackThrust, waypoint, 0f)
						{
                            TargetPlanet = TargetPlanet,
                            SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(slow);
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToDesiredFacing, waypoint, desiredFacing));
					}
				}
			}
		}

        #region Unreferenced code
        //public void OrderMoveTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders, float SpeedLimit)
        //{
        //    this.Target = null;
        //    Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, position));
        //    Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
        //    Vector2 right = new Vector2(-forward.Y, forward.X);
        //    float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
        //    Vector2.Dot(wantedForward, right);
        //    if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
        //    {
        //        this.HasPriorityOrder = true;
        //    }
        //    if (angleDiff > 0.2f)
        //    {
        //        this.Owner.HyperspaceReturn();
        //    }
        //    this.hasPriorityTarget = false;
        //    if (ClearOrders)
        //    {
        //        this.OrderQueue.Clear();
        //    }
        //    this.State = AIState.MoveTo;
        //    this.MovePosition = position;
        //    this.PlotCourseToNew(position, this.Owner.Center);
        //    this.FinalFacingVector = fVec;
        //    this.DesiredFacing = desiredFacing;
        //    for (int i = 0; i < this.ActiveWayPoints.Count; i++)
        //    {
        //        Vector2 waypoint = this.ActiveWayPoints.ToArray()[i];
        //        if (i != 0)
        //        {
        //            ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
        //            {
        //                SpeedLimit = SpeedLimit
        //            };
        //            this.OrderQueue.AddLast(to1k);
        //        }
        //        else
        //        {
        //            ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
        //            {
        //                SpeedLimit = SpeedLimit
        //            };
        //            this.OrderQueue.AddLast(to1k);
        //        }
        //        if (i == this.ActiveWayPoints.Count - 1)
        //        {
        //            ArtificialIntelligence.ShipGoal finalApproach = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproach, waypoint, desiredFacing)
        //            {
        //                SpeedLimit = SpeedLimit
        //            };
        //            this.OrderQueue.AddLast(finalApproach);
        //            this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateInlineWithVelocity, Vector2.Zero, 0f));
        //            ArtificialIntelligence.ShipGoal slow = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.StopWithBackThrust, waypoint, 0f)
        //            {
        //                SpeedLimit = SpeedLimit
        //            };
        //            this.OrderQueue.AddLast(slow);
        //            this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToDesiredFacing, waypoint, desiredFacing));
        //        }
        //    }
        //} 
        #endregion

		public void OrderOrbitNearest(bool ClearOrders)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.Target = null;
			this.Intercepting = false;
			this.Owner.HyperspaceReturn();
			if (ClearOrders)
			{
				this.OrderQueue.Clear();
			}
			IOrderedEnumerable<Planet> sortedList = 
				from toOrbit in this.Owner.loyalty.GetPlanets()
				orderby Vector2.Distance(this.Owner.Center, toOrbit.Position)
				select toOrbit;
			if (sortedList.Count<Planet>() > 0)
			{
				Planet planet = sortedList.First<Planet>();
				this.OrbitTarget = planet;
				ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
				{
					TargetPlanet = planet
				};
				this.resupplyTarget = planet;
				this.OrderQueue.AddLast(orbit);
				this.State = AIState.Orbit;
				return;
			}
			IOrderedEnumerable<SolarSystem> systemList = 
				from solarsystem in this.Owner.loyalty.GetOwnedSystems()
				orderby Vector2.Distance(this.Owner.Center, solarsystem.Position)
				select solarsystem;
			if (systemList.Count<SolarSystem>() > 0)
			{
				Planet item = systemList.First<SolarSystem>().PlanetList[0];
				this.OrbitTarget = item;
				ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
				{
					TargetPlanet = item
				};
				this.resupplyTarget = item;
				this.OrderQueue.AddLast(orbit);
				this.State = AIState.Orbit;
			}
		}
        //added by gremlin to run away
        public void OrderFlee(bool ClearOrders)
        {
            lock (this.wayPointLocker)
            {
                this.ActiveWayPoints.Clear();
            }
            this.Target = null;
            this.Intercepting = false;
            this.Owner.HyperspaceReturn();
            if (ClearOrders)
            {
                this.OrderQueue.Clear();
            }
           
            IOrderedEnumerable<SolarSystem> systemList =
                from solarsystem in this.Owner.loyalty.GetOwnedSystems()
                where solarsystem.combatTimer <= 0f && Vector2.Distance(solarsystem.Position, this.Owner.Position) > 200000f
                orderby Vector2.Distance(this.Owner.Center, solarsystem.Position)
                select solarsystem;
            if (systemList.Count<SolarSystem>() > 0)
            {
                Planet item = systemList.First<SolarSystem>().PlanetList[0];
                this.OrbitTarget = item;
                ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
                {
                    TargetPlanet = item
                };
                this.resupplyTarget = item;
                this.OrderQueue.AddLast(orbit);
                this.State = AIState.Flee;
            }
        }

		public void OrderOrbitPlanet(Planet p)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.Target = null;
			this.Intercepting = false;
			this.Owner.HyperspaceReturn();
			this.OrbitTarget = p;
			this.OrderQueue.Clear();
			ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
			{
				TargetPlanet = p
			};
			this.resupplyTarget = p;
			this.OrderQueue.AddLast(orbit);
			this.State = AIState.Orbit;
		}

		public void OrderQueueSpecificTarget(Ship toAttack)
		{
			if (this.TargetQueue.Count == 0 && this.Target != null && this.Target.Active && this.Target != toAttack)
			{
				this.OrderAttackSpecificTarget(this.Target as Ship);
				this.TargetQueue.Add(this.Target as Ship);
			}
			if (this.TargetQueue.Count == 0)
			{
				this.OrderAttackSpecificTarget(toAttack);
				return;
			}
			if (toAttack == null)
			{
				return;
			}
			if (this.Owner.loyalty.GetRelations().ContainsKey(toAttack.loyalty))
			{
				if (!this.Owner.loyalty.GetRelations()[toAttack.loyalty].Treaty_Peace)
				{
					if (this.State == AIState.AttackTarget && this.Target == toAttack)
					{
						return;
					}
					if (this.State == AIState.SystemDefender && this.Target == toAttack)
					{
						return;
					}
                    if (this.Owner.Weapons.Count == 0 || this.Owner.shipData.Role == ShipData.RoleName.troop)
					{
						this.OrderInterceptShip(toAttack);
						return;
					}
					this.Intercepting = true;
					lock (this.wayPointLocker)
					{
						this.ActiveWayPoints.Clear();
					}
					this.State = AIState.AttackTarget;
					this.TargetQueue.Add(toAttack);
					this.hasPriorityTarget = true;
					this.HasPriorityOrder = false;
					return;
				}
				this.OrderInterceptShip(toAttack);
			}
		}

        public void OrderRebase(Planet p, bool ClearOrders)
        {

            lock (this.wayPointLocker)
            {
                this.ActiveWayPoints.Clear();
            }
            if (ClearOrders)
            {
                this.OrderQueue.Clear();
            }
            int troops = this.Owner.loyalty.GetShips()
    .Where(troop => troop.TroopList.Count > 0)
    .Where(troopAI => troopAI.GetAI().OrderQueue
        .Where(goal => goal.TargetPlanet != null && goal.TargetPlanet == p).Count() > 0).Count();

            if (troops >= p.GetGroundLandingSpots())
            {
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
                return;
            }

            this.OrderMoveTowardsPosition(p.Position, 0f, new Vector2(0f, -1f), false,p);
            this.IgnoreCombat = true;
            ArtificialIntelligence.ShipGoal rebase = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Rebase, Vector2.Zero, 0f)
            {
                TargetPlanet = p
            };
            this.OrderQueue.AddLast(rebase);
            this.State = AIState.Rebase;
            this.HasPriorityOrder = true;
        }

		public void OrderRebaseToNearest()
		{
            ////added by gremlin if rebasing dont rebase.
            //if (this.State == AIState.Rebase && this.OrbitTarget.Owner == this.Owner.loyalty)
            //    return;
            lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
            
            IOrderedEnumerable<Planet> sortedList = 
				from planet in this.Owner.loyalty.GetPlanets()
                //added by gremlin if the planet is full of troops dont rebase there. RERC2 I dont think the about looking at incoming troops works.
                where this.Owner.loyalty.GetShips()
    .Where(troop => troop.TroopList.Count > 0 )
    .Where(troopAI => troopAI.GetAI().OrderQueue
        .Where(goal => goal.TargetPlanet != null && goal.TargetPlanet == planet).Count() > 0).Count() <= planet.GetGroundLandingSpots()


                /*where planet.TroopsHere.Count + this.Owner.loyalty.GetShips()
                .Where(troop => troop.Role == ShipData.RoleName.troop 
                    
                    && troop.GetAI().State == AIState.Rebase 
                    && troop.GetAI().OrbitTarget == planet).Count() < planet.TilesList.Sum(space => space.number_allowed_troops)*/
				orderby Vector2.Distance(this.Owner.Center, planet.Position)
				select planet;

           


			if (sortedList.Count<Planet>() <= 0)
			{
				this.State = AIState.AwaitingOrders;
				return;
			}
			Planet p = sortedList.First<Planet>();
			this.OrderMoveTowardsPosition(p.Position, 0f, new Vector2(0f, -1f), false,p);
			this.IgnoreCombat = true;
			ArtificialIntelligence.ShipGoal rebase = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Rebase, Vector2.Zero, 0f)
			{
				TargetPlanet = p
			};

           
            this.OrderQueue.AddLast(rebase);
        
			this.State = AIState.Rebase;
			this.HasPriorityOrder = true;
		}

		public void OrderRefitTo(string toRefit)
		{
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.HasPriorityOrder = true;
			this.IgnoreCombat = true;
           
			this.OrderQueue.Clear();
          
			IOrderedEnumerable<Ship_Game.Planet> sortedList = 
				from planet in this.Owner.loyalty.GetPlanets()
				orderby Vector2.Distance(this.Owner.Center, planet.Position)
				select planet;
			this.OrbitTarget = null;
			foreach (Ship_Game.Planet Planet in sortedList)
			{
				if (!Planet.HasShipyard && !this.Owner.loyalty.isFaction)
				{
					continue;
				}
				this.OrbitTarget = Planet;
				break;
			}
			if (this.OrbitTarget == null)
			{
				this.State = AIState.AwaitingOrders;
				return;
			}
			this.OrderMoveTowardsPosition(this.OrbitTarget.Position, 0f, Vector2.One, true,this.OrbitTarget);
			ArtificialIntelligence.ShipGoal refit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Refit, Vector2.Zero, 0f)
			{
				TargetPlanet = this.OrbitTarget,
				VariableString = toRefit
			};
			this.OrderQueue.AddLast(refit);
			this.State = AIState.Refit;
		}

		public void OrderResupply(Planet toOrbit, bool ClearOrders)
		{
          
            if (ClearOrders)
			{
				this.OrderQueue.Clear();
                this.HadPO = true;
			}
            else
            {
                this.HadPO = false;
            }
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.Target = null;
			this.OrbitTarget = toOrbit;
            this.awaitClosest = toOrbit;
            this.OrderMoveTowardsPosition(toOrbit.Position, 0f, Vector2.One, ClearOrders, toOrbit);
			this.State = AIState.Resupply;
			this.HasPriorityOrder = true;
		}

		//fbedard: Added dont retreat to a near planet in combat, and flee if nowhere to go
        public void OrderResupplyNearest(bool ClearOrders)
		{
            if (this.Owner.Mothership != null && this.Owner.Mothership.Active && (this.Owner.shipData.Role != ShipData.RoleName.supply 
                || this.Owner.Ordinance > 0 || this.Owner.Health / this.Owner.HealthMax < DmgLevel[(int)this.Owner.shipData.ShipCategory]))
			{
				this.OrderReturnToHangar();
				return;
			}
			List<Planet> shipyards = new List<Planet>();
            if(this.Owner.loyalty.isFaction)
            {                
                return;
            }
			foreach (Planet planet in this.Owner.loyalty.GetPlanets())
			{
                if (!planet.HasShipyard || (this.Owner.InCombat && Vector2.Distance(this.Owner.Center, planet.Position) < 15000f))
				{
					continue;
				}
				shipyards.Add(planet);
			}
            IOrderedEnumerable<Planet> sortedList = null;
            if(this.Owner.NeedResupplyTroops)
                sortedList =
                from p in shipyards
                orderby p.TroopsHere.Count > this.Owner.TroopCapacity,
                Vector2.Distance(this.Owner.Center, p.Position)                
                select p;
            else
			sortedList = 
				from p in shipyards
				orderby Vector2.Distance(this.Owner.Center, p.Position)
				select p;
            if (sortedList.Count<Planet>() > 0)
                this.OrderResupply(sortedList.First<Planet>(), ClearOrders);
            else
                this.OrderFlee(true);

		}

		public void OrderReturnToHangar()
		{
			ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.ReturnToHangar, Vector2.Zero, 0f);
            
            this.OrderQueue.Clear();
			this.OrderQueue.AddLast(g);
            
			this.HasPriorityOrder = true;
			this.State = AIState.ReturnToHangar;
		}

		public void OrderScrapShip()
		{
#if SHOWSCRUB
            //System.Diagnostics.Debug.WriteLine(string.Concat(this.Owner.loyalty.PortraitName, " : ", this.Owner.Role)); 
#endif

            if ((this.Owner.shipData.Role <= ShipData.RoleName.station) && this.Owner.ScuttleTimer < 1)
            {
                this.Owner.ScuttleTimer = 1;
                this.State = AIState.Scuttle;
                this.HasPriorityOrder = true;
                this.Owner.QueueTotalRemoval();  //fbedard
                return;
            }
            lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
            this.Owner.loyalty.ForcePoolRemove(this.Owner);

            if (this.Owner.fleet != null)
            {
                this.Owner.fleet.Ships.Remove(this.Owner);
                this.Owner.fleet = null;
            }
            this.HasPriorityOrder = true;
            this.IgnoreCombat = true;
			this.OrderQueue.Clear();
			IOrderedEnumerable<Ship_Game.Planet> sortedList = 
				from planet in this.Owner.loyalty.GetPlanets()
				orderby Vector2.Distance(this.Owner.Center, planet.Position)
				select planet;
			this.OrbitTarget = null;
			foreach (Ship_Game.Planet Planet in sortedList)
			{
				if (!Planet.HasShipyard)
				{
					continue;
				}
				this.OrbitTarget = Planet;
				break;
			}
			if (this.OrbitTarget == null)
			{
				this.State = AIState.AwaitingOrders;
			}
			else
			{
				this.OrderMoveTowardsPosition(this.OrbitTarget.Position, 0f, Vector2.One, true,this.OrbitTarget);
				ArtificialIntelligence.ShipGoal scrap = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Scrap, Vector2.Zero, 0f)
				{
					TargetPlanet = this.OrbitTarget
				};
				this.OrderQueue.AddLast(scrap);
				this.State = AIState.Scrap;
			}
			this.State = AIState.Scrap;
		}

		private void OrderSupplyShip(Ship tosupply, float ord_amt)
		{
			ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.SupplyShip, Vector2.Zero, 0f);
			this.EscortTarget = tosupply;
			g.VariableNumber = ord_amt;
			this.IgnoreCombat = true;
			this.OrderQueue.Clear();
			this.OrderQueue.AddLast(g);
			this.State = AIState.Ferrying;
		}

		public void OrderSystemDefense(SolarSystem system)
		{
            //if (this.State == AIState.Intercept || this.Owner.InCombatTimer > 0)
            //    return;
            //bool inSystem = true;
            //if (this.Owner.BaseCanWarp && Vector2.Distance(system.Position, this.Owner.Position) / this.Owner.velocityMaximum > 11)
            //    inSystem = false;
            //else 
            //    inSystem = this.Owner.GetSystem() == this.SystemToDefend;
            //if (this.SystemToDefend == null)
            //{
            //    this.HasPriorityOrder = false;
            //    this.SystemToDefend = system;
            //    this.OrderQueue.Clear();
            //}
            //else

            ArtificialIntelligence.ShipGoal goal = this.OrderQueue.LastOrDefault();

            if (this.SystemToDefend == null || (this.SystemToDefend != system || this.awaitClosest == null || this.awaitClosest.Owner == null || this.awaitClosest.Owner != this.Owner.loyalty || (this.Owner.GetSystem() != system && goal != null && this.OrderQueue.LastOrDefault().Plan != Plan.DefendSystem)))
			{

#if SHOWSCRUB
                if (this.Target != null && (this.Target as Ship).Name == "Subspace Projector")
                    System.Diagnostics.Debug.WriteLine(string.Concat("Scrubbed", (this.Target as Ship).Name)); 
#endif
                this.SystemToDefend = system;
                this.HasPriorityOrder = false;
				this.SystemToDefend = system;
				this.OrderQueue.Clear();
                this.OrbitTarget = (Planet)null;
				if (this.SystemToDefend.PlanetList.Count > 0)
				{
					List<Planet> Potentials = new List<Planet>();
					foreach (Planet p in this.SystemToDefend.PlanetList)
					{
						if (p.Owner == null || p.Owner != this.Owner.loyalty)
						{
							continue;
						}
						Potentials.Add(p);
					}
                    //if (Potentials.Count == 0)
                    //    foreach (Planet p in this.SystemToDefend.PlanetList)
                    //        if (p.Owner == null)
                    //            Potentials.Add(p);

                    if (Potentials.Count > 0)
                    {
                        int Ran = (int)((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomBetween(0f, (float)Potentials.Count + 0.85f);
                        if (Ran > Potentials.Count - 1)
                        {
                            Ran = Potentials.Count - 1;
                        }
                        this.awaitClosest = Potentials[Ran];
                        this.OrderMoveTowardsPosition(Potentials[Ran].Position, 0f, Vector2.One, true, null);
                        this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DefendSystem, Vector2.Zero, 0f));
                        this.State = AIState.SystemDefender;                   
                    }
                    else
                        this.OrderResupplyNearest(true);
				}
                //this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DefendSystem, Vector2.Zero, 0f));
			}
        
            //this.State = AIState.SystemDefender;                   
		}

		public void OrderThrustTowardsPosition(Vector2 position, float desiredFacing, Vector2 fVec, bool ClearOrders)
		{
			if (ClearOrders)
			{
				this.OrderQueue.Clear();
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
			}
			this.FinalFacingVector = fVec;
			this.DesiredFacing = desiredFacing;
			lock (this.wayPointLocker)
			{
				for (int i = 0; i < this.ActiveWayPoints.Count; i++)
				{
					Vector2 waypoint = this.ActiveWayPoints.ToArray()[i];
					if (i == 0)
					{
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateInlineWithVelocity, Vector2.Zero, 0f));
						ArtificialIntelligence.ShipGoal stop = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Stop, Vector2.Zero, 0f);
						this.OrderQueue.AddLast(stop);
						this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.RotateToFaceMovePosition, waypoint, 0f));
						ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MoveToWithin1000, waypoint, desiredFacing)
						{
							SpeedLimit = this.Owner.speed
						};
						this.OrderQueue.AddLast(to1k);
					}
				}
			}
		}

		public void OrderToOrbit(Planet toOrbit, bool ClearOrders)
		{
			if (ClearOrders)
			{
           
                this.OrderQueue.Clear();
              
			}
			this.HasPriorityOrder = true;
			lock (this.wayPointLocker)
			{
				this.ActiveWayPoints.Clear();
			}
			this.State = AIState.Orbit;
			this.OrbitTarget = toOrbit;
            if (this.Owner.shipData.ShipCategory == ShipData.Category.Civilian)  //fbedard: civilian ship will use projectors
                this.OrderMoveTowardsPosition(toOrbit.Position, 0f, new Vector2(0f, -1f), false, toOrbit);
			ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
			{
				TargetPlanet = toOrbit
			};
            
			this.OrderQueue.AddLast(orbit);
            
		}
        public float TimeToTarget(Planet target)
        {
            float test = 0;
            test = Vector2.Distance(target.Position, this.Owner.Center) / this.Owner.GetmaxFTLSpeed;
            return test;
        }
        //added by: Gremalin. returns roughly the number of turns to a target planet restricting to targets that can use the freighter. 
        private float TradeSort(Ship ship, Planet PlanetCheck, string ResourceType, float cargoCount,bool Delivery)
        {
            /*here I am trying to predict the planets need versus the ships speed.
             * I am returning a weighted value that is based on this but primarily the returned value is the time it takes the freighter to get to the target in a straight line
             * 
             * 
             */
            //cargoCount = cargoCount > PlanetCheck.MAX_STORAGE ? PlanetCheck.MAX_STORAGE : cargoCount;
            float resourceRecharge =0;
            float resourceAmount =0;
            if (ResourceType == "Food")
            {
                resourceRecharge = PlanetCheck.NetFoodPerTurn;
                resourceAmount = PlanetCheck.FoodHere;
            }
            else if(ResourceType == "Production")
            {
                resourceRecharge =  PlanetCheck.NetProductionPerTurn;
                resourceAmount = PlanetCheck.ProductionHere;
            }
            float timeTotarget = ship.GetAI().TimeToTarget(PlanetCheck);
            float Effeciency =  resourceRecharge * timeTotarget;
            
            // return PlanetCheck.MAX_STORAGE / (PlanetCheck.MAX_STORAGE -(Effeciency + resourceAmount));

            if (Delivery)
            {
                // return Effeciency;// * ((PlanetCheck.MAX_STORAGE + cargoCount) / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency =  (PlanetCheck.MAX_STORAGE - cargoCount) / (cargoCount + Effeciency + resourceAmount) ;
                //return timeTotarget * Effeciency;
                bool badCargo = ( Effeciency + resourceAmount) > PlanetCheck.MAX_STORAGE ;
                //bool badCargo = (cargoCount + Effeciency + resourceAmount) > PlanetCheck.MAX_STORAGE - cargoCount * .5f; //cargoCount + Effeciency < 0 ||
                if (!badCargo)
                    return timeTotarget * (badCargo ? PlanetCheck.MAX_STORAGE / (Effeciency + resourceAmount) : 1);// (float)Math.Ceiling((double)timeTotarget);                
            }
            else
            {
                //return Effeciency * (PlanetCheck.MAX_STORAGE / ((PlanetCheck.MAX_STORAGE - resourceAmount + 1)));
                // Effeciency = (ship.CargoSpace_Max) / (PlanetCheck.MAX_STORAGE);
                //return timeTotarget * Effeciency;
                Effeciency = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpace_Max ? resourceAmount + Effeciency < ship.CargoSpace_Max * .5f ? (ship.CargoSpace_Max*.5f) / (resourceAmount + Effeciency) :1:1;
                //bool BadSupply = PlanetCheck.MAX_STORAGE * .5f < ship.CargoSpace_Max && PlanetCheck.FoodHere + Effeciency < ship.CargoSpace_Max * .5f;
                //if (!BadSupply)
                    return timeTotarget * Effeciency;// (float)Math.Ceiling((double)timeTotarget);
            }
            return timeTotarget + universeScreen.Size.X;
        }
        //added by fbedard OrderTrade
        public void OrderTrade(float elapsedTime)
        {            
            //trade timer is sent but uses arbitrary timer just to delay the routine.
            this.Owner.TradeTimer -= elapsedTime;
            if (this.Owner.TradeTimer > 0f)
                return;

            lock (this.wayPointLocker)
                this.ActiveWayPoints.Clear();
            
            this.OrderQueue.Clear();
            

            if(this.start != null && this.end != null)  //resume trading
            {
                this.Owner.TradeTimer = 5f;
                if (this.Owner.GetCargo()["Food"] > 0f || this.Owner.GetCargo()["Production"] > 0f)
                {
                    this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                    
                    this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
                   
                    this.State = AIState.SystemTrader;
                    return;
                }
                else
                {
                    this.OrderMoveTowardsPosition(this.start.Position, 0f, new Vector2(0f, -1f), true, this.start);
                  
                    this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.PickupGoods, Vector2.Zero, 0f));
                    
                    this.State = AIState.SystemTrader;
                    return;
                }
            }
            Planet potential = null;//<-unused
            List<Planet> planets = new List<Planet>();
            IOrderedEnumerable<Planet> sortPlanets;
            bool flag;
            List<Planet> secondaryPlanets = new List<Planet>();
            //added by gremlin if fleeing keep fleeing
            if (this.Owner.CargoSpace_Max == 0 || this.State == AIState.Flee || this.Owner.isConstructor || this.Owner.isColonyShip)
                return;

            //try
            {
                //if system all systems in combat... OMG no trade.
                if (this.Owner.loyalty.GetOwnedSystems().Where(combat => combat.combatTimer <= 0).Count() == 0)
                {
                    this.Owner.TradeTimer = 5f;
                    return;
                }

                if (this.Owner.loyalty.data.Traits.Cybernetic >0)
                    this.Owner.TradingFood = false;

                bool FoodFirst = true;
                if ((this.Owner.GetCargo()["Production"] > 0f || !this.Owner.TradingFood || RandomMath.RandomBetween(0f, 1f) < 0.5f) && this.Owner.TradingProd && this.Owner.GetCargo()["Food"] == 0f)
                    FoodFirst = false;
                //float GoodMult = RandomMath.RandomBetween(0f, 25f);

                //if already loaded, give any start planet: <-- this doesnt look good but doesnt appear to be a problem
                //if (this.start == null && (this.Owner.GetCargo()["Food"] > 0f || this.Owner.GetCargo()["Production"] > 0f))
                //{
                //    this.start = this.Owner.loyalty.GetPlanets().FirstOrDefault();
                //}
                //FoodFirst
                #region Deliver Food FIRST (return if already loaded)
                if (this.end == null && FoodFirst  && ( this.Owner.GetCargo()["Food"] > 0f))
                {
                    //planets.Clear();

                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
                    for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count(); i++)
                        if (this.Owner.loyalty.GetPlanets()[i].ParentSystem.combatTimer <= 0)
                        {
                            Planet PlanetCheck = this.Owner.loyalty.GetPlanets()[i];
                            if (PlanetCheck == null)
                                continue;

                            if (PlanetCheck.fs == Planet.GoodState.IMPORT)
                            {
                                //if (PlanetCheck.FoodHere / PlanetCheck.MAX_STORAGE  <.1f && PlanetCheck.NetFoodPerTurn <0 ) //(PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                                {
                                    if (this.Owner.AreaOfOperation.Count > 0)
                                    {
                                        foreach (Rectangle areaOfOperation in this.Owner.AreaOfOperation)
                                            if (HelperFunctions.CheckIntersection(areaOfOperation, PlanetCheck.Position))
                                            {
                                                planets.Add(PlanetCheck);
                                                break;
                                            }
                                    }
                                    else
                                        planets.Add(PlanetCheck);
                                }

                            }
                            else if (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere > 0)
                            {
                                secondaryPlanets.Add(PlanetCheck);
                            }

                        }
                   if(planets.Count ==0)
                    {
                        planets.AddRange(secondaryPlanets);
                    }
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
                    if (planets.Count > 0)
                    {
                       // if (this.Owner.GetCargo()["Food"] > 0f)
                            //sortPlanets = planets.OrderBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));
                            sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Used, true);
                            }
                      );
                      //  else
                      //      //    sortPlanets = planets.OrderBy(dest => (dest.FoodHere + (dest.NetFoodPerTurn - dest.consumption) * GoodMult));
                      //      sortPlanets = planets.OrderBy(PlanetCheck =>
                      //      {
                      //          return TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Max, true);
                      //      }
                      //);
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;                            
                            bool faster = true ;
                            float mySpeed = this.TradeSort(this.Owner, p, "Food", this.Owner.CargoSpace_Max, true); 
                            cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                            cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                            cargoSpaceMax = cargoSpaceMax < 0 ? 0 : cargoSpaceMax;
                            //Planet with negative food production need more food:
                            //cargoSpaceMax = (cargoSpaceMax - (p.NetFoodPerTurn * 5f)) / 2f;  //reduced cargoSpacemax on first try!

                            this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                            for (int k = 0; k < this.Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = this.Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != this.Owner && !s.isConstructor)
                                {
                                    if (s.GetAI().State == AIState.SystemTrader && s.GetAI().end == p && s.GetAI().FoodOrProd == "Food" && s.CargoSpace_Used >0
                                        )
                                    {

                                        float currenTrade = this.TradeSort(s, p, "Food", s.CargoSpace_Max, true);                                        
                                        if (currenTrade < mySpeed)
                                            faster = false;
                                        if (currenTrade !=0 )
                                        {
                                            flag = true;
                                            break;
                                        }
                                        float efficiency = currenTrade - mySpeed;
                                        if(mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                        {
                                            continue;
                                        }
                                        if(p.NetFoodPerTurn <=0)
                                        efficiency = s.CargoSpace_Max - efficiency * p.NetFoodPerTurn;                                        
                                        else
                                            efficiency = s.CargoSpace_Max - efficiency * p.NetFoodPerTurn;                                        
                                        if (efficiency > 0)
                                        {
                                            if (efficiency > s.CargoSpace_Max)
                                                efficiency = s.CargoSpace_Max;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //ca

                                    }
                                    if (cargoSpaceMax <= 0f)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                            if (!flag )
                            {
                                this.end = p;
                                break;
                            }
                            if (faster)
                                potential = p;
                        }
                        if (this.end != null)
                        {
                            this.FoodOrProd = "Food";
                            if (this.Owner.GetCargo()["Food"] > 0f)
                            {
                                this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
                                this.State = AIState.SystemTrader;
                                return;
                            }
                        }
                    }
                }
                #endregion

                #region deliver Production (return if already loaded)
                if (this.end == null && (this.Owner.TradingProd || this.Owner.GetCargo()["Production"] > 0f))
                {
                    planets.Clear();
                    secondaryPlanets.Clear();
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
                    for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count(); i++)
                    if (this.Owner.loyalty.GetPlanets()[i].ParentSystem.combatTimer <= 0)
                    {
                        Planet PlanetCheck = this.Owner.loyalty.GetPlanets()[i];
                        if (PlanetCheck == null)
                        continue;

                        if (PlanetCheck.ps == Planet.GoodState.IMPORT)// && (PlanetCheck.ProductionHere / PlanetCheck.MAX_STORAGE < .9f || PlanetCheck.ProductionHere <1))
                           // && (planets.Count==0 || (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere) >= this.Owner.CargoSpace_Max))
                        {
                            if (this.Owner.AreaOfOperation.Count > 0)
                            {
                                foreach (Rectangle areaOfOperation in this.Owner.AreaOfOperation)
                                    if (HelperFunctions.CheckIntersection(areaOfOperation, PlanetCheck.Position))
                                    {
                                        planets.Add(PlanetCheck);
                                        break;
                                    }
                            }
                            else
                                planets.Add(PlanetCheck);
                        }
                            else if (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere > 0)
                            {
                                secondaryPlanets.Add(PlanetCheck);
                            }

                        }                    
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
                    if (planets.Count == 0)
                        planets.AddRange(secondaryPlanets);
                    if (planets.Count > 0)
                    {
                        if (this.Owner.GetCargo()["Production"] > 0f)
                            //sortPlanets = planets.OrderBy(PlanetCheck=> (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));
                            sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(this.Owner, PlanetCheck, "Production", this.Owner.CargoSpace_Used, true);
                                
                            }
                   );//.ThenByDescending(f => f.ProductionHere / f.MAX_STORAGE);
                        else
                            //sortPlanets = planets.OrderBy(PlanetCheck=> (PlanetCheck.MAX_STORAGE - PlanetCheck.ProductionHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => (dest.ProductionHere));
                            sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return TradeSort(this.Owner, PlanetCheck, "Production", this.Owner.CargoSpace_Max, true);
                            }
                   );//.ThenByDescending(f => f.ProductionHere / f.MAX_STORAGE);
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.MAX_STORAGE - p.ProductionHere;
                            bool faster = true;
                            float thisTradeStr = this.TradeSort(this.Owner, p, "Production", this.Owner.CargoSpace_Max, true);
                            if (thisTradeStr >= universeScreen.Size.X && p.ProductionHere >= 0)
                                continue;
                            this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                            for (int k = 0; k < this.Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = this.Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != this.Owner && !s.isConstructor)
                                {
                                    if (s.GetAI().State == AIState.SystemTrader && s.GetAI().end == p && s.GetAI().FoodOrProd == "Prod")
                                    {

                                        float currenTrade = this.TradeSort(s, p, "Production", s.CargoSpace_Max, true);
                                        if (currenTrade < thisTradeStr)
                                            faster = false;
                                        if (currenTrade > UniverseData.UniverseWidth && !faster)
                                        {
                                            flag = true;
                                            break;
                                        }
                                        cargoSpaceMax = cargoSpaceMax - s.CargoSpace_Max;
                                    }

                                    if (cargoSpaceMax <= 0f)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                            if (!flag)
                            {
                                this.end = p;
                                break;
                            }
                            if (faster)
                                potential = p;
                        }
                        if (this.end != null)
                        {
                            this.FoodOrProd = "Prod";
                            if (this.Owner.GetCargo()["Production"] > 0f)
                            {
                                this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
                                this.State = AIState.SystemTrader;
                                return;
                            }
                        }
                    }
                }
                #endregion

                #region Deliver Food LAST (return if already loaded)
                if (this.end == null && (this.Owner.TradingFood || this.Owner.GetCargo()["Food"] > 0f) && this.Owner.GetCargo()["Production"] == 0f)
                {
                    planets.Clear();
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
                    for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count(); i++)
                    if (this.Owner.loyalty.GetPlanets()[i].ParentSystem.combatTimer <= 0)
                    {
                        Planet PlanetCheck = this.Owner.loyalty.GetPlanets()[i];
                        if (PlanetCheck != null && PlanetCheck.fs == Planet.GoodState.IMPORT ) //&& (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                        {
                            if (this.Owner.AreaOfOperation.Count > 0)
                            {
                                foreach (Rectangle areaOfOperation in this.Owner.AreaOfOperation)
                                    if (HelperFunctions.CheckIntersection(areaOfOperation, PlanetCheck.Position))
                                    {
                                        planets.Add(PlanetCheck);
                                        break;
                                    }
                            }
                            else
                                planets.Add(PlanetCheck);
                        }
                    }
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
                    if (planets.Count > 0)
                    {
                        if (this.Owner.GetCargo()["Food"] > 0f)
                          //  sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                        {
                            return TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Used, true);   
                        }
                            );//.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                        else
                            //sortPlanets = planets.OrderBy(PlanetCheck => (PlanetCheck.MAX_STORAGE - PlanetCheck.FoodHere) >= this.Owner.CargoSpace_Max)
                            //    .ThenBy(dest => (dest.FoodHere + (dest.NetFoodPerTurn - dest.consumption) * GoodMult));

                        sortPlanets = planets.OrderBy(PlanetCheck =>
                        {
                            return TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Max, true);   
                        }
                            );//.ThenByDescending(f => f.FoodHere / f.MAX_STORAGE);
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.MAX_STORAGE - p.FoodHere;
                            bool faster = true;
                            float mySpeed = this.TradeSort(this.Owner, p, "Food", this.Owner.CargoSpace_Max, true);
                            if (mySpeed >= universeScreen.Size.X)
                                continue;
                            cargoSpaceMax += p.NetFoodPerTurn * mySpeed;
                            cargoSpaceMax = cargoSpaceMax > p.MAX_STORAGE ? p.MAX_STORAGE : cargoSpaceMax;
                            cargoSpaceMax = cargoSpaceMax < 0 ? 0 : cargoSpaceMax;
                            this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                            for (int k = 0; k < this.Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = this.Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != this.Owner && !s.isConstructor)
                                {
                                    if (s.GetAI().State == AIState.SystemTrader && s.GetAI().end == p && s.GetAI().FoodOrProd == "Food")
                                    {

                                        float currenTrade = this.TradeSort(s, p, "Food", s.CargoSpace_Max, true);
                                        if (currenTrade < mySpeed)
                                            faster = false;
                                        if (currenTrade > UniverseData.UniverseWidth && !faster)
                                            continue;
                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        if (mySpeed * p.NetFoodPerTurn < p.FoodHere && faster)
                                        {
                                            continue;
                                        }
                                        if (p.NetFoodPerTurn == 0)
                                            efficiency = s.CargoSpace_Max + efficiency * p.NetFoodPerTurn;
                                        else
                                            if (p.NetFoodPerTurn < 0)
                                                efficiency = s.CargoSpace_Max + efficiency * p.NetFoodPerTurn;
                                            else
                                            efficiency = s.CargoSpace_Max - efficiency * p.NetFoodPerTurn;
                                        if (efficiency > 0)
                                        {
                                            if (efficiency > s.CargoSpace_Max)
                                                efficiency = s.CargoSpace_Max;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //ca

                                    }
                                    if (cargoSpaceMax <= 0f)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                            if (!flag)
                            {
                                this.end = p;
                                break;
                            }
                        }
                        if (this.end != null)
                        {
                            this.FoodOrProd = "Food";
                            if (this.Owner.GetCargo()["Food"] > 0f)
                            {
                                this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
                                this.State = AIState.SystemTrader;
                                return;
                            }
                        }
                    }
                }
                #endregion
                
                #region Get Food
                if (this.start == null && this.end != null) // && this.FoodOrProd == "Food")
                {
                    planets.Clear();
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
                    for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count(); i++)
                    if (this.Owner.loyalty.GetPlanets()[i].ParentSystem.combatTimer <= 0)
                    {
                        Planet PlanetCheck = this.Owner.loyalty.GetPlanets()[i];
                        if (PlanetCheck == null)
                        continue;

                        float distanceWeight = this.TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Max, false);
                            //PlanetCheck.ExportFSWeight += this.Owner.CargoSpace_Max / (PlanetCheck.ProductionHere + 1) + distanceWeight;
                            //distanceWeight -= 100;
                            //distanceWeight = distanceWeight < 0 && distanceWeight < PlanetCheck.ExportFSWeight ? distanceWeight : 0;
                            PlanetCheck.ExportFSWeight = distanceWeight < PlanetCheck.ExportFSWeight ? distanceWeight : PlanetCheck.ExportFSWeight;   
                        if( PlanetCheck.fs == Planet.GoodState.EXPORT )
                            //&& (planets.Count==0 || PlanetCheck.FoodHere >= this.Owner.CargoSpace_Max))
                        {                            
                            if (this.Owner.AreaOfOperation.Count > 0)
                            {
                                foreach (Rectangle areaOfOperation in this.Owner.AreaOfOperation)
                                    if (HelperFunctions.CheckIntersection(areaOfOperation, PlanetCheck.Position))
                                    {
                                        planets.Add(PlanetCheck);
                                        break;
                                    }
                            }
                            else
                                planets.Add(PlanetCheck);
                        }
                    }
                    float weight = 0;
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
                    if (planets.Count > 0)
                    {
                        sortPlanets = planets.OrderBy(PlanetCheck =>
                            {
                                return this.TradeSort(this.Owner, PlanetCheck, "Food", this.Owner.CargoSpace_Max, false);
                                    //+ this.TradeSort(this.Owner, this.end, "Food", this.Owner.CargoSpace_Max)
                                    ;
                                //weight += this.Owner.CargoSpace_Max / (PlanetCheck.FoodHere + 1);
                                //weight += Vector2.Distance(PlanetCheck.Position, this.Owner.Position) / this.Owner.GetmaxFTLSpeed;
                                //return weight;
                            }
                            );
                        foreach (Planet p in sortPlanets)
                        {
                            float cargoSpaceMax = p.FoodHere; 
                            flag = false;
                            float mySpeed = this.TradeSort(this.Owner, p, "Food", this.Owner.CargoSpace_Max, false);                            
                            //cargoSpaceMax = cargoSpaceMax + p.NetFoodPerTurn * mySpeed;
                            this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                            for (int k = 0; k < this.Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = this.Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != this.Owner && !s.isConstructor)
                                {
                                    ArtificialIntelligence.ShipGoal plan =null;

                                        
                                        
                                       plan = s.GetAI().OrderQueue.LastOrDefault<ArtificialIntelligence.ShipGoal>();
                                       
     

                                    if (plan != null && s.GetAI().State == AIState.SystemTrader && s.GetAI().start == p && plan.Plan == ArtificialIntelligence.Plan.PickupGoods && s.GetAI().FoodOrProd == "Food")
                                    {

                                        float currenTrade = this.TradeSort(s, p, "Food", s.CargoSpace_Max, false);
                                        if (currenTrade > 1000)
                                            continue;

                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        efficiency = s.CargoSpace_Max - efficiency * p.NetFoodPerTurn;
                                        if (efficiency > 0)
                                        {
                                            if (efficiency > s.CargoSpace_Max)
                                                efficiency = s.CargoSpace_Max;
                                            cargoSpaceMax = cargoSpaceMax - efficiency;
                                        }
                                        //cargoSpaceMax = cargoSpaceMax - s.CargoSpace_Max;
                                    }
                                    
                                    if (cargoSpaceMax <=0+p.MAX_STORAGE*.1f)// < this.Owner.CargoSpace_Max)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                            if (!flag)
                            {
                                this.start = p;
                                //this.Owner.TradingFood = true;
                                //this.Owner.TradingProd = false;
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region Get Production
                if (this.start == null && this.end != null) // && this.FoodOrProd == "Prod")
                {
                    planets.Clear();
                    this.Owner.loyalty.GetPlanets().thisLock.EnterReadLock();
                    for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count(); i++)
                        if (this.Owner.loyalty.GetPlanets()[i].ParentSystem.combatTimer <= 0)
                        {
                            Planet PlanetCheck = this.Owner.loyalty.GetPlanets()[i];
                            if (PlanetCheck == null)
                                continue;
                            float distanceWeight =this.TradeSort(this.Owner, PlanetCheck, "Production", this.Owner.CargoSpace_Max, false);
                           // distanceWeight -= 100;
                            //distanceWeight = distanceWeight < PlanetCheck.ExportPSWeight ?//distanceWeight < 0  ? distanceWeight : 0; //
                            PlanetCheck.ExportPSWeight = distanceWeight < PlanetCheck.ExportPSWeight ? distanceWeight : PlanetCheck.ExportPSWeight;
                            //PlanetCheck.ExportFSWeight += this.Owner.CargoSpace_Max / (PlanetCheck.FoodHere + 1) +distanceWeight;                            
                            
                            if (PlanetCheck != null && PlanetCheck.ps == Planet.GoodState.EXPORT)
                            //&& (planets.Count==0|| PlanetCheck.ProductionHere >= this.Owner.CargoSpace_Max))
                            {
                                if (this.Owner.AreaOfOperation.Count > 0)
                                {
                                    foreach (Rectangle areaOfOperation in this.Owner.AreaOfOperation)
                                        if (HelperFunctions.CheckIntersection(areaOfOperation, PlanetCheck.Position))
                                        {
                                            planets.Add(PlanetCheck);
                                            break;
                                        }
                                }
                                else
                                    planets.Add(PlanetCheck);
                            }
                        }
                    this.Owner.loyalty.GetPlanets().thisLock.ExitReadLock();
                    float weight = 0;
                    if (planets.Count > 0)
                    {
                        sortPlanets = planets.OrderBy(PlanetCheck => {//(PlanetCheck.ProductionHere > this.Owner.CargoSpace_Max))
                                //.ThenBy(dest => Vector2.Distance(this.Owner.Position, dest.Position));

                            return this.TradeSort(this.Owner, PlanetCheck, "Production", this.Owner.CargoSpace_Max, false);
                                  // + this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);
                            
                        });
                        foreach (Planet p in sortPlanets)
                        {
                            flag = false;
                            float cargoSpaceMax = p.ProductionHere;
                            
                            float mySpeed = this.TradeSort(this.Owner, p, "Production", this.Owner.CargoSpace_Max, false);
                            //cargoSpaceMax = cargoSpaceMax + p.NetProductionPerTurn * mySpeed;

                            //+ this.TradeSort(this.Owner, this.end, "Production", this.Owner.CargoSpace_Max);
                            
                                ArtificialIntelligence.ShipGoal plan;
                            this.Owner.loyalty.GetShips().thisLock.EnterReadLock();
                            for (int k = 0; k < this.Owner.loyalty.GetShips().Count; k++)
                            {
                                Ship s = this.Owner.loyalty.GetShips()[k];
                                if (s != null && (s.shipData.Role == ShipData.RoleName.freighter || s.shipData.ShipCategory == ShipData.Category.Civilian) && s != this.Owner && !s.isConstructor)
                                {
                                    plan = null;
                                                                      
                                    try
                                    {
                                        
                                        plan = s.GetAI().OrderQueue.LastOrDefault<ArtificialIntelligence.ShipGoal>();
                                        
                                    }
                                    catch
                                    {
                                        System.Diagnostics.Debug.WriteLine("Order Trade Orderqueue fail");
                                    }
                                    if (plan != null && s.GetAI().State == AIState.SystemTrader && s.GetAI().start == p && plan.Plan == ArtificialIntelligence.Plan.PickupGoods && s.GetAI().FoodOrProd == "Prod")
                                    {

                                        float currenTrade = this.TradeSort(s, p, "Production", s.CargoSpace_Max, false);      
                                        if (currenTrade > 1000)
                                            continue;

                                        float efficiency = Math.Abs(currenTrade - mySpeed);
                                        efficiency = s.CargoSpace_Max - efficiency * p.NetProductionPerTurn;
                                        if(efficiency >0)
                                        cargoSpaceMax = cargoSpaceMax - efficiency;
                                    }
                                    
                                    if (cargoSpaceMax <= 0 + p.MAX_STORAGE * .1f) // this.Owner.CargoSpace_Max)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                            this.Owner.loyalty.GetShips().thisLock.ExitReadLock();
                            if (!flag)
                            {
                                this.start = p;
                                //this.Owner.TradingFood = false;
                                //this.Owner.TradingProd = true;
                                break;
                            }
                        }
                    }
                }
                #endregion

                if (this.start != null && this.end != null)
                {
                    //if (this.Owner.CargoSpace_Used == 00 && this.start.Population / this.start.MaxPopulation < 0.2 && this.end.Population > 2000f && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                    //    this.PickupAnyPassengers();
                    //if (this.Owner.CargoSpace_Used == 00 && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                    //    this.PickupAnyGoods();
                    this.OrderMoveTowardsPosition(this.start.Position + (RandomMath.RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true, this.start);
                    
                    this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.PickupGoods, Vector2.Zero, 0f));
                   
                }
                else
                {                    
                    this.awaitClosest = this.start ?? this.end;
                    this.start = null;
                    this.end = null;
                    if(this.Owner.CargoSpace_Used >0)
                    {
                        this.Owner.CargoClear();
                    }
                }
                this.State = AIState.SystemTrader;
                this.Owner.TradeTimer = 5f;
                if (string.IsNullOrEmpty(this.FoodOrProd))
                    if (this.Owner.TradingFood)
                        this.FoodOrProd = "Food";
                    else
                        this.FoodOrProd = "Prod";
            }
            //catch { }
        }

		public void OrderTradeFromSave(bool hasCargo, Guid startGUID, Guid endGUID)
		{
            
            if (this.Owner.CargoSpace_Max == 0 || this.State == AIState.Flee)
            {
                return;
            }
            if (this.Owner.loyalty.GetOwnedSystems().Where(combat => combat.combatTimer < 1).Count() == 0)
                return;
#if DEBUG2
            this.end = null;
            this.start = null;
            return;
#endif
            /*
            if ((this.end != null && this.end.ParentSystem.CombatInSystem)
                || (this.start != null && this.start.ParentSystem.CombatInSystem))
            {
                this.start = null;
                this.end = null;
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
            }
            */
            if (this.start == null && this.end == null)
			{
				foreach (Planet p in this.Owner.loyalty.GetPlanets())
				{
					if (p.guid == startGUID)
					{
						this.start = p;
					}
					if (p.guid != endGUID)
					{
						continue;
					}
					this.end = p;
				}
			}
			if (!hasCargo && this.start != null)
			{
				this.OrderMoveTowardsPosition(this.start.Position + (RandomMath.RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true,this.start);
              
                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.PickupGoods, Vector2.Zero, 0f));
        
				this.State = AIState.SystemTrader;
			}
			if (!hasCargo || this.end == null)
			{
				if (!hasCargo && (this.start == null || this.end == null))
				{
                    this.OrderTrade(5f);
				}
				return;
			}
			this.OrderMoveTowardsPosition(this.end.Position + (RandomMath.RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true,this.end);
          
            this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
           
			this.State = AIState.SystemTrader;
		}


        public void OrderTransportPassengers(float elapsedTime)
        {
            this.Owner.TradeTimer -= elapsedTime;
            if (this.Owner.TradeTimer > 0f)
                return;

            float closestD;
            float Distance;

            if (this.Owner.CargoSpace_Max == 0 || this.State == AIState.Flee || this.Owner.isConstructor)
            {
                return;
            }

            List<SolarSystem> OwnedSystems = new List<SolarSystem>(this.Owner.loyalty.GetOwnedSystems());
            if (OwnedSystems.Where(combat => combat.combatTimer < 1).Count() == 0)
            {
                this.Owner.TradeTimer = 5f;
                return;
            }
            /*
            if ((this.end != null && this.end.ParentSystem.CombatInSystem)
                || (this.start != null && this.start.ParentSystem.CombatInSystem))
            {
                this.start = null;
                this.end = null;
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
                this.Owner.TradeTimer = 5f;
                return;
            }
            */
            if (!this.Owner.GetCargo().ContainsKey("Colonists_1000"))
            {
                this.Owner.GetCargo().Add("Colonists_1000", 0f);
            }
            List<Planet> SafePlanets = new List<Planet>(this.Owner.loyalty.GetPlanets().Where(combat => combat.ParentSystem.combatTimer <= 0));
            //SafePlanets = SafePlanets.Where(combat => combat.ParentSystem.combatTimer <= 0).ToList();

            List<Planet> Possible = new List<Planet>();

            // fbedard: Where to drop nearest Population
            #region Already loaded
            if (this.Owner.GetCargo()["Colonists_1000"] > 0f)
            {
                foreach (Planet p in SafePlanets)
                {
                    if (p == this.start)
                    {
                        continue;
                    }
                    float f = p.Owner.data.Traits.Cybernetic > 0 ? p.MineralRichness : p.Fertility;
                    if (p.Population / p.MaxPopulation >= 0.5f || p.MaxPopulation <= 2000f || f < 1)
                    {
                        continue;
                    }
                    if (this.Owner.AreaOfOperation.Count <= 0)
                    {


                        Possible.Add(p);
                    }
                    else
                    {
                        foreach (Rectangle AO in this.Owner.AreaOfOperation)
                        {
                            if (!HelperFunctions.CheckIntersection(AO, p.Position))
                            {
                                continue;
                            }
                            Possible.Add(p);
                        }
                    }
                }

                closestD = 999999999f;
                this.end = null;

                this.OrderQueue.Clear();

                foreach (Planet p in Possible)
                {
                    Distance = Vector2.Distance(this.Owner.Center, p.Position);
                    if (Distance >= closestD)
                    {
                        continue;
                    }
                    closestD = Distance;
                    this.end = p;
                }
                if (this.end != null)
                {
                    this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                    this.State = AIState.PassengerTransport;
                    this.FoodOrProd = "Pass";
                    this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropoffPassengers, Vector2.Zero, 0f));
                }
                return;
            } 
            #endregion

            //fbedard: Where to load nearest Population
            this.start = null;
         
            this.OrderQueue.Clear();
       
            Possible = new List<Planet>();
            foreach (Planet p in SafePlanets)
            {
                
                if (p.MaxPopulation < 1000) //p.Population / p.MaxPopulation <.5f || 
                {                
                    continue;
                }
                
                if (this.Owner.AreaOfOperation.Count <= 0)
                {
                  
                    Possible.Add(p);
                }
                else
                {
                    foreach (Rectangle AO in this.Owner.AreaOfOperation)
                    {
                        if (!HelperFunctions.CheckIntersection(AO, p.Position))
                        {
                            continue;
                        }
                        Possible.Add(p);
                    }
                }
            }
            closestD = 999999999f;
            bool priority =false;
            foreach (Planet p in Possible)
            {
                float f = p.Owner.data.Traits.Cybernetic > 0 ? p.NetProductionPerTurn : p.NetFoodPerTurn;
                Distance = Vector2.Distance(this.Owner.Center, p.Position);
                bool pri2 = f < 0 && p.Population > 1000;
                if (!priority)
                {
                    if (Distance >= closestD)
                    {
                        
                        {
                            continue;
                        }
                        
                    }
                    if(!pri2 && p.Population/p.MaxPopulation <.5f)
                    {
                        continue;
                    }
                }
                else
                    if (!pri2 || Distance >= closestD)
                    {
                        continue;
                    }
                closestD = Distance;
                priority = pri2;
                this.start = p;
            }

            // fbedard: Where to drop nearest Population
            this.end = null;
            Possible = new List<Planet>();
            foreach (Planet p in SafePlanets)
            {                
                if (p == this.start)
                {
                    continue;
                }
                float f = p.Owner.data.Traits.Cybernetic > 0 ? p.MineralRichness : p.Fertility;
                if (p.Population / p.MaxPopulation >= 0.5f || p.MaxPopulation <= 2000f || f < 1)
                {
                    continue;
                }
                if (this.Owner.AreaOfOperation.Count <= 0)
                {

                    
                    Possible.Add(p);
                }
                else
                {
                    foreach (Rectangle AO in this.Owner.AreaOfOperation)
                    {
                        if (!HelperFunctions.CheckIntersection(AO, p.Position) )
                        {
                            continue;
                        }
                        Possible.Add(p);
                    }
                }
            }

            closestD = 999999999f;
            foreach (Planet p in Possible)
            {
                Distance = Vector2.Distance(this.Owner.Center, p.Position);
                if (Distance / this.Owner.GetmaxFTLSpeed>= closestD )
                {
                    continue;
                }
                closestD = Distance;
                this.end = p;
            }

            if (this.start != null && this.end != null)
            {
                //if (this.Owner.CargoSpace_Used == 00 && Vector2.Distance(this.Owner.Center, this.end.Position) < 500f)  //fbedard: dont make empty run !
                //    this.PickupAnyGoods();
                this.OrderMoveTowardsPosition(this.start.Position + (RandomMath.RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true, this.start);
                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.PickupPassengers, Vector2.Zero, 0f));
            }
            else
            {
                this.awaitClosest = this.start ?? this.end;
                this.start = null;
                this.end = null;
            }
            this.Owner.TradeTimer = 5f;
            this.State = AIState.PassengerTransport;
            this.FoodOrProd = "Pass";
        }

		public void OrderTransportPassengersFromSave()
		{
            float closestD;
            float Distance;

            if (this.Owner.loyalty.GetOwnedSystems().Where(combat => combat.combatTimer < 1).Count() == 0)
                return;
            /*
            if ((this.end != null && this.end.ParentSystem.CombatInSystem)
                || (this.start != null && this.start.ParentSystem.CombatInSystem))
            {
                this.start = null;
                this.end = null;
                this.OrderQueue.Clear();
                this.State = AIState.AwaitingOrders;
            }
            */
            if (!this.Owner.GetCargo().ContainsKey("Colonists_1000"))
			{
				this.Owner.GetCargo().Add("Colonists_1000", 0f);
			}

            List<Planet> Possible = new List<Planet>();

            // fbedard: Where to drop nearest Population
            if (this.Owner.GetCargo()["Colonists_1000"] > 0f)
            {
                foreach (Planet p in this.Owner.loyalty.GetPlanets())
                {
                    if (p == this.start)
                    {
                        continue;
                    }
                    if (this.Owner.AreaOfOperation.Count <= 0)
                    {
                        if (((p.Population / p.MaxPopulation) >= 0.8 && p.MaxPopulation <= 2000f) || p.Population >= 2000f)
                        {
                            continue;
                        }
                        Possible.Add(p);
                    }
                    else
                    {
                        foreach (Rectangle AO in this.Owner.AreaOfOperation)
                        {
                            if (!HelperFunctions.CheckIntersection(AO, p.Position) || ((p.Population / p.MaxPopulation) >= 0.8 && p.MaxPopulation <= 2000f) || p.Population >= 2000f)
                            {
                                continue;
                            }
                            Possible.Add(p);
                        }
                    }
                }

                closestD = 999999999f;
                this.end = null;
                this.OrderQueue.Clear();
                foreach (Planet p in Possible)
                {
                    Distance = Vector2.Distance(this.Owner.Center, p.Position);
                    if (Distance >= closestD)
                    {
                        continue;
                    }
                    closestD = Distance;
                    this.end = p;
                }
                if (this.end != null)
                {
                    this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
                    this.State = AIState.PassengerTransport;
                    this.FoodOrProd = "Pass";
                    this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropoffPassengers, Vector2.Zero, 0f));
                }
                return;
            }

            //fbedard: Where to load nearest Population
            this.start = null;
            this.OrderQueue.Clear();
            Possible = new List<Planet>();
            foreach (Planet p in this.Owner.loyalty.GetPlanets())
            {
                if (this.Owner.AreaOfOperation.Count <= 0)
                {
                    //if (p.Population <= 1000f)
                    if (p.Population <= 2000f)
                    {
                        continue;
                    }
                    Possible.Add(p);
                }
                else
                {
                    foreach (Rectangle AO in this.Owner.AreaOfOperation)
                    {
                        if (!HelperFunctions.CheckIntersection(AO, p.Position) || p.Population <= 2000f)
                        {
                            continue;
                        }
                        Possible.Add(p);
                    }
                }
            }
            closestD = 999999999f;
            foreach (Planet p in Possible)
            {
                Distance = Vector2.Distance(this.Owner.Center, p.Position);
                if (Distance >= closestD)
                {
                    continue;
                }
                closestD = Distance;
                this.start = p;
            }

            // fbedard: Where to drop nearest Population
            this.end = null;
            Possible = new List<Planet>();
            foreach (Planet p in this.Owner.loyalty.GetPlanets())
            {
                if (p == this.start)
                {
                    continue;
                }
                if (this.Owner.AreaOfOperation.Count <= 0)
                {
                    if (((p.Population / p.MaxPopulation) >= 0.8 && p.MaxPopulation <= 2000f) || p.Population >= 2000f)

                    {
                        continue;
                    }
                    Possible.Add(p);
                }
                else
                {
                    foreach (Rectangle AO in this.Owner.AreaOfOperation)
                    {
                        if (!HelperFunctions.CheckIntersection(AO, p.Position) || ((p.Population / p.MaxPopulation) >= 0.8 && p.MaxPopulation <= 2000f) || p.Population >= 2000f)
                        {
                            continue;
                        }
                        Possible.Add(p);
                    }
                }
            }

            closestD = 999999999f;
            foreach (Planet p in Possible)
            {
                Distance = Vector2.Distance(this.Owner.Center, p.Position);
                if (Distance >= closestD)
                {
                    continue;
                }
                closestD = Distance;
                this.end = p;
            }

            if (this.start != null && this.end != null)
            {
                this.OrderMoveTowardsPosition(this.start.Position, 0f, new Vector2(0f, -1f), true, this.start);
                this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.PickupPassengers, Vector2.Zero, 0f));
            }
            else
            {
                this.start = null;
                this.end = null;
            }
            this.State = AIState.PassengerTransport;
            this.FoodOrProd = "Pass";
        }

		public void OrderTroopToBoardShip(Ship s)
		{
			this.HasPriorityOrder = true;
			this.EscortTarget = s;
			ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.BoardShip, Vector2.Zero, 0f);
           
            this.OrderQueue.Clear();
			this.OrderQueue.AddLast(g);
        
		}

		public void OrderTroopToShip(Ship s)
		{
			this.EscortTarget = s;
			ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.TroopToShip, Vector2.Zero, 0f);
			this.OrderQueue.Clear();
			this.OrderQueue.AddLast(g);
		}

		private void PickupGoods()
		{
            if (this.start == null)
            {
                this.OrderTrade
                       (0.1f);
                return;
            }
            if (this.FoodOrProd == "Food")
			{
				if (this.Owner.GetCargo()["Production"] > 0f)
				{
					Planet productionHere = this.start;
					productionHere.ProductionHere = productionHere.ProductionHere + this.Owner.GetCargo()["Production"];
					this.Owner.GetCargo()["Production"] = 0f;
				}
				if (this.Owner.GetCargo()["Colonists_1000"] > 0f)
				{
					Planet population = this.start;
					population.Population = population.Population + this.Owner.GetCargo()["Colonists_1000"] * (float)this.Owner.loyalty.data.Traits.PassengerModifier;
					this.Owner.GetCargo()["Colonists_1000"] = 0f;
				}
                float modifier = this.start.MAX_STORAGE * .10f;
                //if (this.start.FoodHere < this.Owner.CargoSpace_Max)
                //{
                //    //this.OrderTrade(0.1f);
                //    modifier = this.start.FoodHere * .5f;
                //}
                
				{
					while (this.start.FoodHere >  modifier && (int)this.Owner.CargoSpace_Max - (int)this.Owner.CargoSpace_Used > 0)
					{
						this.Owner.AddGood("Food", 1);
						Planet foodHere = this.start;
						foodHere.FoodHere = foodHere.FoodHere - 1f;
					}
                    this.OrderQueue.RemoveFirst();
					this.OrderMoveTowardsPosition(this.end.Position + (((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true,this.end);
					this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
					//this.State = AIState.SystemTrader;
				}
			}
			else if (this.FoodOrProd != "Prod")
			{
				this.OrderTrade
                    (0.1f);
			}
			else
			{
				if (this.Owner.GetCargo()["Food"] > 0f)
				{
					Planet planet = this.start;
					planet.FoodHere = planet.FoodHere + this.Owner.GetCargo()["Food"];
					this.Owner.GetCargo()["Food"] = 0f;
				}
				if (this.Owner.GetCargo()["Colonists_1000"] > 0f)
				{
					Planet population1 = this.start;
					population1.Population = population1.Population + this.Owner.GetCargo()["Colonists_1000"] * (float)this.Owner.loyalty.data.Traits.PassengerModifier;
					this.Owner.GetCargo()["Colonists_1000"] = 0f;
				}
                float modifier = this.start.MAX_STORAGE *.10f;
                //if (this.start.ProductionHere < this.Owner.CargoSpace_Max)
                //{
                //    //this.OrderTrade(0.1f);
                //    modifier= this.start.ProductionHere * .5f;
                //}
                
				{
                    while (this.start.ProductionHere > modifier && (int)this.Owner.CargoSpace_Max - (int)this.Owner.CargoSpace_Used > 0)
					{
						this.Owner.AddGood("Production", 1);
						Planet productionHere1 = this.start;
						productionHere1.ProductionHere = productionHere1.ProductionHere - 1f;
					}
                    this.OrderQueue.RemoveFirst();
					this.OrderMoveTowardsPosition(this.end.Position + (((this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG)).RandomDirection() * 500f), 0f, new Vector2(0f, -1f), true,this.end);
					this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropOffGoods, Vector2.Zero, 0f));
					//this.State = AIState.SystemTrader;
				}
			}
			this.State = AIState.SystemTrader;
		}

        private void PickupAnyGoods()  //fbedard
        {
            if (this.end.FoodHere > this.Owner.CargoSpace_Max && this.end.fs == Planet.GoodState.EXPORT && (this.start.MAX_STORAGE - this.start.FoodHere) > this.Owner.CargoSpace_Max * 3f && this.start.fs == Planet.GoodState.IMPORT)
                while (this.end.FoodHere > 0f && (int)this.Owner.CargoSpace_Max - (int)this.Owner.CargoSpace_Used > 0)
                {
                this.Owner.AddGood("Food", 1);
                Planet foodHere = this.end;
                foodHere.FoodHere = foodHere.FoodHere - 1f;
                }

            if (this.end.ProductionHere > this.Owner.CargoSpace_Max && this.end.ps == Planet.GoodState.EXPORT && (this.start.MAX_STORAGE - this.start.ProductionHere) > this.Owner.CargoSpace_Max * 3f && this.start.ps == Planet.GoodState.IMPORT)
                while (this.end.ProductionHere > 0f && (int)this.Owner.CargoSpace_Max - (int)this.Owner.CargoSpace_Used > 0)
                {
                 this.Owner.AddGood("Production", 1);
                 Planet productionHere1 = this.end;
                 productionHere1.ProductionHere = productionHere1.ProductionHere - 1f;
                }
        }

		private void PickupPassengers()
		{
			if (this.Owner.GetCargo()["Production"] > 0f)
			{
				Planet productionHere = this.start;
				productionHere.ProductionHere = productionHere.ProductionHere + this.Owner.GetCargo()["Production"];
				this.Owner.GetCargo()["Production"] = 0f;
			}
			if (this.Owner.GetCargo()["Food"] > 0f)
			{
				Planet foodHere = this.start;
				foodHere.FoodHere = foodHere.FoodHere + this.Owner.GetCargo()["Food"];
				this.Owner.GetCargo()["Food"] = 0f;
			}
			while (this.Owner.CargoSpace_Used < this.Owner.CargoSpace_Max)
			{
				this.Owner.AddGood("Colonists_1000", 1);
				Planet population = this.start;
				population.Population = population.Population - (float)this.Owner.loyalty.data.Traits.PassengerModifier;
			}
			this.OrderQueue.RemoveFirst();
			this.OrderMoveTowardsPosition(this.end.Position, 0f, new Vector2(0f, -1f), true, this.end);
			this.State = AIState.PassengerTransport;
			this.OrderQueue.AddLast(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DropoffPassengers, Vector2.Zero, 0f));
		}

        private void PickupAnyPassengers()  //fbedard
        {
            while (this.Owner.CargoSpace_Used < this.Owner.CargoSpace_Max)
            {
                this.Owner.AddGood("Colonists_1000", 1);
                Planet population = this.end;
                population.Population = population.Population - (float)this.Owner.loyalty.data.Traits.PassengerModifier;
            }
        }
        /*
		private void PlotCourseToNew(Vector2 endPos, Vector2 startPos)
		{
			float Distance = Vector2.Distance(startPos, endPos);
			if (Distance <= this.Owner.CalculateRange())
			{
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Enqueue(endPos);
				}
				return;
			}
			List<Vector2> PotentialWayPoints = new List<Vector2>();
            foreach (Ship ship in this.Owner.loyalty.GetProjectors())
			{
				if (Vector2.Distance(ship.Center, endPos) >= Distance)
				{
					continue;
				}
				PotentialWayPoints.Add(ship.Center);
			}
			foreach (Planet p in this.Owner.loyalty.GetPlanets())
			{
				if (Vector2.Distance(p.Position, endPos) >= Distance)
				{
					continue;
				}
				PotentialWayPoints.Add(p.Position);
			}
			IOrderedEnumerable<Vector2> sortedList = 
				from point in PotentialWayPoints
				orderby Vector2.Distance(startPos, point)
				select point;
			List<Vector2> Closest3 = new List<Vector2>();
			int i = 0;
			using (IEnumerator<Vector2> enumerator = sortedList.GetEnumerator())
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						break;
					}
					Closest3.Add(enumerator.Current);
					i++;
				}
				while (i != 3);
			}
			sortedList = 
				from point in Closest3
				orderby Vector2.Distance(point, endPos)
				select point;
			bool gotWayPoint = false;
			if (sortedList.Count<Vector2>() > 0)
			{
				if (Vector2.Distance(endPos, startPos) >= Vector2.Distance(startPos, sortedList.First<Vector2>()))
				{
					lock (this.wayPointLocker)
					{
						this.ActiveWayPoints.Enqueue(sortedList.First<Vector2>());
					}
					this.PlotCourseToNew(endPos, sortedList.First<Vector2>());
					gotWayPoint = true;
				}
				else
				{
					gotWayPoint = false;
				}
			}
			if (!gotWayPoint)
			{
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Enqueue(endPos);
				}
			}
		}
        */





        //fbedard: new version not recursive        
        private void PlotCourseToNew(Vector2 endPos, Vector2 startPos)
        {
            if (true)
            {
                bool pathfound = false;
                
                int check = this.Owner.loyalty.pathcache.Count;
                List<Vector2> keyfound = null;
                this.Owner.loyalty.lockPatchCache.EnterReadLock();
                foreach (KeyValuePair<List<Vector2>, int> cache in this.Owner.loyalty.pathcache)
                {
                    List<Vector2> test = cache.Key .ToList();
                    Vector2 aprox = test[0];

                    float area = Vector2.Distance(aprox, startPos);
                    if (area < Empire.ProjectorRadius)
                        if (Vector2.Distance(test[test.Count - 1], endPos) < Empire.ProjectorRadius )
                        {
                            test[0] = startPos;
                            test[test.Count - 1] = endPos;
                            keyfound = cache.Key;
                           // this.Owner.loyalty.pathcache[cache.Key]++;
                            lock (this.wayPointLocker)
                            {
                                if (this.ActiveWayPoints.Count == 0)
                                    this.ActiveWayPoints = new Queue<Vector2>(test.Skip(1));
                                else

                                {
                                    foreach (Vector2 wayp in test.Skip(1))
                                    {

                                        this.ActiveWayPoints.Enqueue(wayp);
                                    }
                                }



                            }
                            pathfound = true;
                            break;
                        }




                }
                this.Owner.loyalty.lockPatchCache.ExitReadLock();
                if (pathfound)
                {
                    this.Owner.loyalty.lockPatchCache.EnterWriteLock();
                    this.Owner.loyalty.pathcache[keyfound]++;
                    this.Owner.loyalty.lockPatchCache.ExitWriteLock();
                    return;
                }
                List<Vector2> goodpoints = new List<Vector2>();
                //Grid path = new Grid(this.Owner.loyalty, 36, 10f);
                if (Empire.universeScreen != null && this.Owner.loyalty.SensorNodes.Count != 0)
                    goodpoints = this.Owner.loyalty.pathhMap.Pathfind(startPos, endPos, false);
                if (goodpoints != null && goodpoints.Count > 0)
                {
                    lock (this.wayPointLocker)
                    {
                        foreach (Vector2 wayp in goodpoints.Skip(1))
                        {

                            this.ActiveWayPoints.Enqueue(wayp);
                        }
                        //this.ActiveWayPoints.Enqueue(endPos);
                    }                    
                    this.Owner.loyalty.lockPatchCache.EnterWriteLock();
                    int cache;
                    if (!this.Owner.loyalty.pathcache.TryGetValue(goodpoints, out cache))
                    {

                        this.Owner.loyalty.pathcache.Add(goodpoints, 0);

                    }
                    cache++;
                    this.Owner.loyalty.lockPatchCache.ExitWriteLock();
                    
                }
                else
                {
                    if (startPos != Vector2.Zero && endPos != Vector2.Zero)
                    {
                       // this.ActiveWayPoints.Enqueue(startPos);
                        this.ActiveWayPoints.Enqueue(endPos);
                    }
                    else
                        this.ActiveWayPoints.Clear();
                }




                return;
            }
           
            float Distance = Vector2.Distance(startPos, endPos);
            if (Distance <= Empire.ProjectorRadius)
            {
                lock (this.wayPointLocker)
                    this.ActiveWayPoints.Enqueue(endPos);
                return;
            }
            //if (PlotCourseToNewViaRoad(endPos, startPos) != null)

            List<Vector2> PickWayPoints = new List<Vector2>(this.GoodRoad(endPos, startPos)); //PlotCourseToNewViaRoad(endPos, startPos));//
            float d1, d2;
            float DistToEnd1, DistToEnd2;
            if (PickWayPoints.Count == 0)
            {
                foreach (Ship proj in this.Owner.loyalty.GetProjectors())
                {
                    d1 = Vector2.Distance(proj.Center, startPos);
                    d2 = Vector2.Distance(proj.Center, endPos);
                    if (d1 <= Distance && d2 <= Distance)
                        // lock (this.wayPointLocker)
                        PickWayPoints.Add(proj.Center);
                }
                if (PickWayPoints.Count == 0) //if no projectors then just go to target.
                {
                    lock (this.wayPointLocker)
                   this.ActiveWayPoints.Enqueue(endPos);
                    return;
                }
                foreach (SolarSystem p in this.Owner.loyalty.GetOwnedSystems())
                {
                    d1 = Vector2.Distance(p.Position, startPos);
                    d2 = Vector2.Distance(p.Position, endPos);
                    if (d1 <= Distance && d2 <= Distance)
                       // lock (this.wayPointLocker)
                            PickWayPoints.Add(p.Position);
                }
            }
//#if DEBUG
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("Empire :" + this.Owner.loyalty.PortraitName);
//                System.Diagnostics.Debug.WriteLine("Ship :" + this.Owner.VanityName);
//            }
//#endif            
            if (!this.ActiveWayPoints.Contains(endPos))
                    PickWayPoints.Add(endPos);

            int pt;
            float distMult;
            Vector2 wp1, wp2, current = this.Owner.Center;
            IOrderedEnumerable<Vector2> sortedList = from point in PickWayPoints where current != point orderby Vector2.Distance(current, point) select point;
           
            // Loop through points.
            for (int i = 0; i < PickWayPoints.Count; i++)
            {
                pt = 0;
                distMult = 1f + (PickWayPoints.Count - i) / PickWayPoints.Count * .2f;
                wp1 = Vector2.Zero;
                wp2 = Vector2.Zero;
                DistToEnd1 = 99999999f;
                DistToEnd2 = 99999999f;
                using (IEnumerator<Vector2> enumerator = sortedList.GetEnumerator())
                {
                    do
                    {// find the nearest among the next 3 valid points.
                        if (!enumerator.MoveNext())
                            break;
                        d1 = Vector2.Distance(enumerator.Current, current);
                        d2 = Vector2.Distance(enumerator.Current, endPos);

                        if (!this.ActiveWayPoints.Contains(enumerator.Current)
                            && (d2 <= Distance && d2 > Empire.ProjectorRadius * 1.5)
                            || (d1 <= Empire.ProjectorRadius * 2.5f && d2 <= Distance * distMult)
                             
                            )
                        {
                            if (d1 <= Empire.ProjectorRadius * 2.5f )
                            {
                                if (d1 + d2 < DistToEnd1)
                                {
                                    wp1 = enumerator.Current;
                                    DistToEnd1 = d1 + d2;
                                }
                            }
                            else
                                if (d1 + d2 < DistToEnd2)
                                {
                                    wp2 = enumerator.Current;
                                    DistToEnd2 = d1 + d2;
                                }
                            pt++;
                        }
                        
                    }
                    while (pt != 3);
                }

                if (wp1 != Vector2.Zero || wp2 != Vector2.Zero)
                {
                    if (wp1 != Vector2.Zero)
                        current = wp1;
                    else
                        current = wp2;
                    sortedList = from point in PickWayPoints where current != point orderby Vector2.Distance(current, point) select point;
                    Distance = Vector2.Distance(current, endPos);
                    //goodroad = this.GoodRoad(current, endPos);
                    //if(goodroad !=null && current != endPos)

                    lock (this.wayPointLocker)
                        this.ActiveWayPoints.Enqueue(current);
                    if (current == endPos)
                        break;
                }
            }

            if (this.ActiveWayPoints.Count == 0 || this.ActiveWayPoints.Last() != endPos)
                lock (this.wayPointLocker)
                    this.ActiveWayPoints.Enqueue(endPos);
        }

        private List<Vector2> GoodRoad(Vector2 endPos, Vector2 startPos)
        {
            SpaceRoad targetRoad =null;
            List<SpaceRoad> StartRoads = new List<SpaceRoad>();
            List<SpaceRoad> endRoads = new List<SpaceRoad>();
            List<Vector2> nodePos = new List<Vector2>();
            foreach (SpaceRoad road in this.Owner.loyalty.SpaceRoadsList)
            {
                Vector2 start = road.GetOrigin().Position;
                Vector2 end = road.GetDestination().Position;
                if (Vector2.Distance(start, startPos) < Empire.ProjectorRadius)
                {
                    if (Vector2.Distance(end, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        StartRoads.Add(road);
                }
                else if (Vector2.Distance(end, startPos) < Empire.ProjectorRadius)
                {
                    if (Vector2.Distance(start, endPos) < Empire.ProjectorRadius)
                        targetRoad = road;
                    else
                        endRoads.Add(road);
                }

                if (  targetRoad !=null)
                    break;
            }
            

            if(targetRoad != null)
            {
                foreach(RoadNode node in targetRoad.RoadNodesList)
                {
                    nodePos.Add(node.Position);
                }
                nodePos.Add(endPos);
                nodePos.Add(targetRoad.GetDestination().Position);
                nodePos.Add(targetRoad.GetOrigin().Position);
            }
            return nodePos;
            


        }

        private List<Vector2> PlotCourseToNewViaRoad(Vector2 endPos, Vector2 startPos)
        {
            //return null;
            List<Vector2> goodPoints = new List<Vector2>();
            List<SpaceRoad> potentialEndRoads = new List<SpaceRoad>();
            List<SpaceRoad> potentialStartRoads = new List<SpaceRoad>();
            RoadNode nearestNode = null;
            float distanceToNearestNode = 0f;
            foreach(SpaceRoad road in this.Owner.loyalty.SpaceRoadsList)
            {
                if (Vector2.Distance(road.GetOrigin().Position, endPos) < 300000f || Vector2.Distance(road.GetDestination().Position, endPos) < 300000f)
                {
                    potentialEndRoads.Add(road);
                }
                foreach(RoadNode projector in road.RoadNodesList)
                {
                    if (nearestNode == null || Vector2.Distance(projector.Position, startPos) < distanceToNearestNode)
                    {
                        potentialStartRoads.Add(road);
                        nearestNode = projector;
                        distanceToNearestNode = Vector2.Distance(projector.Position, startPos);
                    }
                }
            }

            List<SpaceRoad> targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
            if (targetRoads.Count == 1)
            {
                SpaceRoad targetRoad = targetRoads[0];
                bool startAtOrgin = Vector2.Distance(endPos, targetRoad.GetOrigin().Position) > Vector2.Distance(endPos, targetRoad.GetDestination().Position);
                bool foundstart = false;
                if (startAtOrgin)
                    foreach (RoadNode node in targetRoad.RoadNodesList)
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                        {
                            foundstart = true;
                        }
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);

                    }
                else
                    foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                    {
                        if (!foundstart && node != nearestNode)
                            continue;
                        else if (!foundstart)
                        {
                            foundstart = true;
                        }
                        goodPoints.Add(node.Position);
                        goodPoints.Add(targetRoad.GetDestination().Position);
                        goodPoints.Add(targetRoad.GetOrigin().Position);

                    }
          
            }
            else if(true)
            {
                while (potentialStartRoads.Intersect(potentialEndRoads).Count() == 0)
                {
                    bool test = false;
                    foreach (SpaceRoad road in this.Owner.loyalty.SpaceRoadsList)
                    {
                        bool flag = false;

                        if (!potentialStartRoads.Contains(road))
                        {

                            foreach (SpaceRoad proad in potentialStartRoads)
                            {
                                if (proad.GetDestination() == road.GetOrigin() || proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                            }

                        }
                        if (flag)
                        {
                            potentialStartRoads.Add(road);
                            test = true;
                        }
                        
                    }
                     if(!test)
                    {
                        System.Diagnostics.Debug.WriteLine("failed to find road path for " + this.Owner.loyalty.PortraitName);
                        return new List<Vector2>();
                    }
                }
                while (!potentialEndRoads.Contains(potentialStartRoads[0]))
                {
                    bool test = false;
                    foreach (SpaceRoad road in potentialStartRoads)
                    {
                        bool flag = false;

                        if (!potentialEndRoads.Contains(road))
                        {

                            foreach (SpaceRoad proad in potentialEndRoads)
                            {
                                if (proad.GetDestination() == road.GetOrigin() || proad.GetOrigin() == road.GetDestination())
                                    flag = true;
                                
                            }

                        }
                        if (flag)
                        {

                            test = true;
                            potentialEndRoads.Add(road);
                            
                        }
                        
                    }
                    if(!test)
                    {
                        System.Diagnostics.Debug.WriteLine("failed to find road path for " + this.Owner.loyalty.PortraitName);
                        return new List<Vector2>();
                    }


                }
                targetRoads = potentialStartRoads.Intersect(potentialEndRoads).ToList();
                if (targetRoads.Count >0)
                {
                    SpaceRoad targetRoad = null;
                    RoadNode targetnode = null;
                    float distance = -1f;
                    foreach (SpaceRoad road in targetRoads)
                    {
                        foreach (RoadNode node in road.RoadNodesList)
                        {
                            if (distance == -1f || Vector2.Distance(node.Position, startPos) < distance)
                            {
                                targetRoad = road;
                                targetnode = node;
                                distance = Vector2.Distance(node.Position, startPos);

                            }
                        }
                    }
                    bool orgin = false;
                    bool startnode = false;
                    foreach (SpaceRoad road in targetRoads)
                    {
                        if (road.GetDestination() == targetRoad.GetDestination() || road.GetDestination() == targetRoad.GetOrigin())
                            orgin = true;
                    }
                    if (orgin)
                    {
                        foreach (RoadNode node in targetRoad.RoadNodesList)
                        {
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                        }


                    }
                    else
                    {
                        foreach (RoadNode node in targetRoad.RoadNodesList.Reverse<RoadNode>())
                        {
                            if (!startnode || node != targetnode)
                            {
                                continue;
                            }
                            else
                            {
                                startnode = true;
                                goodPoints.Add(node.Position);
                                goodPoints.Add(targetRoad.GetDestination().Position);
                                goodPoints.Add(targetRoad.GetOrigin().Position);
                            }
                        }
                    }
                    while (Vector2.Distance(targetRoad.GetOrigin().Position,endPos)>300000 
                        &&  Vector2.Distance(targetRoad.GetDestination().Position,endPos)>300000)
                    {
                        targetRoads.Remove(targetRoad);
                        if(orgin)
                        {
                            bool test = false;
                            foreach(SpaceRoad road in targetRoads)
                            {
                                if(road.GetOrigin()==targetRoad.GetDestination())
                                {
                                    foreach(RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if(road.GetDestination() == targetRoad.GetDestination())
                                {
                                    orgin = false;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                    {
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    }
                                    test = true;
                                    targetRoad = road;
                                    break;
                                }
                            }
                            if (!test)
                                orgin = false;
                        }
                        else
                        {
                            bool test = false;
                            foreach (SpaceRoad road in targetRoads)
                            {
                                if (road.GetOrigin() == targetRoad.GetOrigin())
                                {
                                    foreach (RoadNode node in road.RoadNodesList)
                                    {
                                        goodPoints.Add(node.Position);
                                        goodPoints.Add(targetRoad.GetDestination().Position);
                                        goodPoints.Add(targetRoad.GetOrigin().Position);
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }
                                else if (road.GetDestination() == targetRoad.GetOrigin())
                                {
                                    orgin = true;
                                    if (road.GetOrigin() == targetRoad.GetDestination())
                                    {
                                        foreach (RoadNode node in road.RoadNodesList.Reverse<RoadNode>())
                                        {
                                            goodPoints.Add(node.Position);
                                            goodPoints.Add(targetRoad.GetDestination().Position);
                                            goodPoints.Add(targetRoad.GetOrigin().Position);
                                        }
                                    }
                                    targetRoad = road;
                                    test = true;
                                    break;
                                }

                                
                            }
                            if (!test)
                                break;
                        }

                    }
                }
            }


            if(goodPoints.Count ==0) return new List<Vector2>();


            


            return goodPoints;
        }

		private void RotateInLineWithVelocity(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
		{
			if (this.Owner.Velocity == Vector2.Zero)
			{
				this.OrderQueue.RemoveFirst();
				return;
			}
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(Vector2.Normalize(this.Owner.Velocity), forward));
			float facing = (Vector2.Dot(Vector2.Normalize(this.Owner.Velocity), right) > 0f ? 1f : -1f);
			if (angleDiff <= 0.2f)
			{
				this.OrderQueue.RemoveFirst();
				return;
			}
			this.RotateToFacing(elapsedTime, angleDiff, facing);
		}

		private void RotateToDesiredFacing(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
			Vector2 p = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, goal.DesiredFacing, 1f);
			Vector2 fvec = HelperFunctions.FindVectorToTarget(Vector2.Zero, p);
			Vector2 wantedForward = Vector2.Normalize(fvec);
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
			float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
			if (angleDiff <= 0.02f)
			{
				this.OrderQueue.RemoveFirst();
				return;
			}
			this.RotateToFacing(elapsedTime, angleDiff, facing);
		}

		private bool RotateToFaceMovePosition(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
            bool turned = false;
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			Vector2 right = new Vector2(-forward.Y, forward.X);
			Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, goal.MovePosition);
			float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
			if (angleDiff > 0.2f)
			{
				this.Owner.HyperspaceReturn();
				this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f));
                turned = true;
			}
			else if (this.OrderQueue.Count > 0)
			{
				this.OrderQueue.RemoveFirst();
				
			}
            return turned;
		}
        private bool RotateToFaceMovePosition(float elapsedTime, Vector2 MovePosition)
        {
            bool turned = false;
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
            Vector2 right = new Vector2(-forward.Y, forward.X);
            Vector2 VectorToTarget = HelperFunctions.FindVectorToTarget(this.Owner.Center, MovePosition);
            float angleDiff = (float)Math.Acos((double)Vector2.Dot(VectorToTarget, forward));
            if (angleDiff > this.Owner.rotationRadiansPerSecond*elapsedTime )
            {
                this.Owner.HyperspaceReturn();
                this.RotateToFacing(elapsedTime, angleDiff, (Vector2.Dot(VectorToTarget, right) > 0f ? 1f : -1f));
                turned = true;
            }
 
            return turned;
        }
		private void RotateToFacing(float elapsedTime, float angleDiff, float facing)
		{
			this.Owner.isTurning = true;
			float RotAmount = Math.Min(angleDiff, facing * elapsedTime * this.Owner.rotationRadiansPerSecond);
			if (Math.Abs(RotAmount) > angleDiff)
			{
				RotAmount = (RotAmount <= 0f ? -angleDiff : angleDiff);
			}
			if (RotAmount > 0f)
			{
				if (this.Owner.yRotation > -this.Owner.maxBank)
				{
					Ship owner = this.Owner;
					owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
				}
			}
			else if (RotAmount < 0f && this.Owner.yRotation < this.Owner.maxBank)
			{
				Ship ship = this.Owner;
				ship.yRotation = ship.yRotation + this.Owner.yBankAmount;
			}
			if (!float.IsNaN(RotAmount))
			{
				Ship rotation = this.Owner;
				rotation.Rotation = rotation.Rotation + RotAmount;
			}
		}

        //added by gremlin Deveksmod Scan for combat targets.
        public GameplayObject ScanForCombatTargets(Vector2 Position, float Radius)
        {

            RandomThreadMath randomThreadMath;
            this.BadGuysNear = false;
            this.FriendliesNearby.Clear();
            this.PotentialTargets.Clear();
            this.NearbyShips.Clear();
            //this.TrackProjectiles.Clear();

            if (this.hasPriorityTarget && this.Target == null)
            {
                this.hasPriorityTarget = false;
                if (this.TargetQueue.Count > 0)
                {
                    this.hasPriorityTarget = true;
                    this.Target = this.TargetQueue.First<Ship>();
                }
            }
            if (this.Target != null)
            {
                if ((this.Target as Ship).loyalty == this.Owner.loyalty)
                {
                    this.Target = null;
                    this.hasPriorityTarget = false;
                }


                else if (
                    !this.Intercepting && (this.Target as Ship).engineState == Ship.MoveState.Warp) //||((double)Vector2.Distance(Position, this.Target.Center) > (double)Radius ||
                {
                    this.Target = (GameplayObject)null;
                    if (!this.HasPriorityOrder && this.Owner.loyalty != ArtificialIntelligence.universeScreen.player)
                        this.State = AIState.AwaitingOrders;
                    return (GameplayObject)null;
                }
            }
            //Doctor: Increased this from 0.66f as seemed slightly on the low side. 
            this.CombatAI.PreferredEngagementDistance = this.Owner.maxWeaponsRange * 0.75f;
            SolarSystem thisSystem = this.Owner.GetSystem();
            if(thisSystem != null)
                foreach (Planet p in thisSystem.PlanetList)
                {
                    Empire emp = p.Owner;
                    if (emp !=null && emp != this.Owner.loyalty)
                    {
                        Relationship test = null;
                        this.Owner.loyalty.GetRelations().TryGetValue(emp, out test);
                        if (!test.Treaty_OpenBorders || !test.Treaty_NAPact || Vector2.Distance(this.Owner.Center, p.Position) >Radius)
                        {
                            //if(p.Projectiles.Count >0) // && Vector2.Distance(p.Position,this.Owner.Center) <10000)
                            this.BadGuysNear = true;
                        }
                        break;
                    }


                }
            {
                if (this.EscortTarget != null && this.EscortTarget.Active && this.EscortTarget.GetAI().Target != null)
                {
                    ArtificialIntelligence.ShipWeight sw = new ArtificialIntelligence.ShipWeight();
                    sw.ship = this.EscortTarget.GetAI().Target as Ship;
                    sw.weight = 2f;
                    this.NearbyShips.Add(sw);
                }
                List<GameplayObject> nearby = UniverseScreen.ShipSpatialManager.GetNearby(Owner);
                for (int i = 0; i < nearby.Count; i++)
                {
                    Ship item1 = nearby[i] as Ship;
                    float distance = Vector2.Distance(this.Owner.Center, item1.Center);
                    if (item1 != null && item1.Active && !item1.dying && (distance <= Radius + (Radius == 0 ? 10000 : 0)))
                    {
                        
                        Empire empire = item1.loyalty;
                        Ship shipTarget = item1.GetAI().Target as Ship;
                        if (empire == Owner.loyalty)
                        {
                            this.FriendliesNearby.Add(item1);
                        }
                        else if (empire != this.Owner.loyalty && Radius > 0
                            && shipTarget != null
                            && shipTarget == this.EscortTarget && item1.engineState != Ship.MoveState.Warp)
                        {

                            ArtificialIntelligence.ShipWeight sw = new ArtificialIntelligence.ShipWeight();
                            sw.ship = item1;
                            sw.weight = 3f;

                            this.NearbyShips.Add(sw);
                            this.BadGuysNear = true;
                            //this.PotentialTargets.Add(item1);
                        }
                        else if (Radius > 0 && (item1.loyalty != this.Owner.loyalty 
                            && this.Owner.loyalty.GetRelations()[item1.loyalty].AtWar
                            || this.Owner.loyalty.isFaction || item1.loyalty.isFaction))//&& Vector2.Distance(this.Owner.Center, item.Center) < 15000f)
                        {
                            ArtificialIntelligence.ShipWeight sw = new ArtificialIntelligence.ShipWeight();
                            sw.ship = item1;
                            sw.weight = 1f;
                            this.NearbyShips.Add(sw);
                            //this.PotentialTargets.Add(item1);
                            this.BadGuysNear = Vector2.Distance(Position, item1.Position) <= Radius;
                        }
                        else if (Radius == 0 &&
                            (item1.loyalty != this.Owner.loyalty
                            && this.Owner.loyalty.GetRelations()[item1.loyalty].AtWar
                            || this.Owner.loyalty.isFaction || item1.loyalty.isFaction)
                            )
                            this.BadGuysNear = true;


                    }
                }
            }


            #region supply ship logic   //fbedard: for launch only
            if (this.Owner.GetHangars().Where(hangar => hangar.IsSupplyBay).Count() > 0 && this.Owner.engineState != Ship.MoveState.Warp)  // && !this.Owner.isSpooling
            {
                IOrderedEnumerable<Ship> sortedList = null;
                {
                    sortedList = FriendliesNearby.Where(ship => ship != this.Owner 
                        && ship.engineState != Ship.MoveState.Warp
                        && ship.GetAI().State != AIState.Scrap
                        && ship.GetAI().State != AIState.Resupply
                        && ship.GetAI().State != AIState.Refit
                        && ship.Mothership == null 
                        && ship.OrdinanceMax > 0 
                        && ship.Ordinance / ship.OrdinanceMax < 0.5f
                        && !ship.IsTethered())
                        .OrderBy(ship => Math.Truncate((Vector2.Distance(this.Owner.Center, ship.Center) + 4999)) / 5000).ThenByDescending(ship => ship.OrdinanceMax - ship.Ordinance);
//                      .OrderBy(ship => ship.HasSupplyBays).ThenBy(ship => ship.OrdAddedPerSecond).ThenBy(ship => Math.Truncate((Vector2.Distance(this.Owner.Center, ship.Center) + 4999)) / 5000).ThenBy(ship => ship.OrdinanceMax - ship.Ordinance);
                }

                    if (sortedList.Count() > 0)
                    {
                        int skip = 0;
                        float inboundOrdinance = 0f;
                    if(Owner.HasSupplyBays)
                        foreach (ShipModule hangar in this.Owner.GetHangars().Where(hangar => hangar.IsSupplyBay))
                        {
                            if (hangar.GetHangarShip() != null && hangar.GetHangarShip().Active)
                            {
                                if (hangar.GetHangarShip().GetAI().State != AIState.Ferrying && hangar.GetHangarShip().GetAI().State != AIState.ReturnToHangar && hangar.GetHangarShip().GetAI().State != AIState.Resupply && hangar.GetHangarShip().GetAI().State != AIState.Scrap)
                                {
                                    if (sortedList.Skip(skip).Count() > 0)
                                    {
                                        ArtificialIntelligence.ShipGoal g1 = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.SupplyShip, Vector2.Zero, 0f);
                                        hangar.GetHangarShip().GetAI().EscortTarget = sortedList.Skip(skip).First();

                                        hangar.GetHangarShip().GetAI().IgnoreCombat = true;
                                        hangar.GetHangarShip().GetAI().OrderQueue.Clear();
                                        hangar.GetHangarShip().GetAI().OrderQueue.AddLast(g1);
                                        hangar.GetHangarShip().GetAI().State = AIState.Ferrying;
                                        continue;
                                    }
                                    else
                                    {
                                        //hangar.GetHangarShip().QueueTotalRemoval();
                                        hangar.GetHangarShip().GetAI().State = AIState.ReturnToHangar;  //shuttle with no target
                                        continue;
                                    }
                                }
                                else if (sortedList.Skip(skip).Count() > 0 && hangar.GetHangarShip().GetAI().EscortTarget == sortedList.Skip(skip).First() && hangar.GetHangarShip().GetAI().State == AIState.Ferrying)
                                {
                                    inboundOrdinance = inboundOrdinance + 100f;
                                    if ((inboundOrdinance + sortedList.Skip(skip).First().Ordinance) / sortedList.First().OrdinanceMax > 0.5f)
                                    {
                                        skip++;
                                        inboundOrdinance = 0;
                                        continue;
                                    }
                                }
                                continue;
                            }
                            if (!hangar.Active || hangar.hangarTimer > 0f || (this.Owner.Ordinance >= 100f && sortedList.Skip(skip).Count() <= 0))
                                continue;                            
                            if (Ship_Game.ResourceManager.ShipsDict["Supply_Shuttle"].Mass / 5f > this.Owner.Ordinance)  //fbedard: New spawning cost
                                continue;
                            Ship shuttle = ResourceManager.CreateShipFromHangar("Supply_Shuttle", this.Owner.loyalty, this.Owner.Center, this.Owner);
                            shuttle.VanityName = "Supply Shuttle";
                            //shuttle.shipData.Role = ShipData.RoleName.supply;
                            //shuttle.GetAI().DefaultAIState = AIState.Flee;
                            Ship ship1 = shuttle;
                            randomThreadMath = (this.Owner.GetSystem() != null ? this.Owner.GetSystem().RNG : ArtificialIntelligence.universeScreen.DeepSpaceRNG);
                            ship1.Velocity = (randomThreadMath.RandomDirection() * shuttle.speed) + this.Owner.Velocity;
                            if (shuttle.Velocity.Length() > shuttle.velocityMaximum)
                                shuttle.Velocity = Vector2.Normalize(shuttle.Velocity) * shuttle.speed;
                            this.Owner.Ordinance -= shuttle.Mass / 5f;

                            if (this.Owner.Ordinance >= 100f)
                            {
                                inboundOrdinance = inboundOrdinance + 100f;
                                this.Owner.Ordinance = this.Owner.Ordinance - 100f;
                                hangar.SetHangarShip(shuttle);
                                ArtificialIntelligence.ShipGoal g = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.SupplyShip, Vector2.Zero, 0f);
                                shuttle.GetAI().EscortTarget = sortedList.Skip(skip).First();
                                shuttle.GetAI().IgnoreCombat = true;
                                shuttle.GetAI().OrderQueue.Clear();
                                shuttle.GetAI().OrderQueue.AddLast(g);
                                shuttle.GetAI().State = AIState.Ferrying;
                            }
                            else  //fbedard: Go fetch ordinance when mothership is low on ordinance
                            {
                                shuttle.Ordinance = 0f;
                                hangar.SetHangarShip(shuttle);
                                shuttle.GetAI().IgnoreCombat = true;
                                shuttle.GetAI().State = AIState.Resupply;
                                shuttle.GetAI().OrderResupplyNearest(true);
                            }
                            break;
                        }
                    }
    
            } 
            if (this.Owner.shipData.Role == ShipData.RoleName.supply && this.Owner.Mothership == null)
            {
                    //this.Owner.QueueTotalRemoval();
                    this.OrderScrapShip();   //Destroy shuttle without mothership
            }
            #endregion

            //}           
            foreach (ArtificialIntelligence.ShipWeight nearbyShip in this.NearbyShips )
                // Doctor: I put modifiers for the ship roles Fighter and Bomber in here, so that when searching for targets they prioritise their targets based on their selected ship role.
                // I'll additionally put a ScanForCombatTargets into the carrier fighter code such that they use this code to select their own weighted targets.
            //Parallel.ForEach(this.NearbyShips, nearbyShip =>
            {
                if (nearbyShip.ship.loyalty != this.Owner.loyalty)
                {
                    if ((this.Target as Ship) == nearbyShip.ship)
                        nearbyShip.weight += 3;
                    if (nearbyShip.ship.Weapons.Count ==0)
                    {
                        ArtificialIntelligence.ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + this.CombatAI.PirateWeight;
                    }
                    
                    if (nearbyShip.ship.Health / nearbyShip.ship.HealthMax < 0.5f)
                    {
                        ArtificialIntelligence.ShipWeight vultureWeight = nearbyShip;
                        vultureWeight.weight = vultureWeight.weight + this.CombatAI.VultureWeight;
                    }
                    if (nearbyShip.ship.Size < 30)
                    {
                        ArtificialIntelligence.ShipWeight smallAttackWeight = nearbyShip;
                        smallAttackWeight.weight = smallAttackWeight.weight + this.CombatAI.SmallAttackWeight;
                        if (this.Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                        {
                            smallAttackWeight.weight *= 2f;
                        }
                        if (this.Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                        {
                            smallAttackWeight.weight /= 2f;
                        }
                    }
                    if (nearbyShip.ship.Size > 30 && nearbyShip.ship.Size < 100)
                    {
                        ArtificialIntelligence.ShipWeight mediumAttackWeight = nearbyShip;
                        mediumAttackWeight.weight = mediumAttackWeight.weight + this.CombatAI.MediumAttackWeight;
                        if (this.Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                        {
                            mediumAttackWeight.weight *= 1.5f;
                        }
                    }
                    if (nearbyShip.ship.Size > 100)
                    {
                        ArtificialIntelligence.ShipWeight largeAttackWeight = nearbyShip;
                        largeAttackWeight.weight = largeAttackWeight.weight + this.CombatAI.LargeAttackWeight;
                        if (this.Owner.shipData.ShipCategory == ShipData.Category.Fighter)
                        {
                            largeAttackWeight.weight /= 2f;
                        }
                        if (this.Owner.shipData.ShipCategory == ShipData.Category.Bomber)
                        {
                            largeAttackWeight.weight *= 2f;
                        }
                    }
                    float rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, this.Owner.Center);
                    if (rangeToTarget <= this.CombatAI.PreferredEngagementDistance) 
                       // && Vector2.Distance(nearbyShip.ship.Center, this.Owner.Center) >= this.Owner.maxWeaponsRange)
                    {
                        ArtificialIntelligence.ShipWeight shipWeight = nearbyShip;
                        shipWeight.weight = (int)Math.Ceiling(shipWeight.weight + 5 *
                            ((this.CombatAI.PreferredEngagementDistance -Vector2.Distance(this.Owner.Center,nearbyShip.ship.Center))
                            / this.CombatAI.PreferredEngagementDistance  ))
                            
                            ;
                    }
                    else if (rangeToTarget > (this.CombatAI.PreferredEngagementDistance + this.Owner.velocityMaximum * 5))
                    {
                        ArtificialIntelligence.ShipWeight shipWeight1 = nearbyShip;
                        shipWeight1.weight = shipWeight1.weight - 2.5f * (rangeToTarget / (this.CombatAI.PreferredEngagementDistance + this.Owner.velocityMaximum * 5));
                    }
                    if(this.Owner.Mothership !=null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, this.Owner.Mothership.Center);
                        if (rangeToTarget < this.CombatAI.PreferredEngagementDistance)
                            nearbyShip.weight += 1;

                    }
                    if (this.EscortTarget != null)
                    {
                        rangeToTarget = Vector2.Distance(nearbyShip.ship.Center, this.EscortTarget.Center);
                        if( rangeToTarget <5000) // / (this.CombatAI.PreferredEngagementDistance +this.Owner.velocityMaximum ))
                            nearbyShip.weight += 1;
                        else
                            nearbyShip.weight -= 2;
                        if (nearbyShip.ship.GetAI().Target == this.EscortTarget)
                            nearbyShip.weight += 1;

                    }
                    if(nearbyShip.ship.Weapons.Count <1)
                    {
                        nearbyShip.weight -= 3;
                    }
                    
                    foreach (ArtificialIntelligence.ShipWeight otherShip in this.NearbyShips)
                    {
                        if (otherShip.ship.loyalty != this.Owner.loyalty)
                        {
                            if (otherShip.ship.GetAI().Target != this.Owner)
                            {
                                continue;
                            }
                            ArtificialIntelligence.ShipWeight selfDefenseWeight = nearbyShip;
                            selfDefenseWeight.weight = selfDefenseWeight.weight + 0.2f * this.CombatAI.SelfDefenseWeight;
                        }
                        else if (otherShip.ship.GetAI().Target != nearbyShip.ship)
                        {
                            continue;
                        }
                    }

                }
                else
                {
                    this.NearbyShips.QueuePendingRemoval(nearbyShip);
                }

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
            this.NearbyShips.ApplyPendingRemovals();
            IEnumerable<ArtificialIntelligence.ShipWeight> sortedList2 =
                from potentialTarget in this.NearbyShips
                orderby potentialTarget.weight descending //, Vector2.Distance(potentialTarget.ship.Center,this.Owner.Center) 
                select potentialTarget;
            
            {
                //this.PotentialTargets.ClearAdd() ;//.ToList() as BatchRemovalCollection<Ship>;

                //trackprojectiles in scan for targets.

                this.PotentialTargets.ClearAdd(sortedList2.Select(ship => ship.ship));
                   // .Where(potentialTarget => Vector2.Distance(potentialTarget.Center, this.Owner.Center) < this.CombatAI.PreferredEngagementDistance));
                    
            }
            if (this.Target != null && !this.Target.Active)
            {
                this.Target = null;
                this.hasPriorityTarget = false;
            }
            else if (this.Target != null && this.Target.Active && this.hasPriorityTarget)
            {
                if (this.Owner.loyalty.GetRelations()[(this.Target as Ship).loyalty].AtWar || this.Owner.loyalty.isFaction || (this.Target as Ship).loyalty.isFaction)
                {
                    //this.PotentialTargets.Add(this.Target as Ship);
                    this.BadGuysNear = true;
                }
                return this.Target;
            }
            if (sortedList2.Count<ArtificialIntelligence.ShipWeight>() > 0)
            {
                //if (this.Owner.shipData.Role == ShipData.RoleName.supply && this.Owner.VanityName != "Supply Shuttle")
                //{
                //    this.Target = sortedList2.ElementAt<ArtificialIntelligence.ShipWeight>(0).ship;
                //}
                this.Target = sortedList2.ElementAt<ArtificialIntelligence.ShipWeight>(0).ship;
            }

            if (this.Owner.Weapons.Count > 0 || this.Owner.GetHangars().Count > 0)
                return this.Target;          
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
            float radius = 30000f;
            Vector2 senseCenter = this.Owner.Center;
            if (UseSensorsForTargets)
            {
                if (this.Owner.Mothership != null)
                {
                    if (Vector2.Distance(this.Owner.Center, this.Owner.Mothership.Center) <= this.Owner.Mothership.SensorRange - this.Owner.SensorRange)
                    {
                        senseCenter = this.Owner.Mothership.Center;
                        radius = this.Owner.Mothership.SensorRange;
                    }
                }
                else
                {
                    radius = this.Owner.SensorRange;
                    if (this.Owner.inborders) radius += 10000;
                }
            }
            else if (this.Owner.Mothership != null )
            {
                senseCenter = this.Owner.Mothership.Center;
                                
            }
         

            if (this.Owner.fleet != null)
            {
                if (!this.hasPriorityTarget)
                {
                    this.Target = this.ScanForCombatTargets(senseCenter, radius);
                }
                else
                {
                    this.ScanForCombatTargets(senseCenter, radius);
                }
            }
            else if (!this.hasPriorityTarget)
            {
                //#if DEBUG
                //                if (this.State == AIState.Intercept && this.Target != null)
                //                    System.Diagnostics.Debug.WriteLine(this.Target); 
                //#endif
                if (this.Owner.Mothership != null)
                {
                    this.Target = this.ScanForCombatTargets(senseCenter, radius);

                    if (this.Target == null)
                    {
                        this.Target = this.Owner.Mothership.GetAI().Target;
                    }
                    
                        
                }
                else
                this.Target = this.ScanForCombatTargets(senseCenter, radius);
            }
            else
            {

                if (this.Owner.Mothership != null)
                {
                    this.Target = this.ScanForCombatTargets(senseCenter, radius);
                    if (this.Target == null)
                    {
                        this.Target = this.Owner.Mothership.GetAI().Target;
                    }
                }
                else
                    this.ScanForCombatTargets(senseCenter, radius);
            }
            if (this.State == AIState.Resupply)
            {
                return;
            }
            if ((((this.Owner.shipData.Role == ShipData.RoleName.freighter || this.Owner.shipData.ShipCategory == ShipData.Category.Civilian) && this.Owner.CargoSpace_Max > 0) || this.Owner.shipData.Role == ShipData.RoleName.scout || this.Owner.isConstructor || this.Owner.shipData.Role == ShipData.RoleName.troop || this.IgnoreCombat || this.State == AIState.Resupply || this.State == AIState.ReturnToHangar || this.State == AIState.Colonize) || this.Owner.shipData.Role == ShipData.RoleName.supply)
            {
                return;
            }
            if (this.Owner.fleet != null && this.State == AIState.FormationWarp)
            {
                bool doreturn = true;
                if (this.Owner.fleet != null && this.State == AIState.FormationWarp && Vector2.Distance(this.Owner.Center, this.Owner.fleet.Position + this.Owner.FleetOffset) < 15000f)
                {
                    doreturn = false;
                }
                if (doreturn)
                {
                    //if (this.Owner.engineState == Ship.MoveState.Sublight && this.NearbyShips.Count > 0)
                    //{
                    //    this.Owner.ShieldsUp = true;
                    //}
                    return;
                }
            }
            if (this.Owner.fleet != null)
            {
                foreach (FleetDataNode datanode in this.Owner.fleet.DataNodes)
                {
                    if (datanode.GetShip() != this.Owner)
                    {
                        continue;
                    }
                    this.node = datanode;
                    break;
                }
            }
            if (this.Target != null && !this.Owner.InCombat)
            {
                this.Owner.InCombatTimer = 15f;
                if (!this.HasPriorityOrder && this.OrderQueue.Count > 0 && this.OrderQueue.ElementAt<ArtificialIntelligence.ShipGoal>(0).Plan != ArtificialIntelligence.Plan.DoCombat)
                {
                    ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
                    this.State = AIState.Combat;
                    this.OrderQueue.AddFirst(combat);
                    return;
                }
                else if (!this.HasPriorityOrder)
                {
                    ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
                    this.State = AIState.Combat;
                    this.OrderQueue.AddFirst(combat);
                    return;
                }
                else 
                {
                    if (!this.HasPriorityOrder || this.CombatState == CombatState.HoldPosition || this.OrderQueue.Count != 0)
                        return;
                    ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
                    this.State = AIState.Combat;
                    this.OrderQueue.AddFirst(combat);
                    
                }
            }
        }

		private void ScrapShip(float elapsedTime, ArtificialIntelligence.ShipGoal goal)
		{
            if (Vector2.Distance(goal.TargetPlanet.Position, this.Owner.Center) >= goal.TargetPlanet.ObjectRadius + this.Owner.Radius)   //2500f)   //OrbitTarget.ObjectRadius *15)
			{
                //goal.MovePosition = goal.TargetPlanet.Position;
                //this.MoveToWithin1000(elapsedTime, goal);
                //goal.SpeedLimit = this.Owner.GetSTLSpeed();
                this.DoOrbit(goal.TargetPlanet, elapsedTime);
				return;
			}
			this.OrderQueue.Clear();
			Planet targetPlanet = goal.TargetPlanet;
			targetPlanet.ProductionHere = targetPlanet.ProductionHere + this.Owner.GetCost(this.Owner.loyalty) / 2f;
			this.Owner.QueueTotalRemoval();
            this.Owner.loyalty.GetGSAI().recyclepool++;
		}

		private void SetCombatStatusorig(float elapsedTime)
		{
			if (this.Owner.fleet != null)
			{
				if (!this.hasPriorityTarget)
				{
					this.Target = this.ScanForCombatTargets(this.Owner.Center, 30000f);
				}
				else
				{
					this.ScanForCombatTargets(this.Owner.Center, 30000f);
				}
			}
			else if (!this.hasPriorityTarget)
			{
				this.Target = this.ScanForCombatTargets(this.Owner.Center, 30000f);
			}
			else
			{
				this.ScanForCombatTargets(this.Owner.Center, 30000f);
			}
			if (this.State == AIState.Resupply)
			{
				return;
			}
            if ((this.Owner.shipData.Role == ShipData.RoleName.freighter || this.Owner.shipData.ShipCategory == ShipData.Category.Civilian || this.Owner.shipData.Role == ShipData.RoleName.scout || this.Owner.isConstructor || this.Owner.shipData.Role == ShipData.RoleName.troop || this.IgnoreCombat || this.State == AIState.Resupply || this.State == AIState.ReturnToHangar) && !this.Owner.IsSupplyShip)
			{
				return;
			}
			if (this.Owner.fleet != null && this.State == AIState.FormationWarp)
			{
				bool doreturn = true;
				if (this.Owner.fleet != null && this.State == AIState.FormationWarp && Vector2.Distance(this.Owner.Center, this.Owner.fleet.Position + this.Owner.FleetOffset) < 15000f)
				{
					doreturn = false;
				}
				if (doreturn)
				{
					return;
				}
			}
			if (this.Owner.fleet != null)
			{
				foreach (FleetDataNode datanode in this.Owner.fleet.DataNodes)
				{
					if (datanode.GetShip() != this.Owner)
					{
						continue;
					}
					this.node = datanode;
					break;
				}
			}
			if (this.Target != null && !this.Owner.InCombat)
			{
				this.Owner.InCombat = true;
				this.Owner.InCombatTimer = 15f;
				if (!this.HasPriorityOrder && this.OrderQueue.Count > 0 && this.OrderQueue.ElementAt<ArtificialIntelligence.ShipGoal>(0).Plan != ArtificialIntelligence.Plan.DoCombat)
				{
					ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
					this.State = AIState.Combat;
					this.OrderQueue.AddFirst(combat);
					return;
				}
				if (!this.HasPriorityOrder)
				{
					ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
					this.State = AIState.Combat;
					this.OrderQueue.AddFirst(combat);
					return;
				}
				if (this.HasPriorityOrder && this.CombatState != Ship_Game.Gameplay.CombatState.HoldPosition && this.OrderQueue.Count == 0)
				{
					ArtificialIntelligence.ShipGoal combat = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f);
					this.State = AIState.Combat;
					this.OrderQueue.AddFirst(combat);
					return;
				}
			}
			else if (this.Target == null)
			{
				this.Owner.InCombat = false;
			}
		}

		public void SetPriorityOrder()
		{
			this.OrderQueue.Clear();
			this.HasPriorityOrder = true;
			this.Intercepting = false;
			this.hasPriorityTarget = false;
		}

		private void Stop(float elapsedTime)
		{
			this.Owner.HyperspaceReturn();
			if (this.Owner.Velocity == Vector2.Zero || this.Owner.Velocity.Length() > this.Owner.VelocityLast.Length())
			{
				this.Owner.Velocity = Vector2.Zero;
				return;
			}
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			if (this.Owner.Velocity.Length() / this.Owner.velocityMaximum <= elapsedTime || (forward.X <= 0f || this.Owner.Velocity.X <= 0f) && (forward.X >= 0f || this.Owner.Velocity.X >= 0f))
			{
				this.Owner.Velocity = Vector2.Zero;
				return;
			}
			Ship owner = this.Owner;
			owner.Velocity = owner.Velocity + (Vector2.Normalize(-forward) * (elapsedTime * this.Owner.velocityMaximum));
		}

		private void Stop(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
		{
			this.Owner.HyperspaceReturn();
			if (this.Owner.Velocity == Vector2.Zero || this.Owner.Velocity.Length() > this.Owner.VelocityLast.Length())
			{
				this.Owner.Velocity = Vector2.Zero;
				this.OrderQueue.RemoveFirst();
				return;
			}
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			if (this.Owner.Velocity.Length() / this.Owner.velocityMaximum <= elapsedTime || (forward.X <= 0f || this.Owner.Velocity.X <= 0f) && (forward.X >= 0f || this.Owner.Velocity.X >= 0f))
			{
				this.Owner.Velocity = Vector2.Zero;
				return;
			}
			Ship owner = this.Owner;
			owner.Velocity = owner.Velocity + (Vector2.Normalize(-forward) * (elapsedTime * this.Owner.velocityMaximum));
		}

		private void StopWithBackwardsThrust(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
		{
			if(Goal.TargetPlanet !=null)
            {
                lock (this.wayPointLocker)
                {
                    this.ActiveWayPoints.Last().Equals(Goal.TargetPlanet.Position);
                    Goal.MovePosition = Goal.TargetPlanet.Position;
                }
            }
            if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
			{
				this.HadPO = true;
			}
			this.HasPriorityOrder = false;
			float Distance = Vector2.Distance(this.Owner.Center, Goal.MovePosition);
			//if (Distance < 100f && Distance < 25f)
            if (Distance < 200f)  //fbedard
			{
				this.OrderQueue.RemoveFirst();
				lock (this.wayPointLocker)
				{
					this.ActiveWayPoints.Clear();
				}
				this.Owner.Velocity = Vector2.Zero;
				if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
				{
					this.HadPO = true;
				}
				this.HasPriorityOrder = false;
			}
			this.Owner.HyperspaceReturn();
            //Vector2 forward2 = Quaternion
            //Quaternion.AngleAxis(_angle, Vector3.forward) * normalizedDirection1
			Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
			if (this.Owner.Velocity == Vector2.Zero || Vector2.Distance(this.Owner.Center + (this.Owner.Velocity * elapsedTime), Goal.MovePosition) > Vector2.Distance(this.Owner.Center, Goal.MovePosition))
			{
				this.Owner.Velocity = Vector2.Zero;
				this.OrderQueue.RemoveFirst();
				if (this.ActiveWayPoints.Count > 0)
				{
					lock (this.wayPointLocker)
					{
						this.ActiveWayPoints.Dequeue();
					}
				}
				return;
			}
			Vector2 velocity = this.Owner.Velocity;
			float timetostop = velocity.Length() / Goal.SpeedLimit;
            //added by gremlin devekmod timetostopfix
            if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <= timetostop + .005) 
            //if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / (this.Owner.Velocity.Length() + 0.001f) <= timetostop)
			{
				Ship owner = this.Owner;
				owner.Velocity = owner.Velocity + (Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit));
				if (this.Owner.Velocity.Length() > Goal.SpeedLimit)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * Goal.SpeedLimit;
				}
			}
			else
			{
				Ship ship = this.Owner;
				ship.Velocity = ship.Velocity + (Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit));
				if (this.Owner.Velocity.Length() > Goal.SpeedLimit)
				{
					this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * Goal.SpeedLimit;
					return;
				}
			}
		}
        private void StopWithBackwardsThrustbroke(float elapsedTime, ArtificialIntelligence.ShipGoal Goal)
        {
            
            if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
            {
                this.HadPO = true;
            }
            this.HasPriorityOrder = false;
            float Distance = Vector2.Distance(this.Owner.Center, Goal.MovePosition);
            if (Distance < 200 )//&& Distance > 25f)
            {
                this.OrderQueue.RemoveFirst();
                lock (this.wayPointLocker)
                {
                    this.ActiveWayPoints.Clear();
                }
                this.Owner.Velocity = Vector2.Zero;
                if (this.Owner.loyalty == EmpireManager.GetEmpireByName(ArtificialIntelligence.universeScreen.PlayerLoyalty))
                {
                    this.HadPO = true;
                }
                this.HasPriorityOrder = false;
            }
            this.Owner.HyperspaceReturn();
            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
            if (this.Owner.Velocity == Vector2.Zero || Vector2.Distance(this.Owner.Center + ( this.Owner.Velocity * elapsedTime), Goal.MovePosition) > Vector2.Distance(this.Owner.Center, Goal.MovePosition))
            {
                this.Owner.Velocity = Vector2.Zero;
                this.OrderQueue.RemoveFirst();
                if (this.ActiveWayPoints.Count > 0)
                {
                    lock (this.wayPointLocker)
                    {
                        this.ActiveWayPoints.Dequeue();
                    }
                }
                return;
            }
            Vector2 velocity = this.Owner.Velocity;
            float timetostop = (int)velocity.Length() / Goal.SpeedLimit;
            if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / Goal.SpeedLimit <= timetostop + .005) //(this.Owner.Velocity.Length() + 1)
                if (Math.Abs((int)(DistanceLast - Distance)) < 10)
                {

                    ArtificialIntelligence.ShipGoal to1k = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.MakeFinalApproach, Goal.MovePosition, 0f)
                                    {
                                        SpeedLimit = this.Owner.speed > Distance ? Distance : this.Owner.GetSTLSpeed()
                                    };
                    lock (this.wayPointLocker)
                        this.OrderQueue.AddFirst(to1k);
                    this.DistanceLast = Distance;
                    return;
                }
            if (Vector2.Distance(this.Owner.Center, Goal.MovePosition) / (this.Owner.Velocity.Length() + 0.001f) <= timetostop)
            {
                Ship owner = this.Owner;
                owner.Velocity = owner.Velocity + (Vector2.Normalize(-forward) * (elapsedTime * Goal.SpeedLimit));
                if (this.Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * Goal.SpeedLimit;
                }
            }
            else
            {
                Ship ship = this.Owner;
                ship.Velocity = ship.Velocity + (Vector2.Normalize(forward) * (elapsedTime * Goal.SpeedLimit));
                if (this.Owner.Velocity.Length() > Goal.SpeedLimit)
                {
                    this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * Goal.SpeedLimit;
                    return;
                }
            }

            this.DistanceLast = Distance;
        }
		
        private void ThrustTowardsPosition(Vector2 Position, float elapsedTime, float speedLimit)
        {
            if (speedLimit == 0f)
                speedLimit = this.Owner.speed;
            float Ownerspeed = this.Owner.speed;
            if (Ownerspeed > speedLimit)
                Ownerspeed = speedLimit;
            float Distance = Vector2.Distance(Position, this.Owner.Center);
 
            if (this.Owner.engineState != Ship.MoveState.Warp )
            {
                Position = Position - this.Owner.Velocity;
            }
            if (!this.Owner.EnginesKnockedOut)
            {
                this.Owner.isThrusting = true;

                Vector2 wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, Position));
                //wantedForward = Vector2.Normalize(wantedForward);
                Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
                //forward = Vector2.Normalize(forward);
                Vector2 right = new Vector2(-forward.Y, forward.X);
                //right = Vector2.Normalize(right);
                double angleDiff = Math.Acos((double)Vector2.Dot(wantedForward, forward));
                double facing = (Vector2.Dot(wantedForward, right)> 0f ? 1f : -1f);
                //facing = facing/(mag1*mag2);

                #region warp
                if (angleDiff > 0.25f && Distance > 2500f && this.Owner.engineState == Ship.MoveState.Warp)
                {
                    //this.Owner.speed *= 0.999f;
                    if (this.ActiveWayPoints.Count > 1)
                    {
                        //wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, this.ActiveWayPoints.ElementAt<Vector2>(1)));
                        //float angleDiffToNext = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
                        //float d = Vector2.Distance(this.Owner.Position, this.ActiveWayPoints.ElementAt<Vector2>(1));
                        //if (d <= this.Owner.velocityMaximum)
                        if (Distance <= Empire.ProjectorRadius / 2f)
                        {
                            //if (angleDiffToNext > 0.4f)// 0.649999976158142) //  )
                            if (angleDiff > 0.4f)// 0.649999976158142) //  )
                            {
                                this.Owner.HyperspaceReturn();
                            }
                            else  //fbedard: 2nd attempt to smooth movement around waypoints
                            {
                                lock (this.wayPointLocker)
                                    this.ActiveWayPoints.Dequeue();
                                if (this.OrderQueue.Count > 0)
                                    this.OrderQueue.RemoveFirst();
                                Position = this.ActiveWayPoints.First();
                                Distance = Vector2.Distance(Position, this.Owner.Center);
                                wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, Position));
                                forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
                                angleDiff = Math.Acos((double)Vector2.Dot(wantedForward, forward));
                            }
                        }
                        //else if (d > 50000f && angleDiffToNext > 1.64999997615814f)
                        else if (Distance <= Empire.ProjectorRadius && angleDiff <= 1.2f) //fbedard: 2nd attempt to smooth movement around waypoints
                        {
                            lock (this.wayPointLocker)
                                this.ActiveWayPoints.Dequeue();
                            if (this.OrderQueue.Count > 0)
                                this.OrderQueue.RemoveFirst();
                            Position = this.ActiveWayPoints.First();
                            Distance = Vector2.Distance(Position, this.Owner.Center);
                            wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, Position));
                            forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
                            angleDiff = Math.Acos((double)Vector2.Dot(wantedForward, forward));
                        }
                        else if (angleDiff > 1.2f)
                        {
                            this.Owner.HyperspaceReturn();
                        }
                    }
                    else if (this.Target != null)
                    {
                        float d = Vector2.Distance(this.Target.Center, this.Owner.Center);
                        if (angleDiff > 0.400000005960464f)
                        {
                            this.Owner.HyperspaceReturn();
                        }
                        else if (d > 25000f)
                        {
                            this.Owner.HyperspaceReturn();
                        }
                    }
                    else if ((this.State != AIState.Bombard && this.State != AIState.AssaultPlanet && this.State != AIState.BombardTroops && !this.IgnoreCombat) || this.OrderQueue.Count <= 0)
                    {
                        this.Owner.HyperspaceReturn();
                    }
                    else if (this.OrderQueue.Last<ArtificialIntelligence.ShipGoal>().TargetPlanet != null)
                    {
                        float d = Vector2.Distance(this.OrderQueue.Last<ArtificialIntelligence.ShipGoal>().TargetPlanet.Position, this.Owner.Center);
                        wantedForward = Vector2.Normalize(HelperFunctions.FindVectorToTarget(this.Owner.Center, this.OrderQueue.Last<ArtificialIntelligence.ShipGoal>().TargetPlanet.Position));
                        angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
                        //float d = Vector2.Distance(this.Owner.Position, this.ActiveWayPoints.ElementAt<Vector2>(1));
                        //if (angleDiff > 0.65f)

                        if (angleDiff > 0.400000005960464f)
                        {
                            this.Owner.HyperspaceReturn();
                        }
                        else if (d > 25000f)
                        {
                            this.Owner.HyperspaceReturn();
                        }
                    }
                    else if (angleDiff > .25)
                        this.Owner.HyperspaceReturn();
                }
                #endregion

                if (this.hasPriorityTarget && Distance < this.Owner.maxWeaponsRange)
                {
                    if (this.Owner.engineState == Ship.MoveState.Warp)
                    {
                        this.Owner.HyperspaceReturn();
                    }
                }
                else if (!this.HasPriorityOrder && !this.hasPriorityTarget && Distance < 1000f && this.ActiveWayPoints.Count <= 1 && this.Owner.engineState == Ship.MoveState.Warp)
                {
                    this.Owner.HyperspaceReturn();
                }
                //if (angleDiff > 0.125000000372529f)
                //    if (this.Owner.engineState == Ship.MoveState.Warp)
                //        this.Owner.speed *= .8f;
                float TurnSpeed = 1;
                if (angleDiff > this.Owner.yBankAmount*.1) // this.Owner.rotationRadiansPerSecond *elapsedTime*.1 ) //0.025000000372529f )//*
                {

                    double RotAmount = Math.Min(angleDiff, facing *  this.Owner.yBankAmount); //this.Owner.rotationRadiansPerSecond * elapsedTime);
                    //RotAmount *= facing;
                    if (RotAmount > 0f)
                    {
                        
                        if (this.Owner.yRotation > -this.Owner.maxBank)
                        {
                            
                            Ship owner = this.Owner;
                            owner.yRotation = owner.yRotation - this.Owner.yBankAmount;
                        }
                    }
                    else if (RotAmount < 0f && this.Owner.yRotation < this.Owner.maxBank)
                    {
                        
                        Ship owner1 = this.Owner;
                        owner1.yRotation = owner1.yRotation + this.Owner.yBankAmount;
                        
                    }

                    this.Owner.isTurning = true;
                    Ship rotation = this.Owner;
                    rotation.Rotation = rotation.Rotation + (RotAmount > angleDiff ? (float)angleDiff: (float)RotAmount);

                    //if (RotAmount > .05f)
                    {
                        float nimble = this.Owner.rotationRadiansPerSecond;// >1?1:this.Owner.rotationRadiansPerSecond;
                        if (angleDiff < nimble)
                            TurnSpeed = (float)((nimble*1.5 -angleDiff )/(nimble*1.5));     //(float)RotAmount / (this.Owner.rotationRadiansPerSecond * elapsedTime);

                        else
                            return;
                    }

                   
                }
                if (this.State != AIState.FormationWarp || this.Owner.fleet == null)
                {
                    if (Distance > 7500f && !this.Owner.InCombat && angleDiff < 0.25f)
                    {
                        this.Owner.EngageStarDrive();
                    }
                    else if (Distance > 15000f && this.Owner.InCombat && angleDiff < 0.25f)
                    {

                        this.Owner.EngageStarDrive();
                    }
                    if (this.Owner.engineState == Ship.MoveState.Warp)
                    {
                        if (angleDiff > .1f)
                        {

                            speedLimit = Ownerspeed; // this.Owner.speed;
                        }
                        else
                            speedLimit = (int)(this.Owner.velocityMaximum);
                    }
                    else if (Distance > Ownerspeed * 10f)
                    {
                        //if (angleDiff > .1f)
                        //    speedLimit = this.Owner.speed;
                        //else
                        speedLimit = Ownerspeed;
                    }
                    speedLimit *= TurnSpeed;
                    Ship velocity = this.Owner;
                    velocity.Velocity = velocity.Velocity +   (Vector2.Normalize(forward) * (elapsedTime * speedLimit));//((forward) * (elapsedTime * speedLimit));
                    if (this.Owner.Velocity.Length() > speedLimit)
                    {
                        this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * speedLimit; //(this.Owner.Velocity) * speedLimit; //
                    }

                }
                else
                {
                    if (Distance > 7500f)
                    //if(Distance > this.Owner.speed -1000)
                    {
                        bool fleetReady = true;
                        this.Owner.fleet.Ships.thisLock.EnterReadLock();
                        foreach (Ship ship in this.Owner.fleet.Ships)
                        {
                            if(ship.GetAI().State != AIState.FormationWarp)
                                continue;
                            if (ship.GetAI().ReadyToWarp
                                
                                && (ship.PowerCurrent / (ship.PowerStoreMax + 0.01f) >= 0.2f || ship.isSpooling ) 
                                
                                
                                )
                            {
                                if (this.Owner.FightersOut)
                                    this.Owner.RecoverFighters();
                                
                                continue;
                            }

                            fleetReady = false;
                            break;
                        }
                        this.Owner.fleet.Ships.thisLock.ExitReadLock();
     
                            float distanceFleetCenterToDistance = this.Owner.fleet.StoredFleetDistancetoMove; //
                            speedLimit = (this.Owner.fleet.speed);

                            #region FleetGrouping


                            float fleetPosistionDistance = Distance;// Vector2.Distance(this.Owner.Center, Position);
                            if (fleetPosistionDistance <= distanceFleetCenterToDistance )
                            {
                                float speedreduction = distanceFleetCenterToDistance - Distance;
                                speedLimit = (int)( this.Owner.fleet.speed - speedreduction); //this.Owner.fleet.speed 
                                if (speedLimit < 0)//this.Owner.fleet.speed * .25f
                                    speedLimit = 0; //this.Owner.fleet.speed * .25f;
                                else if (speedLimit > this.Owner.fleet.speed)
                                    speedLimit = (int)(this.Owner.fleet.speed);
                            }
                            //else if (Distance > distanceFleetCenterToDistance) //radius * 4f
                            else if (fleetPosistionDistance > distanceFleetCenterToDistance && Distance > Ownerspeed)
                            {

                                float speedIncrease = Distance - distanceFleetCenterToDistance ;
                                //distanceShipToFleetCenter > this.Owner.fleet.speed && 
                                speedLimit = (int)(this.Owner.fleet.speed + speedIncrease);
  
                            }
                            //if (Distance < speedimit)
                            //    speedLimit = Distance;



                            #endregion




                            if (fleetReady)
                            {
                                this.Owner.EngageStarDrive();
                            }
                            else if (this.Owner.engineState == Ship.MoveState.Warp)
                            {
                                this.Owner.HyperspaceReturn();
                            }
                    }
                    else if (this.Owner.engineState == Ship.MoveState.Warp)
                    {
                        this.Owner.HyperspaceReturn();
                    }

                    if (speedLimit > this.Owner.velocityMaximum)
                        speedLimit = (this.Owner.velocityMaximum);
                    else if (speedLimit < 0)
                        speedLimit = 0;
                    //if (Distance < 100000)
                    //    speedLimit *= .5f;
                    Ship velocity1 = this.Owner;
                    velocity1.Velocity = velocity1.Velocity + (Vector2.Normalize(forward) * (elapsedTime * (speedLimit)));
                    if (this.Owner.Velocity.Length() > speedLimit)
                    {
                        this.Owner.Velocity = Vector2.Normalize(this.Owner.Velocity) * speedLimit;
                        return;
                    }
                }
            }
        }



        //added by gremlin Devekmod AuUpdate(fixed)
        public void Update(float elapsedTime)
        {
            if(this.BadGuysNear)
            this.CombatAI.UpdateCombatAI(this.Owner);
            ArtificialIntelligence.ShipGoal toEvaluate;
            if (this.State == AIState.AwaitingOrders && this.DefaultAIState == AIState.Exterminate)
                this.State = AIState.Exterminate;
            if (this.ClearOrdersNext)
            {
                this.OrderQueue.Clear();
                this.ClearOrdersNext = false;
                this.awaitClosest = null;
                this.State = AIState.AwaitingOrders;
            }
            List<Ship> ToRemove = new List<Ship>();
            foreach (Ship target in this.TargetQueue)
            {
                if (target.Active)
                {
                    continue;
                }
                ToRemove.Add(target);
            }
            foreach (Ship ship in ToRemove)
            {
                this.TargetQueue.Remove(ship);
            }
            if (!this.hasPriorityTarget)
                this.TargetQueue.Clear();
            if (this.Owner.loyalty == ArtificialIntelligence.universeScreen.player && (this.State == AIState.MoveTo && Vector2.Distance(this.Owner.Center, this.MovePosition) > 100f || this.State == AIState.Orbit || (this.State == AIState.Bombard || this.State == AIState.AssaultPlanet || this.State == AIState.BombardTroops) || this.State == AIState.Rebase || this.State == AIState.Scrap || this.State == AIState.Resupply || this.State == AIState.Refit || this.State == AIState.FormationWarp))
            {
                this.HasPriorityOrder = true;
                this.HadPO = false;
                this.EscortTarget = null;
                
            }
            if (HadPO && this.State != AIState.AwaitingOrders)
                HadPO = false;
            if (this.State == AIState.Resupply)
            {
                this.HasPriorityOrder = true;
				if (this.Owner.Ordinance >= this.Owner.OrdinanceMax && this.Owner.Health >= this.Owner.HealthMax)  //fbedard: consider health also
                {
                    this.HasPriorityOrder = false;
                    this.State = AIState.AwaitingOrders;
                }
            }
            //fbedard: Put back flee! (resupply order with nowhere to go)
            if (this.State == AIState.Flee && !this.BadGuysNear && this.State != AIState.Resupply && !this.HasPriorityOrder) // && Vector2.Distance(this.OrbitTarget.Position, this.Owner.Position) < this.Owner.SensorRange + 10000f)
            {
                if(this.OrderQueue.Count > 0)
                    this.OrderQueue.Remove(this.OrderQueue.Last);
                if (this.FoodOrProd == "Pass")
                    this.State = AIState.PassengerTransport;
                else if (this.FoodOrProd == "Food" || this.FoodOrProd == "Prod")
                    this.State = AIState.SystemTrader;
                else
                    this.State = this.DefaultAIState;
            }
            this.ScanForThreatTimer -=  elapsedTime;
            if (this.ScanForThreatTimer < 0f)
            {
                //if (this.inOrbit == true )//&& !(this.State == AIState.Orbit ||                     this.State == AIState.Flee))
                //{
                //    this.inOrbit = false;
                //}
                this.SetCombatStatus(elapsedTime);
                this.ScanForThreatTimer = 2f;
                if (this.Owner.loyalty.data.Traits.Pack)
                {
                    this.Owner.DamageModifier = -0.25f;
                    Ship owner = this.Owner;
                    owner.DamageModifier = owner.DamageModifier + 0.05f * (float)this.FriendliesNearby.Count;
                    if (this.Owner.DamageModifier > 0.5f)
                    {
                        this.Owner.DamageModifier = 0.5f;
                    }
                }
            }
            this.UtilityModuleCheckTimer -= elapsedTime;
            if (this.Owner.engineState != Ship.MoveState.Warp && this.UtilityModuleCheckTimer <= 0f)
            {
                this.UtilityModuleCheckTimer = 1f;
                //Added by McShooterz: logic for transporter modules
                if (this.Owner.hasTransporter)
                {
                    foreach (ShipModule module in this.Owner.Transporters)
                    {
                        if (module.TransporterTimer <= 0f && module.Active && module.Powered && module.TransporterPower < this.Owner.PowerCurrent)
                        {
                            if (this.FriendliesNearby.Count > 0 && module.TransporterOrdnance > 0 && this.Owner.Ordinance > 0)
                                this.DoOrdinanceTransporterLogic(module);
                            if (module.TransporterTroopAssault > 0 && this.Owner.TroopList.Count() > 0)
                                this.DoAssaultTransporterLogic(module);
                        }
                    }
                }
                //Do repair check if friendly ships around and no combat
                if (!this.Owner.InCombat && this.FriendliesNearby.Count > 0)
                {
                    //Added by McShooterz: logic for repair beams
                    if (this.Owner.hasRepairBeam)
                    {
                        foreach (ShipModule module in this.Owner.RepairBeams)
                        {
                            if (module.InstalledWeapon.timeToNextFire <= 0f && module.InstalledWeapon.moduleAttachedTo.Powered && this.Owner.Ordinance >= module.InstalledWeapon.OrdinanceRequiredToFire && this.Owner.PowerCurrent >= module.InstalledWeapon.PowerRequiredToFire)
                            {
                                this.DoRepairBeamLogic(module.InstalledWeapon);
                            }
                        }
                    }
                    if (this.Owner.HasRepairModule)
                    {
                        foreach (Weapon weapon in this.Owner.Weapons)
                        {
                            if (weapon.timeToNextFire > 0f || !weapon.moduleAttachedTo.Powered || this.Owner.Ordinance < weapon.OrdinanceRequiredToFire || this.Owner.PowerCurrent < weapon.PowerRequiredToFire || !weapon.IsRepairDrone)
                            {
                                //Gretman -- Added this so repair drones would cooldown outside combat (+15s)
                                if (weapon.timeToNextFire > 0f) weapon.timeToNextFire = MathHelper.Max(weapon.timeToNextFire - 1, 0f);
                                continue;
                            }
                            this.DoRepairDroneLogic(weapon);
                        }
                    }
                }
            }
            if (this.State == AIState.ManualControl)
            {
                return;
            }
            this.ReadyToWarp = true;
            this.Owner.isThrusting = false;
            this.Owner.isTurning = false;
            
            #region old flee code
            //if (!this.HasPriorityOrder 
            //    && (this.BadGuysNear || this.Owner.InCombat) 
            //    && (this.Owner.shipData == null || this.Owner.shipData.ShipCategory == ShipData.Category.Civilian) 
            //    && this.Owner.Weapons.Count == 0 && this.Owner.GetHangars().Count == 0 && this.Owner.Transporters.Count() == 0
            //    && (this.Owner.Role !=ShipData.RoleName.troop 
            //    && this.Owner.Role != ShipData.RoleName.construction 
            //    && this.State !=AIState.Colonize 
            //    && !this.IgnoreCombat && this.State != AIState.Rebase) 
            //    && (this.Owner.Role == ShipData.RoleName.freighter || this.Owner.fleet == null || this.Owner.Mothership != null))
            //{
            //    if (this.State != AIState.Flee )
            //    {
            //        this.HasPriorityOrder = true;
            //        this.State = AIState.Flee;
            //        if (this.State == AIState.Flee)
            //        {
            //            this.OrderFlee(false);
            //            this.Owner.InCombatTimer = 15f;

            //        }
            //    }
            //    else if (this.State == AIState.Flee && (this.OrbitTarget != null && Vector2.Distance(this.OrbitTarget.Position, this.Owner.Position) < this.Owner.SensorRange + 10000))
            //    {
            //        this.State = this.DefaultAIState;
            //    }
            //} 
            #endregion

            if (this.State == AIState.SystemTrader && this.start != null && this.end != null && (this.start.Owner != this.Owner.loyalty || this.end.Owner != this.Owner.loyalty))
            {
                this.start = null;
                this.end = null;
                this.OrderTrade(5f);
                return;
            }
            if (this.State == AIState.PassengerTransport && this.start != null && this.end != null && (this.start.Owner != this.Owner.loyalty || this.end.Owner != this.Owner.loyalty))
            {
                this.start = null;
                this.end = null;
                this.OrderTransportPassengers(5f);
                return;
            }
#if !DEBUG
            try
            {
#endif
                if (this.OrderQueue.Count == 0)
                {
                    if (this.Owner.fleet == null)
                    {
                        lock (this.wayPointLocker)
                        {
                            this.ActiveWayPoints.Clear();
                        }
                        AIState state = this.State;
                        if (state <= AIState.MoveTo)
                        {
                            if (state <= AIState.SystemTrader)
                            {
                                if (state == AIState.DoNothing)
                                {
                                    this.AwaitOrders(elapsedTime);
                                }
                                else
                                {
                                    switch (state)
                                    {
                                        case AIState.AwaitingOrders:
                                            {
                                                if (this.Owner.loyalty != ArtificialIntelligence.universeScreen.player)
                                                {
                                                    this.AwaitOrders(elapsedTime);
                                                }
                                                else
                                                {
                                                    this.AwaitOrdersPlayer(elapsedTime);
                                                }
                                                if (this.Owner.loyalty.isFaction)
                                                    break;
                                                //fbedard: resume trading
                                                //if (this.FoodOrProd == "Pass")
                                                //    this.State = AIState.PassengerTransport;
                                                //else if (this.FoodOrProd == "Food" || this.FoodOrProd == "Prod")
                                                //    this.State = AIState.SystemTrader;
                                                if (this.Owner.OrdinanceMax <1 ||  this.Owner.Ordinance / this.Owner.OrdinanceMax >= 0.2f) 
                                                    
                                                {
                                                    break;
                                                }
                                                if (FriendliesNearby.Where(supply => supply.HasSupplyBays && supply.Ordinance >= 100).Count() > 0)
                                                {
                                                    break;
                                                }
                                                List<Planet> shipyards = new List<Planet>();
                                                for (int i = 0; i < this.Owner.loyalty.GetPlanets().Count; i++)
                                                {
                                                    Planet item = this.Owner.loyalty.GetPlanets()[i];
                                                    if (item.HasShipyard)
                                                    {
                                                        shipyards.Add(item);
                                                    }
                                                }
                                                IOrderedEnumerable<Planet> sortedList =
                                                    from p in shipyards
                                                    orderby Vector2.Distance(this.Owner.Center, p.Position)
                                                    select p;
                                                if (sortedList.Count<Planet>() <= 0)
                                                {
                                                    break;
                                                }
                                                this.OrderResupply(sortedList.First<Planet>(), true);
                                                break;
                                            }
                                        case AIState.Escort:
                                            {
                                                if (this.EscortTarget == null || !this.EscortTarget.Active)
                                                    {
                                                        this.EscortTarget = null;    
                                                        this.OrderQueue.Clear();
                                                        this.ClearOrdersNext = false;
                                                        if (this.Owner.Mothership != null && this.Owner.Mothership.Active)
                                                        {
                                                            this.OrderReturnToHangar();
                                                            break;
                                                        }
                                                        this.State = AIState.AwaitingOrders;   //fbedard
                                                        break;
                                                }
                                                if (this.Owner.BaseStrength ==0 || ( this.Owner.Mothership == null && Vector2.Distance(this.EscortTarget.Center,this.Owner.Center) > this.Owner.SensorRange) || this.Owner.Mothership == null || ( !this.Owner.Mothership.GetAI().BadGuysNear ||this.EscortTarget != this.Owner.Mothership))
                                                {
                                                    this.OrbitShip(this.EscortTarget, elapsedTime);
                                                    break;
                                                }
                                                // Doctor: This should make carrier-launched fighters scan for their own combat targets, except using the mothership's position
                                                // and a standard 30k around it instead of their own. This hopefully will prevent them flying off too much, as well as keeping them
                                                // in a carrier-based role while allowing them to pick appropriate target types depending on the fighter type.
                                                //gremlin Moved to setcombat status as target scan is expensive and did some of this already. this also shortcuts the UseSensorforTargets switch. Im not sure abuot the using the mothership target. 
                                                // i thought i had added that in somewhere but i cant remember where. I think i made it so that in the scan it takes the motherships target list and adds it to its own. 
                                                else
                                                {
                                                //if (this.Owner.Mothership != null && this.Target == null)
                                                //{
                                                //    this.ScanForCombatTargets(this.Owner.Mothership.Center, 30000f);
                                                //    if (this.Target == null)
                                                //    {
                                                //        this.Target = this.Owner.Mothership.GetAI().Target;
                                                //    }
                                                //}
                                                this.DoCombat(elapsedTime);
                                                    break;
                                                }
                                            }
                                        case AIState.SystemTrader:
                                            {
                                                this.OrderTrade(elapsedTime);
                                            if (this.start == null || this.end == null)
                                            {
                                                
                                                this.AwaitOrders(elapsedTime);
                                            }
                                                break;
                                            }
                                    }
                                }
                            }
                            else if (state == AIState.PassengerTransport)
                            {
                                this.OrderTransportPassengers(elapsedTime);
                                if (this.start == null || this.end == null)
                                    this.AwaitOrders(elapsedTime);
                            }
                        }
                        else if (state <= AIState.ReturnToHangar)
                        {
                            switch (state)
                            {
                                case AIState.SystemDefender:
                                    {
                                        this.AwaitOrders(elapsedTime);
                                        break;
                                    }
                                case AIState.AwaitingOffenseOrders:
                                    {
                                        break;
                                    }
                                case AIState.Resupply:
                                {
                                    this.AwaitOrders(elapsedTime);
                                    break;
                                }
                                default:
                                    {
                                        if (state == AIState.ReturnToHangar)
                                        {
                                            this.DoReturnToHangar(elapsedTime);
                                            break;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                            }
                        }
                        else if (state != AIState.Intercept)
                        {
                            if (state == AIState.Exterminate)
                            {
                                this.OrderFindExterminationTarget(true);
                            }
                        }
                        else if (this.Target != null)
                        {
                            this.OrbitShip(this.Target as Ship, elapsedTime);
                        }
                    }
                    else
                    {
                        float DistanceToFleetOffset = Vector2.Distance(this.Owner.Center, this.Owner.fleet.Position + this.Owner.FleetOffset);
                        //Vector2 toAdd = (this.Owner.Velocity != Vector2.Zero ? Vector2.Normalize(this.Owner.Velocity) : Vector2.Zero) * 100f;
                        //Vector2.Distance(this.Owner.Center, (this.Owner.fleet.Position + this.Owner.FleetOffset) + toAdd);
                        //Vector2 vector2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.Owner.fleet.Position + this.Owner.FleetOffset, this.Owner.fleet.facing, 1f);
                        //Vector2 fvec = HelperFunctions.FindVectorToTarget(Vector2.Zero, vector2);
                        if (DistanceToFleetOffset <= 75f || this.HasPriorityOrder)
                        {
                            this.Owner.Velocity = Vector2.Zero;
                            Vector2 vector2 = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(Vector2.Zero, this.Owner.fleet.facing, 1f);
                            Vector2 fvec = HelperFunctions.FindVectorToTarget(Vector2.Zero, vector2);
                            Vector2 wantedForward = Vector2.Normalize(fvec);
                            Vector2 forward = new Vector2((float)Math.Sin((double)this.Owner.Rotation), -(float)Math.Cos((double)this.Owner.Rotation));
                            Vector2 right = new Vector2(-forward.Y, forward.X);
                            float angleDiff = (float)Math.Acos((double)Vector2.Dot(wantedForward, forward));
                            float facing = (Vector2.Dot(wantedForward, right) > 0f ? 1f : -1f);
                            if (angleDiff > 0.02f)
                            {
                                this.RotateToFacing(elapsedTime, angleDiff, facing);
                            }                            
                            if (DistanceToFleetOffset <= 75f || (this.State != AIState.Resupply || !this.HasPriorityOrder))  //fbedard: dont override high priority resupply
                            {
								this.State = AIState.AwaitingOrders;
                                this.HasPriorityOrder = false;
                            }
                        }
                        else if (this.State != AIState.HoldPosition && (DistanceToFleetOffset <= 2000f || this.Owner.loyalty.isPlayer == false)) //modified by Gretman
                        {
                            this.ThrustTowardsPosition(this.Owner.fleet.Position + this.Owner.FleetOffset, elapsedTime, this.Owner.fleet.speed);
                            lock (this.wayPointLocker)
                            {
                                this.ActiveWayPoints.Clear();
                                this.ActiveWayPoints.Enqueue(this.Owner.fleet.Position + this.Owner.FleetOffset);
                                if (this.State != AIState.AwaitingOrders)  //fbedard: set new order for ship returning to fleet
                                    this.State = AIState.AwaitingOrders;
                                if (this.Owner.fleet.GetStack().Count > 0)
                                {
                                    this.ActiveWayPoints.Enqueue(this.Owner.fleet.GetStack().Peek().MovePosition + this.Owner.FleetOffset);
                                }
                            }
                        }
                        //else 
                        //{
                        //this.Stop(elapsedTime);     //Gretman - Patch for ships drifting away after combat.
                        //}
                    }
                }
                else if (this.OrderQueue.Count > 0)
                {
#if DEBUG
                    try
#endif
                    {
                        toEvaluate = this.OrderQueue.First<ArtificialIntelligence.ShipGoal>();
                    }
#if DEBUG                    
                    catch

                    {
                        return;
                    }
#endif
                    Planet target = toEvaluate.TargetPlanet;
                    switch (toEvaluate.Plan)
                    {
                        case ArtificialIntelligence.Plan.HoldPosition:
                            {
                                this.HoldPosition();
                                break;
                            }
                        case ArtificialIntelligence.Plan.Stop:
                            {
                                this.Stop(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Scrap:
                            {
                                this.ScrapShip(elapsedTime, toEvaluate);
                                break;
                            }
                    case ArtificialIntelligence.Plan.Bombard:   //Modified by Gretman
                        target = toEvaluate.TargetPlanet;                                             //Stop Bombing if:
                        if (this.Owner.Ordinance < 0.05 * this.Owner.OrdinanceMax                           //'Aint Got no bombs!
                            || (target.TroopsHere.Count == 0 && target.Population <= 0f)                    //Everyone is dead
                            || (target.GetGroundStrengthOther(this.Owner.loyalty) + 1) * 1.5
                             <= target.GetGroundStrength(this.Owner.loyalty)  )   //This will tilt the scale just enough so that if there are 0 troops, a planet can still be bombed.

                        {   //As far as I can tell, if there were 0 troops on the planet, then GetGroundStrengthOther and GetGroundStrength would both return 0,
                            //meaning that the planet could not be bombed since that part of the if statement would always be true (0 * 1.5 <= 0)
                            //Adding +1 to the result of GetGroundStrengthOther tilts the scale just enough so a planet with no troops at all can still be bombed
                            //but having even 1 allied troop will cause the bombine action to abort.

                            this.OrderQueue.Clear();
                            this.State = AIState.AwaitingOrders;
                            ArtificialIntelligence.ShipGoal orbit = new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.Orbit, Vector2.Zero, 0f)
                            {
                                TargetPlanet = toEvaluate.TargetPlanet
                            };
                       
                            this.OrderQueue.AddLast(orbit);         //Stay in Orbit
                      
                            this.HasPriorityOrder = false;
                            //System.Diagnostics.Debug.WriteLine("Bombardment info! " + target.GetGroundStrengthOther(this.Owner.loyalty) + " : " + target.GetGroundStrength(this.Owner.loyalty));

                        }   //Done -Gretman

                        this.DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                        float radius = toEvaluate.TargetPlanet.ObjectRadius + this.Owner.Radius + 1500;
                        if (toEvaluate.TargetPlanet.Owner == this.Owner.loyalty)
                        {
                            this.OrderQueue.Clear();
                            return;
                        }
                        else if (Vector2.Distance(this.Owner.Center, toEvaluate.TargetPlanet.Position) < radius)
                        {
                            using (List<ShipModule>.Enumerator enumerator = this.Owner.BombBays.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    ShipModule current = enumerator.Current;
                                    if ((double)current.BombTimer <= 0.0)
                                    {
                                        Bomb bomb = new Bomb(new Vector3(this.Owner.Center, 0.0f), this.Owner.loyalty);
                                        bomb.WeaponName = current.BombType;
                                        if ((double)this.Owner.Ordinance > (double)ResourceManager.WeaponsDict[current.BombType].OrdinanceRequiredToFire)
                                        {
                                            this.Owner.Ordinance -= ResourceManager.WeaponsDict[current.BombType].OrdinanceRequiredToFire;
                                            bomb.SetTarget(toEvaluate.TargetPlanet);
                                            //lock (GlobalStats.BombLock)
                                            ArtificialIntelligence.universeScreen.BombList.Add(bomb);
                                            current.BombTimer = ResourceManager.WeaponsDict[current.BombType].fireDelay;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        else
                            break;

                    case ArtificialIntelligence.Plan.BombTroops:
                            target = toEvaluate.TargetPlanet;

                            if (target.TroopsHere.Where(unfriendlyTroops => unfriendlyTroops.GetOwner() != this.Owner.loyalty).Count() * 1.5
                                >= target.TilesList.Sum(space => space.number_allowed_troops))
                            {
                                if ( this.Owner.Ordinance < 0.05 * (double)this.Owner.OrdinanceMax)
                                {
                                    this.OrderQueue.Clear();
                                    this.State = AIState.AwaitingOrders;
                                    this.HasPriorityOrder = false;
                                }
                                this.DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                                radius = toEvaluate.TargetPlanet.ObjectRadius + this.Owner.Radius + 1500;
                                if (toEvaluate.TargetPlanet.Owner == this.Owner.loyalty)

                                {
                                    this.OrderQueue.Clear();
                                    return;
                                }                                    
                                else if ( Vector2.Distance(this.Owner.Center, toEvaluate.TargetPlanet.Position) < radius)
                                {
                                    using (List<ShipModule>.Enumerator enumerator = this.Owner.BombBays.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            ShipModule current = enumerator.Current;
                                            if ( current.BombTimer <= 0.0)
                                            {
                                                Bomb bomb = new Bomb(new Vector3(this.Owner.Center, 0.0f), this.Owner.loyalty);
                                                bomb.WeaponName = current.BombType;
                                                if ((double)this.Owner.Ordinance > (double)ResourceManager.WeaponsDict[current.BombType].OrdinanceRequiredToFire)
                                                {
                                                    this.Owner.Ordinance -= ResourceManager.WeaponsDict[current.BombType].OrdinanceRequiredToFire;
                                                    bomb.SetTarget(toEvaluate.TargetPlanet);
                                                    //lock (GlobalStats.BombLock)
                                                        ArtificialIntelligence.universeScreen.BombList.Add(bomb);
                                                    current.BombTimer = ResourceManager.WeaponsDict[current.BombType].fireDelay;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                                else
                                    break;
                            }
                            else if (this.Owner.HasTroopBay || (this.Owner.hasTransporter))
                            {
                                this.State = AIState.AssaultPlanet;
                                this.OrderAssaultPlanet(target);
                            }
                            else
                                this.OrderQueue.Clear();
                            break;

                        case ArtificialIntelligence.Plan.Exterminate:
                            {
                                this.DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                                radius = toEvaluate.TargetPlanet.ObjectRadius + this.Owner.Radius + 1500;
                                if (toEvaluate.TargetPlanet.Owner == this.Owner.loyalty || toEvaluate.TargetPlanet.Owner == null)
                                {
                                    this.OrderQueue.Clear();
                                    this.OrderFindExterminationTarget(true);
                                    return;
                                }
                                else
                                {
                                    if (Vector2.Distance(this.Owner.Center, toEvaluate.TargetPlanet.Position) >= radius)
                                        break;
                                    List<ShipModule>.Enumerator enumerator1 = this.Owner.BombBays.GetEnumerator();
                                    try
                                    {
                                        while (enumerator1.MoveNext())
                                        {
                                            ShipModule mod = enumerator1.Current;
                                            if (mod.BombTimer > 0f)
                                                continue;
                                            Bomb b = new Bomb(new Vector3(this.Owner.Center, 0f), this.Owner.loyalty)
                                            {
                                                WeaponName = mod.BombType
                                            };
                                            if (this.Owner.Ordinance <= ResourceManager.WeaponsDict[mod.BombType].OrdinanceRequiredToFire)
                                            {
                                                continue;
                                            }
                                            Ship owner1 = this.Owner;
                                            owner1.Ordinance = owner1.Ordinance - ResourceManager.WeaponsDict[mod.BombType].OrdinanceRequiredToFire;
                                            b.SetTarget(toEvaluate.TargetPlanet);
                                            //lock (GlobalStats.BombLock)
                                            {
                                                ArtificialIntelligence.universeScreen.BombList.Add(b);
                                            }
                                            mod.BombTimer = ResourceManager.WeaponsDict[mod.BombType].fireDelay;
                                        }
                                        break;
                                    }
                                    finally
                                    {
                                        ((IDisposable)enumerator1).Dispose();
                                    }
                                }
                            }
                        case ArtificialIntelligence.Plan.RotateToFaceMovePosition:
                            {
                                this.RotateToFaceMovePosition(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.RotateToDesiredFacing:
                            {
                                this.RotateToDesiredFacing(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.MoveToWithin1000:
                            {
                                this.MoveToWithin1000(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.MakeFinalApproachFleet:
                            {
                                if (this.Owner.fleet != null)
                                {
                                    this.MakeFinalApproachFleet(elapsedTime, toEvaluate);
                                    break;
                                }
                                else
                                {
                                    this.State = AIState.AwaitingOrders;
                                    break;
                                }
                            }
                        case ArtificialIntelligence.Plan.MoveToWithin1000Fleet:
                            {
                                if (this.Owner.fleet != null)
                                {
                                    this.MoveToWithin1000Fleet(elapsedTime, toEvaluate);
                                    break;
                                }
                                else
                                {
                                    this.State = AIState.AwaitingOrders;
                                    break;
                                }
                            }
                        case ArtificialIntelligence.Plan.MakeFinalApproach:
                            {
                                this.MakeFinalApproach(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.RotateInlineWithVelocity:
                            {
                                this.RotateInLineWithVelocity(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.StopWithBackThrust:
                            {
                                this.StopWithBackwardsThrust(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Orbit:
                            {
                                this.DoOrbit(toEvaluate.TargetPlanet, elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Colonize:
                            {
                                this.Colonize(toEvaluate.TargetPlanet);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Explore:
                            {
                                this.DoExplore(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Rebase:
                            {
                                this.DoRebase(toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.DefendSystem:
                            {
                                //if (this.Target != null)
                                //    System.Diagnostics.Debug.WriteLine(this.Target);
                                this.DoSystemDefense(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.DoCombat:
                            {
                                this.DoCombat(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.MoveTowards:
                            {
                                this.MoveTowardsPosition(this.MovePosition, elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.PickupPassengers:
                            {
                                if (this.start != null)
                                    this.PickupPassengers();
                                else
                                    this.State = AIState.AwaitingOrders;
                                break;
                            }
                        case ArtificialIntelligence.Plan.DropoffPassengers:
                            {

                                try
                                {
                                    this.DropoffPassengers();
                                }
                                catch 
                                {
                                    System.Diagnostics.Debug.WriteLine("DropoffPassengers failed");

                                }
                                break;
                            }
                        case ArtificialIntelligence.Plan.DeployStructure:
                            {
                                this.DoDeploy(toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.PickupGoods:
                            {
                                try
                                {           
                                    this.PickupGoods();
                                }
                                catch
                                { }
                                break;
                            }
                        case ArtificialIntelligence.Plan.DropOffGoods:
                            {
                                this.DropoffGoods();
                                break;
                            }
                        case ArtificialIntelligence.Plan.ReturnToHangar:
                            {
                                this.DoReturnToHangar(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.TroopToShip:
                            {
                                this.DoTroopToShip(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.BoardShip:
                            {
                                this.DoBoardShip(elapsedTime);
                                break;
                            }
                        case ArtificialIntelligence.Plan.SupplyShip:
                            {
                                this.DoSupplyShip(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.Refit:
                            {
                                this.DoRefit(elapsedTime, toEvaluate);
                                break;
                            }
                        case ArtificialIntelligence.Plan.LandTroop:
                            {
                                this.DoLandTroop(elapsedTime, toEvaluate);
                                break;
                            }
                    }
                }
                goto Label0;
#if !DEBUG   
        }
            
            catch

            {
            }
            #endif
        Label0:
        
        /* fbedard: Disabled to save CPU time
            AIState aIState = this.State;
            if (aIState == AIState.SystemTrader)  //fbedard: dont trade with planet in system in combat (AI or AutoFreighters only)
            {
                foreach (ArtificialIntelligence.ShipGoal goal in this.OrderQueue)
                {
                    if (goal.Plan == ArtificialIntelligence.Plan.DropOffGoods && goal.TargetPlanet != null && goal.TargetPlanet.ParentSystem.combatTimer > 0f && (!this.Owner.loyalty.isPlayer || this.Owner.loyalty.AutoFreighters))
                    {
                        this.OrderQueue.Clear();
                        this.State = AIState.AwaitingOrders;
                        break;
                    }
                    else if (goal.Plan == ArtificialIntelligence.Plan.PickupGoods && goal.TargetPlanet != null && goal.TargetPlanet.ParentSystem.combatTimer > 0f && (!this.Owner.loyalty.isPlayer || this.Owner.loyalty.AutoFreighters))
                    {
                        this.OrderQueue.Clear();
                        this.State = AIState.AwaitingOrders;
                        break;
                    }
                }
            }
            else if (aIState == AIState.PassengerTransport)   //fbedard: dont trade with planet in system in combat (AI or AutoFreighters only)
            {
                foreach (ArtificialIntelligence.ShipGoal goal in this.OrderQueue)
                {
                    if (goal.Plan == ArtificialIntelligence.Plan.DropoffPassengers && goal.TargetPlanet != null && goal.TargetPlanet.ParentSystem.combatTimer > 0f && (!this.Owner.loyalty.isPlayer || this.Owner.loyalty.AutoFreighters))
                    {
                        this.OrderQueue.Clear();
                        this.State = AIState.AwaitingOrders;
                        break;
                    }
                    else if (goal.Plan == ArtificialIntelligence.Plan.PickupPassengers && goal.TargetPlanet != null && goal.TargetPlanet.ParentSystem.combatTimer > 0f && (!this.Owner.loyalty.isPlayer || this.Owner.loyalty.AutoFreighters))
                    {
                        this.OrderQueue.Clear();
                        this.State = AIState.AwaitingOrders;
                        break;
                    }
                }
            }
            */
            if (this.State == AIState.Rebase)
            {
                foreach (ArtificialIntelligence.ShipGoal goal in this.OrderQueue)
                //Parallel.ForEach(this.OrderQueue, (goal, state) =>
                {
                    if (goal.Plan != ArtificialIntelligence.Plan.Rebase || goal.TargetPlanet == null || goal.TargetPlanet.Owner == this.Owner.loyalty)
                    {
                        continue;
                    }
                    this.OrderQueue.Clear();
                    this.State = AIState.AwaitingOrders;
                    break;
                }
            }
            //if(targetChangeTimer >-1)
            //targetChangeTimer -= elapsedTime;
            //if(TriggerDelay >-1)
            TriggerDelay -= elapsedTime;
            if ( this.BadGuysNear)// || this.Owner.InCombat )
            {
              
                    bool docombat = false;
                    LinkedListNode<ArtificialIntelligence.ShipGoal> tempShipGoal = this.OrderQueue.First;
                    ShipGoal firstgoal = tempShipGoal != null ? tempShipGoal.Value : null;  //.FirstOrDefault<ArtificialIntelligence.ShipGoal>();
                    if (this.Owner.Weapons.Count > 0 || this.Owner.GetHangars().Count > 0 || this.Owner.Transporters.Count >0)
#if !DEBUG
                        try
#endif
                    {

                        if(this.Target !=null || this.PotentialTargets.Count >0 )

                        docombat = !this.HasPriorityOrder && !this.IgnoreCombat && this.State != AIState.Resupply && (this.OrderQueue.Count == 0 || firstgoal != null && firstgoal.Plan != ArtificialIntelligence.Plan.DoCombat && firstgoal.Plan != Plan.Bombard && firstgoal.Plan != Plan.BoardShip);


                        if (docombat)//|| this.OrderQueue.Count == 0))
                        {
                            this.OrderQueue.AddFirst(new ArtificialIntelligence.ShipGoal(ArtificialIntelligence.Plan.DoCombat, Vector2.Zero, 0f));
                        }
                        //else
                        //{
                        //    this.OrderQueue.thisLock.EnterWriteLock();
                        //    foreach(ShipGoal goal in this.OrderQueue)
                        //    {
                        //        if (goal.Plan == Plan.DoCombat)
                        //            this.OrderQueue.QueuePendingRemoval(goal);
                        //    }
                        //    this.OrderQueue.thisLock.ExitWriteLock();
                        //    this.OrderQueue.ApplyPendingRemovals();
                        //}

                        //this.fireTask = Task.Factory.StartNew(this.FireOnTarget);//,TaskCreationOptions.LongRunning);
                        //fireTask = new Task(this.FireOnTarget);    
                        if (this.TriggerDelay < 0)
                        {
                            TriggerDelay = elapsedTime * 2;
                            this.FireOnTarget();
                        }

                    }

#if !DEBUG
                        catch
                        {
                        }
                
#endif
                
            }
            else
            {
                foreach (Weapon purge in this.Owner.Weapons)
                {
                    if (purge.fireTarget != null)
                    {
                        purge.PrimaryTarget = false;
                        purge.fireTarget = null;
                        purge.SalvoTarget = null;
                    }
                    if(purge.AttackerTargetting != null)
                    purge.AttackerTargetting.Clear();
                }
                if (this.Owner.GetHangars().Count > 0 && this.Owner.loyalty != ArtificialIntelligence.universeScreen.player)
                {
                    foreach (ShipModule hangar in this.Owner.GetHangars())
                    {
                        if (hangar.IsTroopBay || hangar.IsSupplyBay || hangar.GetHangarShip() == null
                            //||hangar.GetHangarShip().InCombat
                            || hangar.GetHangarShip().GetAI().State == AIState.ReturnToHangar)
                        {
                            continue;
                        }
                        hangar.GetHangarShip().GetAI().OrderReturnToHangar();
                    }
                }
                else if (this.Owner.GetHangars().Count > 0)
                {
                    foreach (ShipModule hangar in this.Owner.GetHangars())
                    {
                        if (hangar.IsTroopBay
                            || hangar.IsSupplyBay
                            || hangar.GetHangarShip() == null
                            || hangar.GetHangarShip().GetAI().State == AIState.ReturnToHangar
                            //|| hangar.GetHangarShip().InCombat
                            || hangar.GetHangarShip().GetAI().hasPriorityTarget
                            || hangar.GetHangarShip().GetAI().HasPriorityOrder

                            )
                        {
                            continue;
                        }
                        hangar.GetHangarShip().DoEscort(this.Owner);
                    }
                }
            }
            if (this.Owner.shipData.ShipCategory == ShipData.Category.Civilian && this.BadGuysNear) //fbedard: civilian will evade
            {
                this.CombatState = Gameplay.CombatState.Evade;
            }

            if (this.State != AIState.Resupply && !this.HasPriorityOrder && this.Owner.Health / this.Owner.HealthMax < DmgLevel[(int)this.Owner.shipData.ShipCategory] &&  this.Owner.shipData.Role >= ShipData.RoleName.supply) //fbedard: ships will go for repair
                if (this.Owner.fleet == null || (this.Owner.fleet != null && !this.Owner.fleet.HasRepair))
                {
                    this.OrderResupplyNearest(false);
                }
            if(this.State == AIState.AwaitingOrders && this.Owner.NeedResupplyTroops)
            {
                this.OrderResupplyNearest(false);
            }
            if (this.State == AIState.AwaitingOrders && this.Owner.needResupplyOrdnance)
            {
                this.OrderResupplyNearest(false);

            }
            if (this.State == AIState.Resupply && !this.HasPriorityOrder)
            {
                this.HasPriorityOrder = true;
            }
            if (!this.Owner.isTurning)
            {
                this.DeRotate();
                return;
            }
            else
            {
                return;
            }
        }

		private static float WrapAngle(float radians)
		{
			while (radians < -3.14159274f)
			{
				radians = radians + 6.28318548f;
			}
			while (radians > 3.14159274f)
			{
				radians = radians - 6.28318548f;
			}
			return radians;
		}

		public enum Plan
		{
			Stop,
			Scrap,
			HoldPosition,
			Bombard,
			Exterminate,
			RotateToFaceMovePosition,
			RotateToDesiredFacing,
			MoveToWithin1000,
			MakeFinalApproachFleet,
			MoveToWithin1000Fleet,
			MakeFinalApproach,
			RotateInlineWithVelocity,
			StopWithBackThrust,
			Orbit,
			Colonize,
			Explore,
			Rebase,
			DoCombat,
			MoveTowards,
			Trade,
			DefendSystem,
            TransportPassengers,
			PickupPassengers,
			DropoffPassengers,
			DeployStructure,
			PickupGoods,
			DropOffGoods,
			ReturnToHangar,
			TroopToShip,
			BoardShip,
			SupplyShip,
			Refit,
			LandTroop,
			MoveToWithin7500,
            BombTroops
		}

		public class ShipGoal
		{
			public ArtificialIntelligence.Plan Plan;

			public Goal goal;

			public float VariableNumber;

			public string VariableString;

			public Fleet fleet;

			public Vector2 MovePosition;

			public float DesiredFacing;

			public float FacingVector;

			public Planet TargetPlanet;

			public float SpeedLimit = 1f;

			public ShipGoal(ArtificialIntelligence.Plan p, Vector2 pos, float facing)
			{
				this.Plan = p;
				this.MovePosition = pos;
				this.DesiredFacing = facing;
			}
		}

		public class ShipWeight
		{
			public Ship ship;

			public float weight;
            public bool defendEscort;

			public ShipWeight()
			{
			}
		}
        public class WayPoints
        {
            public Planet planet { get; set; }

            public Ship ship { get; set; }
            
            public Vector2 location { get ; set; }
        }
		private enum transportState
		{
			ChoosePickup,
			GoToPickup,
			ChooseDropDestination,
			GotoDrop,
			DoDrop
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ArtificialIntelligence() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.NearbyShips != null)
                        this.NearbyShips.Dispose();
                    if (this.FriendliesNearby != null)
                        this.FriendliesNearby.Dispose();

                }
                this.NearbyShips = null;
                this.FriendliesNearby = null;
                this.disposed = true;
            }
        }

        
        public struct Grid
        {
            //public List<Vector2> goodpoints; //= //new List<Vector2>();
            public List<Empire.InfluenceNode> goodpoints;
            public List<Vector2> badpoints;
            public float projectorsize;
            public Empire    ai;
            //public byte[,] Weight;
       
           
            // = new Vector2[(int)granularity];
           // = new float[(int)granularity];
            float granularity; //= 8f;
            float projectorWeight;
            class mappoint
            {
                public float radius;
                public Vector2 pin;

            }
            /// <summary>            
            /// </summary>
            /// <param name="ai"></param> 
            /// <param name="PointSearchGranuality"></param> //divide all the points around a point into buckets of angles. Creates a vector2 and a float for each granularity
            /// <param name="ProjectorWeight"></param> //this multiplies the effect of the inborders bonus. (doesnt seem to work yet...)
            public Grid(Empire ai, int PointSearchGranuality, float ProjectorWeightPercentage)
            {
                this.ai = ai;
                projectorWeight = ProjectorWeightPercentage;
                granularity = PointSearchGranuality;
                
                //distance = new float[(int)granularity + 1];
                Empire empire = ai;
                projectorsize = Empire.ProjectorRadius;
                goodpoints = new List<Empire.InfluenceNode>();
                badpoints = new List<Vector2>();

                // List < Ship > ps = empire.GetProjectors();
                Empire.InfluenceNode newpoint = null;
  //              ai.BorderNodeLocker.EnterReadLock();
                {
                    foreach(Empire.InfluenceNode point  in ai.BorderNodes)
                    {
                        float radius = point.Radius * .50f;
                        float radius2 = point.Radius * .50f;
                        if (point.KeyedObject is SolarSystem)
                        {
                            radius = 140000;
                            radius2 = 10000;
                        }

                            for (int x = 0; x < 360; x += 60)
                        {
                            float angle = x;
                            newpoint = new Empire.InfluenceNode();
                            newpoint.Position = HelperFunctions.GeneratePointOnCircle(angle, point.Position, radius);
                            newpoint.Radius = radius2;
                            newpoint.KeyedObject = point.KeyedObject;
                            goodpoints.Add(newpoint);
                        }
                    }
                  //  goodpoints = new List<Empire.InfluenceNode>(ai.BorderNodes .Where(ss => !(ss.KeyedObject is SolarSystem) && !(ss.KeyedObject is Planet)) );
                }
//                ai.BorderNodeLocker.ExitReadLock();

           



                Relationship rel;
                foreach(Empire e in EmpireManager.EmpireList)
                {
                    if (empire.GetRelations().TryGetValue(e, out rel) && rel.Treaty_Alliance)
                    {
                        foreach (Empire.InfluenceNode point in e.BorderNodes)
                        {
                            if (point.KeyedObject is Planet)
                                continue;
                            float radius = point.Radius * .50f;
                            float radius2 = point.Radius * .50f;
                            
                            //if (point.KeyedObject is SolarSystem)
                            //{
                            //    radius = 140000;
                            //    radius2 = 10000;
                            //}

                            for (int x = 0; x < 360; x += 30)
                            {
                                float angle = x;
                                newpoint = new Empire.InfluenceNode();
                                newpoint.Position = HelperFunctions.GeneratePointOnCircle(angle, point.Position, radius);
                                newpoint.Radius = radius2;
                                newpoint.KeyedObject = point.KeyedObject;
                                goodpoints.Add(newpoint);
                            }
                        }
                    }
                    else
                    if (false && rel != null && !rel.Treaty_OpenBorders && !rel.AtWar)
                    {
                        foreach (Empire.InfluenceNode s in e.BorderNodes)
                        {
                            for (int x = 0; x < 360; x += 60)
                            {
                                float angle = x;
                                newpoint = new Empire.InfluenceNode();
                                newpoint.Position = HelperFunctions.GeneratePointOnCircle(angle, s.Position, s.Radius * 1.5f);
                                newpoint.Radius = 0;
                                goodpoints.Add(newpoint);
                                newpoint = new Empire.InfluenceNode();
                                newpoint.Position = HelperFunctions.GeneratePointOnCircle(angle, s.Position, s.Radius);
                                newpoint.Radius = -1;
                                goodpoints.Add(newpoint);
                            }

                        }
                    }

                }
                //List<Vector2> extrabad = new List<Vector2>();
                //List<Vector2> extragood = new List<Vector2>();
                //foreach (Vector2 bp in badpoints)
                //{                   
                //    for (int rad = 0; rad < 360; rad += 60)
                //    {
                //        extragood.Add(HelperFunctions.GeneratePointOnCircle(rad, bp, projectorsize * 2.6f));

                //        extrabad.Add(HelperFunctions.GeneratePointOnCircle(rad, bp, projectorsize*2.5f ));
                //    }
                //}
                //List<Vector2> removep = new List<Vector2>();
                //goodpoints.AddRange(extragood);
                //foreach (Vector2 bp in badpoints)
                //{
                //    foreach(Vector2 eg in extragood )
                //    {
                //        if (Vector2.Distance(bp, eg) < projectorsize*2.5f )
                //            goodpoints.Remove(eg);
                //    }
                //}

                //badpoints.AddRange(extrabad);

                //goodpoints.AddRange(badpoints);

            }
            
            public List<Vector2> Pathfind(Vector2 startv, Vector2 endv,bool mode2)
            {
                float Pathlength = Vector2.Distance(startv, endv);
                //Empire.InfluenceNode end;
                //Empire.InfluenceNode start;

                if (Pathlength < projectorsize )
                    return new List<Vector2> { startv, endv };
               
                if (Empire.universeScreen == null)
                    return null;
                Empire.InfluenceNode end = new Empire.InfluenceNode();
                Empire.InfluenceNode start = new Empire.InfluenceNode();
                end.Position = endv;
                end.Radius = projectorsize;
                start.Position = startv;
                start.Radius = projectorsize;
                // nodes that have already been analyzed and have a path from the start to them
                var closedSet = new List<Empire.InfluenceNode>();
                // nodes that have been identified as a neighbor of an analyzed node, but have 
                // yet to be fully analyzed
                var openSet = new List<Empire.InfluenceNode> { start };
                // a dictionary identifying the optimal origin point to each node. this is used 
                // to back-track from the end to find the optimal path
                var cameFrom = new Dictionary<Empire.InfluenceNode, Empire.InfluenceNode>();
                // a dictionary indicating how far each analyzed node is from the start
                var currentDistance = new Dictionary<Empire.InfluenceNode, float>();
                // a dictionary indicating how far it is expected to reach the end, if the path 
                // travels through the specified node. 
                var predictedDistance = new Dictionary<Empire.InfluenceNode, float>();
                
                //if (!goodpoints.Contains(start))
                //    goodpoints.Add(start);
                // initialize the start node as having a distance of 0, and an estmated distance 
                // of y-distance + x-distance, which is the optimal path in a square grid that 
                // doesn't allow for diagonal movement
                currentDistance.Add(start, 0);
                predictedDistance.Add(
                    start,
                    Pathlength
                );
                //this.end = end;
                //this.start = start;
                // if there are any unanalyzed nodes, process them
                float doublepro = projectorsize * 2.5f;
                
                //float max = 0;
                //max += Vector2.Distance(start, end);
                while (openSet.Count > 0)
                {
               

                    // get the node with the lowest estimated cost to finish
                    var current = (
                        from p in openSet orderby predictedDistance[p] ascending select p
                    ).First();

                    // if it is the finish, return the path
                    if (current ==end    )//Vector2.Distance(current,end) <=projectorsize *2.5)// current.X == end.X && current.Y == end.Y)
                    {
                        // generate the found path
                        return ReconstructPath(cameFrom, end, start);
                    }

                    // move current node from open to closed
                    openSet.Remove(current);
                    closedSet.Add(current);
                    // process each valid node around the current node
                    foreach (Empire.InfluenceNode neighbor in GetNeighborNodes(current, mode2,end )) //, cameFrom, start, mode2))
                    {

                        var neighborDistance = Vector2.Distance(neighbor.Position, current.Position) ;
                        if (current.Radius + neighbor.Radius < neighborDistance)
                            neighborDistance += (neighborDistance - current.Radius - neighbor.Radius) * (projectorWeight * ai.data.Traits.InBordersSpeedBonus);
                        //doublepro = (neighbor.Radius + current.Radius) *1.25f;
                        //if (neighborDistance > doublepro)
                        //{
                        //    float tempd = (neighborDistance - doublepro);
                        //    neighborDistance += tempd * (1 + ai.data.Traits.InBordersSpeedBonus) - tempd;
                        //}

                        var tempCurrentDistance = currentDistance[current] +  neighborDistance;              
                    
                        // if we already know a faster way to this neighbor, use that route and 
                        // ignore this one
                        if (closedSet.Contains(neighbor)
                            && tempCurrentDistance >= currentDistance[neighbor])
                        {
                            continue;
                        }

                        // if we don't know a route to this neighbor, or if this is faster, 
                        // store this route
                        if (!closedSet.Contains(neighbor)
                            || tempCurrentDistance < currentDistance[neighbor])
                        {
                            if (cameFrom.Keys.Contains(neighbor))
                            {
                                cameFrom[neighbor] = current;
                                
                            }
                            else
                            {
                                cameFrom.Add(neighbor, current);
                                //radius = 0;
                            }
                            float tempendDist = Vector2.Distance(neighbor.Position, end.Position) ;
                            if (current.Radius + neighbor.Radius < tempendDist)
                                tempendDist += (tempendDist - current.Radius - neighbor.Radius) * ((1 + projectorWeight) * ai.data.Traits.InBordersSpeedBonus);
                            //if (tempendDist > doublepro)
                            //{
                            //    float tempd = (tempendDist - doublepro);
                            //    tempendDist += tempd * (1 + ai.data.Traits.InBordersSpeedBonus) - tempd;
                            //}
                            currentDistance[neighbor] = tempCurrentDistance;
                            
                            predictedDistance[neighbor] =
                                currentDistance[neighbor]
                                + tempendDist;
                            

                            // if this is a new node, add it to processing
                            if (!openSet.Contains(neighbor))
                            {
                                openSet.Add(neighbor);
                               // radius = 0;
                            }
                        }
                      
                    
                    }

                   
                }
                if (!mode2)
                {
                    return Pathfind(start.Position, end.Position, true);
                }
               // return ReconstructPath(cameFrom, end);
                System.Diagnostics.Debug.WriteLine(string.Format(
                        "unable to find a path between {0},{1} and {2},{3}",
                        start.Position.X, start.Position.Y,
                        end.Position.X, end.Position.Y
                    ));
                
                return null;// cameFrom.Keys.ToList();
            }

            /// <summary>
            /// Return a list of accessible nodes neighboring a specified node
            /// </summary>
            /// <param name="node">The center node to be analyzed.</param>
            /// <returns>A list of nodes neighboring the node that are accessible.</returns>
       

            
            private IEnumerable<Empire.InfluenceNode> GetNeighborNodes2(Empire.InfluenceNode node, Empire.InfluenceNode end)
            {
                var nodes = new List<Empire.InfluenceNode>();
                int granularityl =(int) granularity;
                float[] distance = new float[(int)granularity + 1];
                Empire.InfluenceNode[] nearest = new Empire.InfluenceNode[(int)granularity + 1];
                for (int i=0;i<(int)granularityl; i++)
                {
                    nearest[i] = null;
                    distance[i] = 0;
                }

                float distancetoend = Vector2.Distance(end.Position, node.Position);
                float angletonode = 0;
                float angletoend = HelperFunctions.findAngleToTarget(node.Position, end.Position);
                int y = 0;
                int Ey = y = (int)Math.Floor(angletoend / granularityl);
                float distancecheck = 0;
                
                granularityl = (int)(360 / granularityl);
                foreach (Empire.InfluenceNode point in goodpoints)
                {
                    if (point == node)
                        continue;
                    
                    angletonode = HelperFunctions.findAngleToTarget(node.Position, point.Position);                    
                    y = (int)Math.Floor(angletonode / granularityl);
                    distancecheck = Vector2.Distance(node.Position, point.Position);

                    if (distance[y] == 0 || distance[y] > distancecheck)
                    {
                        nearest[y] = (point);
                        distance[y] = distancecheck;
                    }


                }
                if (nearest[Ey] == null || distance[Ey] >= distancetoend)
                    nearest[Ey] = end;
                
                foreach(Empire.InfluenceNode filternodes in nearest)
                {
                    if(filternodes != null)
                    nodes.Add(filternodes);
                }
                               
                    
                return nodes;
            }
            private IEnumerable<Empire.InfluenceNode> GetNeighborNodes3(Empire.InfluenceNode node, Dictionary<Empire.InfluenceNode, Empire.InfluenceNode> camefrom, Empire.InfluenceNode start, bool mode2)
            {
                if(mode2)
                {
                    //return GetNeighborNodes2(node,camefrom);
                }
                HashSet<Empire.InfluenceNode> nodes = new HashSet<Empire.InfluenceNode>();
                float projector = node.Radius;            
                float distancecheck = 0;                               
                float radius = 0;
                //radius = Vector2.Distance(node, previousPoint) ;
               // do
                {
                    Empire.InfluenceNode lastpoint = camefrom.Count > 0 ? camefrom.Keys.Last() : start;
                    float angletonode = HelperFunctions.findAngleToTarget(lastpoint.Position, node.Position);
                    float angletopoint = 0;
                    foreach (Empire.InfluenceNode point in goodpoints)
                    {
                        distancecheck = Vector2.Distance(point.Position, node.Position);
                        // if (point != previousPoint && point != node)
                        if (distancecheck < radius + projector + point.Radius) //
                            nodes.Add(point);
                        else
                        {
                            angletopoint = HelperFunctions.findAngleToTarget(node.Position, point.Position);

                            if (distancecheck < (radius + projector + point.Radius)*2 && Math.Abs(angletonode - angletopoint) < 5f)
                                nodes.Add(point);
                        }
                        


                    }
    //                radius += projector;

                }
    foreach(Empire.InfluenceNode point in nodes)
                {

                    distancecheck = Vector2.Distance(point.Position, node.Position);
                    if (distancecheck < radius + projector + point.Radius && camefrom.Count > 0)
                    {
                        float angletonode = HelperFunctions.findAngleToTarget(node.Position, point.Position);
                        float anglefrom = HelperFunctions.findAngleToTarget(camefrom.Keys.Last().Position, point.Position);
                        if (Math.Abs(anglefrom - angletonode) < 5f)
                            nodes.Add(point);
                    }
                }
                return nodes;
            }
            private IEnumerable<Empire.InfluenceNode> GetNeighborNodes(Empire.InfluenceNode node,  bool mode2,Empire.InfluenceNode end)
            {
                if (mode2)
                {
                    return GetNeighborNodes2(node,end);
                }
                float[] distance = new float[(int)granularity + 1];
                Empire.InfluenceNode[] nearest = new Empire.InfluenceNode[(int)granularity + 1];
                var nodes = new List<Empire.InfluenceNode>();
                int granularityl = (int)granularity;
                Vector2 endrange = Vector2.Zero;
                for (int i = 0; i < (int)granularityl; i++)
                {
                    nearest[i] = null;
                    distance[i] = 0;
                }
                if (node.KeyedObject != null)
                { }

                float angletonode = 0;
                int y = 0;
                float distancecheck = 0;

                granularityl = (int)(360 / granularityl);
                foreach (Empire.InfluenceNode point in goodpoints)
                {
                    if (node == point)
                        continue;
                    angletonode = HelperFunctions.findAngleToTarget(node.Position, point.Position);
                    y = (int)Math.Floor(angletonode / granularityl);
                    distancecheck = Vector2.Distance(node.Position, point.Position);
                    float max = node.Radius > projectorsize ? node.Radius : projectorsize;
                    if (distancecheck < max * 2f && distancecheck > distance[y])
                    {
                        nearest[y] = (point);
                       distance[y] = distancecheck;
                    }


                }

                foreach (Empire.InfluenceNode filternodes in nearest)
                {
                    if (filternodes != null)
                        nodes.Add(filternodes);
                }
                if (Vector2.Distance(end.Position, node.Position) < projectorsize * 2.5f)
                {
                    nodes.Add(end);
                }
                return nodes;
            }
            /// <summary>
            /// Process a list of valid paths generated by the Pathfind function and return 
            /// a coherent path to current.
            /// </summary>
            /// <param name="cameFrom">A list of nodes and the origin to that node.</param>
            /// <param name="current">The destination node being sought out.</param>
            /// <returns>The shortest path from the start to the destination node.</returns>
            private List<Vector2> ReconstructPath(Dictionary<Empire.InfluenceNode, Empire.InfluenceNode> cameFrom, Empire.InfluenceNode current, Empire.InfluenceNode start)
            {
                Empire.InfluenceNode test;
                if (!cameFrom.TryGetValue(current, out test) ) // .Keys.Contains( current) && )
                {
                    return new List<Vector2> { current.Position };
                }
                
                    var path = ReconstructPath(cameFrom, cameFrom[current], start);
                if (start != current)
                {
                    path.Add(current.Position);
                }
                
                return path;
            }
            public List<Vector2> createNodes (Vector2 Origin, Vector2 Destination, float projectorad)
            {
           
                float Distance = Vector2.Distance(Origin, Destination);
                //List<Vector2> tempNodes = new List<Vector2>();

                List<Vector2> Position = new List<Vector2>();
                float offset = projectorad *2f;
                int NumberOfProjectors = (int)(Math.Ceiling(Distance / offset));
                int max = 2;
                float angle = HelperFunctions.findAngleToTarget(Origin, Destination);
                Position.Add(HelperFunctions.GeneratePointOnCircle(angle, Origin, (float)1 * (Distance / NumberOfProjectors)));
                return Position;
                //max = NumberOfProjectors < 2 ? 1 : NumberOfProjectors;
                //for (int i = 1; i < max; i++)
                //{                    
                //    float angle = HelperFunctions.findAngleToTarget(Origin, Destination);
                //    Position.Add(HelperFunctions.GeneratePointOnCircle(angle, Origin, (float)i * (Distance / NumberOfProjectors)));
           
                //}
                return Position;
            }
        }
    }

}