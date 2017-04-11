using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class GSAI : IDisposable
    {
        public string EmpireName;

        private Empire empire;

        public BatchRemovalCollection<Goal> Goals = new BatchRemovalCollection<Goal>();

        public ThreatMatrix ThreatMatrix = new ThreatMatrix();

        public DefensiveCoordinator DefensiveCoordinator;

        private int desired_ColonyGoals = 2;

        private BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();

        public Array<AO> AreasOfOperations = new Array<AO>();

        private int DesiredAgentsPerHostile = 2;

        private int DesiredAgentsPerNeutral = 1;

        private int DesiredAgentCount;

        private int BaseAgents;

        public Array<int> UsedFleets = new Array<int>();

        public BatchRemovalCollection<MilitaryTask> TaskList = new BatchRemovalCollection<MilitaryTask>();

        private int numberOfShipGoals = 6;

        private int numberTroopGoals = 2;

        private Map<Ship, Array<Ship>> InterceptorDict = new Map<Ship, Array<Ship>>();

        public Array<MilitaryTask> TasksToAdd = new Array<MilitaryTask>();

        public int num_current_invasion_tasks;

        private Array<Planet> DesiredPlanets = new Array<Planet>();

        private int FirstDemand = 20;

        private int SecondDemand = 75;
        public float FreighterUpkeep = 0f;
        public float PlatformUpkeep = 0f;
        public float StationUpkeep = 0f;
        public float spyBudget = 0;
        public float toughnuts = 0;
        //public float SSPBudget = 0;
        public float DefStr;

        //private int ThirdDemand = 75;

        private GSAI.ResearchStrategy res_strat = GSAI.ResearchStrategy.Scripted;
        float minimumWarpRange = GlobalStats.MinimumWarpRange;
        //SizeLimiter

        public int recyclepool =0;
        private float buildCapacity = 0;

        //added by gremlin: Smart Ship research
        Ship BestCombatShip;
        //string BestCombatHull = "";
        //string BestSupportShip = "";
        private OffensiveForcePoolManager OffensiveForcePoolManager;

        private string postResearchTopic = "";
        public GSAI(Empire e)
        {
            this.EmpireName = e.data.Traits.Name;
            this.empire = e;
            this.DefensiveCoordinator = new DefensiveCoordinator(e);
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            if (this.empire.data.EconomicPersonality != null)
            {
                this.numberOfShipGoals = this.numberOfShipGoals + this.empire.data.EconomicPersonality.ShipGoalsPlus;
            }
           // this.desired_ColonyGoals = (int)this.empire.data.difficulty;
     
        }

        public void AcceptOffer(Offer ToUs, Offer FromUs, Empire us, Empire Them)
        {
            if (ToUs.PeaceTreaty)
            {
                empire.GetRelations(Them).AtWar = false;
                empire.GetRelations(Them).PreparingForWar = false;
                empire.GetRelations(Them).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                empire.GetRelations(Them).WarHistory.Add(empire.GetRelations(Them).ActiveWar);
                if (this.empire.data.DiplomaticPersonality != null)
                {
                    empire.GetRelations(Them).Posture = Posture.Neutral;
                    if (empire.GetRelations(Them).Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        empire.GetRelations(Them).Anger_FromShipsInOurBorders = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (empire.GetRelations(Them).Anger_TerritorialConflict > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        empire.GetRelations(Them).Anger_TerritorialConflict = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
                    }
                }
                empire.GetRelations(Them).Anger_MilitaryConflict = 0f;
                empire.GetRelations(Them).WarnedAboutShips = false;
                empire.GetRelations(Them).WarnedAboutColonizing = false;
                empire.GetRelations(Them).HaveRejected_Demand_Tech = false;
                empire.GetRelations(Them).HaveRejected_OpenBorders = false;
                empire.GetRelations(Them).HaveRejected_TRADE = false;
                empire.GetRelations(Them).HasDefenseFleet = false;
                if (empire.GetRelations(Them).DefenseFleet != -1)
                {
                    this.empire.GetFleetsDict()[empire.GetRelations(Them).DefenseFleet].FleetTask.EndTask();
                }
                //lock (GlobalStats.TaskLocker)
                {
                    //foreach (MilitaryTask task in this.TaskList)
                    this.TaskList.ForEach(task =>
                    {
                        if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != Them)
                        {
                            return;
                        }
                        task.EndTask();
                    },false,false,false);
                }
                empire.GetRelations(Them).ActiveWar = null;
                Them.GetRelations(this.empire).AtWar = false;
                Them.GetRelations(this.empire).PreparingForWar = false;
                Them.GetRelations(this.empire).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                Them.GetRelations(this.empire).WarHistory.Add(Them.GetRelations(this.empire).ActiveWar);
                Them.GetRelations(this.empire).Posture = Posture.Neutral;
                if (EmpireManager.Player != Them)
                {
                    if (Them.GetRelations(this.empire).Anger_FromShipsInOurBorders > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_FromShipsInOurBorders = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (Them.GetRelations(this.empire).Anger_TerritorialConflict > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_TerritorialConflict = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    Them.GetRelations(this.empire).Anger_MilitaryConflict = 0f;
                    Them.GetRelations(this.empire).WarnedAboutShips = false;
                    Them.GetRelations(this.empire).WarnedAboutColonizing = false;
                    Them.GetRelations(this.empire).HaveRejected_Demand_Tech = false;
                    Them.GetRelations(this.empire).HaveRejected_OpenBorders = false;
                    Them.GetRelations(this.empire).HaveRejected_TRADE = false;
                    if (Them.GetRelations(this.empire).DefenseFleet != -1)
                    {
                        Them.GetFleetsDict()[Them.GetRelations(this.empire).DefenseFleet].FleetTask.EndTask();
                    }
                    //lock (GlobalStats.TaskLocker)
                    {
                        //foreach (MilitaryTask task in Them.GetGSAI().TaskList)
                        Them.GetGSAI().TaskList.ForEach(task =>
                        {
                            if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != this.empire)
                            {
                                return;
                            }
                            task.EndTask();
                        },false,false,false);
                    }
                }
                Them.GetRelations(this.empire).ActiveWar = null;
                if (Them == Empire.Universe.PlayerEmpire || this.empire == Empire.Universe.PlayerEmpire)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(this.empire, Them);
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(Them).Known && Empire.Universe.PlayerEmpire.GetRelations(this.empire).Known)
                {
                    Empire.Universe.NotificationManager.AddPeaceTreatyEnteredNotification(this.empire, Them);
                }
            }
            if (ToUs.NAPact)
            {
                us.GetRelations(Them).Treaty_NAPact = true;
                TrustEntry te = new TrustEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;
                    if (name != null)
                    {
                        if (str == "Pacifist")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str == "Cunning")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str == "Xenophobic")
                        {
                            te.TrustCost = 15f;
                        }
                        else if (str == "Aggressive")
                        {
                            te.TrustCost = 35f;
                        }
                        else if (str == "Honorable")
                        {
                            te.TrustCost = 5f;
                        }
                        else if (str == "Ruthless")
                        {
                            te.TrustCost = 50f;
                        }
                    }
                }
                te.Type = TrustEntryType.Treaty;
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.NAPact)
            {
                Them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != Them)
                {
                    TrustEntry te = new TrustEntry();
                    string name1 = Them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        if (str1 == "Pacifist")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str1 == "Cunning")
                        {
                            te.TrustCost = 0f;
                        }
                        else if (str1 == "Xenophobic")
                        {
                            te.TrustCost = 15f;
                        }
                        else if (str1 == "Aggressive")
                        {
                            te.TrustCost = 35f;
                        }
                        else if (str1 == "Honorable")
                        {
                            te.TrustCost = 5f;
                        }
                        else if (str1 == "Ruthless")
                        {
                            te.TrustCost = 50f;
                        }
                    }
                    te.Type = TrustEntryType.Treaty;
                    Them.GetRelations(us).TrustEntries.Add(te);
                }
            }
            if (ToUs.TradeTreaty)
            {
                us.GetRelations(Them).Treaty_Trade = true;
                us.GetRelations(Them).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.TradeTreaty)
            {
                Them.GetRelations(us).Treaty_Trade = true;
                Them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 0.1f,
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            if (ToUs.OpenBorders)
            {
                us.GetRelations(Them).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            if (FromUs.OpenBorders)
            {
                Them.GetRelations(us).Treaty_OpenBorders = true;
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = 5f,
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                //Added by McShooterz:
                //Them.UnlockTech(tech);
                Them.AcquireTech(tech, us);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
                    TurnTimer = 40,
                    Type = TrustEntryType.Technology
                };
                us.GetRelations(Them).TrustEntries.Add(te);
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                //Added by McShooterz:
                //us.UnlockTech(tech);
                us.AcquireTech(tech, Them);
                if (Empire.Universe.PlayerEmpire == Them)
                {
                    continue;
                }
                TrustEntry te = new TrustEntry()
                {
                    TrustCost = (Them.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
                    Type = TrustEntryType.Treaty
                };
                Them.GetRelations(us).TrustEntries.Add(te);
            }
            foreach (string Art in FromUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in us.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                us.RemoveArtifact(toGive);
                Them.AddArtifact(toGive);
            }
            foreach (string Art in ToUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in Them.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                Them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.empire)
                        {
                            continue;
                        }
                        pgs.TroopsHere[0].SetPlanet(p);
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    toRemove.Add(p);
                    p.Owner = Them;
                    Them.AddPlanet(p);
                    if (Them != EmpireManager.Player)
                    {
                        p.colonyType = Them.AssessColonyNeeds(p);
                    }
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    TrustEntry te = new TrustEntry()
                    {
                        TrustCost = (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
                        TurnTimer = 40,
                        Type = TrustEntryType.Technology
                    };
                    us.GetRelations(Them).TrustEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in Them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
                        {
                            continue;
                        }
                        pgs.TroopsHere[0].SetPlanet(p);
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    if (Empire.Universe.PlayerEmpire != Them)
                    {
                        TrustEntry te = new TrustEntry()
                        {
                            TrustCost = (Them.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
                            TurnTimer = 40,
                            Type = TrustEntryType.Technology
                        };
                        Them.GetRelations(us).TrustEntries.Add(te);
                    }
                    if (us == EmpireManager.Player)
                    {
                        continue;
                    }
                    p.colonyType = us.AssessColonyNeeds(p);
                }
                foreach (Planet p in toRemove)
                {
                    Them.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
        }

        public void AcceptThreat(Offer ToUs, Offer FromUs, Empire us, Empire Them)
        {
            if (ToUs.PeaceTreaty)
            {
                empire.GetRelations(Them).AtWar = false;
                empire.GetRelations(Them).PreparingForWar = false;
                empire.GetRelations(Them).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                empire.GetRelations(Them).WarHistory.Add(empire.GetRelations(Them).ActiveWar);
                empire.GetRelations(Them).Posture = Posture.Neutral;
                if (empire.GetRelations(Them).Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    empire.GetRelations(Them).Anger_FromShipsInOurBorders = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
                }
                if (empire.GetRelations(Them).Anger_TerritorialConflict > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3))
                {
                    empire.GetRelations(Them).Anger_TerritorialConflict = (float)(this.empire.data.DiplomaticPersonality.Territorialism / 3);
                }
                empire.GetRelations(Them).Anger_MilitaryConflict = 0f;
                empire.GetRelations(Them).WarnedAboutShips = false;
                empire.GetRelations(Them).WarnedAboutColonizing = false;
                empire.GetRelations(Them).HaveRejected_Demand_Tech = false;
                empire.GetRelations(Them).HaveRejected_OpenBorders = false;
                empire.GetRelations(Them).HaveRejected_TRADE = false;
                empire.GetRelations(Them).HasDefenseFleet = false;
                if (empire.GetRelations(Them).DefenseFleet != -1)
                {
                    this.empire.GetFleetsDict()[empire.GetRelations(Them).DefenseFleet].FleetTask.EndTask();
                }
                //lock (GlobalStats.TaskLocker)
                {
                    this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                    {
                        if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != Them)
                        {
                            return;
                        }
                        task.EndTask();
                    });
                }
                empire.GetRelations(Them).ActiveWar = null;
                Them.GetRelations(this.empire).AtWar = false;
                Them.GetRelations(this.empire).PreparingForWar = false;
                Them.GetRelations(this.empire).ActiveWar.EndStarDate = Empire.Universe.StarDate;
                Them.GetRelations(this.empire).WarHistory.Add(Them.GetRelations(this.empire).ActiveWar);
                Them.GetRelations(this.empire).Posture = Posture.Neutral;
                if (EmpireManager.Player != Them)
                {
                    if (Them.GetRelations(this.empire).Anger_FromShipsInOurBorders > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_FromShipsInOurBorders = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    if (Them.GetRelations(this.empire).Anger_TerritorialConflict > (float)(Them.data.DiplomaticPersonality.Territorialism / 3))
                    {
                        Them.GetRelations(this.empire).Anger_TerritorialConflict = (float)(Them.data.DiplomaticPersonality.Territorialism / 3);
                    }
                    Them.GetRelations(this.empire).Anger_MilitaryConflict = 0f;
                    Them.GetRelations(this.empire).WarnedAboutShips = false;
                    Them.GetRelations(this.empire).WarnedAboutColonizing = false;
                    Them.GetRelations(this.empire).HaveRejected_Demand_Tech = false;
                    Them.GetRelations(this.empire).HaveRejected_OpenBorders = false;
                    Them.GetRelations(this.empire).HaveRejected_TRADE = false;
                    if (Them.GetRelations(this.empire).DefenseFleet != -1)
                    {
                        Them.GetFleetsDict()[Them.GetRelations(this.empire).DefenseFleet].FleetTask.EndTask();
                    }
                    //lock (GlobalStats.TaskLocker)
                    {
                        Them.GetGSAI().TaskList.ForEach(task =>//foreach (MilitaryTask task in Them.GetGSAI().TaskList)
                        {
                            if (task.GetTargetPlanet() == null || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != this.empire)
                            {
                                return;
                            }
                            task.EndTask();
                        },false,false,false);
                    }
                }
                Them.GetRelations(this.empire).ActiveWar = null;
            }
            if (ToUs.NAPact)
            {
                us.GetRelations(Them).Treaty_NAPact = true;
                FearEntry te = new FearEntry();
                if (Empire.Universe.PlayerEmpire != us)
                {
                    string name = us.data.DiplomaticPersonality.Name;
                    string str = name;
                    if (name != null)
                    {
                        if (str == "Pacifist")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str == "Cunning")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str == "Xenophobic")
                        {
                            te.FearCost = 15f;
                        }
                        else if (str == "Aggressive")
                        {
                            te.FearCost = 35f;
                        }
                        else if (str == "Honorable")
                        {
                            te.FearCost = 5f;
                        }
                        else if (str == "Ruthless")
                        {
                            te.FearCost = 50f;
                        }
                    }
                }
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.NAPact)
            {
                Them.GetRelations(us).Treaty_NAPact = true;
                if (Empire.Universe.PlayerEmpire != Them)
                {
                    FearEntry te = new FearEntry();
                    string name1 = Them.data.DiplomaticPersonality.Name;
                    string str1 = name1;
                    if (name1 != null)
                    {
                        if (str1 == "Pacifist")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str1 == "Cunning")
                        {
                            te.FearCost = 0f;
                        }
                        else if (str1 == "Xenophobic")
                        {
                            te.FearCost = 15f;
                        }
                        else if (str1 == "Aggressive")
                        {
                            te.FearCost = 35f;
                        }
                        else if (str1 == "Honorable")
                        {
                            te.FearCost = 5f;
                        }
                        else if (str1 == "Ruthless")
                        {
                            te.FearCost = 50f;
                        }
                    }
                    Them.GetRelations(us).FearEntries.Add(te);
                }
            }
            if (ToUs.TradeTreaty)
            {
                us.GetRelations(Them).Treaty_Trade = true;
                us.GetRelations(Them).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.TradeTreaty)
            {
                Them.GetRelations(us).Treaty_Trade = true;
                Them.GetRelations(us).Treaty_Trade_TurnsExisted = 0;
                FearEntry te = new FearEntry()
                {
                    FearCost = 0.1f
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            if (ToUs.OpenBorders)
            {
                us.GetRelations(Them).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            if (FromUs.OpenBorders)
            {
                Them.GetRelations(us).Treaty_OpenBorders = true;
                FearEntry te = new FearEntry()
                {
                    FearCost = 5f
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                Them.UnlockTech(tech);
                if (Empire.Universe.PlayerEmpire == us)
                {
                    continue;
                }
                FearEntry te = new FearEntry()
                {
                    FearCost = (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f),
                    TurnTimer = 40
                };
                us.GetRelations(Them).FearEntries.Add(te);
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                us.UnlockTech(tech);
                if (Empire.Universe.PlayerEmpire == Them)
                {
                    continue;
                }
                FearEntry te = new FearEntry()
                {
                    FearCost = (Them.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f)
                };
                Them.GetRelations(us).FearEntries.Add(te);
            }
            foreach (string Art in FromUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in us.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                us.RemoveArtifact(toGive);
                Them.AddArtifact(toGive);
            }
            foreach (string Art in ToUs.ArtifactsOffered)
            {
                Artifact toGive = ResourceManager.ArtifactsDict[Art];
                foreach (Artifact arti in Them.data.OwnedArtifacts)
                {
                    if (arti.Name != Art)
                    {
                        continue;
                    }
                    toGive = arti;
                }
                Them.RemoveArtifact(toGive);
                us.AddArtifact(toGive);

            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != this.empire)
                        {
                            continue;
                        }
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    toRemove.Add(p);
                    p.Owner = Them;
                    Them.AddPlanet(p);
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    FearEntry te = new FearEntry();
                    if (value < 15f)
                    {
                        value = 15f;
                    }
                    te.FearCost = (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
                    te.TurnTimer = 40;
                    us.GetRelations(Them).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    us.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                Array<Planet> toRemove = new Array<Planet>();
                Array<Ship> TroopShips = new Array<Ship>();
                foreach (Planet p in Them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    toRemove.Add(p);
                    p.Owner = us;
                    us.AddPlanet(p);
                    p.system.OwnerList.Clear();
                    foreach (Planet pl in p.system.PlanetList)
                    {
                        if (pl.Owner == null || p.system.OwnerList.Contains(pl.Owner))
                        {
                            continue;
                        }
                        p.system.OwnerList.Add(pl.Owner);
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.TroopsHere.Count <= 0 || pgs.TroopsHere[0].GetOwner() != Them)
                        {
                            continue;
                        }
                        TroopShips.Add(pgs.TroopsHere[0].Launch());
                    }
                    if (Empire.Universe.PlayerEmpire == Them)
                    {
                        continue;
                    }
                    FearEntry te = new FearEntry()
                    {
                        FearCost = (Them.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value),
                        TurnTimer = 40
                    };
                    Them.GetRelations(us).FearEntries.Add(te);
                }
                foreach (Planet p in toRemove)
                {
                    Them.RemovePlanet(p);
                }
                foreach (Ship ship in TroopShips)
                {
                    ship.AI.OrderRebaseToNearest();
                }
            }
            us.GetRelations(Them).UpdateRelationship(us, Them);
        }

        public string AnalyzeOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
        {
            if (ToUs.Alliance)
            {
                if (!ToUs.IsBlank() || !FromUs.IsBlank())
                {
                    return "OFFER_ALLIANCE_TOO_COMPLICATED";
                }
                if (empire.GetRelations(them).Trust < 90f || empire.GetRelations(them).TotalAnger >= 20f || empire.GetRelations(them).TurnsKnown <= 100)
                {
                    return "AI_ALLIANCE_REJECT";
                }
                this.SetAlliance(true, them);
                return "AI_ALLIANCE_ACCEPT";
            }
            if (ToUs.PeaceTreaty)
            {
                GSAI.PeaceAnswer answer = this.AnalyzePeaceOffer(ToUs, FromUs, them, attitude);
                if (!answer.peace)
                {
                    return answer.answer;
                }
                this.AcceptOffer(ToUs, FromUs, this.empire, them);
                empire.GetRelations(them).Treaty_Peace = true;
                empire.GetRelations(them).PeaceTurnsRemaining = 100;
                them.GetRelations(this.empire).Treaty_Peace = true;
                them.GetRelations(this.empire).PeaceTurnsRemaining = 100;
                return answer.answer;
            }
            Empire us = this.empire;
            float TotalTrustRequiredFromUS = 0f;
            DTrait dt = us.data.DiplomaticPersonality;
            if (FromUs.TradeTreaty)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float)dt.Trade;
            }
            if (FromUs.OpenBorders)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ((float)dt.NAPact + 7.5f);
            }
            if (FromUs.NAPact)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + (float)dt.NAPact;
                int numWars = 0;
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in us.AllRelations)
                {
                    if (Relationship.Key.isFaction || !Relationship.Value.AtWar)
                    {
                        continue;
                    }
                    numWars++;
                }
                if (numWars > 0 && !us.GetRelations(them).AtWar)
                {
                    TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float)dt.NAPact;
                }
                else if (us.GetRelations(them).Threat >= 20f)
                {
                    TotalTrustRequiredFromUS = TotalTrustRequiredFromUS - (float)dt.NAPact;
                }
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                TotalTrustRequiredFromUS = TotalTrustRequiredFromUS + ResourceManager.TechTree[tech].Cost / 50f;
            }
            float ValueFromUs = 0f;
            float ValueToUs = 0f;
            if (FromUs.OpenBorders)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.OpenBorders)
            {
                ValueToUs = ValueToUs + 0.01f;
            }
            if (FromUs.NAPact)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.NAPact)
            {
                ValueToUs = ValueToUs + 5f;
            }
            if (FromUs.TradeTreaty)
            {
                ValueFromUs = ValueFromUs + 5f;
            }
            if (ToUs.TradeTreaty)
            {
                ValueToUs = ValueToUs + 5f;
                if ((double)this.empire.EstimateIncomeAtTaxRate(0.5f) < 1)
                {
                    ValueToUs = ValueToUs + 20f;
                }
            }
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f + ResourceManager.TechTree[tech].Cost / 50f : ResourceManager.TechTree[tech].Cost / 50f);
            }
            foreach (string artifactsOffered in FromUs.ArtifactsOffered)
            {
                ValueFromUs = ValueFromUs + 15f;
            }
            foreach (string str in ToUs.ArtifactsOffered)
            {
                ValueToUs = ValueToUs + 15f;
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 50f * 0.25f + ResourceManager.TechTree[tech].Cost / 50f : ResourceManager.TechTree[tech].Cost / 50f);
            }
            if (us.GetPlanets().Count - FromUs.ColoniesOffered.Count + ToUs.ColoniesOffered.Count < 1)
            {
                us.GetRelations(them).DamageRelationship(us, them, "Insulted", 25f, null);
                return "OfferResponse_Reject_Insulting";
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 25f + p.ProductionHere / 25f + p.Fertility + p.MineralRichness + p.MaxPopulation / 1000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 25f;
                        if (b.Name != "Capital City")
                        {
                            continue;
                        }
                        value = value + 100f;
                    }
                    float multiplier = 0f;
                    foreach (Planet other in p.system.PlanetList)
                    {
                        if (other.Owner != p.Owner)
                        {
                            continue;
                        }
                        multiplier = multiplier + 1.25f;
                    }
                    value = value * multiplier;
                    if (value < 15f)
                    {
                        value = 15f;
                    }
                    ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
                }
            }
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 2000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    int multiplier = 1;
                    foreach (Planet other in p.system.PlanetList)
                    {
                        if (other.Owner != p.Owner)
                        {
                            continue;
                        }
                        multiplier++;
                    }
                    value = value * (float)multiplier;
                    ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value * 0.5f + value : value);
                }
            }
            ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
            if (ValueFromUs == 0f && ValueToUs > 0f)
            {
                us.GetRelations(them).ImproveRelations(ValueToUs, ValueToUs);
                this.AcceptOffer(ToUs, FromUs, us, them);
                return "OfferResponse_Accept_Gift";
            }
            ValueToUs = ValueToUs - ValueToUs * us.GetRelations(them).TotalAnger / 100f;
            float offerdifferential = ValueToUs / (ValueFromUs + 0.01f);
            string OfferQuality = "";
            if (offerdifferential < 0.6f)
            {
                OfferQuality = "Insulting";
            }
            else if (offerdifferential < 0.9f && offerdifferential >= 0.6f)
            {
                OfferQuality = "Poor";
            }
            else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
            {
                OfferQuality = "Fair";
            }
            else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
            {
                OfferQuality = "Good";
            }
            else if (offerdifferential >= 1.45f)
            {
                OfferQuality = "Great";
            }
            if (ValueToUs == ValueFromUs)
            {
                OfferQuality = "Fair";
            }
            switch (attitude)
            {
                case Offer.Attitude.Pleading:
                {
                    if (TotalTrustRequiredFromUS > us.GetRelations(them).Trust)
                    {
                        if (OfferQuality != "Great")
                        {
                            return "OfferResponse_InsufficientTrust";
                        }
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }
                    if (offerdifferential < 0.6f)
                    {
                        OfferQuality = "Insulting";
                    }
                    else if (offerdifferential < 0.8f && offerdifferential > 0.65f)
                    {
                        OfferQuality = "Poor";
                    }
                    else if (offerdifferential >= 0.8f && offerdifferential < 1.1f)
                    {
                        OfferQuality = "Fair";
                    }
                    else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
                    {
                        OfferQuality = "Good";
                    }
                    else if (offerdifferential >= 1.45f)
                    {
                        OfferQuality = "Great";
                    }
                    if (OfferQuality == "Poor")
                    {
                        return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                    }
                    if (OfferQuality == "Insulting")
                    {
                        us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                        return "OfferResponse_Reject_Insulting";
                    }
                    if (OfferQuality == "Fair")
                    {
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Fair_Pleading";
                    }
                    if (OfferQuality == "Good")
                    {
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Good";
                    }
                    if (OfferQuality != "Great")
                    {
                        break;
                    }
                    us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                    this.AcceptOffer(ToUs, FromUs, us, them);
                    return "OfferResponse_Accept_Great";
                }
                case Offer.Attitude.Respectful:
                {
                    if (TotalTrustRequiredFromUS + us.GetRelations(them).TrustUsed <= us.GetRelations(them).Trust)
                    {
                        if (OfferQuality == "Poor")
                        {
                            return "OfferResponse_Reject_PoorOffer_EnoughTrust";
                        }
                        if (OfferQuality == "Insulting")
                        {
                            us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                            return "OfferResponse_Reject_Insulting";
                        }
                        if (OfferQuality == "Fair")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_Accept_Fair";
                        }
                        if (OfferQuality == "Good")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_Accept_Good";
                        }
                        if (OfferQuality != "Great")
                        {
                            break;
                        }
                        us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs - ValueFromUs);
                        this.AcceptOffer(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Great";
                    }
                    else
                    {
                        if (OfferQuality == "Great")
                        {
                            us.GetRelations(them).ImproveRelations(ValueToUs - ValueFromUs, ValueToUs);
                            this.AcceptOffer(ToUs, FromUs, us, them);
                            return "OfferResponse_AcceptGreatOffer_LowTrust";
                        }
                        if (OfferQuality == "Poor")
                        {
                            return "OfferResponse_Reject_PoorOffer_LowTrust";
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good")
                        {
                            return "OfferResponse_InsufficientTrust";
                        }
                        if (OfferQuality != "Insulting")
                        {
                            break;
                        }
                        us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                        return "OfferResponse_Reject_Insulting";
                    }
                }
                case Offer.Attitude.Threaten:
                {
                    if (dt.Name == "Ruthless")
                    {
                        return "OfferResponse_InsufficientFear";
                    }
                    us.GetRelations(them).DamageRelationship(us, them, "Insulted", ValueFromUs - ValueToUs, null);
                    if (OfferQuality == "Great")
                    {
                        this.AcceptThreat(ToUs, FromUs, us, them);
                        return "OfferResponse_AcceptGreatOffer_LowTrust";
                    }
                    if (offerdifferential < 0.95f)
                    {
                        OfferQuality = "Poor";
                    }
                    else if (offerdifferential >= 0.95f)
                    {
                        OfferQuality = "Fair";
                    }
                    if (us.GetRelations(them).Threat <= ValueFromUs || us.GetRelations(them).FearUsed + ValueFromUs >= us.GetRelations(them).Threat)
                    {
                        return "OfferResponse_InsufficientFear";
                    }
                    if (OfferQuality == "Poor")
                    {
                        this.AcceptThreat(ToUs, FromUs, us, them);
                        return "OfferResponse_Accept_Bad_Threatening";
                    }
                    if (OfferQuality != "Fair")
                    {
                        break;
                    }
                    this.AcceptThreat(ToUs, FromUs, us, them);
                    return "OfferResponse_Accept_Fair_Threatening";
                }
            }
            return "";
        }

        public GSAI.PeaceAnswer AnalyzePeaceOffer(Offer ToUs, Offer FromUs, Empire them, Offer.Attitude attitude)
        {
            WarState state;
            Empire us = this.empire;
            DTrait dt = us.data.DiplomaticPersonality;
            float ValueToUs = 0f;
            float ValueFromUs = 0f;
            foreach (string tech in FromUs.TechnologiesOffered)
            {
                ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f);
            }
            foreach (string artifactsOffered in FromUs.ArtifactsOffered)
            {
                ValueFromUs = ValueFromUs + 15f;
            }
            foreach (string str in ToUs.ArtifactsOffered)
            {
                ValueToUs = ValueToUs + 15f;
            }
            foreach (string tech in ToUs.TechnologiesOffered)
            {
                ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Technologists" ? ResourceManager.TechTree[tech].Cost / 100f * 0.25f + ResourceManager.TechTree[tech].Cost / 100f : ResourceManager.TechTree[tech].Cost / 100f);
            }
            foreach (string planetName in FromUs.ColoniesOffered)
            {
                foreach (Planet p in us.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                    }
                    ValueFromUs = ValueFromUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value + value : value + 0.5f * value);
                }
            }
            Array<Planet> PlanetsToUs = new Array<Planet>();
            foreach (string planetName in ToUs.ColoniesOffered)
            {
                foreach (Planet p in them.GetPlanets())
                {
                    if (p.Name != planetName)
                    {
                        continue;
                    }
                    PlanetsToUs.Add(p);
                    float value = p.Population / 1000f + p.FoodHere / 50f + p.ProductionHere / 50f + p.Fertility + p.MineralRichness + p.MaxPopulation / 10000f;
                    foreach (Building b in p.BuildingList)
                    {
                        value = value + b.Cost / 50f;
                        if (b.NameTranslationIndex != 409)
                        {
                            continue;
                        }
                        value = value + 1000000f;
                    }
                    ValueToUs = ValueToUs + (us.data.EconomicPersonality.Name == "Expansionists" ? value * 0.5f + value : value);
                }
            }
            string name = dt.Name;
            string str1 = name;
            if (name != null)
            {
                if (str1 == "Pacifist")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Honorable")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Cunning")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Xenophobic")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 15f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 8f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 8f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 15f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 5f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 10f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Aggressive")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 10f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 75f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 200f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                else if (str1 == "Ruthless")
                {
                    switch (us.GetRelations(them).ActiveWar.WarType)
                    {
                        case WarType.BorderConflict:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs))
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.ImperialistWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                        case WarType.DefensiveWar:
                        {
                            switch (us.GetRelations(them).ActiveWar.GetWarScoreState())
                            {
                                case WarState.LosingBadly:
                                {
                                    ValueToUs = ValueToUs + 5f;
                                    break;
                                }
                                case WarState.LosingSlightly:
                                {
                                    ValueToUs = ValueToUs + 1f;
                                    break;
                                }
                                case WarState.WinningSlightly:
                                {
                                    ValueFromUs = ValueFromUs + 120f;
                                    break;
                                }
                                case WarState.Dominating:
                                {
                                    ValueFromUs = ValueFromUs + 300f;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            ValueToUs = ValueToUs + them.data.Traits.DiplomacyMod * ValueToUs;
            float offerdifferential = ValueToUs / (ValueFromUs + 0.0001f);
            string OfferQuality = "";
            if (offerdifferential < 0.6f)
            {
                OfferQuality = "Insulting";
            }
            else if (offerdifferential < 0.9f && offerdifferential > 0.65f)
            {
                OfferQuality = "Poor";
            }
            else if (offerdifferential >= 0.9f && offerdifferential < 1.1f)
            {
                OfferQuality = "Fair";
            }
            else if ((double)offerdifferential >= 1.1 && (double)offerdifferential < 1.45)
            {
                OfferQuality = "Good";
            }
            else if (offerdifferential >= 1.45f)
            {
                OfferQuality = "Great";
            }
            if (ValueToUs == ValueFromUs && ValueToUs > 0f)
            {
                OfferQuality = "Fair";
            }
            GSAI.PeaceAnswer response = new GSAI.PeaceAnswer()
            {
                peace = false,
                answer = "REJECT_OFFER_PEACE_POOROFFER"
            };
            switch (us.GetRelations(them).ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                {
                    state = us.GetRelations(them).ActiveWar.GetBorderConflictState(PlanetsToUs);
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if ((OfferQuality == "Fair" || OfferQuality == "Good") && us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0)
                        {
                            response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                            return response;
                        }
                        else if (OfferQuality == "Fair" || OfferQuality == "Good")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        if (OfferQuality != "Great")
                        {
                            response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (state != WarState.EvenlyMatched)
                    {
                        if (state != WarState.LosingSlightly)
                        {
                            if (state != WarState.LosingBadly)
                            {
                                return response;
                            }
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else if (OfferQuality != "Poor")
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                            else
                            {
                                response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                                response.peace = true;
                                return response;
                            }
                        }
                        else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else if ((OfferQuality == "Fair" || OfferQuality == "Good") && us.GetRelations(them).ActiveWar.StartingNumContestedSystems > 0)
                    {
                        response.answer = "REJECT_OFFER_PEACE_UNWILLING_BC";
                        return response;
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                case WarType.ImperialistWar:
                {
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.EvenlyMatched)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        string name1 = this.empire.data.DiplomaticPersonality.Name;
                        str1 = name1;
                        if (name1 != null && str1 == "Pacifist")
                        {
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                        }
                        else if (OfferQuality != "Great")
                        {
                            response.answer = "REJECT_PEACE_RUTHLESS";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (state != WarState.LosingSlightly)
                    {
                        if (state != WarState.LosingBadly)
                        {
                            return response;
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if (OfferQuality != "Poor")
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                case WarType.GenocidalWar:
                {
                    return response;
                }
                case WarType.DefensiveWar:
                {
                    state = us.GetRelations(them).ActiveWar.GetWarScoreState();
                    if (state == WarState.WinningSlightly)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.Dominating)
                    {
                        if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.EvenlyMatched)
                    {
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                    }
                    else if (state == WarState.ColdWar)
                    {
                        string name2 = this.empire.data.DiplomaticPersonality.Name;
                        str1 = name2;
                        if (name2 != null && str1 == "Pacifist")
                        {
                            if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                            {
                                response.answer = "ACCEPT_OFFER_PEACE";
                                response.peace = true;
                                return response;
                            }
                            else
                            {
                                response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                                return response;
                            }
                        }
                        else if (OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_PEACE_COLDWAR";
                            response.peace = true;
                            return response;
                        }
                        else
                        {
                            response.answer = "REJECT_PEACE_RUTHLESS";
                            return response;
                        }
                    }
                    else if (state != WarState.LosingSlightly)
                    {
                        if (state != WarState.LosingBadly)
                        {
                            return response;
                        }
                        if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                        {
                            response.answer = "ACCEPT_OFFER_PEACE";
                            response.peace = true;
                            return response;
                        }
                        else if (OfferQuality != "Poor")
                        {
                            response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                            return response;
                        }
                        else
                        {
                            response.answer = "ACCEPT_OFFER_PEACE_RELUCTANT";
                            response.peace = true;
                            return response;
                        }
                    }
                    else if (OfferQuality == "Fair" || OfferQuality == "Good" || OfferQuality == "Great")
                    {
                        response.answer = "ACCEPT_OFFER_PEACE";
                        response.peace = true;
                        return response;
                    }
                    else
                    {
                        response.answer = "REJECT_OFFER_PEACE_POOROFFER";
                        return response;
                    }
                }
                default:
                {
                    return response;
                }
            }
        }

        private void AssessAngerAggressive(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship, Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (Relationship.Value.Treaty_OpenBorders || !Relationship.Value.Treaty_Trade && !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f && Relationship.Value.Trust < Relationship.Value.TotalAnger)
                {
                    Relationship.Value.Posture = Posture.Neutral;
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 50f)
            {
                if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Friends Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders, (bool x) => value.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.Trust >= 20f && Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >= 0.75f * (float)this.empire.data.DiplomaticPersonality.Territorialism)
            {
                if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders, (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10 && Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                {
                    this.ThreatMatrix.ClearBorders();
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Relationship.Key, "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.WarnedAboutShips = true;
                    return;
                }
            }
        }

        private void AssessAngerPacifist(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship, Posture posture, float usedTrust)
        {
            if (posture != Posture.Friendly)
            {
                this.AssessDiplomaticAnger(Relationship);
            }
            else if (!Relationship.Value.Treaty_OpenBorders && (Relationship.Value.Treaty_Trade || Relationship.Value.Treaty_NAPact) && !Relationship.Value.HaveRejected_OpenBorders)
            {
                if (Relationship.Value.Trust >= 50f)
                {
                    if (Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
                    {
                        Offer NAPactOffer = new Offer()
                        {
                            OpenBorders = true,
                            AcceptDL = "Open Borders Accepted",
                            RejectDL = "Open Borders Friends Rejected"
                        };
                        Ship_Game.Gameplay.Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_OpenBorders, (bool x) => value.HaveRejected_OpenBorders = x);
                        Offer OurOffer = new Offer()
                        {
                            OpenBorders = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                            return;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Empire.Universe.PlayerEmpire, "Offer Open Borders Friends", OurOffer, NAPactOffer));
                        return;
                    }
                }
                else if (Relationship.Value.Trust >= 20f && Relationship.Value.Anger_TerritorialConflict + Relationship.Value.Anger_FromShipsInOurBorders >= 0.75f * (float)this.empire.data.DiplomaticPersonality.Territorialism && Relationship.Value.Trust - usedTrust > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        OpenBorders = true,
                        AcceptDL = "Open Borders Accepted",
                        RejectDL = "Open Borders Rejected"
                    };
                    Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_OpenBorders, (bool x) => relationship.HaveRejected_OpenBorders = x);
                    Offer OurOffer = new Offer()
                    {
                        OpenBorders = true
                    };
                    if (Relationship.Key != Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Pleading);
                        return;
                    }
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Empire.Universe.PlayerEmpire, "Offer Open Borders", OurOffer, NAPactOffer));
                    return;
                }
            }
            else if (Relationship.Value.turnsSinceLastContact >= 10)
            {
                if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
                {
                    Ship_Game.Gameplay.Relationship r = Relationship.Value;
                    if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && r.turnsSinceLastContact > 10)
                    {
                        this.ThreatMatrix.ClearBorders();
                        if (!r.WarnedAboutColonizing)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Relationship.Key, "Warning Ships"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Relationship.Key, "Warning Colonized then Ships", r.GetContestedSystem()));
                        }
                        r.WarnedAboutShips = true;
                        return;
                    }
                }
            }
            else if (Relationship.Value.HaveRejected_OpenBorders || Relationship.Value.TotalAnger > 50f && Relationship.Value.Trust < Relationship.Value.TotalAnger)
            {
                Relationship.Value.Posture = Posture.Neutral;
                return;
            }
        }

        private void AssessDiplomaticAnger(KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship)
        {
            if (Relationship.Value.Known && Relationship.Key == Empire.Universe.PlayerEmpire)
             {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 4) && !r.AtWar && !r.WarnedAboutShips && !r.Treaty_Peace && !r.Treaty_OpenBorders)
                {
                    if (!r.WarnedAboutColonizing)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them, "Warning Ships"));
                    }
                    else
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, them, "Warning Colonized then Ships", r.GetContestedSystem()));
                    }
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                    return;
                }
                if (r.Threat < 25f && r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >= (float)this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar && !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                    return;
                }
                if (r.PreparingForWar && r.PreparingForWarType == WarType.BorderConflict)
                {
                    r.PreparingForWar = false;
                    return;
                }
            }
            else if (Relationship.Value.Known)
            {
                Ship_Game.Gameplay.Relationship r = Relationship.Value;
                Empire them = Relationship.Key;
                if (r.Anger_MilitaryConflict >= 5 && !r.AtWar && !r.Treaty_Peace)
                {
                    this.DeclareWarOn(them, WarType.DefensiveWar);
                }
                if (r.Anger_TerritorialConflict + r.Anger_FromShipsInOurBorders >= (float)this.empire.data.DiplomaticPersonality.Territorialism && !r.AtWar && !r.Treaty_OpenBorders && !r.Treaty_Peace)
                {
                    r.PreparingForWar = true;
                    r.PreparingForWarType = WarType.BorderConflict;
                }
                if (r.Anger_FromShipsInOurBorders > (float)(this.empire.data.DiplomaticPersonality.Territorialism / 2) && !r.AtWar && !r.WarnedAboutShips)
                {
                    r.turnsSinceLastContact = 0;
                    r.WarnedAboutShips = true;
                }
            }
        }

        public SolarSystem AssignExplorationTargetORIG(Ship queryingShip)
        {
            Array<SolarSystem> Potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.empire])
                {
                    continue;
                }
                Potentials.Add(s);
            }
            foreach (SolarSystem s in this.MarkedForExploration)
            {
                Potentials.Remove(s);
            }
            IOrderedEnumerable<SolarSystem> sortedList = 
                from system in Potentials
                orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
                select system;
            if (sortedList.Count<SolarSystem>() <= 0)
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            this.MarkedForExploration.Add(sortedList.First<SolarSystem>());
            return sortedList.First<SolarSystem>();
        }

        public Array<Planet> GetKnownPlanets()
        {
            Array<Planet> KnownPlanets = new Array<Planet>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (!s.ExploredDict[empire]) continue;
                KnownPlanets.AddRange(s.PlanetList);
            }
            return KnownPlanets;
        }
        //added by gremlin ExplorationTarget
        public SolarSystem AssignExplorationTarget(Ship queryingShip)
        {
            Array<SolarSystem> Potentials = new Array<SolarSystem>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.ExploredDict[this.empire])
                {
                    continue;
                }
                Potentials.Add(s);
            }
            
            using (MarkedForExploration.AcquireReadLock())
            foreach (SolarSystem s in this.MarkedForExploration)
            {
                Potentials.Remove(s);
            }
            IOrderedEnumerable<SolarSystem> sortedList =
                from system in Potentials
                orderby Vector2.Distance(this.empire.GetWeightedCenter(), system.Position)
                select system;
            if (sortedList.Count<SolarSystem>() <= 0)
            {
                queryingShip.AI.OrderQueue.Clear();
                return null;
            }
            //SolarSystem nearesttoScout = sortedList.OrderBy(furthest => Vector2.Distance(queryingShip.Center, furthest.Position)).FirstOrDefault();
            SolarSystem nearesttoHome = sortedList.OrderBy(furthest => Vector2.Distance(this.empire.GetWeightedCenter(), furthest.Position)).FirstOrDefault(); ;
            //SolarSystem potentialTarget = null;
            foreach (SolarSystem nearest in sortedList)
            {
                if (nearest.CombatInSystem) continue;
                float distanceToScout = Vector2.Distance(queryingShip.Center, nearest.Position);
                float distanceToEarth = Vector2.Distance(this.empire.GetWeightedCenter(), nearest.Position);

                if (distanceToScout > distanceToEarth + 50000f)
                {
                    continue;
                }
                nearesttoHome = nearest;
                break;

            }
            this.MarkedForExploration.Add(nearesttoHome);
            return nearesttoHome;
        }
        public void RemoveShipFromForce(Ship ship, AO ao = null)
        {
            if (ship == null) return;
            empire.ForcePoolRemove(ship);
            ao?.RemoveShip(ship);
            DefensiveCoordinator.Remove(ship);

        }
        public void AssignShipToForce(Ship toAdd)
        {            
            if (toAdd.fleet != null ||empire.GetShipsFromOffensePools().Contains(toAdd) )//|| empire.GetForcePool().Contains(toAdd))
            {
                Log.Error("ship in {0}", toAdd.fleet?.Name ?? "force Pool");
            }
            int numWars = empire.AtWarCount;
            
            float baseDefensePct = 0.1f;
            baseDefensePct = baseDefensePct + 0.15f * (float)numWars;
            if(toAdd .hasAssaultTransporter || toAdd.BombCount >0 || toAdd.HasTroopBay || toAdd.BaseStrength ==0 || toAdd.WarpThrust <= 0 || toAdd.GetStrength() < toAdd.BaseStrength || !toAdd.BaseCanWarp && !empire.GetForcePool().Contains(toAdd))
            {
                empire.GetForcePool().Add(toAdd);
                return;
            }

            if (baseDefensePct > 0.35f)
            {
                baseDefensePct = 0.35f;
            }
            
            bool needDef = empire.currentMilitaryStrength * baseDefensePct - DefStr >0 && DefensiveCoordinator.DefenseDeficit >0;

            if (needDef)   //
            {
                DefensiveCoordinator.AddShip(toAdd);
                return;
            }
            AO area = AreasOfOperations.FindMinFiltered(ao => !ao.AOFull, ao => toAdd.Position.SqDist(ao.Position));
            if (!area?.AddShip(toAdd) ?? false )
                empire.GetForcePool().Add(toAdd);

        }

        public void CallAllyToWar(Empire Ally, Empire Enemy)
        {
            Offer offer = new Offer()
            {
                AcceptDL = "HelpUS_War_Yes",
                RejectDL = "HelpUS_War_No"
            };
            string dialogue = "HelpUS_War";
            Offer OurOffer = new Offer()
            {
                ValueToModify = new Ref<bool>(() => Ally.GetRelations(Enemy).AtWar, (bool x) => {
                    if (x)
                    {
                        Ally.GetGSAI().DeclareWarOnViaCall(Enemy, WarType.ImperialistWar);
                        return;
                    }
                    float Amount = 30f;
                    if (this.empire.data.DiplomaticPersonality != null && this.empire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        Amount = 60f;
                        offer.RejectDL = "HelpUS_War_No_BreakAlliance";
                        this.empire.GetRelations(Ally).Treaty_Alliance = false;
                        Ally.GetRelations(this.empire).Treaty_Alliance = false;
                        this.empire.GetRelations(Ally).Treaty_OpenBorders = false;
                        this.empire.GetRelations(Ally).Treaty_NAPact = false;
                    }
                    Relationship item = this.empire.GetRelations(Ally);
                    item.Trust = item.Trust - Amount;
                    Relationship angerDiplomaticConflict = this.empire.GetRelations(Ally);
                    angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + Amount;
                })
            };
            if (Ally == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, this.empire, Empire.Universe.PlayerEmpire, dialogue, OurOffer, offer, Enemy));
            }
        }

        public void CheckClaim(KeyValuePair<Empire, Relationship> Them, Planet claimedPlanet)
        {
            if (this.empire == Empire.Universe.PlayerEmpire)
            {
                return;
            }
            if (this.empire.isFaction)
            {
                return;
            }
            if (!Them.Value.Known)
            {
                return;
            }
            if (Them.Value.WarnedSystemsList.Contains(claimedPlanet.system.guid) && claimedPlanet.Owner == Them.Key && !Them.Value.AtWar)
            {
                bool TheyAreThereAlready = false;
                foreach (Planet p in claimedPlanet.system.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)
                    {
                        continue;
                    }
                    TheyAreThereAlready = true;
                }
                if (TheyAreThereAlready && Them.Key == Empire.Universe.PlayerEmpire)
                {
                    Relationship item = empire.GetRelations(Them.Key);
                    item.Anger_TerritorialConflict = item.Anger_TerritorialConflict + (5f + (float)Math.Pow(5, (double)empire.GetRelations(Them.Key).NumberStolenClaims));
                    empire.GetRelations(Them.Key).UpdateRelationship(this.empire, Them.Key);
                    Relationship numberStolenClaims = empire.GetRelations(Them.Key);
                    numberStolenClaims.NumberStolenClaims = numberStolenClaims.NumberStolenClaims + 1;
                    if (empire.GetRelations(Them.Key).NumberStolenClaims == 1 && !empire.GetRelations(Them.Key).StolenSystems.Contains(claimedPlanet.guid))
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Stole Claim", claimedPlanet.system));
                    }
                    else if (empire.GetRelations(Them.Key).NumberStolenClaims == 2 && !empire.GetRelations(Them.Key).HaveWarnedTwice && !empire.GetRelations(Them.Key).StolenSystems.Contains(claimedPlanet.system.guid))
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Stole Claim 2", claimedPlanet.system));
                        empire.GetRelations(Them.Key).HaveWarnedTwice = true;
                    }
                    else if (empire.GetRelations(Them.Key).NumberStolenClaims >= 3 && !empire.GetRelations(Them.Key).HaveWarnedThrice && !empire.GetRelations(Them.Key).StolenSystems.Contains(claimedPlanet.system.guid))
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Stole Claim 3", claimedPlanet.system));
                        empire.GetRelations(Them.Key).HaveWarnedThrice = true;
                    }
                    empire.GetRelations(Them.Key).StolenSystems.Add(claimedPlanet.system.guid);
                }
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (empire.GetRelations(them).Trust > 0f)
            {
                empire.GetRelations(them).Trust = 0f;
            }
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

        public void DeclareWarOn(Empire them, WarType wt)
        {
            empire.GetRelations(them).PreparingForWar = false;
            if (this.empire.isFaction || this.empire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;
            empire.GetRelations(them).FedQuest = (FederationQuest)null;
            if (this.empire == Empire.Universe.PlayerEmpire && empire.GetRelations(them).Treaty_NAPact)
            {
                empire.GetRelations(them).Treaty_NAPact = false;
                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                {
                    if (keyValuePair.Key != them)
                    {
                        keyValuePair.Key.GetRelations(this.empire).Trust -= 50f;
                        keyValuePair.Key.GetRelations(this.empire).Anger_DiplomaticConflict += 20f;
                        keyValuePair.Key.GetRelations(this.empire).UpdateRelationship(keyValuePair.Key, this.empire);
                    }
                }
                them.GetRelations(this.empire).Trust -= 50f;
                them.GetRelations(this.empire).Anger_DiplomaticConflict += 50f;
                them.GetRelations(this.empire).UpdateRelationship(them, this.empire);
            }
            if (them == Empire.Universe.PlayerEmpire && !empire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                        if (empire.GetRelations(them).contestedSystemGuid != Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War BC TarSys", empire.GetRelations(them).GetContestedSystem()));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War BC"));
                            break;
                        }
                    case WarType.ImperialistWar:
                        if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Imperialism Break NA"));
                            using (var enumerator = this.empire.AllRelations.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    KeyValuePair<Empire, Relationship> current = enumerator.Current;
                                    if (current.Key != them)
                                    {
                                        current.Value.Trust -= 50f;
                                        current.Value.Anger_DiplomaticConflict += 20f;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Imperialism"));
                            break;
                        }
                    case WarType.DefensiveWar:
                        if (!empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Defense"));
                            empire.GetRelations(them).Anger_DiplomaticConflict += 25f;
                            empire.GetRelations(them).Trust -= 25f;
                            break;
                        }
                        else if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Defense BrokenNA"));
                            empire.GetRelations(them).Treaty_NAPact = false;
                            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                            {
                                if (keyValuePair.Key != them)
                                {
                                    keyValuePair.Value.Trust -= 50f;
                                    keyValuePair.Value.Anger_DiplomaticConflict += 20f;
                                }
                            }
                            empire.GetRelations(them).Trust -= 50f;
                            empire.GetRelations(them).Anger_DiplomaticConflict += 50f;
                            break;
                        }
                        else
                            break;
                }
            }
            if (them == Empire.Universe.PlayerEmpire || this.empire == Empire.Universe.PlayerEmpire)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known && Empire.Universe.PlayerEmpire.GetRelations(this.empire).Known)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate);
            empire.GetRelations(them).ActiveWar.WarType = wt;
            if (empire.GetRelations(them).Trust > 0f)
                empire.GetRelations(them).Trust = 0.0f;
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {

            empire.GetRelations(them).PreparingForWar = false;
            if (this.empire.isFaction || this.empire.data.Defeated || them.data.Defeated || them.isFaction)
            {
                return;
            }
            empire.GetRelations(them).FedQuest = null;
            if (this.empire == Empire.Universe.PlayerEmpire && empire.GetRelations(them).Treaty_NAPact)
            {
                empire.GetRelations(them).Treaty_NAPact = false;
                Relationship item = them.GetRelations(this.empire);
                item.Trust = item.Trust - 50f;
                Relationship angerDiplomaticConflict = them.GetRelations(this.empire);
                angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(this.empire).UpdateRelationship(them, this.empire);
            }
            if (them == Empire.Universe.PlayerEmpire && !empire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                    {
                        if (empire.GetRelations(them).contestedSystemGuid == Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War BC"));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War BC Tarsys", empire.GetRelations(them).GetContestedSystem()));
                            break;
                        }
                    }
                    case WarType.ImperialistWar:
                    {
                        if (!empire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Imperialism"));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Imperialism Break NA"));
                            break;
                        }
                    }
                    case WarType.DefensiveWar:
                    {
                        if (empire.GetRelations(them).Treaty_NAPact)
                        {
                            if (!empire.GetRelations(them).Treaty_NAPact)
                            {
                                break;
                            }
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Defense BrokenNA"));
                            empire.GetRelations(them).Treaty_NAPact = false;
                            Relationship trust = empire.GetRelations(them);
                            trust.Trust = trust.Trust - 50f;
                            Relationship relationship = empire.GetRelations(them);
                            relationship.Anger_DiplomaticConflict = relationship.Anger_DiplomaticConflict + 50f;
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, them, "Declare War Defense"));
                            Relationship item1 = empire.GetRelations(them);
                            item1.Anger_DiplomaticConflict = item1.Anger_DiplomaticConflict + 25f;
                            Relationship trust1 = empire.GetRelations(them);
                            trust1.Trust = trust1.Trust - 25f;
                            break;
                        }
                    }
                }
            }
            if (them == Empire.Universe.PlayerEmpire || this.empire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known && Empire.Universe.PlayerEmpire.GetRelations(this.empire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(this.empire, them);
            }
            empire.GetRelations(them).AtWar = true;
            empire.GetRelations(them).Posture = Posture.Hostile;
            empire.GetRelations(them).ActiveWar = new War(this.empire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (empire.GetRelations(them).Trust > 0f)
            {
                empire.GetRelations(them).Trust = 0f;
            }
            empire.GetRelations(them).Treaty_OpenBorders = false;
            empire.GetRelations(them).Treaty_NAPact = false;
            empire.GetRelations(them).Treaty_Trade = false;
            empire.GetRelations(them).Treaty_Alliance = false;
            empire.GetRelations(them).Treaty_Peace = false;
            them.GetGSAI().GetWarDeclaredOnUs(this.empire, wt);
        }
        private void AssessTeritorialConflicts(float weight)
        {

            weight *= .1f;
            foreach (SystemCommander CheckBorders in this.DefensiveCoordinator.DefenseDict.Values)
            {
                
                if (CheckBorders.RankImportance > 5)
                {
                    foreach (SolarSystem closeenemies in CheckBorders.System.FiveClosestSystems)
                    {
                        foreach (Empire enemy in closeenemies.OwnerList)
                        {
#if PERF
                            if (enemy.isPlayer)
                                continue;
#endif
                            if (enemy.isFaction)
                                continue;
                            Relationship check = null;

                            if (this.empire.TryGetRelations(enemy, out check))
                            {
                                if (!check.Known || check.Treaty_Alliance)
                                    continue;
                                
                                    weight *= (this.empire.currentMilitaryStrength + closeenemies.GetActualStrengthPresent(enemy)) / (this.empire.currentMilitaryStrength +1);
                                
                                if (check.Treaty_OpenBorders)
                                {
                                    weight *= .5f;
                                }
                                if (check.Treaty_NAPact)
                                    weight *= .5f;
                               if (enemy.isPlayer)
                                weight *= ((int)Empire.Universe.GameDifficulty+1);

                                if (check.Anger_TerritorialConflict > 0)
                                    check.Anger_TerritorialConflict += (check.Anger_TerritorialConflict + CheckBorders.RankImportance * weight) / (check.Anger_TerritorialConflict);
                                else
                                    check.Anger_TerritorialConflict += CheckBorders.RankImportance * weight;

                            }
                        }
                        
                    }

                }
            }
        }
        private void DoAggressiveRelations()
        {
            
            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Key.data.Defeated || !Relationship.Value.AtWar && !Relationship.Value.PreparingForWar)
                {
                    continue;
                }
                //numberofWars++;
                numberofWars += (int)Relationship.Key.currentMilitaryStrength;
            }
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism /10f);
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Value.AtWar || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }

                if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar && (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" || Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer()
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                Relationship.Value.Posture = Posture.Neutral;
                if (Relationship.Value.Threat <= 0f)
                {
                    if (!Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
                    {
                        Relationship.Value.HaveInsulted_Military = true;
                        if (Relationship.Key == Empire.Universe.PlayerEmpire)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Insult Military"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Hostile;
                }
                else if (Relationship.Value.Threat > 25f && Relationship.Value.TurnsKnown > this.FirstDemand)
                {
                    if (!Relationship.Value.HaveComplimented_Military && Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Key == Empire.Universe.PlayerEmpire)
                    {
                        Relationship.Value.HaveComplimented_Military = true;
                        if (!Relationship.Value.HaveInsulted_Military || Relationship.Value.TurnsKnown <= this.SecondDemand)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Compliment Military"));
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Compliment Military Better"));
                        }
                    }
                    Relationship.Value.Posture = Posture.Friendly;
                }
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown > this.SecondDemand && Relationship.Value.Trust - usedTrust > (float)this.empire.data.DiplomaticPersonality.Trade && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade)
                        {
                            Offer NAPactOffer = new Offer()
                            {
                                TradeTreaty = true,
                                AcceptDL = "Trade Accepted",
                                RejectDL = "Trade Rejected"
                            };
                            Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                            NAPactOffer.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_TRADE, (bool x) => relationship.HaveRejected_TRADE = x);
                            Offer OurOffer = new Offer()
                            {
                                TradeTreaty = true
                            };
                            if (Relationship.Key != Empire.Universe.PlayerEmpire)
                            {
                                Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                            }
                            else
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", OurOffer, NAPactOffer));
                            }
                        }
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        if (Relationship.Value.TurnsAbove95 <= 100 || Relationship.Value.turnsSinceLastContact <= 10 || Relationship.Value.Treaty_Alliance || !Relationship.Value.Treaty_Trade || !Relationship.Value.Treaty_NAPact || Relationship.Value.HaveRejected_Alliance || Relationship.Value.TotalAnger >= 20f)
                        {
                            continue;
                        }
                        Offer OfferAlliance = new Offer()
                        {
                            Alliance = true,
                            AcceptDL = "ALLIANCE_ACCEPTED",
                            RejectDL = "ALLIANCE_REJECTED"
                        };
                        Ship_Game.Gameplay.Relationship value1 = Relationship.Value;
                        OfferAlliance.ValueToModify = new Ref<bool>(() => value1.HaveRejected_Alliance, (bool x) => {
                            value1.HaveRejected_Alliance = x;
                            this.SetAlliance(!value1.HaveRejected_Alliance);
                        });
                        Offer OurOffer0 = new Offer();
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer0, OfferAlliance, this.empire, Offer.Attitude.Respectful);
                            continue;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", OurOffer0, OfferAlliance));
                            continue;
                        }
                    }
                    case Posture.Neutral:
                    {
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    case Posture.Hostile:
                    {
                        if (Relationship.Value.Threat < -15f && Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Alliance)
                        {
                            if (Relationship.Value.TotalAnger < 75f)
                            {
                                int i = 0;
                                while (i < 5)
                                {
                                    if (i >= this.DesiredPlanets.Count)
                                    {
                                        break;
                                    }
                                    if (this.DesiredPlanets[i].Owner != Relationship.Key)
                                    {
                                        i++;
                                    }
                                    else
                                    {
                                        PotentialTargets.Add(Relationship.Key);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                PotentialTargets.Add(Relationship.Key);
                            }
                        }
                        else if (Relationship.Value.Threat <= -45f && Relationship.Value.TotalAnger > 20f)
                        {
                            PotentialTargets.Add(Relationship.Key);
                        }
                    //Label0:
                        this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                        continue;
                    }
                    default:
                    {
                        continue;   //this doesn't actually do anything, since it's at the end of the loop anyways
                    }
                }
            }
            if (PotentialTargets.Count > 0 && numberofWars *2 < this.empire.currentMilitaryStrength )//<= 1)
            {
                Empire ToAttack = PotentialTargets.First<Empire>();
                this.empire.GetRelations(ToAttack).PreparingForWar = true;
            }
        }


        //added by gremlin aggruthmanager
        private void DoAggRuthAgentManager()
        {
            string Names;

            float income = this.spyBudget;


            this.DesiredAgentsPerHostile = (int)(income * .08f) + 1;
            this.DesiredAgentsPerNeutral = (int)(income * .03f) + 1;

            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 2;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;

            int empirePlanetSpys = this.empire.GetPlanets().Count() / 3 + 3;// (int)(this.spyBudget / (this.empire.GrossTaxes * 3));
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.spyBudget >= 250f && currentSpies < empirePlanetSpys)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            float offSpyModifier = (int)Empire.Universe.GameDifficulty * .1f;
            int DesiredOffense = (int)(this.empire.data.AgentList.Count * offSpyModifier);
            //int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .33f); // (int)(0.33f * (float)this.empire.data.AgentList.Count);
            //int DesiredOffense = this.empire.data.AgentList.Count / 2;
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense )
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                HashSet<AgentMission> PotentialMissions = new HashSet<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (this.empire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);

                        PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (this.empire.GetRelations(Target).Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                }


                if (this.empire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach(AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach(AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }                
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions.Skip(RandomMath.InRange(PotentialMissions.Count)).FirstOrDefault();
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;

            }
        }
    
        //added by gremlin CunningAgent
        private void DoCunningAgentManager()
        {
            
            int income = (int)this.spyBudget;
            string Names;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentsPerHostile = (int)(income * .010f);// +1;
            this.DesiredAgentsPerNeutral = (int)(income * .05f);// +1;

            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            //int empirePlanetSpys = this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            //if (this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            int empireSpyLimit = this.empire.GetPlanets().Count() / 3 + 3;// (int)(this.spyBudget / this.empire.GrossTaxes);
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.spyBudget >= 250f && currentSpies < empireSpyLimit)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }

                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
           // int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .2);// (int)(0.20f * (float)this.empire.data.AgentList.Count);
            float offSpyModifier = (int)Empire.Universe.GameDifficulty *.17f ;

            int DesiredOffense = (int)(this.empire.data.AgentList.Count * offSpyModifier);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense )
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                Array<AgentMission> PotentialMissions = new Array<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (this.empire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {

                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);

                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count)
                            PotentialMissions.Add(AgentMission.Robbery);
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                    }


                }
                if (this.empire.GetRelations(Target).Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);

                    }
                    if (agent.Level >= 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (this.empire.GetRelations(Target).Posture == Posture.Neutral || this.empire.GetRelations(Target).Posture == Posture.Friendly)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost >this.spyBudget)

                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);

                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                        PotentialMissions.Add(AgentMission.StealTech);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }

                }
                if (this.empire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach (AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach (AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }    
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[RandomMath.InRange(PotentialMissions.Count)];
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void DoCunningRelations()
        {
            this.DoHonorableRelations();
        }

        private void DoHonorableRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            float usedTrust1 = 0.0f;
                            foreach (TrustEntry trustEntry in (Array<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust1 += trustEntry.TrustCost;
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && (double)Relationship.Value.Trust - (double)usedTrust1 > (double)this.empire.data.DiplomaticPersonality.Trade && (Relationship.Value.turnsSinceLastContact > this.SecondDemand && !Relationship.Value.Treaty_Trade) && !Relationship.Value.HaveRejected_TRADE)
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_TRADE, x => r.HaveRejected_TRADE = x);
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust1);
                            if (Relationship.Value.TurnsAbove95 > 100 && Relationship.Value.turnsSinceLastContact > 10 && (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) && (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance && (double)Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_Alliance), (Action<bool>)(x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    this.SetAlliance(!r.HaveRejected_Alliance);
                                }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2, offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_NAPACT), (Action<bool>)(x => r.HaveRejected_NAPACT = x));
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            float usedTrust2 = 0.0f;
                            foreach (TrustEntry trustEntry in (Array<TrustEntry>)Relationship.Value.TrustEntries)
                                usedTrust2 += trustEntry.TrustCost;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust2);
                            continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                Array<Empire> list = new Array<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance && keyValuePair.Key.GetRelations(Relationship.Key).Known && !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) && this.empire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if ((double)Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0.0)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((double)(Relationship.Value.Anger_FromShipsInOurBorders + Relationship.Value.Anger_TerritorialConflict) > (double)this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            else
                            {
                                this.AssessAngerPacifist(Relationship, Posture.Hostile, 100f);
                                continue;
                            }
                        default:
                            continue;
                    }
                }
            }
        }

    
        //added by gremlin deveks HonPacManager
        private void DoHonPacAgentManager()
        {
            string Names;

            int income = (int)this.spyBudget;


            this.DesiredAgentsPerHostile = (int)(income * .05f) + 1;
            this.DesiredAgentsPerNeutral = (int)(income * .02f) + 1;


            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 1;
            this.DesiredAgentCount = 0;
            this.BaseAgents = empire.GetPlanets().Count / 2 + (int)(this.spyBudget / (this.empire.GrossTaxes * 2));
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount + this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            //int empirePlanetSpys = empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            int empirePlanetSpys = empire.GetPlanets().Count() / 3 + 3;
            //if (empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;

            if (this.spyBudget >= 250f && this.empire.data.AgentList.Count < empirePlanetSpys)
            {
                Names = (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")) ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt") : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] { ',' });
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 200f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            float offSpyModifier = (int)Empire.Universe.GameDifficulty * .08f;
            int DesiredOffense = (int)(this.empire.data.AgentList.Count * offSpyModifier);// /(int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .4f);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover || Offense >= DesiredOffense )
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated || 
                        Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                Array<AgentMission> PotentialMissions = new Array<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (this.empire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (this.empire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach (AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach (AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }    
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[RandomMath.InRange(PotentialMissions.Count)];
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }
        private void DoPacifistRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 50f);
            foreach (KeyValuePair<Empire, Relationship> Relationship in this.empire.AllRelations)
            {
                if (Relationship.Value.Known && !Relationship.Key.isFaction && !Relationship.Key.data.Defeated)
                {
                    float usedTrust = 0.0f;
                    foreach (TrustEntry trustEntry in (Array<TrustEntry>)Relationship.Value.TrustEntries)
                        usedTrust += trustEntry.TrustCost;
                    switch (Relationship.Value.Posture)
                    {
                        case Posture.Friendly:
                            if (Relationship.Value.TurnsKnown > this.SecondDemand && !Relationship.Value.Treaty_Trade && (!Relationship.Value.HaveRejected_TRADE && (double)Relationship.Value.Trust - (double)usedTrust > (double)this.empire.data.DiplomaticPersonality.Trade) && (!Relationship.Value.Treaty_Trade && Relationship.Value.turnsSinceLastContact > this.SecondDemand && !Relationship.Value.HaveRejected_TRADE))
                            {
                                Offer offer1 = new Offer();
                                offer1.TradeTreaty = true;
                                offer1.AcceptDL = "Trade Accepted";
                                offer1.RejectDL = "Trade Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_TRADE), (Action<bool>)(x => r.HaveRejected_TRADE = x));
                                Offer offer2 = new Offer();
                                offer2.TradeTreaty = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            this.AssessAngerPacifist(Relationship, Posture.Friendly, usedTrust);
                            if (Relationship.Value.TurnsAbove95 > 100 && Relationship.Value.turnsSinceLastContact > 10 && (!Relationship.Value.Treaty_Alliance && Relationship.Value.Treaty_Trade) && (Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Alliance && (double)Relationship.Value.TotalAnger < 20.0))
                            {
                                Offer offer1 = new Offer();
                                offer1.Alliance = true;
                                offer1.AcceptDL = "ALLIANCE_ACCEPTED";
                                offer1.RejectDL = "ALLIANCE_REJECTED";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>((Func<bool>)(() => r.HaveRejected_Alliance), (Action<bool>)(x =>
                                {
                                    r.HaveRejected_Alliance = x;
                                    this.SetAlliance(!r.HaveRejected_Alliance);
                                }));
                                Offer offer2 = new Offer();
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                {
                                    Empire.Universe.ScreenManager.AddScreen((GameScreen)new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "OFFER_ALLIANCE", offer2, offer1));
                                    continue;
                                }
                                else
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                                    continue;
                                }
                            }
                            else
                                continue;
                        case Posture.Neutral:
                            if (Relationship.Value.TurnsKnown == this.FirstDemand && !Relationship.Value.Treaty_NAPact)
                            {
                                Offer offer1 = new Offer();
                                offer1.NAPact = true;
                                offer1.AcceptDL = "NAPact Accepted";
                                offer1.RejectDL = "NAPact Rejected";
                                Relationship r = Relationship.Value;
                                offer1.ValueToModify = new Ref<bool>(() => r.HaveRejected_NAPACT, x => r.HaveRejected_NAPACT = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                Offer offer2 = new Offer();
                                offer2.NAPact = true;
                                if (Relationship.Key == Empire.Universe.PlayerEmpire)
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer NAPact", offer2, offer1));
                                else
                                    Relationship.Key.GetGSAI().AnalyzeOffer(offer2, offer1, this.empire, Offer.Attitude.Respectful);
                            }
                            if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.Treaty_NAPact)
                                Relationship.Value.Posture = Posture.Friendly;
                            else if (Relationship.Value.TurnsKnown > this.FirstDemand && Relationship.Value.HaveRejected_NAPACT)
                                Relationship.Value.Posture = Posture.Neutral;
                            this.AssessAngerPacifist(Relationship, Posture.Neutral, usedTrust);
                            if (Relationship.Value.Trust > 50f && Relationship.Value.TotalAnger < 10)
                            {
                                Relationship.Value.Posture = Posture.Friendly;
                                continue;
                            }
                            else
                                continue;
                        case Posture.Hostile:
                            if (Relationship.Value.ActiveWar != null)
                            {
                                Array<Empire> list = new Array<Empire>();
                                foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.empire.AllRelations)
                                {
                                    if (keyValuePair.Value.Treaty_Alliance && keyValuePair.Key.GetRelations(Relationship.Key).Known && !keyValuePair.Key.GetRelations(Relationship.Key).AtWar)
                                        list.Add(keyValuePair.Key);
                                }
                                foreach (Empire Ally in list)
                                {
                                    if (!Relationship.Value.ActiveWar.AlliesCalled.Contains(Ally.data.Traits.Name) && this.empire.GetRelations(Ally).turnsSinceLastContact > 10)
                                    {
                                        this.CallAllyToWar(Ally, Relationship.Key);
                                        Relationship.Value.ActiveWar.AlliesCalled.Add(Ally.data.Traits.Name);
                                    }
                                }
                                if (Relationship.Value.ActiveWar.TurnsAtWar % 100.0 == 0f)
                                {
                                    switch (Relationship.Value.ActiveWar.WarType)
                                    {
                                        case WarType.BorderConflict:
                                            if ((Relationship.Value.Anger_FromShipsInOurBorders + Relationship.Value.Anger_TerritorialConflict) > (float)this.empire.data.DiplomaticPersonality.Territorialism)
                                                return;
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_WINNINGBC");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_LOSINGBC");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.ImperialistWar:
                                            switch (Relationship.Value.ActiveWar.GetWarScoreState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        case WarType.DefensiveWar:
                                            switch (Relationship.Value.ActiveWar.GetBorderConflictState())
                                            {
                                                case WarState.WinningSlightly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR");
                                                    continue;
                                                case WarState.Dominating:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_FAIR_WINNING");
                                                    continue;
                                                case WarState.EvenlyMatched:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_EVENLY_MATCHED");
                                                    continue;
                                                case WarState.LosingSlightly:
                                                case WarState.LosingBadly:
                                                    this.OfferPeace(Relationship, "OFFERPEACE_PLEADING");
                                                    continue;
                                                default:
                                                    continue;
                                            }
                                        default:
                                            continue;
                                    }
                                }
                                else
                                    continue;
                            }
                            else
                                continue;
                        default:
                            continue;
                    }
                }
            }
        }

        private void DoRuthlessRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 5f);
            int numberofWars = 0;
            Array<Empire> PotentialTargets = new Array<Empire>();
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.AtWar || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                numberofWars+=(int)Relationship.Key.currentMilitaryStrength*2;  //++;
                
            }
        //Label0:
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Key.data.DiplomaticPersonality != null && !Relationship.Value.HaveRejected_TRADE && !Relationship.Value.Treaty_Trade && !Relationship.Value.AtWar && (Relationship.Key.data.DiplomaticPersonality.Name != "Aggressive" || Relationship.Key.data.DiplomaticPersonality.Name != "Ruthless"))
                {
                    Offer NAPactOffer = new Offer()
                    {
                        TradeTreaty = true,
                        AcceptDL = "Trade Accepted",
                        RejectDL = "Trade Rejected"
                    };
                    Ship_Game.Gameplay.Relationship value = Relationship.Value;
                    NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
                    Offer OurOffer = new Offer()
                    {
                        TradeTreaty = true
                    };
                    Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                this.AssessAngerAggressive(Relationship, Relationship.Value.Posture, usedTrust);
                Relationship.Value.Posture = Posture.Hostile;
                if (!Relationship.Value.Known || Relationship.Value.AtWar)
                {
                    continue;
                }
                Relationship.Value.Posture = Posture.Hostile;
                if (Relationship.Key == Empire.Universe.PlayerEmpire && Relationship.Value.Threat <= -15f && !Relationship.Value.HaveInsulted_Military && Relationship.Value.TurnsKnown > this.FirstDemand)
                {
                    Relationship.Value.HaveInsulted_Military = true;
                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Insult Military"));
                }
                if (Relationship.Value.Threat > 0f || Relationship.Value.TurnsKnown <= this.SecondDemand || Relationship.Value.Treaty_Alliance)
                {
                    if (Relationship.Value.Threat > -45f || numberofWars  >this.empire.currentMilitaryStrength ) //!= 0)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relationship.Key);
                }
                else
                {
                    int i = 0;
                    while (i < 5)
                    {
                        if (i >= this.DesiredPlanets.Count)
                        {
                            //goto Label0;    //this tried to restart the loop it's in => bad mojo
                            break;
                        }
                        if (this.DesiredPlanets[i].Owner != Relationship.Key)
                        {
                            i++;
                        }
                        else
                        {
                            PotentialTargets.Add(Relationship.Key);
                            //goto Label0;
                            break;
                        }
                    }
                }
            }
            if (PotentialTargets.Count > 0 && numberofWars <= this.empire.currentMilitaryStrength )//1)
            {
                IOrderedEnumerable<Empire> sortedList = 
                    from target in PotentialTargets
                    orderby Vector2.Distance(this.empire.GetWeightedCenter(), target.GetWeightedCenter())
                    select target;
                bool foundwar = false;
                foreach (Empire e in PotentialTargets)
                {
                    Empire ToAttack = e;
                    if (this.empire.GetRelations(e).Treaty_NAPact)
                    {
                        continue;
                    }
                    this.empire.GetRelations(ToAttack).PreparingForWar = true;
                    foundwar = true;
                }
                if (!foundwar)
                {
                    Empire ToAttack = sortedList.First<Empire>();
                    this.empire.GetRelations(ToAttack).PreparingForWar = true;
                }
            }
        }

        private void DoXenophobicRelations()
        {
            this.AssessTeritorialConflicts(this.empire.data.DiplomaticPersonality.Territorialism / 10f);
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                float usedTrust = 0f;
                foreach (TrustEntry te in Relationship.Value.TrustEntries)
                {
                    usedTrust = usedTrust + te.TrustCost;
                }
                this.AssessDiplomaticAnger(Relationship);
                switch (Relationship.Value.Posture)
                {
                    case Posture.Friendly:
                    {
                        if (Relationship.Value.TurnsKnown <= SecondDemand || Relationship.Value.Trust - usedTrust <= empire.data.DiplomaticPersonality.Trade || Relationship.Value.Treaty_Trade || Relationship.Value.HaveRejected_TRADE || Relationship.Value.turnsSinceLastContact <= this.SecondDemand || Relationship.Value.HaveRejected_TRADE)
                        {
                            continue;
                        }
                        Offer NAPactOffer = new Offer()
                        {
                            TradeTreaty = true,
                            AcceptDL = "Trade Accepted",
                            RejectDL = "Trade Rejected"
                        };
                        Ship_Game.Gameplay.Relationship value = Relationship.Value;
                        NAPactOffer.ValueToModify = new Ref<bool>(() => value.HaveRejected_TRADE, (bool x) => value.HaveRejected_TRADE = x);
                        Offer OurOffer = new Offer()
                        {
                            TradeTreaty = true
                        };
                        if (Relationship.Key != Empire.Universe.PlayerEmpire)
                        {
                            Relationship.Key.GetGSAI().AnalyzeOffer(OurOffer, NAPactOffer, this.empire, Offer.Attitude.Respectful);
                            continue;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Offer Trade", new Offer(), NAPactOffer));
                            continue;
                        }
                    }
                    case Posture.Neutral:
                    {
                        if (Relationship.Value.TurnsKnown >= this.FirstDemand && !Relationship.Value.Treaty_NAPact && !Relationship.Value.HaveRejected_Demand_Tech && !Relationship.Value.XenoDemandedTech)
                        {
                            Array<string> PotentialDemands = new Array<string>();
                            foreach (KeyValuePair<string, TechEntry> tech in Relationship.Key.GetTDict())
                            {
                                //Added by McShooterz: prevent root nodes from being demanded, and secret but not discovered
                                if (!tech.Value.Unlocked || this.empire.GetTDict()[tech.Key].Unlocked || tech.Value.Tech.RootNode == 1 || (tech.Value.Tech.Secret && !tech.Value.Tech.Discovered))
                                {
                                    continue;
                                }
                                PotentialDemands.Add(tech.Key);
                            }
                            if (PotentialDemands.Count > 0)
                            {
                                int Random = (int)RandomMath.RandomBetween(0f, (float)PotentialDemands.Count + 0.75f);
                                if (Random > PotentialDemands.Count - 1)
                                {
                                    Random = PotentialDemands.Count - 1;
                                }
                                string TechToDemand = PotentialDemands[Random];
                                Offer DemandTech = new Offer();
                                DemandTech.TechnologiesOffered.Add(TechToDemand);
                                Relationship.Value.XenoDemandedTech = true;
                                Offer TheirDemand = new Offer()
                                {
                                    AcceptDL = "Xeno Demand Tech Accepted",
                                    RejectDL = "Xeno Demand Tech Rejected"
                                };
                                Ship_Game.Gameplay.Relationship relationship = Relationship.Value;
                                TheirDemand.ValueToModify = new Ref<bool>(() => relationship.HaveRejected_Demand_Tech, (bool x) => relationship.HaveRejected_Demand_Tech = x);
                                Relationship.Value.turnsSinceLastContact = 0;
                                if (Relationship.Key != Empire.Universe.PlayerEmpire)
                                {
                                    Relationship.Key.GetGSAI().AnalyzeOffer(DemandTech, TheirDemand, this.empire, Offer.Attitude.Threaten);
                                }
                                else
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Xeno Demand Tech", DemandTech, TheirDemand));
                                }
                            }
                        }
                        if (!Relationship.Value.HaveRejected_Demand_Tech)
                        {
                            continue;
                        }
                        Relationship.Value.Posture = Posture.Hostile;
                        continue;
                    }
                    default:
                    {
                        continue;
                    }
                }
            }
        }

        public void EndWarFromEvent(Empire them)
        {
            this.empire.GetRelations(them).AtWar = false;
            them.GetRelations(this.empire).AtWar = false;
            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                {
                    if (this.empire.GetFleetsDict().ContainsKey(task.WhichFleet) && this.empire.data.Traits.Name == "Corsairs")
                    {
                        bool foundhome = false;
                        foreach (Ship ship in this.empire.GetShips())
                        {
                            if (!(ship.shipData.Role == ShipData.RoleName.station) && !(ship.shipData.Role == ShipData.RoleName.platform))
                            {
                                continue;
                            }
                            foundhome = true;
                            foreach (Ship fship in empire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.OrderQueue.Clear();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                        if (!foundhome)
                        {
                            foreach (Ship ship in this.empire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                ship.AI.OrderQueue.Clear();
                                ship.AI.State = AIState.AwaitingOrders;
                            }
                        }
                    }
                    task.EndTaskWithMove();

                }, false, false, false);
            }
        }

        public void FactionUpdate()
        {
            string name = this.empire.data.Traits.Name;
            switch (name)
            {
                case "The Remnant":
                    {
                        bool HasPlanets = false; // this.empire.GetPlanets().Count > 0;
                        foreach(Planet planet in this.empire.GetPlanets())
                        {
                            HasPlanets = true;
                            
                            foreach(QueueItem item in planet.ConstructionQueue)
                            {
                            
                                {
                                    item.Cost = 0;                                                                        
                                }
                            

                            }
                            planet.ApplyProductiontoQueue(1, 0);
                            
                        }
                        foreach (Ship assimilate in this.empire.GetShips())
                        {
                            if (assimilate.shipData.ShipStyle != "Remnant" && assimilate.shipData.ShipStyle != null)
                            {
                                if (HasPlanets)
                                {
                                    if (assimilate.GetStrength() <= 0)
                                    {

                                        Planet target = null;
                                        if (assimilate.System!= null)
                                        {
                                            target = assimilate.System.PlanetList.Where(owner => owner.Owner != this.empire && owner.Owner != null).FirstOrDefault();

                                        }
                                        if (target != null)
                                        {
                                            assimilate.shipData.Role = ShipData.RoleName.troop;
                                            assimilate.TroopList.Add(ResourceManager.CreateTroop("Remnant Defender", assimilate.loyalty));

                                            if (assimilate.GetStrength() <= 0)
                                            {
                                                assimilate.isColonyShip = true;

                                                // @todo this looks like FindMinFiltered
                                                Planet capture = Empire.Universe.PlanetsDict.Values
                                                    .Where(potentials => potentials.Owner == null && potentials.habitable)
                                                    .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Position))
                                                    .FirstOrDefault();
                                                if (capture != null)
                                                    assimilate.AI.OrderColonization(capture);
                                            }

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
                                            .Where(potentials => potentials.Owner == null && potentials.habitable)
                                            .OrderBy(potentials => Vector2.Distance(assimilate.Center, potentials.Position))
                                            .FirstOrDefault();
                                        if (capture != null)
                                            assimilate.AI.OrderColonization(capture);



                                    }

                                }


                            }
                            else
                            {
                                Planet target = null;
                                if (assimilate.System!= null && assimilate.AI.State == AIState.AwaitingOrders)
                                {
                                    target = assimilate.System.PlanetList.Where(owner => owner.Owner != this.empire && owner.Owner != null).FirstOrDefault();
                                    if (target !=null && (assimilate.HasTroopBay || assimilate.hasAssaultTransporter))
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
                            this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                            {
                                if (task.type != MilitaryTask.TaskType.CorsairRaid)
                                {
                                    return;
                                }
                                AttackingSomeone = true;
                            }, false, false, false);
                        }
                        if (!AttackingSomeone)
                        {
                            foreach (KeyValuePair<Empire, Relationship> r in this.empire.AllRelations)
                            {
                                if (!r.Value.AtWar || r.Key.GetPlanets().Count <= 0 || this.empire.GetShips().Count <= 0)
                                {
                                    continue;
                                }
                                Vector2 center = new Vector2();
                                foreach (Ship ship in this.empire.GetShips())
                                {
                                    center = center + ship.Center;
                                }
                                center = center / (float)this.empire.GetShips().Count;
                                IOrderedEnumerable<Planet> sortedList =
                                    from planet in r.Key.GetPlanets()
                                    orderby Vector2.Distance(planet.Position, center)
                                    select planet;
                                MilitaryTask task = new MilitaryTask(this.empire);
                                task.SetTargetPlanet(sortedList.First<Planet>());
                                task.TaskTimer = 300f;
                                task.type = MilitaryTask.TaskType.CorsairRaid;
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
                this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.Exploration)
                    {
                        task.Evaluate(this.empire);
                    }
                    else
                    {
                        task.EndTask();
                    }
                }, false, false, false);
            }
        }

        private void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
        {
            Array<Planet> InvasionTargets = new Array<Planet>();
            foreach (Planet p in this.empire.GetPlanets())
            {
                foreach (Planet toCheck in p.system.PlanetList)
                {
                    if (toCheck.Owner == null || toCheck.Owner == this.empire || !toCheck.Owner.isFaction && !this.empire.GetRelations(toCheck.Owner).AtWar)
                    {
                        continue;
                    }
                    InvasionTargets.Add(toCheck);
                }
            }
            if (InvasionTargets.Count > 0)
            {
                Planet target = InvasionTargets[0];
                bool OK = true;

                using (TaskList.AcquireReadLock())
                {
                    foreach (MilitaryTask task in this.TaskList)
                    {
                        if (task.GetTargetPlanet() != target)
                        {
                            continue;
                        }
                        OK = false;
                        break;
                    }
                }
                if (OK)
                {
                    MilitaryTask InvadeTask = new MilitaryTask(target, this.empire);
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(InvadeTask);
                    }
                }
            }
            Array<Planet> PlanetsWeAreInvading = new Array<Planet>();
            //lock (GlobalStats.TaskLocker)
            {
                this.TaskList.ForEach(task =>//foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet || task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner != r.Key)
                    {
                        return;
                    }
                    PlanetsWeAreInvading.Add(task.GetTargetPlanet());
                },false,false,false);
            }
            if (PlanetsWeAreInvading.Count < 3 && this.empire.GetPlanets().Count > 0)
            {
                Vector2 vector2 = this.FindAveragePosition(this.empire);
                this.FindAveragePosition(r.Key);
                IOrderedEnumerable<Planet> sortedList = 
                    from planet in r.Key.GetPlanets()
                    orderby Vector2.Distance(vector2, planet.Position)
                    select planet;
                foreach (Planet p in sortedList)
                {
                    if (PlanetsWeAreInvading.Contains(p))
                    {
                        continue;
                    }
                    if (PlanetsWeAreInvading.Count >= 3)
                    {
                        break;
                    }
                    PlanetsWeAreInvading.Add(p);
                    MilitaryTask invade = new MilitaryTask(p, this.empire);
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(invade);
                    }
                }
            }
        }

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r)
        {
            float warWeight = 1 +this.empire.getResStrat().ExpansionPriority + this.empire.getResStrat().MilitaryPriority;
            foreach (MilitaryTask item_0 in (Array<MilitaryTask>)this.TaskList)
            {
                if (item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                {
                    warWeight--;
                    
                }
                if (warWeight < 0)
                    return;
            }
            Array<SolarSystem> s;
            SystemCommander scom;
            switch (r.Value.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    Array<Planet> list1 = new Array<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy(r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet) / 150000 + (r.Key.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(planet.ParentSystem, out scom) ? scom.RankImportance : 0)));
                        int x = (int)UniverseData.UniverseWidth;
                    s = new Array<SolarSystem>();
                        
                    for (int index = 0; index < Enumerable.Count(orderedEnumerable1); ++index)
                    {
                        Planet p = Enumerable.ElementAt(orderedEnumerable1, index);
                        if(s.Count > warWeight)
                                            break;

                        if (!s.Contains(p.ParentSystem))
                        {s.Add(p.ParentSystem);}
                        //if(s.Count >2)
                        //    break;
                        list1.Add(p);
                    }
                    foreach (Planet planet in list1)
                    {
                        bool canAddTask = true;

                        using (TaskList.AcquireReadLock())
                        {
                            foreach (MilitaryTask task in TaskList)
                            {
                                if (task.GetTargetPlanet() == planet && task.type == MilitaryTask.TaskType.AssaultPlanet)
                                {
                                    canAddTask = false;
                                    break;
                                }
                            }
                        }
                        if (canAddTask)
                        {
                            TaskList.Add(new MilitaryTask(planet, empire));
                        }
                    }
                    break;
                case WarType.ImperialistWar:
                    Array<Planet> list2 = new Array<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable2 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet) / 150000 + (r.Key.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(planet.ParentSystem, out scom) ? scom.RankImportance : 0)));
                    s = new Array<SolarSystem>();
                    for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable2); ++index)
                    {
                        Planet p = Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable2, index);
                        if(s.Count > warWeight)
                                            break;

                        if (!s.Contains(p.ParentSystem))
                        { s.Add(p.ParentSystem); }
                        //if (s.Count > 2)
                        //    break;
                        list2.Add(p);
                 
                    }
                    foreach (Planet planet in list2)
                    {
                        bool flag = true;
                        bool claim = false;
                        bool claimPressent = false;
                        if (!s.Contains(planet.ParentSystem))
                            continue;
                        using (TaskList.AcquireReadLock())
                        {
                            foreach (MilitaryTask item_1 in (Array<MilitaryTask>)this.TaskList)
                            {
                                if (item_1.GetTargetPlanet() == planet)
                                {
                                    if (item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                        flag = false;
                                    if (item_1.type == MilitaryTask.TaskType.DefendClaim)
                                    {
                                        claim = true;
                                        if (item_1.Step == 2)
                                            claimPressent = true;
                                    }

                                }
                            }
                        }
                        if (flag && claimPressent)
                        {
                            TaskList.Add(new MilitaryTask(planet, this.empire));
                        }
                        if (!claim)
                        {
                            MilitaryTask task = new MilitaryTask()
                            {
                                AO = planet.Position
                            };
                            task.SetEmpire(this.empire);
                            task.AORadius = 75000f;
                            task.SetTargetPlanet(planet);
                            task.TargetPlanetGuid = planet.guid;
                            task.type = MilitaryTask.TaskType.DefendClaim;
                            TaskList.Add(task);
                        }
                    }
                    break;
            }
        }

        private Vector2 FindAveragePosition(Empire e)
        {
            Vector2 AvgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())
            {
                AvgPos = AvgPos + p.Position;
            }
            if (e.GetPlanets().Count <= 0)
            {
                return Vector2.Zero;
            }
            Vector2 count = AvgPos / (float)e.GetPlanets().Count;
            AvgPos = count;
            return count;
        }

        private SolarSystem FindBestRoadOrigin(SolarSystem Origin, SolarSystem Destination)
        {
            SolarSystem Closest = Origin;
            Array<SolarSystem> ConnectedToOrigin = new Array<SolarSystem>();
            foreach (SpaceRoad road in this.empire.SpaceRoadsList)
            {
                if (road.GetOrigin() != Origin)
                {
                    continue;
                }
                ConnectedToOrigin.Add(road.GetDestination());
            }
            foreach (SolarSystem system in ConnectedToOrigin)
            {
                if (Vector2.Distance(system.Position, Destination.Position) + 25000f >= Vector2.Distance(Closest.Position, Destination.Position))
                {
                    continue;
                }
                Closest = system;
            }
            if (Closest != Origin)
            {
                Closest = this.FindBestRoadOrigin(Closest, Destination);
            }
            return Closest;
        }

        private float FindTaxRateToReturnAmount(float Amount)
        {
            for (int i = 0; i < 100; i++)
            {
                if (this.empire.EstimateIncomeAtTaxRate((float)i / 100f) >= Amount)
                {
                    return (float)i / 100f;
                }
            }
            //if (this.empire.ActualNetLastTurn < 0 && this.empire.data.TaxRate >=50)
            //{
            //    float tax = this.empire.data.TaxRate + .05f;
            //    tax = tax > 100 ? 100 : tax;
            //}
            return 1;//0.50f;
        }

        private string GetAnAssaultShip()
        {
            Array<Ship> PotentialShips = new Array<Ship>();
            foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
            {
                if (ResourceManager.ShipsDict[shipsWeCanBuild].TroopList.Count <= 0)
                {
                    continue;
                }
                PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild]);
            }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList = 
                    from ship in PotentialShips
                    orderby ship.TroopList.Count descending
                    select ship;
                if (sortedList.Count<Ship>() > 0)
                {
                    return sortedList.First<Ship>().Name;
                }
            }
            return "";
        }

