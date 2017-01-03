// Type: Ship_Game.Goal
// Assembly: StarDrive, Version=1.0.9.0, Culture=neutral, PublicKeyToken=null
// MVID: C34284EE-F947-460F-BF1D-3C6685B19387
// Assembly location: E:\Games\Steam\steamapps\common\StarDrive\oStarDrive.exe

using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game
{
    public class Goal
    {
        public Guid guid = Guid.NewGuid();
        public Empire empire;
        public string GoalName;
        public GoalType type;
        public int Step;
        protected Fleet fleet;
        public Vector2 TetherOffset;
        public Guid TetherTarget;
        public bool Held;
        public Vector2 BuildPosition;
        public string ToBuildUID;
        protected Planet PlanetBuildingAt;
        protected Planet markedPlanet;
        public Ship beingBuilt;
        protected Ship colonyShip;
        protected Ship freighter;
        protected Ship passTran;

        public Goal(Vector2 buildPosition, string platformUid, Empire owner)
        {
            GoalName = "BuildConstructionShip";
            type = GoalType.DeepSpaceConstruction;
            BuildPosition = buildPosition;
            ToBuildUID = platformUid;
            empire = owner;
            DoBuildConstructionShip();
        }

        public Goal(Troop toCopy, Empire owner, Planet p)
        {
            GoalName = "Build Troop";
            type = GoalType.DeepSpaceConstruction;
            PlanetBuildingAt = p;
            ToBuildUID = toCopy.Name;
            empire = owner;
            type = GoalType.BuildTroop;
        }

        public Goal(Planet toColonize, Empire e)
        {
            empire = e;
            GoalName = "MarkForColonization";
            type = GoalType.Colonize;
            markedPlanet = toColonize;
            colonyShip = (Ship)null;
        }

        public Goal(string shipType, string forWhat, Empire e)
        {
            ToBuildUID = shipType;
            empire = e;
            beingBuilt = ResourceManager.GetShipTemplate(shipType);
            GoalName = forWhat;
            Evaluate();
        }

        public Goal()
        {
        }

        public Goal(Empire e)
        {
            empire = e;
        }

        public void SetFleet(Fleet f)
        {
            fleet = f;
        }

        public Fleet GetFleet()
        {
            return fleet;
        }

        public virtual void Evaluate()
        {
            if (Held)
                return;
            switch (GoalName)
            {
                case "MarkForColonization":     DoMarkedColonizeGoal();     break;
                case "IncreaseFreighters":      DoIncreaseFreightersGoal(); break;
                case "IncreasePassengerShips":  DoIncreasePassTranGoal();   break;
                case "BuildDefensiveShips":     DoBuildDefensiveShipsGoal();break;
                case "BuildOffensiveShips":     DoBuildOffensiveShipsGoal();break;
               // case "FleetRequisition":        DoFleetRequisition();       break;
                case "Build Troop":             DoBuildTroop();             break;
                case "Build Scout":             DoBuildScoutGoal();         break;
            }
        }

        public virtual void EvaluateGoal()
        {
            
        }
        public Planet GetPlanetWhereBuilding()
        {
            return PlanetBuildingAt;
        }

        public void SetColonyShip(Ship s)
        {
            colonyShip = s;
        }

        public void SetPlanetWhereBuilding(Planet p)
        {
            PlanetBuildingAt = p;
        }

        public void SetBeingBuilt(Ship s)
        {
            beingBuilt = s;
        }

        public void SetMarkedPlanet(Planet p)
        {
            markedPlanet = p;
        }

        private void DoBuildOffensiveShipsGoal()
        {
            switch (Step)
            {
                case 0:
                    if (beingBuilt == null)
                        beingBuilt = ResourceManager.GetShipTemplate(ToBuildUID);
                    Planet planet1 = null;
                    Array<Planet> list = new Array<Planet>();
                    foreach (Planet planet2 in empire.GetPlanets().OrderBy(planet =>
                    {
                        float weight = 0;
                        switch (planet.colonyType)
                        {
                            case Planet.ColonyType.Core:            weight += 4; break;
                            case Planet.ColonyType.Colony:          break;
                            case Planet.ColonyType.Industrial:      weight += 2; break;
                            case Planet.ColonyType.Research:        weight -= 6; break;
                            case Planet.ColonyType.Agricultural:    weight -= 6; break;
                            case Planet.ColonyType.Military:        weight += 2; break;
                            case Planet.ColonyType.TradeHub:        weight += 2; break;
                        }
                        weight += planet.developmentLevel;
                        weight += planet.MineralRichness;
                        return weight;
                    }))
                    {
                        if (planet2.HasShipyard && planet2.colonyType != Planet.ColonyType.Research)
                            list.Add(planet2);
                    }
                    int num1 = 9999999;
                    int x = 0;
                    foreach (Planet planet2 in list)
                    {
                        if (x > empire.GetPlanets().Count * .2f)
                            break;
                        int num2 = 0;
                        foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                            num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                        if (planet2.ConstructionQueue.Count == 0)
                            num2 = (int)((beingBuilt.GetCost(empire) - planet2.ProductionHere) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                        if (num2 < num1)
                        {
                            num1 = num2;
                            planet1 = planet2;
                        }
                    }
                    if (planet1 == null )
                        break;
                    PlanetBuildingAt = planet1;
                    planet1.ConstructionQueue.Add(new QueueItem()
                    {
                        isShip = true,
                        QueueNumber = planet1.ConstructionQueue.Count,
                        sData = beingBuilt.GetShipData(),
                        Goal = this,
                        Cost = beingBuilt.GetCost(empire)
                        
                    });
                    ++Step;
                    break;
                case 1:
                    {
                        if (PlanetBuildingAt == null || PlanetBuildingAt.ConstructionQueue.Count == 0)
                            break;
                        if (PlanetBuildingAt.ConstructionQueue[0].Goal == this)
                        {
                            if (PlanetBuildingAt.ProductionHere > PlanetBuildingAt.MAX_STORAGE * .5f)
                            {
                                PlanetBuildingAt.ApplyStoredProduction(0);
                            }
                        }

                        break;
                    }
                case 2:
                    beingBuilt.GetAI().State = AIState.AwaitingOrders;
                    empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }

        private void DoBuildDefensiveShipsGoal()
        {
            switch (Step)
            {
                case 0:
                    if (beingBuilt == null)
                        beingBuilt = ResourceManager.ShipsDict[this.ToBuildUID];
                    Planet planet1 = (Planet)null;
                    Array<Planet> list = new Array<Planet>();
                    foreach (Planet planet2 in this.empire.GetPlanets())
                    {
                        if (planet2.HasShipyard)
                            list.Add(planet2);
                    }
                    int num1 = 9999999;
                    foreach (Planet planet2 in list)
                    {
                        if (planet2.ParentSystem.combatTimer > 0f)  //fbedard
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                            if (planet2.ConstructionQueue.Count == 0)
                                num2 = (int)(((double)this.beingBuilt.GetCost(this.empire) - (double)planet2.ProductionHere) / (double)planet2.NetProductionPerTurn);
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                    }
                    if (planet1 == null)
                        foreach (Planet planet2 in list)
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                            if (planet2.ConstructionQueue.Count == 0)
                                num2 = (int)((this.beingBuilt.GetCost(this.empire) - planet2.ProductionHere) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                    if (planet1 == null)
                        break;
                    this.PlanetBuildingAt = planet1;    
                    planet1.ConstructionQueue.Add(new QueueItem()                        
                    {
                        isShip = true,                        
                        QueueNumber = planet1.ConstructionQueue.Count,
                        sData = this.beingBuilt.GetShipData(),
                        Goal = this,
                        Cost = this.beingBuilt.GetCost(this.empire)
                    });
                    ++this.Step;
                    break;
                case 1:
                    {
                        //if (PlanetBuildingAt.ConstructionQueue[0].Goal == this)
                        //{
                        //    if (PlanetBuildingAt.ProductionHere > PlanetBuildingAt.MAX_STORAGE * .75f)
                        //    {
                        //        PlanetBuildingAt.ApplyStoredProduction(0);
                        //    }
                        //}

                        break;
                    }

                case 2:
                    this.beingBuilt.DoDefense();
                    this.empire.ForcePoolAdd(this.beingBuilt);
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }

        private void DoBuildTroop()
        {
            switch (this.Step)
            {
                case 0:
                    if (this.ToBuildUID != null)
                        this.PlanetBuildingAt.ConstructionQueue.Add(new QueueItem()
                        {
                            isTroop = true,
                            QueueNumber = this.PlanetBuildingAt.ConstructionQueue.Count,
                            troop = ResourceManager.CopyTroop(ResourceManager.TroopsDict[this.ToBuildUID]),
                            Goal = this,
                            Cost = ResourceManager.TroopsDict[this.ToBuildUID].GetCost()
                        });
                    else Log.Info("Missing Troop ");
                    this.Step = 1;

                    break;

                case 1:
                    {
                        if (PlanetBuildingAt.ConstructionQueue.Count >0 && PlanetBuildingAt.ConstructionQueue[0].Goal == this)
                        {
                           //if(PlanetBuildingAt.ProductionHere > PlanetBuildingAt.MAX_STORAGE *.25f)
                           //{
                           //    PlanetBuildingAt.ApplyStoredProduction(0);
                           //}
                        }

                        break;
                    }
                case 2:
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }

        public virtual void ReportShipComplete(Ship ship)
        {
            switch (GoalName)
            {
                case "BuildDefensiveShips":
                    {
                        this.beingBuilt = ship;
                        ++this.Step;
                        break;
                    }

                case "BuildOffensiveShips":
                    {
                        this.beingBuilt = ship;
                        ++this.Step;
                        break;
                    }
                case "FleetRequisition":
                    {
                        this.beingBuilt = ship;
                        ++this.Step;
                        break;
                    }
                default: return;
                    
            }

        }

        public Ship GetColonyShip()
        {
            return this.colonyShip;
        }

        public Planet GetMarkedPlanet()
        {
            return this.markedPlanet;
        }

        private void DoBuildConstructionShip()
        {
            switch (this.Step)
            {
                case 0:
                    Array<Planet> list = new Array<Planet>();
                    foreach (Planet planet in this.empire.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list.Add(planet);
                    }                                //I Changed this line so it would pick the LEAST busy shipyard - Gretman
                    IOrderedEnumerable<Planet> orderedEnumerable = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)list, (Func<Planet, float>)(planet => planet.ConstructionQueue.Count ));

                    //Then I got excited, and decided to make it also look for the closest  =)
                    int TotalPlanets = Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable);
                    if (TotalPlanets <= 0) break;

                    int leastque = Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, 0).ConstructionQueue.Count;
                    float leastdist = float.MaxValue;   //So the first on the list will always overwrite this
                    int bestplanet = 0;

                    for (short looper = 0; looper < TotalPlanets; looper++)
                    {   //Look through the list, of all the ones that have the same queue (likely empty) choose the closest of those
                        
                        //if the queue of this one is bigger than the last, then we already found all the least busy
                        if (Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, looper).ConstructionQueue.Count > leastque) break;

                        //If the distance from this planet to the build site is less than the last one, mark this the best planet to assign construction to
                        float currentdist = Vector2.Distance(orderedEnumerable.ElementAt(looper).Position, this.BuildPosition);
                        if (currentdist < leastdist)
                        {
                            bestplanet = looper;    //Mark this one as the best
                            leastdist = currentdist;
                        }
                    }
                    //after all that, assign the contruction site based on the best found above
                    PlanetBuildingAt = orderedEnumerable.ElementAt(bestplanet);

                    //Ok, i'm done   -Gretman

                    QueueItem queueItem = new QueueItem();
                    queueItem.isShip = true;
                    queueItem.DisplayName = "Construction Ship";
                    queueItem.QueueNumber = PlanetBuildingAt.ConstructionQueue.Count; //Gretman
                    queueItem.sData = ResourceManager.ShipsDict[EmpireManager.Player.data.CurrentConstructor].GetShipData();
                    queueItem.Goal = this;
                    queueItem.Cost = ResourceManager.ShipsDict[this.ToBuildUID].GetCost(this.empire);
                    queueItem.NotifyOnEmpty = false;
                    if (!string.IsNullOrEmpty(this.empire.data.CurrentConstructor) && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentConstructor))
                    {
                        this.beingBuilt = ResourceManager.ShipsDict[this.empire.data.CurrentConstructor];
                    }
                    else
                    {
                        this.beingBuilt = null;
                        string empiredefaultShip = this.empire.data.DefaultConstructor;
                        if (string.IsNullOrEmpty(empiredefaultShip))
                        {
                            empiredefaultShip = this.empire.data.DefaultSmallTransport;
                        }
                        ResourceManager.ShipsDict.TryGetValue(empiredefaultShip, out this.beingBuilt);
                        //if(this.beingBuilt == null)
                        //{
                        //    ResourceManager.ShipsDict.TryGetValue(ResourceManager.GetEmpireByName(this.empire.data.Traits.Name).DefaultSmallTransport, out this.beingBuilt);
                        //}
                        this.empire.data.DefaultConstructor = empiredefaultShip;
                    }
                    Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, bestplanet).ConstructionQueue.Add(queueItem); //Gretman
                    ++this.Step;
                    break;
                case 4:
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }

        private void DoMarkedColonizeGoal()
        {

            switch (this.Step)
            {
                case 0:
                    bool flag1 = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if (ship.isColonyShip && !ship.isPlayerShip() && (ship.GetAI() != null && ship.GetAI().State != AIState.Colonize))
                        {
                            this.colonyShip = ship;
                            flag1 = true;
                        }
                    }
                    Planet planet1 = (Planet)null;
                    if (!flag1)
                    {
                        Array<Planet> list = new Array<Planet>();
                        foreach (Planet planet2 in this.empire.GetPlanets())
                        {
                            if (planet2.HasShipyard && planet2.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                                list.Add(planet2);
                        }
                        int num1 = 9999999;
                        foreach (Planet planet2 in list)
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.NetProductionPerTurn);
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                        if (planet1 == null)
                            break;
                        if (this.empire.isPlayer && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentAutoColony))
                        {
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[this.empire.data.CurrentAutoColony].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[this.empire.data.CurrentAutoColony].GetCost(this.empire)
                            });
                            this.PlanetBuildingAt = planet1;
                            ++this.Step;
                            break;
                        }
                        else
                        {
                            QueueItem queueItem = new QueueItem();
                            queueItem.isShip = true;
                            queueItem.QueueNumber = planet1.ConstructionQueue.Count;
                            if (ResourceManager.ShipsDict.ContainsKey(this.empire.data.DefaultColonyShip))
                            {
                                queueItem.sData = ResourceManager.ShipsDict[this.empire.data.DefaultColonyShip].GetShipData();
                            }
                            else
                            {
                                queueItem.sData = ResourceManager.ShipsDict[ResourceManager.GetEmpireByName(this.empire.data.Traits.Name).DefaultColonyShip].GetShipData();
                                this.empire.data.DefaultColonyShip = ResourceManager.GetEmpireByName(this.empire.data.Traits.Name).DefaultColonyShip;
                            }
                            queueItem.Goal = this;
                            queueItem.NotifyOnEmpty = false;
                            queueItem.Cost = ResourceManager.ShipsDict[this.empire.data.DefaultColonyShip].GetCost(this.empire);
                            planet1.ConstructionQueue.Add(queueItem);
                            this.PlanetBuildingAt = planet1;
                            ++this.Step;
                            break;
                        }
                    }
                    else
                    {
                        this.Step = 2;
                        this.DoMarkedColonizeGoal();
                        break;
                    }
                case 1:
                    bool flag2 = false;
                    if (this.PlanetBuildingAt != null)                        
                        foreach (QueueItem queueItem in (Array<QueueItem>)this.PlanetBuildingAt.ConstructionQueue)
                        {
                            if (queueItem.isShip && ResourceManager.ShipsDict[queueItem.sData.Name].isColonyShip)
                                flag2 = true;
                        }
                    if (!flag2)
                    {
                        this.PlanetBuildingAt = (Planet)null;
                        this.Step = 0;
                    }
                    if (this.markedPlanet.Owner == null)
                        break;
                    foreach (KeyValuePair<Empire, Relationship> Them in this.empire.AllRelations)
                        this.empire.GetGSAI().CheckClaim(Them, this.markedPlanet);
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
                case 2:
                    if (this.markedPlanet.Owner != null)
                    {
                        foreach (KeyValuePair<Empire, Relationship> Them in this.empire.AllRelations)
                            this.empire.GetGSAI().CheckClaim(Them, this.markedPlanet);
                        this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                        break;
                    }
                    else
                    {
                        bool flag3;
                        if (this.colonyShip == null)
                        {
                            flag3 = false;
                            foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                            {
                                if (ship.isColonyShip && !ship.isPlayerShip() && (ship.GetAI() != null && ship.GetAI().State != AIState.Colonize))
                                {
                                    this.colonyShip = ship;
                                    flag3 = true;
                                }
                            }
                        }
                        else
                            flag3 = true;
                        if (flag3)
                        {
                            this.colonyShip.DoColonize(this.markedPlanet, this);
                            this.Step = 3;
                            break;
                        }
                        else
                        {
                            this.Step = 0;
                            break;
                        }
                    }
                case 3:
                    if (this.colonyShip == null)
                    {
                        this.Step = 0;
                        break;
                    }
                    else if (this.colonyShip != null && this.colonyShip.Active && this.colonyShip.GetAI().State != AIState.Colonize)
                    {
                        this.Step = 0;
                        break;
                    }
                    else if (this.colonyShip != null && !this.colonyShip.Active && this.markedPlanet.Owner == null)
                    {
                        this.Step = 0;
                        break;
                    }
                    else
                    {
                        if (this.markedPlanet.Owner == null)
                            break;
                        foreach (KeyValuePair<Empire, Relationship> Them in this.empire.AllRelations)
                            this.empire.GetGSAI().CheckClaim(Them, this.markedPlanet);
                        this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                        this.colonyShip.GetAI().State = AIState.AwaitingOrders;
                        this.Step = 4;
                        break;
                    }
            }
        }

        private void DoIncreaseFreightersGoal()
        {
            switch (this.Step)
            {
                case 0:
                    bool flag1 = false;

                    for (int index = 0; index < this.empire.GetShips().Count; ++index)
                    {
                        Ship ship = this.empire.GetShips()[index];
                        if (ship != null && !ship.isColonyShip && !ship.isConstructor && ship.CargoSpace_Max >0 && (ship.shipData.Role == ShipData.RoleName.freighter && (ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.shipData.ShipCategory == ShipData.Category.Unclassified) 
                            && !ship.isPlayerShip()) 
                            && (ship.GetAI() != null && ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader))
                        {
                            this.freighter = ship;
                            flag1 = true;
                        }
                    }
                    if (flag1)
                    {
                        this.Step = 2;
                        break;
                    }
                    else
                    {
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in new Array<Planet>(empire.GetPlanets()))
                        {
                            if (planet.HasShipyard && planet.ParentSystem.combatTimer <=0
                                && planet.developmentLevel >2
                                && planet.colonyType != Planet.ColonyType.Research
                                && (planet.colonyType != Planet.ColonyType.Industrial || planet.developmentLevel >3)
                                )  //fbedard: do not build freighter if combat in system
                                list1.Add(planet);
                        }
                        
                        Planet planet1 = (Planet)null;
                        int num1 = 9999999;
                        
                        foreach (Planet planet2 in list1)
                        {
                            int num2 = 0;
                            int finCon = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                            {
                                num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.NetProductionPerTurn);
                                if (queueItem.Goal != null && queueItem.Goal.GoalName == "IncreaseFreighters")
                                    finCon++;
                            }
                            if (finCon > 2)
                                continue;
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                        if (planet1 == null)
                        {
                            break;
                        }
                        if (this.empire.isPlayer && this.empire.AutoFreighters && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentAutoFreighter))
                        {
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetCost(this.empire),
                                NotifyOnEmpty =false
                            });
                            ++this.Step;
                            break;
                        }
                        else
                        {
                            Array<Ship> list2 = new Array<Ship>();
                            foreach (string index in this.empire.ShipsWeCanBuild)
                            {
                                Ship ship = ResourceManager.ShipsDict[index];
                                if (!ship.isColonyShip && !ship.isConstructor && ship.CargoSpace_Max >0
                                    && (ship.shipData.Role == ShipData.RoleName.freighter 
                                    && (ship.shipData.ShipCategory == ShipData.Category.Civilian || ship.shipData.ShipCategory == ShipData.Category.Unclassified)))
                                    list2.Add(ship);
                            }
                            Ship toBuild = list2
                                .OrderByDescending(ship => ship.CargoSpace_Max <= empire.cargoNeed *.5f  ? ship.CargoSpace_Max : 0)
                                .ThenByDescending(ship => (int)(ship.WarpThrust / ship.Mass/1000f))
                                .ThenByDescending(ship => ship.Thrust / ship.Mass)
                                .FirstOrDefault();

                        

                            if(toBuild == null)
                            {
                                break;
                            }
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                //sData = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetShipData(),
                                sData = toBuild.GetShipData(),
                                Goal = this,
                                //Cost = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetCost(this.empire)
                                Cost = toBuild.GetCost(this.empire)
                            });
                            ++this.Step;
                            break;
                        }                        
                    }
                case 2:
                    bool flag2 = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian) && (!ship.isPlayerShip() && ship.GetAI() != null) && (ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader && (!ship.GetAI().HasPriorityOrder && ship.GetAI().State != AIState.Refit)) && ship.GetAI().State != AIState.Scrap)
                        {
                            this.freighter = ship;
                            flag2 = true;
                        }
                    }
                    if (!flag2)
                        break;
                    this.freighter.GetAI().State = AIState.SystemTrader;                    
                    this.freighter.GetAI().OrderTrade(0.1f);
                    this.empire.ReportGoalComplete(this);
                    break;
            }
        }

        private void DoBuildScoutGoal()
        {
            switch (this.Step)
            {
                case 0:
                    Array<Planet> list1 = new Array<Planet>();
                    foreach (Planet planet in this.empire.GetPlanets())
                    {
                        if (planet.HasShipyard)
                            list1.Add(planet);
                    }
                    Planet planet1 = (Planet)null;
                    int num1 = 9999999;
                    foreach (Planet planet2 in list1)
                    {
                        int num2 = 0;
                        foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                            num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                        if (num2 < num1)
                        {
                            num1 = num2;
                            planet1 = planet2;
                        }
                    }
                    if (planet1 == null)
                        break;
                    if (EmpireManager.Player == this.empire && ResourceManager.ShipsDict.ContainsKey(EmpireManager.Player.data.CurrentAutoScout))
                    {
                        planet1.ConstructionQueue.Add(new QueueItem()
                        {
                            isShip = true,
                            QueueNumber = planet1.ConstructionQueue.Count,
                            sData = ResourceManager.ShipsDict[EmpireManager.Player.data.CurrentAutoScout].GetShipData(),
                            Goal = this,
                            Cost = ResourceManager.ShipsDict[EmpireManager.Player.data.CurrentAutoScout].GetCost(this.empire),
                            NotifyOnEmpty=false
                        });
                        ++this.Step;
                        break;
                    }
                    else
                    {
                        Array<Ship> list2 = new Array<Ship>();
                        foreach (string index in this.empire.ShipsWeCanBuild)
                        {
                            if (ResourceManager.ShipsDict[index].shipData.Role == ShipData.RoleName.scout)
                                list2.Add(ResourceManager.ShipsDict[index]);
                        }
                        IOrderedEnumerable<Ship> orderedEnumerable = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list2, (Func<Ship, float>)(ship => ship.PowerFlowMax - ship.ModulePowerDraw));
                        if (Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable) <= 0)
                            break;
                        planet1.ConstructionQueue.Add(new QueueItem()
                        {
                            isShip = true,
                            QueueNumber = planet1.ConstructionQueue.Count,
                            sData = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable).Name].GetShipData(),
                            Goal = this,
                            Cost = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable).Name].GetCost(this.empire)
                        });
                        ++this.Step;
                        break;
                    }
                case 2:
                    bool flag = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if ((ship.shipData.Role == ShipData.RoleName.scout || ship.Name == EmpireManager.Player.data.CurrentAutoScout) && !ship.isPlayerShip())
                        {
                            this.freighter = ship;
                            flag = true;
                        }
                    }
                    if (!flag)
                        break;
                    this.freighter.GetAI().OrderExplore();
                    this.empire.ReportGoalComplete(this);
                    break;
            }
        }

        private void DoIncreasePassTranGoal()
        {
            switch (this.Step)
            {
                case 0:
                    bool flag1 = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor && ship.CargoSpace_Max >0 && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian) && (!ship.isPlayerShip() && ship.GetAI() != null) && (ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader))
                        {
                            this.passTran = ship;
                            flag1 = true;
                        }
                    }
                    if (flag1)
                    {
                        this.Step = 2;
                        break;
                    }
                    else
                    {
                        Array<Planet> list1 = new Array<Planet>();
                        foreach (Planet planet in this.empire.GetPlanets())
                        {
                            if (planet.HasShipyard && planet.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                                list1.Add(planet);
                        }
                        Planet planet1 = (Planet)null;
                        int num1 = 9999999;
                        foreach (Planet planet2 in list1)
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (Array<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
                        }
                        if (planet1 == null)
                            break;
                        if (this.empire.isPlayer && this.empire.AutoFreighters && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentAutoFreighter))
                        {
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[this.empire.data.CurrentAutoFreighter].GetCost(this.empire),
                                NotifyOnEmpty=false
                            });
                            ++this.Step;
                            break;
                        }
                        else
                        {
                            Array<Ship> list2 = new Array<Ship>();
                            foreach (string index in this.empire.ShipsWeCanBuild)
                            {
                                if (!ResourceManager.ShipsDict[index].isColonyShip && !ResourceManager.ShipsDict[index].isConstructor && (ResourceManager.ShipsDict[index].shipData.Role == ShipData.RoleName.freighter || ResourceManager.ShipsDict[index].shipData.ShipCategory == ShipData.Category.Civilian))
                                    list2.Add(ResourceManager.ShipsDict[index]);
                            }
                            IOrderedEnumerable<Ship> orderedEnumerable1 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list2, (Func<Ship, float>)(ship => ship.CargoSpace_Max));
                            Array<Ship> list3 = new Array<Ship>();
                            foreach (Ship ship in (IEnumerable<Ship>)orderedEnumerable1)
                            {
                                if (!ship.isColonyShip && (double)ship.CargoSpace_Max >= (double)Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable1).CargoSpace_Max)
                                    list3.Add(ship);
                            }
                            IOrderedEnumerable<Ship> orderedEnumerable2 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list2, (Func<Ship, float>)(ship => ship.WarpThrust / ship.Mass));
                            if (Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable2) <= 0)
                                break;
                            planet1.ConstructionQueue.Add(new QueueItem()
                            {
                                isShip = true,
                                QueueNumber = planet1.ConstructionQueue.Count,
                                sData = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetShipData(),
                                Goal = this,
                                Cost = ResourceManager.ShipsDict[Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable2).Name].GetCost(this.empire)
                            });
                            ++this.Step;
                            break;
                        }
                    }
                case 2:
                    bool flag2 = false;
                    foreach (Ship ship in (Array<Ship>)this.empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian) && (!ship.isPlayerShip() && ship.GetAI() != null) && (ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader && (!ship.GetAI().HasPriorityOrder && ship.GetAI().State != AIState.Refit)) && ship.GetAI().State != AIState.Scrap)
                        {
                            this.passTran = ship;
                            flag2 = true;
                        }
                    }
                    if (flag2)
                    {
                        this.passTran.GetAI().OrderTransportPassengers(0.1f);
                        this.empire.ReportGoalComplete(this);
                        break;
                    }
                    else
                    {
                        this.Step = 0;
                        break;
                    }
            }
        }

        public struct PlanetRanker
        {
            public Planet planet;
            public float PV;
            public float Distance;
        }
    }
}
