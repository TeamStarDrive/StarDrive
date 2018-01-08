using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        public void FactionUpdate()
        {
            string name = this.OwnerEmpire.data.Traits.Name;
            switch (name)
            {
                case "The Remnant":
                {
                    bool HasPlanets = false; // this.empire.GetPlanets().Count > 0;
                    foreach (Planet planet in this.OwnerEmpire.GetPlanets())
                    {
                        HasPlanets = true;

                        foreach (QueueItem item in planet.ConstructionQueue)
                        {
                            {
                                item.Cost = 0;
                            }
                        }
                        planet.ApplyProductiontoQueue(1, 0);
                    }
                    foreach (Ship assimilate in this.OwnerEmpire.GetShips())
                    {
                        if (assimilate.shipData.ShipStyle != "Remnant" && assimilate.shipData.ShipStyle != null 
                                && assimilate.AI.State !=  AIState.Colonize && assimilate.AI.State != AIState.Refit)
                        {
                            if (HasPlanets)
                            {
                                if (assimilate.GetStrength() <= 0)
                                {
                                    Planet target = null;
                                    if (assimilate.System != null)
                                    {
                                            target = assimilate.System.PlanetList
                                                .Find(owner => owner.Owner != this.OwnerEmpire && owner.Owner != null);
                                            
                                    }
                                    if (target != null)
                                    {
                                        assimilate.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender",
                                            assimilate.loyalty));
                                        assimilate.isColonyShip = true;

                                            // @todo this looks like FindMinFiltered
                                            //Planet capture = Empire.Universe.PlanetsDict.Values
                                            //    .Where(potentials => potentials.Owner == null && potentials.habitable)
                                            //    .OrderBy(potentials => Vector2.Distance(assimilate.Center,
                                            //        potentials.Center))
                                            //    .FirstOrDefault();

                                        Planet capture = Empire.Universe.PlanetsDict.Values.ToArray().FindMaxFiltered(
                                                potentials => potentials.Owner == null && potentials.Habitable,
                                                potentials => -assimilate.Center.SqDist(potentials.Center));
                                                
                                        if (capture != null)
                                            assimilate.AI.OrderColonization(capture);
                                    }
                                }
                                else
                                {
                                    if (assimilate.Size < 50)
                                        assimilate.AI.OrderRefitTo("Heavy Drone");
                                    else if (assimilate.Size < 100)
                                        assimilate.AI.OrderRefitTo("Remnant Slaver");
                                    else if (assimilate.Size >= 100)
                                        assimilate.AI.OrderRefitTo("Remnant Exterminator");
                                }
                            }
                            else
                            {
                                if (assimilate.GetStrength() <= 0)
                                {
                                    assimilate.isColonyShip = true;


                                    Planet capture = Empire.Universe.PlanetsDict.Values
                                        .Where(potentials => potentials.Owner == null && potentials.Habitable)
                                        .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Center))
                                        .FirstOrDefault();
                                    if (capture != null)
                                        assimilate.AI.OrderColonization(capture);
                                }
                            }
                        }
                        else
                        {
                            Planet target = null;
                            if (assimilate.System != null && assimilate.AI.State == AIState.AwaitingOrders)
                            {
                                target = assimilate.System.PlanetList
                                    .Where(owner => owner.Owner != this.OwnerEmpire && owner.Owner != null)
                                    .FirstOrDefault();
                                if (target != null && (assimilate.HasTroopBay || assimilate.hasAssaultTransporter))
                                    if (assimilate.TroopList.Count > assimilate.GetHangars().Count)
                                        assimilate.AI.OrderAssaultPlanet(target);
                            }
                        }
                    }
                }
                    break;
                case "Corsairs":
                {
                    bool AttackingSomeone = false;
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                        {
                            if (task.type != Tasks.MilitaryTask.TaskType.CorsairRaid)
                            {
                                return;
                            }
                            AttackingSomeone = true;
                        }, false, false, false);
                    }
                    if (!AttackingSomeone)
                    {
                        foreach (KeyValuePair<Empire, Relationship> r in this.OwnerEmpire.AllRelations)
                        {
                            if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || this.OwnerEmpire.GetShips().Count <= 0)
                            {
                                continue;
                            }
                            Vector2 center = new Vector2();
                            foreach (Ship ship in this.OwnerEmpire.GetShips())
                            {
                                center = center + ship.Center;
                            }
                            center = center / (float) this.OwnerEmpire.GetShips().Count;
                            IOrderedEnumerable<Planet> sortedList =
                                from planet in r.Key.GetPlanets()
                                orderby Vector2.Distance(planet.Center, center)
                                select planet;
                            Tasks.MilitaryTask task = new Tasks.MilitaryTask(this.OwnerEmpire);
                            task.SetTargetPlanet(sortedList.First<Planet>());
                            task.TaskTimer = 300f;
                            task.type = Tasks.MilitaryTask.TaskType.CorsairRaid;
                            //  lock (GlobalStats.TaskLocker)
                            {
                                this.TaskList.Add(task);
                            }
                        }
                    }
                }
                    break;
                default:
                    break;
            }


            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task => //foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != Tasks.MilitaryTask.TaskType.Exploration)
                    {
                        task.Evaluate(this.OwnerEmpire);
                    }
                    else
                    {
                        task.EndTask();
                    }
                }, false, false, false);
            }
        }
    }
}