using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public void FactionUpdate()
        {
            string name = OwnerEmpire.data.Traits.Name;
            switch (name)
            {
                case "The Remnant": UpdateRemnantFaction(); break;
                case "Corsairs":
                    if (UpdateCorsairsFaction())
                        return;
                    break;
            }

            foreach (MilitaryTask task in TaskList.AtomicCopy())
            {
                if (task.type != MilitaryTask.TaskType.Exploration)
                {
                    task.Evaluate(OwnerEmpire);
                }
                else
                {
                    task.EndTask();
                }
            }
        }

        bool UpdateCorsairsFaction()
        {
            bool alreadyAttacking = false;
            // @todo Wtf?
            foreach (MilitaryTask task in TaskList)
            {
                if (task.type != MilitaryTask.TaskType.CorsairRaid)
                    return true;
                alreadyAttacking = true;
            }

            if (!alreadyAttacking)
            {
                foreach (KeyValuePair<Empire, Relationship> r in OwnerEmpire.AllRelations)
                {
                    if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || OwnerEmpire.GetShips().Count <= 0)
                    {
                        continue;
                    }

                    Vector2 center = new Vector2();
                    foreach (Ship ship in OwnerEmpire.GetShips())
                    {
                        center = center + ship.Center;
                    }

                    center = center / OwnerEmpire.GetShips().Count;
                    IOrderedEnumerable<Planet> sortedList =
                        from planet in r.Key.GetPlanets()
                        orderby Vector2.Distance(planet.Center, center)
                        select planet;
                    var task = new MilitaryTask(OwnerEmpire);
                    task.SetTargetPlanet(sortedList.First());
                    task.TaskTimer = 300f;
                    task.type = MilitaryTask.TaskType.CorsairRaid;
                    TaskList.Add(task);
                }
            }

            return false;
        }

        void UpdateRemnantFaction()
        {
            bool hasPlanets = false; // this.empire.GetPlanets().Count > 0;
            foreach (Planet planet in OwnerEmpire.GetPlanets())
            {
                hasPlanets = true;
                planet.Construction.RemnantCheatProduction();
            }

            foreach (Ship assimilate in OwnerEmpire.GetShips())
            {
                if (assimilate.shipData.ShipStyle != "Remnant" && assimilate.shipData.ShipStyle != null &&
                    assimilate.AI.State != AIState.Colonize && assimilate.AI.State != AIState.Refit)
                {
                    if (hasPlanets)
                    {
                        if (assimilate.GetStrength() <= 0)
                        {
                            Planet target = null;
                            if (assimilate.System != null)
                            {
                                target = assimilate.System.PlanetList
                                    .Find(owner => owner.Owner != OwnerEmpire && owner.Owner != null);
                            }

                            if (target != null)
                            {
                                assimilate.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender", assimilate.loyalty));
                                assimilate.isColonyShip = true;

                                Planet capture = Empire.Universe.PlanetsDict.Values.ToArray().FindMaxFiltered(
                                    potentials => potentials.Owner == null && potentials.Habitable,
                                    potentials => -assimilate.Center.SqDist(potentials.Center));

                                if (capture != null)
                                    assimilate.AI.OrderColonization(capture);
                            }
                        }
                        else
                        {
                            string shipName = "";
                            if (assimilate.SurfaceArea < 50) shipName = "Heavy Drone";
                            else if (assimilate.SurfaceArea < 100) shipName = "Remnant Slaver";
                            else if (assimilate.SurfaceArea >= 100) shipName = "Remnant Exterminator";
                            ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template);
                            if (template != null)
                                assimilate.AI.OrderRefitTo(template);
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
                    if (assimilate.System != null && assimilate.AI.State == AIState.AwaitingOrders)
                    {
                        Planet target =
                            assimilate.System.PlanetList.Find(owner => owner.Owner != OwnerEmpire && owner.Owner != null);
                        if (target != null && (assimilate.Carrier.HasTroopBays || assimilate.Carrier.HasAssaultTransporters))
                            if (assimilate.TroopList.Count > assimilate.Carrier.NumActiveHangars)
                                assimilate.AI.OrderAssaultPlanet(target);
                    }
                }
            }
        }
    }
}