using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Threading;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public readonly ActionPool AsyncDataCollector = new ActionPool();

        void ProcessTurnsMonitored()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Log.Write(ConsoleColor.Cyan, $"Start Universe.ProcessTurns Thread #{threadId}");
            Log.AddThreadMonitor();
            ProcessTurns();
            Log.RemoveThreadMonitor();
            Log.Write(ConsoleColor.Cyan, $"Stop Universe.ProcessTurns Thread #{threadId}");
        }

        void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (ProcessTurnsThread != null)
            {
                try
                {
                    // Wait for Draw() to finish. While SwapBuffers is blocking, we process the turns in between
                    DrawCompletedEvt.WaitOne();
                    if (ProcessTurnsThread == null)
                        break; // this thread is aborting

                    ProcessNextTurn();
                    failedLoops = 0; // no exceptions this turn
                }
                catch (ThreadAbortException)
                {
                    break; // Game over, Make sure to Quit the loop!
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
                    // if the debug window hits a cyclic crash it can be turned off in game.
                    // i don't see a point in crashing the game because of a debug window error.
                    try
                    {
                        if (Debug)
                            DebugWin?.Update(SimulationDeltaTime);
                    }
                    catch
                    {
                        Debug = false;
                        Log.Warning("DebugWindowCrashed");
                    }

                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        int UpdatesOnPause = 90;

        void ProcessNextTurn()
        {
            float deltaTime = FrameDeltaTime;
            ScreenManager.ExecutePendingEmpireActions();

            if (Paused)
            {
                ++TurnId;
                UpdateAllSystems(0.0f);
                DeepSpaceThread(0.0f);
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
                            ++TurnId;
                            ProcessTurnDelta(deltaTime);
                        }
                        TurnFlipCounter += GameSpeed;
                    }
                    else
                    {
                        // With higher GameSpeed, we take more than 1 turn
                        for (int numTurns = 0; numTurns < GameSpeed && IsActive; ++numTurns)
                        {
                            ++TurnId;
                            ProcessTurnDelta(deltaTime);
                            deltaTime = FrameDeltaTime;
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
        }

        // This is different than normal DeltaTime
        public float SimulationDeltaTime { get; private set; }

        void ProcessTurnDelta(float elapsedTime)
        {
            SimulationDeltaTime = elapsedTime;
            perfavg5.Start(); // total do work perf counter

            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            GlobalStats.ComparisonCounter += 1;
            GlobalStats.ModuleUpdates = 0;

            if (ProcessTurnEmpires(elapsedTime))
            {
                UpdateShipsAndFleets(elapsedTime);

                // this will update all ship Center coordinates
                ProcessTurnShipsAndSystems(elapsedTime);

                CollisionTime.Start();
                // update spatial manager after ships have moved.
                // all the collisions will be triggered here:
                lock(SpaceManager.LockSpaceManager)
                {
                    SpaceManager.Update(elapsedTime);

                    MasterShipList.ApplyPendingRemovals();
                    // bulk remove all dead projectiles to prevent their update next frame
                    RemoveDeadProjectiles();
                }
                QueueActionsForThreading(0.01666667f);
                AsyncDataCollector.MoveItemsToThread();
                CollisionTime.Stop();

                ProcessTurnUpdateMisc(elapsedTime);
            }

            perfavg5.Stop();
        }

        /// <summary>
        /// Used to make ships alive at game load
        /// </summary>
        public void WarmUpShipsForLoad()
        {
            // For some reason it takes about 90 frames to get the spacemanager initially updated
            for (int x = 0; x < 90; x++)
            {
                QueueActionsForThreading(0.016f);
                AsyncDataCollector.MoveItemsToThread();
                UpdateShipsAndFleets(0.016f);
                foreach (var ship in MasterShipList)
                    ship.AI.ApplySensorScanResults();
                lock (SpaceManager.LockSpaceManager);
                    SpaceManager.Update(0.016f);
            }
        }

        void RemoveDeadProjectiles()
        {
            for (int i = 0; i < DeepSpaceShips.Count; i++)
                DeepSpaceShips[i].RemoveDyingProjectiles();

            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystem system = SolarSystemList[i];
                for (int j = 0; j < system.ShipList.Count; ++j)
                    system.ShipList[j].RemoveDyingProjectiles();
            }
        }

        void ProcessTurnUpdateMisc(float elapsedTime)
        {
            UpdateClickableItems();

            bool flag1 = false;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; ++i)
                {
                    ClickablePlanets planet = ClickPlanetList[i];
                    if (Input.CursorPosition.InRadius(planet.ScreenPos, planet.Radius))
                    {
                        flag1 = true;
                        TooltipTimer -= 0.01666667f;
                        tippedPlanet = planet;
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

            bool clickedOnSystem = false;
            if (viewState > UnivScreenState.SectorView)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int i = 0; i < ClickableSystems.Count; ++i)
                    {
                        ClickableSystem system = ClickableSystems[i];
                        if (Input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                        {
                            sTooltipTimer -= 0.01666667f;
                            tippedSystem = system;
                            clickedOnSystem = true;
                        }
                    }
                }
                if (sTooltipTimer <= 0f)
                    sTooltipTimer = 0.5f;
            }

            if (!clickedOnSystem)
                ShowingSysTooltip = false;

            JunkList.ApplyPendingRemovals();

            if (elapsedTime > 0)
            {
                ExplosionManager.Update(this, elapsedTime);
                MuzzleFlashManager.Update(elapsedTime);
            }

            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Update(elapsedTime);
            if (elapsedTime > 0)
            {
                using (BombList.AcquireReadLock())
                {
                    for (int i = 0; i < BombList.Count; ++i)
                    {
                        BombList[i]?.Update(elapsedTime);
                    }
                }
                BombList.ApplyPendingRemovals();
            }
            anomalyManager.AnomaliesList.ApplyPendingRemovals();
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(this, elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            SelectedShipList.ApplyPendingRemovals();
        }

        public void UpdateShipsAndFleets(float elapsedTime)
        {
            PostEmpirePerf.Start();

            if (!Paused && IsActive)
            {
                for (int i = 0; i < EmpireManager.Empires.Count; i++)
                {
                    var empire = EmpireManager.Empires[i];
                    foreach (KeyValuePair<int, Fleet> kv in
                        empire.GetFleetsDict())
                    {
                        kv.Value.SetSpeed();
                    }

                    empire.Pool.UpdatePools();
                    empire.GetEmpireAI().ThreatMatrix.ProcessPendingActions();
                }

                if (elapsedTime > 0.0f && --shiptimer <= 0.0f)
                {
                    shiptimer = 1f;
                    for (int i = 0; i < MasterShipList.Count; i ++)
                    {
                        var ship = MasterShipList[i];
                        {
                            if (!ship.InRadiusOfCurrentSystem)
                            {
                                ship.SetSystem(null);

                                for (int x = 0; x < SolarSystemList.Count; x++)
                                {
                                    SolarSystem system = SolarSystemList[x];

                                    if (ship.InRadiusOfSystem(system))
                                    {
                                        system.SetExploredBy(ship.loyalty);
                                        ship.SetSystem(system);
                                        // No need to keep looping through all other systems
                                        // if one is found -Gretman
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            PostEmpirePerf.Stop();
        }

        public void QueueActionsForThreading(float deltaTime)
        {

            AsyncDataCollector.Add(()=>
            {
                foreach(var ship in MasterShipList.AtomicCopy())
                {
                    if (ship == null) continue;
                    ship.KnownByEmpires.Update(deltaTime);
                    ship.AI.ScanForThreat(deltaTime);
                }
            });
            
            AsyncDataCollector.Add(() =>
            {
                foreach(var ship in MasterShipList.AtomicCopy())
                    ship.UpdateInfluence(deltaTime);
                foreach(var empire in EmpireManager.Empires)
                {
                    if (empire.IsEmpireDead()) continue;
                    empire.UpdateContactsAndBorders(deltaTime);
                    IReadOnlyList<Planet> list = empire.GetPlanets();
                    for (int i = 0; i < list.Count; i ++)
                    {
                        var planet = list[i];
                        planet.UpdateSpaceCombatBuildings(deltaTime); // building weapon timers are in this method. 
                    }
                
                    empire.UpdateMilitaryStrengths();
                }
            });

            AsyncDataCollector.Add(()=>
            {
                foreach(var ship in MasterShipList.ToArray())
                {
                    if (ship?.Active != true) continue;
                    ship.SetFleetCapableStatus();
                    ship.UpdateModulePositions(deltaTime);
                }
            });
        }

        void ProcessTurnShipsAndSystems(float elapsedTime)
        {
            Perfavg2.Start();
            float shipTime = !Paused ? 0.01666667f : 0;
            DeepSpaceThread(shipTime);
            var realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;

            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystemList[i].Update(shipTime, this, realTime);
            }
            Perfavg2.Stop();
        }

        bool ProcessTurnEmpires(float elapsedTime)
        {
            PreEmpirePerf.Start();

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

            if (Paused)
            {
                PreEmpirePerf.Stop();
                return true;
            }

            PreEmpirePerf.Stop();

            if (IsActive)
            {
                EmpireUpdatePerf.Start();
                for (var i = 0; i < EmpireManager.NumEmpires; i++)
                {
                    Empire empire = EmpireManager.Empires[i];
                    if (empire.data.Defeated) continue;
                    empire.Update(elapsedTime);
                }

                MasterShipList.ApplyPendingRemovals();

                EmpireUpdatePerf.Stop();
                return true;
            }

            return false;
        }

        public Vector2 PathMapPointToWorld(int x, int y, int universeOffSet)
        {
            return new Vector2((x - universeOffSet) * PathMapReducer,
                (y - universeOffSet) * PathMapReducer);
        }

        public Point WorldToPathMap(Vector2 worldPostion, int universeOffSet)
        {
            int x = universeOffSet;
            int y = universeOffSet;
            float xround = worldPostion.X > 0 ? .5f : -.5f;
            float yround = worldPostion.Y > 0 ? .5f : -.5f;
            x += (int)(worldPostion.X / PathMapReducer + xround);
            y += (int)(worldPostion.Y / PathMapReducer + yround);
            y = y.Clamped(0, universeOffSet * 2);
            x = x.Clamped(0, universeOffSet * 2);
            return new Point(x, y);
        }

        public void DeepSpaceThread(float elapsedTime)
        {
            SpaceManager.GetDeepSpaceShips(DeepSpaceShips);

            for (int i = 0; i < DeepSpaceShips.Count; i++)
            {
                DeepSpaceShips[i].Update(elapsedTime);
            }
        }

        public void UpdateAllSystems(float elapsedTime)
        {
            if (IsExiting)
                return;
            var realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;
            foreach (SolarSystem system in SolarSystemList)
                system.Update(elapsedTime, this, realTime);
        }

        void HandleGameSpeedChange(InputState input)
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

        float GetGameSpeedAdjust(bool increase)
        {
            return increase
                ? GameSpeed <= 1 ? GameSpeed * 2 : GameSpeed + 1
                : GameSpeed <= 1 ? GameSpeed / 2 : GameSpeed - 1;
        }
    }
}