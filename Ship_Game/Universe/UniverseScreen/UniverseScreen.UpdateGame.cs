using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        private void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (true)
            {                
                try
                {
                    // Wait for Draw() to finish. While SwapBuffers is blocking, we process the turns inbetween
                    DrawCompletedEvt.WaitOne();
                    if (ProcessTurnsThread == null)
                        return; // this thread is aborting

                    float deltaTime = (float) zgameTime.ElapsedGameTime.TotalSeconds;
                    PieMenuTimer += deltaTime;
                    pieMenu.Update(zgameTime);

                    if (Paused)
                    {
                        ++FrameId;

                        UpdateAllSystems(0.0f);
                        foreach (Ship ship in MasterShipList)
                        {
                            if (!ship.ShipInitialized) continue;
                            if (ship.UpdateVisibility())
                            {
                                ship.UpdateWorldTransform();
                            }
                            ship.Update(0);
                        }
                    }
                    else
                    {
                        NotificationManager.Update(deltaTime);
                        AutoSaveTimer -= deltaTime;

                        if (AutoSaveTimer <= 0.0f)
                        {
                            AutoSaveTimer = GlobalStats.AutoSaveFreq;
                            DoAutoSave();
                        }
                        if (IsActive)
                        {
                            if (GameSpeed < 1f) //Speed <1.0
                            {
                                if (TurnFlipCounter >= 1)
                                {
                                    TurnFlipCounter = 0;
                                    ++FrameId;
                                    ProcessTurnDelta(deltaTime);
                                }
                                TurnFlipCounter += GameSpeed;
                            }
                            else
                            {
                                // With higher GameSpeed, we take more than 1 turn
                                for (int numTurns = 0; numTurns < GameSpeed && IsActive; ++numTurns)
                                {
                                    ++FrameId;
                                    ProcessTurnDelta(deltaTime);
                                    deltaTime = (float) zgameTime.ElapsedGameTime.TotalSeconds;
                                }
                            }
                            if (GlobalStats.RestrictAIPlayerInteraction)
                            {
                                if (perfavg5.NumSamples > 0 && perfavg5.AvgTime * GameSpeed < 0.05f)
                                    ++GameSpeed;
                                else if (--GameSpeed < 1.0f) GameSpeed = 1.0f;
                                
                            }
                            
                            
                        }
                    }                    
                    failedLoops = 0; // no exceptions this turn                    
                }
                catch (ThreadAbortException)
                {
                    return; // Game over, Make sure to Quit the loop!
                }
                catch (Exception ex)
                {
                    if (++failedLoops > 1)
                    {                        
                        throw; // the loop is having a cyclic crash, no way to recover
                    }
                    
                    Log.Error(ex, "ProcessTurns crashed");
                }
                finally
                {
                    //if the debug window hits a cyclic crash it can be turned off ingame. 
                    // i dont see a point in crashing the game because of a debug window error. 
                    try { DebugWin?.Update(DeltaTime); }
                    catch { Log.Info("DebugWindowCrashed"); }
                    
                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        private void PathGridtranslateBordernode(Empire empire, byte weight, byte[,] grid)
        {
            //this.reducer = (int)(Empire.ProjectorRadius *.5f  );
            int granularity = (int) (UniverseSize / reducer);
            foreach (var node in empire.BorderNodes)
            {
                SolarSystem ss = node.SourceObject as SolarSystem;
                Planet p = node.SourceObject as Planet;
                if (FTLModifier < 1 && ss != null)
                    weight += 20;
                if ((EnemyFTLModifier < 1 || !FTLInNuetralSystems) && ss != null && weight > 1)
                    weight += 20;
                if (p != null && weight > 1)
                    weight += 20;
                float xround = node.Position.X > 0 ? .5f : -.5f;
                float yround = node.Position.Y > 0 ? .5f : -.5f;
                int ocx = (int) (node.Position.X / reducer + xround);
                int ocy = (int) (node.Position.Y / reducer + yround);
                int cx = ocx + granularity;
                int cy = ocy + granularity;
                cy = cy < 0 ? 0 : cy;
                cy = cy > granularity * 2 ? granularity * 2 : cy;
                cx = cx < 0 ? 0 : cx;
                cx = cx > granularity * 2 ? granularity * 2 : cx;
                Vector2 upscale = new Vector2(ocx * reducer,
                    ocy * reducer);
                if (Vector2.Distance(upscale, node.Position) < node.Radius)
                    grid[cx, cy] = weight;
                if (weight > 1 || weight == 0 || node.Radius > empire.ProjectorRadius)
                {
                    float test = node.Radius > empire.ProjectorRadius ? 1 : 2;
                    int rad = (int) (Math.Ceiling(node.Radius / reducer * test));
                    //rad--;

                    int negx = cx - rad;
                    if (negx < 0)
                        negx = 0;
                    int posx = cx + rad;
                    if (posx > granularity * 2)
                        posx = granularity * 2;
                    int negy = cy - rad;
                    if (negy < 0)
                        negy = 0;
                    int posy = cy + rad;
                    if (posy > granularity * 2)
                        posy = granularity * 2;
                    for (int x = negx; x < posx; x++)
                    for (int y = negy; y < posy; y++)
                    {
                        //if (grid[x, y] >= 80 || grid[x, y] <= weight)
                        {
                            upscale = new Vector2((x - granularity) * reducer,
                                (y - granularity) * reducer);
                            if (Vector2.Distance(upscale, node.Position) <= node.Radius * test)
                                grid[x, y] = weight;
                        }
                    }
                }
            }
        }

        public static float DeltaTime;

        private void ProcessTurnDelta(float elapsedTime)
        {
            DeltaTime = elapsedTime;
            perfavg5.Start(); // total dowork perf counter

            GlobalStats.BeamTests     = 0;
            GlobalStats.Comparisons   = 0;
            ++GlobalStats.ComparisonCounter;
            GlobalStats.ModuleUpdates = 0;
            GlobalStats.ModulesMoved  = 0;

            if (ProcessTurnEmpires(elapsedTime))
                return;

            UpdateShipsAndFleets(elapsedTime);

            // this will update all ship Center coordinates
            ProcessTurnShipsAndSystems();

            // update spatial manager after ships have moved.
            // all the collisions will be triggered here:
            SpaceManager.Update(elapsedTime);

            ProcessTurnUpdateMisc(elapsedTime);
            
            // bulk remove all dead projectiles to prevent their update next frame
            ProcessProjectileDeaths();

            perfavg5.Stop();
            Lag = perfavg5.AvgTime;
        }

        private static void ProcessProjectileDeaths(Ship ship)
        {
            for (int i = 0; i < ship.Projectiles.Count; ++i)
            {
                Projectile projectile = ship.Projectiles[i];
                if (projectile.DieNextFrame)
                    projectile.Die(projectile, false);
            }
        }

        private void ProcessProjectileDeaths()
        {
            for (int i = 0; i < DeepSpaceShips.Count; i++)
                ProcessProjectileDeaths(DeepSpaceShips[i]);

            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystem system = SolarSystemList[i];
                for (int j = 0; j < system.ShipList.Count; ++j)
                    ProcessProjectileDeaths(system.ShipList[j]);
            }
        }

        private void ProcessTurnUpdateMisc(float elapsedTime)
        {
            UpdateClickableItems();
            if (LookingAtPlanet)
                workersPanel.Update(elapsedTime);
            bool flag1 = false;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; ++i)
                {
                    ClickablePlanets local_12 = ClickPlanetList[i];
                    if (Vector2.Distance(new Vector2(Mouse.GetState().X, Mouse.GetState().Y),
                            local_12.ScreenPos) <= local_12.Radius)
                    {
                        flag1 = true;
                        TooltipTimer -= 0.01666667f;
                        tippedPlanet = local_12;
                    }
                }
            }
            if (TooltipTimer <= 0f && !LookingAtPlanet)
                TooltipTimer = 0.5f;
            if (!flag1)
            {
                ShowingPlanetToolTip = false;
                TooltipTimer = 0.5f;
            }

            bool flag2 = false;
            if (viewState > UnivScreenState.SectorView)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int local_15 = 0; local_15 < ClickableSystems.Count; ++local_15)
                    {
                        ClickableSystem local_16 = ClickableSystems[local_15];
                        if (Vector2.Distance(new Vector2(Mouse.GetState().X, Mouse.GetState().Y),
                                local_16.ScreenPos) <= local_16.Radius)
                        {
                            sTooltipTimer -= 0.01666667f;
                            tippedSystem = local_16;
                            flag2 = true;
                        }
                    }
                }
                if (sTooltipTimer <= 0f)
                    sTooltipTimer = 0.5f;
            }
            if (!flag2)
                ShowingSysTooltip = false;
            Zrotate += 0.03f * elapsedTime;

            JunkList.ApplyPendingRemovals();

            if (elapsedTime > 0)
            {
                lock (GlobalStats.ExplosionLocker)
                {
                    ExplosionManager.Update(elapsedTime);
                    ExplosionManager.ExplosionList.ApplyPendingRemovals();
                }
                MuzzleFlashManager.Update(elapsedTime);
            }
            lock (GlobalStats.ExplosionLocker)
                MuzzleFlashManager.FlashList.ApplyPendingRemovals();
            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Update(elapsedTime);
            if (elapsedTime > 0)
            {
                using (BombList.AcquireReadLock())
                {
                    for (int local_19 = 0; local_19 < BombList.Count; ++local_19)
                    {
                        Bomb local_20 = BombList[local_19];
                        if (local_20 != null)
                            local_20.Update(elapsedTime);
                    }
                }
                BombList.ApplyPendingRemovals();
            }
            anomalyManager.AnomaliesList.ApplyPendingRemovals();
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            SelectedShipList.ApplyPendingRemovals();
            MasterShipList.ApplyPendingRemovals();
            if (perStarDateTimer <= StarDate)
            {
                perStarDateTimer = StarDate + .1f;
                perStarDateTimer = (float) Math.Round(perStarDateTimer, 1);
                empireShipCountReserve = EmpireManager.Empires.Sum(empire =>
                    {
                        if (empire == player || empire.data.Defeated || empire.isFaction)
                            return 0;
                        return empire.EmpireShipCountReserve;
                    }
                );
                globalshipCount = MasterShipList.FilterBy(ship => (ship.loyalty != null && ship.loyalty != player) &&
                                                                  ship.shipData.Role != ShipData.RoleName.troop &&
                                                                  ship.Mothership == null)
                    .Length;
            }
        }

        private void UpdateShipsAndFleets(float elapsedTime)
        {
            perfavg4.Start();

            if (elapsedTime > 0.0f && shiptimer <= 0.0f)
            {
                shiptimer = 1f;

                // @todo REMOVE THIS LOOP BASED RADIUS CHECKING AND USE QUADTREE INSTEAD
                for (int i = 0; i < MasterShipList.Count; i++)
                {
                    Ship ship = MasterShipList[i];
                    foreach (SolarSystem system in SolarSystemList)
                    {
                        if (ship.Position.InRadius(system.Position, 100000.0f))
                        {
                            system.SetExploredBy(ship.loyalty);
                            ship.SetSystem(system);
                            break; // No need to keep looping through all other systems if one is found -Gretman
                        }
                    }
                    // Add ships to deepspacemanageer if system is null. 
                    // Ships are not getting added to the deepspace manager from here. 
                    if (ship.System == null)
                        ship.SetSystem(null);
                }
            }

            for (int i = 0; i < EmpireManager.Empires.Count; i++)
            {
                foreach (var kv in EmpireManager.Empires[i].GetFleetsDict())
                {
                    var fleet = kv.Value;
                    if (fleet.Ships.Count <= 0)
                        continue;
                    using (fleet.Ships.AcquireReadLock())
                    {
                        fleet.Setavgtodestination();
                        fleet.SetSpeed();
                        fleet.StoredFleetPosition = fleet.FindAveragePositionset();
                    }
                }
            }

            perfavg4.Stop();
        }

        private void ProcessTurnShipsAndSystems()
        {
            Perfavg2.Start();
#if !PLAYERONLY
            DeepSpaceThread();
            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystemList[i].Update(!Paused ? 0.01666667f : 0.0f, this);                
            }
