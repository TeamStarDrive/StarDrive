using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class MarkForColonization : Goal
    {
        public const string ID = "MarkForColonization";
        public override string UID => ID;

        public MarkForColonization() : base(GoalType.Colonize)
        {
        }
        public MarkForColonization(Planet toColonize, Empire e) : base(GoalType.Colonize)
        {
            empire = e;
            markedPlanet = toColonize;
            colonyShip = null;
        }

        public override void Evaluate()
        {
            if (Held)
                return;

            switch (this.Step)
            {
                case 0:
                    bool flag1 = false;
                    foreach (Ship ship in empire.GetShips())
                    {
                        if (ship.isColonyShip && !ship.PlayerShip && (ship.AI != null && ship.AI.State != AIState.Colonize))
                        {
                            this.colonyShip = ship;
                            flag1 = true;
                        }
                    }
                    Planet planet1 = null;
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
                                queueItem.sData = ResourceManager.ShipsDict[this.empire.data.DefaultColonyShip].GetShipData();
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
                        this.Evaluate();
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
                                if (ship.isColonyShip && !ship.PlayerShip && (ship.AI != null && ship.AI.State != AIState.Colonize))
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
                    else if (this.colonyShip != null && this.colonyShip.Active && this.colonyShip.AI.State != AIState.Colonize)
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
                        this.colonyShip.AI.State = AIState.AwaitingOrders;
                        this.Step = 4;
                        break;
                    }
            }
        }
    }
}