/*
        //added by Gremlin Deveks Get a ship
        private string GetAShip(float Capacity)
        {
            string name;
            float ratio_Fighters = 7f;
            float ratio_Corvettes = 5f;
            float ratio_Frigates = 7f;
            float ratio_Cruisers = 5f;
            float ratio_Capitals = 3f;
            float TotalMilShipCount = 0f;
            float numFighters = 0f;
            float numCorvettes = 0f;
            float numFrigates = 0f;
            float numCruisers = 0f;
            float numCapitals = 0f;
            float numFreighters = 0f;
            float numPlatforms = 0f;
            float numStations = 0f;
            float num_Bombers = 0f;
            float capFighters = 0f;
            float capCorvettes = 0f;
            float capFrigates = 0f;
            float capCruisers = 0f;
            float capCapitals = 0f;
            //float capBombers = 0f;
            float capCarriers = 0f;
            float nonMilitaryCap = 0;
            float numScrapping = 0;
            for (int i = 0; i < this.empire.GetShips() .Count(); i++)
            {
                Ship item = this.empire.GetShips()[i];
                if (item != null)
                {
                    ShipData.RoleName role = item.shipData.Role;
                    ShipData.RoleName str = role;
                    float upkeep = 0f;
                    //if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    //{
                    //    upkeep = item.GetMaintCostRealism();
                    //}
                    //else
                    //{
                        upkeep = item.GetMaintCost();
                    //}
  
                    //item.PowerDraw * this.empire.data.FTLPowerDrainModifier <= item.PowerFlowMax 
                    //&& item.IsWarpCapable &&item.PowerStoreMax /(item.PowerDraw* this.empire.data.FTLPowerDrainModifier) * item.velocityMaximum >Properties.Settings.Default.minimumWarpRange  && item.Name != "Small Supply Ship"
                    
                    if (role != null && item.Mothership == null)
                    {
                        // make the AI actually count scout hulls in its goals!!! Otherwise it can flood shedloads of them...
                        if (str == ShipData.RoleName.fighter || str == ShipData.RoleName.scout)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numFighters = numFighters + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capFighters += upkeep;

     
                        }
                        else if (str == ShipData.RoleName.corvette || str == ShipData.RoleName.gunboat)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numCorvettes = numCorvettes + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capCorvettes += upkeep;    
                        }
                        else if (str == ShipData.RoleName.frigate || str == ShipData.RoleName.destroyer)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numFrigates = numFrigates + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capFrigates += upkeep;
                        }
                        else if (str == ShipData.RoleName.cruiser)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numCruisers = numCruisers + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capCruisers += upkeep;  
                        }
                        else if (str == ShipData.RoleName.capital)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numCapitals = numCapitals + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capCapitals += upkeep;    
                        }
                        else if (str == ShipData.RoleName.carrier)
                        {
                            if (item.GetAI().State == AIState.Scrap)
                            {
                                numScrapping++;
                                TotalMilShipCount++;
                                continue;
                            }
                            numCapitals = numCapitals + 1f;
                            TotalMilShipCount = TotalMilShipCount + 1f;
                            capCarriers += upkeep;
                        }
                        else if (item.GetAI().State == AIState.Scrap)
                        {                           
                            continue;
                        }
                        else if (str == ShipData.RoleName.freighter)
                        {
                            numFreighters += upkeep;

                            nonMilitaryCap += upkeep;
                        }
                        else if (str == ShipData.RoleName.platform)
                        {
                            numPlatforms += upkeep;

                            nonMilitaryCap += upkeep;
                        }
                        else if (str == ShipData.RoleName.station)
                        {
                            numStations += upkeep;

                            nonMilitaryCap += upkeep;
                        }
                    }
                    if (item.GetAI().State == AIState.Scrap)
                    {
                        continue;
                    }
                    if (item.BombBays.Count > 0)
                    {
                        num_Bombers = num_Bombers + 1f;
                        
                    }
                }
            }
            this.FreighterUpkeep = numFreighters -(nonMilitaryCap * .25f);
            this.PlatformUpkeep = numPlatforms -(nonMilitaryCap * .25f);
            this.StationUpkeep = numStations -(nonMilitaryCap * .5f);
            if (!this.empire.canBuildCapitals && Ship_Game.ResourceManager.TechTree.ContainsKey("Battleships"))
                this.empire.canBuildCapitals = this.empire.GetTDict()["Battleships"].Unlocked;
            if (!this.empire.canBuildCruisers && Ship_Game.ResourceManager.TechTree.ContainsKey("Cruisers"))
                this.empire.canBuildCruisers = this.empire.GetTDict()["Cruisers"].Unlocked;
            if (!this.empire.canBuildFrigates && Ship_Game.ResourceManager.TechTree.ContainsKey("FrigateConstruction"))
                this.empire.canBuildFrigates = this.empire.GetTDict()["FrigateConstruction"].Unlocked;
            if (!this.empire.canBuildCorvettes && Ship_Game.ResourceManager.TechTree.ContainsKey("HeavyFighterHull"))
                this.empire.canBuildCorvettes = this.empire.GetTDict()["HeavyFighterHull"].Unlocked;

            //Added by McShooterz: Used to find alternate techs that allow roles to be used by AI.
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useAlternateTech)
            {
                foreach (KeyValuePair<string, TechEntry> techEntry in this.empire.GetTDict().Where(entry => entry.Value.Unlocked))
                {
                    if (this.empire.canBuildCapitals && this.empire.canBuildCruisers && this.empire.canBuildFrigates && this.empire.canBuildCorvettes)
                        break;
                    if (!this.empire.canBuildCapitals && techEntry.Value.GetTech().unlockBattleships)
                        this.empire.canBuildCapitals = true;
                    else if (!this.empire.canBuildCruisers && techEntry.Value.GetTech().unlockCruisers)
                        this.empire.canBuildCruisers = true;
                    else if (!this.empire.canBuildFrigates && techEntry.Value.GetTech().unlockFrigates)
                        this.empire.canBuildFrigates = true;
                    else if (!this.empire.canBuildCorvettes && techEntry.Value.GetTech().unlockCorvettes)
                        this.empire.canBuildCorvettes = true;
                }
            }
            if (this.empire.canBuildCapitals)
            {
                ratio_Fighters = 0f;
                ratio_Corvettes = .1f;
                ratio_Frigates = 2.25f;
                ratio_Cruisers = 4f;
                ratio_Capitals = 3f;
            }
            else if (this.empire.canBuildCruisers)
            {

                ratio_Fighters = .5f;
                ratio_Corvettes = 1f;
                ratio_Frigates = 3f;
                ratio_Cruisers = 5.5f;
                ratio_Capitals = 0f;
            }
            else if (this.empire.canBuildFrigates)
            {
                ratio_Fighters = .5f;
                ratio_Corvettes = 1f;
                ratio_Frigates = 5f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
            }
            else if (this.empire.canBuildCorvettes)
            {
                ratio_Fighters = 2f;
                ratio_Corvettes = 6f;
                ratio_Frigates = 0f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
            }
            else
            {
                ratio_Fighters = 1f;
                ratio_Corvettes = 0f;
                ratio_Frigates = 0f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
            }
            if(!this.empire.canBuildFrigates)
                TotalMilShipCount = TotalMilShipCount < 50f ? 50f : TotalMilShipCount;
            //float single = TotalMilShipCount / 10f;
            float totalRatio = ratio_Fighters + ratio_Corvettes + ratio_Frigates + ratio_Cruisers + ratio_Capitals;
            float adjustedRatio = TotalMilShipCount / totalRatio;
            int DesiredFighters = (int)(adjustedRatio * ratio_Fighters);
            int DesiredBombers = (int)(adjustedRatio * (ratio_Fighters != 0 ? ratio_Fighters * .25f : ratio_Frigates * .25f));
            int DesiredCorvettes = (int)(adjustedRatio * ratio_Corvettes);
            int DesiredFrigates = (int)(adjustedRatio * ratio_Frigates);
            int DesiredCruisers = (int)(adjustedRatio * ratio_Cruisers);
            int DesiredCapitals = (int)(adjustedRatio * ratio_Capitals);
            int DesiredCarriers = (int)(adjustedRatio);
            int DesiredTroopShips = (int)(TotalMilShipCount / 15f);

            float TotalCapacity = this.buildCapacity;// this.empire.GetTotalShipMaintenance(); // 
           // float TotalCapacity = this.empire.GrossTaxes; //ship building is being severely restricted here. changing to increasing a bit.
            float DesiredFighterSpending = (TotalCapacity / totalRatio) * ratio_Fighters;
            float DesiredCorvetteSpending = (TotalCapacity / totalRatio) * ratio_Corvettes;
            float DesiredFrigateSpending = (TotalCapacity / totalRatio) * ratio_Frigates;
            float DesiredCruiserSpending = (TotalCapacity / totalRatio) * ratio_Cruisers;
            float DesiredCapitalSpending = (TotalCapacity / totalRatio) * ratio_Capitals;
            float DesiredCarrierSpending = ((DesiredCruiserSpending + DesiredCapitalSpending) / 4f);
            float DesiredMarineSpending = (TotalCapacity / totalRatio);


            float fighterOverspend = capFighters - DesiredFighterSpending;
            float corvetteOverspend = this.empire.canBuildCorvettes ? capCorvettes - DesiredCorvetteSpending : 0f;
            float frigateOverspend = this.empire.canBuildFrigates ? capFrigates - DesiredFrigateSpending : 0f;
            float cruiserOverspend = this.empire.canBuildCruisers ? capCruisers - DesiredCruiserSpending : 0f;
            float capitalOverspend = this.empire.canBuildCapitals ? capCapitals - DesiredCapitalSpending : 0f;
          
            // this used to be if (Capacity == 0)... Well (if you check the calculation elsewhere) it could very easily be a negative value if capacity was insufficient for fleet due to changes. Made it <=, also made it less than 2 so AI more pro-actively re-arranges fleets.
            if (Capacity <= 0)
            #region MyRegion
            {

                int scrapFighters = (int)numFighters - (int)DesiredFighters;
                int scrapCorvettes = (int)numCorvettes - (int)DesiredCorvettes;
                int scrapFrigates = (int)numFrigates - (int)DesiredFrigates;
                int scrapCruisers = (int)numCruisers - (int)DesiredCruisers;

                // because we actually care about corvettes now. Will trigger only if the overspend on a class is more than 10% over-budget for that class to avoid constant correction over the value of a single ship. The scrapping takes it below 5% when triggered.
                if (fighterOverspend > (DesiredFighterSpending * 0.1f)
                    || corvetteOverspend > (DesiredCorvetteSpending * 0.1f)
                    || frigateOverspend > (DesiredFrigateSpending * 0.1f)
                    || cruiserOverspend > (DesiredCruiserSpending * 0.1f)
                    || capitalOverspend > (DesiredCapitalSpending * 0.1f))
                {
                    foreach (Ship ship in this.empire.GetShips()
                        .Where(ship => !ship.InCombat && ship.inborders && ship.fleet == null)
                        .OrderByDescending(ship => ship.GetAI().State == AIState.Scrap)
                        .ThenByDescending(defense => this.DefensiveCoordinator.DefensiveForcePool.Contains(defense))
                        .ThenBy(ship => ship.Level)
                        .ThenBy(ship => ship.BaseStrength)
                        //.ThenByDescending(ship => ship.fleet==null)
                        )
                    //foreach(Ship ship in this.DefensiveCoordinator.DefensiveForcePool)
                    {
                        if (fighterOverspend > (DesiredFighterSpending * 0.05f) && (ship.shipData.Role == ShipData.RoleName.fighter || ship.shipData.Role == ShipData.RoleName.scout))
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                            {
                                fighterOverspend -= ship.GetMaintCostRealism();
                            }
                            else
                            {
                                fighterOverspend -= ship.GetMaintCost();
                            }
                            if (ship.GetAI().State != AIState.Scrap)
                                ship.GetAI().OrderScrapShip();
                        }
                        if (corvetteOverspend > (DesiredCorvetteSpending * 0.05f) && (ship.shipData.Role == ShipData.RoleName.corvette || ship.shipData.Role == ShipData.RoleName.gunboat))
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                            {
                                fighterOverspend -= ship.GetMaintCostRealism();
                            }
                            else
                            {
                                fighterOverspend -= ship.GetMaintCost();
                            }
                            if (ship.GetAI().State != AIState.Scrap)
                                ship.GetAI().OrderScrapShip();
                        }
                        if (frigateOverspend > (DesiredFrigateSpending * 0.05f) && (ship.shipData.Role == ShipData.RoleName.frigate || ship.shipData.Role == ShipData.RoleName.destroyer))
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                            {
                                fighterOverspend -= ship.GetMaintCostRealism();
                            }
                            else
                            {
                                fighterOverspend -= ship.GetMaintCost();
                            }
                            if (ship.GetAI().State != AIState.Scrap)
                                ship.GetAI().OrderScrapShip();
                        }
                        if (cruiserOverspend > (DesiredCruiserSpending * 0.05f) && ship.shipData.Role == ShipData.RoleName.cruiser)
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                            {
                                fighterOverspend -= ship.GetMaintCostRealism();
                            }
                            else
                            {
                                fighterOverspend -= ship.GetMaintCost();
                            }
                            if (ship.GetAI().State != AIState.Scrap)
                                ship.GetAI().OrderScrapShip();
                        }
                        if (capitalOverspend > (DesiredCapitalSpending * 0.05f) && ship.shipData.Role == ShipData.RoleName.capital)
                        {
                            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                            {
                                fighterOverspend -= ship.GetMaintCostRealism();
                            }
                            else
                            {
                                fighterOverspend -= ship.GetMaintCost();
                            }
                            if (ship.GetAI().State != AIState.Scrap)
                                ship.GetAI().OrderScrapShip();
                        }
                        if (fighterOverspend <= (DesiredFighterSpending * 0.05f)
                            && corvetteOverspend <= (DesiredCorvetteSpending * 0.05f)
                            && frigateOverspend <= (DesiredFrigateSpending * 0.05f)
                            && cruiserOverspend <= (DesiredCruiserSpending * 0.05f)
                            && capitalOverspend <= (DesiredCapitalSpending * 0.05f))
                        {
                            break;
                        }
                        else
                            continue;
                    }
                    // this line below crashes the ship picker if we ever actually get to it and is unnecessary afaik? Doc.

                }
                return "";
            }
            
            #endregion
            Array<Ship> PotentialShips = new Array<Ship>();
            this.empire.UpdateShipsWeCanBuild();

            string buildThis = "";
            // Always prioritise the construction of the most UNDERSPENT (hence <) budget going down the list.
            if (this.empire.canBuildCapitals && capCapitals < DesiredCapitalSpending 
                && capitalOverspend / ratio_Capitals < cruiserOverspend / ratio_Cruisers
                && capitalOverspend / ratio_Capitals < frigateOverspend / ratio_Frigates
                && capitalOverspend / ratio_Capitals < corvetteOverspend / ratio_Corvettes
                && capitalOverspend / ratio_Capitals < fighterOverspend / ratio_Fighters)
            {
                buildThis = this.PickFromCandidates(ShipData.RoleName.capital, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                    return buildThis;
                
            }
            if (this.empire.canBuildCruisers && capCruisers < DesiredCruiserSpending
                && cruiserOverspend / ratio_Cruisers < frigateOverspend / ratio_Frigates
                && cruiserOverspend / ratio_Cruisers < corvetteOverspend / ratio_Corvettes
                && cruiserOverspend / ratio_Cruisers < fighterOverspend / ratio_Fighters)
            {
                buildThis = this.PickFromCandidates(ShipData.RoleName.cruiser, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                    return buildThis;
                
            }
                
            if (num_Bombers < (float)DesiredBombers)
            {
                foreach (string shipsWeCanBuild2 in this.empire.ShipsWeCanBuild)
                {
                    //if (ResourceManager.ShipsDict[shipsWeCanBuild2].BombBays.Count <= 0 || ResourceManager.ShipsDict[shipsWeCanBuild2].BaseStrength <= 0f || ResourceManager.ShipsDict[shipsWeCanBuild2].GetMaintCost() >= Capacity || !(ResourceManager.ShipsDict[shipsWeCanBuild2].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild2].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild2].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild2].PowerFlowMax || ResourceManager.ShipsDict[shipsWeCanBuild2].PowerStoreMax / (ResourceManager.ShipsDict[shipsWeCanBuild2].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild2].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild2].velocityMaximum > minimumWarpRange))))
                    Ship ship = ResourceManager.ShipsDict[shipsWeCanBuild2];
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        if (ship.BombBays.Count <= 0 || Capacity <= ship.GetMaintCostRealism() || !shipIsGoodForGoals(ship))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (ship.BombBays.Count <= 0 || Capacity <= ship.GetMaintCost(this.empire) || !shipIsGoodForGoals(ship))
                        {
                            continue;
                        }
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild2]);
                }
                if (PotentialShips.Count > 0)
                {
                    IOrderedEnumerable<Ship> sortedList =
                        from ship in PotentialShips
                        orderby ship.BaseStrength
                        select ship;
                    float totalStrength = 0f;
                    foreach (Ship ship8 in sortedList)
                    {
                        totalStrength = totalStrength + ship8.BaseStrength;
                    }
                    float ran = RandomMath.RandomBetween(0f, totalStrength);
                    float strcounter = 0f;
                    foreach (Ship ship9 in sortedList)
                    {
                        strcounter = strcounter + ship9.BaseStrength;
                        if (strcounter <= ran)
                        {
                            continue;
                        }
                        name = ship9.Name;
                        return name;
                    }
                    PotentialShips.Clear();
                }
            }
            if (this.empire.canBuildFrigates && capFrigates < DesiredFrigateSpending
                && frigateOverspend / ratio_Frigates < corvetteOverspend / ratio_Corvettes
                && frigateOverspend / ratio_Frigates < fighterOverspend / ratio_Fighters)
            {
                buildThis = this.PickFromCandidates(ShipData.RoleName.destroyer, Capacity, PotentialShips);
                if (string.IsNullOrEmpty(buildThis))
                buildThis = this.PickFromCandidates(ShipData.RoleName.frigate, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                    

                    return buildThis;
                
            }
            if (this.empire.canBuildCorvettes && capCorvettes < DesiredCorvetteSpending
                && corvetteOverspend / ratio_Corvettes < fighterOverspend / ratio_Fighters)
            {
                buildThis = this.PickFromCandidates(ShipData.RoleName.corvette, Capacity, PotentialShips);
                if (string.IsNullOrEmpty(buildThis))
                    buildThis = this.PickFromCandidates(ShipData.RoleName.gunboat, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                    return buildThis;


            }
            if (capFighters < DesiredFighterSpending)
            {
                buildThis = this.PickFromCandidates(ShipData.RoleName.fighter, Capacity, PotentialShips);
                if (!string.IsNullOrEmpty(buildThis))
                    return buildThis;

            }
            //added by Gremlin Get Carriers
            bool carriers = this.empire.ShipsWeCanBuild.Where(hangars =>   ResourceManager.ShipsDict[hangars].GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() >0 == true).Count() > 0;
            if(carriers && capCarriers < DesiredCarrierSpending)
            foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
            {
                //if (!(ResourceManager.ShipsDict[shipsWeCanBuild3].GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0) || ResourceManager.ShipsDict[shipsWeCanBuild3].BaseStrength <= 0f || !(ResourceManager.ShipsDict[shipsWeCanBuild3].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild3].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax || (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerStoreMax) / (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild3].velocityMaximum > minimumWarpRange))))
                Ship ship = ResourceManager.ShipsDict[shipsWeCanBuild3];
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    if (ship.GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() == 0 || Capacity <= ship.GetMaintCostRealism() || !shipIsGoodForGoals(ship))
                    {
                        continue;
                    }
                }
                else
                {
                    if (ship.GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() == 0 || Capacity <= ship.GetMaintCost(this.empire) || !shipIsGoodForGoals(ship))
                    {
                        continue;
                    }
                }
                PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
            }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship in PotentialShips
                    orderby ship.BaseStrength
                    select ship;
                float totalStrength = 0f;
                foreach (Ship ship12 in sortedList)
                {
                    totalStrength = totalStrength + ship12.BaseStrength;
                }
                float ran = RandomMath.RandomBetween(0f, totalStrength);
                float strcounter = 0f;
                foreach (Ship ship13 in sortedList)
                {
                    strcounter = strcounter + ship13.BaseStrength;
                    if (strcounter <= ran)
                    {
                        continue;
                    }
                    name = ship13.Name;
                    return name;
                }
            }

            //added by gremlin troop carriers
            bool TroopShips = this.empire.ShipsWeCanBuild.Where(hangars => ResourceManager.ShipsDict[hangars].GetHangars().Where(fighters => fighters.IsTroopBay).Count() > 0 || ResourceManager.ShipsDict[hangars].hasTransporter).Count() > 0;
            if (TroopShips && DesiredTroopShips > 0)
                foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.ShipsDict[shipsWeCanBuild3];
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        if (ship.GetHangars().Where(fighters => fighters.IsTroopBay ).Count() == 0 || Capacity <= ship.GetMaintCostRealism() || !shipIsGoodForGoals(ship))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (ship.GetHangars().Where(fighters => fighters.IsTroopBay).Count() == 0 || Capacity <= ship.GetMaintCost(this.empire) || !shipIsGoodForGoals(ship))
                        {
                            continue;
                        }
                    }
                    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
                }
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship in PotentialShips
                    orderby ship.BaseStrength
                    select ship;
                float totalStrength = 0f;
                foreach (Ship ship12 in sortedList)
                {
                    totalStrength = totalStrength + ship12.BaseStrength;
                }
                float ran = RandomMath.RandomBetween(0f, totalStrength);
                float strcounter = 0f;
                foreach (Ship ship13 in sortedList)
                {
                    strcounter = strcounter + ship13.BaseStrength;
                    if (strcounter <= ran)
                    {
                        continue;
                    }
                    name = ship13.Name;
                    return name;
                }
            }
            //foreach (string shipsWeCanBuild3 in this.empire.ShipsWeCanBuild)
            //{
            //    //if (!(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == RoleName.fighter) && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == ShipData.RoleName.scout) && !(ResourceManager.ShipsDict[shipsWeCanBuild3].Role == ShipData.RoleName.corvette) || ResourceManager.ShipsDict[shipsWeCanBuild3].BaseStrength <= 0f || !(ResourceManager.ShipsDict[shipsWeCanBuild3].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild3].IsWarpCapable && (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier <= ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax || (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerStoreMax) / (ResourceManager.ShipsDict[shipsWeCanBuild3].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild3].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild3].velocityMaximum > minimumWarpRange))))
            //    Ship ship = ResourceManager.ShipsDict[shipsWeCanBuild3];
            //    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            //    {
            //        if ((ship.Role != ShipData.RoleName.scout && ship.Role != RoleName.fighter) || (Capacity <= ship.GetMaintCostRealism() || !shipIsGoodForGoals(ship)))
            //        {
            //            continue;
            //        }
            //    }
            //    else
            //    {
            //        if ((ship.Role != ShipData.RoleName.scout && ship.Role != RoleName.fighter) || (Capacity <= ship.GetMaintCost(this.empire) || !shipIsGoodForGoals(ship)))
            //        {
            //            continue;
            //        }
            //    }

            //    PotentialShips.Add(ResourceManager.ShipsDict[shipsWeCanBuild3]);
            //}
            //if (PotentialShips.Count > 0)
            //{
            //    IOrderedEnumerable<Ship> sortedList =
            //        from ship in PotentialShips
            //        orderby ship.BaseStrength
            //        select ship;
            //    float totalStrength = 0f;
            //    foreach (Ship ship12 in sortedList)
            //    {
            //        totalStrength = totalStrength + ship12.BaseStrength;
            //    }
            //    float ran = RandomMath.RandomBetween(0f, totalStrength);
            //    float strcounter = 0f;
            //    foreach (Ship ship13 in sortedList)
            //    {
            //        strcounter = strcounter + ship13.BaseStrength;
            //        if (strcounter <= ran)
            //        {
            //            continue;
            //        }
            //        name = ship13.Name;
            //        return name;
            //    }
            //}

           
            return null;
        }
*/

        //fbedard: Build a ship with a random role
        bool nobuild = false;
        private string GetAShip(float Capacity)
        {
            if (nobuild)
                return null;
            float ratio_Fighters = 1f;
            float ratio_Corvettes = 0f;
            float ratio_Frigates = 0f;
            float ratio_Cruisers = 0f;
            float ratio_Capitals = 0f;
            float ratio_Bombers = 0f;
            //float ratio_TroopShips = 0f;          //Not referenced in code, removing to save memory
            float ratio_Carriers = 0f;
            float ratio_Support = 0f;
                  /*     
            float capFighters = 0f;
            float capCorvettes = 0f;
            float capFrigates = 0f;
            float capCruisers = 0f;
            float capCapitals = 0f;
            float capBombers = 0f;
            
                  
            
                    */   
            float capBombers = 0f;
            float capCarriers = 0f;
            float capSupport = 0f;
            float capTroops = 0f;

            float numFighters = 0;
            float numCorvettes = 0;
            float numFrigates = 0;
            float numCruisers = 0;
            float numCarriers = 0f;
            float numBombers = 0f;
            float numCapitals = 0f;
            float numTroops = 0f;
            float numSupport = 0f;
            float capScrapping = 0;
            float TotalUpkeep =0;
            /*
            float capFreighters = 0;
            float capPlatforms = 0;
            float capStations = 0;
            float nonMilitaryCap = 0;
            */
            float TotalMilShipCount = 0f;
            
            //Count the active ships
            for (int i = 0; i < this.empire.GetShips().Count(); i++)
            {
                Ship item = this.empire.GetShips()[i];
                if (item != null && item.Active && item.Mothership == null && item.AI.State != AIState.Scrap)
                {
                    ShipData.RoleName str = item.shipData.HullRole;                    
                    float upkeep = 0f;
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                        upkeep = item.GetMaintCostRealism();
                    else
                        upkeep = item.GetMaintCost();

                    if (item.AI.State == AIState.Scrap)
                    {
                        capScrapping += upkeep;
                        continue;
                    }

                    //carrier
                    if (item.GetHangars().Sum(fighters => fighters.MaximumHangarShipSize > 0 ? fighters.XSIZE* fighters.YSIZE:0) > item.Size*.20f && str >= ShipData.RoleName.freighter)
                    {
                        numCarriers += upkeep;
                        TotalMilShipCount++;
                        capCarriers += upkeep;
                        TotalUpkeep += upkeep;
                    }
                    //troops ship
                    else if ((item.HasTroopBay || item.hasTransporter ||item.hasAssaultTransporter) && str >= ShipData.RoleName.freighter
                        && item.GetHangars().Where(troopbay=> troopbay.IsTroopBay).Sum(size=>size.XSIZE*size.YSIZE ) 
                        + item.Transporters.Sum(troopbay => (troopbay.TransporterTroopAssault >0?troopbay.YSIZE*troopbay.XSIZE:0)) > item.Size *.10f
                        )
                    {
                        numTroops += upkeep;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                        capTroops = +upkeep;

                    }
                 
                    else if (item.hasOrdnanceTransporter || item.hasRepairBeam || item.HasSupplyBays || item.hasOrdnanceTransporter || item.InhibitionRadius > 0                         
                        )
                    {
                        numSupport++;
                        TotalUpkeep += upkeep;
                        TotalMilShipCount++;
                        capSupport += upkeep;
                    }
                    else if (item.BombBays.Count * 4 > item.Size * .20f && str >= ShipData.RoleName.freighter
       )
                    {
                        numBombers+=upkeep;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                        capBombers += upkeep;
                        //capBombers += upkeep;
                    }                               

                    //capital and carrier without hangars
                    else if (str == ShipData.RoleName.capital || str == ShipData.RoleName.carrier)
                    {
                        numCapitals++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    //bomber
               
                    else if (str == ShipData.RoleName.fighter || str == ShipData.RoleName.scout)
                    {
                        numFighters++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                       // capFighters += upkeep;
                    }
                    else if (str == ShipData.RoleName.corvette || str == ShipData.RoleName.gunboat)
                    {
                        numCorvettes++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.frigate || str == ShipData.RoleName.destroyer)
                    {
                        numFrigates++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    else if (str == ShipData.RoleName.cruiser)
                    {
                        numCruisers++;
                        TotalMilShipCount++;
                        TotalUpkeep += upkeep;
                    }
                    

                    /*
                    else if (str == ShipData.RoleName.freighter)
                    {
                        capFreighters += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    else if (str == ShipData.RoleName.platform)
                    {
                        capPlatforms += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    else if (str == ShipData.RoleName.station)
                    {
                        capStations += upkeep;
                        nonMilitaryCap += upkeep;
                    }
                    */
                }
            }

            /*
            this.FreighterUpkeep = capFreighters - (nonMilitaryCap * .25f);
            this.PlatformUpkeep = capPlatforms - (nonMilitaryCap * .25f);
            this.StationUpkeep = capStations - (nonMilitaryCap * .5f);
            */
            //if (!this.empire.canBuildCapitals && Ship_Game.ResourceManager.TechTree.ContainsKey("Battleships"))
            //    this.empire.canBuildCapitals = this.empire.GetTDict()["Battleships"].Unlocked;
            //if (!this.empire.canBuildCruisers && Ship_Game.ResourceManager.TechTree.ContainsKey("Cruisers"))
            //    this.empire.canBuildCruisers = this.empire.GetTDict()["Cruisers"].Unlocked;
            //if (!this.empire.canBuildFrigates && Ship_Game.ResourceManager.TechTree.ContainsKey("FrigateConstruction"))
            //    this.empire.canBuildFrigates = this.empire.GetTDict()["FrigateConstruction"].Unlocked;
            //if (!this.empire.canBuildCorvettes && Ship_Game.ResourceManager.TechTree.ContainsKey("HeavyFighterHull"))
            //    this.empire.canBuildCorvettes = this.empire.GetTDict()["HeavyFighterHull"].Unlocked;

            //Set ratio by class
            numBombers = numBombers * capBombers;
            numCarriers = numCarriers * capCarriers;
            numSupport = numSupport * capSupport;
            numTroops = numTroops * capTroops;
            if (this.empire.canBuildCapitals ) //&& TotalMilShipCount >10)
            {
                ratio_Fighters = 0f;
                ratio_Corvettes = .0f;
                ratio_Frigates = 10f;
                ratio_Cruisers = 5f;
                ratio_Capitals = 1f;
                if(this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                    //numBombers = numBombers  //(float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }
                
                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers =(float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.cruiser].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildCruisers) // && TotalMilShipCount > 10)
            {
                ratio_Fighters = 10f;
                ratio_Corvettes = 10f;
                ratio_Frigates = 5f;
                ratio_Cruisers = 1f;
                ratio_Capitals = 0f;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                   // numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }

                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.frigate].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildFrigates )//&& TotalMilShipCount > 10)
            {
                ratio_Fighters = 10f;
                ratio_Corvettes = 5f;
                ratio_Frigates = 1f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 5f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }

                if (this.empire.canBuildCarriers)
                {
                    ratio_Carriers = 1f;
                    //numCarriers = (float)Math.Ceiling((double)(numCarriers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
                ratio_Support = 1f;
            }
            else if (this.empire.canBuildCorvettes )//&& TotalMilShipCount > 10)
            {
                ratio_Fighters = 5f;
                ratio_Corvettes = 1f;
                ratio_Frigates = 0f;
                ratio_Cruisers = 0f;
                ratio_Capitals = 0f;
                ratio_Carriers=0;
                if (this.empire.canBuildBombers)
                {
                    ratio_Bombers = 1f;
                    //numBombers = (float)Math.Ceiling((double)(numBombers / ResourceManager.ShipRoles[ShipData.RoleName.corvette].Upkeep));
                }
            }
            else 
            {
                ratio_Bombers = 0f;
                ratio_Carriers = 0;
            }
            float totalRatio = ratio_Fighters + ratio_Corvettes + ratio_Frigates + ratio_Cruisers + ratio_Capitals + ratio_Bombers + ratio_Support +ratio_Carriers;
            bool atwar = (this.empire.AllRelations.Where(war => war.Value.AtWar).Count() > 0);
             
            if (TotalMilShipCount <= 0)
                totalRatio = 1;
            if (TotalUpkeep == 0)
                TotalUpkeep = 1;
            ratio_Bombers += this.toughnuts * .2f;
            float goal = Capacity / TotalUpkeep;// / TotalMilShipCount);
            float adjustedRatio = TotalMilShipCount / totalRatio;
            if (adjustedRatio == 0)
                adjustedRatio = 10;
            float DesiredFighters = (float)Math.Ceiling((double)(adjustedRatio * ratio_Fighters * goal));
            float DesiredCorvettes = (float)Math.Ceiling((double)(adjustedRatio * ratio_Corvettes * goal));
            float DesiredFrigates = (float)Math.Ceiling((double)(adjustedRatio * ratio_Frigates * goal));
            float DesiredCruisers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Cruisers * goal));
            float DesiredCapitals = (float)Math.Ceiling((double)(adjustedRatio * ratio_Capitals * goal));
            float DesiredCarriers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Carriers * goal));  //this.empire.canBuildCarriers ? (DesiredCapitals + DesiredCruisers) / 4f : 0;
            float DesiredBombers = (float)Math.Ceiling((double)(adjustedRatio * ratio_Bombers * goal));
            float DesiredSupport = (float)Math.Ceiling((double)(adjustedRatio * ratio_Support * goal));
            float DesiredTroops = 0;
            
            //if(this.empire.canBuildBombers)
            //{
            //    DesiredBombers = TotalMilShipCount / 15f;
                
            //}
            if(this.empire.canBuildTroopShips)
            {
                DesiredTroops = (float)Math.Ceiling((double)(atwar ? TotalMilShipCount / 10f : TotalMilShipCount / 30f));
            }