#else
            FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(this.DeepSpaceThread);
            foreach (SolarSystem solarsystem in this.SolarSystemDict.Values)
            {
                SystemUpdaterTaskBased(solarsystem);
            }
            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();
 #endif
            Perfavg2.Stop();
        }

        private bool ProcessTurnEmpires(float elapsedTime)
        {
            PreEmpirePerf.Start();
            zTime = elapsedTime;

            if (!IsActive)
            {
                ShowingSysTooltip = false;
                ShowingPlanetToolTip = false;
            }
            RecomputeFleetButtons(false);
            if (SelectedShip != null)
            {
                ProjectPieMenu(SelectedShip.Position, 0.0f);
            }
            else if (SelectedPlanet != null)
            {
                ProjectPieMenu(SelectedPlanet.Center, 2500f);
            }
            if (GlobalStats.RemnantArmageddon)
            {
                if (!Paused) ArmageddonTimer -= elapsedTime;
                if (ArmageddonTimer < 0.0)
                {
                    ArmageddonTimer = 300f;
                    ++ArmageddonCounter;
                    if (ArmageddonCounter > 5)
                        ArmageddonCounter = 5;
                    for (int i = 0; i < ArmageddonCounter; ++i)
                    {
                        Ship exterminator = Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                                player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
                                    RandomMath.RandomBetween(-500000f, 500000f)));
                        exterminator.AI.DefaultAIState = AIState.Exterminate;
                    }
                }
            }                
            //clear out general object removal.
            TotallyRemoveGameplayObjects();
            MasterShipList.ApplyPendingRemovals();
            //Create New Ship SceneObjecst
            AddShipSceneObjectsFromQueue();



            if (Paused)
            {
                PreEmpirePerf.Stop();
                return false;
            }

            bool rebuildPathingMap = false; // REBUILD WHAT??? Pathing map.

            for (int i = 0; i < EmpireManager.Empires.Count; i++)
            {
                var empire = EmpireManager.Empires[i];

                if (!empire.isPlayer)
                {
                    Ship[] forcePool = empire.GetForcePool().ToArray();
                    empire.GetForcePool().Clear();
                    for (int j = forcePool.Length - 1; j >= 0; j--)
                    {
                        Ship ship = forcePool[j];
                        empire.ForcePoolAdd(ship);
                    }
                }

                foreach (Ship s in empire.ShipsToAdd)
                {
                    empire.AddShip(s);
                    if (!empire.isPlayer) empire.ForcePoolAdd(s);
                }

                empire.ShipsToAdd.Clear();
                empire.updateContactsTimer -= 0.01666667f; //elapsedTime;
                if (empire.updateContactsTimer <= 0f && !empire.data.Defeated)
                {
                    int check = empire.BorderNodes.Count;
                    empire.ResetBorders();

                    if (empire.BorderNodes.Count != check)
                    {
                        rebuildPathingMap = true;
                        empire.PathCache.Clear();
                    }
                    foreach (Ship ship in MasterShipList)
                        ship.BorderCheck.Remove(empire); // added by gremlin reset border stats.

                    empire.UpdateKnownShips();
                    empire.updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
                }
            }
            if (rebuildPathingMap)
                DoPathingMapRebuild();

            PreEmpirePerf.Stop();

            if (!IsActive)
                return true;

            EmpireUpdatePerf.Start();
            for (var i = 0; i < EmpireManager.Empires.Count; i++)
            {
                Empire empire = EmpireManager.Empires[i];
                empire.Update(elapsedTime);
            }
            MasterShipList.ApplyPendingRemovals();

            shiptimer -= elapsedTime; // 0.01666667f;//
            EmpireUpdatePerf.Stop();
            return false;
        }

        private void DoPathingMapRebuild()
        {
            reducer = (int) (SubSpaceProjectors.Radius * .75f);
            int granularity = (int) (UniverseSize / reducer);
            int elegran = granularity * 2;
            int elements = elegran < 128 ? 128 : elegran < 256 ? 256 : elegran < 512 ? 512 : 1024;
            byte[,] grid = new byte[elements, elements];
            for (int x = 0; x < elements; x++)
            for (int y = 0; y < elements; y++)
            {
                if (x > elegran || y > elegran)
                    grid[x, y] = 0;
                else
                    grid[x, y] = 80;
            }
            foreach (Planet p in PlanetsDict.Values)
            {
                int x = granularity;
                int y = granularity;
                float xround = p.Center.X > 0 ? .5f : -.5f;
                float yround = p.Center.Y > 0 ? .5f : -.5f;
                x += (int) (p.Center.X / reducer + xround);
                y += (int) (p.Center.Y / reducer + yround);
                if (y < 0) y = 0;
                if (x < 0) x = 0;
                grid[x, y] = 200;
            }

            for (int i = 0; i < EmpireManager.Empires.Count; i++)
            {
                var empire = EmpireManager.Empires[i];

                byte[,] grid1 = (byte[,]) grid.Clone();
                PathGridtranslateBordernode(empire, 1, grid1);

                foreach (KeyValuePair<Empire, Relationship> rels in empire.AllRelations)
                {
                    if (!rels.Value.Known)
                        continue;
                    if (rels.Value.Treaty_Alliance)
                    {
                        PathGridtranslateBordernode(rels.Key, 1, grid1);
                    }
                    if (rels.Value.AtWar)
                        PathGridtranslateBordernode(rels.Key, 80, grid1);
                    else if (!rels.Value.Treaty_OpenBorders)
                        PathGridtranslateBordernode(rels.Key, 0, grid1);
                }

                empire.grid = grid1;
                empire.granularity = granularity;
            }
        }
        
        private void DeepSpaceThread()
        {
            float elapsedTime = !Paused ? 0.01666667f : 0.0f;

            SpaceManager.GetDeepSpaceShips(DeepSpaceShips);

            for (int i = 0; i < DeepSpaceShips.Count; i++)
            {
                if (!DeepSpaceShips[i].ShipInitialized)
                    continue;

                if (DeepSpaceShips[i].Active && !DeepSpaceShips[i].ModuleSlotsDestroyed)
                {
                    if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                    {
                        DeepSpaceShips[i].Inhibited = true;
                        DeepSpaceShips[i].InhibitedTimer = 10f;
                    }

                    if (DeepSpaceShips[i].PlayerShip)
                        DeepSpaceShips[i].ProcessInput(elapsedTime);
                }
                else
                {
                    DeepSpaceShips[i].Die(null, true);
                }
                DeepSpaceShips[i].Update(elapsedTime);
            }
        }

        public void UpdateAllSystems(float elapsedTime)
        {
            if (IsExiting)
                return;

            foreach (SolarSystem system in SolarSystemList)
            {
                system.Update(elapsedTime, this);
            }
        }

        private void HandleGameSpeedChange(InputState input)
        {
            if (input.SpeedReset)
                GameSpeed = 1f;
            else if (input.SpeedUp || input.SpeedDown)
            {
                bool unlimited = GlobalStats.UnlimitedSpeed || Debug;
                float speedMin = unlimited ? 0.0625f : 0.25f;
                float speedMax = unlimited ? 128f    : 6f;
                GameSpeed = GetGameSpeedAdjust(input.SpeedUp).Clamped(speedMin, speedMax);
            }
        }

        private float GetGameSpeedAdjust(bool increase)
        {
            return increase
                ? GameSpeed <= 1 ? GameSpeed * 2 : GameSpeed + 1
                : GameSpeed <= 1 ? GameSpeed / 2 : GameSpeed - 1;
        }
    }
}