﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.AI.Tasks;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class LoadUniverseScreen
    {
        static void RestoreCommodities(Planet p, SavedGame.PlanetSaveData psdata)
        {
            p.FoodHere = psdata.foodHere;
            p.ProdHere = psdata.prodHere;
            p.Population = psdata.Population;
        }

        static Empire CreateEmpireFromEmpireSaveData(SavedGame.EmpireSaveData sdata, bool isPlayer)
        {
            var e = new Empire();
            e.isPlayer = isPlayer;
            //TempEmpireData  Tdata = new TempEmpireData();

            e.isFaction = sdata.IsFaction;
            if (sdata.empireData == null)
            {
                e.data.Traits = sdata.Traits;
                e.EmpireColor = sdata.Traits.Color;
            }
            else
            {
                e.data = sdata.empireData;
                e.data.ResearchQueue = sdata.empireData.ResearchQueue;
                e.Research.SetTopic(sdata.ResearchTopic);
                e.PortraitName = e.data.PortraitName;
                e.dd           = ResourceManager.GetDiplomacyDialog(e.data.DiplomacyDialogPath);
                e.EmpireColor  = e.data.Traits.Color;
                e.RestoreMoneyHistoryFromSave(sdata);
                e.data.CurrentAutoScout     = sdata.CurrentAutoScout     ?? e.data.ScoutShip;
                e.data.CurrentAutoColony    = sdata.CurrentAutoColony    ?? e.data.ColonyShip;
                e.data.CurrentAutoFreighter = sdata.CurrentAutoFreighter ?? e.data.FreighterShip;
                e.data.CurrentConstructor   = sdata.CurrentConstructor   ?? e.data.ConstructorShip;

                e.IncreaseFastVsBigFreighterRatio(sdata.FastVsBigFreighterRatio - e.FastVsBigFreighterRatio);
                if (sdata.empireData.DefaultTroopShip.IsEmpty())
                    e.data.DefaultTroopShip = e.data.PortraitName + " " + "Troop";

                e.SetAverageFreighterCargoCap(sdata.AverageFreighterCargoCap);
            }

            foreach (TechEntry tech in sdata.TechTree)
            {
                if (ResourceManager.TryGetTech(tech.UID, out _))
                {
                    tech.ResolveTech();
                    e.TechnologyDict.Add(tech.UID, tech);
                }
                else Log.Warning($"LoadTech ignoring invalid tech: {tech.UID}");
            }
            
            e.InitializeFromSave();
            e.Money = sdata.Money;
            e.GetEmpireAI().AreasOfOperations = sdata.AOs;
            e.GetEmpireAI().ExpansionAI.SetExpandSearchTimer(sdata.ExpandSearchTimer);
            e.GetEmpireAI().ExpansionAI.SetMaxSystemsToCheckedDiv(sdata.MaxSystemsToCheckedDiv.LowerBound(1));
            e.RestoreUnserializableDataFromSave();

            if (e.WeArePirates)
                e.Pirates.RestoreFromSave(sdata);

            return e;
        }

        static Planet CreatePlanetFromPlanetSaveData(SolarSystem forSystem, SavedGame.PlanetSaveData psdata)
        {
            var p = new Planet
            {
                ParentSystem = forSystem,
                guid = psdata.guid,
                Name = psdata.Name,
                OrbitalAngle = psdata.OrbitalAngle
            };

            if (psdata.Owner.NotEmpty())
            {
                p.Owner = EmpireManager.GetEmpireByName(psdata.Owner);
                p.Owner.AddPlanet(p);
            }

            if (psdata.SpecialDescription.NotEmpty())
                p.SpecialDescription = psdata.SpecialDescription;

            p.RestorePlanetTypeFromSave(psdata.WhichPlanet);
            p.Scale = psdata.Scale > 0f ? psdata.Scale : RandomMath.RandomBetween(1f, 2f);
            p.colonyType         = psdata.ColonyType;
            p.GovOrbitals        = psdata.GovOrbitals;
            p.AutoBuildTroops    = psdata.GovMilitia;
            p.GarrisonSize       = psdata.GarrisonSize;
            p.Quarantine         = psdata.Quarantine;
            p.DontScrapBuildings = psdata.DontScrapBuildings;
            p.NumShipyards       = psdata.NumShipyards;
            p.FS                 = psdata.FoodState;
            p.PS                 = psdata.ProdState;
            p.Food.PercentLock   = psdata.FoodLock;
            p.Prod.PercentLock   = psdata.ProdLock;
            p.Res.PercentLock    = psdata.ResLock;
            p.OrbitalRadius      = psdata.OrbitalDistance;
            p.BasePopPerTile     = psdata.BasePopPerTile;

            p.SetBaseFertility(psdata.Fertility, psdata.MaxFertility);

            p.MineralRichness       = psdata.Richness;
            p.HasRings              = psdata.HasRings;
            p.ShieldStrengthCurrent = psdata.ShieldStrength;
            p.CrippledTurns         = psdata.Crippled_Turns;
            p.PlanetTilt            = RandomMath.RandomBetween(45f, 135f);
            p.ObjectRadius          = 1000f * (float)(1 + (Math.Log(p.Scale) / 1.5));
            p.UpdateTerraformPoints(psdata.TerraformPoints);
            p.RestoreBaseFertilityTerraformRatio(psdata.BaseFertilityTerraformRatio);
            foreach (Guid guid in psdata.StationsList)
                p.OrbitalStations[guid] = null; // reserve orbital stations (and platforms)

            p.SetWorkerPercentages(psdata.farmerPercentage, psdata.workerPercentage, psdata.researcherPercentage);
            if (p.HasRings)
                p.RingTilt = RandomMath.RandomBetween(-80f, -45f);

            foreach (SavedGame.PGSData d in psdata.PGSList)
            {
                var pgs = new PlanetGridSquare(d.x, d.y, d.building, d.Habitable)
                {
                    Biosphere = d.Biosphere
                };
                if (pgs.Biosphere)
                    p.BuildingList.Add(ResourceManager.CreateBuilding(Building.BiospheresId));

                p.TilesList.Add(pgs);
                foreach (Troop t in d.TroopsHere)
                {
                    if (!ResourceManager.TroopTypes.Contains(t.Name))
                        continue;
                    var fix = ResourceManager.GetTroopTemplate(t.Name);
                    t.first_frame = fix.first_frame;
                    t.WhichFrame = fix.first_frame;
                    p.AddTroop(t, pgs);
                }

                if (pgs.building == null)
                    continue;

                var template = ResourceManager.GetBuildingTemplate(pgs.building.Name);
                pgs.building.AssignBuildingId(template.BID);
                pgs.building.Scrappable = template.Scrappable;
                pgs.building.CalcMilitaryStrength();
                p.BuildingList.Add(pgs.building);
                p.AddBuildingsFertility(pgs.building.MaxFertilityOnBuild);
            }
            return p;
        }

        SolarSystem CreateSystemFromData(SavedGame.SolarSystemSaveData ssd)
        {
            var system = new SolarSystem
            {
                guid          = ssd.guid,
                Name          = ssd.Name,
                Position      = ssd.Position,
                Sun           = SunType.FindSun(ssd.SunPath), // old SunPath is actually the ID @todo RENAME
            };

            system.SetPiratePresence(ssd.PiratePresence);
            system.AsteroidsList.AddRange(ssd.AsteroidsList);
            system.MoonList.AddRange(ssd.Moons);
            system.SetExploredBy(ssd.ExploredBy);
            system.RingList = new Array<SolarSystem.Ring>();
            foreach (SavedGame.RingSave ring in ssd.RingList)
            {
                if (ring.Asteroids)
                {
                    system.RingList.Add(new SolarSystem.Ring
                    {
                        Asteroids = true,
                        OrbitalDistance = ring.OrbitalDistance
                    });
                }
                else
                {
                    Planet p = CreatePlanetFromPlanetSaveData(system, ring.Planet);
                    p.Center = system.Position.PointFromAngle(p.OrbitalAngle, p.OrbitalRadius);
                    
                    foreach (Building b in p.BuildingList)
                    {
                        if (!b.IsSpacePort)
                            continue;
                        p.Station = new SpaceStation(p);
                        p.Station.LoadContent(ScreenManager);
                        p.HasSpacePort = true;
                    }
                    
                    if (p.Owner != null && !system.OwnerList.Contains(p.Owner))
                    {
                        system.OwnerList.Add(p.Owner);
                    }
                    system.PlanetList.Add(p);
                    p.SetExploredBy(ssd.ExploredBy);

                    system.RingList.Add(new SolarSystem.Ring
                    {
                        planet    = p,
                        Asteroids = false
                    });
                    p.UpdateIncomes(true);  // must be before restoring commodities since max storage is set here           
                    RestoreCommodities(p, ring.Planet);
                }
            }
            return system;
        }

        static void CreateSpaceRoads(UniverseData data, SavedGame.EmpireSaveData d, Empire e)
        {
            e.SpaceRoadsList = new Array<SpaceRoad>();
            foreach (SavedGame.SpaceRoadSave roadSave in d.SpaceRoadData)
            {
                var road = new SpaceRoad();
                foreach (SolarSystem s in data.SolarSystemsList)
                {
                    if (roadSave.OriginGUID == s.guid) road.Origin = s;
                    if (roadSave.DestGUID == s.guid)   road.Destination = s;
                }

                foreach (SavedGame.RoadNodeSave roadNode in roadSave.RoadNodes)
                {
                    var node = new RoadNode();
                    data.FindShip(roadNode.Guid_Platform, out node.Platform);
                    node.Position = roadNode.Position;
                    road.RoadNodesList.Add(node);
                }

                e.SpaceRoadsList.Add(road);
            }
        }
        
        static void CreateShipFromSave(UniverseData data, SavedGame.ShipSaveData shipSave, Empire e)
        {
            Ship ship = Ship.CreateShipFromSave(e, shipSave);
            if (ship == null) // happens if module creation failed
                return;

            e.AddShip(ship);
            data.MasterShipList.Add(ship);
        }

        
        static void RestorePlanetConstructionQueue(SavedGame.UniverseSaveData saveData, SavedGame.RingSave rsave, Planet p)
        {
            foreach (SavedGame.QueueItemSave qisave in rsave.Planet.QISaveList)
            {
                var qi = new QueueItem(p);
                if (qisave.isBuilding)
                {
                    qi.isBuilding    = true;
                    qi.IsMilitary    = qisave.IsMilitary;
                    qi.Building      = ResourceManager.CreateBuilding(qisave.UID);
                    qi.Cost          = qi.Building.ActualCost;
                    qi.NotifyOnEmpty = false;
                    qi.IsPlayerAdded = qisave.isPlayerAdded;

                    foreach (PlanetGridSquare pgs in p.TilesList)
                    {
                        if (pgs.x != (int) qisave.pgsVector.X || pgs.y != (int) qisave.pgsVector.Y)
                            continue;

                        pgs.QItem = qi;
                        qi.pgs    = pgs;
                        break;
                    }
                }

                if (qisave.isTroop)
                {
                    qi.isTroop = true;
                    qi.TroopType = qisave.UID;
                    qi.Cost = ResourceManager.GetTroopCost(qisave.UID);
                    qi.NotifyOnEmpty = false;
                }

                if (qisave.isShip)
                {
                    qi.isShip = true;
                    if (!ResourceManager.ShipsDict.ContainsKey(qisave.UID))
                        continue;

                    Ship shipTemplate  = ResourceManager.GetShipTemplate(qisave.UID);
                    qi.sData           = shipTemplate.shipData;
                    qi.DisplayName     = qisave.DisplayName;
                    qi.Cost            = qisave.Cost;
                    qi.TradeRoutes     = qisave.TradeRoutes;
                    qi.TransportingColonists  = qisave.TransportingColonists;
                    qi.TransportingFood       = qisave.TransportingFood;
                    qi.TransportingProduction = qisave.TransportingProduction;
                    qi.AllowInterEmpireTrade  = qisave.AllowInterEmpireTrade;
                        
                    if (qisave.AreaOfOperation != null)
                    {
                        foreach (Rectangle aoRect in qisave.AreaOfOperation)
                            qi.AreaOfOperation.Add(aoRect);
                    }
                }

                foreach (Goal g in p.Owner.GetEmpireAI().Goals)
                {
                    if (g.guid != qisave.GoalGUID)
                        continue;
                    qi.Goal = g;
                    qi.NotifyOnEmpty = false;
                }

                if (qisave.isShip && qi.Goal != null)
                {
                    qi.Goal.ShipToBuild = ResourceManager.GetShipTemplate(qisave.UID);
                }

                qi.ProductionSpent = qisave.ProgressTowards;
                p.Construction.Enqueue(qi);
            }
        }

        
        static void RestoreSolarSystemCQs(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData sdata in saveData.SolarSystemDataList)
            {
                foreach (SavedGame.RingSave rsave in sdata.RingList)
                {
                    if (rsave.Planet != null)
                    {
                        Planet p = data.FindPlanetOrNull(rsave.Planet.guid);
                        if (p?.Owner != null)
                        {
                            RestorePlanetConstructionQueue(saveData, rsave, p);
                        }
                    }
                }
            }
        }

        
        static void CreateFleetsFromSave(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                foreach (SavedGame.FleetSave fleetsave in d.FleetsList)
                {
                    var fleet = new Fleet
                    {
                        Guid = fleetsave.FleetGuid,
                        IsCoreFleet = fleetsave.IsCoreFleet,
                        // @note savegame compatibility uses facing in radians
                        FinalDirection = fleetsave.facing.RadiansToDirection(),
                        Owner = e
                    };
                    foreach (SavedGame.FleetShipSave ssave in fleetsave.ShipsInFleet)
                    {
                        foreach (Ship ship in data.MasterShipList)
                        {
                            if (ship.guid != ssave.shipGuid)
                                continue;

                            // fleet saves can be corrupted because in older saves,
                            // so for avoiding bugs, don't add ship to the same fleet twice
                            // @todo @hack This "Core Fleet" stuff is just a temp hack, please solve this issue
                            if (ship.fleet == fleet ||
                                ship.fleet != null && (fleet.Name.IsEmpty() || fleet.Name == "Core Fleet"))
                                continue;

                            ship.RelativeFleetOffset = ssave.fleetOffset;
                            fleet.AddShip(ship);
                        }
                    }

                    foreach (FleetDataNode node in fleet.DataNodes)
                    {
                        foreach (Ship ship in fleet.Ships)
                        {
                            if (!(node.ShipGuid != Guid.Empty) || !(ship.guid == node.ShipGuid))
                            {
                                continue;
                            }

                            node.Ship = ship;
                            node.ShipName = ship.Name;
                            break;
                        }
                    }

                    fleet.AssignPositions(fleet.FinalDirection);
                    fleet.Name = fleetsave.Name;
                    fleet.TaskStep = fleetsave.TaskStep;
                    fleet.Owner = e;
                    fleet.FinalPosition = fleetsave.Position;
                    fleet.SetSpeed();
                    e.GetFleetsDict()[fleetsave.Key] = fleet;
                }
            }
        }

        static void CreateAOs(UniverseData data)
        {
            foreach (Empire e in data.EmpireList)
                e.GetEmpireAI().InitializeAOsFromSave();
        }

        static void CreateMilitaryTasks(SavedGame.EmpireSaveData d, Empire e, UniverseData data)
        {
            foreach (MilitaryTask task in d.GSAIData.MilitaryTaskList)
            {
                task.SetEmpire(e);
                if (data.FindPlanet(task.TargetPlanetGuid, out Planet p))
                    task.SetTargetPlanet(p);

                if (data.FindShip(task.TargetShipGuid, out Ship ship))
                    task.SetTargetShip(ship);

                foreach (Guid guid in task.HeldGoals)
                {
                    foreach (Goal g in e.GetEmpireAI().Goals)
                    {
                        if (g.guid == guid)
                        {
                            g.Held = true;
                            break;
                        }
                    }
                }

                if (task.WhichFleet != -1)
                {
                    if (e.GetFleetsDict().TryGetValue(task.WhichFleet, out Fleet fleet))
                        fleet.FleetTask = task;
                    else task.WhichFleet = 0;
                }
            }

            e.GetEmpireAI().ReadFromSave(d.GSAIData);
        }

        static bool IsShipGoalInvalid(SavedGame.GoalSave g)
        {
            if (g.type != GoalType.BuildDefensiveShips &&
                g.type != GoalType.BuildOffensiveShips &&
                g.type != GoalType.IncreaseFreighters)
                return false;
            return g.ToBuildUID != null && !ResourceManager.ShipTemplateExists(g.ToBuildUID);
        }
        
        static void CreateGoals(SavedGame.EmpireSaveData esd, Empire e, UniverseData data)
        {
            foreach (SavedGame.GoalSave gsave in esd.GSAIData.Goals)
            {
                if (IsShipGoalInvalid(gsave))
                    continue;

                Goal g = Goal.Deserialize(gsave.GoalName, e, gsave);
                if (gsave.fleetGuid != Guid.Empty)
                {
                    foreach (KeyValuePair<int, Fleet> fleet in e.GetFleetsDict())
                    {
                        if (fleet.Value.Guid == gsave.fleetGuid) g.Fleet = fleet.Value;
                    }
                }

                foreach (SolarSystem s in data.SolarSystemsList)
                {
                    foreach (Planet p in s.PlanetList)
                    {
                        if (p.guid == gsave.planetWhereBuildingAtGuid) g.PlanetBuildingAt = p;
                        if (p.guid == gsave.markedPlanetGuid) g.ColonizationTarget = p;
                    }
                }

                foreach (Ship s in data.MasterShipList)
                {
                    if      (gsave.colonyShipGuid == s.guid) g.FinishedShip = s;
                    else if (gsave.beingBuiltGUID == s.guid) g.ShipToBuild  = s;
                    else if (gsave.OldShipGuid    == s.guid) g.OldShip      = s;
                    else if (gsave.TargetShipGuid == s.guid) g.TargetShip   = s;
                }

                if (g.type == GoalType.Refit && gsave.ToBuildUID != null)
                {
                    Ship shipToBuild = ResourceManager.GetShipTemplate(gsave.ToBuildUID, false);
                    if (shipToBuild != null)
                        g.ShipToBuild = shipToBuild;
                    else
                        Log.Error($"Could not find ship name {gsave.ToBuildUID} in dictionary when trying to load Refit goal!");
                }

                if (gsave.TargetEmpireId > 0)
                    g.TargetEmpire = EmpireManager.GetEmpireById(gsave.TargetEmpireId);

                g.PostInit();
                e.GetEmpireAI().Goals.Add(g);
            }
        }

        static void CreateShipGoals(SavedGame.EmpireSaveData esd, UniverseData data, Empire e)
        {
            foreach (SavedGame.ShipSaveData shipData in esd.OwnedShips)
            {
                if (!data.FindShip(shipData.guid, out Ship ship))
                    continue;

                if (shipData.AISave.WayPoints != null)
                    ship.AI.SetWayPoints(shipData.AISave.WayPoints);

                foreach (SavedGame.ShipGoalSave sg in shipData.AISave.ShipGoalsList)
                {
                    foreach (SolarSystem s in data.SolarSystemsList)
                    {
                        foreach (Planet p in s.PlanetList)
                        {
                            if (p.guid == sg.TargetPlanetGuid) ship.AI.ColonizeTarget = p;
                        }
                    }

                    if (sg.Plan == ShipAI.Plan.DeployStructure || sg.Plan == ShipAI.Plan.DeployOrbital)
                        ship.IsConstructor = true;

                    ship.AI.AddGoalFromSave(sg, data);
                }
            }
        }

        void CreateEmpires(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                bool isPlayer = d.Traits.Name == PlayerLoyalty;
                Empire e = CreateEmpireFromEmpireSaveData(d, isPlayer);
                data.EmpireList.Add(e);
                if (isPlayer)
                {
                    e.AutoColonize          = saveData.AutoColonize;
                    e.AutoExplore           = saveData.AutoExplore;
                    e.AutoFreighters        = saveData.AutoFreighters;
                    e.AutoPickBestFreighter = saveData.AutoPickBestFreighter;
                    e.AutoBuild             = saveData.AutoProjectors;
                }

                EmpireManager.Add(e);
            }
        }

        static void GiftShipsFromServantEmpire(UniverseData data)
        {
            foreach (Empire e in data.EmpireList)
            {
                if (e.data.AbsorbedBy == null)
                    continue;
                Empire servantEmpire = e;
                Empire masterEmpire = EmpireManager.GetEmpireByName(servantEmpire.data.AbsorbedBy);
                
                masterEmpire.AssimilateTech(servantEmpire);
            }
        }

        
        static void CreateTasksGoalsRoads(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData esd in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(esd.Name);

                CreateSpaceRoads(data, esd, e);
                CreateGoals(esd, e, data);
                e.GetEmpireAI().ThreatMatrix.AddFromSave(esd.GSAIData);
                e.GetEmpireAI().UsedFleets = esd.GSAIData.UsedFleets;
                CreateMilitaryTasks(esd, e, data);
                CreateShipGoals(esd, data, e);
            }
        }

        void CreatePlanetImportExportShipLists(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
                foreach (SavedGame.RingSave ring in ssd.RingList)
                {
                    if (ring.Asteroids)
                        continue;

                    SavedGame.PlanetSaveData savedPlanet = ring.Planet;
                    if (data.FindPlanet(savedPlanet.guid, out Planet planet))
                    {
                        if (savedPlanet.IncomingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.IncomingFreighters)
                            {
                                data.FindShip(freighterGuid, out Ship freighter);
                                planet.AddToIncomingFreighterList(freighter);
                            }
                        }

                        if (savedPlanet.OutgoingFreighters != null)
                        {
                            foreach (Guid freighterGuid in savedPlanet.OutgoingFreighters)
                            {
                                data.FindShip(freighterGuid, out Ship freighter);
                                planet.AddToOutgoingFreighterList(freighter);
                            }
                        }
                    }
                }
        }

        static void CreateAllShips(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.empireData.Traits.Name);
                foreach (SavedGame.ShipSaveData shipData in d.OwnedShips)
                    CreateShipFromSave(data, shipData, e);
            }
        }

        void CreateSolarSystems(SavedGame.UniverseSaveData saveData, UniverseData data)
        {
            foreach (SavedGame.SolarSystemSaveData ssd in saveData.SolarSystemDataList)
            {
                data.SolarSystemsList.Add(CreateSystemFromData(ssd));
            }
        }

        static void CreateRelations(SavedGame.UniverseSaveData saveData)
        {
            foreach (SavedGame.EmpireSaveData d in saveData.EmpireDataList)
            {
                Empire e = EmpireManager.GetEmpireByName(d.Name);
                foreach (Relationship r in d.Relations)
                {
                    Empire empire = EmpireManager.GetEmpireByName(r.Name);
                    e.AddRelationships(empire, r);
                    r.ActiveWar?.SetCombatants(e, empire);
                    r.Risk = new EmpireRiskAssessment(r);
                }
            }
        }
    }
}