#if DEBUG
            //Log.Info("Build Ratios for: " + this.empire.data.PortraitName);
            //Log.Info("fighters: " + DesiredFighters + " / " + numFighters);
            //Log.Info("corvettes: " + DesiredCorvettes + " / " + numCorvettes);
            //Log.Info("Frigates: " + DesiredFrigates + " / " + numFrigates);
            //Log.Info("Cruisers: " + DesiredCruisers + " / " + numCruisers);
            //Log.Info("Capitals: " + DesiredCapitals + " / " + numCapitals);
            //Log.Info("Carriers: " + DesiredCarriers + " / " + numCarriers);
            //Log.Info("Bombers: " + DesiredBombers + " / " + numBombers);
            //Log.Info("TroopsHips: " + DesiredTroops + " / " + numTroops);
            //Log.Info("Capacity: " + Capacity);
            //Log.Info("ShipGoals: " + this.empire.GetGSAI().numberOfShipGoals);

#endif
            //Scrap ships when overspending by class
            if (this.buildCapacity /(TotalUpkeep *.90f +1) <1)  //capScrapping prevent from scrapping too much
            #region MyRegion
            {
                if (numFighters > DesiredFighters    ||
                    numCorvettes > DesiredCorvettes  ||
                    numFrigates > DesiredFrigates    ||
                    numCruisers > DesiredCruisers    ||
                    numCarriers > DesiredCarriers    ||
                    numBombers > DesiredBombers      ||
                    numCapitals > DesiredCapitals    ||
                    numTroops > DesiredTroops)
                    {
                    foreach (Ship ship in this.empire.GetShips()
                        .Where(ship => !ship.InCombat && ship.inborders && ship.fleet == null && ship.AI.State != AIState.Scrap && ship.Mothership == null && ship.Active && ship.shipData.HullRole >= ShipData.RoleName.fighter && ship.GetMaintCost(this.empire) >0)
                        .OrderByDescending(defense => this.DefensiveCoordinator.DefensiveForcePool.Contains(defense))
                        .ThenBy(ship => ship.Level)
                        .ThenBy(ship => ship.BaseStrength)
                        )
                    {
                        if (numFighters > (DesiredFighters) && (ship.shipData.HullRole == ShipData.RoleName.fighter || ship.shipData.HullRole == ShipData.RoleName.scout))
                        {
                            numFighters--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCarriers > (DesiredCarriers) && (ship.GetHangars().Where(fighters => fighters.MaximumHangarShipSize > 0).Count() > 0 == true))
                        {
                            numCarriers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numTroops > (DesiredTroops) && (ship.HasTroopBay || ship.hasTransporter))
                        {
                            numTroops--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numBombers > (DesiredBombers) && (ship.BombBays.Count > 0))
                        {
                            numBombers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCorvettes > (DesiredCorvettes) && (ship.shipData.HullRole == ShipData.RoleName.corvette || ship.shipData.HullRole == ShipData.RoleName.gunboat))
                        {
                            numCorvettes--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numFrigates > (DesiredFrigates) && (ship.shipData.HullRole == ShipData.RoleName.frigate || ship.shipData.HullRole == ShipData.RoleName.destroyer))
                        {
                            numFrigates--;    
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCruisers > (DesiredCruisers) && ship.shipData.HullRole == ShipData.RoleName.cruiser)
                        {
                            numCruisers--;
                            ship.AI.OrderScrapShip();
                        }
                        else if (numCapitals > (DesiredCapitals) && (ship.shipData.HullRole == ShipData.RoleName.capital || ship.shipData.HullRole == ShipData.RoleName.carrier))
                        {
                            numCapitals--;    
                            ship.AI.OrderScrapShip();
                        }
                        
                        if (numFighters <= DesiredFighters
                            && numCorvettes <= DesiredCorvettes
                            && numFrigates <= DesiredFrigates
                            && numCruisers <= DesiredCruisers
                            && numCarriers <= DesiredCarriers
                            && numBombers <= DesiredBombers
                            && numCapitals <= DesiredCapitals
                            && numTroops <= DesiredTroops)
                            {
                                break;
                            }
                    }
                }
                if(Capacity <=0)
                return null;  //no money to build !
            }
            #endregion

            //Find ship to build
            bool ranA = false;
            
            if (RandomMath.RandomBetween(0f, 1f) < 0.5f)
                ranA = true;
            
            Array<Ship> PotentialShips = new Array<Ship>();
            Map<ShipData.RoleName, float> PickRoles = new Map<ShipData.RoleName, float>();
            this.empire.UpdateShipsWeCanBuild();
            string buildThis;

            bool destroyer = false;
            bool gunboats = false;
            bool carriers = false;
            foreach (KeyValuePair<string, bool> hull in this.empire.GetHDict())
            {
                if (!hull.Value)
                    continue;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.destroyer)
                    destroyer = true;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.gunboat)
                    gunboats = true;
                if (ResourceManager.HullsDict[hull.Key].Role == ShipData.RoleName.carrier)
                    carriers = true;
            }
            if (numTroops < DesiredTroops)
                PickRoles.Add(ShipData.RoleName.troop, numTroops / DesiredTroops);
            if (numFighters < DesiredFighters) {
                PickRoles.Add(ShipData.RoleName.fighter, numFighters / (DesiredFighters));
                //PickRoles.Add(ShipData.RoleName.scout, numFighters / (DesiredFighters + ranB)); //scouts are handeled somewhere else and generally are not good at anything
                }
            if (numCorvettes < DesiredCorvettes) {
                if (gunboats)
                {
                    if(ranA)
                    PickRoles.Add(ShipData.RoleName.gunboat, numCorvettes / (DesiredCorvettes ));
                    else
                    PickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (DesiredCorvettes ));
                }
                else
                    PickRoles.Add(ShipData.RoleName.corvette, numCorvettes / (DesiredCorvettes));
                }
            if (numBombers < DesiredBombers)
                PickRoles.Add(ShipData.RoleName.drone, numBombers / DesiredBombers);
            if (numFrigates < DesiredFrigates)
            {
                if (destroyer)
                {
                    if(ranA)
                    PickRoles.Add(ShipData.RoleName.frigate, numFrigates / (DesiredFrigates));
                    else
                    PickRoles.Add(ShipData.RoleName.destroyer, numFrigates / (DesiredFrigates));
                }
                else
                    PickRoles.Add(ShipData.RoleName.frigate, numFrigates / (DesiredFrigates));
            }
            if (numCruisers < DesiredCruisers)
                PickRoles.Add(ShipData.RoleName.cruiser, numCruisers / DesiredCruisers);
            if (numCapitals < DesiredCapitals) {
                if (carriers)
                {
                    if(ranA )
                    PickRoles.Add(ShipData.RoleName.carrier, numCapitals / (DesiredCapitals));
                    else
                    PickRoles.Add(ShipData.RoleName.capital, numCapitals / (DesiredCapitals));
                }
                else {
                    PickRoles.Add(ShipData.RoleName.capital, numCapitals / (DesiredCapitals ));
                }
                }
            if (numCarriers < DesiredCarriers)
                PickRoles.Add(ShipData.RoleName.prototype, numCarriers / DesiredCarriers);
            
            foreach (KeyValuePair<ShipData.RoleName, float> pick in PickRoles.OrderBy(val => val.Value))
                {
                    buildThis = this.PickFromCandidates(pick.Key, Capacity, PotentialShips);
                    if (!string.IsNullOrEmpty(buildThis))
                    {
                        //Log.Info("Chosen: " + buildThis);
                        //Log.Info("TroopsHips: " + DesiredTroops);
                        return buildThis;
                    }
                }
            //if(Empire.Universe.viewState == UniverseScreen.UnivScreenState.GalaxyView )
            //    Log.Info("Chosen: Nothing");
            this.nobuild = true;
            return null;  //Find nothing to build !
        }

        /*
        public string PickFromCandidates(ShipData.RoleName role, float Capacity, Array<Ship> PotentialShips)
        {            
            string name = "";
            Ship ship;
            int maxtech = 0;
            foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
            {
                //if (!((ResourceManager.ShipsDict[shipsWeCanBuild].Role == ShipData.RoleName.capital || ResourceManager.ShipsDict[shipsWeCanBuild].Role == ShipData.RoleName.carrier) && ResourceManager.ShipsDict[shipsWeCanBuild].BaseStrength > 0f && Capacity >= ResourceManager.ShipsDict[shipsWeCanBuild].GetMaintCost()) && !(ResourceManager.ShipsDict[shipsWeCanBuild].BaseCanWarp && (ResourceManager.ShipsDict[shipsWeCanBuild].PowerDraw * this.empire.data.FTLPowerDrainModifier >= ResourceManager.ShipsDict[shipsWeCanBuild].PowerFlowMax || ResourceManager.ShipsDict[shipsWeCanBuild].PowerStoreMax / (ResourceManager.ShipsDict[shipsWeCanBuild].PowerDraw * this.empire.data.FTLPowerDrainModifier - ResourceManager.ShipsDict[shipsWeCanBuild].PowerFlowMax) * ResourceManager.ShipsDict[shipsWeCanBuild].velocityMaximum > minimumWarpRange)))
                if (!ResourceManager.ShipsDict.TryGetValue(shipsWeCanBuild, out ship))
                    continue;

                if (ship.shipData.Role != role || Capacity <= ship.GetMaintCost(this.empire) || !shipIsGoodForGoals(ship))
                {
                    continue;
                }
                if (ship.shipData.techsNeeded.Count > maxtech)
                    maxtech = ship.shipData.techsNeeded.Count;
                PotentialShips.Add(ship);//   ResourceManager.ShipsDict[shipsWeCanBuild]);
            }
            float nearmax = maxtech * .5f;
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship3 in PotentialShips
                    orderby ship3.shipData.techsNeeded.Count >= nearmax descending,  ship3.BaseStrength   descending             
                    select ship3;
                float totalStrength = 0f;
                maxtech++;
                ////foreach (Ship ship1 in sortedList)
                ////{

                ////        totalStrength += ship1.BaseStrength * (ship1.shipData.techsNeeded.Count +1)/ maxtech;
                ////}
                int ran = RandomMath.InRange(3)-1;
                if (ran > sortedList.Count()-1)
                    ran = sortedList.Count()-1;
                float strcounter = 0f;
                name = sortedList.Skip(ran).First().Name;
                //////foreach (Ship ship2 in sortedList)
                //////{
                //////    //strcounter = strcounter + ship2.BaseStrength * (ship2.shipData.techsNeeded.Count+1) / maxtech;
                //////    //if (strcounter <= ran)
                //////    //{
                //////    //    continue;
                //////    //}

                //////    name = ship2.Name;

                //////}
            }
            if(string.IsNullOrEmpty(name))
                PotentialShips.Clear();
            return name;
        }
        */

        //fbedard: add TroopsShip(troop), Bomber(drone) and Carrier(prototype) roles
        public string PickFromCandidates(ShipData.RoleName role, float Capacity, Array<Ship> PotentialShips)
        {            
            string name = "";
            Ship ship;
            int maxtech = 0;
            //float upkeep;          //Not referenced in code, removing to save memory
            foreach (string shipsWeCanBuild in this.empire.ShipsWeCanBuild)
            {
                if (!ResourceManager.ShipsDict.TryGetValue(shipsWeCanBuild, out ship))
                    continue;
                bool bombs = false;
                bool hangars = false;
                bool troops = false;
                int bombcount = 0;
                int hangarcount = 0;

                foreach (ShipModule slot in ship.ModuleSlotList)
                {
                    if (slot.ModuleType == ShipModuleType.Bomb)
                    {
                        bombcount += slot.XSIZE * slot.YSIZE;
                        if (bombcount > ship.Size * .2)
                            bombs = true;
                    }
                    if (slot.MaximumHangarShipSize > 0)
                    {
                        hangarcount += slot.YSIZE * slot.XSIZE;
                        if (hangarcount > ship.Size * .2)
                            hangars = true;
                    }
                    if (slot.IsTroopBay || slot.TransporterRange > 0)
                        troops = true;

                }


                //    upkeep = ship.GetMaintCost(this.empire); //this automatically calls realistic maintenance cost if needed. 
                //Capacity < upkeep ||
                if (role == ShipData.RoleName.drone || role == ShipData.RoleName.troop)
                {
                    if (!this.NonCombatshipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                        continue;
                }
                else
                    if (!shipIsGoodForGoals(ship) || ship.shipData.HullRole < ShipData.RoleName.freighter)
                        continue;
                if (role == ShipData.RoleName.troop && !troops)
                    continue;
                else if (role == ShipData.RoleName.drone && !bombs)
                    continue;
                else if (role == ShipData.RoleName.prototype && !hangars)
                    continue;
                else if (role != ship.shipData.HullRole && role == ShipData.RoleName.prototype && role != ShipData.RoleName.drone && role != ShipData.RoleName.troop)
                    continue;
                if (ship.shipData.techsNeeded.Count > maxtech)
                    maxtech = ship.shipData.techsNeeded.Count;
                PotentialShips.Add(ship);
            }
            float nearmax = maxtech * .5f;
            //Log.Info("number of candidates : " + PotentialShips.Count + " _ trying for : " + role);
            if (PotentialShips.Count > 0)
            {
                IOrderedEnumerable<Ship> sortedList =
                    from ship3 in PotentialShips
                    orderby ship3.shipData.techsNeeded.Count >= nearmax descending,  ship3.BaseStrength descending             
                    select ship3;
                maxtech++;
                int ran = (int)(sortedList.Count()*.5f);
                ran = RandomMath.InRange(ran);
                if (ran > sortedList.Count())
                    ran = sortedList.Count();
                ship = sortedList.Skip(ran).First();
                name = ship.Name;
            if(Empire.Universe.showdebugwindow)
                Log.Info("Chosen Role: {0}  Chosen Hull: {1}  Strength: {2}", 
                    ship.GetShipData().Role, ship.GetShipData().Hull, ship.BaseStrength);            
            }
            else
            {
            #if false
                string ships = "Ships empire has: ";
                foreach (string known in this.empire.ShipsWeCanBuild)
                {
                    ships += known + " : ";
                }
                Log.Info(ships);
            #endif
            }
            PotentialShips.Clear();
            return name;
        }

        //Added by McShooterz: used for AI to get defensive structures to build around planets
        public string GetDefenceSatellite()
        {
            Array<Ship> PotentialSatellites = new Array<Ship>();
            foreach (string platform in this.empire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (platform != "Subspace Projector" && orbitalDefense.shipData.Role == ShipData.RoleName.platform && orbitalDefense.BaseStrength > 0)
                    PotentialSatellites.Add(orbitalDefense);
            }
            if (PotentialSatellites.Count() == 0)
                return "";
            int index = RandomMath.InRange(PotentialSatellites.Count());
            return PotentialSatellites[index].Name;
        }
        public string GetStarBase()
        {
            Array<Ship> PotentialSatellites = new Array<Ship>();
            foreach (string platform in this.empire.structuresWeCanBuild)
            {
                Ship orbitalDefense = ResourceManager.ShipsDict[platform];
                if (orbitalDefense.shipData.Role == ShipData.RoleName.station && (orbitalDefense.shipData.IsOrbitalDefense || !orbitalDefense.shipData.IsShipyard))
                {
                    //if (orbitalDefense.BaseStrength == 0 && orbitalDefense.GetStrength() == 0)
                    //    continue;
                    PotentialSatellites.Add(orbitalDefense);
                }
            }
            if (PotentialSatellites.Count() == 0)
                return "";
            int index = RandomMath.InRange((int)(PotentialSatellites.Count()*.5f));
            return PotentialSatellites.OrderByDescending(tech=> tech.shipData.TechScore).ThenByDescending(stre=>stre.shipData.BaseStrength).Skip(index).FirstOrDefault().Name;
        }

        public bool shipIsGoodForGoals(Ship ship)
        {
            if (ship.BaseStrength > 0f && ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp  && ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                && ship.PowerStoreMax / (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > minimumWarpRange))
                return true;
            return false;
        }
        public bool NonCombatshipIsGoodForGoals(Ship ship)
        {
            if ( ship.shipData.ShipStyle != "Platforms" && !ship.shipData.CarrierShip && ship.BaseCanWarp && ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier <= ship.PowerFlowMax
                || (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier > ship.PowerFlowMax
                && ship.PowerStoreMax / (ship.ModulePowerDraw * this.empire.data.FTLPowerDrainModifier - ship.PowerFlowMax) * ship.velocityMaximum > minimumWarpRange))
                return true;
            return false;
        }

        private float GetDistance(Empire e)
        {
            if (e.GetPlanets().Count == 0 || this.empire.GetPlanets().Count == 0)
            {
                return 0f;
            }
            Vector2 AvgPos = new Vector2();
            foreach (Planet p in e.GetPlanets())
            {
                AvgPos = AvgPos + p.Position;
            }
            AvgPos = AvgPos / (float)e.GetPlanets().Count;
            Vector2 Ouravg = new Vector2();
            foreach (Planet p in this.empire.GetPlanets())
            {
                Ouravg = Ouravg + p.Position;
            }
            Ouravg = Ouravg / (float)this.empire.GetPlanets().Count;
            return Vector2.Distance(AvgPos, Ouravg);
        }

        public float GetDistanceFromOurAO(Planet p)
        {
            IOrderedEnumerable<AO> sortedList = 
                from area in this.AreasOfOperations
                orderby Vector2.Distance(p.Position, area.Position)
                select area;
            if (sortedList.Count<AO>() == 0)
            {
                return 0f;
            }
            return Vector2.Distance(p.Position, sortedList.First<AO>().Position);
        }
        private float GetDistanceFromOurAO(Vector2 p)
        {
            IOrderedEnumerable<AO> sortedList =
                from area in this.AreasOfOperations
                orderby Vector2.Distance(p, area.Position)
                select area;
            if (sortedList.Count<AO>() == 0)
            {
                return 0f;
            }
            return Vector2.Distance(p, sortedList.First<AO>().Position);
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            var relations = empire.GetRelations(warDeclarant);
            relations.AtWar    = true;
            relations.FedQuest = null;
            relations.Posture  = Posture.Hostile;
            relations.ActiveWar = new War(empire, warDeclarant, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (Empire.Universe.PlayerEmpire != empire)
            {
                if (empire.data.DiplomaticPersonality.Name == "Pacifist")
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0 ? WarType.DefensiveWar : WarType.BorderConflict;
                }
            }
            if (relations.Trust > 0f)
                relations.Trust = 0f;
            relations.Treaty_Alliance    = false;
            relations.Treaty_NAPact      = false;
            relations.Treaty_OpenBorders = false;
            relations.Treaty_Trade       = false;
            relations.Treaty_Peace       = false;
        }

        public void InitialzeAOsFromSave(UniverseData data)
        {
            foreach (AO area in AreasOfOperations)
            {
                area.InitFromSave(data, empire);                
            }
        }

        //addedby gremlin manageAOs
        public void ManageAOs()
        {
            //Vector2 empireCenter =this.empire.GetWeightedCenter();
            
            Array<AO> aOs = new Array<AO>();
            float empireStr = this.empire.currentMilitaryStrength; // / (this.AreasOfOperations.Count*2.5f+1);
            foreach (AO areasOfOperation in this.AreasOfOperations)
            {
                
                if (areasOfOperation.GetPlanet().Owner != empire)
                {
                    aOs.Add(areasOfOperation);
                    continue;
                }                
                areasOfOperation.ThreatLevel = (int)ThreatMatrix.PingRadarStrengthLargestCluster(areasOfOperation.Position, areasOfOperation.Radius, empire);

                int min = (int)(areasOfOperation.GetOffensiveForcePool().Sum(str => str.BaseStrength) *.5f);
                if (areasOfOperation.ThreatLevel < min)
                    areasOfOperation.ThreatLevel = min;
                
            }
            foreach (AO aO1 in aOs)
            {
                this.AreasOfOperations.Remove(aO1);
            }
            Array<Planet> planets = new Array<Planet>();
            foreach (Planet planet1 in this.empire.GetPlanets())
            {
                if (planet1.GetMaxProductionPotential() <= 5f || !planet1.HasShipyard)
                {
                    continue;
                }
                bool flag = false;
                foreach (AO areasOfOperation1 in this.AreasOfOperations)
                {
                    if (areasOfOperation1.GetPlanet() != planet1)
                    {
                        continue;
                    }
                    flag = true;
                    break;
                }
                if (flag)
                {
                    continue;
                }
                planets.Add(planet1);
            }
            if (planets.Count == 0)
            {
                return;
            }
            IOrderedEnumerable<Planet> maxProductionPotential =
                from planet in planets
                orderby planet.GetMaxProductionPotential() descending
                select planet;
            
            foreach (Planet planet2 in maxProductionPotential)
            {
                float aoSize = 0;
                foreach (SolarSystem system in planet2.system.FiveClosestSystems)
                {
                    //if (system.OwnerList.Contains(this.empire))
                    //    continue;
                    if (aoSize < Vector2.Distance(planet2.Position, system.Position))
                        aoSize = Vector2.Distance(planet2.Position, system.Position);
                }
                float aomax = Empire.Universe.Size.X * .2f;
                if (aoSize > aomax)
                    aoSize = aomax;
                bool flag1 = true;
                foreach (AO areasOfOperation2 in this.AreasOfOperations)
                {

                    if (Vector2.Distance(areasOfOperation2.GetPlanet().Position, planet2.Position) >= aoSize)
                        continue;
                    flag1 = false;
                    break;
                }
                if (!flag1)
                {
                    continue;
                }

                AO aO2 = new AO(planet2, aoSize);
                this.AreasOfOperations.Add(aO2);
            }
        }

        public void OfferPeace(KeyValuePair<Empire, Relationship> relationship, string whichPeace)
        {
            Offer offerPeace = new Offer()
            {
                PeaceTreaty = true,
                AcceptDL = "OFFERPEACE_ACCEPTED",
                RejectDL = "OFFERPEACE_REJECTED"
            };
            Relationship value = relationship.Value;
            offerPeace.ValueToModify = new Ref<bool>(() => false, x => value.SetImperialistWar());
            string dialogue = whichPeace;
            if (relationship.Key != Empire.Universe.PlayerEmpire)
            {
                Offer ourOffer = new Offer { PeaceTreaty = true };
                relationship.Key.GetGSAI().AnalyzeOffer(ourOffer, offerPeace, empire, Offer.Attitude.Respectful);
                return;
            }
            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, dialogue, new Offer(), offerPeace));
        }

        private void RunAgentManager()
        {
            float spyincomemodifer = .01f; // ((int)Empire.Universe.GameDifficulty + 1) * .0033f;
            int income = (int)((this.empire.Money * spyincomemodifer) * (1 - this.empire.data.TaxRate));//-this.empire.GrossTaxes*5 );//* .2f);
            if (income < 0 || this.empire.data.SpyBudget > this.empire.Money * .75f)
                income = 0;
            
            this.spyBudget = income +this.empire.data.SpyBudget;
            this.empire.Money -= income;
            
            string name = empire.data.DiplomaticPersonality.Name;
            if (spyBudget > 50 && name != null)
            {
                switch (name)
                {
                    case "Cunning":    DoCunningAgentManager(); break;
                    case "Ruthless":   DoAggRuthAgentManager(); break;
                    case "Aggressive": DoAggRuthAgentManager(); break;
                    case "Honorable":  DoHonPacAgentManager();  break;
                    case "Xenophobic": DoCunningAgentManager(); break;
                    case "Pacifist":   DoHonPacAgentManager();  break;
                    default:           DoCunningAgentManager(); break;
                }
            }
            this.empire.data.SpyBudget = this.spyBudget;
            this.spyBudget = 0;
        }

        private void RunDiplomaticPlanner()
        {
            string name = empire.data.DiplomaticPersonality.Name;
            if (name != null)
            {
                switch (name)
                {
                    case "Pacifist":   DoPacifistRelations();   break;
                    case "Aggressive": DoAggressiveRelations(); break;
                    case "Honorable":  DoHonorableRelations();  break;
                    case "Xenophobic": DoXenophobicRelations(); break;
                    case "Ruthless":   DoRuthlessRelations();   break;
                    case "Cunning":    DoCunningRelations();    break;
                }
            }
            foreach (KeyValuePair<Empire, Relationship> relationship in empire.AllRelations)
            {
                if (!relationship.Key.isFaction && !empire.isFaction && !relationship.Key.data.Defeated)
                    RunEventChecker(relationship);
            }
        }

        //added by gremlin Economic planner
        private void RunEconomicPlanner()
        {
            float money = this.empire.Money;
            money= money<1?1:money;            
            //gremlin: Use self adjusting tax rate based on wanted treasury of 10(1 full year) of total income.

            float treasuryGoal = this.empire.GrossTaxes + this.empire.OtherIncome + this.empire.TradeMoneyAddedThisTurn + this.empire.data.FlatMoneyBonus;  //mmore savings than GDP 
            treasuryGoal *= (this.empire.data.treasuryGoal *100);
            treasuryGoal = treasuryGoal <= 1 ? 1: treasuryGoal;
            float zero = this.FindTaxRateToReturnAmount(1);
            float tempTax =this.FindTaxRateToReturnAmount( treasuryGoal   *(1-(money/treasuryGoal))    );
            if (tempTax - this.empire.data.TaxRate > .02f)
                this.empire.data.TaxRate += .02f;
            //else if (tempTax - this.empire.data.TaxRate < -.02f)
            //    this.empire.data.TaxRate -= .02f;
            else
                this.empire.data.TaxRate = tempTax;
            //if (!this.empire.isPlayer)  // The AI will NOT build defense platforms ?
            //    return;
            float DefBudget = this.empire.Money * this.empire.GetPlanets().Count * .001f * (1 - this.empire.data.TaxRate);
            if (DefBudget < 0 || this.empire.data.DefenseBudget > this.empire.Money * .01 * this.empire.GetPlanets().Count)
                DefBudget = 0;
            this.empire.Money -= DefBudget;
            this.empire.data.DefenseBudget += DefBudget;
            return;
        }

        public void RunEventChecker(KeyValuePair<Empire, Relationship> Them)
        {
            if (empire == Empire.Universe.PlayerEmpire || empire.isFaction || !Them.Value.Known)
                return;

            Array<Planet> OurTargetPlanets = new Array<Planet>();
            Array<Planet> TheirTargetPlanets = new Array<Planet>();
            foreach (Goal g in this.Goals)
            {
                if (g.type == GoalType.Colonize)
                    OurTargetPlanets.Add(g.GetMarkedPlanet());
            }
            foreach (Goal g in Them.Key.GetGSAI().Goals)
            {
                if (g.type == GoalType.Colonize)
                    TheirTargetPlanets.Add(g.GetMarkedPlanet());
            }
            SolarSystem sharedSystem = null;
            Them.Key.GetShips().ForEach(ship => //foreach (Ship ship in Them.Key.GetShips())
            {
                if (ship.AI.State != AIState.Colonize || ship.AI.ColonizeTarget == null)
                {
                    return;
                }
                TheirTargetPlanets.Add(ship.AI.ColonizeTarget);
            }, false, false, false);

            foreach (Planet p in OurTargetPlanets)
            {
                bool matchFound = false;
                foreach (Planet other in TheirTargetPlanets)
                {
                    if (p == null || other == null || p.system != other.system)
                    {
                        continue;
                    }
                    sharedSystem = p.system;
                    matchFound = true;
                    break;
                }
                if (matchFound)
                    break;
            }

            if (sharedSystem != null && !Them.Value.AtWar && !Them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
            {
                bool TheyAreThereAlready = false;
                foreach (Planet p in sharedSystem.PlanetList)
                {
                    if (p.Owner == null || p.Owner != Empire.Universe.PlayerEmpire)
                    {
                        continue;
                    }
                    TheyAreThereAlready = true;
                }
                if (!TheyAreThereAlready)
                {
                    if (Them.Key == Empire.Universe.PlayerEmpire)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, empire, Empire.Universe.PlayerEmpire, "Claim System", sharedSystem));
                    }
                    Them.Value.WarnedSystemsList.Add(sharedSystem.guid);
                }
            }
        }

        
        private void RunExpansionPlanner()
        {
            int numColonyGoals = 0;
            this.desired_ColonyGoals = ((int)Empire.Universe.GameDifficulty+3) ;
            foreach (Goal g in this.Goals)
            {
                if (g.type != GoalType.Colonize)
                {
                    continue;
                }
                //added by Gremlin: Colony expansion changes
                Planet markedPlanet = g.GetMarkedPlanet();
                if (markedPlanet != null && markedPlanet.ParentSystem != null)
                {
                    if (markedPlanet.ParentSystem.ShipList.Any(ship => ship.loyalty != null && ship.loyalty.isFaction))
                        --numColonyGoals;
                    ++numColonyGoals;
                }
            }
            if (numColonyGoals < this.desired_ColonyGoals + (this.empire.data.EconomicPersonality != null ? this.empire.data.EconomicPersonality.ColonyGoalsPlus : 0) )//
            {
                Planet toMark = null;
                float DistanceInJumps = 0;
                Vector2 WeightedCenter = new Vector2();
                int numPlanets = 0;
                foreach (Planet p in this.empire.GetPlanets())
                {
                    for (int i = 0; (float)i < p.Population / 1000f; i++)
                    {
                        WeightedCenter = WeightedCenter + p.Position;
                        numPlanets++;
                    }
                }
                WeightedCenter = WeightedCenter / (float)numPlanets;
                Array<Goal.PlanetRanker> ranker = new Array<Goal.PlanetRanker>();
                Array<Goal.PlanetRanker> allPlanetsRanker = new Array<Goal.PlanetRanker>();
                foreach (SolarSystem s in UniverseScreen.SolarSystemList)
                {
                    //added by gremlin make non offensive races act like it.
                    bool systemOK = true;
                    if (!this.empire.isFaction && this.empire.data != null && this.empire.data.DiplomaticPersonality != null 
                        && !(
                        (this.empire.AllRelations.Where(war => war.Value.AtWar).Count() > 0 && this.empire.data.DiplomaticPersonality.Name != "Honorable") 
                        || this.empire.data.DiplomaticPersonality.Name == "Agressive" 
                        || this.empire.data.DiplomaticPersonality.Name == "Ruthless" 
                        || this.empire.data.DiplomaticPersonality.Name == "Cunning")
                        )
                    {
                        foreach (Empire enemy in s.OwnerList)
                        {
                            if (enemy != this.empire && !enemy.isFaction && !this.empire.GetRelations(enemy).Treaty_Alliance)
                            {

                                systemOK = false;

                                break;
                            }
                        }
                    }
                    if (!systemOK) continue;
                    if (!s.ExploredDict[this.empire])
                    {

                        continue;
                    }
                    float str =this.ThreatMatrix.PingRadarStr(s.Position, 300000f, this.empire,true);
                    if (str > 0f)
                    {
                        //Log.Info("Colonization ignored in " + s.Name + " Incorrect pin str :" +str.ToString() );
                        continue;
                    }
                    foreach (Planet planetList in s.PlanetList)
                    {

                        bool ok = true;
                        foreach (Goal g in this.Goals)
                        {
                            if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != planetList)
                            {
                                continue;
                            }
                            ok = false;
                        }
                        if (!ok)
                        {
                            continue;
                        }
                        str = this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire);
                        if (str > 0)
                            continue;
                        IOrderedEnumerable<AO> sorted =
                            from ao in this.empire.GetGSAI().AreasOfOperations
                            orderby Vector2.Distance(planetList.Position, ao.Position)
                            select ao;
                        if (sorted.Count<AO>() > 0)
                        {
                            AO ClosestAO = sorted.First<AO>();
                            if (Vector2.Distance(planetList.Position, ClosestAO.Position) > ClosestAO.Radius * 2f)
                            {
                                continue;
                            }
                        }
                        int commodities = 0;
                        //Added by gremlin adding in commodities
                        foreach (Building commodity in planetList.BuildingList)
                        {
                            if (!commodity.IsCommodity) continue;
                            commodities += 1;
                        }


                        if (planetList.ExploredDict[this.empire] 
                            && planetList .habitable 
                            && planetList.Owner == null)
                        {
                            
                            Goal.PlanetRanker r2 = new Goal.PlanetRanker()
                            {
                                Distance = Vector2.Distance(WeightedCenter, planetList.Position)
                            };
                            DistanceInJumps = r2.Distance / 400000f;
                            if (DistanceInJumps < 1f)
                            {
                                DistanceInJumps = 1f;
                            }
                            r2.planet = planetList;
//Cyberbernetic planet picker
                            if (this.empire.data.Traits.Cybernetic != 0)
                            {

                                r2.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                            }
                            else
                            {
                                r2.PV = (commodities + planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                            }

                            if (commodities > 0)
                                ranker.Add(r2);

                            if (planetList.Type == "Barren"
                                && commodities > 0
                                    || this.empire.GetBDict()["Biospheres"]
                                    || this.empire.data.Traits.Cybernetic != 0
                            )
                            {
                                ranker.Add(r2);
                            }
                            else if (planetList.Type != "Barren"
                                && commodities > 0
                                    || ((double)planetList.Fertility >= .5f || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f))
                                    || (this.empire.GetTDict()["Aeroponics"].Unlocked))
                                    //|| (this.empire.data.Traits.Cybernetic != 0 && this.empire.GetBDict()["Biospheres"]))
                            {
                                ranker.Add(r2);
                            }
                            else if (planetList.Type != "Barren")
                            {
                                if (this.empire.data.Traits.Cybernetic == 0)
                                    foreach (Planet food in this.empire.GetPlanets())
                                    {
                                        if (food.FoodHere > food.MAX_STORAGE * .7f && food.fs == Planet.GoodState.EXPORT)
                                        {
                                            ranker.Add(r2);
                                            break;
                                        }
                                    }
                                else
                                {

                                    if (planetList.MineralRichness < .5f)
                                    {
                                        
                                        foreach (Planet food in this.empire.GetPlanets())
                                        {
                                            if (food.ProductionHere > food.MAX_STORAGE * .7f || food.ps == Planet.GoodState.EXPORT)
                                            {
                                                ranker.Add(r2);
                                                break;
                                            }
                                        }
                                        
                                    }
                                    else
                                    {
                                        ranker.Add(r2);
                                    }

                                    
                                }
                            }


                        }
                        if (!planetList.ExploredDict[this.empire] 
                            || !planetList.habitable 
                            || planetList.Owner == this.empire 
                            || this.empire == EmpireManager.Player 
                                && this.ThreatMatrix.PingRadarStr(planetList.Position, 50000f, this.empire) > 0f)
                        {
                            continue;
                        }
                        Goal.PlanetRanker r = new Goal.PlanetRanker()
                        {
                            Distance = Vector2.Distance(WeightedCenter, planetList.Position)
                        };
                        DistanceInJumps = r.Distance / 400000f;
                        if (DistanceInJumps < 1f)
                        {
                            DistanceInJumps = 1f;
                        }
                        r.planet = planetList;
                        if (this.empire.data.Traits.Cybernetic != 0)
                        {
                            r.PV = (commodities + planetList.MineralRichness + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                        }
                        else
                        {
                            r.PV = (commodities + planetList.MineralRichness + planetList.Fertility + planetList.MaxPopulation / 1000f) / DistanceInJumps;
                        }
                        //if (planetList.Type == "Barren" && (commodities > 0 || this.empire.GetTDict()["Biospheres"].Unlocked || (this.empire.data.Traits.Cybernetic != 0 && (double)planetList.MineralRichness >= .5f)))
                        //if (!(planetList.Type == "Barren") || !this.empire.GetTDict()["Biospheres"].Unlocked)
                        if (planetList.Type == "Barren" 
                            && commodities > 0 
                            || this.empire.GetBDict()["Biospheres"] 
                            || this.empire.data.Traits.Cybernetic != 0)
                                   
                        {
                            if (!(planetList.Type != "Barren") 
                                || planetList.Fertility < .5f 
                                && !this.empire.GetTDict()["Aeroponics"].Unlocked 
                                && this.empire.data.Traits.Cybernetic == 0)
                            {

                                foreach (Planet food in this.empire.GetPlanets())
                                {
                                    if (food.FoodHere > food.MAX_STORAGE * .9f && food.fs == Planet.GoodState.EXPORT)
                                    {
                                        allPlanetsRanker.Add(r);
                                        break;
                                    }
                                }

                                continue;
                            }

                            allPlanetsRanker.Add(r);

                        }
                        else
                        {
                            allPlanetsRanker.Add(r);
                        }
                    }
                }
                if (ranker.Count > 0)
                {
                    Goal.PlanetRanker winner = new Goal.PlanetRanker();
                    float highest = 0f;
                    foreach (Goal.PlanetRanker pr in ranker)
                    {
                        if (pr.PV <= highest)
                        {
                            continue;
                        }
                        bool ok = true;
                        foreach (Goal g in this.Goals)
                        {
                            if (g.GetMarkedPlanet() == null || g.GetMarkedPlanet() != pr.planet)
                            {
                                if (!g.Held || g.GetMarkedPlanet() == null || g.GetMarkedPlanet().system != pr.planet.system)
                                {
                                    continue;
                                }
                                ok = false;
                                break;
                            }
                            else
                            {
                                ok = false;
                                break;
                            }
                        }
                        if (!ok)
                        {
                            continue;
                        }
                        winner = pr;
                        highest = pr.PV;
                    }
                    toMark = winner.planet;
                }
                if (allPlanetsRanker.Count > 0)
                {
                    DesiredPlanets.Clear();
                    IOrderedEnumerable<Goal.PlanetRanker> sortedList =
                        from ran in allPlanetsRanker
                        orderby ran.PV descending
                        select ran;
                    foreach (Goal.PlanetRanker planetRanker in sortedList)
                        DesiredPlanets.Add(planetRanker.planet);
                }
                if (toMark != null)
                {
                    bool ok = true;
                    foreach (Goal g in this.Goals)
                    {
                        if (g.type != GoalType.Colonize || g.GetMarkedPlanet() != toMark)
                        {
                            continue;
                        }
                        ok = false;
                    }
                    if (ok)
                    {
                        Goal cgoal = new Goal(toMark, this.empire)
                        {
                            GoalName = "MarkForColonization"
                        };
                        this.Goals.Add(cgoal);
                        numColonyGoals++;
                    }
                }
            }
        }

        private void RunGroundPlanner()
        {
            if (DefensiveCoordinator.UniverseWants > .8)
                return;
            float totalideal = 0;
            float totalwanted = 0;

            IEnumerable<Troop> troopTemplates = ResourceManager.GetTroopTemplates().Where(t => empire.WeCanBuildTroop(t.Name)).OrderBy(t => t.Cost);
            Troop lowCostTroop = troopTemplates.FirstOrDefault();
            Troop highCostTroop = troopTemplates.LastOrDefault();
            Troop troop = highCostTroop;

            foreach (SolarSystem system in this.empire.GetOwnedSystems())
            {

                SystemCommander defenseSystem = this.DefensiveCoordinator.DefenseDict[system];
                //int planetcount = system.PlanetList.Where(planet => planet.Owner == empire).Count();
                //planetcount = planetcount == 0 ? 1 : planetcount;

                if (defenseSystem.TroopStrengthNeeded <= 0)
                {
                    continue;
                }
                totalwanted += defenseSystem.TroopStrengthNeeded;// >0 ?defenseSystem.TroopStrengthNeeded : 1;
                totalideal += defenseSystem.IdealTroopCount;// >0 ? defenseSystem.IdealTroopStr : 1;
            }
            if (totalwanted / totalideal > .5f)
            {
                troop = lowCostTroop;
            }
            if (totalwanted / totalideal <= .1f)
                return;
            Planet targetBuild = this.empire.GetPlanets()
                .Where(planet => planet.AllowInfantry && planet.colonyType != Planet.ColonyType.Research
                    && planet.GetMaxProductionPotential() > 5 
                    && (planet.ProductionHere) -2*(planet.ConstructionQueue.Where(goal => goal.Goal != null
                        && goal.Goal.type == GoalType.BuildTroop).Sum(cost => cost.Cost)) > 0//10 turns to build curremt troops in queue
                        
                        )
                        .OrderBy(noshipyard => !noshipyard.HasShipyard)
                        .ThenByDescending(build => build.GrossProductionPerTurn).FirstOrDefault();
            if (targetBuild == null)
                return;


            Goal g = new Goal(troop, this.empire, targetBuild);
            this.Goals.Add(g);

        }

        private void RunGroundPlanner2()
        {
            //float requiredStrength =  (float)(this.empire.GetPlanets().Count * 50);
            float requiredStrength = 0;//(float)(this.empire.GetPlanets().Sum(planet =>planet.GetPotentialGroundTroops(this.empire)));
            float developmentlevel = (float)this.empire.GetPlanets().Average(planet => planet.developmentLevel) *.5f;
            //requiredStrength *= developmentlevel;
            //requiredStrength *= 10;
            foreach(KeyValuePair<SolarSystem,SystemCommander> defensiveStrength in this.DefensiveCoordinator.DefenseDict)
            {
                requiredStrength += defensiveStrength.Value.IdealTroopCount ;
            }
            

            requiredStrength = requiredStrength + requiredStrength * this.empire.data.Traits.GroundCombatModifier;

            if (Empire.Universe.GameDifficulty < UniverseData.GameDifficulty.Hard)
            {
                requiredStrength = requiredStrength * .5f;
            }
            if (Empire.Universe.GameDifficulty == UniverseData.GameDifficulty.Easy)
            {
                requiredStrength = requiredStrength * .5f;
            }
            this.numberTroopGoals = this.AreasOfOperations.Count * 2;
            float currentStrength = 0f;
            foreach (Planet p in this.empire.GetPlanets())
            {
                
                foreach (Troop t in p.TroopsHere)
                {
                    if (t.GetOwner() == null || t.GetOwner() != this.empire)
                    {
                        continue;
                    }
                    currentStrength = currentStrength + (float)t.Strength;
                }
            }
            for (int i = 0; i < this.empire.GetShips().Count; i++)
            {
                Ship ship = this.empire.GetShips()[i];
                if (ship != null)
                {
                    for (int j = 0; j < ship.TroopList.Count; j++)
                    {
                        Troop t = ship.TroopList[j];
                        if (t != null)
                        {
                            currentStrength = currentStrength + (float)t.Strength;
                        }
                    }
                }
            }
            int currentgoals = 0;
            int goalStrength =0;
            for (int i = 0; i < this.Goals.Count; i++)
            {
                Goal g = this.Goals[i];
                if (g != null && g.GoalName == "Build Troop")
                {
                    currentgoals++;
                    goalStrength += (int)ResourceManager.GetTroopTemplate(g.ToBuildUID).Strength;
                }
            }
            int wantedStrength = (int)(requiredStrength - (currentStrength + goalStrength));
            //if (currentStrength < requiredStrength || currentgoals < this.numberTroopGoals)
            //if(wantedStrength >0 || )
            {
                Array<Planet> Potentials = new Array<Planet>();
                float totalProduction = 0f;
                foreach (AO area in this.AreasOfOperations)
                {
                    if (!area.GetPlanet().AllowInfantry)
                    {
                        continue;
                    }
                    Potentials.Add(area.GetPlanet());
                    totalProduction = totalProduction + area.GetPlanet().GetNetProductionPerTurn();
                }
                if (Potentials.Count > 0)
                {

                    //for (int i = 0; i < (int)wantedStrength * .1f; i++)
                    while(wantedStrength >0 &&  currentgoals <= this.empire.GetPlanets().Count*3)//  this.Goals.Where(goal=>goal.type == GoalType.BuildTroop).Count() <= Potentials.Count*5)

                    {
                        Planet selectedPlanet = null;
       
                        foreach (Planet p in Potentials.OrderByDescending(queue=> queue.Owner.GetGSAI().Goals.Where(goals=> goals.GetPlanetWhereBuilding() ==queue).Count()).ThenBy(production=> production.GetNetProductionPerTurn()))
                        {

                            //float random = RandomMath.RandomBetween(0f, totalProduction);
 
                            //if (random <= prodPick || random >= prodPick + p.GetNetProductionPerTurn())
                            //{
                            //    prodPick = prodPick + p.GetNetProductionPerTurn();
                            //}
                            //else
                            //{
                            //    selectedPlanet = p;
                            //}
                            selectedPlanet = p;
                            if (selectedPlanet != null)
                            {
                                Array<string> PotentialTroops = new Array<string>();
                                foreach (string troopType in ResourceManager.TroopTypes)
                                {
                                    if (!this.empire.WeCanBuildTroop(troopType))
                                    {
                                        continue;
                                    }
                                    PotentialTroops.Add(troopType);
                                }
                                if (PotentialTroops.Count > 0)
                                {
                                    int ran = (int)RandomMath.RandomBetween(0f, (float)PotentialTroops.Count + 0.75f);
                                    if (ran > PotentialTroops.Count - 1)
                                    {
                                        ran = PotentialTroops.Count - 1;
                                    }
                                    if (ran < 0)
                                    {
                                        ran = 0;
                                    }
                                    Troop troop = ResourceManager.GetTroopTemplate(PotentialTroops[ran]);
                                    wantedStrength -= (int)troop.Strength;
                                    Goal g = new Goal(troop, this.empire, selectedPlanet);
                                    this.Goals.Add(g);
                                    currentgoals++;

                                }
                            }
                        }
                    }
                }
            }
        }

        private void RunInfrastructurePlanner()
        {
            //if (this.empire.SpaceRoadsList.Sum(node=> node.NumberOfProjectors) < ShipCountLimit * GlobalStats.spaceroadlimit)
            float sspBudget = this.empire.Money * (.01f *(1.025f-this.empire.data.TaxRate));
            if (sspBudget < 0 || this.empire.data.SSPBudget > this.empire.Money * .1)
            {
                sspBudget = 0;
                
            }
            else
            {
                this.empire.Money -= sspBudget;
                this.empire.data.SSPBudget += sspBudget;
            }
            sspBudget = this.empire.data.SSPBudget *.1f;            
            float roadMaintenance = 0;
            float nodeMaintenance = ResourceManager.ShipsDict["Subspace Projector"].GetMaintCost(this.empire);
            foreach (SpaceRoad roadBudget in this.empire.SpaceRoadsList)
            {
                if (roadBudget.NumberOfProjectors == 0)
                    roadBudget.NumberOfProjectors = roadBudget.RoadNodesList.Count;
                roadMaintenance += roadBudget.NumberOfProjectors * nodeMaintenance;
            }
  
            sspBudget -= roadMaintenance;
            
            //this.empire.data.SSPBudget += sspBudget;
            //sspBudget = this.empire.data.SSPBudget;
            float UnderConstruction = 0f;
            foreach (Goal g in this.Goals)
            {
                //if (g.GoalName == "BuildOffensiveShips")
                //    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                //    {
                //        {
                //            UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                //        }
                //    }
                //    else
                //    {
                //        {
                //            UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                //        }
                //    }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }
             
                {
                    UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                }
            }
            sspBudget -= UnderConstruction;
            if (sspBudget > nodeMaintenance*2) //- nodeMaintenance * 5
            {
                foreach (SolarSystem ownedSystem in this.empire.GetOwnedSystems())
                //ReaderWriterLockSlim roadlock = new ReaderWriterLockSlim();
                //Parallel.ForEach(this.empire.GetOwnedSystems(), ownedSystem =>
                {

                    IOrderedEnumerable<SolarSystem> sortedList =
                        from otherSystem in this.empire.GetOwnedSystems()
                        orderby Vector2.Distance(otherSystem.Position, ownedSystem.Position)
                        select otherSystem;
                    int devLevelos = 0;
                    foreach (Planet p in ownedSystem.PlanetList)
                    {
                        if (p.Owner != this.empire)
                            continue;
                        //if (p.ps != Planet.GoodState.EXPORT && p.fs != Planet.GoodState.EXPORT)
                        //    continue;
                        devLevelos += p.developmentLevel;

                    }
                    if (devLevelos == 0)
                        //return;
                        continue;
                    foreach (SolarSystem Origin in sortedList)
                    {
                        if (Origin == ownedSystem)
                        {
                            continue;
                        }
                        int devLevel = devLevelos;
                        bool createRoad = true;
                        //roadlock.EnterReadLock();
                        foreach (SpaceRoad road in this.empire.SpaceRoadsList)
                        {
                            if (road.GetOrigin() != ownedSystem && road.GetDestination() != ownedSystem)
                            {
                                continue;
                            }
                            createRoad = false;
                        }
                       // roadlock.ExitReadLock();
                        foreach (Planet p in Origin.PlanetList)
                        {
                            if (p.Owner != this.empire)
                                continue;
                            devLevel += p.developmentLevel;
                        }
                        if (!createRoad)
                        {
                            continue;
                        }
                        SpaceRoad newRoad = new SpaceRoad(Origin, ownedSystem, this.empire, sspBudget, nodeMaintenance);

                        //roadlock.EnterWriteLock();
                        if (sspBudget <= 0 || newRoad.NumberOfProjectors == 0 || newRoad.NumberOfProjectors > devLevel)
                        {
                           // roadlock.ExitWriteLock();
                            continue;                            
                        }
                        sspBudget -= newRoad.NumberOfProjectors * nodeMaintenance;
                        UnderConstruction += newRoad.NumberOfProjectors * nodeMaintenance;
                        
                            this.empire.SpaceRoadsList.Add(newRoad);
                           // roadlock.ExitWriteLock();
                    }
                }//);
            }
            sspBudget = this.empire.data.SSPBudget-roadMaintenance - UnderConstruction;
            Array<SpaceRoad> ToRemove = new Array<SpaceRoad>();
            //float income = this.empire.Money +this.empire.GrossTaxes; //this.empire.EstimateIncomeAtTaxRate(0.25f) +
            foreach (SpaceRoad road in this.empire.SpaceRoadsList.OrderBy(ssps => ssps.NumberOfProjectors))
            {
                
                if (road.RoadNodesList.Count == 0 ||sspBudget <= 0.0f )// || road.NumberOfProjectors ==0)
                {
                    //if(road.NumberOfProjectors ==0)
                    //{
                        //int rnc=0;
                        //foreach(RoadNode rn in road.RoadNodesList)
                        //{
                        //    foreach(Goal G in this.Goals)                            
                        //    {                                
                        //            if (G.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == rn.Position))
                        //            {
                        //                continue;
                        //            }

                        //    }
                        //    if (rn.Platform == null)
                        //        continue;
                        //    rnc++;
                        //}
                        //if (rnc > 0)
                        //    road.NumberOfProjectors = rnc;
                        //else
                     //       ToRemove.Add(road);

                    
                    //else
                    ToRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
                    continue;
                }
                  
                                
                RoadNode ssp = road.RoadNodesList.Where(notNull => notNull != null && notNull.Platform !=null).FirstOrDefault();
                if ((ssp != null && (!road.GetOrigin().OwnerList.Contains(this.empire) || !road.GetDestination().OwnerList.Contains(this.empire))))
                {
                    ToRemove.Add(road);
                    sspBudget += road.NumberOfProjectors * nodeMaintenance;
                    //if(ssp!=null )
                    //{
                    //    this.SSPBudget += road.NumberOfProjectors * nodeMaintenance;
                    //}
                }
                else
                {
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform != null && (node.Platform == null || node.Platform.Active))
                            continue;

                        bool addNew = true;
                        foreach (Goal g in Goals)
                        {
                            if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
                                continue;
                            addNew = false;
                            break;
                        }
                        using (empire.BorderNodes.AcquireReadLock())
                        foreach (Empire.InfluenceNode bordernode in empire.BorderNodes)
                        {
                            float sizecheck = Vector2.Distance(node.Position, bordernode.Position);
                            sizecheck += !(bordernode.SourceObject is Ship) ? Empire.ProjectorRadius : 0;
                            if (sizecheck >= bordernode.Radius)
                                continue;
                            addNew = false;
                            break;
                        }
                        if (addNew) Goals.Add(new Goal(node.Position, "Subspace Projector", empire));
                    }
                }
            }
            if (this.empire != Empire.Universe.player)
            {
                foreach (SpaceRoad road in ToRemove)
                {
                    this.empire.SpaceRoadsList.Remove(road);
                    foreach (RoadNode node in road.RoadNodesList)
                    {
                        if (node.Platform != null && node.Platform.Active ) //(node.Platform == null || node.Platform.Active))
                        {
                            node.Platform.Die(null,true); //.GetAI().OrderScrapShip();
                            
                            continue;
                        }


                        foreach (Goal g in this.Goals)
                        {
                            if (g.type != GoalType.DeepSpaceConstruction || !(g.BuildPosition == node.Position))
                            {
                                continue;
                            }
                            this.Goals.QueuePendingRemoval(g);
                            foreach (Planet p in this.empire.GetPlanets())
                            {
                                foreach (QueueItem qi in p.ConstructionQueue)
                                {
                                    if (qi.Goal != g)
                                    {
                                        continue;
                                    }
                                    Planet productionHere = p;
                                    productionHere.ProductionHere = productionHere.ProductionHere + qi.productionTowards;
                                    if (p.ProductionHere > p.MAX_STORAGE)
                                    {
                                        p.ProductionHere = p.MAX_STORAGE;
                                    }
                                    p.ConstructionQueue.QueuePendingRemoval(qi);
                                }
                                p.ConstructionQueue.ApplyPendingRemovals();
                            }

                            using (empire.GetShips().AcquireReadLock())                               
                            foreach (Ship ship in this.empire.GetShips())
                            {
                                ArtificialIntelligence.ShipGoal goal = ship.AI.OrderQueue.PeekLast;
                                
                                if (goal?.goal == null || goal.goal.type != GoalType.DeepSpaceConstruction || goal.goal.BuildPosition != node.Position)
                                {
                                    continue;
                                }
                                ship.AI.OrderScrapShip();
                                
                                break;

                            }

                        }
                        this.Goals.ApplyPendingRemovals();
                    }
                    this.empire.SpaceRoadsList.Remove(road);
                }
            }
        }

        private void RunManagers()
        {
            if (this.empire.data.IsRebelFaction || this.empire.data.Defeated)
            {
                return;
            }
            if (!this.empire.isPlayer)
            {
                OffensiveForcePoolManager.ManageAOs();
                foreach (AO ao in this.AreasOfOperations)
                {
                    ao.Update();
                }
            }
            //this.UpdateThreatMatrix();
            if (!this.empire.isFaction && !this.empire.MinorRace)
            {
                if (!this.empire.isPlayer || this.empire.AutoColonize)
                    this.RunExpansionPlanner();
                if(!this.empire.isPlayer || this.empire.AutoBuild)
                    this.RunInfrastructurePlanner();
            }
            this.DefensiveCoordinator.ManageForcePool();
            if (!this.empire.isPlayer)
            {
                this.RunEconomicPlanner();
                this.RunDiplomaticPlanner();
                if(this.empire.isFaction)
                {

                }
                if (!this.empire.MinorRace)
                {
                    
                    this.RunMilitaryPlanner();
                    this.RunResearchPlanner();
                    this.RunAgentManager();
                    this.RunWarPlanner();
                }
            }
            //Added by McShooterz: automating research
            else
            {
                if (this.empire.AutoResearch)
                    this.RunResearchPlanner();
                if (this.empire.data.AutoTaxes)
                    this.RunEconomicPlanner();
            }
        }

    
        //added by gremlin deveksmod military planner
        private void RunMilitaryPlanner()
        {
#region ShipBuilding
            this.nobuild = false;
            int ShipCountLimit = GlobalStats.ShipCountLimit;
            if (!this.empire.MinorRace)
                this.RunGroundPlanner();
            this.numberOfShipGoals = 0;// 6 + this.empire.data.EconomicPersonality.ShipGoalsPlus;
            foreach (Planet p in this.empire.GetPlanets())
            {
                // if (!p.HasShipyard || (p.GetMaxProductionPotential() <2f
                if (//(p.GetMaxProductionPotential() < 2f //||( this.empire.data.Traits.Cybernetic !=0 && p.GetMaxProductionPotential()-p.consumption <2f)
                    //|| p.ps == Planet.GoodState.IMPORT
                    (p.WorkerPercentage) > .75 || p.GetMaxProductionPotential() < 2f

                    )//)   //p.GetNetProductionPerTurn() < .5f))
                {
                    continue;
                }

                this.numberOfShipGoals++; //(int)(p.ProductionHere /(1+ p.ConstructionQueue.Sum(q => q.Cost)));
            }

            // this.numberOfShipGoals = this.numberOfShipGoals / this.empire.GetPlanets().Count;
            //  this.numberOfShipGoals = (int)((float)this.numberOfShipGoals* (1 - this.empire.data.TaxRate));
            float numgoals = 0f;
            float offenseUnderConstruction = 0f;
            float UnderConstruction = 0f;
            float TroopStrengthUnderConstruction = 0f;
            foreach (Goal g in this.Goals)
            //Parallel.ForEach(this.Goals, g =>
            {
                if (g.GoalName == "BuildOffensiveShips" || g.GoalName == "BuildDefensiveShips")
                {
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                    }
                    else
                    {
                        UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                    }
                    offenseUnderConstruction += ResourceManager.ShipsDict[g.ToBuildUID].BaseStrength;
                    foreach (Troop t in ResourceManager.ShipsDict[g.ToBuildUID].TroopList)
                    {
                        TroopStrengthUnderConstruction = TroopStrengthUnderConstruction + (float)t.Strength;
                    }
                    numgoals = numgoals + 1f;
                }
                if (g.GoalName != "BuildConstructionShip")
                {
                    continue;
                }
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                }
                else
                {
                    UnderConstruction = UnderConstruction + ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                }
            }
            //this.GetAShip(0);
            //float offensiveStrength = offenseUnderConstruction + this.empire.GetForcePoolStrength();

            //int numWars = 0;
            float offenseNeeded = 0;
            //float FearTrust = 0;
            offenseNeeded += ThreatMatrix.StrengthOfAllThreats(empire);
            offenseNeeded /= empire.currentMilitaryStrength;

            if (offenseNeeded <= 0)
            {
                offenseNeeded = 0;
            }

            //offenseNeeded += FearTrust;
            if (offenseNeeded > 20)
                offenseNeeded = 20;
            numberOfShipGoals += (int)offenseNeeded;

            //float Capacity = this.empire.EstimateIncomeAtTaxRate(tax) + this.empire.Money * -.1f -UnderConstruction + this.empire.GetAverageNetIncome();
            float AtWarBonus = 0.05f;
            if (this.empire.Money > 500f)
                AtWarBonus += (offenseNeeded * (0.03f + this.empire.getResStrat().MilitaryPriority * .03f));
            float Capacity = this.empire.Grossincome() * (.25f + AtWarBonus) - UnderConstruction;// -UnderConstruction - this.empire.GetTotalShipMaintenance();// +this.empire.GetAverageNetIncome();
            float allowable_deficit = -(this.empire.Money * .05f) * AtWarBonus;//*(1.5f-this.empire.data.TaxRate))); //>0?(1 - (this.empire.Money * 10 / this.empire.Money)):0); //-Capacity;// +(this.empire.Money * -.1f);
            //-Capacity;

            if (Capacity > this.buildCapacity)
                this.buildCapacity = Capacity;
            this.empire.data.ShipBudget = this.buildCapacity;
            if (Capacity - this.empire.GetTotalShipMaintenance() - allowable_deficit <= 0f)
            {
                Capacity -= this.empire.GetTotalShipMaintenance() - allowable_deficit;
                float HowMuchWeAreScrapping = 0f;

                foreach (Ship ship1 in this.empire.GetShips())
                {
                    if (ship1.AI.State != AIState.Scrap)
                    {
                        continue;
                    }
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCostRealism();
                    }
                    else
                    {
                        HowMuchWeAreScrapping = HowMuchWeAreScrapping + ship1.GetMaintCost(this.empire);
                    }
                }
                if (HowMuchWeAreScrapping < Math.Abs(Capacity))
                {
                    float Added = 0f;

                    //added by gremlin clear out building ships before active ships.
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        foreach (Goal g in this.Goals.Where(goal => goal.GoalName == "BuildOffensiveShips" || goal.GoalName == "BuildDefensiveShips").OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID].GetMaintCostRealism()))
                        {
                            bool flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {

                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;

                                }
                                //g.GetPlanetWhereBuilding().ProductionHere += shipToRemove.productionTowards;
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                this.Goals.QueuePendingRemoval(g);
                                Added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCostRealism();
                                flag = true;
                                break;

                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (HowMuchWeAreScrapping + Added >= Math.Abs(Capacity))
                                break;

                        }
                    }
                    else
                    {
                        foreach (Goal g in this.Goals.Where(goal => goal.GoalName == "BuildOffensiveShips" || goal.GoalName == "BuildDefensiveShips").OrderByDescending(goal => ResourceManager.ShipsDict[goal.ToBuildUID].GetMaintCost(this.empire)))
                        {
                            bool flag = false;
                            if (g.GetPlanetWhereBuilding() == null)
                                continue;
                            foreach (QueueItem shipToRemove in g.GetPlanetWhereBuilding().ConstructionQueue)
                            {

                                if (shipToRemove.Goal != g || shipToRemove.productionTowards > 0f)
                                {
                                    continue;

                                }
                                //g.GetPlanetWhereBuilding().ProductionHere += shipToRemove.productionTowards;
                                g.GetPlanetWhereBuilding().ConstructionQueue.QueuePendingRemoval(shipToRemove);
                                this.Goals.QueuePendingRemoval(g);
                                Added += ResourceManager.ShipsDict[g.ToBuildUID].GetMaintCost(this.empire);
                                flag = true;
                                break;

                            }
                            if (flag)
                                g.GetPlanetWhereBuilding().ConstructionQueue.ApplyPendingRemovals();
                            if (HowMuchWeAreScrapping + Added >= Math.Abs(Capacity))
                                break;
                        }
                    }

                    this.Goals.ApplyPendingRemovals();
                    Capacity = Capacity + HowMuchWeAreScrapping + Added;
                }
                this.buildCapacity = Capacity;
            }
            //Capacity = this.empire.EstimateIncomeAtTaxRate(tax) - UnderConstruction;

            //if (allowable_deficit > 0f || noIncome > tax)
            //{
            //    allowable_deficit = Math.Abs(allowable_deficit);
            //}

            //this.buildCapacity = Capacity;
            if (this.buildCapacity < 0) //Scrap active ships
                this.GetAShip(this.buildCapacity); //- allowable_deficit

            //fbedard: Build Defensive ships
            bool Def = false;
            float HalfCapacity = this.buildCapacity / 2f;
            foreach (Planet planet2 in this.empire.GetPlanets())
                if (planet2.HasShipyard && planet2.ParentSystem.combatTimer > 0f)
                    Def = true;
            Capacity = this.buildCapacity;
            if (Def)
                while (Capacity - HalfCapacity > 0f
                    && numgoals < this.numberOfShipGoals / 2
                    && (Empire.Universe.globalshipCount < ShipCountLimit + recyclepool
                    || this.empire.empireShipTotal < this.empire.EmpireShipCountReserve)) //shipsize < SizeLimiter)
                {

                    string s = this.GetAShip(this.buildCapacity);//Capacity - allowable_deficit);
                    if (s == null || !this.empire.ShipsWeCanBuild.Contains(s))
                    {
                        break;
                    }
                    if (this.recyclepool > 0)
                    {
                        this.recyclepool--;
                    }

                    Goal g = new Goal(s, "BuildDefensiveShips", this.empire)
                    {
                        type = GoalType.BuildShips
                    };
                    this.Goals.Add(g);
                    if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                    {
                        Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                    }
                    else
                    {
                        Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost(this.empire);
                    }
                    numgoals = numgoals + 1f;
                }
            //if(numgoals >this.numberOfShipGoals)
            //    Log.Info("Offense Needed: " + this.numberOfShipGoals);
            //Build Offensive ships:
            while (Capacity > 0 //this.buildCapacity > 0 //Capacity > allowable_deficit 
                && numgoals < this.numberOfShipGoals
                && (Empire.Universe.globalshipCount < ShipCountLimit + recyclepool
                || this.empire.empireShipTotal < this.empire.EmpireShipCountReserve)) //shipsize < SizeLimiter)
            {

                string s = this.GetAShip(this.buildCapacity);//Capacity - allowable_deficit);
                if (string.IsNullOrEmpty(s))
                {
                    break;
                }
                if (this.recyclepool > 0)
                {
                    this.recyclepool--;
                }

                Goal g = new Goal(s, "BuildOffensiveShips", this.empire)
                {
                    type = GoalType.BuildShips
                };
                this.Goals.Add(g);
                if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
                {
                    Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCostRealism();
                }
                else
                {
                    Capacity = Capacity - ResourceManager.ShipsDict[s].GetMaintCost(this.empire);
                }
                numgoals = numgoals + 1f;
            }

            foreach (Goal g in this.Goals)
            {
                if (g.type != GoalType.Colonize || g.Held)
                {
                    if (g.type != GoalType.Colonize || !g.Held || g.GetMarkedPlanet().Owner == null)
                    {
                        continue;
                    }
                    foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
                    {
                        this.empire.GetGSAI().CheckClaim(Relationship, g.GetMarkedPlanet());
                    }
                    this.Goals.QueuePendingRemoval(g);

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (MilitaryTask task in this.TaskList)
                        {
                            foreach (Guid held in task.HeldGoals)
                            {
                                if (held != g.guid)
                                {
                                    continue;
                                }
                                this.TaskList.QueuePendingRemoval(task);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (g.GetMarkedPlanet() != null)
                    {
                        foreach (KeyValuePair<Guid, ThreatMatrix.Pin> pin in ThreatMatrix.Pins
                            .Where(pin => !((Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f) 
                            || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == empire || pin.Value.Strength <= 0f
                            || !this.empire.GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName)).AtWar)))
                        {
                            if (Vector2.Distance(g.GetMarkedPlanet().Position, pin.Value.Position) >= 75000f 
                                || EmpireManager.GetEmpireByName(pin.Value.EmpireName) == empire || pin.Value.Strength <= 0f 
                                || !empire.GetRelations(EmpireManager.GetEmpireByName(pin.Value.EmpireName)).AtWar 
                                && !EmpireManager.GetEmpireByName(pin.Value.EmpireName).isFaction)
                            {
                                continue;
                            }
                            Array<Goal> tohold = new Array<Goal>()
                        {
                            g
                        };
                            MilitaryTask task = new MilitaryTask(g.GetMarkedPlanet().Position, 125000f, tohold, this.empire);
                            //lock (GlobalStats.TaskLocker)
                            {
                                this.TaskList.Add(task);
                                break;
                            }
                        }
                    }
                }
            }
            if (this.empire.data.DiplomaticPersonality.Territorialism < 50 && this.empire.data.DiplomaticPersonality.Trustworthiness < 50)    //    Name == "Aggressive" || this.empire.data.DiplomaticPersonality.Name == "Ruthless" || this.empire.data.EconomicPersonality.Name == "Expansionist")
            {
                foreach (Goal g in this.Goals)
                {
                    if (g.type != GoalType.Colonize || g.Held)
                    {
                        continue;
                    }
                    bool OK = true;

                    using (TaskList.AcquireReadLock())
                    {
                        foreach (MilitaryTask mt in this.TaskList)
                        //Parallel.ForEach(this.TaskList, (mt,state) =>
                        {
                            if ((mt.type != MilitaryTask.TaskType.DefendClaim
                                && mt.type != MilitaryTask.TaskType.ClearAreaOfEnemies)
                                || g.GetMarkedPlanet() != null
                                && !(mt.TargetPlanetGuid == g.GetMarkedPlanet().guid))
                            {
                                continue;
                            }
                            OK = false;
                            break;
                        }
                    }
                    if (!OK)
                    {
                        continue;
                    }
                    if (g.GetMarkedPlanet() == null)
                        continue;
                    MilitaryTask task = new MilitaryTask()
                    {
                        AO = g.GetMarkedPlanet().Position
                    };
                    task.SetEmpire(this.empire);
                    task.AORadius = 75000f;
                    task.SetTargetPlanet(g.GetMarkedPlanet());
                    task.TargetPlanetGuid = g.GetMarkedPlanet().guid;
                    task.type = MilitaryTask.TaskType.DefendClaim;
                    //lock (GlobalStats.TaskLocker)
                    {
                        this.TaskList.Add(task);
                    }
                }
            }
            this.Goals.ApplyPendingRemovals();
            
