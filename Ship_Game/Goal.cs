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
        private Fleet fleet;
        public Vector2 TetherOffset;
        public Guid TetherTarget;
        public bool Held;
        public Vector2 BuildPosition;
        public string ToBuildUID;
        private Planet PlanetBuildingAt;
        private Planet markedPlanet;
        public Ship beingBuilt;
        private Ship colonyShip;
        private Ship freighter;
        private Ship passTran;

        public Goal(Vector2 BuildPosition, string platformUID, Empire Owner)
        {
            this.GoalName = "BuildConstructionShip";
            this.type = GoalType.DeepSpaceConstruction;
            this.BuildPosition = BuildPosition;
            this.ToBuildUID = platformUID;
            this.empire = Owner;
            this.DoBuildConstructionShip();
        }

        public Goal(Troop toCopy, Empire Owner, Planet p)
        {
            this.GoalName = "Build Troop";
            this.type = GoalType.DeepSpaceConstruction;
            this.PlanetBuildingAt = p;
            this.ToBuildUID = toCopy.Name;
            this.empire = Owner;
            this.type = GoalType.BuildTroop;
        }

        public Goal(Planet toColonize, Empire e)
        {
            this.empire = e;
            this.GoalName = "MarkForColonization";
            this.type = GoalType.Colonize;
            this.markedPlanet = toColonize;
            this.colonyShip = (Ship)null;
        }

        public Goal(string ShipType, string forWhat, Empire e)
        {
            this.ToBuildUID = ShipType;
            this.empire = e;
            this.beingBuilt = ResourceManager.ShipsDict[ShipType];
            this.GoalName = forWhat;
            this.Evaluate();
        }

        public Goal()
        {
        }

        public Goal(Empire e)
        {
            this.empire = e;
        }

        public void SetFleet(Fleet f)
        {
            this.fleet = f;
        }

        public Fleet GetFleet()
        {
            return this.fleet;
        }

        public void Evaluate()
        {
            if (this.Held)
                return;
            switch (this.GoalName)
            {
                case "MarkForColonization":
                    this.DoMarkedColonizeGoal();
                    break;
                case "IncreaseFreighters":
                    this.DoIncreaseFreightersGoal();
                    break;
                case "IncreasePassengerShips":
                    this.DoIncreasePassTranGoal();
                    break;
                case "BuildDefensiveShips":
                    this.DoBuildDefensiveShipsGoal();
                    break;
                case "BuildOffensiveShips":
                    this.DoBuildOffensiveShipsGoal();
                    break;
                case "FleetRequisition":
                    this.DoFleetRequisition();
                    break;
                case "Build Troop":
                    this.DoBuildTroop();
                    break;
                case "Build Scout":
                    this.DoBuildScoutGoal();
                    break;
            }
        }

        public Planet GetPlanetWhereBuilding()
        {
            return this.PlanetBuildingAt;
        }

        public void SetColonyShip(Ship s)
        {
            this.colonyShip = s;
        }

        public void SetPlanetWhereBuilding(Planet p)
        {
            this.PlanetBuildingAt = p;
        }

        public void SetBeingBuilt(Ship s)
        {
            this.beingBuilt = s;
        }

        public void SetMarkedPlanet(Planet p)
        {
            this.markedPlanet = p;
        }

        // Edited by EVWeb
        private void DoFleetRequisition()
        {
            switch (this.Step)
            {
                case 0:
                    Planet planet1 = (Planet)null;
                    List<Planet> list = new List<Planet>();
                    foreach (Planet planet2 in this.empire.GetPlanets())
                    {
                        if (planet2.HasShipyard)
                            list.Add(planet2);
                    }
                    int num1 = 9999999;
                    int x = 0;
                    foreach (Planet planet2 in list.OrderBy(planet =>
                        {
                            float weight = -6;                      // so shipyard modifier works properly
                            switch (planet.colonyType)
                            {                                
                                case Planet.ColonyType.Core:
                                    weight -= 4;
                                    break;
                                case Planet.ColonyType.Colony:
                                    break;
                                case Planet.ColonyType.Industrial:
                                    weight -= 2;
                                    break;
                                case Planet.ColonyType.Research:
                                    weight += 6;
                                    break;
                                case Planet.ColonyType.Agricultural:
                                    weight += 6;
                                    break;
                                case Planet.ColonyType.Military:
                                    weight -= 2;
                                    break;
                                case Planet.ColonyType.TradeHub:
                                    weight -= 2;
                                    break;
                                default:
                                    break;
                            }
                            //weight -= planet.ConstructionQueue.Count;
                 
                            weight -= planet.developmentLevel;          // minus because order by goes smallest to largest, not other way round
                            weight -= planet.MineralRichness;
                            weight /= planet.ShipBuildingModifier;      // planets with shipyards in orbit should be higher in list

                            return weight;
                        }) )
                    {
                        if (x < this.empire.GetPlanets().Count *.2)     // We already checked for whether or not this is a shipyard planet
                        {
                            int num2 = 0;
                            x++;
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(queueItem.Cost - queueItem.productionTowards);


                            num2 += (int)(this.beingBuilt.GetCost(this.empire) - planet2.ProductionHere);   // Include the cost of the ship to be built on EVERY planet, not just ones with nothing to build
                            num2 = (int)(((double)num2 * (double)planet2.ShipBuildingModifier) / (planet2.GovernorOn ? (double)planet2.GetMaxProductionPotential() : (double)planet2.NetProductionPerTurn));         // Apply ship cost reduction to everything just so planets with shipyards in orbit are more likely to be picked, also, if on manual control, don't assume you can get max production
                            if (num2 < num1)
                            {
                                num1 = num2;
                                planet1 = planet2;
                            }
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
                        Cost = this.beingBuilt.GetCost(this.empire),
                        NotifyOnEmpty=false
                    });
                    ++this.Step;
                    break;
                case 2:
                    if (this.fleet != null)
                    {
                        using (List<FleetDataNode>.Enumerator enumerator = this.fleet.DataNodes.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                FleetDataNode current = enumerator.Current;
                                if (current.GoalGUID == this.guid)
                                {
                                    if (this.fleet.Ships.Count == 0)
                                        this.fleet.Position = this.beingBuilt.Position + new Vector2(RandomMath.RandomBetween(-3000f, 3000f), RandomMath.RandomBetween(-3000f, 3000f));
                                    current.SetShip(this.beingBuilt);
                                    this.beingBuilt.fleet = this.fleet;
                                    this.beingBuilt.RelativeFleetOffset = current.FleetOffset;
                                    this.fleet.AddShip(this.beingBuilt);
                                    current.GoalGUID = Guid.Empty;
                                    this.beingBuilt.GetAI().OrderMoveToFleetPosition(this.fleet.Position + this.beingBuilt.FleetOffset, this.beingBuilt.fleet.facing, new Vector2(0.0f, -1f), true, this.fleet.speed, this.fleet);
                                }
                            }
                            break;
                        }
                    }
                    else
                    {
                        this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                        break;
                    }
            }
        }

        private void DoBuildOffensiveShipsGoal()
        {
            switch (this.Step)
            {
                case 0:
                    if (this.beingBuilt == null)
                        this.beingBuilt = ResourceManager.ShipsDict[this.ToBuildUID];
                    Planet planet1 = (Planet)null;
                    List<Planet> list = new List<Planet>();
                    foreach (Planet planet2 in this.empire.GetPlanets().OrderBy(planet =>
                    {
                        float weight = 0;
                        switch (planet.colonyType)
                        {
                            case Planet.ColonyType.Core:
                                weight += 4;
                                break;
                            case Planet.ColonyType.Colony:
                                break;
                            case Planet.ColonyType.Industrial:
                                weight += 2;
                                break;
                            case Planet.ColonyType.Research:
                                weight -= 6;
                                break;
                            case Planet.ColonyType.Agricultural:
                                weight -= 6;
                                break;
                            case Planet.ColonyType.Military:
                                weight += 2;
                                break;
                            case Planet.ColonyType.TradeHub:
                                weight += 2;
                                break;
                            default:
                                break;
                        }
                        //weight -= planet.ConstructionQueue.Count;

                        weight += planet.developmentLevel;
                        weight += planet.MineralRichness;

                        return weight;
                    }))
                    {
                        if (planet2.HasShipyard  && planet2.colonyType != Planet.ColonyType.Research)
                            list.Add(planet2);
                    }
                    int num1 = 9999999;
                    int x = 0;
                    foreach (Planet planet2 in list)
                    {
                        if (x > this.empire.GetPlanets().Count * .2f)
                            break;
                        int num2 = 0;
                        foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
                            num2 += (int)((queueItem.Cost - queueItem.productionTowards) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                        if (planet2.ConstructionQueue.Count == 0)
                            num2 = (int)((this.beingBuilt.GetCost(this.empire) - planet2.ProductionHere) / planet2.GetMaxProductionPotential());//planet2.NetProductionPerTurn);
                        if (num2 < num1)
                        {
                            num1 = num2;
                            planet1 = planet2;
                        }
                    }
                    if (planet1 == null )
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
                        if (this.PlanetBuildingAt == null || this.PlanetBuildingAt.ConstructionQueue.Count == 0)
                            break;
                        //if (this.PlanetBuildingAt.ConstructionQueue[0].Goal == this)
                        //{
                        //    if (this.PlanetBuildingAt.ProductionHere > PlanetBuildingAt.MAX_STORAGE * .5f)
                        //    {
                        //        this.PlanetBuildingAt.ApplyStoredProduction(0);
                        //    }
                        //}

                        break;
                    }
                case 2:
                    this.beingBuilt.GetAI().State = AIState.AwaitingOrders;
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }

        private void DoBuildDefensiveShipsGoal()
        {
            switch (this.Step)
            {
                case 0:
                    if (this.beingBuilt == null)
                        this.beingBuilt = ResourceManager.ShipsDict[this.ToBuildUID];
                    Planet planet1 = (Planet)null;
                    List<Planet> list = new List<Planet>();
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
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
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
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
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
                    else
                        System.Diagnostics.Debug.WriteLine(string.Concat("Missing Troop "));
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

        public void ReportShipComplete(Ship ship)
        {
            if (this.GoalName == "BuildDefensiveShips")
            {
                this.beingBuilt = ship;
                ++this.Step;
            }
            if (this.GoalName == "BuildOffensiveShips")
            {
                this.beingBuilt = ship;
                ++this.Step;
            }
            if (!(this.GoalName == "FleetRequisition"))
                return;
            this.beingBuilt = ship;
            ++this.Step;
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
                    List<Planet> list = new List<Planet>();
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
                        float currentdist = Vector2.Distance(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, looper).Position, this.BuildPosition);
                        if (currentdist < leastdist)
                        {
                            bestplanet = looper;    //Mark this one as the best
                            leastdist = currentdist;
                        }
                    }
                    //after all that, assign the contruction site based on the best found above
                    this.PlanetBuildingAt = Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, bestplanet);

                    //Ok, i'm done   -Gretman

                    QueueItem queueItem = new QueueItem();
                    queueItem.isShip = true;
                    queueItem.DisplayName = "Construction Ship";
                    queueItem.QueueNumber = Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable, bestplanet).ConstructionQueue.Count; //Gretman
                    queueItem.sData = ResourceManager.ShipsDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).data.CurrentConstructor].GetShipData();
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
                    foreach (Ship ship in (List<Ship>)this.empire.GetShips())
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
                        List<Planet> list = new List<Planet>();
                        foreach (Planet planet2 in this.empire.GetPlanets())
                        {
                            if (planet2.HasShipyard && planet2.ParentSystem.combatTimer <= 0)  //fbedard: do not build freighter if combat in system
                                list.Add(planet2);
                        }
                        int num1 = 9999999;
                        foreach (Planet planet2 in list)
                        {
                            int num2 = 0;
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
                                num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
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
                    foreach (QueueItem queueItem in (List<QueueItem>)this.PlanetBuildingAt.ConstructionQueue)
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
                    foreach (KeyValuePair<Empire, Relationship> Them in this.empire.GetRelations())
                        this.empire.GetGSAI().CheckClaim(Them, this.markedPlanet);
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
                case 2:
                    if (this.markedPlanet.Owner != null)
                    {
                        foreach (KeyValuePair<Empire, Relationship> Them in this.empire.GetRelations())
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
                            foreach (Ship ship in (List<Ship>)this.empire.GetShips())
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
                        foreach (KeyValuePair<Empire, Relationship> Them in this.empire.GetRelations())
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
                        if (ship != null && !ship.isColonyShip && !ship.isConstructor && ((ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian) && !ship.isPlayerShip()) && (ship.GetAI() != null && ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader))
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
                        List<Planet> list1 = new List<Planet>();
                        foreach (Planet planet in this.empire.GetPlanets())
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
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
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
                                NotifyOnEmpty =false
                            });
                            ++this.Step;
                            break;
                        }
                        else
                        {
                            List<Ship> list2 = new List<Ship>();
                            foreach (string index in this.empire.ShipsWeCanBuild)
                            {
                                if (!ResourceManager.ShipsDict[index].isColonyShip && (ResourceManager.ShipsDict[index].shipData.Role == ShipData.RoleName.freighter || ResourceManager.ShipsDict[index].shipData.ShipCategory == ShipData.Category.Civilian))
                                    list2.Add(ResourceManager.ShipsDict[index]);
                            }
                            //IOrderedEnumerable<Ship> orderedEnumerable1 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list2, (Func<Ship, float>)(ship => ship.CargoSpace_Max <= empire.cargoNeed ? ship.CargoSpace_Max : 0));
                            //List<Ship> list3 = new List<Ship>();
                            Ship toBuild = list2
                                .OrderByDescending(ship => ship.CargoSpace_Max <= empire.cargoNeed *.5f  ? ship.CargoSpace_Max : 0)
                                .ThenByDescending(ship => (int)(ship.WarpThrust / ship.Mass/1000f))
                                .ThenByDescending(ship => ship.Thrust / ship.Mass)
                                .First();

                        

                            //foreach (Ship ship in (IEnumerable<Ship>)orderedEnumerable1)
                            //{
                            //    if (!ship.isColonyShip && ship.CargoSpace_Max >= Enumerable.First<Ship>((IEnumerable<Ship>)orderedEnumerable1).CargoSpace_Max)
                            //        list3.Add(ship);
                            //}
                            //IOrderedEnumerable<Ship> orderedEnumerable2 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list3, (Func<Ship, float>)(ship => ship.WarpThrust / ship.Mass));
                            //if (Enumerable.Count<Ship>((IEnumerable<Ship>)orderedEnumerable2) <= 0)
                            //    break;
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
                    foreach (Ship ship in (List<Ship>)this.empire.GetShips())
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
                    List<Planet> list1 = new List<Planet>();
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
                        foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
                            num2 += (int)(((double)queueItem.Cost - (double)queueItem.productionTowards) / (double)planet2.NetProductionPerTurn);
                        if (num2 < num1)
                        {
                            num1 = num2;
                            planet1 = planet2;
                        }
                    }
                    if (planet1 == null)
                        break;
                    if (EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) == this.empire && ResourceManager.ShipsDict.ContainsKey(EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).data.CurrentAutoScout))
                    {
                        planet1.ConstructionQueue.Add(new QueueItem()
                        {
                            isShip = true,
                            QueueNumber = planet1.ConstructionQueue.Count,
                            sData = ResourceManager.ShipsDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).data.CurrentAutoScout].GetShipData(),
                            Goal = this,
                            Cost = ResourceManager.ShipsDict[EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).data.CurrentAutoScout].GetCost(this.empire),
                            NotifyOnEmpty=false
                        });
                        ++this.Step;
                        break;
                    }
                    else
                    {
                        List<Ship> list2 = new List<Ship>();
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
                    foreach (Ship ship in (List<Ship>)this.empire.GetShips())
                    {
                        if ((ship.shipData.Role == ShipData.RoleName.scout || ship.Name == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).data.CurrentAutoScout) && !ship.isPlayerShip())
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
                    foreach (Ship ship in (List<Ship>)this.empire.GetShips())
                    {
                        if (!ship.isColonyShip && !ship.isConstructor && (ship.shipData.Role == ShipData.RoleName.freighter || ship.shipData.ShipCategory == ShipData.Category.Civilian) && (!ship.isPlayerShip() && ship.GetAI() != null) && (ship.GetAI().State != AIState.PassengerTransport && ship.GetAI().State != AIState.SystemTrader))
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
                        List<Planet> list1 = new List<Planet>();
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
                            foreach (QueueItem queueItem in (List<QueueItem>)planet2.ConstructionQueue)
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
                            List<Ship> list2 = new List<Ship>();
                            foreach (string index in this.empire.ShipsWeCanBuild)
                            {
                                if (!ResourceManager.ShipsDict[index].isColonyShip && !ResourceManager.ShipsDict[index].isConstructor && (ResourceManager.ShipsDict[index].shipData.Role == ShipData.RoleName.freighter || ResourceManager.ShipsDict[index].shipData.ShipCategory == ShipData.Category.Civilian))
                                    list2.Add(ResourceManager.ShipsDict[index]);
                            }
                            IOrderedEnumerable<Ship> orderedEnumerable1 = Enumerable.OrderByDescending<Ship, float>((IEnumerable<Ship>)list2, (Func<Ship, float>)(ship => ship.CargoSpace_Max));
                            List<Ship> list3 = new List<Ship>();
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
                    foreach (Ship ship in (List<Ship>)this.empire.GetShips())
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