#endregion
            //this where the global AI attack stuff happenes.
            using (TaskList.AcquireReadLock())    
            {
                Array<MilitaryTask> ToughNuts = new Array<MilitaryTask>();
                Array<MilitaryTask> InOurSystems = new Array<MilitaryTask>();
                Array<MilitaryTask> InOurAOs = new Array<MilitaryTask>();
                Array<MilitaryTask> Remainder = new Array<MilitaryTask>();
                Vector2 EmpireCenter = this.empire.GetWeightedCenter();
                //var tasksort = from tasks in  this.TaskList
                //               where tasks.type == MilitaryTask.TaskType.AssaultPlanet
                //               orderby Vector2.Distance(EmpireCenter,tasks.GetTargetPlanet().Owner.GetWeightedCenter()),


                //               select tasks
                //               ;
                //float distance = 0;

                foreach (MilitaryTask task in this.TaskList //.OrderBy(target => Vector2.Distance(target.AO, this.empire.GetWeightedCenter()) / 1500000)
                    .OrderByDescending(empire =>
                    {
                        if (empire.type != MilitaryTask.TaskType.AssaultPlanet)
                            return 0;
                        float weight = 0;
                        weight += (this.empire.currentMilitaryStrength - empire.MinimumTaskForceStrength) / this.empire.currentMilitaryStrength * 5;
                        //weight += ((Empire.Universe.Size.X*.25f) - this.GetDistanceFromOurAO(empire.AO)) / (Empire.Universe.Size.X * .25f) * 10;

                        if (empire.GetTargetPlanet() == null)
                        {

                            return weight * 2;
                        }
                        Empire emp = empire.GetTargetPlanet().Owner;
                        if (emp == null)
                            return 0;
                        if (emp.isFaction)
                            return 0;

                        Relationship test = null;
                        if (this.empire.TryGetRelations(emp, out test) && test != null)
                        {
                            if (test.Treaty_NAPact || test.Treaty_Alliance || test.Posture != Posture.Hostile)
                                return 0;
                            weight += ((test.TotalAnger * .25f) - (100 - test.Threat)) / (test.TotalAnger * .25f) * 5f;
                            if (test.AtWar)
                                weight += 5;

                        }
                        Planet target = empire.GetTargetPlanet();
                        if (target != null)
                        {
                            SystemCommander scom;
                            target.Owner.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(target.system, out scom);
                            if (scom != null)
                                weight += 11-scom.RankImportance;
                            //weight += (target.MaxPopulation /1000) + (target.MineralRichness + (int)target.developmentLevel);
                        }

                        if (emp.isPlayer)
                            weight *= ((int)Empire.Universe.GameDifficulty > 0 ? (int)Empire.Universe.GameDifficulty : 1);
                        return weight;



                    })

                    )
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        continue;
                    }
                    if (task.IsToughNut)
                    {
                        ToughNuts.Add(task);
                    }
                    else if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() == entry.Value)
                            {
                                foreach (AO area in AreasOfOperations)
                                {
                                    if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                        continue;
                                    InOurAOs.Add(task);
                                    dobreak = true;
                                    break;
                                }
                            }
                            break;
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        Remainder.Add(task);
                    }
                    else
                    {
                        InOurSystems.Add(task);
                    }
                }
                //this.TaskList.thisLock.ExitReadLock();
                Array<MilitaryTask> TNInOurSystems = new Array<MilitaryTask>();
                Array<MilitaryTask> TNInOurAOs = new Array<MilitaryTask>();
                Array<MilitaryTask> TNRemainder = new Array<MilitaryTask>();
                this.toughnuts = ToughNuts.Count;
                foreach (MilitaryTask task in ToughNuts)
                {
                    if (!this.empire.GetOwnedSystems().Contains(task.GetTargetPlanet().system))
                    {
                        bool dobreak = false;
                        foreach (KeyValuePair<Guid, Planet> entry in Empire.Universe.PlanetsDict)
                        {
                            if (task.GetTargetPlanet() != entry.Value)
                            {
                                continue;
                            }
                            foreach (AO area in AreasOfOperations)
                            {
                                if (entry.Value.Position.OutsideRadius(area.Position, area.Radius))
                                    continue;
                                TNInOurAOs.Add(task);
                                dobreak = true;
                                break;
                            }
                            break;
                        }
                        if (dobreak)
                        {
                            continue;
                        }
                        TNRemainder.Add(task);
                    }
                    else
                    {
                        TNInOurSystems.Add(task);
                    }
                }
                foreach (MilitaryTask task in TNInOurAOs)
                //Parallel.ForEach(TNInOurAOs, task =>
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                        //return;
                    }
                    task.Evaluate(this.empire);
                }//);
                foreach (MilitaryTask task in TNInOurSystems)
                //Parallel.ForEach(TNInOurSystems, task =>
                {
                    task.Evaluate(this.empire);
                }//);
                foreach (MilitaryTask task in TNRemainder)
                {
                    if (task.GetTargetPlanet().Owner == null || task.GetTargetPlanet().Owner == this.empire || this.empire.GetRelations(task.GetTargetPlanet().Owner).ActiveWar == null || (float)this.empire.TotalScore <= (float)task.GetTargetPlanet().Owner.TotalScore * 1.5f)
                    {
                        continue;
                    }
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurAOs)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in InOurSystems)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in Remainder)
                {
                    task.Evaluate(this.empire);
                }
                foreach (MilitaryTask task in this.TaskList)
                {
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet)
                    {
                        task.Evaluate(this.empire);
                    }
                    if (task.type != MilitaryTask.TaskType.AssaultPlanet && task.type != MilitaryTask.TaskType.GlassPlanet || task.GetTargetPlanet().Owner != null && task.GetTargetPlanet().Owner != this.empire)
                    {
                        continue;
                    }
                    task.EndTask();
                }
            }
            this.TaskList.AddRange(this.TasksToAdd);
            this.TasksToAdd.Clear();
            this.TaskList.ApplyPendingRemovals();

        }

        
        private int ScriptIndex=0;
        private float randomizer(float priority, float bonus)
        {
            float index=0;
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            index += RandomMath.RandomBetween(0, (priority + bonus));
            return index ;
        }
        private void RunResearchPlanner()
        {
            if (string.IsNullOrEmpty(this.empire.ResearchTopic))
            {
                //Added by McShooterz: random tech is less random, selects techs based on priority
                //Check for needs of empire
                        // Ship ship;
                        // int researchneeded = 0;

                        //if( ResourceManager.ShipsDict.TryGetValue(this.BestCombatShip, out ship))
                        //{
                        //    ship.shipData.TechScore / this.empire.Research
                        //}
                bool cybernetic = this.empire.data.Traits.Cybernetic > 0;
                bool atWar = false;
                bool highTaxes = false;
                bool lowResearch = false;
                bool lowincome = false;
                float researchDebt = 0;
                float wars = this.empire.AllRelations.Where(war => !war.Key.isFaction && (war.Value.AtWar || war.Value.PreparingForWar)).Sum(str => str.Key.currentMilitaryStrength /this.empire.currentMilitaryStrength);                                

                if (this.empire.data.TaxRate >= .50f )
                    highTaxes = true;
                if (!string.IsNullOrEmpty(this.postResearchTopic))
                researchDebt =.1f + (this.empire.TechnologyDict[this.postResearchTopic].TechCost/ (.1f + (100*UniverseScreen.GamePaceStatic)*this.empire.GetPlanets().Sum(research => research.NetResearchPerTurn)));
                if(researchDebt >4)
                lowResearch = true;
                //if (this.empire.GetPlanets().Sum(research => research.NetResearchPerTurn) < this.empire.GetPlanets().Count / 3)
                //    lowResearch = true;
                float economics = (this.empire.data.TaxRate * 10);// 10 - (int)(this.empire.Money / (this.empire.GrossTaxes + 1));
                    //(int)(this.empire.data.TaxRate * 10 + this.empire.Money < this.empire.GrossTaxes?5:0);
                float needsFood =0;
                foreach(Planet hunger in this.empire.GetPlanets())
                {
                    if ((cybernetic ? hunger.ProductionHere : hunger.FoodHere) / hunger.MAX_STORAGE < .20f) //: hunger.MAX_STORAGE / (hunger.FoodHere+1) > 25)
                    {
                        needsFood++;
                        
                    }
                    if(!this.empire.GetTDict()["Biospheres"].Unlocked)
                    {
                       if(hunger.Fertility ==0)
                        needsFood+=2;
                    }

                }
                float shipBuildBonus = 0f;
                if (this.empire.data.TechDelayTime > 0)
                    this.empire.data.TechDelayTime--;
                if (this.empire.data.TechDelayTime > 0)
                {
                    shipBuildBonus = -5 - this.empire.data.TechDelayTime;
                }
                else
                    shipBuildBonus = 0;

                needsFood = needsFood>0 ? needsFood /this.empire.GetPlanets().Count :0;
                needsFood *= 10;

                switch (this.res_strat)
                {
                    case GSAI.ResearchStrategy.Random:
                     case    GSAI.ResearchStrategy.Scripted:
                    {

                        if (true)
                        {
                            Map<string, float> priority = new Map<string, float>();
                            var resStrat = empire.getResStrat();
 
                            priority.Add("SHIPTECH", randomizer(resStrat.MilitaryPriority,  wars+ shipBuildBonus));

                            priority.Add("Research", randomizer(resStrat.ResearchPriority ,  (researchDebt)));
                            priority.Add("Colonization", randomizer(resStrat.ExpansionPriority,  (!cybernetic ? needsFood : -1)));
                            priority.Add("Economic", randomizer(resStrat.ExpansionPriority,  (economics)));
                            priority.Add("Industry", randomizer(resStrat.IndustryPriority ,  (cybernetic ? needsFood : 0)));
                            priority.Add("General", randomizer(resStrat.ResearchPriority,0 ));
                            priority.Add("GroundCombat", this.randomizer(resStrat.MilitaryPriority,(wars + shipBuildBonus) * .5f));

                            string sendToScript = string.Empty;
                            int max = 0;
                            foreach (var pWeighted in priority.OrderByDescending(pri => pri.Value))
                            {
                                if (max > priority.Count)
                                    break;
                                if (pWeighted.Value < 0) //&& !string.IsNullOrEmpty(sendToScript))
                                    continue;
                                priority[pWeighted.Key] = -1;
                                sendToScript += ":";
                                if (pWeighted.Key == "SHIPTECH")
                                {
                                    sendToScript += "ShipWeapons:ShipDefense:ShipGeneral:ShipHull";
                                    max += 4;

                                }
                                else
                                {
                                    sendToScript += pWeighted.Key;
                                    max++;
                                }


                            }
                            if (ScriptedResearch("CHEAPEST", "TECH", "TECH"+sendToScript))
                                return;

                            
                        }

                        return;

#if false // All of this was disabled by Crunchy gremlin:
                        //changed by gremlin exclude module tech that we dont have any ships that use it.
                        ConcurrentBag<Technology> AvailableTechs = new ConcurrentBag<Technology>();
                        //foreach (KeyValuePair<string, Ship_Game.Technology> Technology in ResourceManager.TechTree)

                        //System.Threading.Tasks.Parallel.ForEach(ResourceManager.TechTree, Technology =>
                        foreach(var Technology in ResourceManager.TechTree)
                        {
                            TechEntry tech = null;// new TechEntry();
                            bool techexists = this.empire.GetTDict().TryGetValue(Technology.Key, out tech);
                            if (!techexists || tech == null)
                                //continue;
                                return;
                            Technology technology = tech.Tech;
                            if (tech.Unlocked
                                || !this.empire.HavePreReq(Technology.Key)
                                || (Technology.Value.Secret && !tech.Discovered)
                                || technology.BuildingsUnlocked.Where(winsgame => ResourceManager.BuildingsDict[winsgame.Name].WinsGame == true).Count() > 0
                                || !tech.shipDesignsCanuseThis
                                || (tech.shipDesignsCanuseThis && technology.ModulesUnlocked.Count > 0 && technology.HullsUnlocked.Count == 0
                                && !this.empire.WeCanUseThisNow(tech.Tech)))
                            {
                                //continue;
                                return;
                            }
                            AvailableTechs.Add(Technology.Value);
                        }//);
                        if (AvailableTechs.Count == 0)
                            break;
                        foreach(Technology tech in AvailableTechs.OrderBy(tech => tech.Cost))
                        {
                            switch (tech.TechnologyType)
                            {
                                //case TechnologyType.ShipHull:
                                //    {
                                //        //Always research when able
                                //        this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipGeneral:
                                //    {
                                //        if (RandomMath.InRange(4) == 3)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipWeapons:
                                //    {
                                //        if(atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                //case TechnologyType.ShipDefense:
                                //    {
                                //        if (atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                //            this.empire.ResearchTopic = tech.UID;
                                //        break;
                                //    }
                                case TechnologyType.GroundCombat:
                                    {
                                        if (atWar || RandomMath.InRange(this.empire.getResStrat().MilitaryPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Economic:
                                    {
                                        if (highTaxes || RandomMath.InRange(this.empire.getResStrat().ExpansionPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Research:
                                    {
                                        if (lowResearch || RandomMath.InRange(this.empire.getResStrat().ResearchPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Industry:
                                    {
                                        if (RandomMath.InRange(this.empire.getResStrat().IndustryPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                case TechnologyType.Colonization:
                                    {
                                        if (RandomMath.InRange(this.empire.getResStrat().ExpansionPriority + 4) > 2)
                                            this.empire.ResearchTopic = tech.UID;
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                            if (!string.IsNullOrEmpty(this.empire.ResearchTopic))
                                break;
                        }
                        if (string.IsNullOrEmpty(this.empire.ResearchTopic))
                            this.empire.ResearchTopic = AvailableTechs.OrderBy(tech => tech.Cost).First().UID;
                        break;
#endif
                        }
                    //case GSAI.ResearchStrategy.Scripted:
                    default:
                    {
                        int loopcount = 0;    
                    Start:
                        if (this.empire.getResStrat() != null && ScriptIndex < this.empire.getResStrat().TechPath.Count &&loopcount < this.empire.getResStrat().TechPath.Count)
                        {
                            
                        string scriptentry = this.empire.getResStrat().TechPath[ScriptIndex].id;
                            string scriptCommand = this.empire.GetTDict().ContainsKey(scriptentry) ?  scriptentry:scriptentry.Split(':')[0];
                            switch (scriptCommand)
                            {

                                
                                case  "SCRIPT":
                                    {
                                        
                                        string modifier ="";
                                        string[] script =scriptentry.Split(':');
                                        
                                        if(script.Count() >2)
                                        {
                                            modifier = script[2];
                                            
                                        }
                                        ScriptIndex++;
                                        if (ScriptedResearch("CHEAPEST",script[1], modifier))                                            
                                            return;
                                        loopcount++;
                                        goto Start;
                                    }
                                case "LOOP":
                                        {
                                            ScriptIndex=int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                            loopcount++;
                                            goto Start;
                                        }
                                case "CHEAPEST":
                                        {                                            
                                        string modifier ="";
                                        string[] script =scriptentry.Split(':');
                                        
                                        if(script.Count() ==1)
                                        {
                                            this.res_strat = GSAI.ResearchStrategy.Random;
                                            this.RunResearchPlanner();
                                            this.res_strat = GSAI.ResearchStrategy.Scripted;
                                            ScriptIndex++;
                                            return;
                                            
                                        }
                                        string[] modifiers = new string[script.Count()-1];
                                        for(int i=1; i <script.Count();i++)
                                        {
                                            modifiers[i-1] =script[i];
                                        }
                                            modifier =String.Join(":",modifiers);
                                            ScriptIndex++;
                                            if (ScriptedResearch(scriptCommand, script[1], modifier))                                            
                                            return;
                                        loopcount++;
                                        goto Start;

                                        }
                                case "EXPENSIVE":
                                        {
                                            string modifier = "";
                                            string[] script = scriptentry.Split(':');

                                            if (script.Count() == 1)
                                            {
                                                this.res_strat = GSAI.ResearchStrategy.Random;
                                                this.RunResearchPlanner();
                                                this.res_strat = GSAI.ResearchStrategy.Scripted;
                                                ScriptIndex++;
                                                return;

                                            }
                                            string[] modifiers = new string[script.Count() - 1];
                                            for (int i = 1; i < script.Count(); i++)
                                            {
                                                modifiers[i - 1] = script[i];
                                            }
                                            modifier = String.Join(":", modifiers);
                                            ScriptIndex++;
                                            if (ScriptedResearch(scriptCommand,script[1], modifier))
                                                return;
                                            loopcount++;
                                            goto Start;

                                        }
                                case "IFWAR":
                                        {
                                            if (atWar)
                                            {
                                                 ScriptIndex=int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                 loopcount++;
                                            goto Start;
                                            
                                            }
                                            ScriptIndex++;
                                            
                                            goto Start;
                                            
                                        }
                                case "IFHIGHTAX":
                                        {
                                            if (highTaxes)
                                            {
                                                ScriptIndex=int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                            goto Start;
                                            }
                                            ScriptIndex++;
                                            
                                            goto  Start;
                                        }
                                case "IFPEACE":
                                        {
                                            if (!atWar)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;
                                            }
                                            ScriptIndex++;
                                            
                                            goto Start;
                                        }
                                case "IFCYBERNETIC":
                                        {
                                            if(this.empire.data.Traits.Cybernetic>0 )//&& !this.empire.GetBDict()["Biospheres"])//==true)//this.empire.GetTDict().Where(biosphereTech=> biosphereTech.Value.GetTech().BuildingsUnlocked.Where(biosphere=> ResourceManager.BuildingsDict[biosphere.Name].Name=="Biospheres").Count() >0).Count() >0)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;

                                            }
                                            ScriptIndex++;
                                            goto Start;
                                        }
                                case "IFLOWRESEARCH":
                                        {
                                            if (lowResearch)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;

                                            }
                                            ScriptIndex++;
                                            goto Start;

                                        }
                                case "IFNOTLOWRESEARCH":
                                        {
                                            if (!lowResearch)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;

                                            }
                                            ScriptIndex++;
                                            goto Start;

                                        }
                                case "IFLOWINCOME":
                                        {
                                            if (lowincome)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;

                                            }
                                            ScriptIndex++;
                                            goto Start;

                                        }
                                case "IFNOTLOWINCOME":
                                        {
                                            if (!lowincome)
                                            {
                                                ScriptIndex = int.Parse(this.empire.getResStrat().TechPath[ScriptIndex].id.Split(':')[1]);
                                                loopcount++;
                                                goto Start;

                                            }
                                            ScriptIndex++;
                                            goto Start;

                                        }
                                case "RANDOM":
                                        {
                                            this.res_strat = GSAI.ResearchStrategy.Random;
                                            this.RunResearchPlanner();
                                            this.res_strat = GSAI.ResearchStrategy.Scripted;
                                            ScriptIndex++;
                                            return;
                                        }                  
                                default:
                                    {
                                        
                                        TechEntry defaulttech;
                                        if(this.empire.GetTDict().TryGetValue(scriptentry, out defaulttech))
                                        {
                                            if (defaulttech.Unlocked)
                                                
                                            {
                                                ScriptIndex++;
                                                goto Start;
                                            }
                                            if ( !defaulttech.Unlocked && this.empire.HavePreReq(defaulttech.UID))
                                            {
                                                this.empire.ResearchTopic = defaulttech.UID;
                                                ScriptIndex++;
                                                if (!string.IsNullOrEmpty(scriptentry))
                                                    return;
                                            }
                                        }
                                        else
                                        {
                                            Log.Info("TechNotFound : " + scriptentry);
                                            ScriptIndex++;
                                            //Log.Info(scriptentry);
                                        }


                                        foreach (EconomicResearchStrategy.Tech tech in this.empire.getResStrat().TechPath)
                                        {

                                            if (!this.empire.GetTDict().ContainsKey(tech.id) || this.empire.GetTDict()[tech.id].Unlocked || !this.empire.HavePreReq(tech.id))
                                            {
                          
                                                    continue;
                                            }

                                            empire.ResearchTopic = tech.id;
                                            ScriptIndex++;
                                            if(!string.IsNullOrEmpty(tech.id))
                                                    return;
                                        }
                                        this.res_strat = GSAI.ResearchStrategy.Random;
                                        ScriptIndex++;
                                        return;
                                    }
                            }
                        }
                        if (string.IsNullOrEmpty(this.empire.ResearchTopic))
                        {
                            this.res_strat = GSAI.ResearchStrategy.Random;
                        }
                            return;
                        
                            
                        
                    }
                    //default:
                    //{
                    //    return;
                    //}
                }
            }
            if (!string.IsNullOrEmpty(this.empire.ResearchTopic) && this.empire.ResearchTopic != this.postResearchTopic)
            {
                this.postResearchTopic = this.empire.ResearchTopic;
            }
        }
        public class wieght
        {
            public int strength;
            public ShipModule module;
            public int count;
            
        }
        int hullScaler = 1;
        private bool ScriptedResearch(string command1, string command2, string modifier)
        {
            Array<Technology> AvailableTechs = new Array<Technology>();
           
            foreach (var kv in empire.TechnologyDict)
            {
                if ( !kv.Value.Discovered || !kv.Value.shipDesignsCanuseThis || kv.Value.Unlocked || !empire.HavePreReq(kv.Key))
                    continue;
                if (kv.Value.Tech.RootNode == 1)
                {
                    kv.Value.Unlocked = true;
                    continue;                    
                }
                ;
                AvailableTechs.Add(kv.Value.Tech);
            }

            if (AvailableTechs.Count <= 0)
            {
                return false;
            }
            
            List<string> useableTech = new List<string>();



            string researchtopic = "";
            TechnologyType techtype;

#region hull checking.
            this.empire.UpdateShipsWeCanBuild();


            //Ship BestShip = null;// ""; //this.BestCombatShip;          //Not referenced in code, removing to save memory
            //float bestShipStrength = 0f;          //Not referenced in code, removing to save memory
            float techcost = -1;
            float str = 0;
            float moneyNeeded = this.empire.data.ShipBudget * .2f;
            //float curentBestshipStr = 0;          //Not referenced in code, removing to save memory

            if (this.BestCombatShip !=null)
            {
                //this.empire.UpdateShipsWeCanBuild();
                if (this.empire.ShipsWeCanBuild.Contains(this.BestCombatShip.Name))
                    this.BestCombatShip = null;
            }
            if (this.BestCombatShip == null && (modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") || modifier.Contains("ShipGeneral") 
                || modifier.Contains("ShipHull")))
            {
               
                List<string> globalShipTech = new List<string>();
                foreach (string purgeRoots in this.empire.ShipTechs)
                {
                    Technology bestshiptech = null;
                    if (!ResourceManager.TechTree.TryGetValue(purgeRoots, out bestshiptech))
                        continue;
                    switch (bestshiptech.TechnologyType)
                    {
                        case TechnologyType.General:
                        case TechnologyType.Colonization:
                        case TechnologyType.Economic:
                        case TechnologyType.Industry:
                        case TechnologyType.Research:
                        case TechnologyType.GroundCombat:
                            continue;
                        case TechnologyType.ShipHull:
                            break;
                        case TechnologyType.ShipDefense:
                            break;
                        case TechnologyType.ShipWeapons:
                            break;
                        case TechnologyType.ShipGeneral:
                            break;
                        default:
                            break;
                    }
                    globalShipTech.Add(bestshiptech.UID);
                }

                foreach (Technology bestshiptech in AvailableTechs)
                {
                    switch (bestshiptech.TechnologyType)
                    {
                        case TechnologyType.General:                            
                        case TechnologyType.Colonization:
                        case TechnologyType.Economic:
                        case TechnologyType.Industry:
                        case TechnologyType.Research:
                        case TechnologyType.GroundCombat:
                            continue;
                        case TechnologyType.ShipHull:
                            break;
                        case TechnologyType.ShipDefense:
                            break;
                        case TechnologyType.ShipWeapons:
                            break;
                        case TechnologyType.ShipGeneral:
                            break;
                        default:
                            break;
                    }
                    useableTech.Add(bestshiptech.UID); 
                }
                
                
                //now look through are cheapest to research designs that get use closer to the goal ship using pretty much the same logic. 

                bool shipchange = false;
                bool hullKnown =true;
               // do
                {
                    foreach (Ship shortTermBest in ResourceManager.ShipsDict.Values.OrderBy(tech => tech.shipData.TechScore)) //.OrderBy(orderbytech => orderbytech.shipData.TechScore))
                    {
                        try
                        {
                            if (shortTermBest.shipData.HullRole < ShipData.RoleName.fighter || shortTermBest.shipData.Role == ShipData.RoleName.prototype)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }
                        
                        if (shortTermBest.shipData.ShipStyle != this.empire.data.Traits.ShipType) // && (!this.empire.GetHDict().TryGetValue(shortTermBest.shipData.Hull, out empirehulldict) || !empirehulldict))
                        {
                            continue;
                        }
                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                            continue;
                        if (this.empire.ShipsWeCanBuild.Contains(shortTermBest.Name))
                            continue;
                        if (!this.shipIsGoodForGoals(shortTermBest))
                            continue;
                        if (shortTermBest.shipData.techsNeeded.Intersect(useableTech).Count() == 0)
                            continue;

                        if (shortTermBest.shipData.techsNeeded.Count == 0)
                        {
                            if (Empire.Universe.Debug)
                            {
                                Log.Info(this.empire.data.PortraitName + " : no techlist :" + shortTermBest.Name);
                            }
                            continue;
                        }

                        //try to line focus to main goal but if we cant, line focus as best as possible by what we already have. 

                        //Array<string> TechsNeeded =new Array<string>(shortTermBest.shipData.techsNeeded.Except(this.empire.ShipTechs));                        
                        //int techdifference = shortTermBest.shipData.techsNeeded.Intersect(this.empire.ShipTechs).Count();
                        int mod = 0;
                        //shortTermBest.shipData.techsNeeded.Intersect(useableTech).Count();
                        if (!this.empire.canBuildBombers && shortTermBest.BombBays.Count > 0)
                        {
                            mod = (int)ResourceManager.TechTree.Values.Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech.ModulesUnlocked
                                .Where(modu => ResourceManager.GetModuleTemplate(modu.ModuleUID).ModuleType == ShipModuleType.Bomb).Count() > 0).Sum(tech => tech.Cost);
                        }
                        if (!this.empire.canBuildCarriers && shortTermBest.GetHangars().Count > 0)
                            mod = (int)ResourceManager.TechTree.Values.Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech.ModulesUnlocked
                                .Where(modu => ResourceManager.GetModuleTemplate(modu.ModuleUID).ModuleType == ShipModuleType.Hangar).Count() > 0).Sum(tech => tech.Cost);
                        if (!this.empire.canBuildTroopShips && shortTermBest.hasAssaultTransporter || shortTermBest.hasOrdnanceTransporter || shortTermBest.hasRepairBeam || shortTermBest.HasRepairModule || shortTermBest.HasSupplyBays || shortTermBest.hasTransporter || shortTermBest.InhibitionRadius > 0)
                            mod = (int)ResourceManager.TechTree.Values.Where(tech => shortTermBest.shipData.techsNeeded.Contains(tech.UID) && tech.ModulesUnlocked
                                .Where(modu => {
                                    ShipModuleType test = ResourceManager.GetModuleTemplate(modu.ModuleUID).ModuleType;
                                    return (test == ShipModuleType.Troop || test == ShipModuleType.Transporter || test == ShipModuleType.Hangar);                                        
                                }
                                    ).Count() > 0).Sum(tech => tech.Cost);
                        if (!this.empire.canBuildFrigates && shortTermBest.shipData.HullRole == ShipData.RoleName.cruiser)
                            continue;
                        Array<string> currentTechs =new Array<string>(shortTermBest.shipData.techsNeeded.Except(this.empire.ShipTechs));
                        int currentTechCost = (int)ResourceManager.TechTree.Values.Where(tech => currentTechs.Contains(tech.UID)).Sum(tech => tech.Cost);
                        currentTechCost -= mod;
                       
                        currentTechCost = currentTechCost / (int)(this.empire.Research * 10 +1);                        
                        //if (techratio < (.1f * hullScaler) + (mod * .1f) && techratio > techcost)// && realstr > .75f && realTechCost <1.25) //techratio <= .3f && 
                        if ((currentTechCost < techcost && str < shortTermBest.shipData.BaseStrength) || techcost == -1)
                        {
                            if(techcost >0)
                            str = shortTermBest.shipData.BaseStrength;
                            this.BestCombatShip = shortTermBest;
                            techcost = currentTechCost;// techratio;
                            shipchange = true;
                            continue;
                        }



                    }

                    if (shipchange)
                    {
                        if (Empire.Universe.Debug)
                        {
                            Log.Info(this.empire.data.PortraitName + " : NewBestShip :" + this.BestCombatShip.Name + " : " + this.BestCombatShip.shipData.HullRole.ToString());
                        }

                    }
                    
                    if (this.BestCombatShip != null && this.empire.GetHDict().TryGetValue(this.BestCombatShip.shipData.Hull, out hullKnown))
                    {
                        if (hullKnown)
                            hullScaler++;
                    }
                    else
                        hullScaler ++;

                    //End of line focusing. 
                } ///while (this.BestCombatShip == null && hullScaler < 10 );
                if (!hullKnown)
                    hullScaler = 1;
            }



            //now that we have a target ship to buiild filter out all the current techs that are not needed to build it. 
            Array<Technology> bestShiptechs = new Array<Technology>();
            if ((modifier.Contains("ShipWeapons") || modifier.Contains("ShipDefense") || modifier.Contains("ShipGeneral")
                || modifier.Contains("ShipHull")))
            {
                if (this.BestCombatShip != null)
                {
                    //command2 = "SHIPTECH"; //use the shiptech choosers which just chooses tech in the list. 
                    foreach (string shiptech in this.BestCombatShip.shipData.techsNeeded)
                    {
                        Technology test = null;
                        if (ResourceManager.TechTree.TryGetValue(shiptech, out test))
                        {


                            bool skiprepeater = false;
                            //repeater compensator. This needs some deeper logic. I current just say if you research one level. Dont research any more.
                            if (test.MaxLevel > 0)
                            {
                                foreach (TechEntry repeater in this.empire.TechnologyDict.Values)
                                {
                                    if (test.UID == repeater.UID && (repeater.Level > 0))
                                    {
                                        skiprepeater = true;
                                        break;
                                    }
                                }
                                if (skiprepeater)
                                    continue;
                            }
                            bestShiptechs.Add(test);
                        }
                    }

                    bestShiptechs = AvailableTechs.Intersect(bestShiptechs).ToArrayList();
                }
                else
                    Log.Info(this.empire.data.PortraitName + " : NoShipFound :" + hullScaler + " : " );
            }
            HashSet<Technology> remove = new HashSet<Technology>();
            foreach (Technology test in AvailableTechs)
            {
                if (test.MaxLevel > 1)
                {
                    bool skiprepeater = false;
                    foreach (TechEntry repeater in this.empire.TechnologyDict.Values)
                    {
                        if (test.UID == repeater.UID && repeater.Level > 0)
                        {
                            skiprepeater = true;
                            remove.Add(test);
                            break;
                        }
                    }
                    if (skiprepeater)
                        continue;
                }


            }
            
            AvailableTechs = AvailableTechs.Except(remove).ToArrayList();
            Array<Technology> workingSetoftechs = AvailableTechs;
#endregion
            float CostNormalizer = .01f;
            int previousCost = int.MaxValue;
            switch (command2)
            {
                 
                case "TECH":
                    {
                        string[] script = modifier.Split(':');
                        for (int i = 1; i < script.Count(); i++)
                        {
                            try
                            {

                                techtype = (TechnologyType)Enum.Parse(typeof(TechnologyType), script[i]);
                            }
                            catch
                            {
                                //techtype = (TechnologyType)Enum.Parse(typeof(TechnologyType), "General");
                                return false;
                            }
                            if (this.empire.data.Traits.Cybernetic > 0 && techtype == (TechnologyType)Enum.Parse(typeof(TechnologyType), "Colonization")) //this.empire.GetBDict()["Biospheres"] &&
                            {
                                //techtype = TechnologyType.Industry;
                                continue;
                            }
                            if (techtype < TechnologyType.ShipHull)
                            {
                                AvailableTechs = workingSetoftechs;
                            }
                            else
                            {
                                AvailableTechs = bestShiptechs;
                            }
                            Technology ResearchTech = null;
                            if (command1 == "CHEAPEST")
                                ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype).OrderBy(cost => cost.Cost).FirstOrDefault();
                            else if (command1 == "EXPENSIVE")
                                ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype).OrderByDescending(cost => cost.Cost).FirstOrDefault();
                            //AvailableTechs.Where(econ => econ.TechnologyType == techtype).FirstOrDefault();
                            if (ResearchTech == null)
                                continue;
                            if (this.empire.Research > 30 && ResearchTech.Cost > this.empire.Research * 1000 && AvailableTechs.Count > 1)
                                continue;

                            if (techtype == TechnologyType.Economic)
                            {
                                if (ResearchTech.HullsUnlocked.Count > 0)
                                {
                                    //money = this.empire.EstimateIncomeAtTaxRate(.25f);
                                    if (moneyNeeded < 5f)
                                    {
                                        if (command1 == "CHEAPEST")
                                            ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype && econ != ResearchTech).OrderBy(cost => cost.Cost).FirstOrDefault();
                                        else if (command1 == "EXPENSIVE")
                                            ResearchTech = AvailableTechs.Where(econ => econ.TechnologyType == techtype && econ != ResearchTech).OrderByDescending(cost => cost.Cost).FirstOrDefault();

                                        if (ResearchTech == null)
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }

                            string Testresearchtopic = ResearchTech.UID;//AvailableTechs.Where(econ => econ.TechnologyType == techtype).OrderByDescending(cost => cost.Cost).FirstOrDefault().UID;
                            if (string.IsNullOrEmpty(researchtopic))
                                researchtopic = Testresearchtopic;
                            else
                            {
                                int currentCost = (int)(ResearchTech.Cost * CostNormalizer);
                                //int previousCost = (int)(ResourceManager.TechTree[researchtopic].Cost * CostNormalizer);

                                if (this.BestCombatShip != null && (techtype != TechnologyType.ShipHull && //techtype == TechnologyType.ShipHull ||//
                                    ResearchTech.ModulesUnlocked.Count > 0 || ResourceManager.TechTree[researchtopic].ModulesUnlocked.Count > 0))
                                {

                                    Technology PreviousTech = ResourceManager.TechTree[researchtopic];
                                    //Ship 
                                    Ship ship = this.BestCombatShip;
                                    //if (ship.shipData.techsNeeded.Contains(PreviousTech.UID))
                                    //    previousCost = (int)(previousCost * .5f);
                                    if (ship.shipData.techsNeeded.Contains(ResearchTech.UID))
                                        currentCost = (int)(currentCost * .5f);



                                }

                                if (command1 == "CHEAPEST" && currentCost < previousCost)
                                {
                                    researchtopic = Testresearchtopic;
                                    previousCost = currentCost;
                                    CostNormalizer += .01f;
                                }
                                else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                                    researchtopic = Testresearchtopic;
                                
                                
                            }

                        }

                        break;
                    }
                case "SHIPTECH":
                    {
                        if (this.BestCombatShip == null)
                            return false;
                        Ship ship = this.BestCombatShip;
                        Technology shiptech = AvailableTechs
                            .Where(uid => ship.shipData.techsNeeded.Contains(uid.UID)).OrderBy(techscost => techscost.Cost).FirstOrDefault();
                        if (shiptech == null)
                        {
                            //Log.Info(this.BestCombatShip.Name);
                            //foreach (string Bestshiptech in ship.shipData.techsNeeded) //.techsNeeded.Where(uid => !ship.shipData.techsNeeded.Contains(uid.UID))
                            //{
                            //    if (unlockedTech.Contains(Bestshiptech))
                            //        //|| AvailableTechs.Where(uid => uid.UID == Bestshiptech).Count()>0)
                            //        continue;
                            //    Log.Info("Missing Tech: " + Bestshiptech);
                            //}
                            return false;
                        }
                        researchtopic = shiptech.UID;

                        break;
                    }


                default:
                    {
                        try
                        {

                            techtype = (TechnologyType)Enum.Parse(typeof(TechnologyType), command2);
                            //Log.Info(this.EmpireName + " : " + techtype.ToString());

                        }
                        catch
                        {
                            this.res_strat = GSAI.ResearchStrategy.Random;
                            this.RunResearchPlanner();
                            this.res_strat = GSAI.ResearchStrategy.Scripted;
                            researchtopic = this.empire.ResearchTopic;
                            break;
                        }



                        //This should fix issue 414, but someone else will need to verify it
                        // Allium Sativum
                        Technology ResearchTech = null;
                        ResearchTech = AvailableTechs.OrderByDescending(econ => econ.TechnologyType == techtype).ThenBy(cost => cost.Cost).FirstOrDefault();
                        if (ResearchTech != null)
                        {
                            researchtopic = ResearchTech.UID;
                            break;
                        }
                        //float netresearch =this.empire.GetPlanets().Where(owner => owner.Owner == this.empire).Sum(research => research.NetResearchPerTurn);
                        //netresearch = netresearch == 0 ? 1 : netresearch;
                        //if (ResourceManager.TechTree[researchtopic].Cost / netresearch < 500 )
                        researchtopic = null;
                        break;
                    }
            }
            {
                this.empire.ResearchTopic = researchtopic;
            }
            // else
            {
                // researchtopic = AvailableTechs.OrderBy(cost => cost.Cost).First().UID;
            }



            if (string.IsNullOrEmpty(this.empire.ResearchTopic))
                return false;
            else
            {
                //try
                //{
                //    if (ResourceManager.TechTree[this.empire.ResearchTopic].TechnologyType == TechnologyType.ShipHull)
                //    {
                //        this.BestCombatShip = "";
                //    }
                //}
                //catch(Exception e)
                //{
                //    e.Data.Add("Tech Name(UID)", this.empire.ResearchTopic);

                //}
                //Log.Info(this.EmpireName + " : " + ResourceManager.TechTree[this.empire.ResearchTopic].TechnologyType.ToString() + " : " + this.empire.ResearchTopic);
                return true;
            }



        }

        //public void HullChecker(Ship ship, ShipData currentHull, float moneyNeeded)
        //{
        //    foreach (KeyValuePair<string, Ship> wecanbuildit in ResourceManager.ShipsDict.OrderBy(techcost => techcost.Value.shipData != null ? techcost.Value.shipData.TechScore : 0)) // techcost.Value.BaseStrength)) //techcost.Value.shipData != null ? techcost.Value.shipData.techsNeeded.Count : 0))// // shipData !=null? techcost.Value.shipData.TechScore:0))   //Value.shipData.techsNeeded.Count:0))
        //    {

        //        bool test;

        //        ship = wecanbuildit.Value;
        //        if (ship.shipData == null)
        //            continue;
        //        if (ship.BaseStrength < 1f)
        //            continue;
        //        Ship_Game.ShipRole roles;
        //        if (!ResourceManager.ShipRoles.TryGetValue(ship.shipData.Role, out roles)
        //            || roles == null || roles.Protected
        //            || ship.shipData.Role == ShipData.RoleName.freighter
        //            || ship.shipData.Role == ShipData.RoleName.platform
        //            || ship.shipData.Role == ShipData.RoleName.station
        //            )
        //            continue;

        //        test = false;

        //        if (ship.shipData.ShipStyle != this.empire.data.Traits.ShipType
        //            && (this.empire.GetHDict().TryGetValue(ship.shipData.Hull, out test) && test == true) == false)
        //            continue;
        //        test = false;
        //        if (this.empire.ShipsWeCanBuild.Contains(wecanbuildit.Key))//this.empire.GetHDict()[ship.shipData.Hull] || 
        //            continue;
        //        int techCost = ship.shipData.techsNeeded.Count;
        //        if (techCost == 0)
        //            continue;

        //        int trueTechcost = techCost;
        //        if (currentHull != null && ship.shipData.Hull == currentHull.Hull)
        //        {
        //            techCost -= 1;
        //        }


        //        {

        //            bool hangarflag = false;
        //            bool bombFlag = false;
        //            foreach (ModuleSlot hangar in ship.ModuleSlotList)
        //            {

        //                if (hangar.module.ModuleType == ShipModuleType.Hangar)
        //                {
        //                    hangarflag = true;

        //                }
        //                if (hangar.module.ModuleType == ShipModuleType.Bomb)
        //                    bombFlag = true;
        //                if (bombFlag && hangarflag)
        //                    break;
        //            }
        //            techCost -= bombFlag ? 1 : 0;
        //            techCost -= hangarflag ? 1 : 0;
        //        }

        //        if (ship.Name == this.BestCombatShip)
        //            techCost -= 1;
        //        {
        //            moneyNeeded = ship.GetMaintCost(this.empire);// *5;
        //            bool GeneralTechBlock = false;
        //            foreach (string shipTech in ship.shipData.techsNeeded)
        //            {

        //               // if (!test && !unlockedTech.Contains(shipTech))
        //                {
        //                    test = true;
        //                }

        //                {
        //                    TechEntry generalTech;

        //                    if (this.empire.TechnologyDict.TryGetValue(shipTech, out generalTech))
        //                    {
        //                        Technology hull = generalTech.GetTech();
        //                        if (generalTech.Unlocked)
        //                        {
        //                            techCost--;
        //                            trueTechcost--;
        //                            continue;
        //                        }
        //                        else if ((hull.RaceRestrictions.Count > 0 && hull.RaceRestrictions.Where(restrict => restrict.ShipType == this.empire.data.Traits.ShipType).Count() == 0)
        //                            || (hull.RaceRestrictions.Count == 0 && hull.Secret && !hull.Discovered))
        //                        {
        //                            test = false;
        //                            break;
        //                        }
        //                        else if (generalTech.GetTech().TechnologyType == TechnologyType.ShipGeneral)
        //                        {
        //                            if (!GeneralTechBlock)
        //                            {
        //                                techCost--;
        //                                GeneralTechBlock = true;
        //                            }
        //                            else
        //                                GeneralTechBlock = false;

        //                            continue;
        //                        }

        //                        {


        //                            if (this.empire.data.ShipBudget * .3f < ship.GetMaintCost(this.empire))
        //                            {
        //                                test = false;
        //                                break;
        //                                //techCost += 100;
        //                            }
        //                            if (hull.Cost / (this.empire.Research + 1) > 500)
        //                            {
        //                                techCost += 2; //(int)(hull.Cost / (150 * (this.empire.Research + 1)));
        //                            }
        //                            else if (hull.Cost / (this.empire.Research + 1) > 150)
        //                                techCost += 1;


        //                        }
        //                        if (!test)
        //                            continue;
        //                    }
        //                    else
        //                        test = false;
        //                }
        //            }
        //            if (!test)
        //            {
        //                if (ship.Name == this.BestCombatShip)
        //                    this.BestCombatShip = string.Empty;
        //                ship = null;
        //                continue;
        //            }
        //           // useableTech.AddRange(ship.shipData.techsNeeded);
        //            if (trueTechcost == 0)
        //            {
        //                Log.Info("skipped: " + ship.Name);
        //                if (ship.Name == this.BestCombatShip)
        //                    this.BestCombatShip = string.Empty;
        //                ship = null;
        //                continue;
        //            }
        //            //if (BestShip == null
        //            //    || (BestShip.BaseStrength < ship.BaseStrength
        //            //    && BestShipTechCost >= techCost
        //            //    && BestShip.shipData.techsNeeded.Except(this.empire.ShipTechs).Count() > techCost
        //            //    )) //(string.IsNullOrEmpty(BestShip) ||(bestShipStrength < ship.BaseStrength &&  BestShipTechCost >= techCost)) //BestShipTechCost == 0 ||
        //            //{
        //            //    bestShipStrength = ship.BaseStrength;
        //            //    BestShip = wecanbuildit.Value;
        //            //    BestShipTechCost = techCost;
        //            //    //Log.Info("Choosing Best Ship Tech: " + this.empire.data.Traits.ShipType + " -Ship: " + BestShip + " -TechCount: " + trueTechcost +"/"+techCost);
        //            //}

        //        }
        //    }
        //}





        private void RunWarPlanner()
        {

            float warWeight = 1 + this.empire.getResStrat().ExpansionPriority + this.empire.getResStrat().MilitaryPriority;

            foreach (KeyValuePair<Empire, Relationship> r in this.empire.AllRelations.OrderByDescending(anger =>
           {
               float angerMod = Vector2.Distance(anger.Key.GetWeightedCenter(), this.empire.GetWeightedCenter());
               angerMod = (Empire.Universe.Size.X - angerMod) / UniverseData.UniverseWidth;
               if (anger.Value.AtWar)
                   angerMod *= 100;
               return anger.Value.TotalAnger * angerMod;
           }
                ))
            {
                if (warWeight > 0)

                    if (r.Key.isFaction)
                    {
                        r.Value.AtWar = false;
                    }
                    else
                    {
                        warWeight--;
                        SystemCommander scom;
                        if (r.Value.PreparingForWar)
                        {
                            Array<SolarSystem> s;
                            switch (r.Value.PreparingForWarType)
                            {
                                case WarType.BorderConflict:
                                    Array<Planet> list1 = new Array<Planet>();
                                    s = new Array<SolarSystem>();

                                    IOrderedEnumerable<Planet> orderedEnumerable1 = Enumerable.OrderBy<Planet, float>((IEnumerable<Planet>)r.Key.GetPlanets(), (Func<Planet, float>)(planet => this.GetDistanceFromOurAO(planet) / 150000 + (r.Key.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(planet.ParentSystem, out scom) ? scom.RankImportance : 0)));
                                    for (int index = 0; index < Enumerable.Count<Planet>((IEnumerable<Planet>)orderedEnumerable1); ++index)
                                    {


                                        Planet p = Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable1, index);
                                        if (s.Count > warWeight)
                                            break;

                                        if (!s.Contains(p.ParentSystem))
                                        { s.Add(p.ParentSystem); }

                                        list1.Add(p);

                                        //list1.Add(Enumerable.ElementAt<Planet>((IEnumerable<Planet>)orderedEnumerable1, index));
                                        //if (index == 2)
                                        //    break;
                                    }
                                    foreach (Planet planet in list1)
                                    {
                                        bool assault = true;
                                        bool claim = false;
                                        bool claimPresent = false;
                                        //this.TaskList.thisLock.EnterReadLock();
                                        {
                                            //foreach (MilitaryTask item_0 in (Array<MilitaryTask>)this.TaskList)
                                            this.TaskList.ForEach(item_0 =>
                                            {
                                                //if (!assault)
                                                //    return;
                                                if (item_0.GetTargetPlanet() == planet && item_0.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    assault = false;
                                                }
                                                if (item_0.GetTargetPlanet() == planet && item_0.type == MilitaryTask.TaskType.DefendClaim)
                                                {
                                                    if (item_0.Step == 2)
                                                        claimPresent = true;
                                                    //if (s.Contains(current.ParentSystem))
                                                    //    s.Remove(current.ParentSystem);
                                                    claim = true;
                                                }


                                            }, false, false, false);
                                        }
                                        //this.TaskList.thisLock.ExitReadLock();
                                        if (assault && claimPresent)
                                        {
                                            TaskList.Add(new MilitaryTask(planet, empire));
                                        }
                                        if (!claim)
                                        {
                                            MilitaryTask task = new MilitaryTask()
                                            {
                                                AO = planet.Position
                                            };
                                            task.SetEmpire(this.empire);
                                            task.AORadius = 75000f;
                                            task.SetTargetPlanet(planet);
                                            task.TargetPlanetGuid = planet.guid;
                                            task.type = MilitaryTask.TaskType.DefendClaim;
                                            TaskList.Add(task);
                                        }
                                    }
                                    break;
                                case WarType.ImperialistWar:
                                    Array<Planet> list2 = new Array<Planet>();
                                    s = new Array<SolarSystem>();
                                    IOrderedEnumerable<Planet> orderedEnumerable2 = r.Key.GetPlanets().OrderBy( 
                                        (planet => GetDistanceFromOurAO(planet) / 150000 + 
                                        (r.Key.GetGSAI().DefensiveCoordinator.DefenseDict.TryGetValue(planet.ParentSystem, out scom) ? scom.RankImportance : 0)));
                                    for (int index = 0; index < Enumerable.Count(orderedEnumerable2); ++index)
                                    {
                                        Planet p = Enumerable.ElementAt(orderedEnumerable2, index);
                                        if (s.Count > warWeight)
                                            break;

                                        if (!s.Contains(p.ParentSystem))
                                        { s.Add(p.ParentSystem); }
                                        //if (s.Count > 2)
                                        //    break;
                                        list2.Add(p);

                                    }
                                    foreach (Planet planet in list2)
                                    {
                                        bool flag = true;
                                        bool claim = false;
                                        bool claimPresent = false;
                                        //this.TaskList.thisLock.EnterReadLock();
                                        {
                                            // foreach (MilitaryTask item_1 in (Array<MilitaryTask>)this.TaskList)
                                            this.TaskList.ForEach(item_1 =>
                                            {
                                                if (!flag && claim)
                                                    return;
                                                if (item_1.GetTargetPlanet() == planet && item_1.type == MilitaryTask.TaskType.AssaultPlanet)
                                                {
                                                    flag = false;

                                                }
                                                if (item_1.GetTargetPlanet() == planet && item_1.type == MilitaryTask.TaskType.DefendClaim)
                                                {
                                                    if (item_1.Step == 2)
                                                        claimPresent = true;

                                                    claim = true;
                                                }
                                            }, false, false, false);
                                        }
                                        //  this.TaskList.thisLock.ExitReadLock();
                                        if (flag && claimPresent)
                                        {
                                            TaskList.Add(new MilitaryTask(planet, empire));
                                        }
                                        if (!claim)
                                        {
                                            // @todo This is repeated everywhere. Might cut down a lot of code by creating a function

                                            //public MilitaryTask(Vector2 location, float radius, Array<Goal> GoalsToHold, Empire Owner)
                                            MilitaryTask task = new MilitaryTask()
                                            {
                                                AO = planet.Position
                                            };
                                            task.SetEmpire(this.empire);
                                            task.AORadius = 75000f;
                                            task.SetTargetPlanet(planet);
                                            task.TargetPlanetGuid = planet.guid;
                                            task.type = MilitaryTask.TaskType.DefendClaim;
                                            task.EnemyStrength = 0;
                                            //lock (GlobalStats.TaskLocker)
                                            {
                                                TaskList.Add(task);
                                            }
                                        }
                                    }
                                    break;

                            }
                        }
                        if (r.Value.AtWar)
                        {
                            // int num = (int)this.empire.data.difficulty;
                            this.FightDefaultWar(r);
                        }
                    }
                //warWeight--;
            }
        }

        public void SetAlliance(bool ally)
        {
            if (ally)
            {
                this.empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = true;
                this.empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_Alliance = true;
                Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_OpenBorders = true;
                return;
            }
            empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_Alliance = false;
            empire.GetRelations(Empire.Universe.PlayerEmpire).Treaty_OpenBorders = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_Alliance = false;
            Empire.Universe.PlayerEmpire.GetRelations(this.empire).Treaty_OpenBorders = false;
        }

        public void SetAlliance(bool ally, Empire them)
        {
            if (ally)
            {
                this.empire.GetRelations(them).Treaty_Alliance = true;
                this.empire.GetRelations(them).Treaty_OpenBorders = true;
                them.GetRelations(this.empire).Treaty_Alliance = true;
                them.GetRelations(this.empire).Treaty_OpenBorders = true;
                return;
            }
            this.empire.GetRelations(them).Treaty_Alliance = false;
            this.empire.GetRelations(them).Treaty_OpenBorders = false;
            them.GetRelations(this.empire).Treaty_Alliance = false;
            them.GetRelations(this.empire).Treaty_OpenBorders = false;
        }

        public void TriggerRefit()
        {

            bool TechCompare(int[] original, int[] newTech)
            {
                bool compare(int o, int n) => o > 0 && o > n;
                
                for (int x = 0; x < 4; x++)                
                    if (!compare(original[x], newTech[x])) return false;
                
                return true;
            }

            int upgrades = 0;
            var offPool =empire.GetShipsFromOffensePools();
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (upgrades < 5)
                {
                    int techScore = ship.GetTechScore(out int[] origTechs);
                    string name = "";

                    foreach (string shipName in empire.ShipsWeCanBuild)
                    {
                        Ship newTemplate = ResourceManager.GetShipTemplate(shipName);
                        if (newTemplate.GetShipData().Hull != ship.GetShipData().Hull)                                                        
                            continue;
                        int newScore =newTemplate.GetTechScore(out int[] newTech);

                        if(newScore <= techScore || !TechCompare(origTechs, newTech)) continue;                        

                        name      = shipName;
                        techScore = newScore;
                        origTechs = newTech;
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        ship.AI.OrderRefitTo(name);
                        ++upgrades;
                    }                    
                }
                else
                    break;
            }
        }

        public void Update()
        {		    
            DefStr = this.DefensiveCoordinator.GetForcePoolStrength();
            if (!this.empire.isFaction)
            {
                this.RunManagers();
            }
            foreach (Goal g in this.Goals)
            //Parallel.ForEach(this.Goals, g =>
            {
                g.Evaluate();
            }//);
            this.Goals.ApplyPendingRemovals();
        }


        public struct PeaceAnswer
        {
            public string answer;
            public bool peace;
        }

        private enum ResearchStrategy
        {
            Random,
            Scripted
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GSAI() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            TaskList?.Dispose(ref TaskList);
            DefensiveCoordinator?.Dispose(ref DefensiveCoordinator);
            Goals?.Dispose(ref Goals);
        }
    }
}